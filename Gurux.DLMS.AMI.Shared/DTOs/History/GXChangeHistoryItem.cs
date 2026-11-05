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

using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace Gurux.DLMS.AMI.Shared.DTOs.History
{
    /// <summary>Records a change to an entity identified by its history type and entity identifier, with JSON snapshots for comparison and restoration.</summary>
    [System.ComponentModel.Description("Records a change to an entity identified by its history type and entity identifier, with JSON snapshots for comparison and restoration.")]
    [IndexCollection(true, nameof(EntityType), nameof(EntityId), nameof(Revision))]
    public sealed class GXChangeHistoryItem : IUnique<Guid>
    {
        /// <summary>Gets or sets the unique identifier of this history item.</summary>
        [System.ComponentModel.Description("Gets or sets the unique identifier of this history item.")]
        [Key]
        public Guid Id { get; set; }

        /// <summary>Gets or sets the entity type referenced by its stable GUID in GXChangeHistoryType.</summary>
        /// <remarks>The reference uses <see cref="GXChangeHistoryType.Id"/>; the type name is not the key.</remarks>
        [System.ComponentModel.Description("Gets or sets the entity type referenced by its stable GUID in GXChangeHistoryType.")]
        [IsRequired]
        [Index(false)]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        public GXChangeHistoryType? EntityType { get; set; }

        /// <summary>Gets or sets the canonical string identifier of the changed entity within EntityType.</summary>
        [System.ComponentModel.Description("Gets or sets the canonical string identifier of the changed entity within EntityType.")]
        [IsRequired]
        [StringLength(512), Index(false)]
        public string? EntityId { get; set; }

        /// <summary>Gets or sets the operation performed on the entity.</summary>
        [System.ComponentModel.Description("Gets or sets the operation performed on the entity.")]
        [IsRequired]
        public ChangeOperation? Operation { get; set; }

        /// <summary>Gets or sets the date and time of the change.</summary>
        [System.ComponentModel.Description("Gets or sets the date and time of the change.")]
        [IsRequired]
        [Index(false)]
        public DateTimeOffset? ChangedAt { get; set; }

        /// <summary>Gets or sets the user responsible for the change.</summary>
        [System.ComponentModel.Description("Gets or sets the user responsible for the change.")]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        public GXUser? ChangedBy { get; set; }

        /// <summary>Gets or sets the identifier grouping changes made by the same operation.</summary>
        [System.ComponentModel.Description("Gets or sets the identifier grouping changes made by the same operation.")]
        [IsRequired]
        [Index(false)]
        public Guid? ChangeSetId { get; set; }

        /// <summary>Gets or sets the type of source that initiated the change.</summary>
        [System.ComponentModel.Description("Gets or sets the type of source that initiated the change.")]
        [IsRequired]
        public ChangeSource? Source { get; set; }

        /// <summary>Gets or sets the identifier of the specific source that initiated the change, or null when unavailable.</summary>
        /// <remarks>For example, an agent identifier, scheduled task identifier, or module identifier. The source category is specified by <see cref="Source"/>.</remarks>
        [System.ComponentModel.Description("Identifier of the specific source that initiated the change, or null when unavailable.")]
        [StringLength(512)]
        public string? SourceId { get; set; }

        /// <summary>Gets or sets the revision number after this change for the entity identified by EntityType and EntityId.</summary>
        [System.ComponentModel.Description("Gets or sets the revision number after this change for the entity identified by EntityType and EntityId.")]
        [IsRequired]
        public long? Revision { get; set; }

        /// <summary>Gets or sets the schema version used to interpret the JSON snapshots.</summary>
        [System.ComponentModel.Description("Gets or sets the schema version used to interpret the JSON snapshots.")]
        [IsRequired]
        public int? SchemaVersion { get; set; } = 1;

        /// <summary>Gets or sets the entity state before the change as JSON, or null for creation.</summary>
        [System.ComponentModel.Description("Gets or sets the entity state before the change as JSON, or null for creation.")]
        public string? BeforeJson { get; set; }

        /// <summary>Gets or sets the entity state after the change as JSON, or null for deletion.</summary>
        [System.ComponentModel.Description("Gets or sets the entity state after the change as JSON, or null for deletion.")]
        public string? AfterJson { get; set; }

        /// <summary>Gets or sets the identifier of the history item whose saved state was restored by this change, or null when this is not a restoration.</summary>
        [System.ComponentModel.Description("Gets or sets the identifier of the history item whose saved state was restored by this change, or null when this is not a restoration.")]
        public Guid? RestoredFromHistoryItemId { get; set; }

        /// <summary>Gets or sets a human-readable description of the change.</summary>
        [System.ComponentModel.Description("Gets or sets a human-readable description of the change.")]
        public string? Description { get; set; }
    }
}
