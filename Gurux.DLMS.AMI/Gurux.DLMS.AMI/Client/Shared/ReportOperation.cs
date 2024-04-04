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
    /// Report operation.
    /// </summary>
    public enum ReportOperation : byte
    {
        /// <summary>
        /// Sum value.
        /// </summary>
        [XmlEnum("0")]
        Sum = 0,
        /// <summary>
        /// Average value.
        /// </summary>
        [XmlEnum("1")]
        Average = 1,
        /// <summary>
        /// Minimum value.
        /// </summary>
        [XmlEnum("2")]
        Minimum = 2,
        /// <summary>
        /// Maximum value.
        /// </summary>
        [XmlEnum("3")]
        Maximum = 3,
        /// <summary>
        /// Count.
        /// </summary>
        [XmlEnum("4")]
        Count = 4,
        /// <summary>
        /// Instant values are returned.
        /// </summary>
        [XmlEnum("5")]
        Instant = 5
    }
}
