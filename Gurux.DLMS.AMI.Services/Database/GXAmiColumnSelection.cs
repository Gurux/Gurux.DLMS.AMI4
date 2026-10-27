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
