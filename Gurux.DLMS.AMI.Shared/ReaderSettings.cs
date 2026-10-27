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
