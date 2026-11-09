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

using System.Collections.Concurrent;
using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.UI.Components;
using Gurux.UI.Components.Enums;
using Microsoft.AspNetCore.Components.Authorization;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Keeps one notification per user-initiated read until its tasks finish.</summary>
public sealed class GXReadTaskNotifications : IDisposable
{
    private readonly HttpClient _http;
    private readonly IGXNotificationService _notifications;
    private readonly AuthenticationStateProvider _authentication;
    private readonly TimeSpan _interval;
    private Task<AuthenticationState> _authenticationState;
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _running = new();
    private readonly ConcurrentDictionary<Guid, byte> _owned = new();
    private bool _disposed;

    public GXReadTaskNotifications(HttpClient http, IGXNotificationService notifications,
        AuthenticationStateProvider authentication, TimeSpan? pollInterval = null)
    {
        _http = http;
        _notifications = notifications;
        _authentication = authentication;
        _authenticationState = authentication.GetAuthenticationStateAsync();
        _interval = pollInterval ?? TimeSpan.FromSeconds(2);
        _authentication.AuthenticationStateChanged += AuthenticationChanged;
    }

    /// <summary>Tracks the persisted task IDs returned by the server, including expanded tasks.</summary>
    public void Track(IEnumerable<Guid>? taskIds)
    {
        var ids = taskIds?.Where(id => id != Guid.Empty).Distinct().ToArray() ?? [];
        if (_disposed || ids.Length == 0) {
            return;
        }
        var item = new GXNotificationItem(NotificationLevel.Information, Properties.Resources.ReadTasksQueued,
            string.Format(Properties.Resources.ReadTasksProgress, 0, ids.Length), Scope: NotificationScope.Global, Closable: true);
        var cancellation = new CancellationTokenSource();
        _owned[item.Id] = 0;
        _running[item.Id] = cancellation;
        _notifications.Add(item);
        _ = WatchAsync(ids, item, cancellation);
    }

    private async Task WatchAsync(Guid[] ids, GXNotificationItem item, CancellationTokenSource cancellation)
    {
        try
        {
            while (true)
            {
                cancellation.Token.ThrowIfCancellationRequested();
                // Respect an explicitly dismissed notification.
                if (!_notifications.Notifications.Any(n => n.Id == item.Id)) {
                    break;
                }
                var response = await _http.PostAsJson<ListTasksResponse>("api/Task/List",
                    new ListTasks { Included = ids, Count = ids.Length }, cancellation.Token);
                cancellation.Token.ThrowIfCancellationRequested();
                if (!_notifications.Notifications.Any(n => n.Id == item.Id)) {
                    break;
                }
                var tasks = (response.Tasks ?? []).Where(t => ids.Contains(t.Id)).GroupBy(t => t.Id).Select(g => g.First()).ToArray();
                if (tasks.Length != ids.Length)
                    {
                    throw new InvalidOperationException(Properties.Resources.ReadTasksUnavailable);
                }
                int completed = tasks.Count(t => t.Ready != null);
                if (completed == ids.Length)
                {
                    var errors = tasks.Select(t => !string.IsNullOrWhiteSpace(t.Error) ? t.Error : t.Result)
                        .Where(error => !string.IsNullOrWhiteSpace(error)).Distinct().ToArray();
                    if (errors.Length != 0)
                        {
                        _notifications.Add(item with { Level = NotificationLevel.Error, Title = Properties.Resources.ReadTasksFailed, Description = string.Join(Environment.NewLine, errors) });
                    }
                    else
                    {
                        _notifications.Remove(item.Id);
                        _owned.TryRemove(item.Id, out _);
                    }
                    return;
                }
                _notifications.Add(item with
                {
                    Title = tasks.Any(t => t.Start != null || t.Ready != null) ? Properties.Resources.ReadTasksInProgress : Properties.Resources.ReadTasksQueued,
                    Description = string.Format(Properties.Resources.ReadTasksProgress, completed, ids.Length)
                });
                await Task.Delay(_interval, cancellation.Token);
            }
            _owned.TryRemove(item.Id, out _);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception error)
        {
            if (!_disposed && !cancellation.IsCancellationRequested)
                {
                _notifications.Add(item with { Level = NotificationLevel.Error, Title = Properties.Resources.ReadTasksUnavailable, Description = error.Message });
            }
        }
        finally
        {
            _running.TryRemove(item.Id, out _);
            cancellation.Dispose();
        }
    }

    private async void AuthenticationChanged(Task<AuthenticationState> state)
    {
        var previous = _authenticationState;
        _authenticationState = state;
        try
        {
            var oldUser = (await previous).User;
            var newUser = (await state).User;
            if (ReferenceEquals(state, _authenticationState) && UserKey(oldUser) != UserKey(newUser)) {
                Clear();
            }
        }
        catch { Clear(); }
    }
    private static string? UserKey(ClaimsPrincipal user) => user.Identity?.IsAuthenticated == true
        ? user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value ?? user.Identity.Name
        : null;
    private void Clear()
    {
        foreach (var cancellation in _running.Values)
            {
            try { cancellation.Cancel(); } catch (ObjectDisposedException) { }
        }
        foreach (var id in _owned.Keys) {
            _notifications.Remove(id);
        }
        _owned.Clear();
    }
    public void Dispose()
    {
        if (_disposed) {
            return;
        }
        _disposed = true;
        _authentication.AuthenticationStateChanged -= AuthenticationChanged;
        Clear();
    }
}
