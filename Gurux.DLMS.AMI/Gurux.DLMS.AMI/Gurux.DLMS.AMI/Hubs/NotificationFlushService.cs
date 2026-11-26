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

using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Microsoft.AspNetCore.SignalR;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Server.Module;
using System.Text.Json;
using Gurux.DLMS.AMI.Services.Database;

namespace Gurux.DLMS.AMI.Hubs
{
    /// <summary>Delivers buffered notifications to connected clients.</summary>
    /// <remarks>Initializes a new instance of <see cref="NotificationFlushService"/>.</remarks>
    /// <param name="cache">The cache.</param>
    /// <param name="hubContext">The hub context.</param>
    /// <param name="webPush">Background push delivery.</param>
    /// <param name="email">Background email delivery.</param>
    /// <param name="scopes">Creates scopes for notification delivery.</param>
    /// <param name="logger">Reports delivery failures.</param>
    public sealed class NotificationFlushService(
        NotificationCache cache,
        IHubContext<GuruxAMiHub> hubContext, GXWebPushNotifier webPush, GXEmailNotifier email,
        IServiceScopeFactory scopes, ILogger<NotificationFlushService> logger) : BackgroundService
    {
        /// <summary>Stores pending notifications until the next delivery interval.</summary>
        private readonly NotificationCache _cache = cache;

        /// <summary>Delivers authorized batches to connected SignalR clients.</summary>
        private readonly IHubContext<GuruxAMiHub> _hubContext = hubContext;

        /// <summary>Delivers original events through web push.</summary>
        private readonly GXWebPushNotifier _webPush = webPush;

        /// <summary>Resolves scoped delivery and module services.</summary>
        private readonly IServiceScopeFactory _scopes = scopes;

        /// <summary>Reports notification delivery failures.</summary>
        private readonly ILogger<NotificationFlushService> _logger = logger;

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using PeriodicTimer timer = new(TimeSpan.FromSeconds(1));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                IReadOnlyList<CachedNotification> cached = _cache.TakeAll();
                if (cached.Any())
                {
                    using var sqlSuppression = _scopes.CreateScope();
                    using var suppressRecording = sqlSuppression.ServiceProvider.GetService<IGXAmiSqlEvents>()?.Suppress();
                    foreach (var agentGroup in cached.Where(x => x.AgentId != null).GroupBy(x => x.AgentId!.Value))
                    {
                        GXTarget[] commands = [.. agentGroup.Select(x => x.Notification)];
                        await _hubContext.Clients.Group(GuruxAMiHub.AgentGroup(agentGroup.Key))
                            .SendAsync(nameof(IGXHubEvents.OnUpdate), commands, stoppingToken);
                    }
                    var byUser = cached.Where(x => x.AgentId == null).GroupBy(x => x.UserId).ToList();
                    var sent = new List<CachedNotification>();
                    try
                    {
                        foreach (var userGroup in byUser)
                        {
                            // Each module sees only this recipient's authorized batch; other channels retain originals.
                            var clientNotifications = userGroup.Select(x => x.Notification).ToList();
                            var runtime = sqlSuppression.ServiceProvider.GetService<GXModuleRuntime>();
                            if (runtime != null)
                            {
                                await runtime.Events.PublishAsync(("client.notifications", clientNotifications), stoppingToken);
                            }
                            GXTarget[] notifications = [.. clientNotifications];
                            await _hubContext.Clients.User(userGroup.Key).SendAsync(nameof(IGXHubEvents.OnUpdate), notifications, stoppingToken);
                            sent.AddRange(notifications.Select(notification => new CachedNotification { UserId = userGroup.Key, Notification = notification }));
                        }
                    }
                    finally
                    {
                        await RecordSentAsync(sent, stoppingToken);
                    }
                    await Parallel.ForEachAsync(byUser, new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = stoppingToken },
                        async (group, token) =>
                        {
                            GXTarget[] events = [.. group.Select(n => n.Notification)];
                            await _webPush.NotifyAsync(group.Key, events, token);
                            await email.NotifyAsync(group.Key, events, token);
                        });
                }
            }
        }

        /// <summary>Combines only identical payloads, keeping recipient attribution exact.</summary>
        public static IEnumerable<(GXTarget[] Notifications, string[] Recipients)> RecordingBatches(IEnumerable<CachedNotification> cached)
            => cached.GroupBy(n => n.UserId)
                .Select(g => (Notifications: g.Select(n => n.Notification).ToArray(), UserId: g.Key))
                .GroupBy(g => JsonSerializer.Serialize(g.Notifications), StringComparer.Ordinal)
                .Select(g => (g.First().Notifications, g.Select(n => n.UserId).Distinct(StringComparer.Ordinal).ToArray()));

        private async Task RecordSentAsync(IReadOnlyList<CachedNotification> cached, CancellationToken token)
        {
            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                var services = scope.ServiceProvider;
                var runtime = services.GetService<GXModuleRuntime>();
                if (runtime?.GetStartupProviders().Any(p => p.Services.Contains(typeof(IGXAmiSignalRRecorder).FullName!)) != true)
                {
                    return;
                }
            
                var modules = services.GetRequiredService<IGXAmiModuleServices>();
                foreach (var(Notifications, Recipients)in RecordingBatches(cached))
                {
                    await modules.InvokeAsync<IGXAmiSignalRRecorder>((recorder, ct) => recorder.RecordSentAsync(nameof(IGXHubEvents.OnUpdate), [Notifications], Recipients, ct), token);
                }
            }
            catch (OperationCanceledException)when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception error)
            {
                // Recording failure must not prevent notification or WebPush delivery.
                _logger.LogError(error, "Unable to record dispatched SignalR messages.");
            }
        }
    }
}


