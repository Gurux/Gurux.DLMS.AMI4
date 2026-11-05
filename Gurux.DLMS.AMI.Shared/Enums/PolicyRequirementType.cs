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
    /// <summary>The kind of identity or permission check performed by a policy requirement.</summary>
    [System.ComponentModel.Description("The kind of identity or permission check performed by a policy requirement.")]
    public enum PolicyRequirementType : byte
    {
        /// <summary>
        /// User must have a specific permission to access the resource.
        /// </summary>
        [System.ComponentModel.Description("User must have a specific permission to access the resource.")]
        [XmlEnum("0")]
        Permission,
        /// <summary>
        /// User must be in a specific role to access the resource.
        /// </summary>
        [System.ComponentModel.Description("User must be in a specific role to access the resource.")]
        [XmlEnum("1")]
        Role,
        /// <summary>
        /// User must have a specific claim to access the resource.
        /// </summary>
        [System.ComponentModel.Description("User must have a specific claim to access the resource.")]
        [XmlEnum("2")]
        Claim,
        /// <summary>
        /// User must be authenticated to access the resource.
        /// </summary>
        [System.ComponentModel.Description("User must be authenticated to access the resource.")]
        [XmlEnum("3")]
        AuthenticatedUser,
        /// <summary>
        /// Only the user can access the resource. 
        /// This is used to restrict access to a specific user.
        /// </summary>
        [System.ComponentModel.Description("Only the user can access the resource. This is used to restrict access to a specific user.")]
        [XmlEnum("4")]
        User,
        /// <summary>
        /// Only users authenticated with a specific authentication scheme can access the resource.
        /// </summary>
        [System.ComponentModel.Description("Only users authenticated with a specific authentication scheme can access the resource.")]
        [XmlEnum("5")]
        AuthenticationScheme,
        /// <summary>Explicit public access, including anonymous users.</summary>
        [System.ComponentModel.Description("Explicit public access, including anonymous users.")]
        [XmlEnum("6")]
        AnyUser
    }
}
