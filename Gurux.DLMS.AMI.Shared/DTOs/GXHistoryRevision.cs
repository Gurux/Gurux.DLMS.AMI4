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

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Gurux.Service.Orm.Common;

namespace Gurux.DLMS.AMI.Shared.DTOs;

/// <summary>Persistent configuration revision clock independent of user-owned history.</summary>
[Description("Persistent configuration revision clock independent of user-owned history.")]
public sealed class GXHistoryRevision : IUnique<string>
{
    /// <summary>Gets or sets the deterministic identifier of the entity revision clock.</summary>
    [Key, StringLength(64)]
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the stable entity type name.</summary>
    [IsRequired, StringLength(128)]
    public string? EntityType { get; set; }

    /// <summary>Gets or sets the canonical entity identifier.</summary>
    [IsRequired, StringLength(512)]
    public string? EntityId { get; set; }

    /// <summary>Gets or sets the current configuration revision, including changes without a user history record.</summary>
    public long Revision { get; set; }
}
