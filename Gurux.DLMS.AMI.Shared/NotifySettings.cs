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
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using System.Diagnostics;
using System.Text;

namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// Notification settings.
    /// </summary>
    [System.ComponentModel.Description("Notification settings.")]
    public class NotifySettings
    {
        /// <summary>Initializes a new instance of NotifySettings.</summary>
        [System.ComponentModel.Description("Initializes a new instance of NotifySettings.")]
        public NotifySettings()
        {
        }

        /// <summary>
        /// Whether the agent receives meter notifications.
        /// </summary>
        [System.ComponentModel.Description("Whether the agent receives meter notifications.")]
        public bool Enabled { get; set; }

        /// <summary>
        /// Number of notification threads. The default is 100.
        /// </summary>
        [System.ComponentModel.Description("Number of notification threads. The default is 100.")]
        public int Threads { get; set; } = 100;

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
        /// Gets or sets a value indicating whether the received data should be forwarded
        /// exactly as it was received, without any processing, transformation, or validation.
        /// </summary>
        [System.ComponentModel.Description("Gets or sets a value indicating whether the received data should be forwarded exactly as it was received, without any processing, transformation, or validation.")]
        public bool PassThroughData { get; set; }


        /// <summary>
        /// Whether to use DLMS logical name referencing. The default is true.
        /// </summary>
        [System.ComponentModel.Description("Whether to use DLMS logical name referencing. The default is true.")]
        public bool UseLogicalNameReferencing { get; set; } = true;

        /// <summary>
        /// Expiration time in seconds.
        /// </summary>
        /// <remarks>
        /// Clears an incomplete push message after this interval. The default is 10 seconds.
        /// </remarks>
        [System.ComponentModel.Description("Expiration time in seconds.")]
        public int ExpirationTime { get; set; } = 10;

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
        /// Identifier of the script method used to parse received notifications.
        /// </summary>
        [System.ComponentModel.Description("Identifier of the script method used to parse received notifications.")]
        public Guid? ScriptMethod { get; set; }

        /// <summary>
        /// Trace level used for communication diagnostics.
        /// </summary>
        [System.ComponentModel.Description("Trace level used for communication diagnostics.")]
        public TraceLevel TraceLevel { get; set; }

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
        /// Notify script.
        /// </summary>
        [System.ComponentModel.Description("Notify script.")]
        public byte[]? Script { get; set; }

        /// <summary>
        /// Identifier of the saved notification script.
        /// </summary>
        [System.ComponentModel.Description("Identifier of the saved notification script.")]
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
