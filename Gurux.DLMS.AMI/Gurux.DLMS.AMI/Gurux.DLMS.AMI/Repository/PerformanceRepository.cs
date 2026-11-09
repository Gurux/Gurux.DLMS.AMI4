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

using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    /// <summary>
    /// Constructor.
    /// </summary>
    public class PerformanceRepository : IPerformanceRepository
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly NotificationCache _notificationCache;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>Creates a repository for performance snapshots.</summary>
        public PerformanceRepository(IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider,
            NotificationCache notificationCache)
        {
            _ = httpContextAccessor;
            _dbFactory = dbFactory;
            _notificationCache = notificationCache;
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        public Dictionary<string, GXPerformance> Snapshots { get; private set; } = [];

        /// <summary>Reads administrator recipients without changing the active request context.</summary>
        private static async Task<List<string>> GetNotifiedUsersAsync(GXDbConnection connection, CancellationToken cancellationToken)
        {
            var recipients = GXSelectArgs.Select<GXUser>(u => u.Id!, u => u.Removed == null);
            recipients.Joins.AddInnerJoin<GXUser, GXUserRole>(u => u.Id!, r => r.UserId!);
            recipients.Joins.AddInnerJoin<GXUserRole, GXRole>(r => r.RoleId!, r => r.Id);
            recipients.Where.And<GXRole>(r => r.Name == GXRoles.Admin);
            recipients.Distinct = true;
            return [.. (await connection.SelectAsync<GXUser>(recipients, cancellationToken)).Select(u => u.Id!)];
        }

        /// <inheritdoc />
        public async Task ClearAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
        {
            if (user?.IsInRole(GXRoles.Admin) != true)
            {
                throw new UnauthorizedAccessException();
            }
            lock (Snapshots)
            {
                var now = DateTimeOffset.Now;
                foreach (var target in Snapshots.Keys.ToArray())
                {
                    Snapshots[target] = new GXPerformance
                    {
                        Target = target,
                        Start = now,
                        AddCount = 0, ReadCount = 0, UpdateCount = 0,
                        DeleteCount = 0, ListCount = 0, ClearCount = 0,
                        AddTime = 0, ReadTime = 0, UpdateTime = 0,
                        DeleteTime = 0, ListTime = 0, ClearTime = 0,
                        AddNotificationTime = 0, UpdateNotificationTime = 0,
                        DeleteNotificationTime = 0, ClearNotificationTime = 0
                    };
                }
            }
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            connection.Truncate<GXPerformance>();
            var users = await GetNotifiedUsersAsync(connection, cancellationToken);
            _notificationCache.Add(users, new GXTarget(GXPerformancePolicies.Clear));
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXPerformance>> ListAsync(ClaimsPrincipal user,
            ListPerformances? request,
            ListPerformancesResponse? response,
            CancellationToken cancellationToken = default)
        {
            if (user?.IsInRole(GXRoles.Admin) != true)
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXPerformance>();
            arg.Distinct = true;
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
            }
            var dateFiltered = GXDateTimeFilters.Apply<GXPerformance>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            List<GXPerformance> snapshots;
            lock (Snapshots)
            {
                snapshots = [.. Snapshots.Values.Where(s =>
                    (string.IsNullOrEmpty(request?.Filter?.Target) || s.Target == request.Filter.Target) &&
                    (request?.Filter == null || request.Filter.Id == Guid.Empty || s.Id == request.Filter.Id))];
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXPerformance>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXPerformance>(q => q.Start!);
            }
            if (dateFiltered) {
                snapshots.Clear();
            }
            snapshots = [.. (arg.Descending ? snapshots.OrderByDescending(s => s.Start) : snapshots.OrderBy(s => s.Start))];
            var total = GXSelectArgs.Select<GXPerformance>(q => GXSql.Count(1));
            total.Where.Append(arg.Where);
            long storedCount = await connection.SingleOrDefaultAsync<long>(total, cancellationToken);
            long index = Math.Max(0, request?.Index ?? 0);
            long count = request?.Count ?? 0;
            bool paged = count > 0;
            long snapshotOffset = arg.Descending ? 0 : storedCount;
            var visibleSnapshots = snapshots.Skip((int)Math.Min(snapshots.Count, Math.Max(0, index - snapshotOffset)));
            if (paged)
            {
                visibleSnapshots = visibleSnapshots.Take((int)Math.Min(snapshots.Count, Math.Max(0, count - Math.Max(0, snapshotOffset - index))));
            }
            var selectedSnapshots = visibleSnapshots.ToList();
            long storedIndex = arg.Descending ? Math.Max(0, index - snapshots.Count) : index;
            long storedTake = paged ? Math.Max(0, count - selectedSnapshots.Count) : storedCount;
            var performances = new List<GXPerformance>();
            if (storedIndex < storedCount && storedTake > 0)
            {
                arg.Index = storedIndex;
                if (paged)
                {
                    arg.Count = storedTake;
                }

                performances = await connection.SelectAsync<GXPerformance>(arg, cancellationToken);
            }
            if (arg.Descending)
            {
                performances.InsertRange(0, selectedSnapshots);
            }
            else
            {
                performances.AddRange(selectedSnapshots);
            }
            if (response != null)
            {
                response.Count = storedCount + snapshots.Count;
                response.Performances = [.. performances];
            }
            return [.. performances];
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> AddAsync(ClaimsPrincipal user,
            IEnumerable<GXPerformance> performances, CancellationToken cancellationToken)
        {
            if (user?.IsInRole(GXRoles.Admin) != true)
            {
                throw new UnauthorizedAccessException();
            }
            List<Guid> list = [];
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            foreach (GXPerformance it in performances)
            {
                // A prior attempt may have committed this snapshot before recipient lookup failed.
                if (it.Id == Guid.Empty) {
                    it.Id = Guid.NewGuid();
                }
                var existing = await connection.SingleOrDefaultAsync<GXPerformance>(
                    GXSelectArgs.Select<GXPerformance>(p => p.Id, p => p.Id == it.Id), cancellationToken);
                if (existing == null) {
                    await connection.InsertAsync(GXInsertArgs.Insert(it), cancellationToken);
                }
                list.Add(it.Id);
            }
            var users = await GetNotifiedUsersAsync(connection, cancellationToken);
            _notificationCache.Add(users, new GXTarget(GXPerformancePolicies.Add));
            return [.. list];
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal user,
            IEnumerable<Guid> performances, CancellationToken cancellationToken)
        {
            try
            {

                if (user?.IsInRole(GXRoles.Admin) != true)
                {
                    throw new UnauthorizedAccessException();
                }
                GXSelectArgs arg = GXSelectArgs.Select<GXPerformance>(a => a.Id, q => performances.Contains(q.Id));
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                List<GXPerformance> list = connection.Select<GXPerformance>(arg);
                DateTime now = DateTime.Now;
                foreach (GXPerformance it in list)
                {
                    await connection.DeleteAsync(GXDeleteArgs.DeleteById<GXPerformance>(it.Id), cancellationToken);
                }
                var users = await GetNotifiedUsersAsync(connection, cancellationToken);
                if (users.Count != 0)
                {
                    _notificationCache.Add(users, new GXTarget(GXPerformancePolicies.Delete));
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
                        await DeleteAsync(user, performances, linkedCts.Token);
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
    }
}
