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
    /// Specifies the audience that can view a resource.
    /// </summary>
    /// <remarks>
    /// Visibility does not grant permission to modify or delete the resource,
    /// or to perform operations on it. Those permissions are evaluated separately.
    /// The resource's <c>Creator</c> identifies its owner.
    /// </remarks>
    public enum ResourceVisibility
    {
        /// <summary>
        /// Uses the visibility defined by the parent group.
        /// </summary>
        /// <remarks>
        /// If no parent group defines visibility, the resource is private.
        /// </remarks>
        [XmlEnum("0")]
        Inherit = 0,

        /// <summary>
        /// Only the resource owner and administrators can view the resource.
        /// </summary>
        [XmlEnum("1")]
        Private = 1,

        /// <summary>
        /// Authenticated users can view the resource; anonymous users cannot.
        /// </summary>
        [XmlEnum("2")]
        Internal = 2,

        /// <summary>
        /// Everyone can view the resource, including anonymous users.
        /// </summary>
        [XmlEnum("3")]
        Public = 3
    }
}
