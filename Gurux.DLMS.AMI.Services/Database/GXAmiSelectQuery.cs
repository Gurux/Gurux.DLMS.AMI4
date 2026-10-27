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
