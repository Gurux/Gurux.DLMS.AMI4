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

using System.Xml.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Enums
{
    /// <summary>The connection and task execution state of a gateway.</summary>
    [System.ComponentModel.Description("The connection and task execution state of a gateway.")]
    public enum GatewayStatus : byte
    {
        /// <summary>
        /// Gateway is disconnected.
        /// </summary>
        [System.ComponentModel.Description("Gateway is disconnected.")]
        [XmlEnum("0")]
        Offline = 0,
        /// <summary>
        /// Gateway is connected.
        /// </summary>
        [System.ComponentModel.Description("Gateway is connected.")]
        [XmlEnum("1")]
        Connected = 1,
        /// <summary>
        /// Gateway is idle.
        /// </summary>
        [System.ComponentModel.Description("Gateway is idle.")]
        [XmlEnum("2")]
        Idle = 2,
        /// <summary>
        /// Gateway is processing the task.
        /// </summary>
        [System.ComponentModel.Description("Gateway is processing the task.")]
        [XmlEnum("3")]
        Process = 3,
        /// <summary>The gateway is in an error state.</summary>
        [System.ComponentModel.Description("The gateway is in an error state.")]
        [XmlEnum("4")]
        Error = 4,
        /// <summary>
        /// Indicates that the gateway has been detected.
        /// </summary>
        [System.ComponentModel.Description("Indicates that the gateway has been detected.")]
        [XmlEnum("5")]
        Detected = 5
    }
}
