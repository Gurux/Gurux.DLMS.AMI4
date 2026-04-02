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

using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Collections.Concurrent;
namespace Gurux.DLMS.AMI.Server.Services
{
    /// <summary>
    /// Manages a collection of active connections and provides functionality to add, remove, and retrieve connections.
    /// </summary>
    /// <remarks>This class maintains a thread-safe collection of connections and raises the <see
    /// cref="ConnectionsChanged"/> event  whenever the collection is modified. It is designed to be used in scenarios
    /// where managing and monitoring  multiple connections is required.</remarks>
    public class GXConnectionManager : IGXConnectionManager
    {
        private readonly ConcurrentDictionary<string, GXConnectionInfo> _connections = new();

        /// <inheritdoc/>
        public void AddConnection(GXConnectionInfo info)
        {
            _connections[info.ConnectionId] = info;
        }

        /// <inheritdoc/>
        public void RemoveConnection(string connectionId)
        {
            _connections.TryRemove(connectionId, out _);
        }

        /// <inheritdoc/>
        public GXConnectionInfo[] List(ListConnection? request,
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