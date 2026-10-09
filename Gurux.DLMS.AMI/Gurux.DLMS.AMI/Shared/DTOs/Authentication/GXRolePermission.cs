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
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Gurux.DLMS.AMI.Shared.DTOs.Authentication
{
    /// <summary>
    /// Role permission membership table.
    /// </summary>
    [DataContract(Name = "GXRolePermission"), Serializable]
    [IndexCollection(true, nameof(RoleId), nameof(PermissionId), Clustered = true)]
    public class GXRolePermission
    {
        /// <summary>
        /// Role ID.
        /// </summary>
        [ForeignKey(typeof(GXRole), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        [DataMember]
        [StringLength(36)]
        public string? RoleId { get; set; }

        /// <summary>
        /// Permission ID.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXPermission), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public Guid? PermissionId { get; set; }
    }
}

