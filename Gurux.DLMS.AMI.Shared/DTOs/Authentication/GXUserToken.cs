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
    /// <summary>A token value stored for a user and authentication provider.</summary>
    [System.ComponentModel.Description("A token value stored for a user and authentication provider.")]
    [DataContract(Name = "GXUserToken"), Serializable]
    [IndexCollection(true, nameof(Id), nameof(LoginProvider), nameof(Name))]
    public class GXUserToken : IUnique<string>
    {
        /// <summary>Unique identifier of the user token.</summary>
        [System.ComponentModel.Description("Unique identifier of the user token.")]
        [StringLength(36)]
        [DataMember(Name = "UserId")]
        [ForeignKey(typeof(GXUser), OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        public string Id { get; set; } = "";

        /// <summary>
        /// Login provider.
        /// </summary>
        [System.ComponentModel.Description("Login provider.")]
        [DataMember]
        [StringLength(128)]
        public string LoginProvider { get; set; } = "";

        /// <summary>
        /// Name.
        /// </summary>
        [System.ComponentModel.Description("Name.")]
        [DataMember]
        [StringLength(128)]
        public string Name { get; set; } = "";

        /// <summary>
        /// Value.
        /// </summary>
        [System.ComponentModel.Description("Value.")]
        [DataMember]
        public string Value { get; set; } = "";
    }
}
