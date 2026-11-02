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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>Records an entity change with JSON snapshots for comparison and restoration.</summary>
    [System.ComponentModel.Description("Records an entity change with JSON snapshots for comparison and restoration.")]
    public sealed class GXChangeHistoryItem : IUnique<Guid>
    {
        /// <summary>Gets or sets the unique identifier of this history item.</summary>
        [System.ComponentModel.Description("Gets or sets the unique identifier of this history item.")]
        public Guid Id { get; set; }

        /// <summary>Gets or sets the stable type identifier of the changed entity, such as Device or User.</summary>
        [System.ComponentModel.Description("Gets or sets the stable type identifier of the changed entity, such as Device or User.")]
        [IsRequired]
        public string? EntityType { get; set; }

        /// <summary>Gets or sets the identifier of the changed entity as a string.</summary>
        [System.ComponentModel.Description("Gets or sets the identifier of the changed entity as a string.")]
        [IsRequired]
        public string? EntityId { get; set; }

        /// <summary>Gets or sets the operation performed on the entity.</summary>
        [System.ComponentModel.Description("Gets or sets the operation performed on the entity.")]
        [IsRequired]
        public ChangeOperation? Operation { get; set; }

        /// <summary>Gets or sets the date and time of the change.</summary>
        [System.ComponentModel.Description("Gets or sets the date and time of the change.")]
        [IsRequired]
        public DateTimeOffset? ChangedAt { get; set; }

        /// <summary>Gets or sets the user responsible for the change.</summary>
        [System.ComponentModel.Description("Gets or sets the user responsible for the change.")]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        public GXUser? ChangedBy { get; set; }

        /// <summary>Gets or sets the identifier grouping changes made by the same operation.</summary>
        [System.ComponentModel.Description("Gets or sets the identifier grouping changes made by the same operation.")]
        [IsRequired]
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

        /// <summary>Gets or sets the revision number of the entity after this change.</summary>
        [System.ComponentModel.Description("Gets or sets the revision number of the entity after this change.")]
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
