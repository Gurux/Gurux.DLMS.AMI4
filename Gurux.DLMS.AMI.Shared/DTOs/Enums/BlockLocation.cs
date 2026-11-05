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
    /// <summary>The area of the user interface in which a block is displayed.</summary>
    [System.ComponentModel.Description("The area of the user interface in which a block is displayed.")]
    public enum BlockLocation : byte
    {
        /// <summary>Display the block in the page header.</summary>
        [System.ComponentModel.Description("Display the block in the page header.")]
        [XmlEnum("0")]
        Header = 0,
        /// <summary>Display the block in the left sidebar.</summary>
        [System.ComponentModel.Description("Display the block in the left sidebar.")]
        [XmlEnum("1")]
        SidebarLeft = 1,
        /// <summary>Display the block in the right sidebar.</summary>
        [System.ComponentModel.Description("Display the block in the right sidebar.")]
        [XmlEnum("2")]
        SidebarRight = 2,
        /// <summary>Display the block in the main content area.</summary>
        [System.ComponentModel.Description("Display the block in the main content area.")]
        [XmlEnum("3")]
        Content = 3,
        /// <summary>Display the block in the page footer.</summary>
        [System.ComponentModel.Description("Display the block in the page footer.")]
        [XmlEnum("4")]
        Footer = 4,
        /// <summary>Display the block above the page content.</summary>
        [System.ComponentModel.Description("Display the block above the page content.")]
        [XmlEnum("5")]
        ContentHeader = 5,
        /// <summary>Display the block below the page content.</summary>
        [System.ComponentModel.Description("Display the block below the page content.")]
        [XmlEnum("6")]
        ContentFooter = 6,
        /// <summary>Display the block inside a user interface component.</summary>
        [System.ComponentModel.Description("Display the block inside a user interface component.")]
        [XmlEnum("7")]
        Component = 7
    }
}
