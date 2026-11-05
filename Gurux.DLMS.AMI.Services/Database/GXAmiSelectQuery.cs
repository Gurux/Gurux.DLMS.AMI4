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
/// Describes an AMI select query, including projections, filters, joins, ordering, and pagination,
/// without depending on a host ORM.
/// </summary>
/// <typeparam name="T">The primary entity type to select.</typeparam>
/// <param name="columns">The initial column selector, or <see langword="null"/> to select all columns.</param>
/// <param name="where">The initial filter, or <see langword="null"/> for no initial filter.</param>
public sealed class GXAmiSelectQuery<T>(Expression<Func<T, object>>? columns, Expression<Func<T, bool>>? where)
{
    /// <summary>
    /// Gets the initial column selector, or <see langword="null"/> to select all columns.
    /// </summary>
    public Expression<Func<T, object>>? Projection { get; } = columns;

    /// <summary>
    /// Gets the initial filter, which can be replaced by calling <see cref="GXAmiQueryConditions.Clear"/> on <see cref="Where"/>.
    /// </summary>
    public Expression<Func<T, bool>>? Predicate { get; } = where;

    /// <summary>
    /// Gets additional column projections, including columns from joined entities.
    /// </summary>
    public GXAmiColumnSelection Columns { get; } = new();

    /// <summary>
    /// Gets the joins to apply to the query.
    /// </summary>
    public GXAmiJoinSelection Joins { get; } = new();

    /// <summary>
    /// Gets additional conditions combined with AND and controls replacement of the initial filter.
    /// </summary>
    public GXAmiQueryConditions Where { get; } = new();

    /// <summary>
    /// Gets the column selectors used to order the results.
    /// </summary>
    public GXAmiColumnSelection OrderBy { get; } = new();

    /// <summary>
    /// Gets or sets the zero-based starting row index for result pagination; defaults to zero.
    /// </summary>
    public uint Index { get; set; }

    /// <summary>
    /// Gets or sets the result count limit passed to the database provider.
    /// </summary>
    public uint Count { get; set; }
}
