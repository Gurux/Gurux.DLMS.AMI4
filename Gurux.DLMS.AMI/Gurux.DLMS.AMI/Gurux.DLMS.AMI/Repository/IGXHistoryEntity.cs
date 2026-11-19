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
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Explicit domain operations available to history; arbitrary CLR types are never activated.</summary>
internal interface IGXHistoryEntity
{
    /// <summary>Stable identifier of this history entity adapter.</summary>
    Guid Id { get; }
    /// <summary>History type metadata exposed to callers.</summary>
    GXChangeHistoryType HistoryType { get; }
    /// <summary>Stable entity type name used by history storage.</summary>
    string Name { get; }
    /// <summary>CLR entity type supported by this adapter.</summary>
    Type EntityType { get; }
    /// <summary>Reads the persistence identifier from an entity supported by this adapter.</summary>
    /// <param name="entity">Entity whose identifier is requested.</param>
    /// <returns>The entity identifier.</returns>
    object GetId(object entity);
    /// <summary>Converts the invariant stored identifier into the entity's identifier type.</summary>
    /// <param name="id">Invariant identifier text read from history storage.</param>
    /// <returns>The identifier value expected by repository operations.</returns>
    object ParseId(string id);
    /// <summary>Reads the current entity through its authorized domain repository.</summary>
    /// <param name="services">Services for the requesting user's scope.</param>
    /// <param name="id">Identifier of the entity to read.</param>
    /// <param name="token">Cancels the repository operation.</param>
    /// <returns>The current entity, or null if it no longer exists.</returns>
    Task<object?> ReadAsync(IServiceProvider services, object id, CancellationToken token);
    /// <summary>Restores an entity through its authorized domain repository.</summary>
    /// <param name="services">Services for the requesting user's scope.</param>
    /// <param name="entity">Entity snapshot to restore.</param>
    /// <param name="token">Cancels the repository operation.</param>
    /// <returns>A task that completes when the restoration is stored.</returns>
    Task WriteAsync(IServiceProvider services, object entity, CancellationToken token);
    /// <summary>Reads the stored entity using the supplied history database connection.</summary>
    /// <param name="db">Connection participating in the history operation.</param>
    /// <param name="id">Identifier of the entity to read.</param>
    /// <param name="token">Cancels the database query.</param>
    /// <returns>The persisted entity, or null if the identifier is absent.</returns>
    Task<object?> ReadStoredAsync(GXDbConnection db, object id, CancellationToken token);
    /// <summary>Enumerates stored identifiers for entities supported by this adapter.</summary>
    /// <param name="db">Connection used to query stored entities.</param>
    /// <param name="token">Cancels the database query.</param>
    /// <returns>Identifiers available for history capture.</returns>
    Task<IReadOnlyList<object>> ListIdsAsync(GXDbConnection db, CancellationToken token);
}
