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
    /// Task types.
    /// </summary>
    public enum TaskType : byte
    {
        /// <summary>
        /// No task type is selected.
        /// </summary>
        [XmlEnum("0")]
        None = 0,
        /// <summary>
        /// Read.
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
        /// Refresh meter's association view.
        /// </summary>
        [XmlEnum("4")]
        Refresh = 4
    }
}
