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
    public class RawNotificationSettings
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public RawNotificationSettings()
        {
        }

        /// <summary>
        /// Whether the agent receives meter notifications.
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Number of notification threads. The default is 100.
        /// </summary>
        public int Threads { get; set; } = 100;

        /// <summary>
        /// Identifier of the script method that generates names for newly discovered devices.
        /// </summary>
        /// <remarks>
        /// This can be used to get a new device when 
        /// a new meter is connected.
        /// </remarks>
        public Guid? DeviceNameMethod { get; set; }

        /// <summary>
        /// Identifier of the script method that selects a template for a newly discovered device.
        /// </summary>
        /// <remarks>
        /// This can be used to create a new device when 
        /// a connection is established and if the meter isn't found.
        /// </remarks>
        public Guid? DeviceTemplateMethod { get; set; }

        /// <summary>
        /// Identifier of the default template for newly discovered devices.
        /// </summary>
        /// <remarks>
        /// This can be used to create a new device when 
        /// a connection is established and if the meter isn't found
        /// and all meters are using the same device template type.
        /// </remarks>
        public Guid? DefaultDeviceTemplate { get; set; }

        /// <summary>
        /// Identifier of the script method used to parse received notifications.
        /// </summary>
        public Guid? ScriptMethod { get; set; }

        /// <summary>
        /// Trace level used for communication diagnostics.
        /// </summary>
        public TraceLevel TraceLevel { get; set; }

        /// <summary>
        /// Notify script.
        /// </summary>
        public byte[]? Script { get; set; }

        /// <summary>
        /// Identifier of the saved notification script.
        /// </summary>
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
