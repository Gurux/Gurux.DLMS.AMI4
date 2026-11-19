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

using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Security.Claims;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <inheritdoc />
public sealed class RawNotificationRepository : IRawNotificationRepository
{
    // Avoid local read-to-write deadlocks; serializable transactions also protect other server processes.
    /// <summary>Serializes mutation operations across callers.</summary>
    private static readonly SemaphoreSlim _mutationGate = new(1, 1);
    /// <summary>Creates database connections used by this operation.</summary>
    private readonly IDbConnectionFactory _factory;
    /// <summary>Principal whose identity and claims govern repository access.</summary>
    private readonly ClaimsPrincipal _user;
    /// <summary>Evaluates policies before accessing protected data.</summary>
    private readonly IAuthorizationService _authorization;
    /// <summary>Publishes change notifications after successful mutations.</summary>
    private readonly NotificationCache _notifications;

    /// <summary>Creates a repository scoped to the authenticated request user.</summary>
    public RawNotificationRepository(IDbConnectionFactory dbFactory, IHttpContextAccessor context,
        IAuthorizationService authorizationService, NotificationCache notificationCache)
    {
        _factory = dbFactory;
        _user = context.HttpContext?.User ?? throw new UnauthorizedAccessException();
        if (_user.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException();
        }
        _authorization = authorizationService;
        _notifications = notificationCache;
    }

    /// <summary>Builds an active-agent query constrained by current group memberships unless administrative all-user access is allowed.</summary>
    /// <param name="id">Optional agent identity that further limits the query.</param>
    /// <param name="allUsers">Whether administrators may select all agents instead of their own membership scope.</param>
    /// <returns>An agent query restricted to the requested identity and authorized scope.</returns>
    private GXSelectArgs Agents(Guid? id = null, bool allUsers = true)
    {
        var query = GXSelectArgs.Select<GXAgent>(a => a.Id, a => a.Removed == null);
        if (!allUsers || !_user.IsInRole(GXRoles.Admin))
        {
            var userId = _user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();
            // Every link must still be present: soft-deleted memberships cannot grant access.
            var members = GXSelectArgs.Select<GXUserGroupUser>(m => GXSql.One,
                m => m.UserId == userId && m.Removed == null);
            var users = GXSelectArgs.Select<GXUserGroup>(g => GXSql.One, g => g.Removed == null &&
                GXSql.Exists<GXUserGroup, GXUserGroupUser>(g => g.Id, m => m.UserGroupId, members));
            var links = GXSelectArgs.Select<GXUserGroupAgentGroup>(m => GXSql.One, m => m.Removed == null &&
                GXSql.Exists<GXUserGroupAgentGroup, GXUserGroup>(m => m.UserGroupId, g => g.Id, users));
            var groups = GXSelectArgs.Select<GXAgentGroup>(g => GXSql.One, g => g.Removed == null &&
                GXSql.Exists<GXAgentGroup, GXUserGroupAgentGroup>(g => g.Id, m => m.AgentGroupId, links));
            var agents = GXSelectArgs.Select<GXAgentGroupAgent>(m => GXSql.One, m => m.Removed == null &&
                GXSql.Exists<GXAgentGroupAgent, GXAgentGroup>(m => m.AgentGroupId, g => g.Id, groups));
            query.Where.And<GXAgent>(a => GXSql.Exists<GXAgent, GXAgentGroupAgent>(a => a.Id, m => m.AgentId, agents));
        }
        if (id.HasValue)
        {
            query.Where.And<GXAgent>(a => a.Id == id.Value);
        }
        return query;
    }

    /// <summary>Builds a raw notification query limited to agents visible in the requested user scope.</summary>
    /// <param name="allUsers">Whether administrators may select all agents instead of their own membership scope.</param>
    /// <returns>The authorized notification query.</returns>
    private GXSelectArgs Query(bool allUsers = true)
    {
        var agents = Agents(allUsers: allUsers);
        return GXSelectArgs.SelectAll<GXRawNotification>(n =>
            GXSql.Exists<GXRawNotification, GXAgent>(n => n.Agent!, a => a.Id, agents));
    }

    /// <summary>Validates that an agent identifier selects an enabled, accessible agent inside the supplied transaction.</summary>
    /// <param name="db">Database connection used by the history or registry operation.</param>
    /// <param name="tx">Transaction shared by the query or mutation.</param>
    /// <param name="id">Nonempty agent identity that must remain enabled and accessible.</param>
    /// <param name="ct">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>A task that completes when the agent is eligible for notification processing.</returns>
    /// <exception cref="ArgumentException">The agent identifier is empty.</exception>
    /// <exception cref="UnauthorizedAccessException">The agent is disabled, missing, or inaccessible.</exception>
    private async Task AgentAsync(GXDbConnection db, DbTransaction tx, Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Agent identifier is required.");
        }
        var query = Agents(id);
        query.Where.And<GXAgent>(a => a.Enabled == true);
        if (await db.SingleOrDefaultAsync<GXAgent>(tx, query, ct) == null)
        {
            throw new UnauthorizedAccessException("Agent is inactive or inaccessible.");
        }
    }

    /// <summary>Authorizes and serializes a raw-notification mutation inside a committed serializable database transaction.</summary>
    /// <typeparam name="T">Result produced by the supplied callback.</typeparam>
    /// <param name="policy">Authorization policy to evaluate or include in an invalidation notification.</param>
    /// <param name="action">Mutation receiving the opened connection and serializable transaction.</param>
    /// <param name="ct">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>The result returned by the committed mutation.</returns>
    private async Task<T> MutateAsync<T>(string policy,
        Func<GXDbConnection, DbTransaction, Task<T>> action, CancellationToken ct)
    {
        await AuthorizeAsync(policy);
        await _mutationGate.WaitAsync(ct);
        try
        {
            await using var db = await _factory.GetHistoryConnectionAsync(ct);
            await using var tx = await db.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var result = await action(db, tx);
            await tx.CommitAsync(ct);
            return result;
        }
        finally
        {
            _mutationGate.Release();
        }
    }

    /// <summary>Rejects raw-notification access unless the current principal satisfies the selected policy.</summary>
    /// <param name="policy">Authorization policy to evaluate or include in an invalidation notification.</param>
    /// <returns>A task that completes when authorization succeeds.</returns>
    /// <exception cref="UnauthorizedAccessException">The current principal does not satisfy the selected policy.</exception>
    private async Task AuthorizeAsync(string policy)
    {
        if (!(await _authorization.AuthorizeAsync(_user, policy)).Succeeded)
        {
            throw new UnauthorizedAccessException();
        }
    }

    /// <summary>Computes a UTC lease expiration with millisecond precision and rejects submillisecond durations.</summary>
    /// <param name="duration">Lease lifetime of at least one millisecond.</param>
    /// <returns>The lease expiration timestamp rounded to Unix millisecond precision.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The lease duration is shorter than one millisecond.</exception>
    private static DateTimeOffset Expires(TimeSpan duration)
    {
        if (duration < TimeSpan.FromMilliseconds(1))
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Lease duration must be at least one millisecond.");
        }
        return DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.Add(duration).ToUnixTimeMilliseconds());
    }

    /// <summary>Checks whether a notification currently has a nonexpired processing lease.</summary>
    /// <param name="n">Notification chunk whose processing lease state is tested.</param>
    /// <param name="now">UTC timestamp against which lease expiration is evaluated.</param>
    /// <returns>True when status, token, and expiration describe an active lease.</returns>
    private static bool Active(GXRawNotification n, DateTimeOffset now) =>
        n.Status == RawNotificationStatus.Processing && n.LeaseToken.HasValue && n.LeaseExpires > now;

    /// <summary>Reads notification records and separately materializes receiving-agent and processing-agent identities.</summary>
    /// <param name="db">Database connection used by the history or registry operation.</param>
    /// <param name="tx">Transaction shared by the query or mutation.</param>
    /// <param name="query">ORM select arguments receiving the additional authorization or projection clauses.</param>
    /// <param name="ct">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>Notification records with both agent references populated correctly.</returns>
    private static async Task<List<GXRawNotification>> SelectAsync(GXDbConnection db, DbTransaction? tx,
        GXSelectArgs query, CancellationToken ct)
    {
        // Read the two GXAgent roles separately so each join populates its own navigation property.
        query.Joins.AddLeftJoin<GXRawNotification, GXAgent>(n => n.Agent!, a => a.Id);
        query.Columns.Add<GXAgent>(a => a.Id);
        query.Columns.Exclude<GXRawNotification>(n => n.Processor!);
        var rows = tx == null ? await db.SelectAsync<GXRawNotification>(query, ct)
            : await db.SelectAsync<GXRawNotification>(tx, query, ct);
        if (rows.Count == 0)
        {
            return rows;
        }
        var ids = rows.Select(n => n.Id).ToArray();
        var processorsQuery = GXSelectArgs.Select<GXRawNotification>(n => new { n.Id, n.Processor }, n => ids.Contains(n.Id));
        processorsQuery.Joins.AddLeftJoin<GXRawNotification, GXAgent>(n => n.Processor!, a => a.Id);
        processorsQuery.Columns.Add<GXAgent>(a => a.Id);
        var processors = tx == null ? await db.SelectAsync<GXRawNotification>(processorsQuery, ct)
            : await db.SelectAsync<GXRawNotification>(tx, processorsQuery, ct);
        var map = processors.ToDictionary(n => n.Id);
        foreach (var row in rows)
        {
            row.Processor = map[row.Id].Processor;
        }
        return rows;
    }

    /// <summary>Reads every visible notification chunk for the selected agent connection in sequence-number order.</summary>
    /// <param name="db">Database connection used by the history or registry operation.</param>
    /// <param name="tx">Transaction shared by the query or mutation.</param>
    /// <param name="record">Notification chunk supplying connection identity or processing state.</param>
    /// <param name="ct">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>The ordered chunks belonging to the selected connection.</returns>
    private async Task<List<GXRawNotification>> ConnectionAsync(GXDbConnection db, DbTransaction tx,
        GXRawNotification record, CancellationToken ct)
    {
        var query = Query();
        var agent = new GXAgent { Id = record.Agent!.Id };
        query.Where.And<GXRawNotification>(n => n.Agent == agent && n.ConnectionId == record.ConnectionId);
        query.OrderBy.Add<GXRawNotification>(n => n.SequenceNumber);
        return await SelectAsync(db, tx, query, ct);
    }

    /// <summary>Validates processor ownership and expiration of a lease, then reads the complete leased connection.</summary>
    /// <param name="db">Database connection used by the history or registry operation.</param>
    /// <param name="tx">Transaction shared by the query or mutation.</param>
    /// <param name="processorId">Agent identity that must own the active processing lease.</param>
    /// <param name="token">Nonempty lease token that must belong to the processor.</param>
    /// <param name="ct">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>All chunks belonging to the valid leased connection.</returns>
    /// <exception cref="ArgumentException">The lease token is empty.</exception>
    /// <exception cref="InvalidOperationException">The lease is absent, expired, or owned by another processor.</exception>
    private async Task<List<GXRawNotification>> LeaseAsync(GXDbConnection db, DbTransaction tx,
        Guid processorId, Guid token, CancellationToken ct)
    {
        await AgentAsync(db, tx, processorId, ct);
        if (token == Guid.Empty)
        {
            throw new ArgumentException("Lease token is required.");
        }
        var query = Query();
        query.Where.And<GXRawNotification>(n => n.LeaseToken == token);
        var records = await SelectAsync(db, tx, query, ct);
        var now = DateTimeOffset.UtcNow;
        if (records.Count == 0 || records.Any(n => n.Processor?.Id != processorId || !Active(n, now)))
        {
            throw new InvalidOperationException("The connection lease is missing, expired or owned by another processor.");
        }
        return await ConnectionAsync(db, tx, records[0], ct);
    }

    /// <summary>Persists a notification's processing status, lease ownership, retry counters, timestamps, and error state.</summary>
    /// <param name="db">Database connection used by the history or registry operation.</param>
    /// <param name="tx">Transaction shared by the query or mutation.</param>
    /// <param name="record">Notification chunk containing the processing and lease fields to update.</param>
    /// <param name="ct">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>The number of notification rows updated.</returns>
    private static Task<int> SaveAsync(GXDbConnection db, DbTransaction tx, GXRawNotification record, CancellationToken ct) =>
        db.UpdateAsync(tx, GXUpdateArgs.Update(record, n => new {
            n.Status, n.Processor, n.LeaseToken, n.LeaseExpires, n.Attempts, n.NextAttempt, n.Processed, n.Error }), ct);

    /// <inheritdoc />
    public async Task<IEnumerable<Guid>> AddAsync(IEnumerable<GXRawNotification> notifications, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notifications);
        var input = notifications.ToArray();
        if (input.Length == 0)
        {
            throw new ArgumentException("Notifications are required.");
        }
        var (Ids, Deliveries) = await MutateAsync(GXRawNotificationPolicies.Add, async (db, tx) =>
        {
            var ids = new List<Guid>();
            var added = new List<GXRawNotification>();
            foreach (var item in input)
            {
                if (item == null || item.Id == Guid.Empty || item.Agent == null || item.ConnectionId == Guid.Empty ||
                    item.SequenceNumber < 0 || item.Received == null || (!item.ConnectionClosed && (item.Value == null || item.Value.Length == 0)))
                {
                    throw new ArgumentException("Each chunk requires an identifier, receiving agent, connection, sequence, timestamp and data (unless closing).");
                }
                await AgentAsync(db, tx, item.Agent.Id, cancellationToken);
                // Check globally: an inaccessible duplicate must never overwrite another user's data.
                var existing = (await SelectAsync(db, tx,
                    GXSelectArgs.SelectAll<GXRawNotification>(n => n.Id == item.Id), cancellationToken)).SingleOrDefault();
                if (existing != null)
                {
                    if (existing.Agent?.Id != item.Agent.Id || existing.ConnectionId != item.ConnectionId ||
                        existing.SequenceNumber != item.SequenceNumber || existing.Received?.ToUnixTimeMilliseconds() != item.Received.Value.ToUnixTimeMilliseconds() ||
                        existing.Interface != item.Interface || existing.ConnectionClosed != item.ConnectionClosed ||
                        !BytesEqual(existing.Sender, item.Sender) || !BytesEqual(existing.Value, item.Value))
                    {
                        throw new ArgumentException("Notification identifier already exists with different content.");
                    }
                    ids.Add(existing.Id);
                    continue;
                }
                var connection = await ConnectionAsync(db, tx, item, cancellationToken);
                // Enforce transport ordering at ingestion so a parser never sees a later chunk before an earlier one.
                if (connection.Any(n => n.ConnectionClosed || n.Status == RawNotificationStatus.Completed) ||
                    item.SequenceNumber != (connection.Count == 0 ? 0 : connection[^1].SequenceNumber + 1))
                {
                    throw new ArgumentException("Chunks must arrive in sequence starting at zero; a closed connection cannot receive more data.");
                }
                var failed = connection.FirstOrDefault(n => n.Status == RawNotificationStatus.Failed);
                var record = new GXRawNotification {
                    Id = item.Id, Agent = new GXAgent { Id = item.Agent.Id }, Sender = item.Sender,
                    Received = DateTimeOffset.FromUnixTimeMilliseconds(item.Received.Value.ToUnixTimeMilliseconds()), Value = item.Value, ConnectionId = item.ConnectionId,
                    SequenceNumber = item.SequenceNumber, ConnectionClosed = item.ConnectionClosed, Interface = item.Interface,
                    Status = failed == null ? RawNotificationStatus.Pending : RawNotificationStatus.Failed,
                    NextAttempt = failed?.NextAttempt, Error = failed?.Error
                };
                await db.InsertAsync(tx, GXInsertArgs.Insert(record), cancellationToken);
                ids.Add(record.Id);
                added.Add(record);
            }
            // Resolve recipients inside the write transaction, but publish only after it commits.
            // An identical retry is not in added and must not wake parsers a second time.
            var deliveries = new List<(List<string> Users, GXTarget Target)>();
            foreach (var group in added.GroupBy(n => n.Agent!.Id))
            {
                var users = await GXRepositoryUsers.GetNotifiedUsersAsync(_factory, typeof(GXAgent), group.Key,
                    NotificationAction.Add, GXRawNotificationPolicies.View, db, tx, this._notifications);
                foreach (var record in group)
                {
                    deliveries.Add((users, new GXTarget(GXRawNotificationPolicies.Add, record.Id)));
                }
            }
            return (Ids: ids, Deliveries: deliveries);
        }, cancellationToken);
        foreach (var (Users, Target) in Deliveries)
        {
            this._notifications.Add(Users, Target);
        }

        return Ids;
    }

    /// <summary>Compares nullable byte sequences by content.</summary>
    /// <param name="a">First byte sequence to compare.</param>
    /// <param name="b">Second byte sequence to compare.</param>
    /// <returns>True when both sequences are null or contain the same bytes.</returns>
    private static bool BytesEqual(byte[]? a, byte[]? b) => a == null ? b == null : b != null && a.AsSpan().SequenceEqual(b);

    /// <inheritdoc />
    public Task<IEnumerable<GXRawNotification>> GetNextAsync(Guid processorId, TimeSpan leaseDuration,
        Guid? receivingAgentId = null, CancellationToken cancellationToken = default) =>
        MutateAsync<IEnumerable<GXRawNotification>>(GXRawNotificationPolicies.Edit, async (db, tx) =>
        {
            _ = Expires(leaseDuration);
            await AgentAsync(db, tx, processorId, cancellationToken);
            if (receivingAgentId.HasValue)
            {
                await AgentAsync(db, tx, receivingAgentId.Value, cancellationToken);
            }
            var query = Query();
            query.Where.And<GXRawNotification>(n => n.Status != RawNotificationStatus.Completed);
            if (receivingAgentId.HasValue)
            {
                var receiver = new GXAgent { Id = receivingAgentId.Value };
                query.Where.And<GXRawNotification>(n => n.Agent == receiver);
            }
            // Scan metadata only; payloads are loaded only for the selected connection.
            query.Columns.Exclude<GXRawNotification>(n => new { n.Value, n.Sender });
            var candidates = await SelectAsync(db, tx, query, cancellationToken);
            var now = DateTimeOffset.UtcNow;
            var selected = candidates.GroupBy(n => (n.Agent!.Id, n.ConnectionId))
                .Where(g => g.All(n => !Active(n, now) && (n.Status != RawNotificationStatus.Failed || n.NextAttempt <= now)))
                .OrderBy(g => g.Min(n => n.Received)).ThenBy(g => g.Key.Id).ThenBy(g => g.Key.ConnectionId).FirstOrDefault();
            if (selected == null)
            {
                return [];
            }
            var chunks = await ConnectionAsync(db, tx, selected.First(), cancellationToken);
            var token = Guid.NewGuid();
            var expiration = Expires(leaseDuration);
            foreach (var chunk in chunks)
            {
                chunk.Status = RawNotificationStatus.Processing;
                chunk.Processor = new GXAgent { Id = processorId };
                chunk.LeaseToken = token;
                chunk.LeaseExpires = expiration;
                chunk.NextAttempt = null;
                chunk.Attempts = checked(chunk.Attempts + 1);
                await SaveAsync(db, tx, chunk, cancellationToken);
            }
            return chunks;
        }, cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<GXRawNotification>> GetLeasedAsync(Guid processorId, Guid leaseToken, CancellationToken cancellationToken = default) =>
        MutateAsync<IEnumerable<GXRawNotification>>(GXRawNotificationPolicies.Edit, async (db, tx) =>
        {
            var chunks = await LeaseAsync(db, tx, processorId, leaseToken, cancellationToken);
            var owner = chunks.First(n => n.LeaseToken == leaseToken);
            foreach (var chunk in chunks.Where(n => n.LeaseToken != leaseToken))
            {
                chunk.Status = RawNotificationStatus.Processing;
                chunk.Processor = new GXAgent { Id = processorId };
                chunk.LeaseToken = leaseToken;
                chunk.LeaseExpires = owner.LeaseExpires;
                chunk.Attempts = checked(chunk.Attempts + 1);
                await SaveAsync(db, tx, chunk, cancellationToken);
            }
            return chunks;
        }, cancellationToken);

    /// <inheritdoc />
    public Task<DateTimeOffset> RenewLeaseAsync(Guid processorId, Guid leaseToken, TimeSpan leaseDuration, CancellationToken cancellationToken = default) =>
        MutateAsync(GXRawNotificationPolicies.Edit, async (db, tx) =>
        {
            var expiration = Expires(leaseDuration);
            var chunks = await LeaseAsync(db, tx, processorId, leaseToken, cancellationToken);
            var currentExpiration = chunks.First(n => n.LeaseToken == leaseToken).LeaseExpires!.Value;
            if (expiration < currentExpiration)
            {
                expiration = currentExpiration;
            }
            foreach (var chunk in chunks.Where(n => n.LeaseToken == leaseToken))
            {
                chunk.LeaseExpires = expiration;
                await SaveAsync(db, tx, chunk, cancellationToken);
            }
            return expiration;
        }, cancellationToken);

    /// <inheritdoc />
    public Task DoneAsync(Guid processorId, Guid leaseToken, CancellationToken cancellationToken = default) =>
        MutateAsync(GXRawNotificationPolicies.Edit, async (db, tx) =>
        {
            var chunks = await LeaseAsync(db, tx, processorId, leaseToken, cancellationToken);
            if (!chunks[^1].ConnectionClosed || chunks.Any(n => n.LeaseToken != leaseToken))
            {
                throw new InvalidOperationException("Read all chunks including the connection-end record before completing the connection.");
            }
            foreach (var chunk in chunks)
            {
                chunk.Status = RawNotificationStatus.Completed;
                chunk.Processed = DateTimeOffset.UtcNow;
                chunk.Error = null;
                Release(chunk);
                await SaveAsync(db, tx, chunk, cancellationToken);
            }
            return true;
        }, cancellationToken);

    /// <inheritdoc />
    public Task FailAsync(Guid processorId, Guid leaseToken, string error, DateTimeOffset? nextAttempt = null, CancellationToken cancellationToken = default) =>
        MutateAsync(GXRawNotificationPolicies.Edit, async (db, tx) =>
        {
            if (string.IsNullOrWhiteSpace(error))
            {
                throw new ArgumentException("An error description is required.");
            }
            var chunks = await LeaseAsync(db, tx, processorId, leaseToken, cancellationToken);
            foreach (var chunk in chunks)
            {
                chunk.Status = RawNotificationStatus.Failed;
                chunk.Error = error;
                chunk.NextAttempt = nextAttempt?.ToUniversalTime();
                Release(chunk);
                await SaveAsync(db, tx, chunk, cancellationToken);
            }
            return true;
        }, cancellationToken);

    /// <summary>Clears processing ownership and lease metadata, retaining retry scheduling only for failed chunks.</summary>
    /// <param name="chunk">Notification chunk whose processing lease metadata is released.</param>
    private static void Release(GXRawNotification chunk)
    { chunk.Processor = null; chunk.LeaseToken = null; chunk.LeaseExpires = null; chunk.NextAttempt = chunk.Status == RawNotificationStatus.Failed ? chunk.NextAttempt : null; }

    /// <summary>Expands selected visible notifications to complete connections and rejects connections with active leases.</summary>
    /// <param name="db">Database connection used by the history or registry operation.</param>
    /// <param name="tx">Transaction shared by the query or mutation.</param>
    /// <param name="notifications">Notification identities expanded to their complete agent connections.</param>
    /// <param name="ct">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>All chunks in the selected, currently unleased connections.</returns>
    /// <exception cref="ArgumentException">No valid notification identities were supplied.</exception>
    /// <exception cref="UnauthorizedAccessException">A selected notification is absent or inaccessible.</exception>
    /// <exception cref="InvalidOperationException">A selected connection still has an active lease.</exception>
    private async Task<List<GXRawNotification>> SelectedConnectionsAsync(GXDbConnection db, DbTransaction tx,
        IEnumerable<Guid> notifications, CancellationToken ct)
    {
        var ids = notifications.Distinct().ToArray();
        if (ids.Length == 0 || ids.Contains(Guid.Empty))
        {
            throw new ArgumentException("Notification identifiers are required.");
        }
        var query = Query();
        query.Where.And<GXRawNotification>(n => ids.Contains(n.Id));
        var selected = await SelectAsync(db, tx, query, ct);
        if (selected.Count != ids.Length)
        {
            throw new UnauthorizedAccessException("A selected notification is missing or inaccessible.");
        }
        var result = new List<GXRawNotification>();
        foreach (var group in selected.GroupBy(n => (n.Agent!.Id, n.ConnectionId)))
        {
            result.AddRange(await ConnectionAsync(db, tx, group.First(), ct));
        }

        if (result.Any(n => Active(n, DateTimeOffset.UtcNow)))
        {
            throw new InvalidOperationException("The connection has an active lease.");
        }
        return result;
    }

    /// <inheritdoc />
    public Task RestartAsync(IEnumerable<Guid> notifications, CancellationToken cancellationToken = default) =>
        MutateAsync(GXRawNotificationPolicies.Edit, async (db, tx) =>
        {
            ArgumentNullException.ThrowIfNull(notifications);
            var chunks = await SelectedConnectionsAsync(db, tx, notifications, cancellationToken);
            if (chunks.GroupBy(n => (n.Agent!.Id, n.ConnectionId)).Any(g =>
                g.Any(n => n.Status == RawNotificationStatus.Completed) ||
                !g.Any(n => n.Status == RawNotificationStatus.Failed || n.Status == RawNotificationStatus.Processing)))
            {
                throw new InvalidOperationException("Only failed or expired connections can be restarted.");
            }
            foreach (var chunk in chunks)
            {
                chunk.Status = RawNotificationStatus.Pending;
                Release(chunk);
                await SaveAsync(db, tx, chunk, cancellationToken);
            }
            return true;
        }, cancellationToken);

    /// <inheritdoc />
    public Task DeleteAsync(IEnumerable<Guid> notifications, CancellationToken cancellationToken = default) =>
        MutateAsync(GXRawNotificationPolicies.Delete, async (db, tx) =>
        {
            ArgumentNullException.ThrowIfNull(notifications);
            foreach (var chunk in await SelectedConnectionsAsync(db, tx, notifications, cancellationToken))
            {
                await db.DeleteAsync(tx, GXDeleteArgs.Delete<GXRawNotification>(n => n.Id == chunk.Id), cancellationToken);
            }

            return true;
        }, cancellationToken);

    /// <inheritdoc />
    public Task ClearAsync(DateTimeOffset processedBefore, CancellationToken cancellationToken = default) =>
        MutateAsync(GXRawNotificationPolicies.Clear, async (db, tx) =>
        {
            if (processedBefore == default)
            {
                throw new ArgumentException("A retention cutoff is required.");
            }
            var query = Query();
            query.Where.And<GXRawNotification>(n => n.Status == RawNotificationStatus.Completed && n.Processed < processedBefore);
            var chunks = await SelectAsync(db, tx, query, cancellationToken);
            foreach (var group in chunks.GroupBy(n => (n.Agent!.Id, n.ConnectionId)))
            {
                var connection = await ConnectionAsync(db, tx, group.First(), cancellationToken);
                if (connection.All(n => n.Status == RawNotificationStatus.Completed && n.Processed < processedBefore))
                {
                    foreach (var chunk in connection)
                    {
                        await db.DeleteAsync(tx, GXDeleteArgs.Delete<GXRawNotification>(n => n.Id == chunk.Id), cancellationToken);
                    }
                }
            }
            return true;
        }, cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<GXRawNotification>> ListAsync(ListRawNotifications? request = null,
        ListRawNotificationsResponse? response = null, CancellationToken cancellationToken = default)
    {
        await AuthorizeAsync(GXRawNotificationPolicies.View);
        if (request?.Index < 0 || request?.Count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request));
        }
        var query = Query(request == null || request.AllUsers);
        if (request?.Filter != null)
        {
            query.Where.FilterBy(request.Filter);
        }
        if (request?.Included?.Any() == true)
        {
            query.Where.And<GXRawNotification>(n => request.Included.Contains(n.Id));
        }
        if (request?.Exclude?.Any() == true)
        {
            query.Where.And<GXRawNotification>(n => !request.Exclude.Contains(n.Id));
        }
        GXDateTimeFilters.Apply<GXRawNotification>(query, request?.DateFilters);
        await using var db = await _factory.GetHistoryConnectionAsync(cancellationToken);
        await using var tx = await db.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        if (response != null)
        {
            var count = GXSelectArgs.Select<GXRawNotification>(n => GXSql.Count(1));
            count.Where.Append(query.Where);
            response.Count = await db.SingleOrDefaultAsync<long>(tx, count, cancellationToken);
        }
        query.Index = (request?.Index ?? 0);
        query.Count = (request?.Count ?? 0);
        query.Descending = request?.Descending ?? false;
        if (!string.IsNullOrEmpty(request?.OrderBy))
        {
            query.OrderBy.Add<GXRawNotification>(request.OrderBy);
        }
        else
        {
            query.OrderBy.Add<GXRawNotification>(n => n.Received!);
        }

        query.OrderBy.Add<GXRawNotification>(n => n.Id);
        var rows = await SelectAsync(db, tx, query, cancellationToken);
        await tx.CommitAsync(cancellationToken);
        response?.RawNotifications = rows;
        return rows;
    }

    /// <inheritdoc />
    public async Task<GXRawNotification> ReadAsync(Guid id, Expression<Func<GXRawNotification, object>>? columns = null,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeAsync(GXRawNotificationPolicies.View);
        var query = Query();
        query.Where.And<GXRawNotification>(n => n.Id == id);
        if (columns != null) { query.Columns.Clear(); query.Columns.Add(columns); query.Columns.Add<GXRawNotification>(n => n.Id); }
        await using var db = await _factory.GetHistoryConnectionAsync(cancellationToken);
        await using var tx = await db.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var row = (await SelectAsync(db, tx, query, cancellationToken)).SingleOrDefault()
            ?? throw new KeyNotFoundException("Raw notification not found.");
        await tx.CommitAsync(cancellationToken);
        return row;
    }
}
