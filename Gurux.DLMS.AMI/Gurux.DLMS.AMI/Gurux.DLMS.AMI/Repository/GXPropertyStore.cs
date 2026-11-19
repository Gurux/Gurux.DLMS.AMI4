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

using System.Data;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Common;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>
/// Persists named properties within an owner. Callers enforce authorization and
/// publish notifications after committing their transaction.
/// </summary>
internal sealed class GXPropertyStore<T, TOwner, TKey>(Expression<Func<T, TOwner?>> ownerColumn)
    where T : class, IGXProperty, new()
    where TOwner : class, IUnique<TKey>
{
    /// <summary>Reflected ownership property used to scope entity access.</summary>
    private readonly PropertyInfo _ownerProperty = (PropertyInfo)((MemberExpression)ownerColumn.Body).Member;
    /// <summary>Reflected creation timestamp property used by generic persistence operations.</summary>
    private static readonly PropertyInfo _creationTime = typeof(T).GetProperty("CreationTime")!;
    /// <summary>Reflected modification timestamp property used by generic persistence operations.</summary>
    private static readonly PropertyInfo _updated = typeof(T).GetProperty("Updated")!;
    /// <summary>Column selection that updates both the value and its modification timestamp.</summary>
    private static readonly Expression<Func<T, object>> _valueAndUpdated = UpdateColumns(true);
    /// <summary>Column selection that updates only the modification timestamp.</summary>
    private static readonly Expression<Func<T, object>> _updatedOnly = UpdateColumns(false);

    /// <summary>Creates the typed column selection used for property updates.</summary>
    /// <param name="includeValue">Whether the value column is updated along with its timestamp.</param>
    /// <returns>A column selection containing the timestamp and optionally the value.</returns>
    private static Expression<Func<T, object>> UpdateColumns(bool includeValue)
    {
        var p = Expression.Parameter(typeof(T), "p");
        var columns = (includeValue ? new[] { nameof(IGXProperty.Value), "Updated" } : ["Updated"])
            .Select(name => Expression.Convert(Expression.Property(p, name), typeof(object)));
        return Expression.Lambda<Func<T, object>>(Expression.NewArrayInit(typeof(object), columns), p);
    }

    /// <summary>Rejects property names that are blank or longer than 64 characters.</summary>
    /// <param name="name">Property name required to contain between 1 and 64 characters.</param>
    /// <exception cref="ArgumentException">The name is blank or exceeds 64 characters.</exception>
    [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Preserves the existing instance API used by repository consumers.")]

    internal void ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 64)
        {
            throw new ArgumentException("A property name of at most 64 characters is required.", nameof(name));
        }
    }

    /// <summary>Validates property owner identity, immutable ownership, property name, and optional module identity.</summary>
    /// <param name="owner">Entity owning the property; its identity must be valid and immutable.</param>
    /// <param name="property">Owner-scoped property record to validate, save, or delete.</param>
    /// <exception cref="ArgumentNullException">The owner or property is null.</exception>
    /// <exception cref="ArgumentException">Owner, name, module, or immutable ownership checks fail.</exception>
    private void Validate(TOwner owner, T property)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(property);
        if (EqualityComparer<TKey>.Default.Equals(owner.Id, default!) ||
            owner.Id is string text && string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("A valid property owner identifier is required.");
        }
        ValidateName(property.Name);
        if (_ownerProperty.GetValue(property) is TOwner supplied &&
            !EqualityComparer<TKey>.Default.Equals(supplied.Id, owner.Id))
        {
            throw new ArgumentException("A property cannot be moved to another owner.");
        }
        if (property.Module != null && string.IsNullOrWhiteSpace(property.Module.Id))
        {
            throw new ArgumentException("A valid module identifier is required.");
        }
    }

    /// <summary>Creates the predicate matching properties to their owning entity.</summary>
    /// <param name="owner">Entity whose identifier scopes the stored properties.</param>
    /// <returns>A predicate selecting only properties owned by the supplied entity.</returns>
    private Expression<Func<T, bool>> OwnerPredicate(TOwner owner)
    {
        var parameter = Expression.Parameter(typeof(T), "p");
        Expression<Func<TOwner>> value = () => owner;
        return Expression.Lambda<Func<T, bool>>(Expression.Equal(
            Expression.Property(parameter, _ownerProperty), value.Body), parameter);
    }

    /// <summary>Restricts a query to records that have not been removed.</summary>
    /// <param name="query">Query whose filtering or selected columns are updated.</param>
    private static void ActiveOnly(GXSelectArgs query)
    {
        if (typeof(T).GetProperty("Removed") is not { } removed)
        {
            return;
        }
        var parameter = Expression.Parameter(typeof(T), "p");
        query.Where.And(Expression.Lambda<Func<T, bool>>(Expression.Equal(
            Expression.Property(parameter, removed), Expression.Constant(null, removed.PropertyType)), parameter));
    }

    /// <summary>Reads active properties for an owner and materializes their module identities.</summary>
    /// <param name="connection">Database connection used for this query or persistence operation.</param>
    /// <param name="transaction">Transaction shared by the query or mutation.</param>
    /// <param name="owner">Entity whose active properties are read.</param>
    /// <param name="cancellationToken">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>Active owner-scoped property records.</returns>
    internal async Task<List<T>> ListAsync(GXDbConnection connection, IDbTransaction? transaction,
        TOwner owner, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var query = GXSelectArgs.SelectAll<T>(OwnerPredicate(owner));
        ActiveOnly(query);
        query.Joins.AddLeftJoin<T, GXModule>(p => p.Module!, m => m.Id);
        query.Columns.Add<GXModule>(m => m.Id);
        var rows = await connection.SelectAsync<T>(transaction, query, cancellationToken);
        foreach (var row in rows)
        {
            _ownerProperty.SetValue(row, owner);
        }

        return rows;
    }

    /// <summary>Finds an active property by its owner, optional module, and property name.</summary>
    /// <param name="connection">Database connection used for this query or persistence operation.</param>
    /// <param name="transaction">Transaction shared by the query or mutation.</param>
    /// <param name="owner">Entity whose natural-key property is selected.</param>
    /// <param name="module">Optional module identity forming part of the property natural key.</param>
    /// <param name="name">Property name forming part of the owner and module natural key.</param>
    /// <param name="cancellationToken">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>The matching property with owner and module references populated, or null if absent.</returns>
    internal async Task<T?> FindAsync(GXDbConnection connection, IDbTransaction? transaction,
        TOwner owner, GXModule? module, string name, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateName(name);
        var query = GXSelectArgs.SelectAll<T>(OwnerPredicate(owner));
        ActiveOnly(query);
        query.Where.And<T>(p => p.Module == module && p.Name == name);
        var row = await connection.SingleOrDefaultAsync<T>(transaction, query, cancellationToken);
        if (row != null)
        {
            _ownerProperty.SetValue(row, owner);
            row.Module = module;
        }
        return row;
    }

    /// <summary>Validates a property, resolves its existing natural-key record, and inserts or updates it with concurrency checks.</summary>
    /// <param name="connection">Database connection used for this query or persistence operation.</param>
    /// <param name="transaction">Transaction shared by the query or mutation.</param>
    /// <param name="owner">Entity whose property is inserted or updated.</param>
    /// <param name="property">Owner-scoped property record to validate, save, or delete.</param>
    /// <param name="columns">Optional selected columns retained by the persistence or query contract.</param>
    /// <param name="cancellationToken">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>True when the stored property was changed.</returns>
    internal async Task<bool> UpsertAsync(GXDbConnection connection, IDbTransaction? transaction,
        TOwner owner, T property, Expression<Func<T, object>>? columns = null,
        CancellationToken cancellationToken = default)
    {
        Validate(owner, property);
        var existing = await FindAsync(connection, transaction, owner, property.Module, property.Name!, cancellationToken);
        return await SaveAsync(connection, transaction, owner, property, existing, columns, cancellationToken);
    }

    // Existing is read in the caller's transaction so it can authorize Add or Edit before writing.
    /// <summary>Persists a validated property with immutable natural-key identity, optimistic concurrency, and an optional predetermined insert identifier.</summary>
    /// <param name="connection">Database connection used for this query or persistence operation.</param>
    /// <param name="transaction">Transaction shared by the query or mutation.</param>
    /// <param name="owner">Entity whose immutable identity scopes the property write.</param>
    /// <param name="property">Owner-scoped property record to validate, save, or delete.</param>
    /// <param name="existing">Current persisted property matching the natural key, or null for a new record.</param>
    /// <param name="columns">Optional selected columns retained by the persistence or query contract.</param>
    /// <param name="cancellationToken">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <param name="insertId">Optional predetermined identifier to use when inserting a new property.</param>
    /// <returns>True when persistence changed the property record.</returns>
    [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "Preserves the established argument order used by repository and transaction callers.")]
    internal async Task<bool> SaveAsync(GXDbConnection connection, IDbTransaction? transaction,
        TOwner owner, T property, T? existing, Expression<Func<T, object>>? columns = null,
        CancellationToken cancellationToken = default, Guid? insertId = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Validate(owner, property);
        if (property.Module is { } module && await connection.SingleOrDefaultAsync<GXModule>(transaction,
            GXSelectArgs.Select<GXModule>(m => m.Id, m => m.Id == module.Id), cancellationToken) == null)
        {
            throw new ArgumentException("The module is unavailable.");
        }

        if (existing != null)
        {
            if (property.Id != Guid.Empty && property.Id != existing.Id)
            {
                throw new ArgumentException("A property identifier cannot refer to a different key.");
            }
            var expectedVersion = existing.ConcurrencyStamp;
            if (property.ConcurrencyStamp != expectedVersion)
            {
                throw new ArgumentException("The property was edited by another operation.");
            }
            property.Id = existing.Id;
            _ownerProperty.SetValue(property, owner);
            _creationTime.SetValue(property, _creationTime.GetValue(existing));
            _updated.SetValue(property, DateTimeOffset.UtcNow);
            // Metadata, ownership and logical keys are never client-writable.
            var selected = columns == null || IncludesValue(columns.Body) ? _valueAndUpdated : _updatedOnly;
            var update = GXUpdateArgs.Update(property, selected);
            update.Where.And<T>(p => p.ConcurrencyStamp == expectedVersion);
            update.Where.And(OwnerPredicate(owner));
            if (await connection.UpdateAsync(transaction, update, cancellationToken) != 1)
            {
                throw new ArgumentException("The property was edited by another operation.");
            }
            return false;
        }

        if (property.Id != Guid.Empty && await connection.SingleOrDefaultAsync<T>(transaction,
            GXSelectArgs.SelectAll<T>(p => p.Id == property.Id), cancellationToken) != null)
        {
            throw new ArgumentException("A property identifier cannot refer to a different key.");
        }
        // A stable primary key also enforces uniqueness when Module is NULL.
        var key = JsonSerializer.Serialize(new[] { Convert.ToString(owner.Id, CultureInfo.InvariantCulture), property.Module?.Id, property.Name });
        property.Id = insertId ?? new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(key)).AsSpan(0, 16));
        property.ConcurrencyStamp = null; // Gurux Service generates the insert/update version.
        _creationTime.SetValue(property, DateTimeOffset.UtcNow);
        _updated.SetValue(property, null);
        _ownerProperty.SetValue(property, owner);
        await connection.InsertAsync(transaction, GXInsertArgs.Insert(property), cancellationToken);
        return true;
    }

    /// <summary>Deletes an owner-scoped property only when its concurrency stamp still matches and reports stale writes as an error.</summary>
    /// <param name="connection">Database connection used for this query or persistence operation.</param>
    /// <param name="transaction">Transaction shared by the query or mutation.</param>
    /// <param name="owner">Entity whose property deletion is authorized by its persisted owner identity.</param>
    /// <param name="property">Owner-scoped property record to validate, save, or delete.</param>
    /// <param name="cancellationToken">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>A task that completes after the property is deleted.</returns>
    /// <exception cref="ArgumentException">The matching property was changed or deleted by another operation.</exception>
    internal async Task DeleteAsync(GXDbConnection connection, IDbTransaction? transaction,
        TOwner owner, T property, CancellationToken cancellationToken = default)
    {
        if (!await TryDeleteAsync(connection, transaction, owner, property, cancellationToken))
        {
            throw new ArgumentException("The property was edited by another operation.");
        }
    }

    /// <summary>Attempts an owner- and concurrency-scoped property deletion without converting a stale write into an exception.</summary>
    /// <param name="connection">Database connection used for this query or persistence operation.</param>
    /// <param name="transaction">Transaction shared by the query or mutation.</param>
    /// <param name="owner">Entity whose property deletion is scoped by owner identity.</param>
    /// <param name="property">Owner-scoped property record to validate, save, or delete.</param>
    /// <param name="cancellationToken">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>True when exactly one matching property was deleted.</returns>
    internal async Task<bool> TryDeleteAsync(GXDbConnection connection, IDbTransaction? transaction,
        TOwner owner, T property, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Validate(owner, property);
        var id = property.Id;
        var stamp = property.ConcurrencyStamp;
        var delete = GXDeleteArgs.Delete<T>(p => p.Id == id && p.ConcurrencyStamp == stamp);
        delete.Where.And(OwnerPredicate(owner));
        return await connection.DeleteAsync(transaction, delete, cancellationToken) == 1;
    }

    /// <summary>Replaces the owner's complete property collection, deleting active properties omitted by the caller.</summary>
    /// <param name="connection">Database connection used for this query or persistence operation.</param>
    /// <param name="transaction">Transaction shared by the query or mutation.</param>
    /// <param name="owner">Entity whose active property collection is replaced.</param>
    /// <param name="properties">Desired owner-scoped property collection replacing the active stored set.</param>
    /// <param name="cancellationToken">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>A task that completes after the property set is reconciled.</returns>
    internal Task ReplaceAsync(GXDbConnection connection, IDbTransaction? transaction,
        TOwner owner, IEnumerable<T> properties, CancellationToken cancellationToken = default)
        => ReplacePreservingAsync(connection, transaction, owner, properties, cancellationToken, null);

    /// <summary>Reconciles an owner's property collection while retaining records selected by the preservation predicate.</summary>
    /// <param name="connection">Database connection used for this query or persistence operation.</param>
    /// <param name="transaction">Transaction shared by the query or mutation.</param>
    /// <param name="owner">Entity whose active property collection is reconciled.</param>
    /// <param name="properties">Desired owner-scoped property collection replacing the active stored set.</param>
    /// <param name="cancellationToken">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <param name="preserve">Optional predicate selecting existing properties that replacement must retain.</param>
    /// <returns>A task that completes after validated inserts, updates, and eligible deletions.</returns>
    /// <exception cref="ArgumentNullException">The replacement property collection is null.</exception>
    /// <exception cref="ArgumentException">A property is invalid or the replacement contains a duplicate natural key.</exception>
    internal async Task ReplacePreservingAsync(GXDbConnection connection, IDbTransaction? transaction,
        TOwner owner, IEnumerable<T> properties, CancellationToken cancellationToken, Func<T, bool>? preserve)
    {
        ArgumentNullException.ThrowIfNull(properties);
        cancellationToken.ThrowIfCancellationRequested();
        var items = properties.Where(p => preserve?.Invoke(p) != true).ToArray();
        var keys = new HashSet<(string? Module, string Name)>();
        foreach (var item in items)
        {
            Validate(owner, item);
            if (!keys.Add((item.Module?.Id, item.Name!)))
            {
                throw new ArgumentException("A property key occurs more than once.", nameof(properties));
            }
        }
        if (transaction == null)
        {
            await using var local = await connection.BeginTransactionAsync(cancellationToken);
            await ReplacePreservingAsync(connection, local, owner, items, cancellationToken, preserve);
            await local.CommitAsync(cancellationToken);
            return;
        }
        var existing = await ListAsync(connection, transaction, owner, cancellationToken);
        foreach (var item in items)
        {
            var row = existing.SingleOrDefault(p => p.Module?.Id == item.Module?.Id && p.Name == item.Name);
            await SaveAsync(connection, transaction, owner, item, row, cancellationToken: cancellationToken);
        }
        foreach (var row in existing.Where(p => preserve?.Invoke(p) != true && !keys.Contains((p.Module?.Id, p.Name!))))
        {
            await DeleteAsync(connection, transaction, owner, row, cancellationToken);
        }
    }

    /// <summary>Checks whether a column selection includes the stored value property.</summary>
    /// <param name="expression">Column-selection expression to inspect.</param>
    /// <returns>True when the selection includes the value column.</returns>
    private static bool IncludesValue(Expression expression) => expression switch
    {
        MemberExpression member => member.Member.Name == nameof(IGXProperty.Value),
        UnaryExpression unary => IncludesValue(unary.Operand),
        NewExpression tuple => tuple.Arguments.Any(IncludesValue),
        NewArrayExpression array => array.Expressions.Any(IncludesValue),
        _ => false
    };
}
