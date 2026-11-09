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
using System.Reflection;
using System.Runtime.Serialization;
using Gurux.DLMS.AMI.Shared;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Validates date ranges and applies the same inclusive predicates to stored rows.</summary>
internal static class GXDateTimeFilters
{
    /// <summary>Adds date ranges before an ORM query is counted or paged.</summary>
    public static bool Apply<T>(GXSelectArgs query, IEnumerable<GXDateTimeFilter>? filters)
    {
        var predicate = Create<T>(filters);
        if (predicate == null) {
            return false;
        }
        query.Where.And(predicate);
        return true;
    }

    /// <summary>Applies DTO date ranges to a storage type with matching property names.</summary>
    public static bool Apply<TFilter, TStorage>(GXSelectArgs query, IEnumerable<GXDateTimeFilter>? filters)
    {
        var ranges = filters?.ToArray();
        _ = Create<TFilter>(ranges);
        return Apply<TStorage>(query, ranges);
    }

    /// <summary>Builds a typed AND predicate, validating all entries even when a range has no bounds.</summary>
    public static Expression<Func<T, bool>>? Create<T>(IEnumerable<GXDateTimeFilter>? filters)
    {
        if (filters == null) {
            return null;
        }
        var parameter = Expression.Parameter(typeof(T), "row");
        Expression? body = null;
        foreach (var filter in filters)
        {
            if (filter == null || string.IsNullOrWhiteSpace(filter.Property))
                {
                throw new ArgumentException("A date filter must specify a property.", nameof(filters));
            }
            var property = typeof(T).GetProperty(filter.Property, BindingFlags.Instance | BindingFlags.Public);
            if (property?.GetMethod?.IsPublic != true || property.GetIndexParameters().Length != 0)
                {
                throw new ArgumentException("Unknown date filter property.", nameof(filters));
            }
            if ((typeof(T).IsDefined(typeof(DataContractAttribute)) && !property.IsDefined(typeof(DataMemberAttribute))) ||
                property.IsDefined(typeof(IgnoreDataMemberAttribute)) ||
                (property.GetCustomAttribute<IgnoreAttribute>()?.IgnoreType & IgnoreType.Db) == IgnoreType.Db)
                {
                throw new ArgumentException("A date filter property must be stored in the database.", nameof(filters));
            }
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (type != typeof(DateTime) && type != typeof(DateTimeOffset))
                {
                throw new ArgumentException("A date filter property must be a date and time value.", nameof(filters));
            }
            if (filter.From > filter.To)
                {
                throw new ArgumentException("A date filter's lower bound must not exceed its upper bound.", nameof(filters));
            }
            var member = Expression.Property(parameter, property);
            if (filter.From is DateTimeOffset from)
                {
                Add(Expression.GreaterThanOrEqual(member, Bound(from, property.PropertyType, type)));
            }
            if (filter.To is DateTimeOffset to)
                {
                Add(Expression.LessThanOrEqual(member, Bound(to, property.PropertyType, type)));
            }
        }
        return body == null ? null : Expression.Lambda<Func<T, bool>>(body, parameter);

        void Add(Expression condition) => body = body == null ? condition : Expression.AndAlso(body, condition);
    }

    private static ConstantExpression Bound(DateTimeOffset value, Type propertyType, Type valueType)
        => Expression.Constant(valueType == typeof(DateTime) ? (object)value.UtcDateTime : value.ToUniversalTime(), propertyType);
}
