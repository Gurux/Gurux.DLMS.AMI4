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
    private static readonly AsyncLocal<HashSet<string>?> Capturing = new();
    private static readonly AsyncLocal<HashSet<string>?> Suppressed = new();

    internal static bool IsSuppressed(Type type, object id) => Suppressed.Value?.Contains(Key(GXHistoryEntities.Get(type), id)) == true;

    internal static IDisposable Suppress(Type type, object id)
    {
        var previous = Suppressed.Value;
        Suppressed.Value = previous == null ? [] : new(previous, StringComparer.Ordinal);
        Suppressed.Value.Add(Key(GXHistoryEntities.Get(type), id));
        return new Release(() => Suppressed.Value = previous);
    }

    internal static Task RunAsync<T>(IDbConnectionFactory factory, ClaimsPrincipal? actor, IServiceProvider? services,
        IEnumerable<T> entities, Func<Task> action, CancellationToken token = default)
        => RunAsync<T, bool>(factory, actor, services, entities, async () => { await action(); return true; }, token);

    internal static Task<TResult> RunAsync<T, TResult>(IDbConnectionFactory factory, ClaimsPrincipal? actor,
        IServiceProvider? services, IEnumerable<T> entities, Func<Task<TResult>> action, CancellationToken token = default)
    {
        var values = entities.Cast<object>().ToArray();
        return RunCoreAsync(factory, actor, services, GXHistoryEntities.Get(typeof(T)),
            () => values.Select(value => TrackingId(GXHistoryEntities.Get(typeof(T)), value, actor)), action, false, token);
    }

    internal static Task RunDeleteAsync<T, TKey>(IDbConnectionFactory factory, ClaimsPrincipal? actor,
        IServiceProvider? services, IEnumerable<TKey> ids, Func<Task> action, CancellationToken token = default)
        => RunDeleteAsync<T, TKey, bool>(factory, actor, services, ids, async () => { await action(); return true; }, token);

    internal static Task<TResult> RunDeleteAsync<T, TKey, TResult>(IDbConnectionFactory factory, ClaimsPrincipal? actor,
        IServiceProvider? services, IEnumerable<TKey> ids, Func<Task<TResult>> action, CancellationToken token = default)
        => RunCoreAsync(factory, actor, services, GXHistoryEntities.Get(typeof(T)), () => ids.Cast<object>(), action, true, token);

    internal static Task RunIdsAsync<T, TKey>(IDbConnectionFactory factory, ClaimsPrincipal? actor,
        IServiceProvider? services, IEnumerable<TKey> ids, Func<Task> action, CancellationToken token = default)
        => RunIdsAsync<T, TKey, bool>(factory, actor, services, ids, async () => { await action(); return true; }, token);

    internal static Task<TResult> RunIdsAsync<T, TKey, TResult>(IDbConnectionFactory factory, ClaimsPrincipal? actor,
        IServiceProvider? services, IEnumerable<TKey> ids, Func<Task<TResult>> action, CancellationToken token = default)
        => RunCoreAsync(factory, actor, services, GXHistoryEntities.Get(typeof(T)), () => ids.Cast<object>(), action, false, token);

    internal static Task RunAllAsync<T>(IDbConnectionFactory factory, ClaimsPrincipal? actor,
        IServiceProvider? services, Func<Task> action, CancellationToken token = default)
        => RunAllAsync<T, bool>(factory, actor, services, async () => { await action(); return true; }, token);

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

    private static async Task<TResult> RunCoreAsync<TResult>(IDbConnectionFactory factory, ClaimsPrincipal? actor,
        IServiceProvider? services, IGXHistoryEntity adapter, Func<IEnumerable<object>> identities,
        Func<Task<TResult>> action, bool deleted, CancellationToken token)
    {
        return await GXHistoryScope.RunAsync(factory, async db =>
        {
            await using var identity = await GXHistoryIdentityScope.JoinAsync(
                services?.GetService(typeof(Gurux.DLMS.AMI.Data.ApplicationDbContext)) as Gurux.DLMS.AMI.Data.ApplicationDbContext, token);
            var previous = Capturing.Value;
            Capturing.Value ??= new(StringComparer.Ordinal);
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
                    if (Suppressed.Value?.Contains(key) == true || !Capturing.Value.Add(key))
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
                    if (Suppressed.Value?.Contains(key) == true)
                    {
                        continue;
                    }

                    if (!owned.Contains(key) && !Capturing.Value.Add(key))
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
                    Capturing.Value?.Remove(key);
                }

                Capturing.Value = previous;
            }
        }, token);
    }

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

    private static bool ValidId(object? id) => id != null && id switch
    { Guid value => value != Guid.Empty, string value => !string.IsNullOrWhiteSpace(value), int value => value != 0, _ => true };
    internal static string IdString(object id) => Convert.ToString(id, CultureInfo.InvariantCulture)!;
    private static string Key(IGXHistoryEntity adapter, object id) => adapter.Name + "\0" + IdString(id);
    private sealed class Release(Action release) : IDisposable { public void Dispose() => release(); }
}
