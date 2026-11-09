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

using System.Reflection;
using Gurux.DLMS.AMI.Shared.Enums;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Authoritative catalog of explicitly registered, published trigger events.</summary>
internal static class GXTriggerEventCatalog
{
    internal sealed record Activity(string Name, string Permission);

    public static IReadOnlyList<Activity> GetActivities(Type type)
    {
        if (type.IsAbstract || !typeof(ITriggerAction).IsAssignableFrom(type))
            {
            return [];
        }
        return type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => new { Name = f.GetRawConstantValue() as string, Registration = f.GetCustomAttribute<GXTriggerEventAttribute>() })
            .Where(f => f.Registration != null && !string.IsNullOrWhiteSpace(f.Name)
                && !string.Equals(f.Name, "View", StringComparison.OrdinalIgnoreCase)
                && !f.Name.EndsWith(".view", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(f.Registration.Permission)
                && !f.Registration.Permission.EndsWith(".view", StringComparison.OrdinalIgnoreCase))
            .Select(f => new Activity(f.Name!, f.Registration!.Permission))
            .DistinctBy(f => f.Name, StringComparer.Ordinal).ToArray();
    }

    /// <summary>Resolves a published event code to its registered built-in source and activity.</summary>
    internal static bool TryResolveEvent(string eventCode, out Type triggerType, out string activityName)
    {
        var definitions = typeof(GXTriggerEventCatalog).Assembly.GetTypes()
            .SelectMany(t => GetActivities(t).Select(a => (Type: t, Activity: a)))
            .Where(x => x.Activity.Name == eventCode || x.Activity.Permission == eventCode).ToArray();
        if (definitions.Length == 1)
        {
            triggerType = definitions[0].Type;
            activityName = definitions[0].Activity.Name;
            return true;
        }
        triggerType = null!;
        activityName = string.Empty;
        return false;
    }
    public static bool TryGetPermission(string? className, string? activityName, out string permission)
    {
        permission = string.Empty;
        if (string.IsNullOrEmpty(className) || string.IsNullOrEmpty(activityName))
            {
            return false;
        }
        // Registered module trigger types can live outside the server assembly. Ambiguous names fail closed.
        var types = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(className, false))
            .OfType<Type>().Where(t => typeof(ITriggerAction).IsAssignableFrom(t)).Distinct().ToArray();
        var type = types.Length == 1 ? types[0] : null;
        var activity = type == null ? null : GetActivities(type).FirstOrDefault(a => a.Name == activityName);
        if (activity == null)
            {
            return false;
        }
        permission = activity.Permission;
        return true;
    }
}
