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
using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Records module-owned user setting changes in the module's existing transaction.</summary>
internal static class GXHistoryModulePropertyCapture
{
    /// <summary>Advances the shared revision clock and records user-attributed changes without opening a second transaction.</summary>
    /// <param name="scope">The module's active repository transaction.</param>
    /// <param name="beforeJson">The immutable snapshot captured before the original setting row was mutated.</param>
    /// <param name="after">The persisted setting after mutation, or null after a successful deletion.</param>
    /// <param name="id">The stable identity of the affected user setting.</param>
    /// <param name="token">The cancellation token.</param>
    internal static async Task RecordAsync(GXModuleRepositoryScope scope, string? beforeJson,
        GXUserProperty? after, Guid id, CancellationToken token)
    {
        if (GXHistoryMutation.IsSuppressed(typeof(GXUserProperty), id))
        {
            return;
        }

        string? afterJson = after == null ? null : GXHistorySnapshot.Serialize(after);
        if (beforeJson == afterJson)
        {
            return;
        }

        var identity = id.ToString();
        var revision = await GXHistoryStore.AdvanceAsync(scope.Connection, scope.Transaction,
            GXHistoryEntities.Get(typeof(GXUserProperty)).HistoryType, identity, null, token);
        var actor = new HttpContextAccessor().HttpContext?.User;
        var actorId = actor?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? actor?.FindFirst("sub")?.Value;
        if (actor?.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(actorId) ||
            await scope.Connection.SingleOrDefaultAsync<GXUser>(scope.Transaction,
                GXSelectArgs.Select<GXUser>(u => u.Id!, u => u.Id == actorId && u.Removed == null), token) == null)
        {
            return;
        }

        await GXHistoryStore.AppendAsync(scope.Connection, scope.Transaction, new GXChangeHistoryItem
        {
            EntityType = GXHistoryEntities.Get(typeof(GXUserProperty)).HistoryType,
            EntityId = identity,
            Operation = beforeJson == null ? ChangeOperation.Create : afterJson == null ? ChangeOperation.Delete : ChangeOperation.Update,
            BeforeJson = beforeJson,
            AfterJson = afterJson,
            Source = ChangeSource.Module,
            SourceId = scope.ModuleId,
        }, actor, scope.ChangeSetId, token, revision);
    }
}
