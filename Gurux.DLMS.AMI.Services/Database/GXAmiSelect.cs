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

using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Services.Database;

/// <summary>
/// Provides factory methods for AMI select query descriptions.
/// </summary>
public static class GXAmiSelect
{
    /// <summary>
    /// Creates a query that selects all columns of an entity type.
    /// </summary>
    /// <typeparam name="T">The entity type to select.</typeparam>
    /// <param name="where">The optional initial filter.</param>
    /// <returns>A select query that can be configured with additional query options.</returns>
    public static GXAmiSelectQuery<T> SelectAll<T>(Expression<Func<T, bool>>? where = null) => new(null, where);

    /// <summary>
    /// Creates a query that selects specified columns of an entity type.
    /// </summary>
    /// <typeparam name="T">The entity type to select.</typeparam>
    /// <param name="columns">The expression that selects the columns.</param>
    /// <param name="where">The optional initial filter.</param>
    /// <returns>A select query that can be configured with additional query options.</returns>
    public static GXAmiSelectQuery<T> Select<T>(Expression<Func<T, object>> columns, Expression<Func<T, bool>>? where = null) => new(columns, where);
}
