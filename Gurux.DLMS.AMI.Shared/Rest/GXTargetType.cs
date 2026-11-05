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

using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Target type.
    /// </summary>
    [System.ComponentModel.Description("Target type.")]
    [DataContract]
    public class GXTargetType
    {
        /// <summary>
        /// Data type identifier.
        /// </summary>
        [System.ComponentModel.Description("Data type identifier.")]
        [DataMember]
        public string? Id { get; set; }

        /// <summary>
        /// Localized name for the data type.
        /// </summary>
        [System.ComponentModel.Description("Localized name for the data type.")]
        [DataMember]
        public string? LocalizedName { get; set; }

        /// <summary>
        /// Imported item count.
        /// </summary>
        [System.ComponentModel.Description("Imported item count.")]
        [DataMember]
        public long? Count { get; set; }
    }
}
