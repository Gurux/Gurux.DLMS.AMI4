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

using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// Provides operations for managing active device connections.
    /// </summary>
    /// <remarks>
    /// Implementations maintain an in-memory view of currently active connections and
    /// offer filtered listing support for API responses.
    /// </remarks>
    public interface IGXConnectionManager
    {
        /// <summary>
        /// Adds a new connection to the system using the specified connection information.
        /// </summary>
        /// <remarks>The connection information provided in <paramref name="info"/> is used to configure
        /// and register the connection.  Ensure that all required fields in the <see cref="GXConnectionInfo"/> object
        /// are properly populated before calling this method.</remarks>
        /// <param name="info">The connection information to be added. This parameter must not be <see langword="null"/>.</param>
        void AddConnection(GXConnectionInfo info);

        /// <summary>
        /// Removes the connection with the specified connection ID.
        /// </summary>
        /// <remarks>This method removes the connection identified by the specified <paramref
        /// name="connectionId"/> from the system. If the connection does not exist, the method has no effect.</remarks>
        /// <param name="connectionId">The unique identifier of the connection to remove. Cannot be null or empty.</param>
        void RemoveConnection(string connectionId);

        /// <summary>
        /// Retrieves active connections using optional filter and paging information.
        /// </summary>
        /// <param name="request">Optional list request that defines filtering and pagination.</param>
        /// <param name="response">Optional response context populated with paging metadata.</param>
        /// <returns>Matching active connections. Returns an empty sequence if no matches are found.</returns>
        IEnumerable<GXConnectionInfo> List(
            ListConnection? request = null,
            ListConnectionResponse? response = null);
    }
}