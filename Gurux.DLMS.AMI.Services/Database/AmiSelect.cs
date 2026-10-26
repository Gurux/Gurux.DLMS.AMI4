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
/// Provides factory methods for AMI select query descriptions.
/// </summary>
public static class AmiSelect
{
    /// <summary>
    /// Creates a query that selects all columns of an entity type.
    /// </summary>
    /// <typeparam name="T">The entity type to select.</typeparam>
    /// <param name="where">The optional initial filter.</param>
    /// <returns>A select query that can be configured with additional query options.</returns>
    public static AmiSelectQuery<T> SelectAll<T>(Expression<Func<T, bool>>? where = null) => new(null, where);

    /// <summary>
    /// Creates a query that selects specified columns of an entity type.
    /// </summary>
    /// <typeparam name="T">The entity type to select.</typeparam>
    /// <param name="columns">The expression that selects the columns.</param>
    /// <param name="where">The optional initial filter.</param>
    /// <returns>A select query that can be configured with additional query options.</returns>
    public static AmiSelectQuery<T> Select<T>(Expression<Func<T, object>> columns, Expression<Func<T, bool>>? where = null) => new(columns, where);
}
