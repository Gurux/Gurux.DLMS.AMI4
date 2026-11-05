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

namespace Gurux.DLMS.AMI.Shared.Enums
{
    /// <summary>Processing state of a received raw notification.</summary>
    [System.ComponentModel.Description("Processing state of a received raw notification.")]
    public enum RawNotificationStatus : byte
    {
        /// <summary>Waiting to be processed.</summary>
        [System.ComponentModel.Description("Waiting to be processed.")]
        Pending = 0,

        /// <summary>Reserved by a processing agent.</summary>
        [System.ComponentModel.Description("Reserved by a processing agent.")]
        Processing = 1,

        /// <summary>Processed successfully.</summary>
        [System.ComponentModel.Description("Processed successfully.")]
        Completed = 2,

        /// <summary>Processing failed.</summary>
        [System.ComponentModel.Description("Processing failed.")]
        Failed = 3
    }
}
