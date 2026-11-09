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

using System.Diagnostics.CodeAnalysis;
using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Server.Internal
{
    /// <summary>
    /// UniqueObjectComparer is used to find objects ID and device.
    /// </summary>
    internal class UniqueAttributeComparer : IEqualityComparer<GXAttribute>
    {
        /// <summary>
        /// Check are objects equal.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(GXAttribute? x, GXAttribute? y)
        {
            return ReferenceEquals(x, y) || (x is not null && y is not null && x.Id == y.Id && x.Object?.Device == y.Object?.Device);
        }

        /// <summary>
        /// Get hash code for the object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetHashCode([DisallowNull] GXAttribute obj)
        {
            return HashCode.Combine(obj.Id, obj.Object?.Device);
        }
    }
}