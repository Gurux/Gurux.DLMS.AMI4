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
using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Client.Layout;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Gurux.UI.Components;
using Gurux.UI.Components.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Repository
{

    /// <inheritdoc />
    public class BlockRepository : IBlockRepository
    {

        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IUserRepository _userRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IBlockGroupRepository _blockGroupRepository;
        private readonly IComponentViewRepository _componentViewRepository;
        private readonly NotificationCache _notificationCache;
        private readonly IAuthorizationService _authorizationService;
        /// <summary>
        /// Constructor.
        /// </summary>
        public BlockRepository(
            IAuthorizationService authorizationService,
            IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor,
            IUserRepository userRepository,
            IServiceProvider serviceProvider,
            IBlockGroupRepository blockGroupRepository,
            IComponentViewRepository componentViewRepository,
            NotificationCache notificationCache)
        {
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            User = httpContextAccessor.HttpContext.User;
            _dbFactory = dbFactory;
            _userRepository = userRepository;
            _serviceProvider = serviceProvider;
            _blockGroupRepository = blockGroupRepository;
            _notificationCache = notificationCache;
            _authorizationService = authorizationService;
            _componentViewRepository = componentViewRepository;
        }

        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXBlock), id, action, GXBlockPolicies.View, connection, transaction, notifications: _notificationCache);

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? blockId, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXBlockPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByBlock(s => s.Id!,
                ServerHelpers.GetUserId(User), blockId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (blockId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXBlock), blockId, token: cancellationToken);
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
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? blockIds, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXBlockPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByBlocks(s => s.Id!,
                ServerHelpers.GetUserId(User), blockIds);
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
        public async Task DeleteAsync(IEnumerable<Guid> blocks, bool delete = true, bool notify = true,
            CancellationToken cancellationToken = default)
        {
            blocks = blocks.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXBlock, Guid>(_dbFactory, User, _serviceProvider, blocks,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXBlockPolicies.Delete);
            blocks.ValidateIds();
            try
            {
                GXSelectArgs arg = GXSelectArgs.Select<GXBlock>(a => a.Id, q => blocks.Contains(q.Id));
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                List<GXBlock> list = connection.Select<GXBlock>(arg);
                DateTime now = DateTime.Now;
                Dictionary<GXBlock, List<string>> updates = [];
                foreach (GXBlock it in list)
                {
                    it.Removed = now;
                    List<string> users = await GetNotifiedUsers(it.Id, NotificationAction.Remove, connection);
                    if (delete)
                    {
                        await connection.DeleteAsync(GXDeleteArgs.DeleteById<GXBlock>(it.Id), cancellationToken);
                    }
                    else
                    {
                        connection.Update(GXUpdateArgs.Update(it, q => q.Removed!));
                    }
                    updates[it] = users;
                }
                foreach (var it in updates)
                {
                    _notificationCache.Add(it.Value,
                        new GXTarget(GXBlockPolicies.Delete, it.Key.Id, it.Key.Name));
                }

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
                        await DeleteAsync(blocks, delete, notify, linkedCts.Token);
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

        private async Task<List<Guid>> GetVisibleBlockIdsAsync(GXDbConnection connection, CancellationToken cancellationToken)
        {
            GXSelectArgs groups;
            if (User?.IsInRole(GXRoles.Admin) == true)
            {
                groups = GXSelectArgs.Select<GXBlockGroup>(q => q.Id, q => q.Removed == null);
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(User, false);
                if (userId == null)
                {
                    return [];
                }
                groups = GXQuery.GetBlockGroupsByUser(q => q.Id, userId);
            }
            var allowedGroups = new List<Guid>();
            foreach (var group in await connection.SelectAsync<GXBlockGroup>(groups))
            {
                group.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXBlockGroup), group.Id, cancellationToken);
                if (await GXResourcePolicyAccess.AllowsAsync(connection, _authorizationService, User, group.Policies, cancellationToken))
                {
                    allowedGroups.Add(group.Id);
                }
            }
            if (allowedGroups.Count == 0)
            {
                return [];
            }
            var mappings = GXSelectArgs.Select<GXBlockGroupBlock>(q => q.BlockId,
                q => q.Removed == null && allowedGroups.Contains(q.BlockGroupId));
            return [.. (await connection.SelectAsync<GXBlockGroupBlock>(mappings)).Select(q => q.BlockId).Distinct()];
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXBlock>> ListAsync(
            ListBlocks? request,
            ListBlocksResponse? response,
            CancellationToken cancellationToken = default)
        {
            //All users can see the blocks that are shared with them. Admin can see all the blocks.
            GXSelectArgs arg;
            if (User?.IsInRole(GXRoles.Admin) == true)
            {
                // Group policies below determine normal admin visibility. Applying the
                // user's memberships here would discard groups reserved for admins.
                arg = GXSelectArgs.SelectAll<GXBlock>();
                if (request?.AllUsers != true && request?.Filter?.Removed == null)
                {
                    arg.Where.And<GXBlock>(b => b.Removed == null);
                }
                if (request?.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXBlock>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included?.Any() == true)
                {
                    arg.Where.And<GXBlock>(w => request.Included.Contains(w.Id));
                }
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(User, false);
                if (userId == null)
                {
                    //Return a list of blocks that anonymous users can access.
                    return [];
                }
                arg = GXQuery.GetBlocksByUser(s => "*", userId, null, request?.Filter?.Removed, request?.Exclude,
                    request?.Included);
            }
            if (request?.Filter != null)
            {
                //User is already filtered. It can be removed.
                GXUser? orig = request.Filter.User;
                try
                {
                    request.Filter.User = null;
                    arg.Where.FilterBy(request.Filter);
                }
                finally
                {
                    request.Filter.User = orig;
                }
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXBlock>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXBlock>(q => q.CreationTime!);
                arg.Descending = true;
            }
            arg.Distinct = true;
            GXDateTimeFilters.Apply<GXBlock>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            string? closingUserId = ServerHelpers.GetUserId(User, false);
            if (request?.ExcludeClosed == true && closingUserId != null)
            {
                var closed = GXSelectArgs.Select<GXUserBlockSettings>(s => GXSql.One,
                    s => s.UserId == closingUserId && s.Closed != null);
                arg.Where.And<GXBlock>(b => !GXSql.Exists<GXBlock, GXUserBlockSettings>(
                    b => b.Id, s => s.BlockId, closed));
            }
            if (!(request?.AllUsers == true && User?.IsInRole(GXRoles.Admin) == true))
            {
                var visibleIds = await GetVisibleBlockIdsAsync(connection, cancellationToken);
                if (visibleIds.Count == 0)
                {
                    if (response != null)
                    {
                        response.Count = 0;
                        response.Blocks = [];
                    }
                    return [];
                }
                arg.Where.And<GXBlock>(q => visibleIds.Contains(q.Id));
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXBlock>(q => GXSql.Count(1));
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
            GXBlock[] blocks = [.. (await connection.SelectAsync<GXBlock>(arg))];
            foreach (var block in blocks)
            {
                foreach (var group in block.BlockGroups ?? [])
                {
                    group.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXBlockGroup), group.Id, cancellationToken);
                }
            }

            foreach (var block in blocks)
            {
                switch (block.BlockType)
                {
                    case BlockType.Html:
                        break;
                    case BlockType.Component:
                        {
                            arg = GXSelectArgs.Select<GXComponentView>(s => new { s.Id, s.Name, s.ClassName });
                            arg.Joins.AddInnerJoin<GXComponentView, GXBlock>(j => j.Id, j => j.ComponentView!);
                            arg.Where.And<GXBlock>(w => w.Id == block.Id);
                            block.ComponentView = await connection.SingleOrDefaultAsync<GXComponentView>(arg);
                        }
                        break;
                    case BlockType.Script:
                        arg = GXSelectArgs.Select<GXScriptMethod>(s => new { s.Id, s.Name });
                        arg.Columns.Add<GXScript>(s => new { s.Id, s.Name });
                        arg.Joins.AddInnerJoin<GXScriptMethod, GXBlock>(j => j.Id, j => j.ScriptMethod!);
                        arg.Joins.AddInnerJoin<GXScriptMethod, GXScript>(j => j.Script!, j => j.Id);
                        arg.Where.And<GXBlock>(w => w.Id == block.Id);
                        block.ScriptMethod = await connection.SingleOrDefaultAsync<GXScriptMethod>(arg);
                        break;
                }
            }
            if (request?.Filter?.Enabled.GetValueOrDefault() == true)
            {
                blocks = [.. blocks.Where(w => w.Enabled == true)];
            }
            if (response != null)
            {
                response.Blocks = blocks;
                if (response.Count == 0)
                {
                    response.Count = blocks.Length;
                }
            }
            return blocks;
        }

        /// <inheritdoc />
        public async Task<GXBlock> ReadAsync(Guid id, Expression<Func<GXBlock, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {

                //All users can see the blocks that are shared with them. Admin can see all the blocks.
                GXSelectArgs arg;
                if (User?.IsInRole(GXRoles.Admin) == true)
                {
                    //Admin can see all the blocks.
                    arg = GXSelectArgs.SelectAll<GXBlock>(w => w.Id == id);
                    arg.Joins.AddLeftJoin<GXBlock, GXBlockGroupBlock>(x => x.Id, y => y.BlockId);
                    arg.Joins.AddLeftJoin<GXBlockGroupBlock, GXBlockGroup>(j => j.BlockGroupId, j => j.Id);
                }
                else
                {
                    string userId = ServerHelpers.GetUserId(User);
                    arg = GXQuery.GetBlocksByUser(s => "*", userId, id);
                    arg.Joins.AddInnerJoin<GXBlock, GXBlockGroupBlock>(x => x.Id, y => y.BlockId);
                    arg.Joins.AddInnerJoin<GXBlockGroupBlock, GXBlockGroup>(j => j.BlockGroupId, j => j.Id);
                }
                arg.Columns.Add<GXBlockGroup>();
                arg.Columns.Exclude<GXBlockGroup>(e => e.Blocks!);
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                var visibleIds = await GetVisibleBlockIdsAsync(connection, cancellationToken);
                if (!visibleIds.Contains(id))
                {
                    throw new UnauthorizedAccessException();
                }
                GXBlock? block = await connection.SingleOrDefaultAsync<GXBlock>(arg) ?? throw new GXAmiForbiddenException(id.ToString());
                if (block.BlockGroups != null)
                {
                    var visibleGroups = new List<GXBlockGroup>();
                    foreach (var group in block.BlockGroups)
                    {
                        group.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXBlockGroup), group.Id, cancellationToken);
                        if (group.Removed == null && await GXResourcePolicyAccess.AllowsAsync(connection, _authorizationService, User, group.Policies, cancellationToken))
                        {
                            visibleGroups.Add(group);
                        }
                    }
                    block.BlockGroups = visibleGroups;
                }
                if (block.BlockType == BlockType.Script)
                {
                    //Get script and method.
                    arg = GXSelectArgs.SelectAll<GXScriptMethod>();
                    arg.Columns.Add<GXScript>();
                    arg.Columns.Exclude<GXScript>(e => e.Methods!);
                    arg.Joins.AddInnerJoin<GXScriptMethod, GXBlock>(y => y.Id, x => x.ScriptMethod!);
                    arg.Joins.AddInnerJoin<GXScriptMethod, GXScript>(y => y.Script!, x => x.Id);
                    arg.Where.And<GXBlock>(w => w.Id == block.Id);
                    block.ScriptMethod = await connection.SingleOrDefaultAsync<GXScriptMethod>(arg);
                }
                else if (block.BlockType == BlockType.Component)
                {
                    //Get component view.
                    arg = GXSelectArgs.SelectAll<GXComponentView>();
                    arg.Joins.AddInnerJoin<GXComponentView, GXBlock>(y => y.Id, x => x.ComponentView!);
                    arg.Where.And<GXBlock>(w => w.Id == block.Id);
                    block.ComponentView = await connection.SingleOrDefaultAsync<GXComponentView>(arg);
                }
                //Get creator with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName }, q => q.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXBlock, GXUser>(s => s.Creator!, o => o.Id!);
                arg.Where.And<GXBlock>(w => w.Id == id);
                block.Creator = await connection.SingleOrDefaultAsync<GXUser>(arg);
                return block;

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
            IEnumerable<GXBlock> blocks,
            Expression<Func<GXBlock, object>>? columns, CancellationToken cancellationToken = default)
        {
            blocks = blocks.ToArray();
            return await GXHistoryMutation.RunAsync<GXBlock, IEnumerable<Guid>>(_dbFactory, User, _serviceProvider, blocks,
        async () =>
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXBlockPolicies.Add, GXBlockPolicies.Edit);
                DateTime now = DateTime.Now;
                GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
                Dictionary<GXBlock, List<string>> updates = [];
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                foreach (GXBlock block in blocks)
                {
                    if (string.IsNullOrEmpty(block.Name))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    if (block.BlockGroups == null || block.BlockGroups.Count == 0)
                    {
                        ListBlockGroups request = new()
                        {
                            Filter = new GXBlockGroup() { Default = true }
                        };
                        block.BlockGroups = [.. await _blockGroupRepository.ListAsync(request, cancellationToken: cancellationToken)];
                        if (block.BlockGroups.Count == 0)
                        {
                            throw new ArgumentException(Properties.Resources.TargetMustBelongToOneGroup);
                        }
                    }
                    if (block.BlockType is BlockType.Html or BlockType.Markdown or BlockType.HtmlFile or BlockType.MarkdownFile or BlockType.RSS)
                    {
                        block.ComponentView = null;
                        if (block.BlockType == BlockType.RSS)
                        {
                            Gurux.DLMS.AMI.Server.Internal.GXBlockRssFeed.ValidateAddress(block.Body);
                        }
                        if (block.BlockType is BlockType.HtmlFile or BlockType.MarkdownFile && string.IsNullOrWhiteSpace(block.Body))
                        {
                            throw new ArgumentException("Select a server content file.");
                        }
                    }
                    else if (block.BlockType == BlockType.Component)
                    {
                        if (block.ComponentView == null)
                        {
                            throw new ArgumentException("Component is not selected.");
                        }
                    }
                    else if (block.BlockType == BlockType.Script)
                    {
                        if (block.ScriptMethod == null)
                        {
                            throw new ArgumentException("Script is not selected.");
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Invalid block type.");
                    }
                    if (block.Id == Guid.Empty)
                    {
                        block.Creator = creator;
                        block.CreationTime = now;
                        await using (var transaction = await connection.BeginTransactionAsync(cancellationToken))
                        {
                            try
                            {
                                GXInsertArgs args = GXInsertArgs.Insert(block);
                                args.Exclude<GXBlock>(q => new
                                {
                                    q.BlockGroups,
                                });
                                connection.Insert(transaction, args);
                                AddBlockToBlockGroups(connection, block.Id, block.BlockGroups, transaction);
                                await transaction.CommitAsync(cancellationToken);
                            }
                            catch (Exception insertError)
                            {
                                try
                                {
                                    await transaction.RollbackAsync(CancellationToken.None);
                                }
                                catch (Exception rollbackError)
                                {
                                    insertError.Data["RollbackError"] = rollbackError;
                                }
                                finally
                                {
                                    // Preserve create semantics if the outer deadlock handler retries.
                                    block.Id = Guid.Empty;
                                }
                                throw;
                            }
                        }
                        updates[block] = await GetNotifiedUsers(block.Id, NotificationAction.Edit, connection);
                    }
                    else
                    {
                        GXSelectArgs m = GXSelectArgs.Select<GXBlock>(q => q.ConcurrencyStamp!, where => where.Id == block.Id);
                        string? updated = connection.SingleOrDefault<string>(m);
                        if (!string.IsNullOrEmpty(updated) && updated != block.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }
                        block.Updated = now;
                        block.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs args = GXUpdateArgs.Update(block, columns);
                        args.Exclude<GXBlock>(q => new
                        {
                            q.CreationTime,
                            q.BlockGroups,
                        });
                        if (!User.IsInRole(GXRoles.Admin) ||
                            block.Creator == null ||
                            string.IsNullOrEmpty(block.Creator.Id))
                        {
                            //Only admin can update the creator.
                            args.Exclude<GXBlock>(q => q.Creator!);
                        }
                        connection.Update(args);
                        //Map block groups to block.
                        List<GXBlockGroup> blockGroups;
                        using (IServiceScope scope = _serviceProvider.CreateScope())
                        {
                            IBlockGroupRepository blockGroupRepository = scope.ServiceProvider.GetRequiredService<IBlockGroupRepository>();
                            blockGroups = await blockGroupRepository.GetJoinedBlockGroups(block.Id);
                        }
                        var comparer = new UniqueComparer<GXBlockGroup, Guid>();
                        List<GXBlockGroup> removedBlockGroups = [.. blockGroups.Except(block.BlockGroups, comparer)];
                        List<GXBlockGroup> addedBlockGroups = [.. block.BlockGroups.Except(blockGroups, comparer)];
                        if (removedBlockGroups.Count != 0)
                        {
                            RemoveBlocksFromBlockGroup(connection, block.Id, removedBlockGroups);
                        }
                        if (addedBlockGroups.Count != 0)
                        {
                            AddBlockToBlockGroups(connection, block.Id, addedBlockGroups);
                        }
                        updates[block] = await GetNotifiedUsers(block.Id, NotificationAction.Edit, connection);
                    }
                }
                foreach (var it in updates)
                {
                    _notificationCache.Add(await GetNotifiedUsers(it.Key.Id, NotificationAction.Edit, connection),
                        new GXTarget(GXBlockPolicies.Edit, it.Key.Id, it.Key.Name));
                }
                return [.. blocks.Select(s => s.Id)];

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
                        return await UpsertAsync(blocks, columns, linkedCts.Token);
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
        /// Map block group to user groups.
        /// </summary>
        /// <param name="blockId">Block ID.</param>
        /// <param name="groups">Group IDs of the block groups where the block is added.</param>
        /// <param name="connection">The database connection.</param>
        /// <param name="transaction">Transaction shared by the block and its group links.</param>
        private static void AddBlockToBlockGroups(GXDbConnection connection,
            Guid blockId, IEnumerable<GXBlockGroup> groups, System.Data.IDbTransaction? transaction = null)
        {
            DateTime now = DateTime.Now;
            List<GXBlockGroupBlock> list = [];
            foreach (GXBlockGroup it in groups.DistinctBy(group => group.Id))
            {
                list.Add(new GXBlockGroupBlock()
                {
                    BlockId = blockId,
                    BlockGroupId = it.Id,
                    CreationTime = now
                });
            }
            connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between block group and block.
        /// </summary>
        /// <param name="blockId">Block ID.</param>
        /// <param name="groups">Group IDs of the block groups where the block is removed.</param>
        /// <param name="connection">The database connection.</param>
        private static void RemoveBlocksFromBlockGroup(GXDbConnection connection,
            Guid blockId, IEnumerable<GXBlockGroup> groups)
        {
            foreach (GXBlockGroup it in groups)
            {
                connection.Delete(GXDeleteArgs.Delete<GXBlockGroupBlock>(w => w.BlockId == blockId && w.BlockGroupId == it.Id));
            }
        }

        /// <inheritdoc />
        public async Task CloseAsync(IEnumerable<Guid>? blocks, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXBlockPolicies.Close);
            string userId = ServerHelpers.GetUserId(User);
            var ids = (blocks ?? []).Distinct().ToArray();
            if (ids.Length == 0)
            {
                return;
            }
            var now = DateTimeOffset.UtcNow;
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable);
            foreach (Guid it in ids)
            {
                GXSelectArgs args = GXSelectArgs.SelectAll<GXUserBlockSettings>(where => where.BlockId == it && where.UserId == userId);
                GXUserBlockSettings? s = await connection.SingleOrDefaultAsync<GXUserBlockSettings>(transaction, args, cancellationToken);
                if (s?.Closed != null)
                {
                    continue;
                }
                if (s == null)
                {
                    s = new GXUserBlockSettings() { UserId = userId, BlockId = it };
                }
                else
                {
                    // Preserve the user's other settings when replacing the composite-key row.
                    await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXUserBlockSettings>(where => where.BlockId == it && where.UserId == userId), cancellationToken);
                }
                s.Closed = now;
                await connection.InsertAsync(transaction, GXInsertArgs.Insert(s), cancellationToken);
            }
            transaction.Commit();
            foreach (var id in ids)
            {
                _notificationCache.Add(userId, new GXTarget(GXBlockPolicies.Close, id));
            }
        }

        /// <inheritdoc />
        public async Task EnsureDefaultsAsync(CancellationToken cancellationToken = default) {
            await GXHistoryMutation.RunAllAsync<GXBlock>(_dbFactory, User, _serviceProvider,
        async () =>
        {
            await ApplyDefaultsAsync(null, cancellationToken);
        }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task ResetToDefaultsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            ids = ids.ToArray();
            await GXHistoryMutation.RunIdsAsync<GXBlock, Guid>(_dbFactory, User, _serviceProvider, ids,
        async () =>
        {
            ArgumentNullException.ThrowIfNull(ids);
            var selected = ids.Distinct().ToArray();
            if (selected.Length == 0)
            {
                throw new ArgumentException("Select at least one built-in block.", nameof(ids));
            }

            await ApplyDefaultsAsync(selected, cancellationToken);
        }, cancellationToken);
        }

        private async Task ApplyDefaultsAsync(Guid[]? blocks, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXBlockPolicies.Regenerate);
            cancellationToken.ThrowIfCancellationRequested();
            var ids = (blocks ?? []).Distinct().ToArray();
            var now = DateTime.Now;
            var creator = new GXUser { Id = ServerHelpers.GetUserId(User) };
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            var selected = ids.Length == 0
                ? await connection.SelectAsync<GXBlock>(GXSelectArgs.SelectAll<GXBlock>(), cancellationToken)
                : await connection.SelectAsync<GXBlock>(GXSelectArgs.SelectAll<GXBlock>(b => ids.Contains(b.Id)), cancellationToken);
            const string walkthroughs = "Walkthroughs";
            string[] defaultNames = ["Sidebar header", "Left sidebar", "Footer", "Not found", walkthroughs];
            if (ids.Length != 0 && (selected.Count != ids.Length || selected.Any(b => !defaultNames.Contains(b.Name))))
            {
                throw new ArgumentException("Only existing built-in blocks (Sidebar header, Left sidebar, Footer, Not found, and Walkthroughs) can be regenerated by ID.", nameof(blocks));
            }

            // Validate the whole selection before creating dependencies or changing persisted blocks.
            var names = ids.Length == 0 ? defaultNames.Where(name => !selected.Any(b => b.Name == name)).ToArray() : [.. selected.Select(b => b.Name!).Distinct()];
            if (names.Length == 0)
            {
                return;
            }

            var groups = new List<GXBlockGroup>();
            if (ids.Length == 0 && names.Any(name => name != walkthroughs))
            {
                var request = new ListBlockGroups { Filter = new GXBlockGroup { Default = true } };
                groups.AddRange(await _blockGroupRepository.ListAsync(request, cancellationToken: cancellationToken));
                if (groups.Count == 0)
                {
                    await _blockGroupRepository.UpsertAsync([new GXBlockGroup
                    {
                        Name = GXRoles.Anonymous, Default = true, Creator = creator, CreationTime = now
                    }], cancellationToken: cancellationToken);
                    groups.AddRange(await _blockGroupRepository.ListAsync(request, cancellationToken: cancellationToken));
                }
                if (groups.Count == 0)
                {
                    throw new InvalidOperationException("A default block group could not be created.");
                }
            }

            // Admins can see all block groups. This dedicated group has no user-group
            // memberships, so ordinary users cannot receive its blocks from the server.
            GXBlockGroup? catalogGroup = null;
            bool insertCatalogGroup = false;
            if (ids.Length == 0 && names.Contains(walkthroughs))
            {
                catalogGroup = await connection.SingleOrDefaultAsync<GXBlockGroup>(
                    GXSelectArgs.SelectAll<GXBlockGroup>(g => g.Name == "Catalog updates (admin)" && g.Removed == null), cancellationToken);
                if (catalogGroup == null)
                {
                    catalogGroup = new GXBlockGroup { Name = "Catalog updates (admin)", Default = false, Creator = creator, CreationTime = now };
                    insertCatalogGroup = true;
                }
            }

            (string Name, Type Type)[] componentDefaults = [
                ("Sidebar header", typeof(NavMenuHeader)), (GXMenus.NavMenu, typeof(NavMenu)),
                (walkthroughs, typeof(Walkthroughs))
            ];
            var required = componentDefaults.Where(c => (names.Contains("Left sidebar") && c.Type == typeof(NavMenu))
                || (names.Contains("Sidebar header") && c.Type == typeof(NavMenuHeader))
                || (names.Contains(walkthroughs) && c.Type == typeof(Walkthroughs))).ToArray();
            var components = new List<GXComponentView>();
            if (required.Length != 0)
            {
                // Names and active state are editable; reuse each existing component by its class.
                var request = new ListComponentViews();
                components.AddRange(await _componentViewRepository.ListAsync(request, cancellationToken: cancellationToken));
                var missing = required.Where(c => !components.Any(v => v.ClassName == c.Type.FullName && v.Removed == null))
                    .Select(c => new GXComponentView
                    {
                        Name = c.Name,
                        ClassName = c.Type.FullName,
                        Enabled = true,
                        Creator = creator,
                        CreationTime = now
                    }).ToArray();
                if (missing.Length != 0)
                {
                    await _componentViewRepository.UpsertAsync(missing, cancellationToken: cancellationToken);
                    components.Clear();
                    components.AddRange(await _componentViewRepository.ListAsync(request, cancellationToken: cancellationToken));
                }
            }
            GXComponentView Component(Type type) => components.FirstOrDefault(c => c.ClassName == type.FullName && c.Removed == null)
                ?? throw new InvalidOperationException("Default component view is missing: " + type.FullName);
            GXBlock Default(string name) => new()
            {
                Name = name,
                BlockType = name is "Footer" or "Not found" ? BlockType.Html : BlockType.Component,
                Visibility = (int)(name == walkthroughs || name == "Not found" ? Visibility.All : Visibility.Medium),
                ComponentView = name == "Left sidebar" ? Component(typeof(NavMenu))
                    : name == "Sidebar header" ? Component(typeof(NavMenuHeader))
                    : name == walkthroughs ? Component(typeof(Walkthroughs)) : null,
                Location = name is "Left sidebar" or "Sidebar header" ? BlockLocation.SidebarLeft
                    : name == "Not found" ? BlockLocation.Component : name == walkthroughs ? BlockLocation.Content : BlockLocation.Footer,
                Title = name is "Footer" or "Sidebar header" or "Not found" ? null : name,
                Body = name == "Not found" ? "<h1>Page not found</h1>\n\n<p>The page you are looking for does not exist.</p>\n\n<a href=\"/\">Back to homepage</a>" : name == "Footer" ? @"<img src='footer.png' style='position: absolute; bottom: 0; left: 0; height: 3px; width: 100%;' />
                    <img src='leaf.png' style='position: absolute; bottom: 0; left: 0;' />
                    <img src='factory.png' style='position: absolute; bottom: 0; right: 0;' />" : null,
                Enabled = true,
                PageVisibility = BlockPageVisibility.ShowOnListedPages,
                Preload = true,
                Pages = name == walkthroughs ? "/" : null,
                LocationOrder = name == "Sidebar header" || name == walkthroughs ? -10 : 0,
                Creator = creator,
                CreationTime = now,
                BlockGroups = name == walkthroughs ? [catalogGroup!] : groups
            };
            var replacements = ids.Length == 0 ? names.Select(Default).ToArray() : [.. selected.Select(b =>
            {
                var replacement = Default(b.Name!);
                replacement.Id = b.Id;
                replacement.Updated = now;
                replacement.ConcurrencyStamp = Guid.NewGuid().ToString();
                return replacement;
            })];
            await using (var transaction = await connection.BeginTransactionAsync(cancellationToken))
            {
                if (insertCatalogGroup)
                {
                    await connection.InsertAsync(transaction, GXInsertArgs.Insert(catalogGroup!), cancellationToken);
                }
                foreach (var block in replacements)
                {
                    if (ids.Length == 0)
                    {
                        var insert = GXInsertArgs.Insert(block);
                        insert.Exclude<GXBlock>(b => b.BlockGroups!);
                        await connection.InsertAsync(transaction, insert, cancellationToken);
                        AddBlockToBlockGroups(connection, block.Id, block.BlockGroups!, transaction);
                    }
                    else
                    {
                        var update = GXUpdateArgs.Update(block);
                        update.Exclude<GXBlock>(b => new { b.Creator, b.CreationTime, b.BlockGroups, b.User, b.Parent });
                        await connection.UpdateAsync(transaction, update, cancellationToken);
                    }
                }
                await transaction.CommitAsync(cancellationToken);
            }
            foreach (var block in replacements)
            {
                _notificationCache.Add(await GetNotifiedUsers(block.Id, NotificationAction.Edit, connection), new GXTarget(GXBlockPolicies.Edit, block.Id, block.Name));
            }
        }
    }
}
