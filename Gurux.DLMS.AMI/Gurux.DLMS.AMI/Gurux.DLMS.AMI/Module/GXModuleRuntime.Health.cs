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

using Gurux.DLMS.AMI.Module.Enums;
using Gurux.DLMS.AMI.Module.Health;
using Microsoft.Extensions.Hosting;
namespace Gurux.DLMS.AMI.Server.Module;
/// <summary>Coordinates module generations, dependency availability, event delivery, and lifecycle operations.</summary>
public sealed partial class GXModuleRuntime
{
    /// <summary>Final health snapshots retained after module generations stop.</summary>
    private readonly Dictionary<string, AmiModuleHealth> _completedHealth = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Reads status without resolving module services or acquiring a runtime lease.</summary>
    public AmiModuleHealth? GetHealth(string id)
    {
        lock (_sync)
        {
            if (_entries.TryGetValue(id, out var entry))
            {
                return SnapshotHealth(entry, entry.Manifest.State == AmiModuleState.Failed ? AmiModuleState.Failed : entry.State);
            }
            if (_completedHealth.TryGetValue(id, out var completed))
            {
                return completed;
            }
            if (_manifests.TryGetValue(id, out var manifest))
            {
                return new(id, "", manifest.State, 0, null, null, null, false, false,
                    manifest.Error == null ? null : "Module failed. See authorized module logs for details.", []);
            }
            return null;
        }
    }
    /// <summary>Snapshots each background service task state or returns the retained final service-health snapshot.</summary>
    /// <param name="entry">Loaded module generation whose lifecycle resources and call counts are managed.</param>
    /// <returns>Background service health entries for the generation.</returns>
    private static AmiModuleBackgroundHealth[] SnapshotBackground(Entry entry)
    {
        return entry.Background.Length == 0 ? entry.BackgroundHealth : [.. entry.Background.Select(service =>
        {
            var task = (service as BackgroundService)?.ExecuteTask;
            string state = task == null ? (entry.State == AmiModuleState.Stopping ? "Stopping" : "Started") :
                task.IsFaulted ? "Faulted" : task.IsCanceled ? "Canceled" : task.IsCompleted ? "Completed" :
                entry.State == AmiModuleState.Stopping ? "Stopping" : "Running";
            return new AmiModuleBackgroundHealth(service.GetType().Name, state);
        })];
    }
    /// <summary>Creates a module health snapshot with call counts, lifecycle times, cancellation state, and sanitized failure information.</summary>
    /// <param name="entry">Loaded module generation whose lifecycle resources and call counts are managed.</param>
    /// <param name="state">Lifecycle state included in the health snapshot.</param>
    /// <returns>The module generation's current health report.</returns>
    private static AmiModuleHealth SnapshotHealth(Entry entry, AmiModuleState state)
    {
        return new(entry.Manifest.Id, entry.Generation, state, entry.Calls, entry.StartedAt,
            entry.StopRequestedAt, entry.StoppedAt, entry.StoppedAt == null && entry.State == AmiModuleState.Stopping,
            entry.ForcedCancellation, entry.Manifest.Error != null || entry.CleanupError != null || state == AmiModuleState.Failed
                ? "Module failed. See authorized module logs for details." : null, SnapshotBackground(entry));
    }
}
