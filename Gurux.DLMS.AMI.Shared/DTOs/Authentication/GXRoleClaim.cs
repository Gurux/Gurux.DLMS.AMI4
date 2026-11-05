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

using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;

namespace Gurux.DLMS.AMI.Shared.DTOs.Authentication
{
    /// <summary>A claim assigned to an authorization role.</summary>
    [System.ComponentModel.Description("A claim assigned to an authorization role.")]
    [DataContract(Name = "GXRoleClaim"), Serializable]
    public class GXRoleClaim : IUnique<int>
    {
        /// <summary>Unique identifier of the role claim.</summary>
        [System.ComponentModel.Description("Unique identifier of the role claim.")]
        [Key]
        [DataMember(Name = "ID"), Index]
        [AutoIncrement]
        public int Id { get; set; }

        /// <summary>Role identifier.</summary>
        [System.ComponentModel.Description("Role identifier.")]
        [DataMember]
        [StringLength(36)]
        [Index(Unique = false)]
        [IsRequired]
        [ForeignKey(typeof(GXRole), OnDelete = ForeignKeyDelete.Cascade)]
        public string RoleId { get; set; } = "";

        /// <summary>
        /// Claim type.
        /// </summary>
        [System.ComponentModel.Description("Claim type.")]
        [DataMember]
        public string? ClaimType { get; set; }

        /// <summary>
        /// Claim value.
        /// </summary>
        [System.ComponentModel.Description("Claim value.")]
        [DataMember]
        public string? ClaimValue { get; set; }
    }
}
