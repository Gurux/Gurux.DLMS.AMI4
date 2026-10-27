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
    /// <summary>The destination or transport used to deliver a report.</summary>
    [System.ComponentModel.Description("The destination or transport used to deliver a report.")]
    public enum ReportDelivery : byte
    {
        /// <summary>Return the report to the caller in JSON format.</summary>
        [System.ComponentModel.Description("Return the report to the caller in JSON format.")]
        [XmlEnum("0")]
        Caller = 0,
        /// <summary>Deliver the report by email.</summary>
        [System.ComponentModel.Description("Deliver the report by email.")]
        [XmlEnum("1")]
        Email = 1,
        /// <summary>Deliver the report using FTP.</summary>
        [System.ComponentModel.Description("Deliver the report using FTP.")]
        [XmlEnum("2")]
        Ftp = 2,
        /// <summary>Deliver the report using HTTP.</summary>
        [System.ComponentModel.Description("Deliver the report using HTTP.")]
        [XmlEnum("3")]
        Http = 3,
    }
}
