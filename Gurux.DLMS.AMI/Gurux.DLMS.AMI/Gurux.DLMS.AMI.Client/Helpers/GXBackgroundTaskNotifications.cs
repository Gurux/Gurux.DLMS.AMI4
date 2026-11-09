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

using System.Text.Json;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Http;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.UI.Components;
using Gurux.UI.Components.Enums;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Tracks notifications for update checks across page navigation, using each execution's RunId.</summary>
public sealed class GXBackgroundTaskNotifications : IDisposable
{
    private sealed record Pending(string TaskId, GXNotificationItem Item);
    private readonly HttpClient _http;
    private readonly IGXNotificationService _notifications;
    private readonly IGXNotifier? _notifier;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Lock _gate = new();
    private readonly Dictionary<Guid, Pending> _active = [];
    private readonly Dictionary<Guid, GXNotificationItem> _pending = [];
    private readonly Dictionary<Guid, GXBackgroundTaskStatus> _completed = [];
    private readonly Queue<Guid> _completionOrder = [];
    private bool _disposed;

    /// <summary>Subscribes independently of the page that starts an update check.</summary>
    public GXBackgroundTaskNotifications(HttpClient http, IGXNotificationService notifications, IServiceProvider services)
    {
        _http = http;
        _notifications = notifications;
        _notifier = services.GetService<IGXNotifier>();
        _notifier?.On<IEnumerable<GXTarget>>(this, nameof(IGXHubEvents.OnUpdate), OnUpdated);
    }

    /// <summary>Queues a manufacturer catalog check and tracks its notification.</summary>
    public Task<Guid> CheckManufacturersAsync(CancellationToken token = default) => StartAsync(
        "update.manufacturers", Properties.Resources.Manufacturers, GXProgress.CheckingManufacturers,
        async ct => (await _http.PostAsJson<CheckManufacturerResponse>("api/Manufacturer/Check", new CheckManufacturer(), ct)).RunId, token);

    /// <summary>Queues a module version check and tracks its notification.</summary>
    public Task<Guid> CheckModulesAsync(CancellationToken token = default) => StartAsync(
        "update.modules", Properties.Resources.ModulesTitle, GXProgress.CheckingNewVersions,
        async ct => (await _http.PostAsJson<CheckModuleResponse>("api/Module/Check", new CheckModule(), ct)).RunId, token);

    /// <summary>Queues an agent version check and tracks its notification.</summary>
    public Task<Guid> CheckAgentsAsync(CancellationToken token = default) => StartAsync(
        "update.agents", Properties.Resources.Agents, GXProgress.CheckingNewVersions,
        async ct => (await _http.PostAsJson<CheckModuleResponse>("api/Agent/Check", new CheckModule(), ct)).RunId, token);

    private async Task<Guid> StartAsync(string taskId, string title, string message,
        Func<CancellationToken, Task<Guid?>> start, CancellationToken token)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(token, _lifetime.Token);
        cancellation.Token.ThrowIfCancellationRequested();
        var item = new GXNotificationItem(NotificationLevel.Information, title, message,
            "background-task:pending:" + Guid.NewGuid(), true, NotificationScope.Global);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _pending.Add(item.Id, item);
            _notifications.Add(item);
        }
        try
        {
            var runId = await start(cancellation.Token);
            if (runId is null || runId == Guid.Empty)
            {
                throw new InvalidOperationException("The update check did not return an execution ID.");
            }
            lock (_gate)
            {
                cancellation.Token.ThrowIfCancellationRequested();
                ObjectDisposedException.ThrowIf(_disposed, this);
                _pending.Remove(item.Id);
                var tracked = new Pending(taskId, item with { Key = "background-task:" + runId.Value.ToString("D") });
                if (!_active.TryAdd(runId.Value, tracked))
                {
                    throw new InvalidOperationException("The update check returned an execution ID that is already being tracked.");
                }
                // Keep the same notification identity when associating the server's run ID.
                _notifications.Add(tracked.Item);
                if (_completed.TryGetValue(runId.Value, out var completed) && completed.TaskId == taskId)
                {
                    Finish(completed);
                }
            }
            return runId.Value;
        }
        catch
        {
            lock (_gate)
            {
                _pending.Remove(item.Id);
                _notifications.Remove(item.Id);
            }
            throw;
        }
    }

    private void OnUpdated(IEnumerable<GXTarget> items)
    {
        foreach (var item in items.Where(i => i.Type == GXConfigurationPolicies.Cron && i.Id == GXCronNotifications.Completed))
        {
            if (string.IsNullOrWhiteSpace(item.Settings))
            {
                continue;
            }
            GXBackgroundTaskStatus? status;
            try
            { status = JsonSerializer.Deserialize<GXBackgroundTaskStatus>(item.Settings); }
            catch (JsonException) { continue; }
            if (status == null || status.RunId == Guid.Empty ||
                status.TaskId is not ("update.manufacturers" or "update.modules" or "update.agents") ||
                status.State is not (BackgroundTaskState.Succeeded or BackgroundTaskState.Failed or BackgroundTaskState.Cancelled))
            {
                continue;
            }
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }
                // Completion can arrive before the HTTP response containing the execution ID.
                // Bound the retained early/duplicate completion snapshots.
                if (_completed.TryAdd(status.RunId, status))
                {
                    _completionOrder.Enqueue(status.RunId);
                    while (_completionOrder.Count > 256)
                    {
                        _completed.Remove(_completionOrder.Dequeue());
                    }
                }
                Finish(status);
            }
        }
    }

    private void Finish(GXBackgroundTaskStatus status)
    {
        if (!_active.TryGetValue(status.RunId, out var tracked) || tracked.TaskId != status.TaskId)
        {
            return;
        }
        _active.Remove(status.RunId);
        _notifications.Remove(tracked.Item.Id);
        if (status.State == BackgroundTaskState.Succeeded)
        {
            return;
        }
        var description = status.State == BackgroundTaskState.Cancelled
            ? "Update check cancelled."
            : status.Error ?? "Update check failed. See the server log for details.";
        _notifications.Add(tracked.Item with
        {
            Level = status.State == BackgroundTaskState.Cancelled ? NotificationLevel.Warning : NotificationLevel.Error,
            Description = description
        });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            foreach (var item in _pending.Values.Concat(_active.Values.Select(p => p.Item)))
            {
                _notifications.Remove(item.Id);
            }
            _active.Clear();
            _pending.Clear();
            _completed.Clear();
            _completionOrder.Clear();
        }
        _notifier?.RemoveListener(this);
        _lifetime.Cancel();
        _lifetime.Dispose();
    }
}
