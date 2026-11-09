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
using Gurux.Service.Orm.Common;

namespace Gurux.DLMS.AMI.Server.Internal
{
    /// <summary>
    /// UniqueComparer is used to find objects using IUnique.
    /// </summary>
    /// <typeparam name="T">Class type.</typeparam>
    /// <typeparam name="TYPE">Data type of the IUnique.</typeparam>
    internal class UniqueComparer<T, TYPE> : IEqualityComparer<T>
    {
        /// <summary>
        /// Check are values equal.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(T? x, T? y)
        {
            if ((x is IUnique<TYPE> X) && (y is IUnique<TYPE> Y))
            {
                return EqualityComparer<TYPE>.Default.Equals(X.Id, Y.Id);
            }
            return false;
        }

        /// <summary>
        /// Get hash code for the object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetHashCode([DisallowNull] T obj)
        {
            if (obj is IUnique<TYPE> o)
            {
                return o.Id is { } id ? EqualityComparer<TYPE>.Default.GetHashCode(id) : 0;
            }
            return base.GetHashCode();
        }
    }
}