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

using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Common;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Loads only relationships named by the explicit snapshot contract after the root's authorized domain read.</summary>
internal static class GXHistoryEntityRelations
{
    internal static async Task HydrateAsync(GXDbConnection db, object entity, CancellationToken token, bool root = true, int depth = 0)
    {
        if (depth > 8)
        {
            throw new InvalidOperationException("The owned history configuration graph is too deep.");
        }

        var type = entity.GetType();
        foreach (var property in GXHistorySnapshot.CaptureProperties(type, root))
        {
            var relation = property.GetCustomAttribute<ForeignKeyAttribute>();
            if (relation == null)
            {
                continue;
            }

            var child = property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(List<>)
                            ? property.PropertyType.GetGenericArguments()[0] : null;
            if (child != null)
            {
                if (child.GetProperty("Id") == null)
                {
                    continue;
                }

                object? values;
                if (relation.MapTable != null)
                {
                    values = await InvokeAsync(nameof(ReadMapped), [type, child, relation.MapTable], [db, entity, token]);
                }
                else
                {
                    values = await InvokeAsync(nameof(ReadOwned), [type, child], [db, entity, token]);
                }

                if (values == null)
                {
                    continue;
                }

                property.SetValue(entity, values);
                if (GXHistorySnapshot.IsOwned(child))
                {
                    foreach (var item in (IEnumerable)values)
                    {
                        await HydrateAsync(db, item!, token, false, depth + 1);
                    }
                }
            }
            else if (!property.PropertyType.IsValueType && property.PropertyType != typeof(string) && property.PropertyType.GetProperty("Id") is { } childId)
            {
                var valueType = childId.PropertyType;
                var identity = await InvokeAsync(nameof(ReadReference), [type, valueType], [db, entity, property, token]);
                if (identity == null || identity is Guid guid && guid == Guid.Empty || identity is int integer && integer == 0 || identity is long number && number == 0) { property.SetValue(entity, null); continue; }
                var reference = Activator.CreateInstance(property.PropertyType)!;
                childId.SetValue(reference, identity);
                property.SetValue(entity, reference);
            }
        }
    }

    private static async Task<object?> InvokeAsync(string method, Type[] arguments, object?[] values)
    {
        var task = (Task)typeof(GXHistoryEntityRelations).GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!.MakeGenericMethod(arguments).Invoke(null, values)!;
        await task;
        return task.GetType().GetProperty("Result")!.GetValue(task);
    }

    private static Expression<Func<T, object>> Column<T>(PropertyInfo property)
    {
        var p = Expression.Parameter(typeof(T), "row");
        return Expression.Lambda<Func<T, object>>(Expression.Convert(Expression.Property(p, property), typeof(object)), p);
    }

    private static Expression<Func<T, bool>> Equal<T>(PropertyInfo property, object value)
    {
        var p = Expression.Parameter(typeof(T), "row");
        if (property.PropertyType.GetProperty("Id") is { } identity && property.PropertyType.IsInstanceOfType(value))
        {
            var key = identity.GetValue(value) ?? throw new ArgumentException("A relationship owner identifier is required.");
            return Expression.Lambda<Func<T, bool>>(Expression.Equal(Expression.Convert(Expression.Property(p, property), typeof(object)), Expression.Constant(key, typeof(object))), p);
        }
        var right = property.PropertyType.IsInstanceOfType(value) ? value : Convert.ChangeType(value, property.PropertyType, System.Globalization.CultureInfo.InvariantCulture);
        return Expression.Lambda<Func<T, bool>>(Expression.Equal(Expression.Property(p, property), Expression.Constant(right, property.PropertyType)), p);
    }

    private static async Task<object?> ReadReference<T, TKey>(GXDbConnection db, object owner, PropertyInfo property, CancellationToken token)
        where T : class, new()
    {
        var identity = typeof(T).GetProperty("Id")!;
        var query = GXSelectArgs.Select(Column<T>(property), Equal<T>(identity, identity.GetValue(owner)!));
        var row = Expression.Parameter(typeof(T), "row");
        query.Where.And(Expression.Lambda<Func<T, bool>>(Expression.NotEqual(Expression.Property(row, property), Expression.Constant(null, property.PropertyType)), row));
        var values = await db.SingleOrDefaultAsync<object[]>(query, token);
        var value = values?.FirstOrDefault();
        if (value == null || value is DBNull)
        {
            return null;
        }

        if (typeof(TKey) == typeof(Guid))
        {
            return value is Guid guid ? guid : value is byte[] bytes ? new Guid(bytes) : Guid.Parse(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)!);
        }

        if (typeof(TKey) == typeof(string))
        {
            return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        return Convert.ChangeType(value, typeof(TKey), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<List<TChild>?> ReadMapped<TOwner, TChild, TMap>(GXDbConnection db, object owner, CancellationToken token)
        where TChild : class, new() where TMap : class, new()
    {
        var source = typeof(TMap).GetProperties().SingleOrDefault(p => p.GetCustomAttribute<ForeignKeyAttribute>()?.Type == typeof(TOwner));
        var target = typeof(TMap).GetProperties().SingleOrDefault(p => p.GetCustomAttribute<ForeignKeyAttribute>()?.Type == typeof(TChild));
        if (source == null || target == null || source == target)
        {
            return null;
        }

        var ownerIdentity = typeof(TOwner).GetProperty("Id")!.GetValue(owner)!;
        if (!source.PropertyType.IsInstanceOfType(ownerIdentity))
        {
            throw new InvalidOperationException($"History relationship {typeof(TMap).Name} has an incompatible owner identifier.");
        }

        var query = ChildQuery<TChild>();
        query.Joins.AddInnerJoin(Column<TMap>(target), Column<TChild>(typeof(TChild).GetProperty("Id")!));
        query.Where.And(Equal<TMap>(source, ownerIdentity));
        Active<TMap>(query);
        return await db.SelectAsync<TChild>(query, token);
    }

    private static async Task<List<TChild>?> ReadOwned<TOwner, TChild>(GXDbConnection db, object owner, CancellationToken token)
        where TChild : class, new()
    {
        var ownerProperty = typeof(TChild).GetProperties().FirstOrDefault(p => p.PropertyType == typeof(TOwner) && p.GetCustomAttribute<ForeignKeyAttribute>() != null);
        if (ownerProperty == null)
        {
            return null;
        }

        var query = ChildQuery<TChild>();
        query.Where.And(Equal<TChild>(ownerProperty, owner));
        return await db.SelectAsync<TChild>(query, token);
    }

    private static GXSelectArgs ChildQuery<T>() where T : class, new()
    {
        var query = GXHistorySnapshot.IsOwned(typeof(T)) ? GXSelectArgs.SelectAll<T>() : GXSelectArgs.Select(Column<T>(typeof(T).GetProperty("Id")!));
        Active<T>(query);
        return query;
    }

    private static void Active<T>(GXSelectArgs query)
    {
        var removed = typeof(T).GetProperty("Removed");
        if (removed == null || removed.PropertyType.IsValueType && Nullable.GetUnderlyingType(removed.PropertyType) == null)
        {
            return;
        }

        var p = Expression.Parameter(typeof(T), "row");
        query.Where.And(Expression.Lambda<Func<T, bool>>(Expression.Equal(Expression.Property(p, removed), Expression.Constant(null, removed.PropertyType)), p));
    }
}
