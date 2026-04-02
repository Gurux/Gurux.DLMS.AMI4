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
    /// <summary>
    /// Content publish type.
    /// </summary>
    [Flags]
    public enum ContentVisibility : byte
    {
        /// <summary>
        /// Content is released immediately and user can't close it.
        /// </summary>
        /// <remarks>
        /// The user cannot select a publish start time or end time if the content is set to be published immediately.
        /// </remarks>
        [XmlEnum("0")]
        None = 0,
        /// <summary>
        /// User can close the content.
        /// </summary>
        [XmlEnum("1")]
        Closable = 1,
        /// <summary>
        /// User can select content publishing start time.
        /// </summary>
        [XmlEnum("2")]
        Start = 2,
        /// <summary>
        /// User can select content publishing end time.
        /// </summary>
        [XmlEnum("4")]
        End = 4,
        /// <summary>
        /// Content is promoted to front page.
        /// </summary>
        [XmlEnum("8")]
        Promoted = 8,
        /// <summary>
        /// Content is added to user menu.
        /// </summary>
        [XmlEnum("16")]
        Menu = 16,
    }
}
