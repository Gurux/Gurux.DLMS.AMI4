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
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text;

namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>Configures an agent that retrieves stored raw notifications and parses them into device data.</summary>
    /// <remarks>
    /// The receiving agent forwards transport bytes using NotifySettings.PassThroughData.
    /// These settings belong to the separate processing agent; no listening socket or serial port is configured here.
    /// Chunks from one connection are processed in sequence by the same worker. Each invocation receives
    /// a GXRawNotification, which may contain only part of a TCP message. The script's assembly load context
    /// is retained for that connection lease, so static parser state can accumulate partial messages.
    /// </remarks>
    [System.ComponentModel.Description("Configures an agent that retrieves stored raw notifications and parses them into device data.")]
    public class RawNotificationSettings
    {
        /// <summary>Whether raw notification processing is enabled for this agent. Disabled by default.</summary>
        [System.ComponentModel.Description("Whether raw notification processing is enabled for this agent. Disabled by default.")]
        public bool Enabled { get; set; }

        /// <summary>Maximum number of connections to process concurrently. The default is 100.</summary>
        /// <remarks>Each worker retains its connection's parser state until processing finishes.</remarks>
        [System.ComponentModel.Description("Maximum number of connections to process concurrently. The default is 100.")]
        [Range(1, int.MaxValue)]
        public int Threads { get; set; } = 100;

        /// <summary>Optional script method that names a device discovered while parsing raw data.</summary>
        [System.ComponentModel.Description("Optional script method that names a device discovered while parsing raw data.")]
        public Guid? DeviceNameMethod { get; set; }

        /// <summary>Optional script method that selects a template for a device discovered while parsing.</summary>
        [System.ComponentModel.Description("Optional script method that selects a template for a device discovered while parsing.")]
        public Guid? DeviceTemplateMethod { get; set; }

        /// <summary>Fallback device template when a notification identifies a previously unknown device.</summary>
        /// <remarks>Null means no default template has been selected.</remarks>
        [System.ComponentModel.Description("Fallback device template when a notification identifies a previously unknown device.")]
        public Guid? DefaultDeviceTemplate { get; set; }

        /// <summary>Identifier of the script method selected to parse stored raw notification bytes.</summary>
        /// <remarks>
        /// Required when processing is enabled. The method takes one GXRawNotification parameter and may
        /// return a Task. It must handle partial chunks and ConnectionClosed, update the intended device
        /// objects and attributes, and tolerate replay after a lease expires (notification IDs stay stable).
        /// </remarks>
        [System.ComponentModel.Description("Identifier of the script method selected to parse stored raw notification bytes.")]
        public Guid? ScriptMethod { get; set; }

        /// <summary>Diagnostic logging level for raw notification processing.</summary>
        [System.ComponentModel.Description("Diagnostic logging level for raw notification processing.")]
        public TraceLevel TraceLevel { get; set; }

        /// <summary>Optional compiled parser script supplied to the agent at runtime.</summary>
        /// <remarks>Binary script contents are omitted from the diagnostic string.</remarks>
        [System.ComponentModel.Description("Optional compiled parser script supplied to the agent at runtime.")]
        public byte[]? Script { get; set; }

        /// <summary>Identifier of the saved script containing the selected parser method.</summary>
        [System.ComponentModel.Description("Identifier of the saved script containing the selected parser method.")]
        public Guid? ScriptId { get; set; }

        /// <summary>Returns readable settings for diagnostics without including the script binary.</summary>
        [System.ComponentModel.Description("Returns readable settings for diagnostics without including the script binary.")]
        public override string ToString()
        {
            StringBuilder sb = new();
            foreach (var item in Helpers.GetDictionaryProperties(this))
            {
                if (item.Key == nameof(Script))
                {
                    continue;
                }

                if (sb.Length != 0)
                {
                    sb.Append(", ");
                }

                sb.Append(item.Key).Append(": ").Append(item.Value);
            }
            return sb.ToString();
        }
    }
}
