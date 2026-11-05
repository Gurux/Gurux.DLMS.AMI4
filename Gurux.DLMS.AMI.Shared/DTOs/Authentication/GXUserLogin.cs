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
    /// <summary>An external authentication provider login associated with a user account.</summary>
    [System.ComponentModel.Description("An external authentication provider login associated with a user account.")]
    [DataContract(Name = "GXUserLogin"), Serializable]
    public class GXUserLogin : IUnique<string>
    {
        /// <summary>
        /// Gets or sets the login provider.
        /// </summary>
        [System.ComponentModel.Description("Gets or sets the login provider.")]
        [StringLength(128)]
        [DataMember(Name = "LoginProvider")]
        [Filter(FilterType.Exact)]
        public string Id { get; set; } = "";

        /// <summary>
        /// Gets or sets the unique identifier for the authentication provider.
        /// </summary>
        [System.ComponentModel.Description("Gets or sets the unique identifier for the authentication provider.")]
        [DataMember]
        [StringLength(128)]
        public string? ProviderKey { get; set; } = default!;

        /// <summary>
        /// Gets or sets the display name of the authentication provider.
        /// </summary>
        [System.ComponentModel.Description("Gets or sets the display name of the authentication provider.")]
        [DataMember]
        [StringLength(128)]
        public string? ProviderDisplayName { get; set; } = default!;

        /// <summary>User identifier.</summary>
        [System.ComponentModel.Description("User identifier.")]
        [DataMember]
        [Index]
        public Guid UserId { get; set; }
    }
}
