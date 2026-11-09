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
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Persists coalesced runtime-cache updates in the configuration transaction and retries delivery after commit.</summary>
internal static class GXHistoryRuntimeEffects
{
    private static readonly SemaphoreSlim DispatchGate = new(1, 1);
    private static readonly JsonSerializerOptions PayloadOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        MaxDepth = 64
    };

    internal static void EnqueueScript(GXScript script, IWorkflowHandler handler, bool deleted = false)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(handler);
        if (GXHistoryScope.CurrentFactory is not { } factory)
        {
            handler.UnloadScript(script.Id);
            if (!deleted)
            {
                handler.Update(script);
            }

            return;
        }
        Enqueue(factory, "Script", script.Id, JsonSerializer.Serialize(script, PayloadOptions), deleted, handler);
    }

    internal static void EnqueueWorkflow(GXWorkflow workflow, IWorkflowHandler handler, bool deleted = false)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(handler);
        if (GXHistoryScope.CurrentFactory is not { } factory)
        {
            if (deleted)
            {
                handler.Delete(workflow);
            }
            else
            {
                handler.Add(workflow);
            }

            return;
        }
        Enqueue(factory, "Workflow", workflow.Id, JsonSerializer.Serialize(workflow, PayloadOptions), deleted, handler);
    }

    private static void Enqueue(IDbConnectionFactory factory, string kind, Guid entityId, string payload,
        bool deleted, IWorkflowHandler handler)
    {
        if (entityId == Guid.Empty)
        {
            throw new ArgumentException("A runtime effect requires a persisted target identifier.");
        }

        using var db = GXHistoryScope.TryLease(factory)
            ?? throw new InvalidOperationException("A runtime effect must join its configuration transaction.");
        using var transaction = db.BeginTransaction();
        var key = new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(kind + "\0" + entityId.ToString("D"))).AsSpan(0, 16));
        var existing = db.SingleOrDefault<GXChangeHistoryRuntimeEffect>(transaction,
            GXSelectArgs.Select<GXChangeHistoryRuntimeEffect>(e => e.Id, e => e.Id == key));
        var row = new GXChangeHistoryRuntimeEffect
        {
            Id = key, Kind = kind, EntityId = entityId, Version = Guid.NewGuid(),
            Deleted = deleted, Payload = payload, CreatedAt = DateTimeOffset.UtcNow
        };
        if (existing == null)
        {
            db.Insert(transaction, GXInsertArgs.Insert(row));
        }
        else
        {
            db.Update(transaction, GXUpdateArgs.Update(row, e => new { e.Kind, e.EntityId, e.Version, e.Deleted, e.Payload, e.CreatedAt }));
        }

        transaction.Commit();
        // The durable row already belongs to the outer transaction. A crash before dispatch leaves it for the worker.
        GXHistoryScope.Defer(() =>
        {
            try { DispatchPendingAsync(factory, handler).GetAwaiter().GetResult(); }
            catch (Exception error) { Trace.TraceError("Runtime effect dispatch is pending ({0}).", error.GetType().Name); }
        });
    }

    /// <summary>Delivers current pending updates; failures stay durable for a later attempt.</summary>
    internal static async Task DispatchPendingAsync(IDbConnectionFactory factory, IWorkflowHandler handler,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(handler);
        if (GXHistoryScope.IsActive)
        {
            throw new InvalidOperationException("Runtime effects can be dispatched only after their configuration transaction commits.");
        }

        await DispatchGate.WaitAsync(token);
        try
        {
            await using var db = await factory.GetConnectionAsync(token, notifySqlExcution: false);
            var keys = await db.SelectAsync<GXChangeHistoryRuntimeEffect>(GXSelectArgs.Select<GXChangeHistoryRuntimeEffect>(e => e.Id), token);
            foreach (var pending in keys)
            {
                token.ThrowIfCancellationRequested();
                var row = await db.SingleOrDefaultAsync<GXChangeHistoryRuntimeEffect>(
                    GXSelectArgs.SelectAll<GXChangeHistoryRuntimeEffect>(e => e.Id == pending.Id), token);
                if (row == null)
                {
                    continue;
                }

                try
                {
                    await DeliverAsync(db, row, handler, token);
                    var version = row.Version;
                    await db.DeleteAsync(GXDeleteArgs.Delete<GXChangeHistoryRuntimeEffect>(e => e.Id == row.Id && e.Version == version), token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
                catch (Exception error)
                {
                    // Runtime DTOs can contain private configuration. Never trace the payload or exception message.
                    Trace.TraceError("Runtime effect {0}/{1} remains pending ({2}).", row.Kind, row.EntityId, error.GetType().Name);
                }
            }
        }
        finally { DispatchGate.Release(); }
    }

    private static async Task DeliverAsync(GXDbConnection db, GXChangeHistoryRuntimeEffect row,
        IWorkflowHandler handler, CancellationToken token)
    {
        if (row.Kind == "Script")
        {
            var script = JsonSerializer.Deserialize<GXScript>(row.Payload, PayloadOptions)
                ?? throw new InvalidDataException("Missing script runtime payload.");
            if (script.Id != row.EntityId)
            {
                throw new InvalidDataException("Mismatched runtime target.");
            }

            var current = await db.SingleOrDefaultAsync<GXScript>(GXSelectArgs.Select<GXScript>(s => new { s.Id, s.Removed }, s => s.Id == row.EntityId), token);
            handler.UnloadScript(row.EntityId);
            if (!row.Deleted && current != null && current.Removed == null)
            {
                handler.Update(script);
            }
        }
        else if (row.Kind == "Workflow")
        {
            var workflow = JsonSerializer.Deserialize<GXWorkflow>(row.Payload, PayloadOptions)
                ?? throw new InvalidDataException("Missing workflow runtime payload.");
            if (workflow.Id != row.EntityId)
            {
                throw new InvalidDataException("Mismatched runtime target.");
            }

            var current = await db.SingleOrDefaultAsync<GXWorkflow>(GXSelectArgs.Select<GXWorkflow>(w => new { w.Id, w.Removed }, w => w.Id == row.EntityId), token);
            if (row.Deleted || current == null || current.Removed != null)
            {
                handler.Delete(workflow);
            }
            else
            {
                handler.Add(workflow);
            }
        }
        else
        {
            throw new InvalidDataException("Unsupported runtime effect family.");
        }
    }
}
