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
using Gurux.DLMS.AMI.Client;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Module;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Gurux.DLMS.AMI.Server.Module;

public sealed partial class GXModuleRuntime
{
    private sealed class DownstreamFailure(Exception error) : Exception("Downstream request failed.", error);

    // Rejected input, access denial, an unavailable dependency, and cancellation are controlled operation errors.
    private static bool IsModuleFault(Exception error) => error is not
        (ArgumentException or UnauthorizedAccessException or BadHttpRequestException or AmiModuleUnavailableException or OperationCanceledException);

    private async Task ObserveBackgroundAsync(string id, Entry entry, Task execution)
    {
        try
        {
            await execution;
        }
        catch (OperationCanceledException) when (entry.Lifetime.IsCancellationRequested || entry.State != AmiModuleState.Active)
        {
        }
        catch (Exception error)
        {
            await FaultAsync(id, entry, error);
        }
    }

    private async Task FaultAsync(string id, Entry entry, Exception error)
    {
        var affected = new List<(string Id, Entry Entry)>();
        lock (sync)
        {
            if (!ReferenceEquals(entries.GetValueOrDefault(id), entry) || entry.State is AmiModuleState.Stopping or AmiModuleState.Failed)
            {
                return;
            }

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { id };
            var queue = new Queue<string>();
            queue.Enqueue(id);
            while (queue.TryDequeue(out var provider))
            {
                foreach (var dependent in dependencyGraph.GetBlockingDependents(provider))
                {
                    if (ids.Add(dependent))
                    {
                        queue.Enqueue(dependent);
                    }
                }
            }

            foreach (var moduleId in dependencyGraph.GetStopOrder(ids))
            {
                if (!entries.TryGetValue(moduleId, out var item))
                {
                    continue;
                }

                item.StopRequestedAt ??= DateTimeOffset.UtcNow;
                item.Faulted = true;
                item.State = AmiModuleState.Stopping;
                item.OwnedEndpoints.Clear();
                item.Sources = [];
                manifests[moduleId] = item.Manifest = item.Manifest with
                {
                    State = AmiModuleState.Failed,
                    Error = moduleId == id ? error.Message : $"Required module '{id}' failed: {error.Message}"
                };
                foreach (var contract in item.Exports)
                {
                    if (exports.GetValueOrDefault(contract) == moduleId)
                    {
                        exports.Remove(contract);
                    }
                }

                if (item.Calls == 0)
                {
                    item.Drained.TrySetResult();
                }

                affected.Add((moduleId, item));
            }
        }
        RefreshServiceAvailability();
        logger.LogError(error, "Module {Id} failed; its runtime registrations were withdrawn.", id);
        foreach (var item in affected)
        {
            Cancel(item.Entry, item.Id);
        }

        var timeout = TimeSpan.FromSeconds(configuration.GetValue("Modules:StopTimeoutSeconds", 30));
        foreach (var item in affected)
        {
            lock (sync)
            {
                var cleanup = item.Entry.Cleanup ??= ScheduleCleanup(item.Id, item.Entry);
                item.Entry.ForcedSignal ??= SignalForcedCancellationAsync(item.Entry, cleanup, timeout, item.Id);
            }
        }

        NotifyChanged();
        foreach (var item in affected)
        {
            if (Faulted != null)
            {
                foreach (Func<AmiModuleManifest, Task> subscriber in Faulted.GetInvocationList().Cast<Func<AmiModuleManifest, Task>>())
                {
                    try
                    {
                        await subscriber(item.Entry.Manifest);
                    }
                    catch (Exception failure)
                    {
                        logger.LogError(failure, "Cannot persist failed module {Id}.", item.Id);
                    }
                }
            }
        }
    }

    /// <summary>Performs the record startup failure operation.</summary>
    /// <param name="id">The item identifier.</param>
    /// <param name="name">The name.</param>
    /// <param name="error">The error.</param>
    public void RecordStartupFailure(string id, string name, Exception error)
    {
        lock (sync)
        {
            if (entries.ContainsKey(id))
            {
                return;
            }

            completedHealth.Remove(id);
            var manifest = manifests.GetValueOrDefault(id) ?? new AmiModuleManifest(id, name, "", "", AmiModuleState.Failed, []);
            manifests[id] = manifest with
            {
                State = AmiModuleState.Failed,
                Error = error.Message
            };
        }
        NotifyChanged();
    }
}

