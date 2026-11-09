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

using Gurux.DLMS.AMI.Shared.DTOs;
using System.Collections.Concurrent;
using Gurux.DLMS.AMI.Client.Shared;
namespace Gurux.DLMS.AMI.Hubs
{
    /// <summary>Buffers notifications for delivery to connected clients.</summary>
    public sealed class NotificationCache
    {
        private readonly ConcurrentQueue<CachedNotification> _items = new();
        private HashSet<string> _ignored = new(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _disabledSources = new(StringComparer.Ordinal);

        /// <summary>Replaces server-wide event exclusions; an empty selection allows every event.</summary>
        public void Configure(PerformanceSettings settings)
        {
            if (Server.Repository.GXHistoryScope.Defer(() => Configure(settings))) {
                return;
            }
            Volatile.Write(ref _ignored, new HashSet<string>(settings.IgnoreNotification ?? [], StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>Checks whether the server permits this complete resource and action event name.</summary>
        public bool IsEnabled(string type)
        {
            if (Volatile.Read(ref _ignored).Contains(type)) {
                return false;
            }
            var disabled = Volatile.Read(ref _disabledSources);
            if (disabled.Contains(type)) {
                return false;
            }
            for (var index = type.LastIndexOf('.'); index > 0; index = type.LastIndexOf('.', index - 1))
                {
                if (disabled.Contains(type[..index])) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Atomically replaces source delivery settings after the surrounding transaction commits.</summary>
        public void ConfigureSources(IEnumerable<Gurux.DLMS.AMI.Shared.DTOs.Notification.GXNotificationSource> sources)
        {
            var disabled = sources.Where(s => s.Enabled != true).Select(s => s.Source).ToHashSet(StringComparer.Ordinal);
            void Apply() => Volatile.Write(ref _disabledSources, disabled);
            if (!Server.Repository.GXHistoryScope.Defer(Apply)) {
                Apply();
            }
        }


        /// <summary>Performs the add operation.</summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="notification">The notification.</param>
        public void Add(string userId, GXTarget notification)
        {
            if (notification.Type == GXAgentPolicies.Update)
                {
                throw new ArgumentException("Agent update commands require an agent recipient.", nameof(notification));
            }
            if (Server.Repository.GXHistoryScope.Defer(() => Add(userId, notification))) {
                return;
            }
            if (!IsEnabled(notification.Type))
            {
                return;
            }
            Server.Internal.GXNotificationText.Fill(notification);
            _items.Enqueue(new CachedNotification
            {
                UserId = userId,
                Notification = notification
            });
        }

        /// <summary>Queues an internal update command exclusively for the selected agent.</summary>
        public void AddAgent(Guid agentId, GXTarget notification)
        {
            if (agentId == Guid.Empty || notification.Type != GXAgentPolicies.Update ||
                notification.Id?.ToString() != agentId.ToString())
                {
                throw new ArgumentException("An agent update command must identify its recipient.", nameof(notification));
            }
            if (Server.Repository.GXHistoryScope.Defer(() => AddAgent(agentId, notification))) {
                return;
            }
            _items.Enqueue(new CachedNotification { AgentId = agentId, Notification = notification });
        }

        /// <summary>Performs the add operation.</summary>
        /// <param name="userIds">The user ids.</param>
        /// <param name="notification">The notification.</param>
        public void Add(IEnumerable<string> userIds, GXTarget notification)
        {
            foreach (string userId in userIds.Distinct())
            {
                Add(userId, notification);
            }
        }

        /// <summary>Performs the add operation.</summary>
        /// <param name="userIds">The user ids.</param>
        /// <param name="notifications">The notifications.</param>
        public void Add(IEnumerable<string> userIds, IEnumerable<GXTarget> notifications)
        {
            foreach (string userId in userIds.Distinct())
            {
                foreach (var notification in notifications)
                {
                    Add(userId, notification);
                }
            }
        }

        /// <summary>Performs the take all operation.</summary>
        public IReadOnlyList<CachedNotification> TakeAll()
        {
            List<CachedNotification> result = [];
            while (_items.TryDequeue(out CachedNotification? item))
            {
                if (item.AgentId != null || IsEnabled(item.Notification.Type))
                {
                    result.Add(item);
                }
            }
            return result;
        }
    }
}
