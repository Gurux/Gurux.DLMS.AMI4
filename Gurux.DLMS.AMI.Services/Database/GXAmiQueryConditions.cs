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
