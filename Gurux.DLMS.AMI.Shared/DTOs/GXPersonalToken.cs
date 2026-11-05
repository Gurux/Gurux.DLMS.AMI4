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

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>A personal access token and its ownership, expiry, and usage information.</summary>
    [System.ComponentModel.Description("A personal access token and its ownership, expiry, and usage information.")]
    public class GXPersonalToken
    {
        /// <summary>
        /// Token identifier.
        /// </summary>
        [System.ComponentModel.Description("Token identifier.")]
        public string Id { get; set; }

        /// <summary>
        /// Token Name.
        /// </summary>
        [System.ComponentModel.Description("Token Name.")]
        public string? Name { get; set; }

        /// <summary>Time when the record was created.</summary>
        [System.ComponentModel.Description("Time when the record was created.")]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Expiration time.
        /// </summary>
        [System.ComponentModel.Description("Expiration time.")]
        public DateTimeOffset? Expiration { get; set; }

        /// <summary>
        /// Custom scopes.
        /// </summary>
        /// <remarks>
        /// Custom scopes can be used if full access is not wanted to give for the client.
        /// </remarks>
        [System.ComponentModel.Description("Custom scopes.")]
        public IEnumerable<string>? Scopes { get; set; }

        /// <summary>Initializes a new instance of GXPersonalToken.</summary>
        [System.ComponentModel.Description("Initializes a new instance of GXPersonalToken.")]
        public GXPersonalToken()
        {
            Id = "";
        }
    }
}
