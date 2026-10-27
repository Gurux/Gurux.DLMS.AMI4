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
    /// <summary>The execution state of a report request.</summary>
    [System.ComponentModel.Description("The execution state of a report request.")]
    public enum ReportStatus : byte
    {
        /// <summary>The report is waiting to be processed.</summary>
        [System.ComponentModel.Description("The report is waiting to be processed.")]
        [XmlEnum("0")]
        Idle = 0,
        /// <summary>The report is being processed.</summary>
        [System.ComponentModel.Description("The report is being processed.")]
        [XmlEnum("1")]
        Process = 1,
        /// <summary>Report processing has been cancelled.</summary>
        [System.ComponentModel.Description("Report processing has been cancelled.")]
        [XmlEnum("2")]
        Cancel = 2,
        /// <summary>Report processing has failed.</summary>
        [System.ComponentModel.Description("Report processing has failed.")]
        [XmlEnum("3")]
        Error = 3
    }
}
