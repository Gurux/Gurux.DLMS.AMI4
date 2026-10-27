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
    /// <summary>Device operations and other work that can be queued as tasks.</summary>
    [System.ComponentModel.Description("Device operations and other work that can be queued as tasks.")]
    public enum TaskType : byte
    {
        /// <summary>
        /// No task type is selected.
        /// </summary>
        [System.ComponentModel.Description("No task type is selected.")]
        [XmlEnum("0")]
        None = 0,
        /// <summary>Read an attribute value from a device.</summary>
        [System.ComponentModel.Description("Read an attribute value from a device.")]
        [XmlEnum("1")]
        Read = 1,
        /// <summary>Write an attribute value to a device.</summary>
        [System.ComponentModel.Description("Write an attribute value to a device.")]
        [XmlEnum("2")]
        Write = 2,
        /// <summary>Invoke a method on a COSEM object.</summary>
        [System.ComponentModel.Description("Invoke a method on a COSEM object.")]
        [XmlEnum("3")]
        Action = 3,
        /// <summary>
        /// Refresh meter's association view.
        /// </summary>
        [System.ComponentModel.Description("Refresh meter's association view.")]
        [XmlEnum("4")]
        Refresh = 4,
        /// <summary>
        /// Send email.
        /// </summary>
        [System.ComponentModel.Description("Send email.")]
        [XmlEnum("5")]
        Email = 5,
    }
}
