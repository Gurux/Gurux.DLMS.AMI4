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

using Gurux.DLMS.AMI.Shared.Enums;

namespace Gurux.DLMS.AMI.Server.Triggers
{
    /// <summary>
    /// Agent trigger type.
    /// </summary>
    public class AgentTrigger : ITriggerAction
    {
        /// <summary>Published agent.connected event.</summary>
        [GXTriggerEvent("agent.connected")]
        public const string EventConnected = "agent.connected";

        /// <summary>Published agent.disconnected event.</summary>
        [GXTriggerEvent("agent.disconnected")]
        public const string EventDisconnected = "agent.disconnected";

        /// <inheritdoc />
        public string Name => "Agent";

        /// <inheritdoc />
        public Type? ConfigurationUI => null;

        /// <inheritdoc />
        public string? Icon => "oi oi-wifi";

        /// <summary>
        /// A new agent is added.
        /// </summary>
        public const string Add = "Add";
        /// <summary>
        /// Agent is updated.
        /// </summary>
        public const string Updated = "Updated";
        /// <summary>
        /// Agent error is occurred.
        /// </summary>
        public const string Error = "Error";
        /// <summary>
        /// Agent is removed.
        /// </summary>
        public const string Removed = "Removed";
        /// <summary>
        /// Agent is detected.
        /// </summary>
        public const string Detected = "Detected";
    }
}
