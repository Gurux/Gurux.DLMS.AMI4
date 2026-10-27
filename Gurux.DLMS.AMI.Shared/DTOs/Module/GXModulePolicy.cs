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
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;

namespace Gurux.DLMS.AMI.Shared.DTOs.Module;

/// <summary>Assigns an alternative named policy to a module.</summary>
[System.ComponentModel.Description("Assigns an alternative named policy to a module.")]
[DataContract, Serializable]
[IndexCollection(true, nameof(ModuleId), nameof(PolicyId))]
public class GXModulePolicy
{
    /// <summary>Identifier of the module to which the policy is assigned.</summary>
    [System.ComponentModel.Description("Identifier of the module to which the policy is assigned.")]
    [DataMember, IsRequired, StringLength(64)]
    [ForeignKey(typeof(GXModule), OnDelete = ForeignKeyDelete.Cascade)]
    public string ModuleId { get; set; } = "";

    /// <summary>Identifier of the assigned authorization policy.</summary>
    [System.ComponentModel.Description("Identifier of the assigned authorization policy.")]
    [DataMember, IsRequired]
    [ForeignKey(typeof(GXPolicy))]
    public Guid PolicyId { get; set; }
}
