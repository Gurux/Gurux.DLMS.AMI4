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
    /// <summary>Resource events that can cause a notification to be delivered.</summary>
    [System.ComponentModel.Description("Resource events that can cause a notification to be delivered.")]
    [Flags]
    public enum NotificationAction : UInt16
    {
        /// <summary>Do not send notifications for any action.</summary>
        [System.ComponentModel.Description("Do not send notifications for any action.")]
        [XmlEnum("0")]
        None = 0x0,
        /// <summary>Notify when an item is added.</summary>
        [System.ComponentModel.Description("Notify when an item is added.")]
        [XmlEnum("1")]
        Add = 0x1,
        /// <summary>Notify when an item is modified.</summary>
        [System.ComponentModel.Description("Notify when an item is modified.")]
        [XmlEnum("2")]
        Edit = 0x2,
        /// <summary>Notify when an item is removed.</summary>
        [System.ComponentModel.Description("Notify when an item is removed.")]
        [XmlEnum("4")]
        Remove = 0x4,
        /// <summary>Notify when items are cleared within a resource scope.</summary>
        [System.ComponentModel.Description("Notify when items are cleared within a resource scope.")]
        [XmlEnum("8")]
        Clear = 0x8,
        /// <summary>Notify when an item is executed.</summary>
        [System.ComponentModel.Description("Notify when an item is executed.")]
        [XmlEnum("16")]
        Execute = 0x10,
        /// <summary>Notify when execution reports an error.</summary>
        [System.ComponentModel.Description("Notify when execution reports an error.")]
        [XmlEnum("32")]
        Error = 0x20,
        /// <summary>Notify when an item is closed.</summary>
        [System.ComponentModel.Description("Notify when an item is closed.")]
        [XmlEnum("64")]
        Close = 0x40,
        /// <summary>Notify when an item's status changes.</summary>
        [System.ComponentModel.Description("Notify when an item's status changes.")]
        [XmlEnum("128")]
        StatusChange = 0x80,
        /// <summary>Notify when an operation starts.</summary>
        [System.ComponentModel.Description("Notify when an operation starts.")]
        [XmlEnum("256")]
        Start = 0x100,
        /// <summary>Notify when an operation ends.</summary>
        [System.ComponentModel.Description("Notify when an operation ends.")]
        [XmlEnum("512")]
        End = 0x200,
    }
}
