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
    /// Device group trigger type.
    /// </summary>
    public class DeviceGroupTrigger : ITriggerAction
    {
        /// <inheritdoc cref="ITriggerAction.Name"/>
        public string Name => "Device Group";

        /// <inheritdoc cref="ITriggerAction.ConfigurationUI"/>
        public Type? ConfigurationUI => null;

        /// <inheritdoc cref="ITriggerAction.Icon"/>
        public string? Icon => null;

        /// <summary>
        /// A new device group is added.
        /// </summary>
        public static string Add = "Add";
        /// <summary>
        /// Device group is updated.
        /// </summary>
        public static string Updated = "Updated";
        /// <summary>
        /// Device error is occurred on the device group devices.
        /// </summary>
        public static string Error = "Error";
        /// <summary>
        /// Device group is removed.
        /// </summary>
        public static string Removed = "Removed";
    }
}
