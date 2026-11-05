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
