// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

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
