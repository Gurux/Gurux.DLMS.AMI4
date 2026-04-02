//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
using System.Security.Claims;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Client.Shared;
using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using System.Data;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Client.Shared.Enums;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class NotificationRepository : INotificationRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly INotificationGroupRepository _notificationGroupRepository;
        private readonly GXPerformanceSettings _performanceSettings;

        /// <summary>
        /// Constructor.
        /// </summary>
        public NotificationRepository(IGXHost host,
            IGXAmiContextAccessor contextAccessor,
            IUserRepository userRepository,
            IServiceProvider serviceProvider,
            INotificationGroupRepository notificationGroupRepository,
            IGXEventsNotifier eventsNotifier,
            GXPerformanceSettings performanceSettings)
        {
            var user = contextAccessor?.User;
            if (user == null ||
                (!user.IsInRole(GXRoles.Admin) &&
                !user.IsInRole(GXRoles.User) &&
                !user.IsInRole(GXRoles.Notification) &&
                !user.IsInRole(GXRoles.NotificationManager)))
            {
                throw new UnauthorizedAccessException();
            }
            User = user;
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
            _serviceProvider = serviceProvider;
            _notificationGroupRepository = notificationGroupRepository;
            _performanceSettings = performanceSettings;
        }

        /// <summary>
        /// Returns notificated users.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="performanceSettings">Performance settings tells is server notifying the actions.</param>
        /// <param name="users">Users who can be notified.</param>
        /// <param name="targetType">Target type.</param>
        /// <param name="action">Action.</param>
        /// <returns>List of notified users or null if server doesn't use notifications.</returns>
        internal static List<string>? GetNotifiedUsers(
            GXDbConnection connection,
            GXPerformanceSettings performanceSettings,
            List<string>? users,
            string targetType,
            NotificationAction action)
        {
            if (users != null && users.Any() &&
                performanceSettings.Notification(targetType))
            {
                GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
                args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
                args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
                args.Joins.AddLeftJoin<GXUserGroup, GXUserGroupNotificationGroup>(a => a.Id, b => b.UserGroupId);
                args.Joins.AddLeftJoin<GXUserGroupNotificationGroup, GXNotificationGroup>(a => a.NotificationGroupId, b => b.Id);
                args.Joins.AddLeftJoin<GXNotificationGroup, GXNotificationGroupNotification>(a => a.Id, b => b.NotificationGroupId);
                args.Joins.AddLeftJoin<GXNotificationGroupNotification, GXNotification>(a => a.NotificationId, b => b.Id);
                UInt16 a = (UInt16)action;
                args.Where.And<GXNotification>(w => (w.Action & a) != 0 && w.Targets.Contains(targetType));
                List<GXUser> tmp = connection.Select<GXUser>(args);
                return tmp.Select(s => s.Id).ToList();
            }
            return null;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? notificationId)
        {
            GXSelectArgs args = GXQuery.GetUsersByNotification(s => s.Id,
                ServerHelpers.GetUserId(User), notificationId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? notificationIds)
        {
            GXSelectArgs args = GXQuery.GetUsersByNotifications(s => s.Id,
                ServerHelpers.GetUserId(User), notificationIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(
            IEnumerable<Guid> notifications,
            bool delete)
        {
            if (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.NotificationManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXNotification>(a => a.Id, q => notifications.Contains(q.Id));
            List<GXNotification> list = _host.Connection.Select<GXNotification>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXNotification, List<string>> updates = new();
            foreach (GXNotification it in list)
            {
                it.Removed = now;
                List<string> users = await GetUsersAsync(it.Id);
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXNotification>(it.Id));
                }
                else
                {
                    _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
                }
                updates[it] = users;
            }
            foreach (var it in updates)
            {
                var users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                    it.Value, TargetType.Notification, NotificationAction.Remove);
                if (users == null)
                {
                    break;
                }
                GXNotification tmp = new GXNotification() { Id = it.Key.Id };
                await _eventsNotifier.NotificationDelete(users, [tmp]);
            }
        }

        /// <inheritdoc />
        public async Task<GXNotification[]> ListAsync(
            ListNotifications? request,
            ListNotificationResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the notifications.
                arg = GXSelectArgs.SelectAll<GXNotification>();
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetNotificationsByUser(s => "*", userId);
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXNotification>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXNotification>(w => request.Included.Contains(w.Id));
                }
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXNotification>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXNotification>(q => q.CreationTime);
                arg.Descending = true;
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXNotification>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            if (request?.Select != null && request.Select.Contains(TargetType.User))
            {
                arg.Joins.AddInnerJoin<GXNotification, GXUser>(j => j.Creator, j => j.Id);
                arg.Columns.Add<GXUser>(c => c.Id);
            }
            GXNotification[] notifications = (await _host.Connection.SelectAsync<GXNotification>(arg)).ToArray();
            if (response != null)
            {
                response.Notifications = notifications;
                if (response.Count == 0)
                {
                    response.Count = notifications.Length;
                }
            }
            return notifications;
        }

        /// <inheritdoc />
        public async Task<GXNotification> ReadAsync(Guid id)
        {
            string userId = ServerHelpers.GetUserId(User);
            GXSelectArgs arg;
            if (User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the notifications.
                arg = GXSelectArgs.SelectAll<GXNotification>(w => w.Id == id);
                arg.Joins.AddLeftJoin<GXNotification, GXNotificationGroupNotification>(x => x.Id, y => y.NotificationId);
                arg.Joins.AddLeftJoin<GXNotificationGroupNotification, GXNotificationGroup>(j => j.NotificationGroupId, j => j.Id);
            }
            else
            {
                arg = GXQuery.GetNotificationsByUser(s => "*", userId, id);
            }
            arg.Distinct = true;
            GXNotification notification = await _host.Connection.SingleOrDefaultAsync<GXNotification>(arg);
            if (notification == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            //Get groups.
            arg = GXSelectArgs.Select<GXNotificationGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Joins.AddInnerJoin<GXNotificationGroup, GXNotificationGroupNotification>(y => y.Id, x => x.NotificationId);
            arg.Where.And<GXNotificationGroupNotification>(w => w.NotificationId == notification.Id);
            notification.NotificationGroups = (await _host.Connection.SelectAsync<GXNotificationGroup>(arg)).ToList();
            //Get agents.
            arg = GXSelectArgs.Select<GXAgent>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Joins.AddInnerJoin<GXNotification, GXNotificationAgent>(y => y.Id, x => x.NotificationId);
            arg.Joins.AddInnerJoin<GXNotificationAgent, GXAgent>(y => y.AgentId, x => x.Id);
            arg.Where.And<GXNotification>(w => w.Id == notification.Id);
            notification.Agents = (await _host.Connection.SelectAsync<GXAgent>(arg)).ToList();
            //Get gateways.
            arg = GXSelectArgs.Select<GXGateway>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Joins.AddInnerJoin<GXNotification, GXNotificationGateway>(y => y.Id, x => x.NotificationId);
            arg.Joins.AddInnerJoin<GXNotificationGateway, GXGateway>(y => y.GatewayId, x => x.Id);
            arg.Where.And<GXNotification>(w => w.Id == notification.Id);
            notification.Gateways = (await _host.Connection.SelectAsync<GXGateway>(arg)).ToList();
            //Get device attributes with own query.
            arg = GXSelectArgs.Select<GXAttributeTemplate>(s => new { s.Id, s.Name, s.ObjectTemplate, s.Index }, w => w.Removed == null);
            arg.Joins.AddInnerJoin<GXNotification, GXNotificationDeviceAttributeTemplate>(y => y.Id, x => x.NotificationId);
            arg.Joins.AddInnerJoin<GXNotificationDeviceAttributeTemplate, GXAttributeTemplate>(y => y.AttributeTemplateId, x => x.Id);
            arg.Joins.AddInnerJoin<GXAttributeTemplate, GXObjectTemplate>(y => y.ObjectTemplate, x => x.Id);
            arg.Columns.Add<GXObjectTemplate>(s => new { s.Id, s.Name, s.LogicalName, s.Attributes });
            arg.Columns.Exclude<GXObjectTemplate>(e => e.Attributes);
            arg.Where.And<GXNotification>(w => w.Id == notification.Id);
            notification.DeviceAttributeTemplates = (await _host.Connection.SelectAsync<GXAttributeTemplate>(arg)).ToList();
            //Get devices with own query.
            arg = GXSelectArgs.Select<GXDevice>(s => new { s.Id, s.Name, s.Template }, w => w.Removed == null);
            arg.Columns.Add<GXDeviceTemplate>(s => new { s.Id });
            arg.Joins.AddInnerJoin<GXNotification, GXNotificationDevice>(y => y.Id, x => x.NotificationId);
            arg.Joins.AddInnerJoin<GXNotificationDevice, GXDevice>(y => y.DeviceId, x => x.Id);
            arg.Joins.AddInnerJoin<GXDevice, GXDeviceTemplate>(y => y.Template, x => x.Id);
            arg.Where.And<GXNotification>(w => w.Id == notification.Id);
            notification.Devices = (await _host.Connection.SelectAsync<GXDevice>(arg)).ToList();
            //Get device groups with own query.
            arg = GXSelectArgs.Select<GXDeviceGroup>(s => new { s.Id, s.Name, }, w => w.Removed == null);
            arg.Joins.AddInnerJoin<GXNotification, GXNotificationDeviceGroup>(y => y.Id, x => x.NotificationId);
            arg.Joins.AddInnerJoin<GXNotificationDeviceGroup, GXDeviceGroup>(y => y.DeviceGroupId, x => x.Id);
            arg.Where.And<GXNotification>(w => w.Id == notification.Id);
            notification.DeviceGroups = (await _host.Connection.SelectAsync<GXDeviceGroup>(arg)).ToList();
            //Get device group attributes with own query.
            arg = GXSelectArgs.Select<GXAttributeTemplate>(s => new { s.Id, s.Name, s.ObjectTemplate, s.Index }, w => w.Removed == null);
            arg.Joins.AddInnerJoin<GXNotification, GXNotificationDeviceGroupAttributeTemplate>(y => y.Id, x => x.NotificationId);
            arg.Joins.AddInnerJoin<GXNotificationDeviceGroupAttributeTemplate, GXAttributeTemplate>(y => y.AttributeTemplateId, x => x.Id);
            arg.Joins.AddInnerJoin<GXAttributeTemplate, GXObjectTemplate>(y => y.ObjectTemplate, x => x.Id);
            arg.Columns.Add<GXObjectTemplate>(s => new { s.Id, s.Name, s.LogicalName, s.Attributes });
            arg.Columns.Exclude<GXObjectTemplate>(e => e.Attributes);
            arg.Where.And<GXNotification>(w => w.Id == notification.Id);
            notification.DeviceGroupAttributeTemplates = (await _host.Connection.SelectAsync<GXAttributeTemplate>(arg)).ToList();

            //Get agent groups.
            arg = GXSelectArgs.Select<GXAgentGroup>(s => new { s.Id, s.Name });
            arg.Joins.AddInnerJoin<GXNotification, GXNotificationAgentGroup>(y => y.Id, x => x.NotificationId);
            arg.Joins.AddInnerJoin<GXNotificationAgentGroup, GXAgentGroup>(y => y.AgentGroupId, x => x.Id);
            arg.Where.And<GXNotification>(w => w.Id == notification.Id);
            notification.AgentGroups = (await _host.Connection.SelectAsync<GXAgentGroup>(arg)).ToList();
            //Get gateway groups.
            arg = GXSelectArgs.Select<GXGatewayGroup>(s => new { s.Id, s.Name });
            arg.Joins.AddInnerJoin<GXNotification, GXNotificationGatewayGroup>(y => y.Id, x => x.NotificationId);
            arg.Joins.AddInnerJoin<GXNotificationGatewayGroup, GXGatewayGroup>(y => y.GatewayGroupId, x => x.Id);
            arg.Where.And<GXNotification>(w => w.Id == notification.Id);
            notification.GatewayGroups = (await _host.Connection.SelectAsync<GXGatewayGroup>(arg)).ToList();
            //Get creator with own query. It's faster for some DBs.
            arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName });
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXNotification, GXUser>(s => s.Creator, o => o.Id);
            arg.Where.And<GXNotification>(w => w.Id == id);
            notification.Creator = await _host.Connection.SingleOrDefaultAsync<GXUser>(arg);
            return notification;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            IEnumerable<GXNotification> notifications,
            Expression<Func<GXNotification, object?>>? columns)
        {
            GXUser creator = new GXUser() { Id = ServerHelpers.GetUserId(User) };
            DateTime now = DateTime.Now;
            List<Guid> list = new();
            using IDbTransaction transaction = _host.Connection.BeginTransaction();
            try
            {
                foreach (GXNotification notification in notifications)
                {
                    if (string.IsNullOrEmpty(notification.Name) &&
                        (columns == null || ServerHelpers.Contains(columns, nameof(GXNotification.Name))))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    if (notification.NotificationGroups == null || !notification.NotificationGroups.Any())
                    {
                        ListNotificationGroups request = new ListNotificationGroups()
                        {
                            Filter = new GXNotificationGroup() { Default = true }
                        };
                        notification.NotificationGroups =
                        [
                            .. await _notificationGroupRepository.ListAsync(request, null, CancellationToken.None),
                        ];
                    }
                    if (notification.Id == Guid.Empty)
                    {
                        notification.CreationTime = now;
                        notification.Creator = creator;
                        GXInsertArgs args = GXInsertArgs.Insert(notification);
                        args.Exclude<GXNotification>(q => new
                        {
                            q.NotificationGroups,
                            q.DeviceGroups,
                            q.Devices,
                            q.DeviceAttributeTemplates,
                            q.DeviceGroupAttributeTemplates,
                            q.Agents,
                            q.Gateways,
                            q.Updated,
                            q.Removed
                        });
                        _host.Connection.Insert(args);
                        list.Add(notification.Id);
                        AddNotificationToNotificationGroups(transaction, notification.Id, notification.NotificationGroups);
                        AddDeviceAttributesToNotification(transaction, notification.Id, notification.DeviceAttributeTemplates);
                        AddDevicesToNotification(transaction, notification.Id, notification.Devices);
                        AddDeviceGroupsToNotification(transaction, notification.Id, notification.DeviceGroups);
                        AddDeviceGroupAttributesToNotification(transaction, notification.Id, notification.DeviceGroupAttributeTemplates);
                        AddAgentsToNotification(transaction, notification.Id, notification.Agents);
                        AddGatewaysToNotification(transaction, notification.Id, notification.Gateways);
                    }
                    else
                    {
                        GXSelectArgs m = GXSelectArgs.Select<GXNotification>(q => q.ConcurrencyStamp, where => where.Id == notification.Id);
                        string updated = _host.Connection.SingleOrDefault<string>(m);
                        if (!string.IsNullOrEmpty(updated) && updated != notification.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }
                        notification.ConcurrencyStamp = Guid.NewGuid().ToString();
                        notification.Updated = now;
                        GXUpdateArgs args = GXUpdateArgs.Update(notification, columns);
                        args.Exclude<GXNotification>(q => new
                        {
                            q.CreationTime,
                            q.NotificationGroups,
                            q.DeviceGroups,
                            q.Devices,
                            q.DeviceAttributeTemplates,
                            q.DeviceGroupAttributeTemplates,
                            q.Agents,
                            q.Gateways,
                        });
                        if (!User.IsInRole(GXRoles.Admin) ||
                            notification.Creator == null ||
                            string.IsNullOrEmpty(notification.Creator.Id))
                        {
                            //Only admin can update the creator.
                            args.Exclude<GXNotification>(q => q.Creator);
                        }
                        _host.Connection.Update(args);
                        //Map notification groups to notification.
                        List<GXNotificationGroup> notificationGroups;
                        using (IServiceScope scope = _serviceProvider.CreateScope())
                        {
                            {
                                INotificationGroupRepository notificationGroupRepository = scope.ServiceProvider.GetRequiredService<INotificationGroupRepository>();
                                notificationGroups = await notificationGroupRepository.GetJoinedNotificationGroups(notification.Id);
                                var comparer = new UniqueComparer<GXNotificationGroup, Guid>();
                                List<GXNotificationGroup> removedNotificationGroups = notificationGroups.Except(notification.NotificationGroups, comparer).ToList();
                                List<GXNotificationGroup> addedNotificationGroups = notification.NotificationGroups.Except(notificationGroups, comparer).ToList();
                                if (removedNotificationGroups.Any())
                                {
                                    RemoveNotificationsFromNotificationGroup(transaction,
                                        notification.Id, removedNotificationGroups);
                                }
                                if (addedNotificationGroups.Any())
                                {
                                    AddNotificationToNotificationGroups(transaction,
                                        notification.Id, addedNotificationGroups);
                                }
                            }
                        }
                        //Add agents.
                        if (notification.Agents != null &&
                            (columns == null || ServerHelpers.Contains(columns, nameof(GXNotification.Agents))))
                        {
                            GXSelectArgs arg = GXSelectArgs.SelectAll<GXAgent>(where => where.Removed == null);
                            arg.Joins.AddInnerJoin<GXAgent, GXNotificationAgent>(a => a.Id, b => b.AgentId);
                            arg.Where.And<GXNotificationAgent>(where => where.Removed == null && where.NotificationId == notification.Id);
                            List<GXAgent> agents = (await _host.Connection.SelectAsync<GXAgent>(arg)).ToList();
                            var comparer = new UniqueComparer<GXAgent, Guid>();
                            List<GXAgent> removed = agents.Except(notification.Agents, comparer).ToList();
                            List<GXAgent> added = notification.Agents.Except(agents, comparer).ToList();
                            if (removed.Any())
                            {
                                RemoveAgentsFromNotifications(transaction,
                                    notification.Id, removed);
                            }
                            if (added.Any())
                            {
                                AddAgentsToNotification(transaction,
                                    notification.Id, added);
                            }
                        }
                        //Add gateways.
                        if (notification.Gateways != null &&
                            (columns == null || ServerHelpers.Contains(columns, nameof(GXNotification.Gateways))))
                        {
                            GXSelectArgs arg = GXSelectArgs.SelectAll<GXGateway>(where => where.Removed == null);
                            arg.Joins.AddInnerJoin<GXGateway, GXNotificationGateway>(a => a.Id, b => b.GatewayId);
                            arg.Where.And<GXNotificationGateway>(where => where.Removed == null && where.NotificationId == notification.Id);
                            List<GXGateway> gateways = (await _host.Connection.SelectAsync<GXGateway>(arg)).ToList();
                            var comparer = new UniqueComparer<GXGateway, Guid>();
                            List<GXGateway> removed = gateways.Except(notification.Gateways, comparer).ToList();
                            List<GXGateway> added = notification.Gateways.Except(gateways, comparer).ToList();
                            if (removed.Any())
                            {
                                RemoveGatewaysFromNotifications(transaction,
                                    notification.Id, removed);
                            }
                            if (added.Any())
                            {
                                AddGatewaysToNotification(transaction,
                                    notification.Id, added);
                            }
                        }
                        //Add device template attributes.
                        if (notification.DeviceAttributeTemplates != null &&
                            (columns == null || ServerHelpers.Contains(columns, nameof(GXNotification.DeviceAttributeTemplates))))
                        {
                            GXSelectArgs arg = GXSelectArgs.SelectAll<GXAttributeTemplate>(where => where.Removed == null);
                            arg.Joins.AddInnerJoin<GXAttributeTemplate, GXNotificationDeviceAttributeTemplate>(a => a.Id, b => b.AttributeTemplateId);
                            arg.Where.And<GXNotificationDeviceAttributeTemplate>(where => where.Removed == null && where.NotificationId == notification.Id);
                            List<GXAttributeTemplate> attributes = (await _host.Connection.SelectAsync<GXAttributeTemplate>(arg)).ToList();
                            var comparer = new UniqueComparer<GXAttributeTemplate, Guid>();
                            List<GXAttributeTemplate> removedNotificationAttributes = attributes.Except(notification.DeviceAttributeTemplates, comparer).ToList();
                            List<GXAttributeTemplate> addedNotificationAttributes = notification.DeviceAttributeTemplates.Except(attributes, comparer).ToList();
                            if (removedNotificationAttributes.Any())
                            {
                                RemoveDeviceAttributesFromNotifications(transaction,
                                    notification.Id, removedNotificationAttributes);
                            }
                            if (addedNotificationAttributes.Any())
                            {
                                AddDeviceAttributesToNotification(transaction,
                                    notification.Id, addedNotificationAttributes);
                            }
                        }
                        if (notification.DeviceGroupAttributeTemplates != null &&
                            (columns == null || ServerHelpers.Contains(columns, nameof(GXNotification.DeviceGroupAttributeTemplates))))
                        {
                            GXSelectArgs arg = GXSelectArgs.SelectAll<GXAttributeTemplate>(where => where.Removed == null);
                            arg.Joins.AddInnerJoin<GXAttributeTemplate, GXNotificationDeviceGroupAttributeTemplate>(a => a.Id, b => b.AttributeTemplateId);
                            arg.Where.And<GXNotificationDeviceGroupAttributeTemplate>(where => where.Removed == null && where.NotificationId == notification.Id);
                            List<GXAttributeTemplate> attributes = (await _host.Connection.SelectAsync<GXAttributeTemplate>(arg)).ToList();
                            var comparer = new UniqueComparer<GXAttributeTemplate, Guid>();
                            List<GXAttributeTemplate> removedNotificationAttributes = attributes.Except(notification.DeviceGroupAttributeTemplates, comparer).ToList();
                            List<GXAttributeTemplate> addedNotificationAttributes = notification.DeviceGroupAttributeTemplates.Except(attributes, comparer).ToList();
                            if (removedNotificationAttributes.Any())
                            {
                                RemoveDeviceGroupAttributesFromNotifications(transaction,
                                    notification.Id, removedNotificationAttributes);
                            }
                            if (addedNotificationAttributes.Any())
                            {
                                AddDeviceGroupAttributesToNotification(transaction,
                                    notification.Id, addedNotificationAttributes);
                            }
                        }
                        if (notification.Devices != null &&
                            (columns == null || ServerHelpers.Contains(columns, nameof(GXNotification.Devices))))
                        {
                            GXSelectArgs arg = GXSelectArgs.SelectAll<GXDevice>(where => where.Removed == null);
                            arg.Joins.AddInnerJoin<GXDevice, GXNotificationDevice>(a => a.Id, b => b.DeviceId);
                            arg.Where.And<GXNotificationDevice>(where => where.Removed == null && where.NotificationId == notification.Id);
                            List<GXDevice> devices = (await _host.Connection.SelectAsync<GXDevice>(arg)).ToList();
                            var comparer = new UniqueComparer<GXDevice, Guid>();
                            List<GXDevice> removedNotificationDevices = devices.Except(notification.Devices, comparer).ToList();
                            List<GXDevice> addedNotificationDevices = notification.Devices.Except(devices, comparer).ToList();
                            if (removedNotificationDevices.Any())
                            {
                                RemoveDevicesFromNotifications(transaction,
                                    notification.Id, removedNotificationDevices);
                            }
                            if (addedNotificationDevices.Any())
                            {
                                AddDevicesToNotification(transaction,
                                    notification.Id, addedNotificationDevices);
                            }
                        }
                        if (notification.DeviceGroups != null &&
                            (columns == null || ServerHelpers.Contains(columns, nameof(GXNotification.DeviceGroups))))
                        {
                            GXSelectArgs arg = GXSelectArgs.SelectAll<GXDeviceGroup>(where => where.Removed == null);
                            arg.Joins.AddInnerJoin<GXDeviceGroup, GXNotificationDeviceGroup>(a => a.Id, b => b.DeviceGroupId);
                            arg.Where.And<GXNotificationDeviceGroup>(where => where.Removed == null && where.NotificationId == notification.Id);
                            List<GXDeviceGroup> deviceGroups = (await _host.Connection.SelectAsync<GXDeviceGroup>(arg)).ToList();
                            var comparer = new UniqueComparer<GXDeviceGroup, Guid>();
                            List<GXDeviceGroup> removedNotificationDeviceGroups = deviceGroups.Except(notification.DeviceGroups, comparer).ToList();
                            List<GXDeviceGroup> addedNotificationDeviceGroups = notification.DeviceGroups.Except(deviceGroups, comparer).ToList();
                            if (removedNotificationDeviceGroups.Any())
                            {
                                RemoveDeviceGroupsFromNotifications(transaction,
                                    notification.Id, removedNotificationDeviceGroups);
                            }
                            if (addedNotificationDeviceGroups.Any())
                            {
                                AddDeviceGroupsToNotification(transaction,
                                    notification.Id, addedNotificationDeviceGroups);
                            }
                        }
                    }
                }
                _host.Connection.CommitTransaction(transaction);
            }
            catch (Exception)
            {
                _host.Connection.RollbackTransaction(transaction);
                throw;
            }
            Dictionary<GXNotification, List<string>> updates = new();
            foreach (GXNotification notification in notifications)
            {
                updates[notification] = await GetUsersAsync(notification.Id);
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.NotificationUpdate(it.Value, [it.Key]);
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map notification to notification groups.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="notificationId">Notification ID.</param>
        /// <param name="groups">Group IDs of the notification groups where the notification is added.</param>
        public void AddNotificationToNotificationGroups(
            IDbTransaction transaction,
            Guid notificationId, IEnumerable<GXNotificationGroup>? groups)
        {
            if (groups != null)
            {
                DateTime now = DateTime.Now;
                List<GXNotificationGroupNotification> list = new();
                foreach (GXNotificationGroup it in groups)
                {
                    list.Add(new GXNotificationGroupNotification()
                    {
                        NotificationId = notificationId,
                        NotificationGroupId = it.Id,
                        CreationTime = now
                    });
                }
                _host.Connection.Insert(transaction,
                    GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between notification group and notification.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="notificationId">Notification ID.</param>
        /// <param name="groups">Group IDs of the notification groups where the notification is removed.</param>
        public void RemoveNotificationsFromNotificationGroup(
            IDbTransaction transaction,
            Guid notificationId, IEnumerable<GXNotificationGroup> groups)
        {
            foreach (GXNotificationGroup it in groups)
            {
                _host.Connection.Delete(transaction,
                GXDeleteArgs.Delete<GXNotificationGroupNotification>(w => w.NotificationId == notificationId && w.NotificationGroupId == it.Id));
            }
        }

        /// <summary>
        /// Map device with notification.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="notificationId">Notification ID.</param>
        /// <param name="devices">Joined devices.</param>
        public void AddDevicesToNotification(
            IDbTransaction transaction,
            Guid notificationId, IEnumerable<GXDevice>? devices)
        {
            if (devices != null)
            {
                DateTime now = DateTime.Now;
                List<GXNotificationDevice> list = new();
                foreach (GXDevice it in devices)
                {
                    list.Add(new GXNotificationDevice()
                    {
                        NotificationId = notificationId,
                        DeviceId = it.Id,
                        CreationTime = now
                    });
                }
                _host.Connection.Insert(transaction,
                    GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between device and notification.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="notificationId">Notification ID.</param>
        /// <param name="devices">Removed devices.</param>
        public void RemoveDevicesFromNotifications(
            IDbTransaction transaction,
            Guid notificationId, IEnumerable<GXDevice> devices)
        {
            foreach (GXDevice it in devices)
            {
                _host.Connection.Delete(transaction,
                    GXDeleteArgs.Delete<GXNotificationDevice>(w => w.NotificationId == notificationId && w.DeviceId == it.Id));
            }
        }

        /// <summary>
        /// Map device group with notification.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="notificationId">Notification ID.</param>
        /// <param name="groups">Joined device groups.</param>
        public void AddDeviceGroupsToNotification(IDbTransaction transaction,
            Guid notificationId, IEnumerable<GXDeviceGroup>? groups)
        {
            if (groups != null)
            {
                DateTime now = DateTime.Now;
                List<GXNotificationDeviceGroup> list = new();
                foreach (GXDeviceGroup it in groups)
                {
                    list.Add(new GXNotificationDeviceGroup()
                    {
                        NotificationId = notificationId,
                        DeviceGroupId = it.Id,
                        CreationTime = now
                    });
                }
                _host.Connection.Insert(transaction,
                    GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between device group and notification.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="notificationId">Notification ID.</param>
        /// <param name="groups">Removed device groups.</param>
        public void RemoveDeviceGroupsFromNotifications(IDbTransaction transaction,
            Guid notificationId, IEnumerable<GXDeviceGroup> groups)
        {
            foreach (GXDeviceGroup it in groups)
            {
                _host.Connection.Delete(transaction,
                    GXDeleteArgs.Delete<GXNotificationDeviceGroup>(w => w.NotificationId == notificationId && w.DeviceGroupId == it.Id));
            }
        }

        /// <summary>
        /// Map agents with notification.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="notificationId">Notification ID.</param>
        /// <param name="agents">Joined agents.</param>
        public void AddAgentsToNotification(IDbTransaction transaction,
            Guid notificationId, IEnumerable<GXAgent>? agents)
        {
            if (agents != null)
            {
                DateTime now = DateTime.Now;
                List<GXNotificationAgent> list = new();
                foreach (GXAgent it in agents)
                {
                    list.Add(new GXNotificationAgent()
                    {
                        NotificationId = notificationId,
                        AgentId = it.Id,
                        CreationTime = now
                    });
                }
                _host.Connection.Insert(transaction,
                    GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between agents and notification.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="notificationId">Notification ID.</param>
        /// <param name="agents">Removed agents.</param>
        public void RemoveAgentsFromNotifications(IDbTransaction transaction,
            Guid notificationId, IEnumerable<GXAgent> agents)
        {
            foreach (GXAgent it in agents)
            {
                _host.Connection.Delete(transaction,
                    GXDeleteArgs.Delete<GXNotificationAgent>(w =>
                    w.NotificationId == notificationId &&
                    w.AgentId == it.Id));
            }
        }

        /// <summary>
        /// Map gateways with notification.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="notificationId">Notification ID.</param>
        /// <param name="gateways">Joined gateways.</param>
        public void AddGatewaysToNotification(IDbTransaction transaction,
            Guid notificationId, IEnumerable<GXGateway>? gateways)
        {
            if (gateways != null)
            {
                DateTime now = DateTime.Now;
                List<GXNotificationGateway> list = new();
                foreach (GXGateway it in gateways)
                {
                    list.Add(new GXNotificationGateway()
                    {
                        NotificationId = notificationId,
                        GatewayId = it.Id,
                        CreationTime = now
                    });
                }
                _host.Connection.Insert(transaction,
                    GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between gateways and notification.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="notificationId">Notification ID.</param>
        /// <param name="gateways">Removed gateways.</param>
        public void RemoveGatewaysFromNotifications(IDbTransaction transaction,
            Guid notificationId, IEnumerable<GXGateway> gateways)
        {
            foreach (GXGateway it in gateways)
            {
                _host.Connection.Delete(transaction,
                    GXDeleteArgs.Delete<GXNotificationGateway>(w =>
                    w.NotificationId == notificationId &&
                    w.GatewayId == it.Id));
            }
        }

        /// <summary>
        /// Map device attributes with notification.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="notificationId">Notification ID.</param>
        /// <param name="attributes">Joined attributes.</param>
        public void AddDeviceAttributesToNotification(IDbTransaction transaction,
            Guid notificationId, IEnumerable<GXAttributeTemplate>? attributes)
        {
            if (attributes != null)
            {
                DateTime now = DateTime.Now;
                List<GXNotificationDeviceAttributeTemplate> list = new();
                foreach (GXAttributeTemplate it in attributes)
                {
                    list.Add(new GXNotificationDeviceAttributeTemplate()
                    {
                        NotificationId = notificationId,
                        AttributeTemplateId = it.Id,
                        CreationTime = now
                    });
                }
                _host.Connection.Insert(transaction,
                    GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between device attribute and notification.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="notificationId">Notification ID.</param>
        /// <param name="attributes">Removed attributes.</param>
        public void RemoveDeviceAttributesFromNotifications(IDbTransaction transaction,
            Guid notificationId, IEnumerable<GXAttributeTemplate> attributes)
        {
            foreach (GXAttributeTemplate it in attributes)
            {
                _host.Connection.Delete(transaction,
                    GXDeleteArgs.Delete<GXNotificationDeviceAttributeTemplate>(w =>
                    w.NotificationId == notificationId &&
                    w.AttributeTemplateId == it.Id));
            }
        }

        /// <summary>
        /// Map device group attributes with notification.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="notificationId">Notification ID.</param>
        /// <param name="attributes">Joined attributes.</param>
        public void AddDeviceGroupAttributesToNotification(IDbTransaction transaction,
            Guid notificationId, IEnumerable<GXAttributeTemplate>? attributes)
        {
            if (attributes != null)
            {
                DateTime now = DateTime.Now;
                List<GXNotificationDeviceGroupAttributeTemplate> list = new();
                foreach (GXAttributeTemplate it in attributes)
                {
                    list.Add(new GXNotificationDeviceGroupAttributeTemplate()
                    {
                        NotificationId = notificationId,
                        AttributeTemplateId = it.Id,
                        CreationTime = now
                    });
                }
                _host.Connection.Insert(transaction,
                    GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between device group attribute and notification.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="notificationId">Notification ID.</param>
        /// <param name="attributes">Removed attributes.</param>
        public void RemoveDeviceGroupAttributesFromNotifications(IDbTransaction transaction,
            Guid notificationId, IEnumerable<GXAttributeTemplate> attributes)
        {
            foreach (GXAttributeTemplate it in attributes)
            {
                _host.Connection.Delete(transaction,
                    GXDeleteArgs.Delete<GXNotificationDeviceGroupAttributeTemplate>(w =>
                    w.NotificationId == notificationId &&
                    w.AttributeTemplateId == it.Id));
            }
        }
    }
}
