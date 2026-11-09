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
using System.Reflection;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Adds missing registered trigger definitions while preserving configured resources.</summary>
internal static class GXTriggerDefaults
{
    public static async Task<List<GXTrigger>> EnsureAsync(GXDbConnection connection,
        IDbTransaction? transaction, Assembly assembly, CancellationToken cancellationToken = default)
    {
        List<GXTrigger> changed = [];
        foreach (Type type in assembly.GetTypes())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var definitions = GXTriggerEventCatalog.GetActivities(type);
            if (definitions.Count == 0)
                {
                continue;
            }
            var args = GXSelectArgs.SelectAll<GXTrigger>(t => t.ClassName == type.FullName);
            args.Columns.Add<GXTriggerActivity>();
            args.Joins.AddLeftJoin<GXTrigger, GXTriggerActivity>(t => t.Id, a => a.Trigger!);
            var trigger = transaction == null
                ? connection.SingleOrDefault<GXTrigger>(args)
                : connection.SingleOrDefault<GXTrigger>(transaction, args);
            if (trigger == null)
            {
                var instance = (ITriggerAction)Activator.CreateInstance(type)!;
                trigger = new GXTrigger
                {
                    ClassName = type.FullName, Name = instance.Name, Icon = instance.Icon,
                    ConfigurationUI = instance.ConfigurationUI?.FullName, Enabled = true,
                    CreationTime = DateTime.Now, Activities = []
                };
                // Insert activities explicitly so every write uses the supplied transaction.
                await connection.InsertAsync(transaction, GXInsertArgs.Insert(trigger), cancellationToken);
                changed.Add(trigger);
            }
            trigger.Activities ??= [];
            foreach (var definition in definitions)
            {
                var activity = trigger.Activities.FirstOrDefault(a => a.Name == definition.Name);
                if (activity == null)
                {
                    activity = new GXTriggerActivity { Name = definition.Name, Permission = definition.Permission, Trigger = trigger };
                    await connection.InsertAsync(transaction, GXInsertArgs.Insert(activity), cancellationToken);
                    trigger.Activities.Add(activity);
                    if (!changed.Contains(trigger)) {
                        changed.Add(trigger);
                    }
                }
                else if (activity.Permission != definition.Permission)
                {
                    // Permission is authoritative event metadata, never user configuration.
                    activity.Permission = definition.Permission;
                    await connection.UpdateAsync(transaction, GXUpdateArgs.Update(activity, a => a.Permission!), cancellationToken);
                    if (!changed.Contains(trigger)) {
                        changed.Add(trigger);
                    }
                }
            }
        }
        return changed;
    }
}
