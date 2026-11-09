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
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
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
using System.Text.RegularExpressions;

namespace Gurux.DLMS.AMI.Server.Repository
{

    /// <inheritdoc />
    public class DeviceTemplateGroupRepository : IDeviceTemplateGroupRepository
    {

        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IUserRepository _userRepository;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly NotificationCache _notificationCache;
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DeviceTemplateGroupRepository(
            IAuthorizationService authorizationService,
            IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor,
            IUserRepository userRepository,
            IUserGroupRepository userGroupRepository,
            NotificationCache notificationCache)
        {
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            User = httpContextAccessor.HttpContext.User;
            _dbFactory = dbFactory;
            _userRepository = userRepository;
            _userGroupRepository = userGroupRepository;
            _notificationCache = notificationCache;
            _authorizationService = authorizationService;
        }

        private static async Task<List<GXUserGroup>> GetUserGroupsByDeviceTemplateGroupId(GXDbConnection connection,
          IDbTransaction transaction, Guid groupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceTemplateGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupDeviceTemplateGroup, GXDeviceTemplateGroup>(a => a.DeviceTemplateGroupId, b => b.Id);
            arg.Where.And<GXDeviceTemplateGroup>(where => where.Removed == null && where.Id == groupId);
            return (await connection.SelectAsync<GXUserGroup>(transaction, arg));
        }

        /// <summary>
        /// Returns user groups where device template belongs.
        /// </summary>
        /// <param name="groupId">Device template group ID.</param>
        /// <returns>User groups where device template belongs.</returns>
        public async Task<List<GXUserGroup>> GetJoinedUserGroups(Guid groupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceTemplateGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupDeviceTemplateGroup, GXDeviceTemplateGroup>(a => a.DeviceTemplateGroupId, b => b.Id);
            arg.Where.And<GXDeviceTemplateGroup>(where => where.Removed == null && where.Id == groupId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            return (await connection.SelectAsync<GXUserGroup>(arg));
        }

        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXDeviceTemplateGroup), id, action, GXDeviceTemplateGroupPolicies.View, connection, transaction, notifications: _notificationCache);

        /// <inheritdoc />
        public async Task<List<GXDeviceTemplateGroup>> GetJoinedDeviceTemplateGroups(Guid deviceTemplateId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXDeviceTemplateGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXDeviceTemplateGroup, GXDeviceTemplateGroupDeviceTemplate>(a => a.Id, b => b.DeviceTemplateGroupId);
            arg.Joins.AddInnerJoin<GXDeviceTemplateGroupDeviceTemplate, GXDeviceTemplate>(a => a.DeviceTemplateId, b => b.Id);
            arg.Where.And<GXDeviceTemplate>(where => where.Removed == null && where.Id == deviceTemplateId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            return (await connection.SelectAsync<GXDeviceTemplateGroup>(arg));
        }

        /// <summary>
        /// Returns user groups where device template belongs.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="groupId">Device template group ID.</param>
        /// <returns>User groups where device template belongs.</returns>
        /// <param name="transaction">The database transaction.</param>
        private static async Task<List<GXDeviceTemplate>> GetJoinedDeviceTemplate(GXDbConnection connection,
            IDbTransaction transaction, Guid groupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXDeviceTemplate>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXDeviceTemplateGroup, GXDeviceTemplateGroupDeviceTemplate>(a => a.Id, b => b.DeviceTemplateGroupId);
            arg.Joins.AddInnerJoin<GXDeviceTemplateGroupDeviceTemplate, GXDeviceTemplate>(a => a.DeviceTemplateId, b => b.Id);
            arg.Where.And<GXDeviceTemplateGroup>(where => where.Removed == null && where.Id == groupId);
            return await connection.SelectAsync<GXDeviceTemplate>(transaction, arg);
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? groupId, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXDeviceTemplateGroupPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByDeviceTemplateGroup(s => s.Id!,
                ServerHelpers.GetUserId(User), groupId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (groupId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXDeviceTemplateGroup), groupId, token: cancellationToken);
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
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? groupIds, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXDeviceTemplateGroupPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByDeviceTemplateGroups(s => s.Id!,
                ServerHelpers.GetUserId(User), groupIds);
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
        public async Task DeleteAsync(
            IEnumerable<Guid> groups,
            bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            groups = groups.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXDeviceTemplateGroup, Guid>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, groups,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXDeviceTemplateGroupPolicies.Delete);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            IEnumerable<GXDeviceTemplateGroup> exists;
            if (User.IsInRole(GXRoles.Admin))
            {
                GXSelectArgs arg = GXSelectArgs.Select<GXDeviceTemplateGroup>(s => new { s.Id, s.Name }, w => groups.Contains(w.Id));
                exists = connection.Select<GXDeviceTemplateGroup>(arg);
            }
            else
            {
                //Check that the user has access rights to the removed device template groups.
                string userId = ServerHelpers.GetUserId(User);
                var arg = GXQuery.GetDeviceTemplateGroupsByUser(s => new { s.Id, s.Name }, userId, null,
                    DateTimeOffset.MinValue, null, groups);
                exists = connection.Select<GXDeviceTemplateGroup>(arg);
                if (!exists.Any())
                {
                    //If user try to remove item that is not in the list, throw exception.
                    //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                    throw new GXAmiForbiddenException(string.Join(", ", groups));
                }
            }
            DateTime now = DateTime.Now;
            Dictionary<GXDeviceTemplateGroup, List<string>> updates = [];
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (var it in exists)
                {
                    if (!groups.Contains(it.Id))
                    {
                        //If user try to remove item that is not in the list, throw exception.
                        //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                        throw new GXAmiForbiddenException(it.Id.ToString());
                    }
                    List<string> users = await GetNotifiedUsers(it.Id, NotificationAction.Remove, connection, transaction);
                    if (!delete)
                    {
                        it.Removed = now;
                        connection.Update(transaction, GXUpdateArgs.Update(it, q => q.Removed!));
                    }
                    updates[it] = users;
                }
                if (delete)
                {
                    var arg = GXDeleteArgs.Delete<GXDeviceTemplateGroupDeviceTemplate>(w => groups.Contains(w.DeviceTemplateGroupId));
                    connection.Delete(transaction, arg);
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
                _notificationCache.Add(it.Value,
                    new GXTarget(GXDeviceTemplateGroupPolicies.Delete, it.Key.Id, it.Key.Name));
            }
        }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXDeviceTemplateGroup>> ListAsync(
            ListDeviceTemplateGroups? request,
            ListDeviceTemplateGroupsResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXDeviceTemplateGroupPolicies.View);
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the device template groups.
                arg = GXSelectArgs.SelectAll<GXDeviceTemplateGroup>();
                if (request.Exclude?.Any() == true)
                {
                    arg.Where.And<GXDeviceTemplateGroup>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included?.Any() == true)
                {
                    arg.Where.And<GXDeviceTemplateGroup>(w => request.Included.Contains(w.Id));
                }
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetDeviceTemplateGroupsByUser(s => "*", userId, null,
                    request?.Filter?.Removed, request?.Exclude, request?.Included);
            }
            if (request != null)
            {
                //If device template groups are filtered by user.
                if (request.Filter?.UserGroups != null)
                {
                    var ug = request.Filter.UserGroups.FirstOrDefault();
                    if (ug?.Users != null && ug.Users.Count != 0)
                    {
                        var user = ug.Users.FirstOrDefault();
                        if (user != null)
                        {
                            arg.Joins.AddLeftJoin<GXUserGroupDeviceTemplateGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                            arg.Joins.AddLeftJoin<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId);
                            arg.Joins.AddLeftJoin<GXUserGroupUser, GXUser>(j => j.UserId!, j => j.Id!);
                            arg.Where.FilterBy(user);
                        }
                    }
                    request.Filter.UserGroups = null;
                }
                arg.Where.FilterBy(request.Filter);

            }
            GXDateTimeFilters.Apply<GXDeviceTemplateGroup>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXDeviceTemplateGroup>(q => GXSql.Count(1));
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
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXDeviceTemplateGroup>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXDeviceTemplateGroup>(q => q.CreationTime!);
            }
            GXDeviceTemplateGroup[] groups = [.. (await connection.SelectAsync<GXDeviceTemplateGroup>(arg))];
            response?.DeviceTemplateGroups = groups;
            return groups;
        }

        /// <inheritdoc />
        public async Task<GXDeviceTemplateGroup> ReadAsync(Guid id, Expression<Func<GXDeviceTemplateGroup, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXDeviceTemplateGroupPolicies.View);
                GXSelectArgs arg;
                if (User.IsInRole(GXRoles.Admin))
                {
                    //Admin can see all the groups.
                    arg = GXSelectArgs.SelectAll<GXDeviceTemplateGroup>(w => w.Id == id);
                }
                else
                {
                    string userId = ServerHelpers.GetUserId(User);
                    arg = GXQuery.GetDeviceTemplateGroupsByUser(s => "*", userId, id);
                }
                arg.Columns.Exclude<GXDeviceTemplateGroup>(e => e.DeviceTemplates!);
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                var group = (await connection.SingleOrDefaultAsync<GXDeviceTemplateGroup>(arg)) ?? throw new GXAmiForbiddenException(id.ToString());
                ////////////////////////////////////////////////////
                //Get device templates that belongs for this device template group.
                arg = GXSelectArgs.Select<GXDeviceTemplate>(s => new { s.Id, s.Name }, w => w.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXDeviceTemplateGroupDeviceTemplate, GXDeviceTemplate>(j => j.DeviceTemplateId, j => j.Id);
                arg.Where.And<GXDeviceTemplateGroupDeviceTemplate>(q => q.Removed == null && q.DeviceTemplateGroupId == id);
                group.DeviceTemplates = await connection.SelectAsync<GXDeviceTemplate>(arg);

                ////////////////////////////////////////////////////
                //Get user groups that belongs for this device template group.
                arg = GXSelectArgs.Select<GXUserGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXUserGroupDeviceTemplateGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                arg.Where.And<GXUserGroupDeviceTemplateGroup>(q => q.Removed == null && q.DeviceTemplateGroupId == id);
                group.UserGroups = await connection.SelectAsync<GXUserGroup>(arg);
                //Get creator with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName }, q => q.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXDeviceTemplateGroup, GXUser>(s => s.Creator!, o => o.Id!);
                arg.Where.And<GXDeviceTemplateGroup>(w => w.Id == id);
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

        /// <summary>Inserts a new group and its memberships in the caller's transaction without notifications or history.</summary>
        internal static async Task UpsertAsync(GXDbConnection connection, System.Data.IDbTransaction transaction,
            GXDeviceTemplateGroup group, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(transaction);
            cancellationToken.ThrowIfCancellationRequested();
            if (group.Id != Guid.Empty)
                {
                throw new ArgumentException("This overload only inserts new groups.", nameof(group));
            }
            if (string.IsNullOrEmpty(group.Name))
                {
                throw new ArgumentException(Gurux.DLMS.AMI.Properties.Resources.InvalidName);
            }
            if (string.IsNullOrEmpty(group.Creator?.Id))
                {
                throw new ArgumentException("A group creator is required.", nameof(group));
            }
            group.CreationTime ??= DateTime.Now;
            if (group.UserGroups?.Count is not > 0 || group.UserGroups.Any(g => g.Id == Guid.Empty))
                {
                throw new ArgumentException(Gurux.DLMS.AMI.Properties.Resources.TargetMustBelongToOneGroup);
            }
            var args = GXInsertArgs.Insert(group);
            args.Exclude<GXDeviceTemplateGroup>(group => new { group.Updated, group.Removed, group.UserGroups });
            await connection.InsertAsync(transaction, args, cancellationToken);
            var memberships = group.UserGroups.Select(userGroup => new GXUserGroupDeviceTemplateGroup
            {
                UserGroupId = userGroup.Id,
                DeviceTemplateGroupId = group.Id,
                CreationTime = DateTime.Now
            }).ToArray();
            await connection.InsertAsync(transaction, GXInsertArgs.InsertRange(memberships), cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXDeviceTemplateGroup> DeviceTemplateGroups,
            Expression<Func<GXDeviceTemplateGroup, object>>? columns, CancellationToken cancellationToken = default)
        {
            DeviceTemplateGroups = DeviceTemplateGroups.ToArray();
            return await GXHistoryMutation.RunAsync<GXDeviceTemplateGroup, IEnumerable<Guid>>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, DeviceTemplateGroups,
        async () =>
        {
            DateTime now = DateTime.Now;
            GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
            Dictionary<GXDeviceTemplateGroup, List<string>> updates = [];
            List<GXUserGroup>? defaultGroups = null;
            var newGroups = DeviceTemplateGroups.Where(w => w.Id == Guid.Empty).ToList();
            var updatedGroups = DeviceTemplateGroups.Where(w => w.Id != Guid.Empty).ToList();
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            foreach (GXDeviceTemplateGroup it in DeviceTemplateGroups)
            {
                if (string.IsNullOrEmpty(it.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                //Check the device templates are already inserted.
                if (it.DeviceTemplates != null && it.DeviceTemplates.Where(w => w.Id == Guid.Empty).Any())
                {
                    throw new ArgumentException(Properties.Resources.JoinedGroupsAreNotInserted);
                }

                if (it.UserGroups?.Count is not > 0)
                {
                    //Get default user groups.
                    defaultGroups ??= await _userGroupRepository.GetDefaultUserGroupsAsync(
                            creator.Id);
                    it.UserGroups = defaultGroups;
                    if (it.UserGroups?.Count is not > 0)
                    {
                        throw new ArgumentException(Properties.Resources.TargetMustBelongToOneGroup);
                    }
                }
            }
            try
            {
                if (newGroups.Count != 0)
                {
                    await _authorizationService.EnsureAnyPolicyAsync(User, GXDeviceTemplateGroupPolicies.Add);
                    foreach (var it in newGroups)
                    {
                        it.Creator = creator;
                        it.CreationTime = now;
                        await UpsertAsync(connection, transaction, it, cancellationToken);
                        _notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Add, connection, transaction), new GXTarget(GXDeviceTemplatePolicies.Add, it.Id, it.Name));
                    }
                }
                if (updatedGroups.Count != 0)
                {
                    await _authorizationService.EnsureAnyPolicyAsync(User, GXDeviceTemplateGroupPolicies.Edit);
                    foreach (var it in updatedGroups)
                    {
                        GXSelectArgs m = GXSelectArgs.Select<GXDeviceTemplateGroup>(q => q.ConcurrencyStamp!, where => where.Id == it.Id);
                        string? updated = connection.SingleOrDefault<string>(transaction, m);
                        if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }
                        it.Updated = now;
                        it.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                        args.Exclude<GXDeviceTemplateGroup>(q => new
                        {
                            q.CreationTime,
                            q.UserGroups,
                            q.DeviceTemplates
                        });
                        if (!User.IsInRole(GXRoles.Admin) ||
                            it.Creator == null ||
                            string.IsNullOrEmpty(it.Creator.Id))
                        {
                            //Only admin can update the creator.
                            args.Exclude<GXDeviceTemplateGroup>(q => q.Creator!);
                        }
                        connection.Update(transaction, args);

                        //Map user group to device template group.
                        if (it.UserGroups != null && it.UserGroups.Count != 0)
                        {
                            List<GXUserGroup> list2 = await GetUserGroupsByDeviceTemplateGroupId(connection, transaction, it.Id);
                            List<Guid> groups = [.. list2.Select(s => s.Id)];
                            Guid[] tmp = [.. it.UserGroups.Select(s => s.Id)];
                            Guid[] removed = [.. groups.Except(tmp)];
                            Guid[] added = [.. tmp.Except(groups)];
                            if (added.Length != 0)
                            {
                                AddDeviceTemplateGroupToUserGroups(connection, transaction, it.Id, added);
                            }
                            if (removed.Length != 0)
                            {
                                RemoveDeviceTemplateGroupFromUserGroups(connection, transaction, it.Id, removed);
                            }
                        }
                        //Map device templates to device template group.
                        if (it.DeviceTemplates != null)
                        {
                            List<GXDeviceTemplate> list2 = await GetJoinedDeviceTemplate(connection, transaction, it.Id);
                            List<Guid> groups = [.. list2.Select(s => s.Id)];
                            Guid[] tmp = [.. it.DeviceTemplates.Select(s => s.Id)];
                            Guid[] removed = [.. groups.Except(tmp)];
                            Guid[] added = [.. tmp.Except(groups)];
                            if (added.Length != 0)
                            {
                                AddDeviceTemplateGroupsToDeviceTemplate(connection, transaction, it.Id, added);
                            }
                            if (removed.Length != 0)
                            {
                                RemoveDeviceTemplateGroupsFromDeviceTemplate(connection, transaction, it.Id, removed);
                            }
                        }
                        _notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Edit, connection, transaction),
                            new GXTarget(GXDeviceTemplatePolicies.Edit, it.Id, it.Name));
                    }
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
                        return await UpsertAsync(DeviceTemplateGroups, columns, linkedCts.Token);
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
            return [.. DeviceTemplateGroups.Select(s => s.Id)];
        }, cancellationToken);
        }

        /// <summary>
        /// Add DeviceTemplate group to user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="DeviceTemplateGroupId">DeviceTemplate group ID.</param>
        /// <param name="groups">Group IDs of the DeviceTemplate groups where the DeviceTemplate is added.</param>
        public void AddDeviceTemplateGroupToUserGroups(GXDbConnection connection,
            IDbTransaction transaction, Guid DeviceTemplateGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupDeviceTemplateGroup> list = [];
            foreach (var it in groups)
            {
                list.Add(new GXUserGroupDeviceTemplateGroup()
                {
                    DeviceTemplateGroupId = DeviceTemplateGroupId,
                    UserGroupId = it,
                    CreationTime = now
                });
            }
            connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove DeviceTemplate group to user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="deviceTemplateGroupId">DeviceTemplate group ID.</param>
        /// <param name="groups">Group IDs of the DeviceTemplate groups where the DeviceTemplate is added.</param>
        public void RemoveDeviceTemplateGroupFromUserGroups(GXDbConnection connection,
            IDbTransaction transaction, Guid deviceTemplateGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupDeviceTemplateGroup>();
            foreach (var it in groups)
            {
                args.Where.Or<GXUserGroupDeviceTemplateGroup>(w => w.UserGroupId == it &&
                    w.DeviceTemplateGroupId == deviceTemplateGroupId);
            }
            connection.Delete(transaction, args);
        }
        /// <summary>
        /// Add DeviceTemplate group to user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="deviceTemplateGroupId">DeviceTemplate group ID.</param>
        /// <param name="groups">Group IDs of the DeviceTemplate groups where the DeviceTemplate is added.</param>
        public void AddDeviceTemplateGroupsToDeviceTemplate(GXDbConnection connection,
            IDbTransaction transaction, Guid deviceTemplateGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXDeviceTemplateGroupDeviceTemplate> list = [];
            foreach (var it in groups)
            {
                list.Add(new GXDeviceTemplateGroupDeviceTemplate()
                {
                    DeviceTemplateGroupId = deviceTemplateGroupId,
                    DeviceTemplateId = it,
                    CreationTime = now
                });
            }
            connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove DeviceTemplate group to user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="deviceTemplateGroupId">DeviceTemplate group ID.</param>
        /// <param name="groups">Group IDs of the DeviceTemplate groups where the DeviceTemplate is added.</param>
        public void RemoveDeviceTemplateGroupsFromDeviceTemplate(GXDbConnection connection,
            IDbTransaction transaction, Guid deviceTemplateGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXDeviceTemplateGroupDeviceTemplate>();
            foreach (var it in groups)
            {
                args.Where.Or<GXDeviceTemplateGroupDeviceTemplate>(w => w.DeviceTemplateId == it &&
                    w.DeviceTemplateGroupId == deviceTemplateGroupId);
            }
            connection.Delete(transaction, args);
        }
    }
}
