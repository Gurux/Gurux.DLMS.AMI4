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

namespace Gurux.DLMS.AMI.Shared;

/// <summary>
/// Describes a column projection for an entity or a joined entity without depending on a host ORM.
/// </summary>
/// <param name="EntityType">The entity type that owns the columns.</param>
/// <param name="Expression">The column selector, or <see langword="null"/> to select all columns.</param>
public sealed record AmiDatabaseColumns(Type EntityType, LambdaExpression? Expression);

/// <summary>
/// Describes a left or inner join between two entity types.
/// </summary>
/// <param name="From">The entity type that owns the foreign key.</param>
/// <param name="To">The entity type that owns the referenced key.</param>
/// <param name="ForeignKey">The foreign-key column selector.</param>
/// <param name="Key">The referenced-key column selector.</param>
/// <param name="Inner">Whether to use an inner join instead of a left join.</param>
public sealed record AmiDatabaseJoin(Type From, Type To, LambdaExpression ForeignKey, LambdaExpression Key, bool Inner = false);

/// <summary>
/// Collects column selections for query projections or ordering.
/// </summary>
public sealed class AmiColumnSelection
{
    /// <summary>
    /// Gets the column selections in the order they were added.
    /// </summary>
    public List<AmiDatabaseColumns> Items { get; } = [];

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

/// <summary>
/// Describes an AMI select query, including projections, filters, joins, ordering, and pagination,
/// without depending on a host ORM.
/// </summary>
/// <typeparam name="T">The primary entity type to select.</typeparam>
/// <param name="columns">The initial column selector, or <see langword="null"/> to select all columns.</param>
/// <param name="where">The initial filter, or <see langword="null"/> for no initial filter.</param>
public sealed class AmiSelectQuery<T>(Expression<Func<T, object>>? columns, Expression<Func<T, bool>>? where)
{
    /// <summary>
    /// Gets the initial column selector, or <see langword="null"/> to select all columns.
    /// </summary>
    public Expression<Func<T, object>>? Projection { get; } = columns;

    /// <summary>
    /// Gets the initial filter, which can be replaced by calling <see cref="AmiQueryConditions.Clear"/> on <see cref="Where"/>.
    /// </summary>
    public Expression<Func<T, bool>>? Predicate { get; } = where;

    /// <summary>
    /// Gets additional column projections, including columns from joined entities.
    /// </summary>
    public AmiColumnSelection Columns { get; } = new();

    /// <summary>
    /// Gets the joins to apply to the query.
    /// </summary>
    public AmiJoinSelection Joins { get; } = new();

    /// <summary>
    /// Gets additional conditions combined with AND and controls replacement of the initial filter.
    /// </summary>
    public AmiQueryConditions Where { get; } = new();

    /// <summary>
    /// Gets the column selectors used to order the results.
    /// </summary>
    public AmiColumnSelection OrderBy { get; } = new();

    /// <summary>
    /// Gets or sets the starting index for result pagination.
    /// </summary>
    public uint Index { get; set; }

    /// <summary>
    /// Gets or sets the result count limit passed to the database provider.
    /// </summary>
    public uint Count { get; set; }
}

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

/// <summary>
/// Describes an AMI insert operation and the columns to exclude from it.
/// </summary>
/// <typeparam name="T">The type of the value to insert.</typeparam>
/// <param name="value">The value to insert.</param>
public sealed class AmiInsertQuery<T>(T value)
{
    /// <summary>
    /// Gets the value to insert.
    /// </summary>
    public T Value { get; } = value;

    /// <summary>
    /// Gets the column selections to omit from the insert operation.
    /// </summary>
    public List<AmiDatabaseColumns> ExcludedColumns { get; } = [];

    /// <summary>
    /// Excludes selected columns of an entity type from the insert operation.
    /// </summary>
    /// <typeparam name="TEntity">The entity type that owns the excluded columns.</typeparam>
    /// <param name="columns">The expression that selects the columns to exclude.</param>
    public void Exclude<TEntity>(Expression<Func<TEntity, object>> columns) => ExcludedColumns.Add(new(typeof(TEntity), columns));
}

/// <summary>
/// Provides a factory method for AMI insert query descriptions.
/// </summary>
public static class AmiInsert
{
    /// <summary>
    /// Creates a query that inserts the supplied value.
    /// </summary>
    /// <typeparam name="T">The type of the value to insert.</typeparam>
    /// <param name="value">The value to insert.</param>
    /// <returns>An insert query with configurable column exclusions.</returns>
    public static AmiInsertQuery<T> Insert<T>(T value) => new(value);
}

/// <summary>
/// Collects additional conditions combined with AND and controls replacement of a query's initial filter.
/// </summary>
public sealed class AmiQueryConditions
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

/// <summary>
/// Describes an AMI update operation with optional column selection and additional conditions.
/// </summary>
/// <typeparam name="T">The type of the value to update.</typeparam>
/// <param name="value">The value containing the updated data.</param>
/// <param name="columns">The column selector, or <see langword="null"/> to use the provider's default column selection.</param>
public sealed class AmiUpdateQuery<T>(T value, Expression<Func<T, object>>? columns)
{
    /// <summary>
    /// Gets the value containing the updated data.
    /// </summary>
    public T Value { get; } = value;

    /// <summary>
    /// Gets the column selector, or <see langword="null"/> to use the provider's default column selection.
    /// </summary>
    public Expression<Func<T, object>>? Columns { get; } = columns;

    /// <summary>
    /// Gets additional conditions and controls replacement of the default entity-key filter.
    /// </summary>
    public AmiQueryConditions Where { get; } = new();
}

/// <summary>
/// Provides a factory method for AMI update query descriptions.
/// </summary>
public static class AmiUpdate
{
    /// <summary>
    /// Creates a query that updates the supplied value, optionally restricting the updated columns.
    /// </summary>
    /// <typeparam name="T">The type of the value to update.</typeparam>
    /// <param name="value">The value containing the updated data.</param>
    /// <param name="columns">The optional column selector; <see langword="null"/> uses the provider's default column selection.</param>
    /// <returns>An update query with configurable conditions.</returns>
    public static AmiUpdateQuery<T> Update<T>(T value, Expression<Func<T, object>>? columns = null) => new(value, columns);
}

/// <summary>
/// Describes an AMI delete operation based on an entity value or an explicit predicate.
/// </summary>
/// <typeparam name="T">The entity type to delete.</typeparam>
/// <param name="Value">The entity used to identify the row when <paramref name="Predicate"/> is <see langword="null"/>.</param>
/// <param name="Predicate">The filter identifying rows to delete, or <see langword="null"/> to delete by entity value.</param>
public sealed record AmiDeleteQuery<T>(T? Value, Expression<Func<T, bool>>? Predicate);

/// <summary>
/// Provides factory methods for AMI delete query descriptions.
/// </summary>
public static class AmiDelete
{
    /// <summary>
    /// Creates a query that deletes the row identified by the supplied entity value.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <param name="value">The entity value identifying the row to delete.</param>
    /// <returns>A delete query based on the entity value.</returns>
    public static AmiDeleteQuery<T> Delete<T>(T value) => new(value, null);

    /// <summary>
    /// Creates a query that deletes rows matching the supplied predicate.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <param name="where">The filter identifying rows to delete.</param>
    /// <returns>A delete query based on the predicate.</returns>
    public static AmiDeleteQuery<T> Delete<T>(Expression<Func<T, bool>> where) => new(default, where);
}
