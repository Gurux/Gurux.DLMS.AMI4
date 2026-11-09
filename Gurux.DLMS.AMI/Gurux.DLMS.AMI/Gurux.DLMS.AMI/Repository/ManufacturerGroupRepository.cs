// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq.Expressions;
using System.Security.Claims;


namespace Gurux.DLMS.AMI.Server.Repository
{

    /// <inheritdoc />
    public class ManufacturerGroupRepository : IManufacturerGroupRepository
    {

        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly IUserRepository _userRepository;
        private readonly NotificationCache _notificationCache;
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ManufacturerGroupRepository(
            IAuthorizationService authorizationService,
            IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor,
            IUserGroupRepository userGroupRepository,
            IUserRepository userRepository,
            NotificationCache notificationCache)
        {
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            User = httpContextAccessor.HttpContext.User;
            _dbFactory = dbFactory;
            _userGroupRepository = userGroupRepository;
            _userRepository = userRepository;
            _notificationCache = notificationCache;
            _authorizationService = authorizationService;
        }

        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXManufacturerGroup), id, action, GXManufacturerGroupPolicies.View, connection, transaction, notifications: _notificationCache);

        /// <inheritdoc />
        public async Task<List<GXManufacturerGroup>> GetJoinedManufacturerGroups(Guid moduleId)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXManufacturerGroupPolicies.View);
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXManufacturerGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXManufacturerGroup, GXManufacturerGroupManufacturer>(a => a.Id, b => b.ManufacturerGroupId);
            arg.Joins.AddInnerJoin<GXManufacturerGroupManufacturer, GXManufacturer>(a => a.ManufacturerId, b => b.Id);
            arg.Where.And<GXManufacturer>(where => where.Removed == null && where.Id == moduleId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            return (await connection.SelectAsync<GXManufacturerGroup>(arg));
        }

        private static async Task<List<GXUserGroup>> GetJoinedUserGroups(GXDbConnection connection, Guid manufacturerGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupManufacturerGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupManufacturerGroup, GXManufacturerGroup>(a => a.ManufacturerGroupId, b => b.Id);
            arg.Where.And<GXManufacturerGroup>(where => where.Removed == null && where.Id == manufacturerGroupId);
            return (await connection.SelectAsync<GXUserGroup>(arg));
        }

        private async Task<List<GXManufacturer>> GetJoinedManufacturers(GXDbConnection connection, Guid manufacturerGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXManufacturer>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXManufacturer, GXManufacturerGroupManufacturer>(a => a.Id, b => b.ManufacturerId);
            arg.Joins.AddInnerJoin<GXManufacturerGroupManufacturer, GXManufacturerGroup>(a => a.ManufacturerGroupId, b => b.Id);

            arg.Joins.AddInnerJoin<GXManufacturerGroup, GXUserGroupManufacturerGroup>(j => j.Id, j => j.ManufacturerGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupManufacturerGroup, GXUserGroup>(x => x.UserGroupId, y => y.Id);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(x => x.Id, y => y.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(x => x.UserId!, y => y.Id!);
            var id = ServerHelpers.GetUserId(User);
            arg.Where.And<GXUser>(q => q.Removed == null && q.Id == id);
            arg.Where.And<GXManufacturerGroup>(where => where.Removed == null && where.Id == manufacturerGroupId);
            return (await connection.SelectAsync<GXManufacturer>(arg));
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? groupId, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXManufacturerGroupPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByManufacturerGroup(s => s.Id!,
                ServerHelpers.GetUserId(User), groupId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (groupId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXManufacturerGroup), groupId, token: cancellationToken);
                args.Where.And<GXUser>(u => members.Contains(u.Id!));
            }
            List<GXUser> users = await connection.SelectAsync<GXUser>(args);
            List<string> ret = [.. users.Select(s => s.Id!)];
            if (User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? agentIds, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXManufacturerGroupPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByManufacturerGroups(s => s.Id!,
                ServerHelpers.GetUserId(User), agentIds);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            List<GXUser> users = await connection.SelectAsync<GXUser>(args);
            List<string> ret = [.. users.Select(s => s.Id!)];
            if (User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(IEnumerable<Guid> groups,
            bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            groups = groups.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXManufacturerGroup, Guid>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, groups,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXManufacturerGroupPolicies.Delete);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            IEnumerable<GXManufacturerGroup> exists;
            if (User.IsInRole(GXRoles.Admin))
            {
                GXSelectArgs arg = GXSelectArgs.Select<GXManufacturerGroup>(s => new { s.Id, s.Name }, w => groups.Contains(w.Id));
                exists = connection.Select<GXManufacturerGroup>(arg);
            }
            else
            {
                //Check that the user has access rights to the removed agent groups.
                string userId = ServerHelpers.GetUserId(User);
                var arg = GXQuery.GetManufacturerGroupsByUser(s => new { s.Id, s.Name }, userId, null,
                    DateTimeOffset.MinValue, null, groups);
                exists = connection.Select<GXManufacturerGroup>(arg);
            }
            if (!exists.Any())
            {
                //If user try to remove item that is not in the list, throw exception.
                //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                throw new GXAmiForbiddenException(string.Join(", ", groups));
            }
            DateTime now = DateTime.Now;
            Dictionary<GXManufacturerGroup, List<string>> updates = [];
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (GXManufacturerGroup it in exists)
                {
                    if (!groups.Contains(it.Id))
                    {
                        //If user try to remove item that is not in the list, throw exception.
                        //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                        throw new GXAmiForbiddenException(it.Id.ToString());
                    }
                    it.Removed = now;
                    List<string> users = await GetNotifiedUsers(it.Id, NotificationAction.Remove, connection, transaction);
                    if (!delete)
                    {
                        connection.Update(transaction, GXUpdateArgs.Update(it, q => q.Removed!));
                    }
                    updates[it] = users;
                }
                if (delete)
                {
                    await connection.DeleteAsync(transaction, GXDeleteArgs.DeleteRange(exists), cancellationToken);
                }
                await transaction.CommitAsync(cancellationToken);
            }
            catch (GXDatabaseException ex)
            {
                if (ex.ErrorType == DatabaseErrorType.Deadlock)
                {
                    //Max time is 5 seconds to avoid infinite retry when there is some other issue that cause deadlock and not just temporary deadlock.
                    CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(5));
                    try
                    {
                        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                        // If deadlock, wait max 1 second and retry.
                        await Task.Delay(1000, linkedCts.Token);
                        await DeleteAsync(groups, delete, notify, linkedCts.Token);
                    }
                    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                    {
                        throw ex;
                    }
                }
                else
                {
                    throw;
                }
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
            foreach (var it in updates)
            {
                var users = it.Value;
                if (users == null)
                {
                    break;
                }
                GXManufacturerGroup tmp = new() { Id = it.Key.Id, Name = it.Key.Name };
                _notificationCache.Add(users, new GXTarget(GXManufacturerGroupPolicies.Delete, tmp.Id));
            }
        }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXManufacturerGroup>> ListAsync(
            ListManufacturerGroups? request,
            ListManufacturerGroupsResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXManufacturerGroupPolicies.View);
            GXSelectArgs arg;
            if (request != null && request.AllUsers &&
                User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the groups.
                arg = GXSelectArgs.SelectAll<GXManufacturerGroup>();
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetManufacturerGroupsByUser(s => "*", userId, null,
                    request?.Filter?.Removed, request?.Exclude, request?.Included);
            }
            if (request != null)
            {
                //If manufacturer groups are filtered by user.
                if (request.Filter?.UserGroups != null)
                {
                    var ug = request.Filter.UserGroups.FirstOrDefault();
                    if (ug?.Users != null && ug.Users.Count != 0)
                    {
                        var user2 = ug.Users.FirstOrDefault();
                        if (user2 != null)
                        {
                            arg.Joins.AddLeftJoin<GXUserGroupManufacturerGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                            arg.Joins.AddLeftJoin<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId);
                            arg.Joins.AddLeftJoin<GXUserGroupUser, GXUser>(j => j.UserId!, j => j.Id!);
                            arg.Where.FilterBy(user2);
                        }
                    }
                    request.Filter.UserGroups = null;
                }
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude?.Any() == true)
                {
                    arg.Where.And<GXManufacturerGroup>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included?.Any() == true)
                {
                    arg.Where.And<GXManufacturerGroup>(w => request.Included.Contains(w.Id));
                }
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXManufacturerGroup>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXManufacturerGroup>(q => q.CreationTime!);
                arg.Descending = true;
            }
            arg.Distinct = true;
            GXDateTimeFilters.Apply<GXManufacturerGroup>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXManufacturerGroup>(q => GXSql.Count(1));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = connection.SingleOrDefault<long>(total);
                    if (response.Count == 0)
                    {
                        //No need to retrieve data when none is available.
                        return [];
                    }
                }
                arg.Index = request.Index;
                arg.Count = request.Count;
            }
            GXManufacturerGroup[] groups = [.. (await connection.SelectAsync<GXManufacturerGroup>(arg))];
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
        public async Task<GXManufacturerGroup> ReadAsync(Guid id, Expression<Func<GXManufacturerGroup, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXManufacturerGroupPolicies.View);
                GXSelectArgs arg;
                if (User.IsInRole(GXRoles.Admin))
                {
                    //Admin can see all the groups.
                    arg = GXSelectArgs.SelectAll<GXManufacturerGroup>(w => w.Id == id);
                }
                else
                {
                    string userId = ServerHelpers.GetUserId(User);
                    arg = GXQuery.GetManufacturerGroupsByUser(s => "*", userId, id);
                }
                arg.Columns.Add<GXManufacturerGroup>();
                arg.Columns.Exclude<GXManufacturerGroup>(e => e.Manufacturers!);
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                var group = (await connection.SingleOrDefaultAsync<GXManufacturerGroup>(arg)) ?? throw new GXAmiForbiddenException(id.ToString());
                ////////////////////////////////////////////////////
                //Get manufacturers that belong for this manufacturer group.
                arg = GXSelectArgs.Select<GXManufacturer>(s => new { s.Id, s.Name }, w => w.Removed == null);
                arg.Distinct = true;
                //It might be that there are no manufacturers in the group. For that reason left join is used.
                arg.Joins.AddLeftJoin<GXManufacturerGroupManufacturer, GXManufacturer>(j => j.ManufacturerId, j => j.Id);
                arg.Where.And<GXManufacturerGroupManufacturer>(q => q.Removed == null && q.ManufacturerGroupId == id);
                group.Manufacturers = await connection.SelectAsync<GXManufacturer>(arg);

                ////////////////////////////////////////////////////
                //Get user groups that belong for this manufacturer group.
                arg = GXSelectArgs.Select<GXUserGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
                arg.Distinct = true;
                //It might be that there are no manufacturers in the group. For that reason left join is used.
                arg.Joins.AddInnerJoin<GXUserGroupManufacturerGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                arg.Where.And<GXUserGroupManufacturerGroup>(q => q.Removed == null && q.ManufacturerGroupId == id);
                group.UserGroups = await connection.SelectAsync<GXUserGroup>(arg);
                //Get creator with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName }, q => q.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXManufacturerGroup, GXUser>(s => s.Creator!, o => o.Id!);
                arg.Where.And<GXManufacturerGroup>(w => w.Id == id);
                group.Creator = await connection.SingleOrDefaultAsync<GXUser>(arg);
                return group;

            }
            catch (GXDatabaseException ex)
            {
                if (ex.ErrorType == DatabaseErrorType.Deadlock)
                {
                    //Max time is 5 seconds to avoid infinite retry when there is some other issue that cause deadlock and not just temporary deadlock.
                    CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(5));
                    try
                    {
                        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                        // If deadlock, wait max 1 second and retry.
                        await Task.Delay(1000, linkedCts.Token);
                        return await ReadAsync(id, columns, linkedCts.Token);
                    }
                    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                    {
                        throw ex;
                    }
                }
                else
                {
                    throw;
                }
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXManufacturerGroup> ManufacturerGroups,
            Expression<Func<GXManufacturerGroup, object>>? columns, CancellationToken cancellationToken = default)
        {
            ManufacturerGroups = ManufacturerGroups.ToArray();
            return await GXHistoryMutation.RunAsync<GXManufacturerGroup, IEnumerable<Guid>>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, ManufacturerGroups,
        async () =>
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXManufacturerGroupPolicies.Add, GXManufacturerGroupPolicies.Edit);
                DateTime now = DateTime.Now;
                GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                foreach (GXManufacturerGroup it in ManufacturerGroups)
                {
                    if (string.IsNullOrEmpty(it.Name))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    if (it.UserGroups?.Count is not > 0)
                    {
                        //Get default user groups.
                        it.UserGroups = await _userGroupRepository.GetDefaultUserGroupsAsync(
                            ServerHelpers.GetUserId(User));
                        if (it.UserGroups?.Count is not > 0)
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
                        args.Exclude<GXManufacturerGroup>(e => new { e.UserGroups });
                        connection.Insert(args);
                        AddManufacturerGroupToUserGroups(connection, it.Id, [.. it.UserGroups.Select(s => s.Id)]);
                        _notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Add, connection),
                            new GXTarget(GXManufacturerGroupPolicies.Add, it.Id, it.Name));
                    }
                    else
                    {
                        GXSelectArgs m = GXSelectArgs.Select<GXManufacturerGroup>(q => q.ConcurrencyStamp!, where => where.Id == it.Id);
                        string? updated = connection.SingleOrDefault<string>(m);
                        if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }
                        it.Updated = now;
                        it.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                        args.Exclude<GXManufacturerGroup>(q => new
                        {
                            q.UserGroups,
                            q.CreationTime,
                            q.Manufacturers
                        });
                        if (!User.IsInRole(GXRoles.Admin) ||
                            it.Creator == null ||
                            string.IsNullOrEmpty(it.Creator.Id))
                        {
                            //Only admin can update the creator.
                            args.Exclude<GXManufacturerGroup>(q => q.Creator!);
                        }
                        connection.Update(args);
                        //Map user group to Manufacturer group.
                        List<GXUserGroup> list2 = await GetJoinedUserGroups(connection, it.Id);
                        List<Guid> groups = [.. list2.Select(s => s.Id)];
                        Guid[] tmp = [.. it.UserGroups.Select(s => s.Id)];
                        Guid[] removed = [.. groups.Except(tmp)];
                        Guid[] added = [.. tmp.Except(groups)];
                        if (added.Length != 0)
                        {
                            AddManufacturerGroupToUserGroups(connection, it.Id, added);
                        }
                        if (removed.Length != 0)
                        {
                            RemoveManufacturerGroupFromUserGroups(connection, it.Id, removed);
                        }
                        //Map manufacturers to Manufacturer group.
                        if (it.Manufacturers != null && it.Manufacturers.Count != 0)
                        {
                            List<GXManufacturer> list3 = await GetJoinedManufacturers(connection, it.Id);
                            List<Guid> groups2 = [.. list3.Select(s => s.Id)];
                            tmp = [.. it.Manufacturers.Select(s => s.Id)];
                            removed = [.. groups2.Except(tmp)];
                            added = [.. tmp.Except(groups2)];
                            if (added.Length != 0)
                            {
                                AddManufacturersToManufacturerGroup(connection, it.Id, added);
                            }
                            if (removed.Length != 0)
                            {
                                RemoveManufacturersFromManufacturerGroup(connection, it.Id, removed);
                            }
                        }
                        _notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Edit, connection),
                            new GXTarget(GXManufacturerGroupPolicies.Edit, it.Id, it.Name));
                    }
                }
                return [.. ManufacturerGroups.Select(s => s.Id)];

            }
            catch (GXDatabaseException ex)
            {
                if (ex.ErrorType == DatabaseErrorType.Deadlock)
                {
                    //Max time is 5 seconds to avoid infinite retry when there is some other issue that cause deadlock and not just temporary deadlock.
                    CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(5));
                    try
                    {
                        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                        // If deadlock, wait max 1 second and retry.
                        await Task.Delay(1000, linkedCts.Token);
                        return await UpsertAsync(ManufacturerGroups, columns, linkedCts.Token);
                    }
                    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                    {
                        throw ex;
                    }
                }
                else
                {
                    throw;
                }
            }
        }, cancellationToken);
        }

        /// <summary>
        /// Map manufacturer group to user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="manufacturerGroupId">Manufacturer group ID.</param>
        /// <param name="groups">Group IDs of the manufacturer groups where the manufacturer is added.</param>
        private static void AddManufacturerGroupToUserGroups(GXDbConnection connection,
            Guid manufacturerGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupManufacturerGroup> list = [];
            foreach (var ug in groups)
            {
                list.Add(new GXUserGroupManufacturerGroup()
                {
                    ManufacturerGroupId = manufacturerGroupId,
                    UserGroupId = ug,
                    CreationTime = now
                });
            }
            connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between manufacturer group and user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="manufacturerGroupId">Manufacturer group ID.</param>
        /// <param name="groups">Group IDs of the manufacturer groups where the manufacturer is removed.</param>
        private static void RemoveManufacturerGroupFromUserGroups(GXDbConnection connection,
            Guid manufacturerGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupManufacturerGroup>();
            foreach (var ug in groups)
            {
                args.Where.Or<GXUserGroupManufacturerGroup>(w => w.UserGroupId == ug &&
                    w.ManufacturerGroupId == manufacturerGroupId);
            }
            connection.Delete(args);
        }

        /// <summary>
        /// Map manufacturers to manufacturer group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="manufacturerGroupId">Manufacturer group ID.</param>
        /// <param name="groups">Group IDs of the manufacturer groups where the manufacturer is added.</param>
        private static void AddManufacturersToManufacturerGroup(GXDbConnection connection,
            Guid manufacturerGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXManufacturerGroupManufacturer> list = [];
            foreach (var ug in groups)
            {
                list.Add(new GXManufacturerGroupManufacturer()
                {
                    ManufacturerGroupId = manufacturerGroupId,
                    ManufacturerId = ug,
                    CreationTime = now
                });
            }
            connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between manufacturers and manufacturer group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="manufacturerGroupId">Manufacturer group ID.</param>
        /// <param name="groups">Group IDs of the manufacturer groups where the manufacturer is removed.</param>
        private static void RemoveManufacturersFromManufacturerGroup(GXDbConnection connection,
            Guid manufacturerGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXManufacturerGroupManufacturer>();
            foreach (var it in groups)
            {
                args.Where.Or<GXManufacturerGroupManufacturer>(w => w.ManufacturerId == it &&
                    w.ManufacturerGroupId == manufacturerGroupId);
            }
            connection.Delete(args);
        }
    }
}
