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

using System.Runtime.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;

namespace Gurux.DLMS.AMI.Shared.DTOs.View;

/// <summary>Assigns an alternative named authorization policy to a table view column.</summary>
[System.ComponentModel.Description("Assigns an alternative named authorization policy to a table view column.")]
[DataContract, Serializable]
[IndexCollection(true, nameof(ViewColumnId), nameof(PolicyId))]
public class GXViewColumnPolicy
{
    /// <summary>Gets or sets the view column associated with the policy.</summary>
    [System.ComponentModel.Description("Gets or sets the view column associated with the policy.")]
    [DataMember, IsRequired]
    [ForeignKey(typeof(GXViewColumn), OnDelete = ForeignKeyDelete.Cascade)]
    public Guid ViewColumnId { get; set; }

    /// <summary>Gets or sets the identifier of the assigned authorization policy.</summary>
    [System.ComponentModel.Description("Gets or sets the identifier of the assigned authorization policy.")]
    [DataMember, IsRequired]
    [ForeignKey(typeof(GXPolicy))]
    public Guid PolicyId { get; set; }
}
