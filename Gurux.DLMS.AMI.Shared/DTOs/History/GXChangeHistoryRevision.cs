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
