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

using Gurux.DLMS.AMI.Shared.Enums;

namespace Gurux.DLMS.AMI.Server.Triggers
{
    /// <summary>
    /// Device trigger type.
    /// </summary>
    public class DeviceTrigger : ITriggerAction
    {
        /// <inheritdoc cref="ITriggerAction.Name"/>
        public string Name => "Device";

        /// <inheritdoc cref="ITriggerAction.ConfigurationUI"/>
        public Type? ConfigurationUI => null;

        /// <inheritdoc cref="ITriggerAction.Icon"/>
        public string? Icon => "oi oi-tablet";

        /// <summary>
        /// A new device is added.
        /// </summary>
        /// <remarks>
        /// This trigger can be used to update the block cipher and authentication keys when the connection is stableished for the first time.
        /// </remarks>
        public static string Add = "Add";
        /// <summary>
        /// The connected is established to the meter for the first time.
        /// </summary>
        /// <remarks>
        /// This trigger can be used to update the block cipher and authentication keys when the connection is stableished for the first time.
        /// </remarks>
        public static string Initialize = "Initialize";
        /// <summary>
        /// The connected is established to the meter.
        /// </summary>
        /// <remarks>
        /// This trigger can be used to update the block cipher and authentication keys when the connection is stableished for the first time.
        /// </remarks>
        /// <remarks>
        /// This trigger can be used to update the clock for the meter.
        /// </remarks>
        public static string Connected = "Connected";
        /// <summary>
        /// The meter is disconnecing.
        /// </summary>
        public static string Disconnecting = "Disconnecting";
        /// <summary>
        /// Device is updated.
        /// </summary>
        public static string Updated = "Updated";
        /// <summary>
        /// Device error is occurred.
        /// </summary>
        public static string Error = "Error";
        /// <summary>
        /// Device is removed.
        /// </summary>
        public static string Removed = "Removed";
    }
}
