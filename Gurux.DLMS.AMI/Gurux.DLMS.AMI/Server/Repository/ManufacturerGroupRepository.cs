///
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
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using System.Linq;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class ManufacturerGroupRepository : IManufacturerGroupRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly IUserRepository _userRepository;



        /// <summary>
        /// Constructor.
        /// </summary>
        public ManufacturerGroupRepository(
            IGXHost host,
            IGXEventsNotifier eventsNotifier,
            IUserGroupRepository userGroupRepository,
            IUserRepository userRepository)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userGroupRepository = userGroupRepository;
            _userRepository = userRepository;
        }

        /// <inheritdoc />
        public async Task<List<GXManufacturerGroup>> GetJoinedManufacturerGroups(ClaimsPrincipal user, Guid moduleId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXManufacturerGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXManufacturerGroup, GXManufacturerGroupManufacturer>(a => a.Id, b => b.ManufacturerGroupId);
            arg.Joins.AddInnerJoin<GXManufacturerGroupManufacturer, GXManufacturer>(a => a.ManufacturerId, b => b.Id);
            arg.Where.And<GXManufacturer>(where => where.Removed == null && where.Id == moduleId);
            return (await _host.Connection.SelectAsync<GXManufacturerGroup>(arg));
        }

        private async Task<List<GXUserGroup>> GetJoinedUserGroups(Guid manufacturerGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupManufacturerGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupManufacturerGroup, GXManufacturerGroup>(a => a.ManufacturerGroupId, b => b.Id);
            arg.Where.And<GXManufacturerGroup>(where => where.Removed == null && where.Id == manufacturerGroupId);
            return (await _host.Connection.SelectAsync<GXUserGroup>(arg));
        }

        private async Task<List<GXManufacturer>> GetJoinedManufacturers(ClaimsPrincipal user, Guid manufacturerGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXManufacturer>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXManufacturer, GXManufacturerGroupManufacturer>(a => a.Id, b => b.ManufacturerId);
            arg.Joins.AddInnerJoin<GXManufacturerGroupManufacturer, GXManufacturerGroup>(a => a.ManufacturerGroupId, b => b.Id);

            arg.Joins.AddInnerJoin<GXManufacturerGroup, GXUserGroupManufacturerGroup>(j => j.Id, j => j.ManufacturerGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupManufacturerGroup, GXUserGroup>(x => x.UserGroupId, y => y.Id);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(x => x.Id, y => y.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(x => x.UserId, y => y.Id);
            var id = ServerHelpers.GetUserId(user);
            arg.Where.And<GXUser>(q => q.Removed == null && q.Id == id);
            arg.Where.And<GXManufacturerGroup>(where => where.Removed == null && where.Id == manufacturerGroupId);
            return (await _host.Connection.SelectAsync<GXManufacturer>(arg));
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal user, Guid? groupId)
        {
            GXSelectArgs args = GXQuery.GetUsersByManufacturerGroup(ServerHelpers.GetUserId(user), groupId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal user, IEnumerable<Guid>? agentIds)
        {
            GXSelectArgs args = GXQuery.GetUsersByManufacturerGroups(ServerHelpers.GetUserId(user), agentIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> userGrouprs,
            bool delete)
        {
            if (User == null || (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.ManufacturerGroupManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXManufacturerGroup>(a => new { a.Id, a.Name }, q => userGrouprs.Contains(q.Id));
            List<GXManufacturerGroup> list = _host.Connection.Select<GXManufacturerGroup>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXManufacturerGroup, List<string>> updates = new Dictionary<GXManufacturerGroup, List<string>>();
            foreach (GXManufacturerGroup it in list)
            {
                it.Removed = now;
                List<string> users = await GetUsersAsync(User, it.Id);
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXManufacturerGroup>(it.Id));
                }
                else
                {
                    _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
                }
                updates[it] = users;
            }
            foreach (var it in updates)
            {
                GXManufacturerGroup tmp = new GXManufacturerGroup() { Id = it.Key.Id, Name = it.Key.Name };
                await _eventsNotifier.ManufacturerGroupDelete(it.Value, new GXManufacturerGroup[] { tmp });
            }
        }

        /// <inheritdoc />
        public async Task<GXManufacturerGroup[]> ListAsync(
            ClaimsPrincipal user,
            ListManufacturerGroups? request,
            ListManufacturerGroupsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && user.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the groups.
                arg = GXSelectArgs.SelectAll<GXManufacturerGroup>();
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetManufacturerGroupsByUser(userId, null);
            }
            if (request != null)
            {
                //If manufacturer groups are filtered by user.
                if (request.Filter?.UserGroups != null)
                {
                    var ug = request.Filter.UserGroups.FirstOrDefault();
                    if (ug?.Users != null && ug.Users.Any())
                    {
                        var user2 = ug.Users.FirstOrDefault();
                        if (user2 != null)
                        {
                            arg.Joins.AddLeftJoin<GXUserGroupManufacturerGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                            arg.Joins.AddLeftJoin<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId);
                            arg.Joins.AddLeftJoin<GXUserGroupUser, GXUser>(j => j.UserId, j => j.Id);
                            arg.Where.FilterBy(user2);
                        }
                    }
                    request.Filter.UserGroups = null;
                }
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXManufacturerGroup>(w => request.Exclude.Contains(w.Id) == false);
                }
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXManufacturerGroup>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXManufacturerGroup>(q => q.CreationTime);
                arg.Descending = true;
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXManufacturerGroup>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            GXManufacturerGroup[] groups = (await _host.Connection.SelectAsync<GXManufacturerGroup>(arg)).ToArray();
            if (response != null)
            {
                response.ManufacturerGroups = groups;
                if (response.Count == 0)
                {
                    response.Count = groups.Length;
                }
            }
            return groups;
        }

        /// <inheritdoc />
        public async Task<GXManufacturerGroup> ReadAsync(
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
                arg = GXSelectArgs.SelectAll<GXManufacturerGroup>(w => w.Id == id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetManufacturerGroupsByUser(userId, id);
            }
            arg.Columns.Add<GXManufacturerGroup>();
            arg.Columns.Exclude<GXManufacturerGroup>(e => e.Manufacturers);
            arg.Distinct = true;
            var group = (await _host.Connection.SingleOrDefaultAsync<GXManufacturerGroup>(arg));
            if (group == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            ////////////////////////////////////////////////////
            //Get manufacturers that belong for this manufacturer group.
            arg = GXSelectArgs.Select<GXManufacturer>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            //It might be that there are no manufacturers in the group. For that reason left join is used.
            arg.Joins.AddLeftJoin<GXManufacturerGroupManufacturer, GXManufacturer>(j => j.ManufacturerId, j => j.Id);
            arg.Where.And<GXManufacturerGroupManufacturer>(q => q.Removed == null && q.ManufacturerGroupId == id);
            group.Manufacturers = await _host.Connection.SelectAsync<GXManufacturer>(arg);

            ////////////////////////////////////////////////////
            //Get user groups that belong for this manufacturer group.
            arg = GXSelectArgs.Select<GXUserGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            //It might be that there are no manufacturers in the group. For that reason left join is used.
            arg.Joins.AddInnerJoin<GXUserGroupManufacturerGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
            arg.Where.And<GXUserGroupManufacturerGroup>(q => q.Removed == null && q.ManufacturerGroupId == id);
            group.UserGroups = await _host.Connection.SelectAsync<GXUserGroup>(arg);
            return group;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal user,
            IEnumerable<GXManufacturerGroup> ManufacturerGroups,
            Expression<Func<GXManufacturerGroup, object?>>? columns)
        {
            DateTime now = DateTime.Now;
            List<Guid> list = new List<Guid>();
            Dictionary<GXManufacturerGroup, List<string>> updates = new Dictionary<GXManufacturerGroup, List<string>>();
            foreach (GXManufacturerGroup it in ManufacturerGroups)
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
                if (it.Id == Guid.Empty)
                {
                    it.CreationTime = now;
                    GXInsertArgs args = GXInsertArgs.Insert(it);
                    //User groups must hanlde separetly because users are identified with name and not Guid.
                    args.Exclude<GXManufacturerGroup>(e => new { e.UserGroups });
                    _host.Connection.Insert(args);
                    list.Add(it.Id);
                    AddManufacturerGroupToUserGroups(it.Id, it.UserGroups.Select(s => s.Id).ToArray());
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXManufacturerGroup>(q => q.ConcurrencyStamp, where => where.Id == it.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    it.Updated = now;
                    it.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                    args.Exclude<GXManufacturerGroup>(q => new { q.UserGroups, q.CreationTime, q.Manufacturers });
                    _host.Connection.Update(args);
                    //Map user group to Manufacturer group.
                    List<GXUserGroup> list2 = await GetJoinedUserGroups(it.Id);
                    List<Guid> groups = list2.Select(s => s.Id).ToList();
                    Guid[] tmp = it.UserGroups.Select(s => s.Id).ToArray();
                    Guid[] removed = groups.Except(tmp).ToArray();
                    Guid[] added = tmp.Except(groups).ToArray();
                    if (added.Length != 0)
                    {
                        AddManufacturerGroupToUserGroups(it.Id, added);
                    }
                    if (removed.Length != 0)
                    {
                        RemoveManufacturerGroupFromUserGroups(it.Id, removed);
                    }
                    //Map manufacturers to Manufacturer group.
                    if (it.Manufacturers != null && it.Manufacturers.Count != 0)
                    {
                        List<GXManufacturer> list3 = await GetJoinedManufacturers(user, it.Id);
                        List<Guid> groups2 = list3.Select(s => s.Id).ToList();
                        tmp = it.Manufacturers.Select(s => s.Id).ToArray();
                        removed = groups2.Except(tmp).ToArray();
                        added = tmp.Except(groups2).ToArray();
                        if (added.Length != 0)
                        {
                            AddManufacturersToManufacturerGroup(it.Id, added);
                        }
                        if (removed.Length != 0)
                        {
                            RemoveManufacturersFromManufacturerGroup(it.Id, removed);
                        }
                    }
                }
                updates[it] = await GetUsersAsync(user, it.Id);
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.ManufacturerGroupUpdate(it.Value, new GXManufacturerGroup[] { it.Key });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map manufacturer group to user groups.
        /// </summary>
        /// <param name="manufacturerGroupId">Manufacturer group ID.</param>
        /// <param name="groups">Group IDs of the manufacturer groups where the manufacturer is added.</param>
        public void AddManufacturerGroupToUserGroups(Guid manufacturerGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupManufacturerGroup> list = new List<GXUserGroupManufacturerGroup>();
            foreach (var ug in groups)
            {
                list.Add(new GXUserGroupManufacturerGroup()
                {
                    ManufacturerGroupId = manufacturerGroupId,
                    UserGroupId = ug,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between manufacturer group and user groups.
        /// </summary>
        /// <param name="manufacturerGroupId">Manufacturer group ID.</param>
        /// <param name="groups">Group IDs of the manufacturer groups where the manufacturer is removed.</param>
        public void RemoveManufacturerGroupFromUserGroups(Guid manufacturerGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupManufacturerGroup>();
            foreach (var ug in groups)
            {
                args.Where.Or<GXUserGroupManufacturerGroup>(w => w.UserGroupId == ug &&
                    w.ManufacturerGroupId == manufacturerGroupId);
            }
            _host.Connection.Delete(args);
        }

        /// <summary>
        /// Map manufacturers to manufacturer group.
        /// </summary>
        /// <param name="manufacturerGroupId">Manufacturer group ID.</param>
        /// <param name="groups">Group IDs of the manufacturer groups where the manufacturer is added.</param>
        public void AddManufacturersToManufacturerGroup(Guid manufacturerGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXManufacturerGroupManufacturer> list = new List<GXManufacturerGroupManufacturer>();
            foreach (var ug in groups)
            {
                list.Add(new GXManufacturerGroupManufacturer()
                {
                    ManufacturerGroupId = manufacturerGroupId,
                    ManufacturerId = ug,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between manufacturers and manufacturer group.
        /// </summary>
        /// <param name="manufacturerGroupId">Manufacturer group ID.</param>
        /// <param name="groups">Group IDs of the manufacturer groups where the manufacturer is removed.</param>
        public void RemoveManufacturersFromManufacturerGroup(Guid manufacturerGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXManufacturerGroupManufacturer>();
            foreach (var it in groups)
            {
                args.Where.Or<GXManufacturerGroupManufacturer>(w => w.ManufacturerId == it &&
                    w.ManufacturerGroupId == manufacturerGroupId);
            }
            _host.Connection.Delete(args);
        }
    }
}