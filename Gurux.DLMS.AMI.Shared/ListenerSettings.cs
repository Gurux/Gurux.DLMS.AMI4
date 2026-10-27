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
    /// Listener settings. Listener is used with dynamic TCP/IP connections
    /// where the meter makes the connection to the server.
    /// </summary>
    [System.ComponentModel.Description("Listener settings. Listener is used with dynamic TCP/IP connections where the meter makes the connection to the server.")]
    public class ListenerSettings
    {
        /// <summary>Initializes a new instance of ListenerSettings.</summary>
        [System.ComponentModel.Description("Initializes a new instance of ListenerSettings.")]
        public ListenerSettings()
        {
        }

        /// <summary>
        /// Whether the agent accepts incoming meter connections.
        /// </summary>
        [System.ComponentModel.Description("Whether the agent accepts incoming meter connections.")]
        public bool Enabled { get; set; }

        /// <summary>
        /// Number of listener threads. The default is 100.
        /// </summary>
        [System.ComponentModel.Description("Number of listener threads. The default is 100.")]
        public int Threads { get; set; } = 100;

        /// <summary>
        /// How long the identify message is waited from the meter.
        /// </summary>
        /// <remarks>
        /// If value is zero, the meter doesn't send the identify message.
        /// </remarks>
        [System.ComponentModel.Description("How long the identify message is waited from the meter.")]
        public int IdentifyWaitTime { get; set; }

        /// <summary>
        /// Whether the DLMS connection is pre-established.
        /// </summary>
        [System.ComponentModel.Description("Whether the DLMS connection is pre-established.")]
        public bool PreEstablished { get; set; }

        /// <summary>
        /// Media type.
        /// </summary>
        [System.ComponentModel.Description("Media type.")]
        public string MediaType { get; set; } = string.Empty;

        /// <summary>Serialized settings for the communication media.</summary>
        [System.ComponentModel.Description("Serialized settings for the communication media.")]
        public string? MediaSettings { get; set; }

        /// <summary>
        /// DLMS interface type encoded as an integer.
        /// </summary>
        [System.ComponentModel.Description("DLMS interface type encoded as an integer.")]
        public int Interface { get; set; }

        /// <summary>
        /// Whether to use DLMS logical name referencing. The default is true.
        /// </summary>
        [System.ComponentModel.Description("Whether to use DLMS logical name referencing. The default is true.")]
        public bool UseLogicalNameReferencing { get; set; } = true;

        /// <summary>
        /// DLMS client address. The default is 16.
        /// </summary>
        [System.ComponentModel.Description("DLMS client address. The default is 16.")]
        public int ClientAddress { get; set; } = 0x10;

        /// <summary>
        /// DLMS server address. The default is 1.
        /// </summary>
        [System.ComponentModel.Description("DLMS server address. The default is 1.")]
        public int ServerAddress { get; set; } = 1;

        /// <summary>
        /// Authentication level.
        /// </summary>
        [System.ComponentModel.Description("Authentication level.")]
        public int Authentication { get; set; }

        /// <summary>
        /// Password.
        /// </summary>
        [System.ComponentModel.Description("Password.")]
        public string? Password { get; set; }

        /// <summary>
        /// Security.
        /// </summary>
        [System.ComponentModel.Description("Security.")]
        public int Security { get; set; }

        /// <summary>
        /// Logical name of the invocation counter.
        /// </summary>
        [System.ComponentModel.Description("Logical name of the invocation counter.")]
        public string? InvocationCounter { get; set; }

        /// <summary>
        /// Trace level used for communication diagnostics.
        /// </summary>
        [System.ComponentModel.Description("Trace level used for communication diagnostics.")]
        public TraceLevel TraceLevel { get; set; }

        /// <summary>
        /// Absolute expiration time in seconds.
        /// </summary>
        [System.ComponentModel.Description("Absolute expiration time in seconds.")]
        public int ExpirationTime { get; set; }

        /// <summary>
        /// Identifier of the script method that generates names for newly discovered devices.
        /// </summary>
        /// <remarks>
        /// This can be used to get a new device when 
        /// a new meter is connected.
        /// </remarks>
        [System.ComponentModel.Description("Identifier of the script method that generates names for newly discovered devices.")]
        public Guid? DeviceNameMethod { get; set; }

        /// <summary>
        /// Identifier of the script method that selects a template for a newly discovered device.
        /// </summary>
        /// <remarks>
        /// This can be used to create a new device when 
        /// a connection is established and if the meter isn't found.
        /// </remarks>
        [System.ComponentModel.Description("Identifier of the script method that selects a template for a newly discovered device.")]
        public Guid? DeviceTemplateMethod { get; set; }

        /// <summary>
        /// Identifier of the default template for newly discovered devices.
        /// </summary>
        /// <remarks>
        /// This can be used to create a new device when 
        /// a connection is established and if the meter isn't found
        /// and all meters are using the same device template type.
        /// </remarks>
        [System.ComponentModel.Description("Identifier of the default template for newly discovered devices.")]
        public Guid? DefaultDeviceTemplate { get; set; }

        /// <summary>
        /// Identifier of the script method used to identify a connecting meter.
        /// </summary>
        [System.ComponentModel.Description("Identifier of the script method used to identify a connecting meter.")]
        public Guid? ScriptMethod { get; set; }

        /// <summary>
        /// Gateway script is used to identify the connecting gateway.
        /// </summary>
        /// <remarks>
        /// This is obsolete.
        /// </remarks>
        [System.ComponentModel.Description("Gateway script is used to identify the connecting gateway.")]
        [Obsolete("Use Gateway settings instead of.")]
        public Guid? GatewayScriptMethod { get; set; }

        /// <summary>
        /// Time in seconds to retain the connection after reading the meter.
        /// </summary>
        /// <remarks>
        /// Automatically connecting devices can use this to keep the
        /// connection open after a read completes.
        /// If value is zero, the connection is closed without delay.
        /// If the value is -1, the connection remains open.
        /// </remarks>
        [System.ComponentModel.Description("Time in seconds to retain the connection after reading the meter.")]
        public int? ConnectionUpTime { get; set; }

        /// <summary>
        /// Listener script.
        /// </summary>
        [System.ComponentModel.Description("Listener script.")]
        public byte[]? Script { get; set; }

        /// <summary>
        /// Identifier of the saved listener script.
        /// </summary>
        [System.ComponentModel.Description("Identifier of the saved listener script.")]
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
