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

using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Collections.Concurrent;
namespace Gurux.DLMS.AMI.Server.Services
{
    /// <summary>
    /// Manages a collection of active connections and provides functionality to add, remove, and retrieve connections.
    /// </summary>
    /// <remarks>Stores active connections in a thread-safe collection.</remarks>
    public class GXConnectionManager : IGXConnectionManager
    {
        private readonly ConcurrentDictionary<string, GXConnectionInfo> _connections = new();

        /// <inheritdoc/>
        public void AddConnection(GXConnectionInfo info)
        {
            ArgumentException.ThrowIfNullOrEmpty(info.ConnectionId);
            _connections[info.ConnectionId] = info;
        }

        /// <inheritdoc/>
        public void RemoveConnection(string connectionId)
        {
            _connections.TryRemove(connectionId, out _);
        }

        /// <inheritdoc/>
        public IEnumerable<GXConnectionInfo> List(ListConnection? request,
            ListConnectionResponse? response = null)
        {
            var connections = _connections.Values.ToArray();
            if (response != null)
            {
                response.Connections = connections;
                response.Count = connections.Length;
            }
            return connections;
        }
    }
}
