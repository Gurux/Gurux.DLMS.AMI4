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
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Gurux.DLMS.AMI.Shared.DTOs.Authentication
{
    /// <summary>A permission assigned directly to a user account.</summary>
    [System.ComponentModel.Description("A permission assigned directly to a user account.")]
    [DataContract(Name = "GXUserPermission"), Serializable]
    [IndexCollection(true, nameof(UserId), nameof(PermissionId), Clustered = true)]
    public class GXUserPermission
    {
        /// <summary>User identifier.</summary>
        [System.ComponentModel.Description("User identifier.")]
        [ForeignKey(typeof(GXUser), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        [DataMember]
        [StringLength(36)]
        public string? UserId { get; set; }

        /// <summary>Permission identifier.</summary>
        [System.ComponentModel.Description("Permission identifier.")]
        [DataMember]
        [ForeignKey(typeof(GXPermission), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public Guid? PermissionId { get; set; }
    }
}

