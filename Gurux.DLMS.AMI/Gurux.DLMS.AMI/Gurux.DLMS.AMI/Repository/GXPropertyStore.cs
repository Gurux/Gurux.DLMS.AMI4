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
    private readonly PropertyInfo ownerProperty = (PropertyInfo)((MemberExpression)ownerColumn.Body).Member;
    private static readonly PropertyInfo CreationTime = typeof(T).GetProperty("CreationTime")!;
    private static readonly PropertyInfo Updated = typeof(T).GetProperty("Updated")!;
    private static readonly Expression<Func<T, object>> ValueAndUpdated = UpdateColumns(true);
    private static readonly Expression<Func<T, object>> UpdatedOnly = UpdateColumns(false);

    private static Expression<Func<T, object>> UpdateColumns(bool includeValue)
    {
        var p = Expression.Parameter(typeof(T), "p");
        var columns = (includeValue ? new[] { nameof(IGXProperty.Value), "Updated" } : ["Updated"])
            .Select(name => Expression.Convert(Expression.Property(p, name), typeof(object)));
        return Expression.Lambda<Func<T, object>>(Expression.NewArrayInit(typeof(object), columns), p);
    }

    [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Preserves the existing instance API used by repository consumers.")]

    internal void ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 64)
        {
            throw new ArgumentException("A property name of at most 64 characters is required.", nameof(name));
        }
    }

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
        if (ownerProperty.GetValue(property) is TOwner supplied &&
            !EqualityComparer<TKey>.Default.Equals(supplied.Id, owner.Id))
        {
            throw new ArgumentException("A property cannot be moved to another owner.");
        }
        if (property.Module != null && string.IsNullOrWhiteSpace(property.Module.Id))
        {
            throw new ArgumentException("A valid module identifier is required.");
        }
    }

    private Expression<Func<T, bool>> OwnerPredicate(TOwner owner)
    {
        var parameter = Expression.Parameter(typeof(T), "p");
        Expression<Func<TOwner>> value = () => owner;
        return Expression.Lambda<Func<T, bool>>(Expression.Equal(
            Expression.Property(parameter, ownerProperty), value.Body), parameter);
    }

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
            ownerProperty.SetValue(row, owner);
        }

        return rows;
    }

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
            ownerProperty.SetValue(row, owner);
            row.Module = module;
        }
        return row;
    }

    internal async Task<bool> UpsertAsync(GXDbConnection connection, IDbTransaction? transaction,
        TOwner owner, T property, Expression<Func<T, object>>? columns = null,
        CancellationToken cancellationToken = default)
    {
        Validate(owner, property);
        var existing = await FindAsync(connection, transaction, owner, property.Module, property.Name!, cancellationToken);
        return await SaveAsync(connection, transaction, owner, property, existing, columns, cancellationToken);
    }

    // Existing is read in the caller's transaction so it can authorize Add or Edit before writing.
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
            ownerProperty.SetValue(property, owner);
            CreationTime.SetValue(property, CreationTime.GetValue(existing));
            Updated.SetValue(property, DateTimeOffset.UtcNow);
            // Metadata, ownership and logical keys are never client-writable.
            var selected = columns == null || IncludesValue(columns.Body) ? ValueAndUpdated : UpdatedOnly;
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
        CreationTime.SetValue(property, DateTimeOffset.UtcNow);
        Updated.SetValue(property, null);
        ownerProperty.SetValue(property, owner);
        await connection.InsertAsync(transaction, GXInsertArgs.Insert(property), cancellationToken);
        return true;
    }

    internal async Task DeleteAsync(GXDbConnection connection, IDbTransaction? transaction,
        TOwner owner, T property, CancellationToken cancellationToken = default)
    {
        if (!await TryDeleteAsync(connection, transaction, owner, property, cancellationToken))
        {
            throw new ArgumentException("The property was edited by another operation.");
        }
    }

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

    internal Task ReplaceAsync(GXDbConnection connection, IDbTransaction? transaction,
        TOwner owner, IEnumerable<T> properties, CancellationToken cancellationToken = default)
        => ReplacePreservingAsync(connection, transaction, owner, properties, cancellationToken, null);

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

    private static bool IncludesValue(Expression expression) => expression switch
    {
        MemberExpression member => member.Member.Name == nameof(IGXProperty.Value),
        UnaryExpression unary => IncludesValue(unary.Operand),
        NewExpression tuple => tuple.Arguments.Any(IncludesValue),
        NewArrayExpression array => array.Expressions.Any(IncludesValue),
        _ => false
    };
}
