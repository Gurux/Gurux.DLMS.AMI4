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
using System.Globalization;
using System.Security.Claims;
using Gurux.DLMS.AMI.Http;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.UI.Components;
using Gurux.UI.Components.Enums;
using Microsoft.AspNetCore.Components.Authorization;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Keeps one notification per user-initiated read or write until its tasks finish.</summary>
public sealed class GXReadTaskNotifications : IDisposable
{
    /// <summary>Queries the persisted tasks returned by the task-submission API.</summary>
    private readonly HttpClient _http;
    /// <summary>Publishes and updates task progress notifications shared across pages.</summary>
    private readonly IGXNotificationService _notifications;
    /// <summary>Identifies the user whose task notifications must be cleared on account changes.</summary>
    private readonly AuthenticationStateProvider _authentication;
    /// <summary>Delay between successive task-status requests.</summary>
    private readonly TimeSpan _interval;
    /// <summary>Most recent authentication state used to detect changes of notification ownership.</summary>
    private Task<AuthenticationState> _authenticationState;
    /// <summary>Cancellation sources for notifications whose tasks are still being monitored.</summary>
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _running = new();
    /// <summary>Notifications retained until dismissal, successful completion, or account changes.</summary>
    private readonly ConcurrentDictionary<Guid, byte> _owned = new();
    /// <summary>Prevents tracking or publishing after the service is disposed.</summary>
    private bool _disposed;

    /// <summary>Creates a tracker that updates notifications while the initiating user remains signed in.</summary>
    /// <param name="http">HTTP client used to retrieve task status.</param>
    /// <param name="notifications">Notification service that owns the visible progress messages.</param>
    /// <param name="authentication">Authentication state used to isolate notifications between users.</param>
    /// <param name="pollInterval">Optional delay between polls; defaults to two seconds.</param>
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

    /// <summary>Tracks the persisted read task IDs returned by the server, including expanded tasks.</summary>
    /// <param name="taskIds">Persisted read task identifiers to monitor.</param>
    public void Track(IEnumerable<Guid>? taskIds) => Track(taskIds, writing: false);

    /// <summary>Tracks writes, removing their information message on success or retaining an error on failure.</summary>
    /// <param name="taskIds">Persisted write task identifiers returned by the server.</param>
    public void TrackWrites(IEnumerable<Guid>? taskIds) => Track(taskIds, writing: true);

    /// <summary>Creates one progress notification for a distinct set of persisted read or write tasks.</summary>
    /// <param name="taskIds">Persisted task identifiers, which may contain duplicates or empty values.</param>
    /// <param name="writing">Whether write-specific labels and immediate failure reporting are used.</param>
    private void Track(IEnumerable<Guid>? taskIds, bool writing)
    {
        var ids = taskIds?.Where(id => id != Guid.Empty).Distinct().ToArray() ?? [];
        if (_disposed || ids.Length == 0)
        {
            return;
        }
        var item = new GXNotificationItem(NotificationLevel.Information,
            writing ? Properties.Resources.WriteTasksQueued : Properties.Resources.ReadTasksQueued,
            Progress(writing, 0, ids.Length), Scope: NotificationScope.Global, Closable: true);
        var cancellation = new CancellationTokenSource();
        _owned[item.Id] = 0;
        _running[item.Id] = cancellation;
        _notifications.Add(item);
        _ = WatchAsync(ids, item, cancellation, writing);
    }

    /// <summary>Formats the localized completed-task count for one read or write operation.</summary>
    /// <param name="writing">Whether the operation writes attribute values.</param>
    /// <param name="completed">Number of persisted tasks that have completed.</param>
    /// <param name="total">Total number of persisted tasks being monitored.</param>
    /// <returns>The complete localized task-progress sentence.</returns>
    private static string Progress(bool writing, int completed, int total) => string.Format(CultureInfo.CurrentCulture,
        writing ? Properties.Resources.WriteTasksProgress : Properties.Resources.ReadTasksProgress, completed, total);

    /// <summary>Polls task status until completion, failure, explicit dismissal, or cancellation.</summary>
    /// <param name="ids">Distinct persisted task identifiers belonging to the operation.</param>
    /// <param name="item">Notification whose identifier is retained during progress and failure updates.</param>
    /// <param name="cancellation">Cancellation source owned by this polling operation.</param>
    /// <param name="writing">Whether write-specific labels and immediate failure reporting are used.</param>
    /// <returns>A task that completes after polling stops and its cancellation source is released.</returns>
    private async Task WatchAsync(Guid[] ids, GXNotificationItem item, CancellationTokenSource cancellation, bool writing)
    {
        string unavailable = writing ? Properties.Resources.WriteTasksUnavailable : Properties.Resources.ReadTasksUnavailable;
        try
        {
            while (true)
            {
                cancellation.Token.ThrowIfCancellationRequested();
                // Respect an explicitly dismissed notification.
                if (!_notifications.Notifications.Any(n => n.Id == item.Id))
                {
                    break;
                }
                var response = await _http.PostAsJson<ListTasksResponse>("api/Task/List",
                    new ListTasks { Included = ids, Count = ids.Length }, cancellation.Token);
                cancellation.Token.ThrowIfCancellationRequested();
                if (!_notifications.Notifications.Any(n => n.Id == item.Id))
                {
                    break;
                }
                var tasks = (response.Tasks ?? []).Where(t => ids.Contains(t.Id)).GroupBy(t => t.Id).Select(g => g.First()).ToArray();
                if (tasks.Length != ids.Length)
                {
                    throw new InvalidOperationException(unavailable);
                }
                int completed = tasks.Count(t => t.Ready != null);
                var errors = tasks.Where(t => !writing || t.Ready != null).Select(t => t.Error)
                    .Where(error => !string.IsNullOrWhiteSpace(error)).Distinct().ToArray();
                if (errors.Length != 0 && (writing || completed == ids.Length))
                {
                    _notifications.Add(item with
                    {
                        Level = NotificationLevel.Error,
                        Title = writing ? Properties.Resources.WriteTasksFailed : Properties.Resources.ReadTasksFailed,
                        Description = string.Join(Environment.NewLine, errors)
                    });
                    return;
                }
                if (completed == ids.Length)
                {
                    _notifications.Remove(item.Id);
                    _owned.TryRemove(item.Id, out _);
                    return;
                }
                _notifications.Add(item with
                {
                    Title = tasks.Any(t => t.Start != null || t.Ready != null)
                        ? (writing ? Properties.Resources.WriteTasksInProgress : Properties.Resources.ReadTasksInProgress)
                        : (writing ? Properties.Resources.WriteTasksQueued : Properties.Resources.ReadTasksQueued),
                    Description = Progress(writing, completed, ids.Length)
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
                _notifications.Add(item with { Level = NotificationLevel.Error, Title = unavailable, Description = error.Message });
            }
        }
        finally
        {
            _running.TryRemove(item.Id, out _);
            cancellation.Dispose();
        }
    }

    /// <summary>Clears tracked notifications when authentication changes to a different account or signs out.</summary>
    /// <param name="state">The newly published authentication state.</param>
    private async void AuthenticationChanged(Task<AuthenticationState> state)
    {
        var previous = _authenticationState;
        _authenticationState = state;
        try
        {
            var oldUser = (await previous).User;
            var newUser = (await state).User;
            if (ReferenceEquals(state, _authenticationState) && UserKey(oldUser) != UserKey(newUser))
            {
                Clear();
            }
        }
        catch { Clear(); }
    }

    /// <summary>Identifies the authenticated account that owns the visible task notifications.</summary>
    /// <param name="user">Principal whose stable account identifier is requested.</param>
    /// <returns>The authenticated user's identifier or name, or null for an anonymous principal.</returns>
    private static string? UserKey(ClaimsPrincipal user) => user.Identity?.IsAuthenticated == true
        ? user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value ?? user.Identity.Name
        : null;

    /// <summary>Cancels active polling and removes all notifications owned by this tracker.</summary>
    private void Clear()
    {
        foreach (var cancellation in _running.Values)
        {
            try
            { cancellation.Cancel(); }
            catch (ObjectDisposedException) { }
        }
        foreach (var id in _owned.Keys)
        {
            _notifications.Remove(id);
        }
        _owned.Clear();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _authentication.AuthenticationStateChanged -= AuthenticationChanged;
        Clear();
    }
}