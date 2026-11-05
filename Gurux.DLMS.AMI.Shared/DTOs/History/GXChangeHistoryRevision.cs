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

using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Gurux.DLMS.AMI.Shared.DTOs.History;

/// <summary>Persistent configuration revision clock independent of user-owned history.</summary>
[Description("Persistent configuration revision clock independent of user-owned history.")]
[IndexCollection(true, nameof(EntityType), nameof(EntityId))]
public sealed class GXChangeHistoryRevision : IUnique<Guid>
{
    /// <summary>Gets or sets the unique GUID of this entity revision record.</summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>Gets or sets the entity type referenced by its stable GUID in GXChangeHistoryType.</summary>
    [IsRequired]
    [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
    public GXChangeHistoryType? EntityType { get; set; }

    /// <summary>Gets or sets the canonical entity identifier.</summary>
    [IsRequired, StringLength(512)]
    public string? EntityId { get; set; }

    /// <summary>Gets or sets the current configuration revision, including changes without a user history record.</summary>
    public long Revision { get; set; }
}
