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
    /// <summary>The content format or renderer used by a user interface block.</summary>
    [System.ComponentModel.Description("The content format or renderer used by a user interface block.")]
    public enum BlockType : byte
    {
        /// <summary>
        /// Block is an HTML block.
        /// </summary>
        [System.ComponentModel.Description("Block is an HTML block.")]
        [XmlEnum("0")]
        Html = 0,
        /// <summary>Render the block using a user interface component.</summary>
        [System.ComponentModel.Description("Render the block using a user interface component.")]
        [XmlEnum("1")]
        Component = 1,
        /// <summary>Generate the block content using a script.</summary>
        [System.ComponentModel.Description("Generate the block content using a script.")]
        [XmlEnum("2")]
        Script = 2,
        /// <summary>Block contains inline Markdown.</summary>
        [System.ComponentModel.Description("Block contains inline Markdown.")]
        [XmlEnum("3")]
        Markdown = 3,
        /// <summary>Block contains HTML read from a server content file.</summary>
        [System.ComponentModel.Description("Block contains HTML read from a server content file.")]
        [XmlEnum("4")]
        HtmlFile = 4,
        /// <summary>Block contains Markdown read from a server content file.</summary>
        [System.ComponentModel.Description("Block contains Markdown read from a server content file.")]
        [XmlEnum("5")]
        MarkdownFile = 5,
        /// <summary>Display content from an RSS feed.</summary>
        [System.ComponentModel.Description("Display content from an RSS feed.")]
        [XmlEnum("6")]
        RSS = 6,
    }
}
