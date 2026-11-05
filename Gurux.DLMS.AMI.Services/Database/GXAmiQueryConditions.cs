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
/// Collects additional conditions combined with AND and controls replacement of a query's initial filter.
/// </summary>
public sealed class GXAmiQueryConditions
{
    /// <summary>
    /// Gets whether the database provider must remove the initial predicate or default entity-key filter.
    /// </summary>
    public bool ReplaceKeyFilter { get; private set; }

    /// <summary>
    /// Gets the additional predicates to combine with AND.
    /// </summary>
    public List<LambdaExpression> Predicates { get; } = [];

    /// <summary>
    /// Removes all additional predicates and requests removal of the query's initial or default key filter.
    /// Conditions added after this call become the replacement filter.
    /// </summary>
    public void Clear()
    {
        ReplaceKeyFilter = true;
        Predicates.Clear();
    }

    /// <summary>
    /// Adds a predicate to combine with the other query conditions using AND.
    /// </summary>
    /// <typeparam name="T">The entity type evaluated by the predicate.</typeparam>
    /// <param name="predicate">The condition to add.</param>
    public void And<T>(Expression<Func<T, bool>> predicate) => Predicates.Add(predicate);
}
