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
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
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
    public class ScheduleGroupRepository : IScheduleGroupRepository
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
        public ScheduleGroupRepository(
            IDbConnectionFactory dbFactory,
            IAuthorizationService authorizationService,
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

        private static async Task<List<GXUserGroup>> GetJoinedUserGroups(GXDbConnection connection, IDbTransaction transaction, Guid scheduleGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupScheduleGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupScheduleGroup, GXScheduleGroup>(a => a.ScheduleGroupId, b => b.Id);
            arg.Where.And<GXScheduleGroup>(where => where.Removed == null && where.Id == scheduleGroupId);
            return await connection.SelectAsync<GXUserGroup>(transaction, arg);
        }

        private async Task<List<GXSchedule>> GetJoinedSchedules(GXDbConnection connection, IDbTransaction transaction, Guid scheduleGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXSchedule>(where => where.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXSchedule, GXScheduleGroupSchedule>(a => a.Id, b => b.ScheduleId);
            arg.Joins.AddInnerJoin<GXScheduleGroupSchedule, GXScheduleGroup>(a => a.ScheduleGroupId, b => b.Id);

            arg.Joins.AddInnerJoin<GXScheduleGroup, GXUserGroupScheduleGroup>(j => j.Id, j => j.ScheduleGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupScheduleGroup, GXUserGroup>(x => x.UserGroupId, y => y.Id);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(x => x.Id, y => y.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(x => x.UserId!, y => y.Id!);
            if (!User.IsInRole(GXRoles.Admin))
            {
                var id = ServerHelpers.GetUserId(User);
                arg.Where.And<GXUser>(q => q.Removed == null && q.Id == id);
            }
            arg.Where.And<GXScheduleGroup>(where => where.Removed == null && where.Id == scheduleGroupId);
            return await connection.SelectAsync<GXSchedule>(transaction, arg);
        }

        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXScheduleGroup), id, action, GXScheduleGroupPolicies.View, connection, transaction, notifications: _notificationCache);

        /// <inheritdoc />
        public async Task<List<GXScheduleGroup>> GetJoinedScheduleGroups(Guid scheduleId)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXScheduleGroupPolicies.View);
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXScheduleGroup>(where => where.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXScheduleGroup, GXScheduleGroupSchedule>(a => a.Id, b => b.ScheduleGroupId);
            arg.Joins.AddInnerJoin<GXScheduleGroupSchedule, GXSchedule>(a => a.ScheduleId, b => b.Id);

            arg.Joins.AddInnerJoin<GXScheduleGroup, GXUserGroupScheduleGroup>(j => j.Id, j => j.ScheduleGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupScheduleGroup, GXUserGroup>(x => x.UserGroupId, y => y.Id);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(x => x.Id, y => y.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(x => x.UserId!, y => y.Id!);
            var id = ServerHelpers.GetUserId(User);
            arg.Where.And<GXUser>(q => q.Removed == null && q.Id == id);
            arg.Where.And<GXSchedule>(where => where.Removed == null && where.Id == scheduleId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            return (await connection.SelectAsync<GXScheduleGroup>(arg));
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? groupId, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXScheduleGroupPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByScheduleGroup(s => s.Id!,
                ServerHelpers.GetUserId(User), groupId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (groupId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXScheduleGroup), groupId, token: cancellationToken);
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
            await _authorizationService.EnsureAnyPolicyAsync(User, GXScheduleGroupPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByScheduleGroups(s => s.Id!,
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
        public async Task DeleteAsync(IEnumerable<Guid> groups, bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            groups = groups.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXScheduleGroup, Guid>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, groups,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXScheduleGroupPolicies.Delete);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            IEnumerable<GXScheduleGroup> exists;
            if (User.IsInRole(GXRoles.Admin))
            {
                GXSelectArgs arg = GXSelectArgs.Select<GXScheduleGroup>(s => new { s.Id, s.Name }, w => groups.Contains(w.Id));
                exists = connection.Select<GXScheduleGroup>(arg);
            }
            else
            {
                //Check that the user has access rights to the removed schedule groups.
                string userId = ServerHelpers.GetUserId(User);
                var arg = GXQuery.GetScheduleGroupsByUser(s => new { s.Id, s.Name }, userId, null,
                    DateTimeOffset.MinValue, null, groups);
                exists = connection.Select<GXScheduleGroup>(arg);
            }
            if (!exists.Any())
            {
                //If user try to remove item that is not in the list, throw exception.
                //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                throw new GXAmiForbiddenException(string.Join(", ", groups));
            }
            DateTime now = DateTime.Now;
            Dictionary<GXScheduleGroup, List<string>> updates = [];
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
                    new GXTarget(GXScheduleGroupPolicies.Delete, it.Key.Id, it.Key.Name));
            }
        }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXScheduleGroup>> ListAsync(
            ListScheduleGroups? request,
            ListScheduleGroupsResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXScheduleGroupPolicies.View);
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the schedule groups.
                arg = GXSelectArgs.SelectAll<GXScheduleGroup>();
                arg.Joins.AddLeftJoin<GXScheduleGroup, GXUserGroupScheduleGroup>(j => j.Id, j => j.UserGroupId);
                if (request.Exclude?.Any() == true)
                {
                    arg.Where.And<GXScheduleGroup>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included?.Any() == true)
                {
                    arg.Where.And<GXScheduleGroup>(w => request.Included.Contains(w.Id));
                }
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetScheduleGroupsByUser(s => "*", userId, null,
                    request?.Filter?.Removed, request?.Exclude, request?.Included);
            }
            if (request != null)
            {
                //If schedule groups are filtered by user.
                if (request.Filter?.UserGroups != null)
                {
                    var ug = request.Filter.UserGroups.FirstOrDefault();
                    if (ug?.Users != null && ug.Users.Count != 0)
                    {
                        var user = ug.Users.FirstOrDefault();
                        if (user != null)
                        {
                            arg.Joins.AddLeftJoin<GXUserGroupScheduleGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                            arg.Joins.AddLeftJoin<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId);
                            arg.Joins.AddLeftJoin<GXUserGroupUser, GXUser>(j => j.UserId!, j => j.Id!);
                            arg.Where.FilterBy(user);
                        }
                    }
                    request.Filter.UserGroups = null;
                }
                arg.Where.FilterBy(request.Filter);
            }
            arg.Distinct = true;
            GXDateTimeFilters.Apply<GXScheduleGroup>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXScheduleGroup>(q => GXSql.Count(1));
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
                arg.OrderBy.Add<GXScheduleGroup>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXScheduleGroup>(q => q.CreationTime!);
            }
            GXScheduleGroup[] groups = [.. (await connection.SelectAsync<GXScheduleGroup>(arg))];
            if (response != null)
            {
                response.ScheduleGroups = groups;
                if (response.Count == 0)
                {
                    response.Count = groups.Length;
                }
            }
            return groups;
        }

        /// <inheritdoc />
        public async Task<GXScheduleGroup> ReadAsync(Guid id, Expression<Func<GXScheduleGroup, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXScheduleGroupPolicies.View);
                GXSelectArgs arg;
                if (User.IsInRole(GXRoles.Admin))
                {
                    //Admin can see all the groups.
                    arg = GXSelectArgs.SelectAll<GXScheduleGroup>(w => w.Id == id);
                }
                else
                {
                    string userId = ServerHelpers.GetUserId(User);
                    arg = GXQuery.GetScheduleGroupsByUser(s => "*", userId, id);
                }
                arg.Columns.Exclude<GXScheduleGroup>(e => e.Schedules!);
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                var group = (await connection.SingleOrDefaultAsync<GXScheduleGroup>(arg)) ?? throw new GXAmiForbiddenException(id.ToString());
                ////////////////////////////////////////////////////
                //Get schedules that belongs for this schedule group.
                arg = GXSelectArgs.Select<GXSchedule>(s => new { s.Id, s.Name }, w => w.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXScheduleGroupSchedule, GXSchedule>(j => j.ScheduleId, j => j.Id);
                arg.Where.And<GXScheduleGroupSchedule>(q => q.Removed == null && q.ScheduleGroupId == id);
                group.Schedules = await connection.SelectAsync<GXSchedule>(arg);

                ////////////////////////////////////////////////////
                //Get user groups that belongs for this schedule group.
                arg = GXSelectArgs.Select<GXUserGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXUserGroupScheduleGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                arg.Where.And<GXUserGroupScheduleGroup>(q => q.Removed == null && q.ScheduleGroupId == id);
                group.UserGroups = await connection.SelectAsync<GXUserGroup>(arg);

                //Get creator with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName });
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXScheduleGroup, GXUser>(s => s.Creator!, o => o.Id!);
                arg.Where.And<GXScheduleGroup>(w => w.Id == id);
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
            GXScheduleGroup group, CancellationToken cancellationToken = default)
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
            foreach (var child in group.Schedules ?? [])
            {
                if (child.Id == Guid.Empty)
                {
                    child.Creator = group.Creator;
                    child.CreationTime = group.CreationTime;
                }
            }
            var args = GXInsertArgs.Insert(group);
            args.Exclude<GXScheduleGroup>(group => new { group.UserGroups, group.Schedules, group.Updated, group.Removed });
            await connection.InsertAsync(transaction, args, cancellationToken);
            var memberships = group.UserGroups.Select(userGroup => new GXUserGroupScheduleGroup
            {
                UserGroupId = userGroup.Id,
                ScheduleGroupId = group.Id,
                CreationTime = DateTime.Now
            }).ToArray();
            await connection.InsertAsync(transaction, GXInsertArgs.InsertRange(memberships), cancellationToken);
            if (group.Schedules?.Count > 0) {
                AddSchedulesToScheduleGroup(connection, transaction, group.Id, group.Schedules);
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXScheduleGroup> scheduleGroups,
            Expression<Func<GXScheduleGroup, object>>? columns, CancellationToken cancellationToken = default)
        {
            scheduleGroups = scheduleGroups.ToArray();
            return await GXHistoryMutation.RunAsync<GXScheduleGroup, IEnumerable<Guid>>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, scheduleGroups,
        async () =>
        {
            DateTime now = DateTime.Now;
            GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
            List<Guid> list = [];
            List<GXUserGroup>? defaultGroups = null;
            var newGroups = scheduleGroups.Where(w => w.Id == Guid.Empty).ToList();
            var updatedGroups = scheduleGroups.Where(w => w.Id != Guid.Empty).ToList();
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (var it in scheduleGroups)
                {
                    if (string.IsNullOrEmpty(it.Name))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
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
                    if (it.Schedules != null)
                    {
                        //Update creator and creation time for all schedules.
                        foreach (var schedule in it.Schedules)
                        {
                            if (schedule.Id == Guid.Empty)
                            {
                                schedule.CreationTime = now;
                                schedule.Creator = creator;
                            }
                        }
                    }
                }
                if (newGroups.Count != 0)
                {
                    await _authorizationService.EnsureAnyPolicyAsync(User, GXScheduleGroupPolicies.Add);
                    foreach (var it in newGroups)
                    {
                        it.Creator = creator;
                        it.CreationTime = now;
                        await UpsertAsync(connection, transaction, it, cancellationToken);
                        list.Add(it.Id);
                        _notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Add, connection, transaction), new GXTarget(GXScheduleGroupPolicies.Add, it.Id, it.Name) { ConcurrencyStamp = it.ConcurrencyStamp });
                    }
                }
                if (updatedGroups.Count != 0)
                {
                    await _authorizationService.EnsureAnyPolicyAsync(User, GXScheduleGroupPolicies.Edit);
                    foreach (var it in updatedGroups)
                    {
                        GXSelectArgs m = GXSelectArgs.Select<GXScheduleGroup>(q => q.ConcurrencyStamp!, where => where.Id == it.Id);
                        string? updated = connection.SingleOrDefault<string>(transaction, m);
                        if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }
                        it.Updated = now;
                        it.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                        args.Exclude<GXScheduleGroup>(q => new
                        {
                            q.UserGroups,
                            q.CreationTime,
                            q.Schedules
                        });
                        if (!User.IsInRole(GXRoles.Admin) ||
                            it.Creator == null ||
                            string.IsNullOrEmpty(it.Creator.Id))
                        {
                            //Only admin can update the creator.
                            args.Exclude<GXScheduleGroup>(q => q.Creator!);
                        }
                        connection.Update(transaction, args);
                        await MapUserGroups(connection, transaction, it, false);
                        await MapSchedules(connection, transaction, it, false);
                        list.Add(it.Id);
                        //Notify users
                        _notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Edit, connection, transaction),
                            new GXTarget(GXScheduleGroupPolicies.Edit, it.Id, it.Name) { ConcurrencyStamp = it.ConcurrencyStamp });
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
                        return await UpsertAsync(scheduleGroups, columns, linkedCts.Token);
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
            return [.. list];
        }, cancellationToken);
        }

        /// <summary>
        /// Map user group to Schedule group.
        /// </summary>
        private static async Task MapUserGroups(GXDbConnection connection,
            IDbTransaction transaction, GXScheduleGroup it, bool add)
        {
            if (it.UserGroups != null)
            {
                if (add)
                {
                    AddScheduleGroupToUserGroups(connection, transaction, it.Id, [.. it.UserGroups.Select(s => s.Id)]);
                }
                else
                {
                    List<GXUserGroup> list2 = await GetJoinedUserGroups(connection, transaction, it.Id);
                    List<Guid> groups = [.. list2.Select(s => s.Id)];
                    Guid[] tmp = [.. it.UserGroups.Select(s => s.Id)];
                    Guid[] removed = [.. groups.Except(tmp)];
                    Guid[] added = [.. tmp.Except(groups)];
                    if (added.Length != 0)
                    {
                        AddScheduleGroupToUserGroups(connection, transaction, it.Id, added);
                    }
                    if (removed.Length != 0)
                    {
                        RemoveScheduleGroupFromUserGroups(connection, transaction, it.Id, removed);
                    }
                }
            }
        }

        private async Task MapSchedules(GXDbConnection connection,
            IDbTransaction transaction, GXScheduleGroup it, bool add)
        {
            //Map schedules to Schedule group.
            if (it.Schedules != null)
            {
                if (add)
                {
                    AddSchedulesToScheduleGroup(connection, transaction, it.Id, it.Schedules);
                }
                else
                {
                    List<GXSchedule> list = await GetJoinedSchedules(connection, transaction, it.Id);
                    var comparer = new UniqueComparer<GXSchedule, Guid>();
                    List<GXSchedule> removed = [.. list.Except(it.Schedules, comparer)];
                    List<GXSchedule> added = [.. it.Schedules.Except(list, comparer)];
                    if (added.Count != 0)
                    {
                        AddSchedulesToScheduleGroup(connection, transaction, it.Id, added);
                    }
                    if (removed.Count != 0)
                    {
                        RemoveSchedulesFromScheduleGroup(connection, transaction, it.Id, removed);
                    }
                }
            }
        }

        /// <summary>
        /// Map schedule group to user groups.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleGroupId">Schedule group ID.</param>
        /// <param name="groups">Group IDs of the schedule groups where the schedule is added.</param>
        /// <param name="connection">The database connection.</param>
        private static void AddScheduleGroupToUserGroups(GXDbConnection connection,
            IDbTransaction transaction, Guid scheduleGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupScheduleGroup> list = [];
            foreach (var ug in groups)
            {
                list.Add(new GXUserGroupScheduleGroup()
                {
                    ScheduleGroupId = scheduleGroupId,
                    UserGroupId = ug,
                    CreationTime = now
                });
            }
            connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between schedule group and user groups.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleGroupId">Schedule group ID.</param>
        /// <param name="groups">Group IDs of the schedule groups where the schedule is removed.</param>
        /// <param name="connection">The database connection.</param>
        private static void RemoveScheduleGroupFromUserGroups(GXDbConnection connection,
            IDbTransaction transaction, Guid scheduleGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupScheduleGroup>();
            foreach (var ug in groups)
            {
                args.Where.Or<GXUserGroupScheduleGroup>(w => w.UserGroupId == ug &&
                    w.ScheduleGroupId == scheduleGroupId);
            }
            connection.Delete(transaction, args);
        }

        /// <summary>
        /// Map schedules to schedule group.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleGroupId">Schedule group ID.</param>
        /// <param name="schedules">Added schedules.</param>
        /// <param name="connection">The database connection.</param>
        private static void AddSchedulesToScheduleGroup(
            GXDbConnection connection, IDbTransaction transaction,
            Guid scheduleGroupId,
            IEnumerable<GXSchedule> schedules)
        {
            DateTime now = DateTime.Now;
            List<GXSchedule> added = [];
            List<GXScheduleGroupSchedule> list = [];
            foreach (var schedule in schedules)
            {
                if (schedule.Id == Guid.Empty)
                {
                    added.Add(schedule);
                }
            }
            connection.Insert(transaction, GXInsertArgs.InsertRange(added));
            foreach (var schedule in schedules)
            {
                list.Add(new GXScheduleGroupSchedule()
                {
                    ScheduleGroupId = scheduleGroupId,
                    ScheduleId = schedule.Id,
                    CreationTime = now
                });
            }
            connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between schedules and schedule group.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleGroupId">Schedule group ID.</param>
        /// <param name="groups">Group IDs of the schedule groups where the schedule is removed.</param>
        /// <param name="connection">The database connection.</param>
        private static void RemoveSchedulesFromScheduleGroup(
            GXDbConnection connection, IDbTransaction transaction,
            Guid scheduleGroupId,
            IEnumerable<GXSchedule> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXScheduleGroupSchedule>();
            foreach (var it in groups)
            {
                args.Where.Or<GXScheduleGroupSchedule>(w => w.ScheduleId == it.Id &&
                    w.ScheduleGroupId == scheduleGroupId);
            }
            connection.Delete(transaction, args);
        }
    }
}
