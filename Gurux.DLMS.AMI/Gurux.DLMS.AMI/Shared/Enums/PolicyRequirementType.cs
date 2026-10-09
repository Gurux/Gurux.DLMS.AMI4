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
using System;
using System.Xml.Serialization;

namespace Gurux.DLMS.AMI.Shared.Enums
{
    /// <summary>
    /// Enumeration for policy requirement types.
    /// </summary>
    public enum PolicyRequirementType : byte
    {
        /// <summary>
        /// User must have a specific permission to access the resource.
        /// </summary>
        [XmlEnum("0")]
        Permission,
        /// <summary>
        /// User must be in a specific role to access the resource.
        /// </summary>
        [XmlEnum("1")]
        Role,
        /// <summary>
        /// User must have a specific claim to access the resource.
        /// </summary>
        [XmlEnum("2")]
        Claim,
        /// <summary>
        /// User must be authenticated to access the resource.
        /// </summary>
        [XmlEnum("3")]
        AuthenticatedUser,
        /// <summary>
        /// Only the user can access the resource. 
        /// This is used to restrict access to a specific user.
        /// </summary>
        [XmlEnum("4")]
        User,
        /// <summary>
        /// Only users authenticated with a specific authentication scheme can access the resource.
        /// </summary>
        [XmlEnum("5")]
        AuthenticationScheme,
        /// <summary>Explicit public access, including anonymous users.</summary>
        [XmlEnum("6")]
        AnyUser
    }
}
