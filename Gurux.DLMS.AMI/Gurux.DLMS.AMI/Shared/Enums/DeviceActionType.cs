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

namespace Gurux.DLMS.AMI.Shared.Enums
{
    /// <summary>
    /// Device action types.
    /// </summary>
    [Flags]
    public enum DeviceActionType : byte
    {
        /// <summary>
        /// Connecting to the meter.
        /// </summary>
        [XmlEnum("1")]
        Connect = 0x01,
        /// <summary>
        /// Disconnecting from the meter.
        /// </summary>
        [XmlEnum("2")]
        Disconnect = 0x2,
        /// <summary>
        /// Read the meter.
        /// </summary>
        [XmlEnum("4")]
        Read = 0x4,
        /// <summary>
        /// Write the meter.
        /// </summary>
        [XmlEnum("8")]
        Write = 0x8,
        /// <summary>
        /// Invoke the action.
        /// </summary>
        [XmlEnum("16")]
        Action = 0x10,
        /// <summary>
        /// Error has occurred.
        /// </summary>
        [XmlEnum("127")]
        Error = 0x73
    }
}
