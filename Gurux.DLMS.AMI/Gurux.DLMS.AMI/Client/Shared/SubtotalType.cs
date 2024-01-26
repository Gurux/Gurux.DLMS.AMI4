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

namespace Gurux.DLMS.AMI.Client.Shared
{
    /// <summary>
    /// Subtotal type.
    /// </summary>
    public enum SubtotalType : int
    {
        /// <summary>
        /// Value.
        /// </summary>
        [XmlEnum("0")]
        Value = 0,
        /// <summary>
        /// How many times target is read.
        /// </summary>
        [XmlEnum("1")]
        Read = 1,
        /// <summary>
        /// Write.
        /// </summary>
        [XmlEnum("2")]
        Write = 2,
        /// <summary>
        /// Action.
        /// </summary>
        [XmlEnum("3")]
        Action = 3,
        /// <summary>
        /// Read error.
        /// </summary>
        [XmlEnum("4")]
        ReadError = 4,
        /// <summary>
        /// Write error.
        /// </summary>
        [XmlEnum("5")]
        WriteError = 5,
        /// <summary>
        /// Action error.
        /// </summary>
        [XmlEnum("6")]
        ActionError = 6,
        /// <summary>
        /// Execution time.
        /// </summary>
        [XmlEnum("7")]
        ExecutionTime = 7,
        /// <summary>
        /// Connect.
        /// </summary>
        [XmlEnum("8")]
        Connect = 8,
        /// <summary>
        /// Disconnect.
        /// </summary>
        [XmlEnum("9")]
        Disconnect = 9,
        /// <summary>
        /// Generic errors.
        /// </summary>
        [XmlEnum("10")]
        Error = 10,
        /// <summary>
        /// Generic warnings.
        /// </summary>
        [XmlEnum("11")]
        Warning = 11,
    }
}
