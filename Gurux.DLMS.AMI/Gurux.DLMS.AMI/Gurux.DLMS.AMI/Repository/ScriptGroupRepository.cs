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
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq.Expressions;

using System.Text.RegularExpressions;
using Gurux.DLMS.AMI.Hubs;
using System.Security.Claims;
using Gurux.Service.Orm.Enums;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc/>
    public class ScriptGroupRepository : IScriptGroupRepository
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
        public ScriptGroupRepository(
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

        private static async Task<List<GXUserGroup>> GetJoinedUserGroups(GXDbConnection connection, Guid scriptGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupScriptGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupScriptGroup, GXScriptGroup>(a => a.ScriptGroupId, b => b.Id);
            arg.Where.And<GXScriptGroup>(where => where.Removed == null && where.Id == scriptGroupId);
            return (await connection.SelectAsync<GXUserGroup>(arg));
        }

        private static async Task<List<GXScript>> GetJoinedScripts(GXDbConnection connection, Guid scriptGroupId)
        {
            //Membership reconciliation needs IDs only; DISTINCT cannot compare script BLOB data on Oracle.
            GXSelectArgs arg = GXSelectArgs.Select<GXScript>(script => script.Id, where => where.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXScript, GXScriptGroupScript>(a => a.Id, b => b.ScriptId);
            arg.Joins.AddInnerJoin<GXScriptGroupScript, GXScriptGroup>(a => a.ScriptGroupId, b => b.Id);
            arg.Where.And<GXScriptGroup>(where => where.Removed == null && where.Id == scriptGroupId);
            return (await connection.SelectAsync<GXScript>(arg));
        }

        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXScriptGroup), id, action, GXScriptGroupPolicies.View, connection, transaction, notifications: _notificationCache);

        /// <inheritdoc />
        public async Task<List<GXScriptGroup>> GetJoinedScriptGroups(Guid scriptId)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXScriptGroupPolicies.View);

            GXSelectArgs arg = GXSelectArgs.SelectAll<GXScriptGroup>(where => where.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXScriptGroup, GXScriptGroupScript>(a => a.Id, b => b.ScriptGroupId);
            arg.Joins.AddInnerJoin<GXScriptGroupScript, GXScript>(a => a.ScriptId, b => b.Id);
            arg.Where.And<GXScript>(where => where.Removed == null && where.Id == scriptId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            return (await connection.SelectAsync<GXScriptGroup>(arg));
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? groupId, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXScriptGroupPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByScriptGroup(s => s.Id!,
                ServerHelpers.GetUserId(User), groupId);
            args.Distinct = true;
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (groupId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXScriptGroup), groupId, token: cancellationToken);
                args.Where.And<GXUser>(u => members.Contains(u.Id!));
            }
            List<GXUser> users = await connection.SelectAsync<GXUser>(args);
            List<string> ret = [.. users.Select(s => s.Id!)];
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? groupIds, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXScriptGroupPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByScriptGroups(s => s.Id!,
                ServerHelpers.GetUserId(User), groupIds);
            args.Distinct = true;
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            List<GXUser> users = await connection.SelectAsync<GXUser>(args);
            List<string> ret = [.. users.Select(s => s.Id!)];
            if (User != null && User.IsInRole(GXRoles.Admin))
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
            await GXHistoryMutation.RunDeleteAsync<GXScriptGroup, Guid>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, groups,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXScriptGroupPolicies.Delete);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            IEnumerable<GXScriptGroup> exists;
            if (User.IsInRole(GXRoles.Admin))
            {
                GXSelectArgs arg = GXSelectArgs.Select<GXScriptGroup>(s => new { s.Id, s.Name }, w => groups.Contains(w.Id));
                exists = connection.Select<GXScriptGroup>(arg);
            }
            else
            {
                //Check that the user has access rights to the removed agent groups.
                string userId = ServerHelpers.GetUserId(User);
                var arg = GXQuery.GetScriptGroupsByUser(s => new { s.Id, s.Name }, userId, null,
                    DateTimeOffset.MinValue, null, groups);
                exists = connection.Select<GXScriptGroup>(arg);
            }
            if (!exists.Any())
            {
                //If user try to remove item that is not in the list, throw exception.
                //This can happen if user try to remove item that is already removed or user try to remove item that he doesn't have access.
                throw new GXAmiForbiddenException(string.Join(", ", groups));
            }
            DateTime now = DateTime.Now;
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (GXScriptGroup it in exists)
                {
                    _notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Remove, connection, transaction),
                        new GXTarget(GXScriptGroupPolicies.Delete, it.Id, it.Name));
                    it.Removed = now;
                    if (!delete)
                    {
                        connection.Update(transaction, GXUpdateArgs.Update(it, q => q.Removed!));
                    }
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
        }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXScriptGroup>> ListAsync(
            ListScriptGroups? request,
            ListScriptGroupsResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXScriptGroupPolicies.View);
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the script groups.
                arg = GXSelectArgs.SelectAll<GXScriptGroup>();
            }
            else
            {
                string userId = Internal.ServerHelpers.GetUserId(User);
                arg = GXQuery.GetScriptGroupsByUser(s => "*", userId, null, request?.Filter?.Removed, request?.Exclude, request?.Included);
            }
            if (request != null)
            {
                //If script groups are filtered by user.
                if (request.Filter?.UserGroups != null)
                {
                    var ug = request.Filter.UserGroups.FirstOrDefault();
                    if (ug?.Users != null && ug.Users.Count != 0)
                    {
                        var user = ug.Users.FirstOrDefault();
                        if (user != null)
                        {
                            arg.Joins.AddLeftJoin<GXUserGroupScriptGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                            arg.Joins.AddLeftJoin<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId);
                            arg.Joins.AddLeftJoin<GXUserGroupUser, GXUser>(j => j.UserId!, j => j.Id!);
                            arg.Where.FilterBy(user);
                        }
                    }
                    request.Filter.UserGroups = null;
                }
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude?.Any() == true)
                {
                    arg.Where.And<GXScriptGroup>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included?.Any() == true)
                {
                    arg.Where.And<GXScriptGroup>(w => request.Included.Contains(w.Id));
                }
            }
            arg.Distinct = true;
            GXDateTimeFilters.Apply<GXScriptGroup>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXScriptGroup>(q => GXSql.Count(1));
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
                arg.OrderBy.Add<GXScriptGroup>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXScriptGroup>(q => q.CreationTime!);
            }
            GXScriptGroup[] groups = [.. (await connection.SelectAsync<GXScriptGroup>(arg))];
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
        public async Task<GXScriptGroup> ReadAsync(Guid id, Expression<Func<GXScriptGroup, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXScriptGroupPolicies.View);
                GXSelectArgs arg;
                if (User.IsInRole(GXRoles.Admin))
                {
                    //Admin can see all the groups.
                    arg = GXSelectArgs.SelectAll<GXScriptGroup>(w => w.Id == id);
                }
                else
                {
                    string userId = ServerHelpers.GetUserId(User);
                    arg = GXQuery.GetScriptGroupsByUser(s => "*", userId, id);
                }
                arg.Columns.Exclude<GXScriptGroup>(e => e.Scripts!);
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                var group = (await connection.SingleOrDefaultAsync<GXScriptGroup>(arg)) ?? throw new GXAmiForbiddenException(id.ToString());
                ////////////////////////////////////////////////////
                //Get scripts that belongs for this script group.
                arg = GXSelectArgs.SelectAll<GXScript>(w => w.Removed == null);
                //Distinct can't be used with Oracle DB because there is a BLOB field in the script table. This causes that same script is returned multiple times if it belongs to multiple groups. To avoid this, distinct is removed and duplicates are removed in memory.
                //This is not optimal but there is no better solution for this because of Oracle DB limitations.
                //arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXScriptGroupScript, GXScript>(j => j.ScriptId, j => j.Id);
                arg.Where.And<GXScriptGroupScript>(q => q.Removed == null && q.ScriptGroupId == id);
                group.Scripts = await connection.SelectAsync<GXScript>(arg);

                ////////////////////////////////////////////////////
                //Get user groups that belongs for this script group.
                arg = GXSelectArgs.SelectAll<GXUserGroup>(w => w.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXUserGroupScriptGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                arg.Where.And<GXUserGroupScriptGroup>(q => q.Removed == null && q.ScriptGroupId == id);
                group.UserGroups = await connection.SelectAsync<GXUserGroup>(arg);
                //Get creator with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName });
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXScriptGroup, GXUser>(s => s.Creator!, o => o.Id!);
                arg.Where.And<GXScriptGroup>(w => w.Id == id);
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
            GXScriptGroup group, CancellationToken cancellationToken = default)
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
            args.Exclude<GXScriptGroup>(group => new { group.UserGroups });
            await connection.InsertAsync(transaction, args, cancellationToken);
            var memberships = group.UserGroups.Select(userGroup => new GXUserGroupScriptGroup
            {
                UserGroupId = userGroup.Id,
                ScriptGroupId = group.Id,
                CreationTime = DateTime.Now
            }).ToArray();
            await connection.InsertAsync(transaction, GXInsertArgs.InsertRange(memberships), cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXScriptGroup> ScriptGroups,
            Expression<Func<GXScriptGroup, object>>? columns, CancellationToken cancellationToken = default)
        {
            ScriptGroups = ScriptGroups.ToArray();
            return await GXHistoryMutation.RunAsync<GXScriptGroup, IEnumerable<Guid>>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, ScriptGroups,
        async () =>
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXScriptGroupPolicies.Add, GXScriptGroupPolicies.Edit);
                DateTime now = DateTime.Now;
                GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                foreach (GXScriptGroup it in ScriptGroups)
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
                    if (it.Scripts != null)
                    {
                        foreach (GXScript script in it.Scripts)
                        {
                            script.CreationTime = now;
                            script.Creator = creator;
                        }
                    }
                    if (it.Id == Guid.Empty)
                    {
                        it.Creator = creator;
                        it.CreationTime = now;
                        using (var insertTransaction = connection.BeginTransaction())
                        {
                            await UpsertAsync(connection, insertTransaction, it, cancellationToken);
                            insertTransaction.Commit();
                        }

                        _notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Add, connection),
                            new GXTarget(GXScriptGroupPolicies.Add, it.Id, it.Name) { ConcurrencyStamp = it.ConcurrencyStamp });
                    }
                    else
                    {
                        GXSelectArgs m = GXSelectArgs.Select<GXScriptGroup>(q => q.ConcurrencyStamp!, where => where.Id == it.Id);
                        string? updated = connection.SingleOrDefault<string>(m);
                        if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }
                        it.Updated = now;
                        it.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs args = GXUpdateArgs.Update(it);
                        args.Exclude<GXScriptGroup>(q => new
                        {
                            q.UserGroups,
                            q.CreationTime,
                            q.Scripts
                        });
                        if (!User.IsInRole(GXRoles.Admin) ||
                            it.Creator == null ||
                            string.IsNullOrEmpty(it.Creator.Id))
                        {
                            //Only admin can update the creator.
                            args.Exclude<GXScriptGroup>(q => q.Creator!);
                        }
                        connection.Update(args);
                        //Map user group to Script group.
                        List<GXUserGroup> list2 = await GetJoinedUserGroups(connection, it.Id);
                        List<Guid> groups = [.. list2.Select(s => s.Id)];
                        Guid[] tmp = [.. it.UserGroups.Select(s => s.Id)];
                        Guid[] removed = [.. groups.Except(tmp)];
                        Guid[] added = [.. tmp.Except(groups)];
                        if (added.Length != 0)
                        {
                            AddScriptGroupToUserGroups(connection, it.Id, added);
                        }
                        if (removed.Length != 0)
                        {
                            RemoveScriptGroupFromUserGroups(connection, it.Id, removed);
                        }
                        //Map scripts to Script group.
                        if (it.Scripts != null)
                        {
                            List<GXScript> list3 = await GetJoinedScripts(connection, it.Id);
                            List<Guid> groups2 = [.. list3.Select(s => s.Id)];
                            tmp = [.. it.Scripts.Select(s => s.Id)];
                            removed = [.. groups2.Except(tmp)];
                            added = [.. tmp.Except(groups2)];
                            if (added.Length != 0)
                            {
                                AddScriptsToScriptGroup(connection, it.Id, added);
                            }
                            if (removed.Length != 0)
                            {
                                RemoveScriptsFromScriptGroup(connection, it.Id, removed);
                            }
                        }
                        _notificationCache.Add(await GetNotifiedUsers(it.Id, NotificationAction.Edit, connection),
                            new GXTarget(GXScriptGroupPolicies.Edit, it.Id, it.Name) { ConcurrencyStamp = it.ConcurrencyStamp });
                    }
                }
                return [.. ScriptGroups.Select(s => s.Id)];

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
                        return await UpsertAsync(ScriptGroups, columns, linkedCts.Token);
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
        /// Map script group to user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="scriptGroupId">Script group ID.</param>
        /// <param name="groups">Group IDs of the script groups where the script is added.</param>
        private static void AddScriptGroupToUserGroups(GXDbConnection connection,
            Guid scriptGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupScriptGroup> list = [];
            foreach (var ug in groups)
            {
                list.Add(new GXUserGroupScriptGroup()
                {
                    ScriptGroupId = scriptGroupId,
                    UserGroupId = ug,
                    CreationTime = now
                });
            }
            connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between script group and user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="scriptGroupId">Script group ID.</param>
        /// <param name="groups">Group IDs of the script groups where the script is removed.</param>
        private static void RemoveScriptGroupFromUserGroups(GXDbConnection connection,
            Guid scriptGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupScriptGroup>();
            foreach (var ug in groups)
            {
                args.Where.Or<GXUserGroupScriptGroup>(w => w.UserGroupId == ug &&
                    w.ScriptGroupId == scriptGroupId);
            }
            connection.Delete(args);
        }

        /// <summary>
        /// Map scripts to script group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="scriptGroupId">Script group ID.</param>
        /// <param name="groups">Group IDs of the script groups where the script is added.</param>
        private static void AddScriptsToScriptGroup(GXDbConnection connection,
            Guid scriptGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXScriptGroupScript> list = [];
            foreach (var ug in groups)
            {
                list.Add(new GXScriptGroupScript()
                {
                    ScriptGroupId = scriptGroupId,
                    ScriptId = ug,
                    CreationTime = now
                });
            }
            connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between scripts and script group.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="scriptGroupId">Script group ID.</param>
        /// <param name="groups">Group IDs of the script groups where the script is removed.</param>
        private static void RemoveScriptsFromScriptGroup(GXDbConnection connection,
            Guid scriptGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXScriptGroupScript>();
            foreach (var it in groups)
            {
                args.Where.Or<GXScriptGroupScript>(w => w.ScriptId == it &&
                    w.ScriptGroupId == scriptGroupId);
            }
            connection.Delete(args);
        }
    }
}
