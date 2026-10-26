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
/// Collects joins to apply to a select query.
/// </summary>
public sealed class AmiJoinSelection
{
    /// <summary>
    /// Gets the joins in the order they were added.
    /// </summary>
    public List<AmiDatabaseJoin> Items { get; } = [];

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
