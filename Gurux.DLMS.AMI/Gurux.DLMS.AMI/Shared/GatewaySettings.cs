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
    /// Gateway settings. Gatewayis used with dynamic TCP/IP connections
    /// where the gateway establish the connection to the agent.
    /// </summary>
    public class GatewaySettings
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GatewaySettings()
        {
        }

        /// <summary>
        /// Is gateway of the agent active.
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// How long in seconds the gateway identify message is waited from the gateway.
        /// </summary>
        public int IdentifyWaitTime { get; set; }

        /// <summary>
        /// Media settings as a string.
        /// </summary>
        public string? MediaSettings
        {
            get;
            set;
        }

        /// <summary>
        /// Used trace level.
        /// </summary>
        public TraceLevel TraceLevel
        {
            get;
            set;
        }

        /// <summary>
        /// Absolute expiration time in seconds. 
        /// After that time the agent closes the idle connection.
        /// </summary>
        public int ExpirationTime
        {
            get;
            set;
        }

        /// <summary>
        /// Gateway script is used to identify the connecting gateway.
        /// </summary>
        public Guid? GatewayScriptMethod { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Helpers.GetProperties(this);
        }
    }
}
