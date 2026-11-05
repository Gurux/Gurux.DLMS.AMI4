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
    /// <summary>Determines whether a block's page list includes or excludes pages where it is displayed.</summary>
    [System.ComponentModel.Description("Determines whether a block's page list includes or excludes pages where it is displayed.")]
    public enum BlockPageVisibility : byte
    {
        /// <summary>Display the block only on the listed pages.</summary>
        [System.ComponentModel.Description("Display the block only on the listed pages.")]
        [XmlEnum("0")]
        ShowOnListedPages = 0,
        /// <summary>Display the block on every page except those listed.</summary>
        [System.ComponentModel.Description("Display the block on every page except those listed.")]
        [XmlEnum("1")]
        HideOnListedPages = 1
    }
}
