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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.DTOs.ComponentView
{
    /// <summary>Reusable communication settings for device connections.</summary>
    [Description("Reusable communication settings for device connections.")]
    public class GXCommunicationProfile
    {
        /// <summary>
        /// Used interface type.
        /// </summary>
        [Description("Used interface type.")]
        public int InterfaceType { get; set; }

        /// <summary>Used logical client identifier.</summary>
        /// <remarks>
        /// Client ID is always 1 byte long in HDLC and 2 bytes long when WRAPPER is used.
        /// </remarks>
        [DefaultValue(0x10)]
        [Description("Used logical client identifier.")]
        [Range(0, 127, ErrorMessage = "Value must be between 1 to 127")]

        public int ClientAddress { get; set; }

        /// <summary>
        /// Used Physical address.
        /// </summary>
        /// <remarks>
        /// Server HDLC Address (Logical + Physical address)  might be 1,2 or 4 bytes long.
        /// </remarks>
        [Description("Used Physical address.")]
        [DefaultValue(1)]
        [Range(1, 16383, ErrorMessage = "Value must be between 1 to 16383")]
        virtual public int PhysicalAddress { get; set; }

        /// <summary>
        /// Used logical address. Logical address is used only with HDLC.
        /// </summary>
        [Description("Used logical address. Logical address is used only with HDLC.")]
        [DefaultValue(0)]
        [Range(0, 16383, ErrorMessage = "Value must be between 0 to 16383")]
        public int LogicalAddress { get; set; }
    }
}
