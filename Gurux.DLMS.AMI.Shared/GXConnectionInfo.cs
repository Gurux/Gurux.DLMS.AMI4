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

namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// Represents information about a connection, including its identifier, associated user, and metadata.
    /// </summary>
    /// <remarks>This class provides details about a connection, such as the connection ID, user information, 
    /// IP address, user agent, and the timestamp when the connection was established. It is commonly  used to track and
    /// manage active connections in a system.</remarks>
    [System.ComponentModel.Description("Represents information about a connection, including its identifier, associated user, and metadata.")]
    public class GXConnectionInfo
    {
        /// <summary>
        /// Gets or sets the unique identifier for the connection.
        /// </summary>
        [System.ComponentModel.Description("Gets or sets the unique identifier for the connection.")]
        public string? ConnectionId { get; set; }
        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        [System.ComponentModel.Description("Gets or sets the unique identifier for the user.")]
        public string? UserId { get; set; }

        /// <summary>
        /// Gets or sets the user email address.
        /// </summary>
        [System.ComponentModel.Description("Gets or sets the user email address.")]
        public string? Email { get; set; }

        /// <summary>
        /// Gets or sets the remote client IP address.
        /// </summary>
        [System.ComponentModel.Description("Gets or sets the remote client IP address.")]
        public string? IpAddress { get; set; }
        /// <summary>
        /// Gets or sets the user agent string that identifies the client application.
        /// </summary>
        [System.ComponentModel.Description("Gets or sets the user agent string that identifies the client application.")]
        public string? UserAgent { get; set; }
        /// <summary> 
        /// Gets or sets the timestamp indicating when the connection was established.
        /// </summary>
        [System.ComponentModel.Description("Gets or sets the timestamp indicating when the connection was established.")]
        public DateTimeOffset? Connected { get; set; }
    }

}