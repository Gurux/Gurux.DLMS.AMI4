﻿//
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
    /// Notify settings.
    /// </summary>
    public class NotifySettings
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public NotifySettings()
        {
        }

        /// <summary>
        /// Is notify active.
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Amount of the notify threads.
        /// </summary>
        public int Threads { get; set; } = 100;

        /// <summary>
        /// Media type.
        /// </summary>
        public string MediaType
        {
            get;
            set;
        } = string.Empty;

        /// <summary>
        /// Media settings as a string.
        /// </summary>
        public string? MediaSettings
        {
            get;
            set;
        }

        /// <summary>
        /// Interface type.
        /// </summary>
        public int Interface { get; set; }

        /// <summary>
        /// Use logical name referencing.
        /// </summary>
        public bool UseLogicalNameReferencing { get; set; } = true;

        /// <summary>
        /// Expiration time in seconds.
        /// </summary>
        /// <remarks>
        /// Sometimes only part of the push message is received. Received data is clear after expiration time.
        /// </remarks>
        public int ExpirationTime { get; set; } = 10;

        /// <summary>
        /// Device name method is used to to generate device name for the new device.
        /// </summary>
        /// <remarks>
        /// This can be used to get a new device when 
        /// a new meter is connected.
        /// </remarks>
        public Guid? DeviceNameMethod { get; set; }

        /// <summary>
        /// Device template method is used to to get 
        /// device template type for the new device.
        /// </summary>
        /// <remarks>
        /// This can be used to create a new device when 
        /// a connection is established and if the meter isn't found.
        /// </remarks>
        public Guid? DeviceTemplateMethod { get; set; }

        /// <summary>
        /// Default device template is used to to get 
        /// default device template type for the new device.
        /// </summary>
        /// <remarks>
        /// This can be used to create a new device when 
        /// a connection is established and if the meter isn't found
        /// and all meters are using the same device template type.
        /// </remarks>
        public Guid? DefaultDeviceTemplate { get; set; }

        /// <summary>
        /// Script method is used to parse received notification message.
        /// </summary>
        public Guid? ScriptMethod { get; set; }

        /// <summary>
        /// Used trace level.
        /// </summary>
        public TraceLevel TraceLevel
        {
            get;
            set;
        }

        /// <summary>
        /// How long the the connection is keeped up in seconds.
        /// </summary>
        /// <remarks>
        /// Auto connect devices can use this so 
        /// connection is not closed right after meter has read.
        /// If value is zero, the connection is closed without delay.
        /// If value is -1 the connection is not closed.
        /// </remarks>
        public int? ConnectionUpTime { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Helpers.GetProperties(this);
        }
    }
}
