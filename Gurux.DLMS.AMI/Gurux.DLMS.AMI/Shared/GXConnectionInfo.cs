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

namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// Represents information about a connection, including its identifier, associated user, and metadata.
    /// </summary>
    /// <remarks>This class provides details about a connection, such as the connection ID, user information, 
    /// IP address, user agent, and the timestamp when the connection was established. It is commonly  used to track and
    /// manage active connections in a system.</remarks>
    public class GXConnectionInfo
    {
        /// <summary>
        /// Gets or sets the unique identifier for the connection.
        /// </summary>
        public string? ConnectionId { get; set; }
        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Gets or sets the user email address.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Gets or sets the IP address associated with the current instance.
        /// </summary>
        public string? IpAddress { get; set; }
        /// <summary>
        /// Gets or sets the user agent string that identifies the client application.
        /// </summary>
        public string? UserAgent { get; set; }
        /// <summary> 
        /// Gets or sets the timestamp indicating when the connection was established.
        /// </summary>
        public DateTimeOffset? Connected { get; set; }
    }

}