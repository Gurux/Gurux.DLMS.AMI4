//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

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
