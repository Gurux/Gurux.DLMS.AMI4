//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using System.Security.Claims;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc/>
    public class ScriptGroupRepository : IScriptGroupRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IUserGroupRepository _userGroupRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ScriptGroupRepository(
            IGXHost host,
            IUserRepository userRepository,
            IUserGroupRepository userGroupRepository,
            IGXEventsNotifier eventsNotifier)
        {
            _host = host;
            _userRepository = userRepository;
            _eventsNotifier = eventsNotifier;
            _userGroupRepository = userGroupRepository;
        }

        private async Task<List<GXUserGroup>> GetJoinedUserGroups(Guid scriptGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupScriptGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupScriptGroup, GXScriptGroup>(a => a.ScriptGroupId, b => b.Id);
            arg.Where.And<GXScriptGroup>(where => where.Removed == null && where.Id == scriptGroupId);
            return (await _host.Connection.SelectAsync<GXUserGroup>(arg));
        }

        private async Task<List<GXScript>> GetJoinedScripts(Guid scriptGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXScript>(where => where.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXScript, GXScriptGroupScript>(a => a.Id, b => b.ScriptId);
            arg.Joins.AddInnerJoin<GXScriptGroupScript, GXScriptGroup>(a => a.ScriptGroupId, b => b.Id);
            arg.Where.And<GXScriptGroup>(where => where.Removed == null && where.Id == scriptGroupId);
            return (await _host.Connection.SelectAsync<GXScript>(arg));
        }


        /// <inheritdoc />
        public async Task<List<GXScriptGroup>> GetJoinedScriptGroups(ClaimsPrincipal User, Guid scriptId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXScriptGroup>(where => where.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXScriptGroup, GXScriptGroupScript>(a => a.Id, b => b.ScriptGroupId);
            arg.Joins.AddInnerJoin<GXScriptGroupScript, GXScript>(a => a.ScriptId, b => b.Id);
            arg.Where.And<GXScript>(where => where.Removed == null && where.Id == scriptId);
            return (await _host.Connection.SelectAsync<GXScriptGroup>(arg));
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal User, Guid? groupId)
        {
            GXSelectArgs args = GXQuery.GetUsersByScriptGroup(ServerHelpers.GetUserId(User), groupId);
            args.Distinct = true;
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal User, IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXQuery.GetUsersByScriptGroups(ServerHelpers.GetUserId(User), groupIds);
            args.Distinct = true;
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal User,
            IEnumerable<Guid> userGrouprs,
            bool delete)
        {
            if (User == null || (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.ScriptGroupManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXScriptGroup>(a => new { a.Id, a.Name }, q => userGrouprs.Contains(q.Id));
            List<GXScriptGroup> list = _host.Connection.Select<GXScriptGroup>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXScriptGroup, List<string>> updates = new Dictionary<GXScriptGroup, List<string>>();
            foreach (GXScriptGroup it in list)
            {
                it.Removed = now;
                List<string> users = await GetUsersAsync(User, it.Id);
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXScriptGroup>(it.Id));
                }
                else
                {
                    _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
                }
                updates[it] = users;
            }
            foreach (var it in updates)
            {
                GXScriptGroup tmp = new GXScriptGroup() { Id = it.Key.Id, Name = it.Key.Name };
                await _eventsNotifier.ScriptGroupDelete(it.Value, new GXScriptGroup[] { tmp });
            }
        }

        /// <inheritdoc />
        public async Task<GXScriptGroup[]> ListAsync(
            ClaimsPrincipal User,
            ListScriptGroups? request,
            ListScriptGroupsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the script groups.
                arg = GXSelectArgs.SelectAll<GXScriptGroup>();
            }
            else
            {
                string userId = Internal.ServerHelpers.GetUserId(User);
                arg = GXQuery.GetScriptGroupsByUser(userId, null);
            }
            if (request != null)
            {
                //If script groups are filtered by user.
                if (request.Filter?.UserGroups != null)
                {
                    var ug = request.Filter.UserGroups.FirstOrDefault();
                    if (ug?.Users != null && ug.Users.Any())
                    {
                        var user = ug.Users.FirstOrDefault();
                        if (user != null)
                        {
                            arg.Joins.AddLeftJoin<GXUserGroupScriptGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                            arg.Joins.AddLeftJoin<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId);
                            arg.Joins.AddLeftJoin<GXUserGroupUser, GXUser>(j => j.UserId, j => j.Id);
                            arg.Where.FilterBy(user);
                        }
                    }
                    request.Filter.UserGroups = null;
                }
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXScriptGroup>(w => !request.Exclude.Contains(w.Id));
                }
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXScriptGroup>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXScriptGroup>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXScriptGroup>(q => q.CreationTime);
            }
            GXScriptGroup[] groups = (await _host.Connection.SelectAsync<GXScriptGroup>(arg)).ToArray();
            if (response != null)
            {
                response.ScriptGroups = groups;
                if (response.Count == 0)
                {
                    response.Count = groups.Length;
                }
            }
            return groups;
        }

        /// <inheritdoc />
        public async Task<GXScriptGroup> ReadAsync(
        ClaimsPrincipal user,
         Guid id)
        {
            bool isAdmin = false;
            if (user != null)
            {
                isAdmin = user.IsInRole(GXRoles.Admin);
            }
            GXSelectArgs arg;
            if (user == null || isAdmin)
            {
                //Admin can see all the groups.
                arg = GXSelectArgs.SelectAll<GXScriptGroup>(w => w.Id == id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetScriptGroupsByUser(userId, id);
            }
            arg.Columns.Exclude<GXScriptGroup>(e => e.Scripts);
            arg.Distinct = true;
            var group = (await _host.Connection.SingleOrDefaultAsync<GXScriptGroup>(arg));
            if (group == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            ////////////////////////////////////////////////////
            //Get scripts that belongs for this script group.
            arg = GXSelectArgs.SelectAll<GXScript>(w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXScriptGroupScript, GXScript>(j => j.ScriptId, j => j.Id);
            arg.Where.And<GXScriptGroupScript>(q => q.Removed == null && q.ScriptGroupId == id);
            group.Scripts = await _host.Connection.SelectAsync<GXScript>(arg);

            ////////////////////////////////////////////////////
            //Get user groups that belongs for this script group.
            arg = GXSelectArgs.SelectAll<GXUserGroup>(w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXUserGroupScriptGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
            arg.Where.And<GXUserGroupScriptGroup>(q => q.Removed == null && q.ScriptGroupId == id);
            group.UserGroups = await _host.Connection.SelectAsync<GXUserGroup>(arg);
            return group;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal user,
            IEnumerable<GXScriptGroup> ScriptGroups,
            Expression<Func<GXScriptGroup, object?>>? columns)
        {
            string userId = ServerHelpers.GetUserId(user);
            DateTime now = DateTime.Now;
            List<Guid> list = new List<Guid>();
            Dictionary<GXScriptGroup, List<string>> updates = new Dictionary<GXScriptGroup, List<string>>();
            foreach (GXScriptGroup it in ScriptGroups)
            {
                if (string.IsNullOrEmpty(it.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                if (it.UserGroups == null || !it.UserGroups.Any())
                {
                    //Get default user groups.
                    if (user != null)
                    {
                        it.UserGroups = await _userGroupRepository.GetDefaultUserGroups(user,
                                                   ServerHelpers.GetUserId(user));
                    }
                    if (it.UserGroups == null || !it.UserGroups.Any())
                    {
                        throw new ArgumentException(Properties.Resources.TargetMustBelongToOneGroup);
                    }
                }
                if (it.Scripts != null)
                {
                    foreach (GXScript script in it.Scripts)
                    {
                        script.CreationTime = now;
                        script.Creator = new GXUser() { Id = userId };
                    }
                }
                if (it.Id == Guid.Empty)
                {
                    it.CreationTime = now;
                    GXInsertArgs args = GXInsertArgs.Insert(it);
                    //User groups must hanlde separetly because users are identified with name and not Guid.
                    args.Exclude<GXScriptGroup>(e => e.UserGroups);
                    _host.Connection.Insert(args);
                    list.Add(it.Id);
                    AddScriptGroupToUserGroups(it.Id, it.UserGroups.Select(s => s.Id).ToArray());
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXScriptGroup>(q => q.ConcurrencyStamp, where => where.Id == it.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    it.Updated = now;
                    it.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(it);
                    args.Exclude<GXScriptGroup>(q => new { q.UserGroups, q.CreationTime, q.Scripts });
                    _host.Connection.Update(args);
                    //Map user group to Script group.
                    List<GXUserGroup> list2 = await GetJoinedUserGroups(it.Id);
                    List<Guid> groups = list2.Select(s => s.Id).ToList();
                    Guid[] tmp = it.UserGroups.Select(s => s.Id).ToArray();
                    Guid[] removed = groups.Except(tmp).ToArray();
                    Guid[] added = tmp.Except(groups).ToArray();
                    if (added.Length != 0)
                    {
                        AddScriptGroupToUserGroups(it.Id, added);
                    }
                    if (removed.Length != 0)
                    {
                        RemoveScriptGroupFromUserGroups(it.Id, removed);
                    }
                    //Map scripts to Script group.
                    if (it.Scripts != null)
                    {
                        List<GXScript> list3 = await GetJoinedScripts(it.Id);
                        List<Guid> groups2 = list3.Select(s => s.Id).ToList();
                        tmp = it.Scripts.Select(s => s.Id).ToArray();
                        removed = groups2.Except(tmp).ToArray();
                        added = tmp.Except(groups2).ToArray();
                        if (added.Length != 0)
                        {
                            AddScriptsToScriptGroup(it.Id, added);
                        }
                        if (removed.Length != 0)
                        {
                            RemoveScriptsFromScriptGroup(it.Id, removed);
                        }
                    }
                }
                updates[it] = await GetUsersAsync(user, it.Id);
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.ScriptGroupUpdate(it.Value, new GXScriptGroup[] { it.Key });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map script group to user groups.
        /// </summary>
        /// <param name="scriptGroupId">Script group ID.</param>
        /// <param name="groups">Group IDs of the script groups where the script is added.</param>
        public void AddScriptGroupToUserGroups(Guid scriptGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupScriptGroup> list = new List<GXUserGroupScriptGroup>();
            foreach (var ug in groups)
            {
                list.Add(new GXUserGroupScriptGroup()
                {
                    ScriptGroupId = scriptGroupId,
                    UserGroupId = ug,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between script group and user groups.
        /// </summary>
        /// <param name="scriptGroupId">Script group ID.</param>
        /// <param name="groups">Group IDs of the script groups where the script is removed.</param>
        public void RemoveScriptGroupFromUserGroups(Guid scriptGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupScriptGroup>();
            foreach (var ug in groups)
            {
                args.Where.Or<GXUserGroupScriptGroup>(w => w.UserGroupId == ug &&
                    w.ScriptGroupId == scriptGroupId);
            }
            _host.Connection.Delete(args);
        }

        /// <summary>
        /// Map scripts to script group.
        /// </summary>
        /// <param name="scriptGroupId">Script group ID.</param>
        /// <param name="groups">Group IDs of the script groups where the script is added.</param>
        public void AddScriptsToScriptGroup(Guid scriptGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXScriptGroupScript> list = new List<GXScriptGroupScript>();
            foreach (var ug in groups)
            {
                list.Add(new GXScriptGroupScript()
                {
                    ScriptGroupId = scriptGroupId,
                    ScriptId = ug,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between scripts and script group.
        /// </summary>
        /// <param name="scriptGroupId">Script group ID.</param>
        /// <param name="groups">Group IDs of the script groups where the script is removed.</param>
        public void RemoveScriptsFromScriptGroup(Guid scriptGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXScriptGroupScript>();
            foreach (var it in groups)
            {
                args.Where.Or<GXScriptGroupScript>(w => w.ScriptId == it &&
                    w.ScriptGroupId == scriptGroupId);
            }
            _host.Connection.Delete(args);
        }
    }
}