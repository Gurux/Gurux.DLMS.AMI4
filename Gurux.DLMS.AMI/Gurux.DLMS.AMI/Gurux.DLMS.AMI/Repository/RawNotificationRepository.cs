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

using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Security.Claims;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
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
    private static readonly SemaphoreSlim MutationGate = new(1, 1);
    private readonly IDbConnectionFactory factory;
    private readonly ClaimsPrincipal user;
    private readonly IAuthorizationService authorization;

    /// <summary>Creates a repository scoped to the authenticated request user.</summary>
    public RawNotificationRepository(IDbConnectionFactory dbFactory, IHttpContextAccessor context,
        IAuthorizationService authorizationService)
    {
        factory = dbFactory;
        user = context.HttpContext?.User ?? throw new UnauthorizedAccessException();
        if (user.Identity?.IsAuthenticated != true) throw new UnauthorizedAccessException();
        authorization = authorizationService;
    }

    private GXSelectArgs Agents(Guid? id = null, bool allUsers = true)
    {
        var query = GXSelectArgs.Select<GXAgent>(a => a.Id, a => a.Removed == null);
        if (!allUsers || !user.IsInRole(GXRoles.Admin))
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();
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
        if (id.HasValue) query.Where.And<GXAgent>(a => a.Id == id.Value);
        return query;
    }

    private GXSelectArgs Query(bool allUsers = true)
    {
        var agents = Agents(allUsers: allUsers);
        return GXSelectArgs.SelectAll<GXRawNotification>(n =>
            GXSql.Exists<GXRawNotification, GXAgent>(n => n.Agent!, a => a.Id, agents));
    }

    private async Task AgentAsync(GXDbConnection db, DbTransaction tx, Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty) throw new ArgumentException("Agent identifier is required.");
        var query = Agents(id);
        query.Where.And<GXAgent>(a => a.Active == true);
        if (await db.SingleOrDefaultAsync<GXAgent>(tx, query, ct) == null)
            throw new UnauthorizedAccessException("Agent is inactive or inaccessible.");
    }

    private async Task<T> MutateAsync<T>(string policy,
        Func<GXDbConnection, DbTransaction, Task<T>> action, CancellationToken ct)
    {
        await AuthorizeAsync(policy);
        await MutationGate.WaitAsync(ct);
        try
        {
            await using var db = await factory.GetConnectionAsync(ct);
            await using var tx = await db.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var result = await action(db, tx);
            await tx.CommitAsync(ct);
            return result;
        }
        finally { MutationGate.Release(); }
    }

    private async Task AuthorizeAsync(string policy)
    {
        if (!(await authorization.AuthorizeAsync(user, policy)).Succeeded)
            throw new UnauthorizedAccessException();
    }

    private static DateTimeOffset Expires(TimeSpan duration)
    {
        if (duration < TimeSpan.FromMilliseconds(1))
            throw new ArgumentOutOfRangeException(nameof(duration), "Lease duration must be at least one millisecond.");
        return DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.Add(duration).ToUnixTimeMilliseconds());
    }

    private static bool Active(GXRawNotification n, DateTimeOffset now) =>
        n.Status == RawNotificationStatus.Processing && n.LeaseToken.HasValue && n.LeaseExpires > now;

    private static async Task<List<GXRawNotification>> SelectAsync(GXDbConnection db, DbTransaction? tx,
        GXSelectArgs query, CancellationToken ct)
    {
        // Read the two GXAgent roles separately so each join populates its own navigation property.
        query.Joins.AddLeftJoin<GXRawNotification, GXAgent>(n => n.Agent!, a => a.Id);
        query.Columns.Add<GXAgent>(a => a.Id);
        query.Columns.Exclude<GXRawNotification>(n => n.Processor!);
        var rows = tx == null ? await db.SelectAsync<GXRawNotification>(query, ct)
            : await db.SelectAsync<GXRawNotification>(tx, query, ct);
        if (rows.Count == 0) return rows;
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

    private async Task<List<GXRawNotification>> ConnectionAsync(GXDbConnection db, DbTransaction tx,
        GXRawNotification record, CancellationToken ct)
    {
        var query = Query();
        var agent = new GXAgent { Id = record.Agent!.Id };
        query.Where.And<GXRawNotification>(n => n.Agent == agent && n.ConnectionId == record.ConnectionId);
        query.OrderBy.Add<GXRawNotification>(n => n.SequenceNumber);
        return await SelectAsync(db, tx, query, ct);
    }

    private async Task<List<GXRawNotification>> LeaseAsync(GXDbConnection db, DbTransaction tx,
        Guid processorId, Guid token, CancellationToken ct)
    {
        await AgentAsync(db, tx, processorId, ct);
        if (token == Guid.Empty) throw new ArgumentException("Lease token is required.");
        var query = Query();
        query.Where.And<GXRawNotification>(n => n.LeaseToken == token);
        var records = await SelectAsync(db, tx, query, ct);
        var now = DateTimeOffset.UtcNow;
        if (records.Count == 0 || records.Any(n => n.Processor?.Id != processorId || !Active(n, now)))
            throw new InvalidOperationException("The connection lease is missing, expired or owned by another processor.");
        return await ConnectionAsync(db, tx, records[0], ct);
    }

    private static Task SaveAsync(GXDbConnection db, DbTransaction tx, GXRawNotification record, CancellationToken ct) =>
        db.UpdateAsync(tx, GXUpdateArgs.Update(record, n => new {
            n.Status, n.Processor, n.LeaseToken, n.LeaseExpires, n.Attempts, n.NextAttempt, n.Processed, n.Error }), ct);

    /// <inheritdoc />
    public Task<IEnumerable<Guid>> AddAsync(IEnumerable<GXRawNotification> notifications, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notifications);
        var input = notifications.ToArray();
        if (input.Length == 0) throw new ArgumentException("Notifications are required.");
        return MutateAsync<IEnumerable<Guid>>(GXRawNotificationPolicies.Add, async (db, tx) =>
        {
            var ids = new List<Guid>();
            foreach (var item in input)
            {
                if (item == null || item.Id == Guid.Empty || item.Agent == null || item.ConnectionId == Guid.Empty ||
                    item.SequenceNumber < 0 || item.Received == null || (!item.ConnectionClosed && (item.Value == null || item.Value.Length == 0)))
                    throw new ArgumentException("Each chunk requires an identifier, receiving agent, connection, sequence, timestamp and data (unless closing).");
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
                        throw new ArgumentException("Notification identifier already exists with different content.");
                    ids.Add(existing.Id);
                    continue;
                }
                var connection = await ConnectionAsync(db, tx, item, cancellationToken);
                // Enforce transport ordering at ingestion so a parser never sees a later chunk before an earlier one.
                if (connection.Any(n => n.ConnectionClosed || n.Status == RawNotificationStatus.Completed) ||
                    item.SequenceNumber != (connection.Count == 0 ? 0 : connection[^1].SequenceNumber + 1))
                    throw new ArgumentException("Chunks must arrive in sequence starting at zero; a closed connection cannot receive more data.");
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
            }
            return ids;
        }, cancellationToken);
    }

    private static bool BytesEqual(byte[]? a, byte[]? b) => a == null ? b == null : b != null && a.AsSpan().SequenceEqual(b);

    /// <inheritdoc />
    public Task<IEnumerable<GXRawNotification>> GetNextAsync(Guid processorId, TimeSpan leaseDuration,
        Guid? receivingAgentId = null, CancellationToken cancellationToken = default) =>
        MutateAsync<IEnumerable<GXRawNotification>>(GXRawNotificationPolicies.Edit, async (db, tx) =>
        {
            _ = Expires(leaseDuration);
            await AgentAsync(db, tx, processorId, cancellationToken);
            if (receivingAgentId.HasValue) await AgentAsync(db, tx, receivingAgentId.Value, cancellationToken);
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
            if (selected == null) return Array.Empty<GXRawNotification>();
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
            if (expiration < currentExpiration) expiration = currentExpiration;
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
                throw new InvalidOperationException("Read all chunks including the connection-end record before completing the connection.");
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
            if (string.IsNullOrWhiteSpace(error)) throw new ArgumentException("An error description is required.");
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

    private static void Release(GXRawNotification chunk)
    { chunk.Processor = null; chunk.LeaseToken = null; chunk.LeaseExpires = null; chunk.NextAttempt = chunk.Status == RawNotificationStatus.Failed ? chunk.NextAttempt : null; }

    private async Task<List<GXRawNotification>> SelectedConnectionsAsync(GXDbConnection db, DbTransaction tx,
        IEnumerable<Guid> notifications, CancellationToken ct)
    {
        var ids = notifications.Distinct().ToArray();
        if (ids.Length == 0 || ids.Contains(Guid.Empty)) throw new ArgumentException("Notification identifiers are required.");
        var query = Query();
        query.Where.And<GXRawNotification>(n => ids.Contains(n.Id));
        var selected = await SelectAsync(db, tx, query, ct);
        if (selected.Count != ids.Length) throw new UnauthorizedAccessException("A selected notification is missing or inaccessible.");
        var result = new List<GXRawNotification>();
        foreach (var group in selected.GroupBy(n => (n.Agent!.Id, n.ConnectionId)))
            result.AddRange(await ConnectionAsync(db, tx, group.First(), ct));
        if (result.Any(n => Active(n, DateTimeOffset.UtcNow))) throw new InvalidOperationException("The connection has an active lease.");
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
                throw new InvalidOperationException("Only failed or expired connections can be restarted.");
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
                await db.DeleteAsync(tx, GXDeleteArgs.Delete<GXRawNotification>(n => n.Id == chunk.Id), cancellationToken);
            return true;
        }, cancellationToken);

    /// <inheritdoc />
    public Task ClearAsync(DateTimeOffset processedBefore, CancellationToken cancellationToken = default) =>
        MutateAsync(GXRawNotificationPolicies.Clear, async (db, tx) =>
        {
            if (processedBefore == default) throw new ArgumentException("A retention cutoff is required.");
            var query = Query();
            query.Where.And<GXRawNotification>(n => n.Status == RawNotificationStatus.Completed && n.Processed < processedBefore);
            var chunks = await SelectAsync(db, tx, query, cancellationToken);
            foreach (var group in chunks.GroupBy(n => (n.Agent!.Id, n.ConnectionId)))
            {
                var connection = await ConnectionAsync(db, tx, group.First(), cancellationToken);
                if (connection.All(n => n.Status == RawNotificationStatus.Completed && n.Processed < processedBefore))
                    foreach (var chunk in connection)
                        await db.DeleteAsync(tx, GXDeleteArgs.Delete<GXRawNotification>(n => n.Id == chunk.Id), cancellationToken);
            }
            return true;
        }, cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<GXRawNotification>> ListAsync(ListRawNotifications? request = null,
        ListRawNotificationsResponse? response = null, CancellationToken cancellationToken = default)
    {
        await AuthorizeAsync(GXRawNotificationPolicies.View);
        if (request?.Index < 0 || request?.Count < 0) throw new ArgumentOutOfRangeException(nameof(request));
        var query = Query(request == null || request.AllUsers);
        if (request?.Filter != null) query.Where.FilterBy(request.Filter);
        if (request?.Included?.Any() == true) query.Where.And<GXRawNotification>(n => request.Included.Contains(n.Id));
        if (request?.Exclude?.Any() == true) query.Where.And<GXRawNotification>(n => !request.Exclude.Contains(n.Id));
        await using var db = await factory.GetConnectionAsync(cancellationToken);
        await using var tx = await db.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        if (response != null)
        {
            var count = GXSelectArgs.Select<GXRawNotification>(n => GXSql.Count(1));
            count.Where.Append(query.Where);
            response.Count = await db.SingleOrDefaultAsync<int>(tx, count, cancellationToken);
        }
        query.Index = (uint)(request?.Index ?? 0);
        query.Count = (uint)(request?.Count ?? 0);
        query.Descending = request?.Descending ?? false;
        if (!string.IsNullOrEmpty(request?.OrderBy)) query.OrderBy.Add<GXRawNotification>(request.OrderBy);
        else query.OrderBy.Add<GXRawNotification>(n => n.Received!);
        query.OrderBy.Add<GXRawNotification>(n => n.Id);
        var rows = await SelectAsync(db, tx, query, cancellationToken);
        await tx.CommitAsync(cancellationToken);
        if (response != null) response.RawNotifications = rows;
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
        await using var db = await factory.GetConnectionAsync(cancellationToken);
        await using var tx = await db.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var row = (await SelectAsync(db, tx, query, cancellationToken)).SingleOrDefault()
            ?? throw new KeyNotFoundException("Raw notification not found.");
        await tx.CommitAsync(cancellationToken);
        return row;
    }
}
