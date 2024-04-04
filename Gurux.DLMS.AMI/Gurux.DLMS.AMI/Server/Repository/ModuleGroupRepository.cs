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
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Client.Shared;
using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Client.Pages.User;
using Gurux.DLMS.AMI.Shared.DTOs.Report;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class ModuleGroupRepository : IModuleGroupRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IUserGroupRepository _userGroupRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ModuleGroupRepository(
            IGXHost host,
            IUserRepository userRepository,
            IGXEventsNotifier eventsNotifier,
            IUserGroupRepository userGroupRepository)
        {
            _host = host;
            _userRepository = userRepository;
            _eventsNotifier = eventsNotifier;
            _userGroupRepository = userGroupRepository;
        }

        /// <inheritdoc />
        public async Task<List<GXUserGroup>> GetJoinedUserGroups(Guid moduleGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupModuleGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupModuleGroup, GXModuleGroup>(a => a.ModuleGroupId, b => b.Id);
            arg.Where.And<GXModuleGroup>(where => where.Removed == null && where.Id == moduleGroupId);
            return (await _host.Connection.SelectAsync<GXUserGroup>(arg));
        }

        /// <inheritdoc />
        public async Task<List<GXModule>> GetJoinedModules(Guid moduleGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXModule>();
            arg.Joins.AddInnerJoin<GXModule, GXModuleGroupModule>(a => a.Id, b => b.ModuleId);
            arg.Joins.AddInnerJoin<GXModuleGroupModule, GXModuleGroup>(a => a.ModuleGroupId, b => b.Id);
            arg.Where.And<GXModuleGroup>(where => where.Removed == null && where.Id == moduleGroupId);
            return (await _host.Connection.SelectAsync<GXModule>(arg));
        }

        /// <inheritdoc />
        public async Task<List<GXModuleGroup>> GetJoinedModuleGroups(string moduleId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXModuleGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXModuleGroup, GXModuleGroupModule>(a => a.Id, b => b.ModuleGroupId);
            arg.Joins.AddInnerJoin<GXModuleGroupModule, GXModule>(a => a.ModuleId, b => b.Id);
            arg.Where.And<GXModule>(where => where.Active.Value && where.Id == moduleId);
            return (await _host.Connection.SelectAsync<GXModuleGroup>(arg));
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal User, Guid? groupId)
        {
            GXSelectArgs args = GXQuery.GetUsersByModuleGroup(ServerHelpers.GetUserId(User), groupId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal User, IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXQuery.GetUsersByModuleGroups(ServerHelpers.GetUserId(User), groupIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(ClaimsPrincipal User,
            IEnumerable<Guid> userGrouprs,
            bool delete)
        {
            if (User == null || (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.ModuleGroupManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXModuleGroup>(a => a.Id, q => userGrouprs.Contains(q.Id));
            List<GXModuleGroup> list = _host.Connection.Select<GXModuleGroup>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXModuleGroup, List<string>> updates = new Dictionary<GXModuleGroup, List<string>>();
            foreach (GXModuleGroup it in list)
            {
                it.Removed = now;
                List<string> users = await GetUsersAsync(User, it.Id);
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXModuleGroup>(it.Id));
                }
                else
                {
                    _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
                }
                updates[it] = users;
            }
            foreach (var it in updates)
            {
                GXModuleGroup tmp = new GXModuleGroup() { Id = it.Key.Id };
                await _eventsNotifier.ModuleGroupDelete(it.Value, new GXModuleGroup[] { tmp });
            }
        }

        /// <inheritdoc />
        public async Task<GXModuleGroup[]> ListAsync(
        ClaimsPrincipal User,
            ListModuleGroups? request,
            ListModuleGroupsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the module groups.
                arg = GXSelectArgs.SelectAll<GXModuleGroup>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetModuleGroupsByUser(userId, null);
            }
            if (request != null)
            {
                //If module groups are filtered by user.
                if (request.Filter?.UserGroups != null)
                {
                    var ug = request.Filter.UserGroups.FirstOrDefault();
                    if (ug?.Users != null && ug.Users.Any())
                    {
                        var user = ug.Users.FirstOrDefault();
                        if (user != null)
                        {
                            arg.Joins.AddLeftJoin<GXUserGroupModuleGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
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
                    arg.Where.And<GXModuleGroup>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXModuleGroup>(w => request.Included.Contains(w.Id));
                }
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXModuleGroup>(q => GXSql.DistinctCount(q.Id));
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
                arg.OrderBy.Add<GXModuleGroup>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXModuleGroup>(q => q.CreationTime);
            }
            GXModuleGroup[] groups = (await _host.Connection.SelectAsync<GXModuleGroup>(arg)).ToArray();
            if (response != null)
            {
                response.ModuleGroups = groups;
                if (response.Count == 0)
                {
                    response.Count = groups.Length;
                }
            }
            return groups;
        }

        /// <inheritdoc />
        public async Task<GXModuleGroup> ReadAsync(
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
                arg = GXSelectArgs.SelectAll<GXModuleGroup>(w => w.Id == id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetModuleGroupsByUser(userId, id);
            }
            arg.Columns.Exclude<GXModuleGroup>(e => e.Modules);
            arg.Distinct = true;
            var group = (await _host.Connection.SingleOrDefaultAsync<GXModuleGroup>(arg));
            if (group == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            ////////////////////////////////////////////////////
            //Get modules that belongs for this module group.
            arg = GXSelectArgs.Select<GXModule>(s => s.Id);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXModuleGroupModule, GXModule>(j => j.ModuleId, j => j.Id);
            arg.Where.And<GXModuleGroupModule>(q => q.Removed == null && q.ModuleGroupId == id);
            group.Modules = await _host.Connection.SelectAsync<GXModule>(arg);

            ////////////////////////////////////////////////////
            //Get user groups that belongs for this module group.
            arg = GXSelectArgs.Select<GXUserGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXUserGroupModuleGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
            arg.Where.And<GXUserGroupModuleGroup>(q => q.Removed == null && q.ModuleGroupId == id);
            group.UserGroups = await _host.Connection.SelectAsync<GXUserGroup>(arg);
            return group;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal User,
            IEnumerable<GXModuleGroup> ModuleGroups,
            Expression<Func<GXModuleGroup, object?>>? columns)
        {
            DateTime now = DateTime.Now;
            GXUser creator = new GXUser() { Id = ServerHelpers.GetUserId(User) };
            List<Guid> list = new List<Guid>();
            Dictionary<GXModuleGroup, List<string>> updates = new Dictionary<GXModuleGroup, List<string>>();
            foreach (GXModuleGroup it in ModuleGroups)
            {
                if (string.IsNullOrEmpty(it.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                if (it.UserGroups == null || !it.UserGroups.Any())
                {
                    //Get default user groups.
                    if (User != null)
                    {
                        it.UserGroups = await _userGroupRepository.GetDefaultUserGroups(User,
                           ServerHelpers.GetUserId(User));
                    }
                    if (it.UserGroups == null || !it.UserGroups.Any())
                    {
                        throw new ArgumentException(Properties.Resources.TargetMustBelongToOneGroup);
                    }
                }
                if (it.Id == Guid.Empty)
                {
                    it.Creator = creator;
                    it.CreationTime = now;
                    GXInsertArgs args = GXInsertArgs.Insert(it);
                    //User groups must hanlde separetly because users are identified with name and not Guid.
                    args.Exclude<GXModuleGroup>(e => new { e.Modules, e.UserGroups, e.Updated });
                    _host.Connection.Insert(args);
                    list.Add(it.Id);
                    AddModuleGroupToUserGroups(it.Id, it.UserGroups.Select(s => s.Id).ToArray());
                    if (it.Modules != null)
                    {
                        AddModulesToModuleGroup(it.Id, it.Modules.Select(s => s.Id).ToArray());
                    }
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXModuleGroup>(q => q.ConcurrencyStamp, where => where.Id == it.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }

                    List<string> users = await GetUsersAsync(User, it.Id);
                    it.Updated = now;
                    it.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                    args.Exclude<GXModuleGroup>(q => new
                    {
                        q.Modules,
                        q.UserGroups,
                        q.CreationTime
                    });
                    if (!User.IsInRole(GXRoles.Admin) ||
                        string.IsNullOrEmpty(it.Creator?.Id))
                    {
                        //Only admin can update the creator.
                        args.Exclude<GXModuleGroup>(q => q.Creator);
                    }
                    _host.Connection.Update(args);
                    //Map user group to Module group.
                    List<GXUserGroup> list2 = await GetJoinedUserGroups(it.Id);
                    List<Guid> groups = list2.Select(s => s.Id).ToList();
                    Guid[] tmp = it.UserGroups.Select(s => s.Id).ToArray();
                    Guid[] removed = groups.Except(tmp).ToArray();
                    Guid[] added = tmp.Except(groups).ToArray();
                    if (added.Length != 0)
                    {
                        AddModuleGroupToUserGroups(it.Id, added);
                    }
                    if (removed.Length != 0)
                    {
                        RemoveModuleGroupFromUserGroups(it.Id, removed);
                    }
                    //Map modules to Module group.
                    if (it.Modules != null)
                    {
                        List<GXModule> list3 = await GetJoinedModules(it.Id);
                        List<string> groups2 = list3.Select(s => s.Id).ToList();
                        string[] tmp2 = it.Modules.Select(s => s.Id).ToArray();
                        string[] removed2 = groups2.Except(tmp2).ToArray();
                        string[] added2 = tmp2.Except(groups2).ToArray();
                        if (added2.Length != 0)
                        {
                            AddModulesToModuleGroup(it.Id, added2);
                        }
                        if (removed2.Length != 0)
                        {
                            RemoveModulesFromModuleGroup(it.Id, removed2);
                        }
                    }
                    updates[it] = users;
                }
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.ModuleGroupUpdate(it.Value, new GXModuleGroup[] { it.Key });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map module group to user groups.
        /// </summary>
        /// <param name="moduleGroupId">Module group ID.</param>
        /// <param name="groups">Group IDs of the module groups where the module is added.</param>
        public void AddModuleGroupToUserGroups(Guid moduleGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupModuleGroup> list = new List<GXUserGroupModuleGroup>();
            foreach (var ug in groups)
            {
                list.Add(new GXUserGroupModuleGroup()
                {
                    ModuleGroupId = moduleGroupId,
                    UserGroupId = ug,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between module group and user groups.
        /// </summary>
        /// <param name="moduleGroupId">Module group ID.</param>
        /// <param name="groups">Group IDs of the module groups where the module is removed.</param>
        public void RemoveModuleGroupFromUserGroups(Guid moduleGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupModuleGroup>();
            foreach (var ug in groups)
            {
                args.Where.Or<GXUserGroupModuleGroup>(w => w.UserGroupId == ug &&
                    w.ModuleGroupId == moduleGroupId);
            }
            _host.Connection.Delete(args);
        }

        /// <summary>
        /// Map modules to module group.
        /// </summary>
        /// <param name="moduleGroupId">Module group ID.</param>
        /// <param name="groups">Group IDs of the module groups where the module is added.</param>
        public void AddModulesToModuleGroup(Guid moduleGroupId, IEnumerable<string> groups)
        {
            DateTime now = DateTime.Now;
            List<GXModuleGroupModule> list = new List<GXModuleGroupModule>();
            foreach (var ug in groups)
            {
                list.Add(new GXModuleGroupModule()
                {
                    ModuleGroupId = moduleGroupId,
                    ModuleId = ug,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between modules and module group.
        /// </summary>
        /// <param name="moduleGroupId">Module group ID.</param>
        /// <param name="groups">Group IDs of the module groups where the module is removed.</param>
        public void RemoveModulesFromModuleGroup(Guid moduleGroupId, IEnumerable<string> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXModuleGroupModule>();
            foreach (var it in groups)
            {
                args.Where.Or<GXModuleGroupModule>(w => w.ModuleId == it &&
                    w.ModuleGroupId == moduleGroupId);
            }
            _host.Connection.Delete(args);
        }
    }
}