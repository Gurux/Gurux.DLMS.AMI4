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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using System.Linq.Expressions;
using System.Linq;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Client.Pages.User;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class KeyManagementGroupRepository : IKeyManagementGroupRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IUserGroupRepository _userGroupRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public KeyManagementGroupRepository(
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

        private async Task<List<GXUserGroup>> GetJoinedUserGroups(Guid groupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupKeyManagementGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupKeyManagementGroup, GXKeyManagementGroup>(a => a.KeyManagementGroupId, b => b.Id);
            arg.Where.And<GXKeyManagementGroup>(where => where.Removed == null && where.Id == groupId);
            return (await _host.Connection.SelectAsync<GXUserGroup>(arg));
        }

        private async Task<List<GXKeyManagement>> GetJoinedKeyManagements(ClaimsPrincipal user, Guid groupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXKeyManagement>(where => where.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXKeyManagement, GXKeyManagementGroupKeyManagement>(a => a.Id, b => b.KeyManagementId);
            arg.Joins.AddInnerJoin<GXKeyManagementGroupKeyManagement, GXKeyManagementGroup>(a => a.KeyManagementGroupId, b => b.Id);
            arg.Joins.AddInnerJoin<GXKeyManagementGroup, GXUserGroupKeyManagementGroup>(j => j.Id, j => j.KeyManagementGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupKeyManagementGroup, GXUserGroup>(x => x.UserGroupId, y => y.Id);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(x => x.Id, y => y.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(x => x.UserId, y => y.Id);
            if (user != null && !user.IsInRole(GXRoles.Admin))
            {
                var id = ServerHelpers.GetUserId(user);
                arg.Where.And<GXUser>(q => q.Removed == null && q.Id == id);
            }
            arg.Where.And<GXKeyManagementGroup>(where => where.Removed == null && where.Id == groupId);
            return (await _host.Connection.SelectAsync<GXKeyManagement>(arg));
        }

        /// <inheritdoc />
        public async Task<List<GXKeyManagementGroup>> GetJoinedKeyManagementGroups(ClaimsPrincipal user, Guid keyId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXKeyManagementGroup>(where => where.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXKeyManagementGroup, GXKeyManagementGroupKeyManagement>(a => a.Id, b => b.KeyManagementGroupId);
            arg.Joins.AddInnerJoin<GXKeyManagementGroupKeyManagement, GXKeyManagement>(a => a.KeyManagementId, b => b.Id);

            arg.Joins.AddInnerJoin<GXKeyManagementGroup, GXUserGroupKeyManagementGroup>(j => j.Id, j => j.KeyManagementGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupKeyManagementGroup, GXUserGroup>(x => x.UserGroupId, y => y.Id);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(x => x.Id, y => y.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(x => x.UserId, y => y.Id);
            var id = ServerHelpers.GetUserId(user);
            arg.Where.And<GXUser>(q => q.Removed == null && q.Id == id);
            arg.Where.And<GXKeyManagement>(where => where.Removed == null && where.Id == keyId);
            return (await _host.Connection.SelectAsync<GXKeyManagementGroup>(arg));
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal user, Guid? groupId)
        {
            GXSelectArgs args = GXQuery.GetUsersByKeyManagementGroup(ServerHelpers.GetUserId(user), groupId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (user != null && user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal user, IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXQuery.GetUsersByKeyManagementGroups(ServerHelpers.GetUserId(user), groupIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (user != null && user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal User,
            IEnumerable<Guid> userGrouprs,
            bool delete)
        {
            if (User == null || (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.KeyManagementGroupManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXKeyManagementGroup>(a => new { a.Id, a.Name }, q => userGrouprs.Contains(q.Id));
            List<GXKeyManagementGroup> list = _host.Connection.Select<GXKeyManagementGroup>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXKeyManagementGroup, List<string>> updates = new Dictionary<GXKeyManagementGroup, List<string>>();
            foreach (GXKeyManagementGroup it in list)
            {
                it.Removed = now;
                List<string> users = await GetUsersAsync(User, it.Id);
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXKeyManagementGroup>(it.Id));
                }
                else
                {
                    _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
                }
                updates[it] = users;
            }
            foreach (var it in updates)
            {
                GXKeyManagementGroup tmp = new GXKeyManagementGroup() { Id = it.Key.Id, Name = it.Key.Name };
                await _eventsNotifier.KeyManagementGroupDelete(it.Value, new GXKeyManagementGroup[] { tmp });
            }
        }

        /// <inheritdoc />
        public async Task<GXKeyManagementGroup[]> ListAsync(
        ClaimsPrincipal User,
            ListKeyManagementGroups? request,
            ListKeyManagementGroupsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the key management groups.
                arg = GXSelectArgs.SelectAll<GXKeyManagementGroup>();
                arg.Joins.AddLeftJoin<GXKeyManagementGroup, GXUserGroupKeyManagementGroup>(j => j.Id, j => j.UserGroupId);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetKeyManagementGroupsByUser(userId, null);
            }
            if (request != null)
            {
                //If key management groups are filtered by user.
                if (request.Filter?.UserGroups != null)
                {
                    var ug = request.Filter.UserGroups.FirstOrDefault();
                    if (ug?.Users != null && ug.Users.Any())
                    {
                        var user = ug.Users.FirstOrDefault();
                        if (user != null)
                        {
                            arg.Joins.AddLeftJoin<GXUserGroupKeyManagementGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
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
                    arg.Where.And<GXKeyManagementGroup>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXKeyManagementGroup>(w => request.Included.Contains(w.Id));
                }
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXKeyManagementGroup>(q => GXSql.DistinctCount(q.Id));
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
                arg.OrderBy.Add<GXKeyManagementGroup>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXKeyManagementGroup>(q => q.CreationTime);
            }
            GXKeyManagementGroup[] groups = (await _host.Connection.SelectAsync<GXKeyManagementGroup>(arg)).ToArray();
            if (response != null)
            {
                response.KeyManagementGroups = groups;
                if (response.Count == 0)
                {
                    response.Count = groups.Length;
                }
            }
            return groups;
        }

        /// <inheritdoc />
        public async Task<GXKeyManagementGroup> ReadAsync(
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
                arg = GXSelectArgs.SelectAll<GXKeyManagementGroup>(w => w.Id == id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetKeyManagementGroupsByUser(userId, id);
            }
            arg.Columns.Exclude<GXKeyManagementGroup>(e => e.KeyManagements);
            arg.Distinct = true;
            var group = (await _host.Connection.SingleOrDefaultAsync<GXKeyManagementGroup>(arg));
            if (group == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            ////////////////////////////////////////////////////
            //Get key managements that belongs for this key management group.
            arg = GXSelectArgs.Select<GXKeyManagement>(s => new { s.Id, s.SystemTitle }, w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXKeyManagementGroupKeyManagement, GXKeyManagement>(j => j.KeyManagementId, j => j.Id);
            arg.Where.And<GXKeyManagementGroupKeyManagement>(q => q.Removed == null && q.KeyManagementGroupId == id);
            group.KeyManagements = await _host.Connection.SelectAsync<GXKeyManagement>(arg);

            ////////////////////////////////////////////////////
            //Get user groups that belongs for this key management group.
            arg = GXSelectArgs.Select<GXUserGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXUserGroupKeyManagementGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
            arg.Where.And<GXUserGroupKeyManagementGroup>(q => q.Removed == null && q.KeyManagementGroupId == id);
            group.UserGroups = await _host.Connection.SelectAsync<GXUserGroup>(arg);
            return group;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal user,
            IEnumerable<GXKeyManagementGroup> KeyManagementGroups,
            Expression<Func<GXKeyManagementGroup, object?>>? columns)
        {
            DateTime now = DateTime.Now;
            GXUser creator = new GXUser() { Id = ServerHelpers.GetUserId(user) };
            List<Guid> list = new List<Guid>();
            Dictionary<GXKeyManagementGroup, List<string>> updates = new Dictionary<GXKeyManagementGroup, List<string>>();
            foreach (GXKeyManagementGroup it in KeyManagementGroups)
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
                        it.UserGroups = await _userGroupRepository.GetDefaultUserGroups(user, creator.Id);
                    }
                    if (it.UserGroups == null || !it.UserGroups.Any())
                    {
                        throw new ArgumentException(Properties.Resources.TargetMustBelongToOneGroup);
                    }
                }
                if (it.KeyManagements != null)
                {
                    //Update creator and creation time for all key managements.
                    foreach (var key in it.KeyManagements)
                    {
                        key.CreationTime = now;
                        key.Creator = creator;
                    }
                }
                if (it.Id == Guid.Empty)
                {
                    it.Creator = creator;
                    it.CreationTime = now;
                    GXInsertArgs args = GXInsertArgs.Insert(it);
                    //User groups must hanlde separetly because users are identified with name and not Guid.
                    args.Exclude<GXKeyManagementGroup>(e => new { e.UserGroups });
                    _host.Connection.Insert(args);
                    list.Add(it.Id);
                    AddKeyManagementGroupToUserGroups(it.Id, it.UserGroups.Select(s => s.Id).ToArray());
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXKeyManagementGroup>(q => q.ConcurrencyStamp, where => where.Id == it.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    it.Updated = now;
                    it.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                    args.Exclude<GXKeyManagementGroup>(q => new
                    {
                        q.UserGroups,
                        q.CreationTime,
                        q.KeyManagements
                    });
                    if (!user.IsInRole(GXRoles.Admin) ||
                        it.Creator == null ||
                        string.IsNullOrEmpty(it.Creator.Id))
                    {
                        //Only admin can update the creator.
                        args.Exclude<GXKeyManagementGroup>(q => q.Creator);
                    }
                    _host.Connection.Update(args);
                    //Map user group to KeyManagement group.
                    List<GXUserGroup> list2 = await GetJoinedUserGroups(it.Id);
                    List<Guid> groups = list2.Select(s => s.Id).ToList();
                    Guid[] tmp = it.UserGroups.Select(s => s.Id).ToArray();
                    Guid[] removed = groups.Except(tmp).ToArray();
                    Guid[] added = tmp.Except(groups).ToArray();
                    if (added.Length != 0)
                    {
                        AddKeyManagementGroupToUserGroups(it.Id, added);
                    }
                    if (removed.Length != 0)
                    {
                        RemoveKeyManagementGroupFromUserGroups(it.Id, removed);
                    }
                    //Map key managements to KeyManagement group.
                    if (it.KeyManagements != null)
                    {
                        List<GXKeyManagement> list3 = await GetJoinedKeyManagements(user, it.Id);
                        List<Guid> groups2 = list3.Select(s => s.Id).ToList();
                        tmp = it.KeyManagements.Select(s => s.Id).ToArray();
                        removed = groups2.Except(tmp).ToArray();
                        added = tmp.Except(groups2).ToArray();
                        if (added.Length != 0)
                        {
                            AddKeyManagementsToKeyManagementGroup(it.Id, added);
                        }
                        if (removed.Length != 0)
                        {
                            RemoveKeyManagementsFromKeyManagementGroup(it.Id, removed);
                        }
                    }
                }
                updates[it] = await GetUsersAsync(user, it.Id);
            }
            foreach (var it in updates)
            {
                //Only ID and name are notified.
                await _eventsNotifier.KeyManagementGroupUpdate(it.Value,
                    new GXKeyManagementGroup[] { new GXKeyManagementGroup() { Id = it.Key.Id, Name = it.Key.Name } });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map key management group to user groups.
        /// </summary>
        /// <param name="groupId">KeyManagement group ID.</param>
        /// <param name="groups">Group IDs of the key management groups where the key management is added.</param>
        public void AddKeyManagementGroupToUserGroups(Guid groupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupKeyManagementGroup> list = new List<GXUserGroupKeyManagementGroup>();
            foreach (var ug in groups)
            {
                list.Add(new GXUserGroupKeyManagementGroup()
                {
                    KeyManagementGroupId = groupId,
                    UserGroupId = ug,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between key management group and user groups.
        /// </summary>
        /// <param name="groupId">KeyManagement group ID.</param>
        /// <param name="groups">Group IDs of the key management groups where the key management is removed.</param>
        public void RemoveKeyManagementGroupFromUserGroups(Guid groupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupKeyManagementGroup>();
            foreach (var ug in groups)
            {
                args.Where.Or<GXUserGroupKeyManagementGroup>(w => w.UserGroupId == ug &&
                    w.KeyManagementGroupId == groupId);
            }
            _host.Connection.Delete(args);
        }

        /// <summary>
        /// Map key managements to key management group.
        /// </summary>
        /// <param name="groupId">KeyManagement group ID.</param>
        /// <param name="groups">Group IDs of the key management groups where the key management is added.</param>
        public void AddKeyManagementsToKeyManagementGroup(Guid groupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXKeyManagementGroupKeyManagement> list = new List<GXKeyManagementGroupKeyManagement>();
            foreach (var ug in groups)
            {
                list.Add(new GXKeyManagementGroupKeyManagement()
                {
                    KeyManagementGroupId = groupId,
                    KeyManagementId = ug,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between key managements and key management group.
        /// </summary>
        /// <param name="groupId">KeyManagement group ID.</param>
        /// <param name="groups">Group IDs of the key management groups where the key management is removed.</param>
        public void RemoveKeyManagementsFromKeyManagementGroup(Guid groupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXKeyManagementGroupKeyManagement>();
            foreach (var it in groups)
            {
                args.Where.Or<GXKeyManagementGroupKeyManagement>(w => w.KeyManagementId == it &&
                    w.KeyManagementGroupId == groupId);
            }
            _host.Connection.Delete(args);
        }
    }
}