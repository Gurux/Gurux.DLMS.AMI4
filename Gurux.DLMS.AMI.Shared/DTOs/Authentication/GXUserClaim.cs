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
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Authentication
{
    /// <summary>A claim assigned to a user account.</summary>
    [System.ComponentModel.Description("A claim assigned to a user account.")]
    [DataContract(Name = "GXUserClaim"), Serializable]
    public class GXUserClaim : IUnique<int>
    {
        /// <summary>Unique identifier of the user claim.</summary>
        [System.ComponentModel.Description("Unique identifier of the user claim.")]
        [Key]
        [DataMember(Name = "ID"), Index(Unique = true), AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// User identifier.
        /// </summary>
        /// <remarks>
        /// User who has the claim.
        /// </remarks>
        [System.ComponentModel.Description("User identifier.")]
        [DataMember]
        [Index(false)]
        [IsRequired]
        [ForeignKey(typeof(GXUser), OnDelete = ForeignKeyDelete.Cascade)]
        [StringLength(36)]
        public string? UserId { get; set; }

        /// <summary>
        /// Claim type defines e,g. role, name, etc.
        /// </summary>
        [System.ComponentModel.Description("Claim type defines e,g. role, name, etc.")]
        [DataMember]
        public string? ClaimType { get; set; }

        /// <summary>
        /// Claim value defines the value of the claim.
        /// </summary>
        [System.ComponentModel.Description("Claim value defines the value of the claim.")]
        [DataMember]
        public string? ClaimValue { get; set; }
    }
}
