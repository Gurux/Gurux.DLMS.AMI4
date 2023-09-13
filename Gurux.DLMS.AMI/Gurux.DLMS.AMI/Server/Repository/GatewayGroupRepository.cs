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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Client.Shared;
using System.Linq.Expressions;
using System.Data;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class GatewayGroupRepository : IGatewayGroupRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IServiceProvider _serviceProvider;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly IUserRepository _userRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GatewayGroupRepository(
            IGXHost host,
            IServiceProvider serviceProvider,
            IUserGroupRepository userGroupRepository,
            IGXEventsNotifier eventsNotifier,
            IUserRepository userRepository)
        {
            _host = host;
            _serviceProvider = serviceProvider;
            _userGroupRepository = userGroupRepository;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
        }

        private async Task<List<GXUserGroup>> GetUserGroupsByGatewayGroupId(Guid gatewayGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupGatewayGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupGatewayGroup, GXGatewayGroup>(a => a.GatewayGroupId, b => b.Id);
            arg.Where.And<GXGatewayGroup>(where => where.Removed == null && where.Id == gatewayGroupId);
            return (await _host.Connection.SelectAsync<GXUserGroup>(arg));
        }

        private List<GXGateway> GetGatewaysByGatewayGroupId(Guid gatewayGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXGateway>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXGateway, GXGatewayGroupGateway>(a => a.Id, b => b.GatewayId);
            arg.Joins.AddInnerJoin<GXGatewayGroupGateway, GXGatewayGroup>(a => a.GatewayGroupId, b => b.Id);
            arg.Where.And<GXGatewayGroup>(where => where.Removed == null && where.Id == gatewayGroupId);
            return _host.Connection.Select<GXGateway>(arg);
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal user, Guid? groupId)
        {
            GXSelectArgs args = GXQuery.GetUsersByGatewayGroup(ServerHelpers.GetUserId(user), groupId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal user, IEnumerable<Guid>? gatewayIds)
        {
            GXSelectArgs args = GXQuery.GetUsersByGatewayGroups(ServerHelpers.GetUserId(user), gatewayIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> userGrouprs, bool delete)
        {
            if (User == null || (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.GatewayGroupManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXGatewayGroup>(a => new { a.Id, a.Name }, q => userGrouprs.Contains(q.Id));
            List<GXGatewayGroup> list = _host.Connection.Select<GXGatewayGroup>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXGatewayGroup, List<string>> updates = new Dictionary<GXGatewayGroup, List<string>>();
            using IDbTransaction transaction = _host.Connection.BeginTransaction();
            try
            {
                foreach (GXGatewayGroup it in list)
                {
                    it.Removed = now;
                    List<string> users = await GetUsersAsync(User, it.Id);
                    if (!delete)
                    {
                        await _host.Connection.UpdateAsync(transaction, GXUpdateArgs.Update(it, q => q.Removed));
                    }
                    updates[it] = users;
                }
                if (delete)
                {
                    await _host.Connection.DeleteAsync(transaction, GXDeleteArgs.DeleteRange(list));
                }
                _host.Connection.CommitTransaction(transaction);
            }
            catch (Exception)
            {
                _host.Connection.RollbackTransaction(transaction);
                throw;
            }
            foreach (var it in updates)
            {
                GXGatewayGroup tmp = new GXGatewayGroup() { Id = it.Key.Id, Name = it.Key.Name };
                await _eventsNotifier.GatewayGroupDelete(it.Value, new GXGatewayGroup[] { tmp });
            }
        }

        /// <inheritdoc />
        public async Task<GXGatewayGroup[]> ListAsync(
            ClaimsPrincipal User,
            ListGatewayGroups? request,
            ListGatewayGroupsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the gateway groups.
                arg = GXSelectArgs.SelectAll<GXGatewayGroup>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetGatewayGroupsByUser(userId, null);
            }
            if (request != null)
            {
                //If gateway groups are filtered by user.
                if (request.Filter?.UserGroups != null)
                {
                    var ug = request.Filter.UserGroups.FirstOrDefault();
                    if (ug?.Users != null && ug.Users.Any())
                    {
                        var user = ug.Users.FirstOrDefault();
                        if (user != null)
                        {
                            arg.Joins.AddLeftJoin<GXUserGroupGatewayGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
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
                    arg.Where.And<GXGatewayGroup>(w => !request.Exclude.Contains(w.Id));
                }
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXGatewayGroup>(q => GXSql.DistinctCount(q.Id));
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
                arg.OrderBy.Add<GXGatewayGroup>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXGatewayGroup>(q => q.CreationTime);
            }
            GXGatewayGroup[] groups = (await _host.Connection.SelectAsync<GXGatewayGroup>(arg)).ToArray();
            if (response != null)
            {
                response.GatewayGroups = groups;
                if (response.Count == 0)
                {
                    response.Count = groups.Length;
                }
            }
            return groups;
        }

        /// <inheritdoc />
        public async Task<GXGatewayGroup> ReadAsync(
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
                arg = GXSelectArgs.SelectAll<GXGatewayGroup>(w => w.Id == id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetGatewayGroupsByUser(userId, id);
            }
            arg.Columns.Exclude<GXGatewayGroup>(e => e.Gateways);
            arg.Distinct = true;
            var group = await _host.Connection.SingleOrDefaultAsync<GXGatewayGroup>(arg);
            if (group == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            ////////////////////////////////////////////////////
            //Get gateways that belongs for this gateway group.
            arg = GXSelectArgs.Select<GXGateway>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXGatewayGroupGateway, GXGateway>(j => j.GatewayId, j => j.Id);
            arg.Where.And<GXGatewayGroupGateway>(q => q.Removed == null && q.GatewayGroupId == id);
            group.Gateways = await _host.Connection.SelectAsync<GXGateway>(arg);

            ////////////////////////////////////////////////////
            //Get user groups that belongs for this gateway group.
            arg = GXSelectArgs.Select<GXUserGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXUserGroupGatewayGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
            arg.Where.And<GXUserGroupGatewayGroup>(q => q.Removed == null && q.GatewayGroupId == id);
            group.UserGroups = await _host.Connection.SelectAsync<GXUserGroup>(arg);
            return group;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal User,
            IEnumerable<GXGatewayGroup> GatewayGroups,
            Expression<Func<GXGatewayGroup, object?>>? columns)
        {
            string userId = ServerHelpers.GetUserId(User);
            DateTime now = DateTime.Now;
            List<Guid> list = new List<Guid>();
            Dictionary<GXGatewayGroup, List<string>> updates = new Dictionary<GXGatewayGroup, List<string>>();
            List<GXUserGroup>? defaultGroups = null;
            var newGroups = GatewayGroups.Where(w => w.Id == Guid.Empty).ToList();
            var updatedGroups = GatewayGroups.Where(w => w.Id != Guid.Empty).ToList();
            //Get notified users.
            if (newGroups.Any())
            {
                var first = newGroups.First();
                var users = await GetUsersAsync(User, first.Id);
                foreach (var it in newGroups)
                {
                    updates[it] = users;
                }
            }
            foreach (var it in updatedGroups)
            {
                updates[it] = await GetUsersAsync(User, it.Id);
            }

            using IDbTransaction transaction = _host.Connection.BeginTransaction();
            try
            {
                foreach (GXGatewayGroup it in GatewayGroups)
                {
                    if (string.IsNullOrEmpty(it.Name))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    if (it.UserGroups == null || !it.UserGroups.Any())
                    {
                        if (defaultGroups == null)
                        {
                            //Get default user groups.
                            defaultGroups = await _userGroupRepository.GetDefaultUserGroups(User, ServerHelpers.GetUserId(User));
                        }
                        it.UserGroups = defaultGroups;
                        if (it.UserGroups == null || !it.UserGroups.Any())
                        {
                            throw new ArgumentException(Properties.Resources.TargetMustBelongToOneGroup);
                        }
                    }
                }
                if (newGroups.Any())
                {
                    foreach (var it in newGroups)
                    {
                        it.CreationTime = now;
                    }
                    GXInsertArgs args = GXInsertArgs.InsertRange(newGroups);
                    args.Exclude<GXGatewayGroup>(e => new
                    {
                        e.Updated,
                        e.Removed,
                        //User groups must hanlde separetly because users are identified with name and not Guid.
                        e.UserGroups
                    });
                    await _host.Connection.InsertAsync(transaction, args);
                    foreach (var it in newGroups)
                    {
                        if (it.UserGroups != null)
                        {
                            AddGatewayGroupToUserGroups(transaction, it.Id, it.UserGroups);
                        }
                    }
                    foreach (var it in newGroups)
                    {
                        list.Add(it.Id);
                    }
                }
                foreach (var it in updatedGroups)
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXGatewayGroup>(q => q.ConcurrencyStamp, where => where.Id == it.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    it.Updated = now;
                    it.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                    args.Exclude<GXGatewayGroup>(q => new { q.CreationTime, q.UserGroups, q.Gateways});
                    _host.Connection.Update(args);
                    //Map user group to Gateway group.
                    {
                        List<GXUserGroup> userGroups = await GetUserGroupsByGatewayGroupId(it.Id);
                        var comparer = new UniqueComparer<GXUserGroup, Guid>();
                        List<GXUserGroup> removedUserGroups = userGroups.Except(it.UserGroups, comparer).ToList();
                        List<GXUserGroup> addedUserGroups = it.UserGroups.Except(userGroups, comparer).ToList();
                        if (removedUserGroups.Any())
                        {
                            RemoveGatewayGroupFromUserGroups(transaction, it.Id, removedUserGroups);
                        }
                        if (addedUserGroups.Any())
                        {
                            AddGatewayGroupToUserGroups(transaction, it.Id, addedUserGroups);
                        }
                    }
                    //Map gateways to Gateway group.
                    if (it.Gateways != null)
                    {
                        List<GXGateway> gateways = GetGatewaysByGatewayGroupId(it.Id);
                        var comparer = new UniqueComparer<GXGateway, Guid>();
                        List<GXGateway> removedGateways = gateways.Except(it.Gateways, comparer).ToList();
                        List<GXGateway> addedGateways = it.Gateways.Except(gateways, comparer).ToList();
                        if (removedGateways.Any())
                        {
                            RemoveGatewaysFromGatewayGroup(transaction, it.Id, removedGateways);
                        }
                        if (addedGateways.Any())
                        {
                            AddGatewaysToGatewayGroup(transaction, it.Id, addedGateways);
                        }
                    }
                    updates[it] = await GetUsersAsync(User, it.Id);
                }
                _host.Connection.CommitTransaction(transaction);
            }
            catch (Exception)
            {
                _host.Connection.RollbackTransaction(transaction);
                throw;
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.GatewayGroupUpdate(it.Value, new GXGatewayGroup[] { it.Key });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map gateway group to user groups.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="gatewayGroupId">Gateway group ID.</param>
        /// <param name="groups">User groups where the gateway is added.</param>
        public void AddGatewayGroupToUserGroups(IDbTransaction transaction, Guid gatewayGroupId, IEnumerable<GXUserGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupGatewayGroup> list = new List<GXUserGroupGatewayGroup>();
            foreach (var it in groups)
            {
                list.Add(new GXUserGroupGatewayGroup()
                {
                    GatewayGroupId = gatewayGroupId,
                    UserGroupId = it.Id,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between gateway group and user groups.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="gatewayGroupId">Gateway group ID.</param>
        /// <param name="groups">User groups where the gateway is removed.</param>
        public void RemoveGatewayGroupFromUserGroups(IDbTransaction transaction, Guid gatewayGroupId, IEnumerable<GXUserGroup> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupGatewayGroup>();
            foreach (var it in groups)
            {
                args.Where.Or<GXUserGroupGatewayGroup>(w => w.UserGroupId == it.Id &&
                    w.GatewayGroupId == gatewayGroupId);
            }
            _host.Connection.Delete(transaction, args);
        }

        /// <summary>
        /// Map gateways to gateway group.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="gatewayGroupId">Gateway group ID.</param>
        /// <param name="gateways">Added Gateways.</param>
        public void AddGatewaysToGatewayGroup(IDbTransaction transaction, Guid gatewayGroupId, IEnumerable<GXGateway> gateways)
        {
            DateTime now = DateTime.Now;
            List<GXGatewayGroupGateway> list = new List<GXGatewayGroupGateway>();
            foreach (var it in gateways)
            {
                list.Add(new GXGatewayGroupGateway()
                {
                    GatewayGroupId = gatewayGroupId,
                    GatewayId = it.Id,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between gateways and gateway group.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="gatewayGroupId">Gateway group ID.</param>
        /// <param name="gateways">Removed gateways.</param>
        public void RemoveGatewaysFromGatewayGroup(IDbTransaction transaction, Guid gatewayGroupId, IEnumerable<GXGateway> gateways)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXGatewayGroupGateway>();
            foreach (var it in gateways)
            {
                args.Where.Or<GXGatewayGroupGateway>(w => w.GatewayId == it.Id &&
                    w.GatewayGroupId == gatewayGroupId);
            }
            _host.Connection.Delete(transaction, args);
        }        
    }
}