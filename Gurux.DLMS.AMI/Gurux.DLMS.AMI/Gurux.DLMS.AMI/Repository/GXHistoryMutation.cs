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

using Gurux.DLMS.AMI.Shared.DTOs.History;
using System.Collections;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Captures repository changes inside their shared local database transaction.</summary>
internal static class GXHistoryMutation
{
    /// <summary>Async-local entity identities whose snapshots are currently being captured.</summary>
    private static readonly AsyncLocal<HashSet<string>?> _capturing = new();
    /// <summary>Async-local entity identities excluded from recursive history capture.</summary>
    private static readonly AsyncLocal<HashSet<string>?> _suppressed = new();

    /// <summary>Checks whether recursive history capture is suppressed for the selected entity identity in this asynchronous flow.</summary>
    /// <param name="type">Entity or resource classification used to constrain this operation.</param>
    /// <param name="id">Stable identity of the record or module selected by this operation.</param>
    /// <returns>True when capture is suppressed for that entity.</returns>
    internal static bool IsSuppressed(Type type, object id) => _suppressed.Value?.Contains(Key(GXHistoryEntities.Get(type), id)) == true;

    /// <summary>Temporarily suppresses history capture for one entity while preserving the enclosing suppression set.</summary>
    /// <param name="type">Entity or resource classification used to constrain this operation.</param>
    /// <param name="id">Stable identity of the record or module selected by this operation.</param>
    /// <returns>A scope that restores the previous suppression set on disposal.</returns>
    internal static IDisposable Suppress(Type type, object id)
    {
        var previous = _suppressed.Value;
        _suppressed.Value = previous == null ? [] : new(previous, StringComparer.Ordinal);
        _suppressed.Value.Add(Key(GXHistoryEntities.Get(type), id));
        return new Release(() => _suppressed.Value = previous);
    }

    /// <summary>Runs an entity mutation while capturing its before and after history snapshots in the shared transaction.</summary>
    /// <typeparam name="T">Entity type whose persistence and history operations are tracked.</typeparam>
    /// <param name="factory">Creates or leases the database connection shared by the history transaction.</param>
    /// <param name="actor">Principal attributed to the mutation or workflow event.</param>
    /// <param name="services">Optional request-scoped services used for authorized aggregate reads and identity enlistment.</param>
    /// <param name="entities">Entity models whose identities are tracked before and after the mutation.</param>
    /// <param name="action">Callback executed inside the protected operation or transaction scope.</param>
    /// <param name="token">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>A task that completes after mutation and history capture.</returns>
    internal static Task RunAsync<T>(IDbConnectionFactory factory, ClaimsPrincipal? actor, IServiceProvider? services,
        IEnumerable<T> entities, Func<Task> action, CancellationToken token = default)
        => RunAsync<T, bool>(factory, actor, services, entities, async () => { await action(); return true; }, token);

    /// <summary>Runs an entity mutation and returns its result while capturing its before and after history snapshots.</summary>
    /// <typeparam name="T">Entity type whose persistence and history operations are tracked.</typeparam>
    /// <typeparam name="TResult">Result returned by the supplied asynchronous operation.</typeparam>
    /// <param name="factory">Creates or leases the database connection shared by the history transaction.</param>
    /// <param name="actor">Principal attributed to the mutation or workflow event.</param>
    /// <param name="services">Optional request-scoped services used for authorized aggregate reads and identity enlistment.</param>
    /// <param name="entities">Entity models whose identities are tracked before and after the mutation.</param>
    /// <param name="action">Callback executed inside the protected operation or transaction scope.</param>
    /// <param name="token">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>The mutation result after associated history work has completed.</returns>
    internal static Task<TResult> RunAsync<T, TResult>(IDbConnectionFactory factory, ClaimsPrincipal? actor,
        IServiceProvider? services, IEnumerable<T> entities, Func<Task<TResult>> action, CancellationToken token = default)
    {
        var values = entities.Cast<object>().ToArray();
        return RunCoreAsync(factory, actor, services, GXHistoryEntities.Get(typeof(T)),
            () => values.Select(value => TrackingId(GXHistoryEntities.Get(typeof(T)), value, actor)), action, false, token);
    }

    /// <summary>Runs an entity deletion while capturing the selected identifiers as deleted history targets.</summary>
    /// <typeparam name="T">Entity type whose persistence and history operations are tracked.</typeparam>
    /// <typeparam name="TKey">Persistence identifier type accepted by the entity repository.</typeparam>
    /// <param name="factory">Creates or leases the database connection shared by the history transaction.</param>
    /// <param name="actor">Principal attributed to the mutation or workflow event.</param>
    /// <param name="services">Optional request-scoped services used for authorized aggregate reads and identity enlistment.</param>
    /// <param name="ids">Persistence identifiers of entities being deleted.</param>
    /// <param name="action">Callback executed inside the protected operation or transaction scope.</param>
    /// <param name="token">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>A task that completes after deletion and history capture.</returns>
    internal static Task RunDeleteAsync<T, TKey>(IDbConnectionFactory factory, ClaimsPrincipal? actor,
        IServiceProvider? services, IEnumerable<TKey> ids, Func<Task> action, CancellationToken token = default)
        => RunDeleteAsync<T, TKey, bool>(factory, actor, services, ids, async () => { await action(); return true; }, token);

    /// <summary>Runs an entity deletion and records history for the selected persistence identifiers.</summary>
    /// <typeparam name="T">Entity type whose persistence and history operations are tracked.</typeparam>
    /// <typeparam name="TKey">Persistence identifier type accepted by the entity repository.</typeparam>
    /// <typeparam name="TResult">Result returned by the supplied asynchronous operation.</typeparam>
    /// <param name="factory">Creates or leases the database connection shared by the history transaction.</param>
    /// <param name="actor">Principal attributed to the mutation or workflow event.</param>
    /// <param name="services">Optional request-scoped services used for authorized aggregate reads and identity enlistment.</param>
    /// <param name="ids">Persistence identifiers of entities being deleted.</param>
    /// <param name="action">Callback executed inside the protected operation or transaction scope.</param>
    /// <param name="token">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>The deletion action's result after associated history work has completed.</returns>
    internal static Task<TResult> RunDeleteAsync<T, TKey, TResult>(IDbConnectionFactory factory, ClaimsPrincipal? actor,
        IServiceProvider? services, IEnumerable<TKey> ids, Func<Task<TResult>> action, CancellationToken token = default)
        => RunCoreAsync(factory, actor, services, GXHistoryEntities.Get(typeof(T)), () => ids.Cast<object>(), action, true, token);

    /// <summary>Runs a mutation for explicit entity identifiers while capturing their before and after history state.</summary>
    /// <typeparam name="T">Entity type whose persistence and history operations are tracked.</typeparam>
    /// <typeparam name="TKey">Persistence identifier type accepted by the entity repository.</typeparam>
    /// <param name="factory">Creates or leases the database connection shared by the history transaction.</param>
    /// <param name="actor">Principal attributed to the mutation or workflow event.</param>
    /// <param name="services">Optional request-scoped services used for authorized aggregate reads and identity enlistment.</param>
    /// <param name="ids">Persistence identities selected for this mutation.</param>
    /// <param name="action">Callback executed inside the protected operation or transaction scope.</param>
    /// <param name="token">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>A task that completes after mutation and history capture.</returns>
    internal static Task RunIdsAsync<T, TKey>(IDbConnectionFactory factory, ClaimsPrincipal? actor,
        IServiceProvider? services, IEnumerable<TKey> ids, Func<Task> action, CancellationToken token = default)
        => RunIdsAsync<T, TKey, bool>(factory, actor, services, ids, async () => { await action(); return true; }, token);

    /// <summary>Runs a mutation for explicit entity identifiers and records their before and after history state.</summary>
    /// <typeparam name="T">Entity type whose persistence and history operations are tracked.</typeparam>
    /// <typeparam name="TKey">Persistence identifier type accepted by the entity repository.</typeparam>
    /// <typeparam name="TResult">Result returned by the supplied asynchronous operation.</typeparam>
    /// <param name="factory">Creates or leases the database connection shared by the history transaction.</param>
    /// <param name="actor">Principal attributed to the mutation or workflow event.</param>
    /// <param name="services">Optional request-scoped services used for authorized aggregate reads and identity enlistment.</param>
    /// <param name="ids">Persistence identities selected for this mutation.</param>
    /// <param name="action">Callback executed inside the protected operation or transaction scope.</param>
    /// <param name="token">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>The mutation action's result after associated history work has completed.</returns>
    internal static Task<TResult> RunIdsAsync<T, TKey, TResult>(IDbConnectionFactory factory, ClaimsPrincipal? actor,
        IServiceProvider? services, IEnumerable<TKey> ids, Func<Task<TResult>> action, CancellationToken token = default)
        => RunCoreAsync(factory, actor, services, GXHistoryEntities.Get(typeof(T)), () => ids.Cast<object>(), action, false, token);

    /// <summary>Runs an aggregate mutation while tracking every supported entity identifier before and after the action.</summary>
    /// <typeparam name="T">Entity type whose persistence and history operations are tracked.</typeparam>
    /// <param name="factory">Creates or leases the database connection shared by the history transaction.</param>
    /// <param name="actor">Principal attributed to the mutation or workflow event.</param>
    /// <param name="services">Optional request-scoped services used for authorized aggregate reads and identity enlistment.</param>
    /// <param name="action">Callback executed inside the protected operation or transaction scope.</param>
    /// <param name="token">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>A task that completes after the aggregate mutation and history capture.</returns>
    internal static Task RunAllAsync<T>(IDbConnectionFactory factory, ClaimsPrincipal? actor,
        IServiceProvider? services, Func<Task> action, CancellationToken token = default)
        => RunAllAsync<T, bool>(factory, actor, services, async () => { await action(); return true; }, token);

    /// <summary>Runs an aggregate mutation and refreshes the tracked identifier set after the action to capture additions as well as edits.</summary>
    /// <typeparam name="T">Entity type whose persistence and history operations are tracked.</typeparam>
    /// <typeparam name="TResult">Result returned by the supplied asynchronous operation.</typeparam>
    /// <param name="factory">Creates or leases the database connection shared by the history transaction.</param>
    /// <param name="actor">Principal attributed to the mutation or workflow event.</param>
    /// <param name="services">Optional request-scoped services used for authorized aggregate reads and identity enlistment.</param>
    /// <param name="action">Callback executed inside the protected operation or transaction scope.</param>
    /// <param name="token">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>The aggregate mutation's result after history capture.</returns>
    internal static Task<TResult> RunAllAsync<T, TResult>(IDbConnectionFactory factory, ClaimsPrincipal? actor,
        IServiceProvider? services, Func<Task<TResult>> action, CancellationToken token = default)
        => GXHistoryScope.RunAsync(factory, async db =>
        {
            var adapter = GXHistoryEntities.Get(typeof(T));
            var ids = await adapter.ListIdsAsync(db, token);
            return await RunCoreAsync(factory, actor, services, adapter, () => ids, async () =>
            {
                var result = await action();
                ids = await adapter.ListIdsAsync(db, token);
                return result;
            }, false, token);
        }, token);

    /// <summary>Joins the shared history transaction, captures authorized before and after snapshots, and records effective changes without recursive duplication.</summary>
    /// <typeparam name="TResult">Result returned by the supplied asynchronous operation.</typeparam>
    /// <param name="factory">Creates or leases the database connection shared by the history transaction.</param>
    /// <param name="actor">Principal attributed to the mutation or workflow event.</param>
    /// <param name="services">Optional request-scoped services used for authorized aggregate reads and identity enlistment.</param>
    /// <param name="adapter">Explicitly registered entity adapter defining allowed history access and serialization.</param>
    /// <param name="identities">Supplies the affected persistence identifiers before and after the action.</param>
    /// <param name="action">Callback executed inside the protected operation or transaction scope.</param>
    /// <param name="deleted">Whether the action represents deletion rather than an ordinary mutation.</param>
    /// <param name="token">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>The supplied action's result after history recording.</returns>
    private static async Task<TResult> RunCoreAsync<TResult>(IDbConnectionFactory factory, ClaimsPrincipal? actor,
        IServiceProvider? services, IGXHistoryEntity adapter, Func<IEnumerable<object>> identities,
        Func<Task<TResult>> action, bool deleted, CancellationToken token)
    {
        return await GXHistoryScope.RunAsync(factory, async db =>
        {
            await using var identity = await GXHistoryIdentityScope.JoinAsync(
                services?.GetService(typeof(Gurux.DLMS.AMI.Data.ApplicationDbContext)) as Gurux.DLMS.AMI.Data.ApplicationDbContext, token);
            var previous = _capturing.Value;
            _capturing.Value ??= new(StringComparer.Ordinal);
            var owned = new HashSet<string>(StringComparer.Ordinal);
            var before = new Dictionary<string, (object Id, string? Json)>(StringComparer.Ordinal);
            var actorId = actor?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? actor?.FindFirst("sub")?.Value;
            bool userChange = actor?.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(actorId) &&
                await db.SingleOrDefaultAsync<GXUser>(GXSelectArgs.Select<GXUser>(u => u.Id!, u => u.Id == actorId && u.Removed == null), token) != null;
            var captureServices = userChange ? services : null;
            try
            {
                foreach (var id in identities().Where(ValidId).Distinct())
                {
                    var key = Key(adapter, id);
                    if (_suppressed.Value?.Contains(key) == true || !_capturing.Value.Add(key))
                    {
                        continue;
                    }

                    owned.Add(key);
                    before[key] = (id, await SnapshotAsync(adapter, db, captureServices, id, token));
                }
                TResult result = await action();
                var afterIds = identities().Where(ValidId).ToList();
                // Several repositories return generated IDs rather than mutating their input objects.
                if (result is IEnumerable returned && result is not string)
                {
                    foreach (var id in returned)
                    {
                        if (id != null && (id is Guid || id is string || id is int))
                        {
                            afterIds.Add(id);
                        }
                    }
                }

                foreach (var id in afterIds.Distinct())
                {
                    var key = Key(adapter, id);
                    if (_suppressed.Value?.Contains(key) == true)
                    {
                        continue;
                    }

                    if (!owned.Contains(key) && !_capturing.Value.Add(key))
                    {
                        continue;
                    }

                    owned.Add(key);
                    if (!before.ContainsKey(key))
                    {
                        before[key] = (id, null);
                    }
                }
                bool record = userChange &&
                    await db.SingleOrDefaultAsync<GXUser>(GXSelectArgs.Select<GXUser>(u => u.Id!, u => u.Id == actorId && u.Removed == null), token) != null;
                await using var transaction = await db.BeginTransactionAsync(token);
                foreach (var entry in before.Values)
                {
                    var remaining = deleted ? await adapter.ReadStoredAsync(db, entry.Id, token) : null;
                    var isDeleted = deleted && (remaining == null || remaining.GetType().GetProperty("Removed")?.GetValue(remaining) != null);
                    var after = isDeleted ? null : await SnapshotAsync(adapter, db, captureServices, entry.Id, token);
                    if (entry.Json == null && after == null || userChange && entry.Json == after)
                    {
                        continue;
                    }

                    var revision = await GXHistoryStore.AdvanceAsync(db, transaction, adapter.HistoryType, IdString(entry.Id), null, token);
                    if (record)
                    {
                        await GXHistoryStore.AppendAsync(db, transaction, new GXChangeHistoryItem
                        {
                            EntityType = adapter.HistoryType,
                            EntityId = IdString(entry.Id),
                            BeforeJson = entry.Json,
                            AfterJson = after,
                            Operation = entry.Json == null ? ChangeOperation.Create : after == null ? ChangeOperation.Delete : ChangeOperation.Update
                        }, actor!, GXHistoryScope.ChangeSetId, token, revision);
                    }
                }
                await transaction.CommitAsync(token);
                return result;
            }
            finally
            {
                foreach (var key in owned)
                {
                    _capturing.Value?.Remove(key);
                }

                _capturing.Value = previous;
            }
        }, token);
    }

    /// <summary>Captures a nonremoved entity through its authorized repository or stored-state projection.</summary>
    /// <param name="adapter">Explicitly registered entity adapter defining allowed history access and serialization.</param>
    /// <param name="db">Database connection used by the history or registry operation.</param>
    /// <param name="services">Optional request-scoped services used for authorized aggregate reads and identity enlistment.</param>
    /// <param name="id">Stable identity of the record or module selected by this operation.</param>
    /// <param name="token">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>Snapshot JSON, or null when the logical entity no longer exists.</returns>
    internal static async Task<string?> SnapshotAsync(IGXHistoryEntity adapter, GXDbConnection db,
        IServiceProvider? services, object id, CancellationToken token)
    {
        var stored = await adapter.ReadStoredAsync(db, id, token);
        // A soft-deleted record is already absent from the logical aggregate. Domain
        // readers can reject it; a later physical purge must not try to reopen it.
        if (stored == null || stored.GetType().GetProperty("Removed")?.GetValue(stored) != null)
        {
            return null;
        }
        // User-facing reads materialize aggregate memberships and owned properties through domain repositories.
        var value = services == null ? stored : await adapter.ReadAsync(services, id, token) ?? stored;
        return services == null ? GXHistorySnapshot.SerializeStored(value) : GXHistorySnapshot.Serialize(value);
    }

    /// <summary>Uses a persisted entity identifier or derives a stable natural-key identifier for a new user or agent property.</summary>
    /// <param name="adapter">Explicitly registered entity adapter defining allowed history access and serialization.</param>
    /// <param name="entity">Entity whose persistence or natural-key identity must be tracked.</param>
    /// <param name="actor">Principal attributed to the mutation or workflow event.</param>
    /// <returns>The persistence identifier or deterministic property tracking identifier.</returns>
    private static object TrackingId(IGXHistoryEntity adapter, object entity, ClaimsPrincipal? actor)
    {
        var id = adapter.GetId(entity);
        if (id is Guid guid && guid == Guid.Empty)
        {
            string? owner = null, module = null, name = null;
            if (entity is GXUserProperty property)
            {
                owner = property.User?.Id ?? actor?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? actor?.FindFirst("sub")?.Value;
                module = property.Module?.Id;
                name = property.Name;
            }
            else if (entity is GXAgentProperty agentProperty)
            {
                owner = agentProperty.Agent?.Id.ToString();
                module = agentProperty.Module?.Id;
                name = agentProperty.Name;
            }
            if (!string.IsNullOrWhiteSpace(owner) && !string.IsNullOrWhiteSpace(name))
            {
                return new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new[] { owner, module, name }))).AsSpan(0, 16));
            }
        }
        return id;
    }

    /// <summary>Rejects missing or empty identifiers while accepting nonzero numeric and nonempty textual identities.</summary>
    /// <param name="id">Stable identity of the record or module selected by this operation.</param>
    /// <returns>True when the identity can represent a tracked entity.</returns>
    private static bool ValidId(object? id) => id != null && id switch
    { Guid value => value != Guid.Empty, string value => !string.IsNullOrWhiteSpace(value), int value => value != 0, _ => true };
    /// <summary>Converts an entity identifier to invariant text for history storage.</summary>
    /// <param name="id">Identifier of the affected record.</param>
    /// <returns>The identifier formatted using the invariant culture.</returns>
    internal static string IdString(object id) => Convert.ToString(id, CultureInfo.InvariantCulture)!;
    /// <summary>Combines an adapter's stable entity name and invariant identifier into a collision-resistant capture key.</summary>
    /// <param name="adapter">Explicitly registered entity adapter defining allowed history access and serialization.</param>
    /// <param name="id">Stable identity of the record or module selected by this operation.</param>
    /// <returns>The entity-type-qualified tracking key.</returns>
    private static string Key(IGXHistoryEntity adapter, object id) => adapter.Name + "\0" + IdString(id);
    /// <summary>Releases a history mutation lease when its enclosing scope ends.</summary>
    /// <param name="release">Callback that releases the mutation lease.</param>
    private sealed class Release(Action release) : IDisposable
    {
        /// <inheritdoc />
        public void Dispose() => release();
    }
}
