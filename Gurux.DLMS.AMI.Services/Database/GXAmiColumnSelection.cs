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
/// Collects column selections for query projections or ordering.
/// </summary>
public sealed class GXAmiColumnSelection
{
    /// <summary>
    /// Gets the column selections in the order they were added.
    /// </summary>
    public List<GXAmiDatabaseColumns> Items { get; } = [];

    /// <summary>
    /// Adds selected columns from an entity type.
    /// </summary>
    /// <typeparam name="T">The entity type that owns the columns.</typeparam>
    /// <param name="columns">The expression that selects the columns.</param>
    public void Add<T>(Expression<Func<T, object>> columns) => Items.Add(new(typeof(T), columns));

    /// <summary>
    /// Adds all columns from an entity type to the projection.
    /// </summary>
    /// <typeparam name="T">The entity type whose columns are selected.</typeparam>
    public void Add<T>() => Items.Add(new(typeof(T), null));
}
