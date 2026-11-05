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

namespace Gurux.DLMS.AMI.Shared.DIs.Enums
{
    /// <summary>Determines how an import handles data that already exists.</summary>
    [System.ComponentModel.Description("Determines how an import handles data that already exists.")]
    public enum DataExchangeRule
    {
        /// <summary>Keep the existing data and ignore the imported replacement.</summary>
        [System.ComponentModel.Description("Keep the existing data and ignore the imported replacement.")]
        Keep,
        /// <summary>Replace the existing data with the imported data.</summary>
        [System.ComponentModel.Description("Replace the existing data with the imported data.")]
        Replace,
        /// <summary>Create a separate copy when the imported data already exists.</summary>
        [System.ComponentModel.Description("Create a separate copy when the imported data already exists.")]
        Copy,
        /// <summary>Reject the import when the data already exists.</summary>
        [System.ComponentModel.Description("Reject the import when the data already exists.")]
        Error
    };
}
