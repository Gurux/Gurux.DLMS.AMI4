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
using System.Text;

namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// Settings for dynamic TCP/IP gateway connections
    /// initiated by the gateway to the agent.
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
        /// Whether the agent accepts gateway connections.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Selected gateway ID.
        /// </summary>
        /// <remarks>
        /// This is used when the gateway is selected from the list of saved gateways.
        /// </remarks>
        public Guid? Id { get; set; }

        /// <summary>
        /// Time in seconds to wait for the gateway identification message.
        /// </summary>
        public int IdentifyWaitTime { get; set; }

        /// <summary>
        /// Media settings as a string.
        /// </summary>
        public string? MediaSettings { get; set; }

        /// <summary>
        /// Used trace level.
        /// </summary>
        public TraceLevel TraceLevel { get; set; }

        /// <summary>
        /// Absolute expiration time in seconds. 
        /// After that time the agent closes the idle connection.
        /// </summary>
        public int ExpirationTime { get; set; }

        /// <summary>
        /// Identifier of the script method used to identify a connecting gateway.
        /// </summary>
        public Guid? ScriptMethod { get; set; }

        /// <summary>
        /// Gateway script.
        /// </summary>
        public byte[]? Script { get; set; }

        /// <summary>
        /// Identifier of the saved gateway script.
        /// </summary>
        public Guid? ScriptId { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            var list = Helpers.GetDictionaryProperties(this);
            foreach (var it in list)
            {
                if (sb.Length != 0)
                {
                    sb.Append(", ");
                }
                //Script is not shown in the string.
                if (it.Key != nameof(Script))
                {
                    sb.Append(it.Key + ": " + it.Value);
                }
            }
            return sb.ToString();
        }
    }
}
