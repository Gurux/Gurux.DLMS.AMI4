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

namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// Settings for the agent that reads meters and retrieves tasks from the server.
    /// </summary>
    [System.ComponentModel.Description("Settings for the agent that reads meters and retrieves tasks from the server.")]
    public class ReaderSettings
    {
        /// <summary>
        /// Whether the meter reader is enabled. The default is true.
        /// </summary>
        [System.ComponentModel.Description("Whether the meter reader is enabled. The default is true.")]
        public bool Enabled { get; set; } = true;
        /// <summary>
        /// Number of meter reading threads. The default is 10.
        /// </summary>
        [System.ComponentModel.Description("Number of meter reading threads. The default is 10.")]
        public int Threads { get; set; } = 10;
        /// <summary>
        /// Time in seconds to wait for a new task before retrying.
        /// </summary>
        /// <remarks>
        /// Limits the wait when the server restarts so the reader does not have to
        /// wait for the session to time out.
        /// </remarks>
        [System.ComponentModel.Description("Time in seconds to wait for a new task before retrying.")]
        public int TaskWaitTime { get; set; } = 100000;
        /// <summary>
        /// How often reader sends alive message to the server in minutes.
        /// If value is zero only start message is sent and no alive messages are sent.
        /// </summary>
        [System.ComponentModel.Description("How often reader sends alive message to the server in minutes. If value is zero only start message is sent and no alive messages are sent.")]
        public int AliveTime { get; set; } = 2;
        /// <summary>Trace level used for communication diagnostics.</summary>
        [System.ComponentModel.Description("Trace level used for communication diagnostics.")]
        public TraceLevel TraceLevel { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Helpers.GetProperties(this);
        }
    }
}
