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
using Gurux.DLMS.AMI.Shared;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Agent.Shared
{
    /// <summary>
    /// Agent settings.
    /// </summary>
    public class AgentOptions
    {
        /// <summary>
        /// Unique agent ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gurux.DLMS.AMI server address. Agent is connecting to this address.
        /// </summary>
        public string Address { get; set; } = "https://ami.gurux.fi";

        /// <summary>
        /// Personal Access Token.
        /// </summary>
        /// <remarks>
        /// Agent can access meters that belongs for token owner.
        /// </remarks>
        public string Token { get; set; } = "";

        /// <summary>
        /// Trace level.
        /// </summary>
        public TraceLevel TraceLevel { get; set; }

        /// <summary>
        /// Agent version to run.
        /// </summary>
        /// <remarks>
        /// If version is null the default agent is used.
        /// </remarks>
        public string? Version { get; set; }

        /// <summary>
        /// Reader settings.
        /// </summary>
        [JsonIgnore]
        public ReaderSettings? ReaderSettings
        {
            get;
            set;
        }

        /// <summary>
        /// Listener settings. Agent waits server to connect for the listener.
        /// </summary>
        [JsonIgnore]
        public ListenerSettings? ListenerSettings
        {
            get;
            set;
        }

        /// <summary>
        /// Notify settings. Agent waits notify, event or push messages to this port.
        /// </summary>
        [JsonIgnore]
        public NotifySettings? NotifySettings
        {
            get;
            set;
        }

        /// <summary>
        /// Available serial ports.
        /// </summary>
        public string? SerialPorts
        {
            get;
            set;
        }

        /// <summary>
        /// Selected serial port.
        /// </summary>
        public string? SerialPort
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Helpers.GetProperties(this);
        }
    }
}
