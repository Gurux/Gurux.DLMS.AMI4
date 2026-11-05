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
    /// <summary>The execution state of a subtotal calculation.</summary>
    [System.ComponentModel.Description("The execution state of a subtotal calculation.")]
    public enum SubtotalStatus : byte
    {
        /// <summary>No subtotal calculation is in progress.</summary>
        [System.ComponentModel.Description("No subtotal calculation is in progress.")]
        [XmlEnum("0")]
        Idle = 0,
        /// <summary>
        /// Subtotals are currently being calculated.
        /// </summary>
        [System.ComponentModel.Description("Subtotals are currently being calculated.")]
        [XmlEnum("1")]
        Calculate = 1,
        /// <summary>The subtotals have been cleared.</summary>
        [System.ComponentModel.Description("The subtotals have been cleared.")]
        [XmlEnum("2")]
        Clear = 2,
        /// <summary>The subtotal calculation has been cancelled.</summary>
        [System.ComponentModel.Description("The subtotal calculation has been cancelled.")]
        [XmlEnum("3")]
        Cancel = 3,
        /// <summary>The subtotal calculation has failed.</summary>
        [System.ComponentModel.Description("The subtotal calculation has failed.")]
        [XmlEnum("4")]
        Error = 4
    }
}
