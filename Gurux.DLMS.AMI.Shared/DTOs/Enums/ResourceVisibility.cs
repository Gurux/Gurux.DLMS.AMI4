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
