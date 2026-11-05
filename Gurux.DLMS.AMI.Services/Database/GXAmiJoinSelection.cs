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
/// Collects joins to apply to a select query.
/// </summary>
public sealed class GXAmiJoinSelection
{
    /// <summary>
    /// Gets the joins in the order they were added.
    /// </summary>
    public List<GXAmiDatabaseJoin> Items { get; } = [];

    /// <summary>
    /// Adds a left join, retaining source rows that have no matching target row.
    /// </summary>
    /// <typeparam name="TFrom">The entity type that owns the foreign key.</typeparam>
    /// <typeparam name="TTo">The entity type that owns the referenced key.</typeparam>
    /// <param name="foreignKey">The foreign-key column selector.</param>
    /// <param name="key">The referenced-key column selector.</param>
    public void AddLeftJoin<TFrom, TTo>(Expression<Func<TFrom, object>> foreignKey, Expression<Func<TTo, object>> key)
        => Items.Add(new(typeof(TFrom), typeof(TTo), foreignKey, key));

    /// <summary>
    /// Adds an inner join, retaining only rows with matching keys in both entities.
    /// </summary>
    /// <typeparam name="TFrom">The entity type that owns the foreign key.</typeparam>
    /// <typeparam name="TTo">The entity type that owns the referenced key.</typeparam>
    /// <param name="foreignKey">The foreign-key column selector.</param>
    /// <param name="key">The referenced-key column selector.</param>
    public void AddInnerJoin<TFrom, TTo>(Expression<Func<TFrom, object>> foreignKey, Expression<Func<TTo, object>> key)
        => Items.Add(new(typeof(TFrom), typeof(TTo), foreignKey, key, true));
}
