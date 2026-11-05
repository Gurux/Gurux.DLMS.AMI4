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

using System;
using System.Xml.Serialization;

namespace Gurux.DLMS.AMI.Shared.Enums
{
    /// <summary>Controls whether a configuration value is hidden, displayed, or editable in the user interface.</summary>
    [System.ComponentModel.Description("Controls whether a configuration value is hidden, displayed, or editable in the user interface.")]
    public enum AccessType : byte
    {
        /// <summary>Hide the configuration value in the user interface.</summary>
        [System.ComponentModel.Description("Hide the configuration value in the user interface.")]
        [XmlEnum("0")]
        None = 0x0,
        /// <summary>Display the configuration value without allowing edits.</summary>
        [System.ComponentModel.Description("Display the configuration value without allowing edits.")]
        [XmlEnum("1")]
        View = 0x1,
        /// <summary>Allow the user to view and edit the configuration value.</summary>
        [System.ComponentModel.Description("Allow the user to view and edit the configuration value.")]
        [XmlEnum("2")]
        Edit = 0x2
    }
}
