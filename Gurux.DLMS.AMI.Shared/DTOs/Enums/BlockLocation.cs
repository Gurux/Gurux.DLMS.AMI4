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
    /// <summary>The area of the user interface in which a block is displayed.</summary>
    public enum BlockLocation : byte
    {
        /// <summary>Display the block in the page header.</summary>
        [XmlEnum("0")]
        Header = 0,
        /// <summary>Display the block in the left sidebar.</summary>
        [XmlEnum("1")]
        SidebarLeft = 1,
        /// <summary>Display the block in the right sidebar.</summary>
        [XmlEnum("2")]
        SidebarRight = 2,
        /// <summary>Display the block in the main content area.</summary>
        [XmlEnum("3")]
        Content = 3,
        /// <summary>Display the block in the page footer.</summary>
        [XmlEnum("4")]
        Footer = 4,
        /// <summary>Display the block above the page content.</summary>
        [XmlEnum("5")]
        ContentHeader = 5,
        /// <summary>Display the block below the page content.</summary>
        [XmlEnum("6")]
        ContentFooter = 6,
        /// <summary>Display the block inside a user interface component.</summary>
        [XmlEnum("7")]
        Component = 7
    }
}
