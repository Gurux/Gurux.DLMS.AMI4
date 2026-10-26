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
using System.Runtime.Serialization;
using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// List active connections.
    /// </summary>
    /// <remarks>The current in-memory connection manager ignores these request controls and returns all tracked connections.</remarks>
    [DataContract]
    [Description("List active connections.")]
    public class ListConnection
    {
        /// <summary>
        /// Requested connection criteria, such as a user identifier.
        /// </summary>
        public GXConnectionInfo? Filter { get; set; }


        /// <summary>
        /// Whether to request connections for all users as an administrator.
        /// </summary>
        /// <remarks>
        /// The current connection manager returns all tracked connections regardless of this flag.
        /// </remarks>
        public bool AllUsers { get; set; }

        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Requested maximum number of connections to retrieve.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Selected extra information.
        /// </summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        public IEnumerable<string>? Select { get; set; }

        /// <summary>
        /// Name of the property to sort by.
        /// </summary>
        /// <remarks>
        /// Uses the endpoint's default ordering when not specified.
        /// </remarks>
        /// <seealso cref="Descending"/>
        public string? OrderBy { get; set; }

        /// <summary>
        /// Whether to sort results in descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        public bool Descending { get; set; }

        /// <summary>
        /// Identifiers to include in the results.
        /// </summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        public IEnumerable<Guid>? Exclude { get; set; }
    }

    /// <summary>
    /// List active connections response.
    /// </summary>
    [Description("List active connections response.")]
    [DataContract]
    public class ListConnectionResponse
    {
        /// <summary>
        /// Active connections.
        /// </summary>
        [DataMember]
        [Description("Active connections.")]
        public IEnumerable<GXConnectionInfo>? Connections { get; set; }

        /// <summary>
        /// Amount of the active connections.
        /// </summary>
        /// <remarks>
        /// The current connection manager returns the size of its in-memory connection snapshot.
        /// This count is the number of connections in the response.
        /// </remarks>
        [DataMember]
        [Description("Amount of the active connections.")]
        public int Count { get; set; }
    }
}
