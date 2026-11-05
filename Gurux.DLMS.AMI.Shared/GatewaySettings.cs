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

using System.Diagnostics;
using System.Text;

namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// Settings for dynamic TCP/IP gateway connections
    /// initiated by the gateway to the agent.
    /// </summary>
    [System.ComponentModel.Description("Settings for dynamic TCP/IP gateway connections initiated by the gateway to the agent.")]
    public class GatewaySettings
    {
        /// <summary>Initializes a new instance of GatewaySettings.</summary>
        [System.ComponentModel.Description("Initializes a new instance of GatewaySettings.")]
        public GatewaySettings()
        {
        }

        /// <summary>
        /// Whether the agent accepts gateway connections.
        /// </summary>
        [System.ComponentModel.Description("Whether the agent accepts gateway connections.")]
        public bool Enabled { get; set; }

        /// <summary>Selected gateway identifier.</summary>
        /// <remarks>
        /// This is used when the gateway is selected from the list of saved gateways.
        /// </remarks>
        [System.ComponentModel.Description("Selected gateway identifier.")]
        public Guid? Id { get; set; }

        /// <summary>
        /// Time in seconds to wait for the gateway identification message.
        /// </summary>
        [System.ComponentModel.Description("Time in seconds to wait for the gateway identification message.")]
        public int IdentifyWaitTime { get; set; }

        /// <summary>Serialized settings for the communication media.</summary>
        [System.ComponentModel.Description("Serialized settings for the communication media.")]
        public string? MediaSettings { get; set; }

        /// <summary>Trace level used for communication diagnostics.</summary>
        [System.ComponentModel.Description("Trace level used for communication diagnostics.")]
        public TraceLevel TraceLevel { get; set; }

        /// <summary>
        /// Absolute expiration time in seconds. 
        /// After that time the agent closes the idle connection.
        /// </summary>
        [System.ComponentModel.Description("Absolute expiration time in seconds. After that time the agent closes the idle connection.")]
        public int ExpirationTime { get; set; }

        /// <summary>
        /// Identifier of the script method used to identify a connecting gateway.
        /// </summary>
        [System.ComponentModel.Description("Identifier of the script method used to identify a connecting gateway.")]
        public Guid? ScriptMethod { get; set; }

        /// <summary>
        /// Gateway script.
        /// </summary>
        [System.ComponentModel.Description("Gateway script.")]
        public byte[]? Script { get; set; }

        /// <summary>
        /// Identifier of the saved gateway script.
        /// </summary>
        [System.ComponentModel.Description("Identifier of the saved gateway script.")]
        public Guid? ScriptId { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            StringBuilder sb = new();
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
