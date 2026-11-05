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

using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// Provides operations for tracking active client connections.
    /// </summary>
    /// <remarks>
    /// Implementations maintain an in-memory view of currently active connections and
    /// populate connection list API responses.
    /// </remarks>
    [System.ComponentModel.Description("Provides operations for tracking active client connections.")]
    public interface IGXConnectionManager
    {
        /// <summary>Registers connection information, replacing an existing entry with the same connection identifier.</summary>
        /// <remarks>The connection information provided in <paramref name="info"/> is used to configure
        /// and register the connection.  Ensure that all required fields in the <see cref="GXConnectionInfo"/> object
        /// are properly populated before calling this method.</remarks>
        /// <param name="info">The connection information to be added. This parameter must not be <see langword="null"/>.</param>
        [System.ComponentModel.Description("Registers connection information, replacing an existing entry with the same connection identifier.")]
        void AddConnection(GXConnectionInfo info);

        /// <summary>Removes the connection with the specified connection identifier.</summary>
        /// <remarks>This method removes the connection identified by the specified <paramref
        /// name="connectionId"/> from the system. If the connection does not exist, the method has no effect.</remarks>
        /// <param name="connectionId">The unique identifier of the connection to remove. Cannot be null or empty.</param>
        [System.ComponentModel.Description("Removes the connection with the specified connection identifier.")]
        void RemoveConnection(string connectionId);

        /// <summary>
        /// Retrieves the currently tracked active connections.
        /// </summary>
        /// <param name="request">Optional list request. The current in-memory implementation ignores its controls.</param>
        /// <param name="response">Optional response populated with the connection snapshot and its count.</param>
        /// <returns>A snapshot of the active connections, or an empty sequence when none are tracked.</returns>
        [System.ComponentModel.Description("Retrieves the currently tracked active connections.")]
        IEnumerable<GXConnectionInfo> List(
            ListConnection? request = null,
            ListConnectionResponse? response = null);
    }
}