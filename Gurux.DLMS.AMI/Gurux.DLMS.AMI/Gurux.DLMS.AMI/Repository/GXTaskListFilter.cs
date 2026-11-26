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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Applies task-list criteria without mixing task and related-entity columns.</summary>
public static class GXTaskListFilter
{
    /// <summary>Adds task and related-entity predicates to the list query before counting or paging.</summary>
    /// <param name="query">The task query receiving the filters.</param>
    /// <param name="filter">Optional task criteria supplied by the caller.</param>
    public static void Apply(GXSelectArgs query, GXTask? filter)
    {
        if (filter == null)
        {
            return;
        }
        // Keep the caller's filter intact, including references reused by UI or later queries.
        var scalar = new GXTask();
        foreach (var property in typeof(GXTask).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.CanRead && property.CanWrite)
            {
                property.SetValue(scalar, property.GetValue(filter));
            }
        }
        ApplyRelation(query, scalar, filter.Creator, task => task.Creator!);
        ApplyRelation(query, scalar, filter.ScriptMethod, task => task.ScriptMethod!);
        ApplyRelation(query, scalar, filter.Module, task => task.Module!);
        ApplyRelation(query, scalar, filter.DeviceGroup, task => task.DeviceGroup!);
        ApplyRelation(query, scalar, filter.Device, task => task.Device!);
        ApplyRelation(query, scalar, filter.Object, task => task.Object!);
        ApplyRelation(query, scalar, filter.Attribute, task => task.Attribute!);
        ApplyRelation(query, scalar, filter.TriggerSchedule, task => task.TriggerSchedule!);
        ApplyRelation(query, scalar, filter.TriggerUser, task => task.TriggerUser!);
        ApplyRelation(query, scalar, filter.TriggerScript, task => task.TriggerScript!);
        ApplyRelation(query, scalar, filter.TriggerModule, task => task.TriggerModule!);
        ApplyRelation(query, scalar, filter.OperatingAgent, task => task.OperatingAgent!);
        ApplyRelation(query, scalar, filter.Condition, task => task.Condition!);
        ApplyRelation(query, scalar, filter.Action, task => task.Action!);
        query.Where.FilterBy(scalar);
    }

    /// <summary>Filters a foreign key through its related table, keeping removal predicates within that table.</summary>
    /// <typeparam name="T">The mapped related entity type.</typeparam>
    /// <param name="query">The task query receiving the foreign-key predicate.</param>
    /// <param name="scalar">The private filter copy from which the handled relationship is removed.</param>
    /// <param name="related">The caller's related-entity criteria.</param>
    /// <param name="foreignKey">The task property referring to the related entity.</param>
    private static void ApplyRelation<T>(GXSelectArgs query, GXTask scalar, T? related,
        Expression<Func<GXTask, object>> foreignKey) where T : class
    {
        if (related == null)
        {
            return;
        }
        var parameter = Expression.Parameter(typeof(T), "entity");
        var id = Expression.Convert(Expression.Property(parameter, nameof(GXTask.Id)), typeof(object));
        var relatedQuery = GXSelectArgs.Select(Expression.Lambda<Func<T, object>>(id, parameter));
        relatedQuery.Where.FilterBy(related);
        var predicate = Expression.Call(typeof(GXSql), nameof(GXSql.In), Type.EmptyTypes,
            foreignKey.Body, Expression.Constant(relatedQuery));
        query.Where.And(Expression.Lambda<Func<GXTask, bool>>(predicate, foreignKey.Parameters));
        var member = foreignKey.Body is UnaryExpression conversion ? conversion.Operand : foreignKey.Body;
        ((PropertyInfo)((MemberExpression)member).Member).SetValue(scalar, null);
    }
}
