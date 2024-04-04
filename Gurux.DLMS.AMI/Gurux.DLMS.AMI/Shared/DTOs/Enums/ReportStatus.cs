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
    /// <summary>
    /// Report state.
    /// </summary>
    public enum ReportStatus : byte
    {
        /// <summary>
        /// Report is on the idle state.
        /// </summary>
        [XmlEnum("0")]
        Idle = 0,
        /// <summary>
        /// Report handling is in process.
        /// </summary>
        [XmlEnum("1")]
        Process = 1,
        /// <summary>
        /// The handling of report is cancelled.
        /// </summary>
        [XmlEnum("2")]
        Cancel = 2,
        /// <summary>
        /// Report handling is in error state.
        /// </summary>
        [XmlEnum("3")]
        Error = 3
    }
}
