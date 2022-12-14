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
using System.Diagnostics;

namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// Listener settings. Listener is used with dynamic TCP/IP connections
    /// where the meter makes the connection to the server.
    /// </summary>
    public class ListenerSettings
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public ListenerSettings()
        {
        }

        /// <summary>
        /// Is listener active.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Listener port. Server waits meters to connect to this port..
        /// </summary>
        public int Port { get; set; } = 4059;

        /// <summary>
        /// Network type. UDP = 1 and TCP = 2.
        /// </summary>
        public int NetworkType { get; set; } = 1;

        /// <summary>
        /// Interface type.
        /// </summary>
        public int Interface { get; set; }


        /// <summary>
        /// Use logical name referencing.
        /// </summary>
        public bool UseLogicalNameReferencing { get; set; } = true;

        /// <summary>
        /// Client address.
        /// </summary>
        public int ClientAddress { get; set; } = 0x10;

        /// <summary>
        /// Server address.
        /// </summary>
        public int ServerAddress { get; set; } = 1;

        /// <summary>
        /// Authentication level.
        /// </summary>
        public int Authentication { get; set; }

        /// <summary>
        /// Password.
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Security.
        /// </summary>
        public int Security { get; set; }

        /// <summary>
        /// Logical name of the invocation counter.
        /// </summary>
        public string? InvocationCounter { get; set; }

        /// <summary>
        /// Used trace level.
        /// </summary>
        public TraceLevel TraceLevel
        {
            get;
            set;
        }

        /// <summary>
        /// Listener script is used to establish the connection for the meter 
        /// and read values from the meter.
        /// </summary>
        public Guid? ScriptMethod { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Helpers.GetProperties(this);
        }

    }
}
