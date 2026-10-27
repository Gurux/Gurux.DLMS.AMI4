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
    /// Defines connection and worker settings for a Gurux DLMS AMI agent.
    /// </summary>
    public class AgentOptions
    {
        /// <summary>
        /// Gets or sets the unique identifier of the agent.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the base address of the Gurux DLMS AMI server.
        /// </summary>
        /// <value>The server address. The default is <c>https://ami.gurux.fi</c>.</value>
        public string Address { get; set; } = "https://ami.gurux.fi";

        /// <summary>
        /// Gets or sets the personal access token used to authenticate with the server.
        /// </summary>
        /// <remarks>
        /// The agent can access meters that belong to the token owner.
        /// Treat this value as a secret.
        /// </remarks>
        public string Token { get; set; } = "";

        /// <summary>
        /// Gets or sets the diagnostic trace level for the agent.
        /// </summary>
        public TraceLevel TraceLevel { get; set; }

        /// <summary>
        /// Gets or sets the agent version to run.
        /// </summary>
        /// <remarks>
        /// If the value is <see langword="null"/>, the default agent version is used.
        /// </remarks>
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets the settings for the meter reader worker.
        /// </summary>
        public ReaderSettings? ReaderSettings { get; set; }

        /// <summary>
        /// Gets or sets the listener settings for incoming meter connections.
        /// </summary>
        public ListenerSettings? ListenerSettings { get; set; }

        /// <summary>
        /// Gets or sets the settings for receiving notification, event, or push messages.
        /// </summary>
        public NotifySettings? NotifySettings { get; set; }

        /// <summary>
        /// Gets or sets the settings for the worker that retrieves and parses stored raw notifications.
        /// </summary>
        public RawNotificationSettings? RawNotificationSettings { get; set; }

        /// <summary>
        /// Gets or sets the settings for accepting gateway connections.
        /// </summary>
        public GatewaySettings? GatewaySettings { get; set; }

        /// <summary>
        /// Gets or sets the available serial ports reported by the agent.
        /// </summary>
        public string? SerialPorts { get; set; }

        /// <summary>
        /// Gets or sets the serial port selected for meter communication.
        /// </summary>
        public string? SerialPort { get; set; }

        /// <summary>
        /// Returns a textual representation of the agent settings.
        /// </summary>
        /// <returns>The property descriptions produced by <see cref="Helpers.GetProperties"/>.</returns>
        public override string ToString()
        {
            return Helpers.GetProperties(this);
        }
    }
}
