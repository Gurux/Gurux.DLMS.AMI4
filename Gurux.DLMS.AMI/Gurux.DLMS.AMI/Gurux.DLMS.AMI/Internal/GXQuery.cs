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

using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm;
using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared.DTOs.View;

namespace Gurux.DLMS.AMI.Server.Internal
{
    static class GXQuery
    {
        /// <summary>
        /// Get user groups where user belongs.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="id">User group id.</param>
        /// <param name="removed">Remove time.</param>
        /// <param name="exclude">User group Ids to exclude.</param>
        /// <param name="include">User group Ids to include.</param>
        /// <returns>List of user groups where user belongs.</returns>
        public static GXSelectArgs GetUserGroupsByUser(
            Expression<Func<GXUserGroup, object>> columns,
            string userId,
            Guid? id = null,
            DateTimeOffset? removed = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns);
            if (removed == null)
            {
                args.Where.And<GXUserGroup>(q => q.Removed == null);
            }
            else if (removed != DateTimeOffset.MinValue)
            {
                args.Where.And<GXUserGroup>(q => q.Removed >= removed);
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => q.UserId == userId);
            //Distinct is not needed because values are indexed;
            args.Where.And<GXUserGroupUser>(q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, sub));
            if (id != null && id != Guid.Empty)
            {
                args.Where.And<GXUserGroup>(q => q.Id == id);
            }
            if (exclude?.Any() == true)
            {
                args.Where.And<GXUserGroup>(q => !exclude.Contains(q.Id));
            }
            if (include?.Any() == true)
            {
                args.Where.And<GXUserGroup>(q => include.Contains(q.Id));
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users who belong this user group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupId">User group ID.</param>
        public static GXSelectArgs GetUsersByUserGroup(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One);
            if (groupId != null && groupId != Guid.Empty)
            {
                sub.Where.And<GXUserGroupUser>(q => q.UserGroupId == groupId);
            }
            args.Where.And<GXUserGroupUser>(q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, sub));
            return args;
        }

        /// <summary>
        /// Returns a collection of users who belong user groups.
        /// </summary>
        /// <param name="columns">Selected columns from the user table.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupIds">User group IDs.</param>
        public static GXSelectArgs GetUsersByUserGroups(Expression<Func<GXUser, object>> columns,
            string? userId, IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Id == userId && q.Removed == null);
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One);
            if (groupIds != null && groupIds.Any())
            {
                sub.Where.And<GXUserGroupUser>(q => groupIds.Contains(q.UserGroupId));
            }
            args.Distinct = true;
            args.Where.And<GXUserGroupUser>(q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, sub));
            return args;
        }

        /// <summary>
        /// Get users that belong for the same group(s) with the user.
        /// </summary>
        /// <param name="columns">Selected columns from the user table.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="removed">Remove time.</param>
        /// <param name="exclude">User Ids to exclude.</param>
        /// <param name="include">User Ids to include.</param>
        /// <returns>List of users that user can access.</returns>
        public static GXSelectArgs GetUsersByUser(
            Expression<Func<GXUser, object>> columns,
            string userId,
            DateTimeOffset? removed = null,
            IEnumerable<string>? exclude = null,
            IEnumerable<string>? include = null)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Id == userId);
            if (removed == null)
            {
                args.Where.And<GXUser>(q => q.Removed == null);
            }
            else if (removed != DateTimeOffset.MinValue)
            {
                args.Where.And<GXUser>(q => q.Removed >= removed);
            }
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => s.Id, userId);
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One);
            sub.Where.And<GXUserGroupUser>(q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            args.Where.And<GXUserGroupUser>(q => GXSql.Exists(sub));
            if (exclude?.Any() == true)
            {
                args.Where.And<GXUser>(q => !exclude.Contains(q.Id));
            }
            if (include?.Any() == true)
            {
                args.Where.And<GXUser>(q => include.Contains(q.Id));
            }

            return args;
        }

        /// <summary>
        /// Get users errors by the user.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <returns>User errors by the user.</returns>
        public static GXSelectArgs GetUserErrorsByUser(string userId)
        {
            var types = GXSelectArgs.Select<Gurux.DLMS.AMI.Shared.DTOs.Log.GXLogType>(t => GXSql.One, t => t.NormalizedName == "BUILTIN:USER-ERROR");
            GXSelectArgs args = GXSelectArgs.SelectAll<Gurux.DLMS.AMI.Shared.DTOs.Log.GXLog>(l => GXSql.Exists<Gurux.DLMS.AMI.Shared.DTOs.Log.GXLog, Gurux.DLMS.AMI.Shared.DTOs.Log.GXLogType>(l => l.LogType!, t => t.Id, types));
            args.Distinct = true;
            var owners = GXSelectArgs.Select<GXUser>(u => GXSql.One, u => u.Removed == null && u.Id == userId);
            args.Where.And<Gurux.DLMS.AMI.Shared.DTOs.Log.GXLog>(l => GXSql.Exists<Gurux.DLMS.AMI.Shared.DTOs.Log.GXLog, GXUser>(l => l.User!, u => u.Id!, owners));
            return args;
        }

        /// <summary>
        /// Get users actions by the user.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <returns>User actions by the user.</returns>
        public static GXSelectArgs GetUserActionsByUser(string userId)
        {
            var types = GXSelectArgs.Select<Gurux.DLMS.AMI.Shared.DTOs.Log.GXLogType>(t => GXSql.One, t => t.NormalizedName == "BUILTIN:USER-ACTION");
            GXSelectArgs args = GXSelectArgs.SelectAll<Gurux.DLMS.AMI.Shared.DTOs.Log.GXLog>(l => GXSql.Exists<Gurux.DLMS.AMI.Shared.DTOs.Log.GXLog, Gurux.DLMS.AMI.Shared.DTOs.Log.GXLogType>(l => l.LogType!, t => t.Id, types));
            args.Distinct = true;
            args.Where.And<GXUser>(q => q.Id == userId && q.Removed == null);
            args.Joins.AddInnerJoin<Gurux.DLMS.AMI.Shared.DTOs.Log.GXLog, GXUser>(j => j.User!, j => j.Id!);
            return args;
        }

        /// <summary>
        /// Get device groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">Device group id.</param>
        /// <param name="removed">Remove time.</param>
        /// <param name="exclude">Agent group Ids to exclude.</param>
        /// <param name="include">Agent group Ids to include.</param>
        /// <returns>List of device groups that user can access.</returns>
        public static GXSelectArgs GetDeviceGroupsByUser(
            Expression<Func<GXDeviceGroup, object>> columns,
            string userId,
            Guid? groupId = null,
            DateTimeOffset? removed = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupDeviceGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupDeviceGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns);
            if (removed == null)
            {
                args.Where.And<GXDeviceGroup>(q => q.Removed == null);
            }
            else if (removed != DateTimeOffset.MinValue)
            {
                args.Where.And<GXDeviceGroup>(q => q.Removed >= removed);
            }
            args.Where.And<GXDeviceGroup>(q => GXSql.Exists<GXDeviceGroup, GXUserGroupDeviceGroup>(j => j.Id, j => j.DeviceGroupId, map2));
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXDeviceGroup>(q => q.Id == groupId);
            }
            if (exclude?.Any() == true)
            {
                args.Where.And<GXDeviceGroup>(q => !exclude.Contains(q.Id));
            }
            if (include?.Any() == true)
            {
                args.Where.And<GXDeviceGroup>(q => include.Contains(q.Id));
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the device group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupId">Device group id.</param>
        /// <returns>List of users who can access the device group.</returns>
        public static GXSelectArgs GetUsersByDeviceGroup(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXDeviceGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupId != null && groupId != Guid.Empty)
            {
                ag.Where.And<GXDeviceGroup>(q => q.Id == groupId);
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupDeviceGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupDeviceGroup>(q => GXSql.Exists<GXDeviceGroup, GXUserGroupDeviceGroup>(j => j.Id, j => j.DeviceGroupId, ag));
            args.Where.And<GXUserGroupDeviceGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the device group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupIds">Device group ids.</param>
        /// <returns>List of users who can access the device group.</returns>
        public static GXSelectArgs GetUsersByDeviceGroups(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Distinct = true;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXDeviceGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupIds != null && groupIds.Any())
            {
                ag.Where.And<GXDeviceGroup>(q => groupIds.Contains(q.Id));
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupDeviceGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupDeviceGroup>(q => GXSql.Exists<GXDeviceGroup, GXUserGroupDeviceGroup>(j => j.Id, j => j.DeviceGroupId, ag));
            args.Where.And<GXUserGroupDeviceGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Get devices that user can access.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="id">Device Id.</param>
        /// <returns>List of devices that user can access.</returns>
        /// <param name="args">The args.</param>
        public static void GetDevicesByUser(
            GXSelectArgs args,
            string userId,
            Guid? id = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs groups = GetDeviceGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXDeviceGroupDevice>(s => GXSql.One, q => GXSql.Exists<GXDeviceGroup, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceGroupId, groups));
            if (id != null && id != Guid.Empty)
            {
                args.Where.And<GXDevice>(q => q.Id == id);
            }
            args.Where.And<GXDevice>(q => GXSql.Exists<GXDevice, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceId, map2));
        }

        /// <summary>
        /// Get devices that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="id">Device Id.</param>
        /// <returns>List of devices that user can access.</returns>
        public static GXSelectArgs GetDevicesByUser(
            Expression<Func<GXDevice, object>> columns,
            string userId,
            Guid? id = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs groups = GetDeviceGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXDeviceGroupDevice>(s => GXSql.One, q => GXSql.Exists<GXDeviceGroup, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceGroupId, groups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            if (id != null && id != Guid.Empty)
            {
                args.Where.And<GXDevice>(q => q.Id == id);
            }
            args.Where.And<GXDevice>(q => GXSql.Exists<GXDevice, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceId, map2));
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the device.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="deviceId">Device id.</param>
        /// <returns>List of users who can access the device.</returns>
        public static GXSelectArgs GetUsersByDevice(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid deviceId)
        {
            _ = userId;
            GXSelectArgs deviceGroups = GXSelectArgs.Select<GXDeviceGroup>(s => GXSql.One, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXDeviceGroupDevice>(s => GXSql.One, w => w.DeviceId == deviceId);
            deviceGroups.Where.And<GXDeviceGroupDevice>(q => GXSql.Exists<GXDeviceGroup, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupDeviceGroup>(s => GXSql.One, q => GXSql.Exists<GXDeviceGroup, GXUserGroupDeviceGroup>(j => j.Id, j => j.DeviceGroupId, deviceGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupDeviceGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Returns a collection of users that can access the devices.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="deviceIds">Device ids.</param>
        /// <returns>List of users who can access the device.</returns>
        public static GXSelectArgs GetUsersByDevices(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? deviceIds)
        {
            _ = userId;
            GXSelectArgs deviceGroups = GXSelectArgs.Select<GXDeviceGroup>(s => GXSql.One, w => w.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXDeviceGroupDevice>(s => GXSql.One);
            if (deviceIds != null && deviceIds.Any())
            {
                map1.Where.And<GXDeviceGroupDevice>(w => deviceIds.Contains(w.DeviceId));
            }
            deviceGroups.Where.And<GXDeviceGroupDevice>(q => GXSql.Exists<GXDeviceGroup, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupDeviceGroup>(s => GXSql.One, q => GXSql.Exists<GXDeviceGroup, GXUserGroupDeviceGroup>(j => j.Id, j => j.DeviceGroupId, deviceGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupDeviceGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Returns a collection of device groups where device belongs.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="deviceId">Device Id.</param>
        /// <param name="groupId">Device group id.</param>
        /// <returns>List of device groups where device belongs.</returns>
        public static GXSelectArgs GetDeviceGroupsByDevice(
            Expression<Func<GXDeviceGroup, object>> columns,
            Guid deviceId,
            Guid? groupId = null)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            GXSelectArgs sub = GXSelectArgs.Select<GXDeviceGroupDevice>(s => GXSql.One, q => q.DeviceId == deviceId);
            //Distinct is not needed because values are indexed;
            args.Where.And<GXDeviceGroupDevice>(q => GXSql.Exists<GXDeviceGroup, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceGroupId, sub));
            if (groupId != null)
            {
                args.Where.And<GXDeviceGroup>(q => q.Id == groupId);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of device groups where that are mapped with agent.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="agentId">Agent Id.</param>
        /// <returns>List of device groups where device belongs.</returns>
        public static GXSelectArgs GetDeviceGroupsByAgent(
            Expression<Func<GXDeviceGroup, object>> columns,
            string userId,
            Guid agentId)
        {
            _ = userId;
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs agentGroups = GetAgentGroupsByAgent(s => GXSql.One, agentId);
            GXSelectArgs map = GXSelectArgs.Select<GXAgentGroupDeviceGroup>(s => GXSql.One, q => GXSql.Exists<GXAgentGroup, GXAgentGroupDeviceGroup>(j => j.Id, j => j.AgentGroupId, agentGroups));
            args.Where.And<GXDeviceGroup>(q => GXSql.Exists<GXDeviceGroup, GXAgentGroupDeviceGroup>(j => j.Id, j => j.DeviceGroupId, map));
            args.Where.And<GXDeviceGroup>(q => q.Removed == null);
            return args;
        }


        /// <summary>
        /// Returns a collection of device where that are mapped with agent.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="agentId">Agent Id.</param>
        /// <returns>List of device groups where device belongs.</returns>
        public static GXSelectArgs GetDevicesByAgent(
            Expression<Func<GXDevice, object>> columns,
            string userId,
            Guid agentId)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs deviceGroups = GetDeviceGroupsByAgent(s => GXSql.One, userId, agentId);
            GXSelectArgs map3 = GXSelectArgs.Select<GXDeviceGroupDevice>(s => GXSql.One, q => GXSql.Exists<GXDeviceGroup, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceGroupId, deviceGroups));
            GXSelectArgs devices = GXSelectArgs.Select(columns, q => GXSql.Exists<GXDevice, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceId, map3));
            devices.Where.And<GXDevice>(q => q.Removed == null);
            return devices;
        }

        /// <summary>
        /// Returns a collection of device groups where device belongs.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="deviceIds">Device ID.</param>
        public static GXSelectArgs GetDeviceGroupsByDevices(string userId, IEnumerable<Guid> deviceIds)
        {
            _ = userId;
            GXSelectArgs args = GXSelectArgs.Select<GXDeviceGroup>(s => s.Id, w => w.Removed == null);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXDeviceGroup, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceGroupId);
            args.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDevice>(j => j.DeviceId, j => j.Id);
            args.Where.And<GXDevice>(q => q.Removed == null && deviceIds.Contains(q.Id));
            return args;
        }

        /// <summary>
        /// Returns a collection of users who can access the device.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="deviceId">Device ID.</param>
        public static GXSelectArgs GetUsersByDevice(string userId, Guid? deviceId)
        {
            _ = userId;
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id!);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id!, b => b.UserId!);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupDeviceGroup, GXDeviceGroup>(a => a.DeviceGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXDeviceGroup, GXDeviceGroupDevice>(a => a.Id, b => b.DeviceGroupId);
            args.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDevice>(a => a.DeviceId, b => b.Id);
            args.Where.And<GXDevice>(where => where.Id == deviceId);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXDeviceGroup>(where => where.Removed == null);
            args.Where.And<GXDevice>(where => where.Removed == null);
            return args;
        }

        /// <summary>
        /// Returns a collection of users who can access the device.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="deviceIds">Device IDs.</param>
        public static GXSelectArgs GetUsersByDevices(string userId, IEnumerable<Guid>? deviceIds)
        {
            _ = userId;
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id!);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id!, b => b.UserId!);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupDeviceGroup, GXDeviceGroup>(a => a.DeviceGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXDeviceGroup, GXDeviceGroupDevice>(a => a.Id, b => b.DeviceGroupId);
            args.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDevice>(a => a.DeviceId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXDeviceGroup>(where => where.Removed == null);
            args.Where.And<GXDevice>(where => where.Removed == null);
            if (deviceIds != null)
            {
                args.Where.And<GXDevice>(where => deviceIds.Contains(where.Id));
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the device.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="deviceId">Device id.</param>
        /// <returns>List of users who can access the device.</returns>
        public static GXSelectArgs GetUsersByDevice(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? deviceId)
        {
            _ = userId;
            GXSelectArgs deviceGroups = GXSelectArgs.Select<GXDeviceGroup>(s => GXSql.One, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXDeviceGroupDevice>(s => GXSql.One, w => w.DeviceId == deviceId);
            deviceGroups.Where.And<GXDeviceGroupDevice>(q => GXSql.Exists<GXDeviceGroup, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupDeviceGroup>(s => GXSql.One, q => GXSql.Exists<GXDeviceGroup, GXUserGroupDeviceGroup>(j => j.Id, j => j.DeviceGroupId, deviceGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupDeviceGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Returns a collection of devices that can access the object.
        /// </summary>
        /// <param name="objectId">Object ID.</param>
        public static GXSelectArgs GetDevicesByObject(Guid? objectId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXDevice>(s => s.Id, w => w.Removed == null);
            args.Where.And<GXObject>(w => w.Id == objectId && w.Removed == null);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXDevice, GXObject>(j => j.Id, j => j.Device!);
            return args;
        }

        /// <summary>
        /// Returns a collection of devices that can access the object.
        /// </summary>
        /// <param name="objectId">Object ID.</param>
        public static GXSelectArgs GetDeviceGroupsByObject(Guid? objectId)
        {
            GXSelectArgs args = GetDevicesByObject(objectId);
            args.Where.And<GXDeviceGroup>(w => w.Removed == null);
            args.Joins.AddInnerJoin<GXDevice, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceGroupId);
            args.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDeviceGroup>(j => j.DeviceId, j => j.Id);
            return args;
        }

        /// <summary>
        /// Returns a collection of users who can access the object.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="objectId">Object ID.</param>
        public static GXSelectArgs GetUsersByObjectTemplate(string userId, Guid? objectId)
        {
            _ = userId;
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id!);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id!, b => b.UserId!);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceTemplateGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupDeviceTemplateGroup, GXDeviceTemplateGroup>(a => a.DeviceTemplateGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXDeviceTemplateGroup, GXDeviceTemplateGroupDeviceTemplate>(a => a.Id, b => b.DeviceTemplateId);
            args.Joins.AddInnerJoin<GXDeviceTemplateGroupDeviceTemplate, GXDeviceTemplate>(j => j.DeviceTemplateId, j => j.Id);
            args.Joins.AddInnerJoin<GXDeviceTemplate, GXObjectTemplate>(a => a.Id, b => b.DeviceTemplate!);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (objectId != null && objectId != Guid.Empty)
            {
                args.Where.And<GXObjectTemplate>(q => q.Removed == null && q.Id == objectId);
            }
            else
            {
                args.Where.And<GXObjectTemplate>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users who can access the object templates.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="objectIds">Object IDs.</param>
        public static GXSelectArgs GetUsersByObjectTemplates(string userId, IEnumerable<Guid>? objectIds)
        {
            _ = userId;
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id!);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id!, b => b.UserId!);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceTemplateGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupDeviceTemplateGroup, GXDeviceTemplateGroup>(a => a.DeviceTemplateGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXDeviceTemplateGroup, GXDeviceTemplateGroupDeviceTemplate>(a => a.Id, b => b.DeviceTemplateId);
            args.Joins.AddInnerJoin<GXDeviceTemplateGroupDeviceTemplate, GXDeviceTemplate>(a => a.DeviceTemplateId, b => b.Id);
            args.Joins.AddInnerJoin<GXDeviceTemplate, GXObjectTemplate>(a => a.Id, b => b.DeviceTemplate!);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            args.Where.And<GXDeviceTemplateGroup>(q => q.Removed == null);
            args.Where.And<GXDeviceTemplate>(q => q.Removed == null);
            if (objectIds != null && objectIds.Any())
            {
                args.Where.And<GXObjectTemplate>(q => q.Removed == null && objectIds.Contains(q.Id));
            }
            else
            {
                args.Where.And<GXObjectTemplate>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users who can access the object.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="objectId">Object ID.</param>
        public static GXSelectArgs GetUsersByObject(string userId, Guid? objectId)
        {
            _ = userId;
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id!);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id!, b => b.UserId!);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupDeviceGroup, GXDeviceGroup>(a => a.DeviceGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXDeviceGroup, GXDeviceGroupDevice>(a => a.Id, b => b.DeviceId);
            args.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDevice>(j => j.DeviceId, j => j.Id);
            args.Joins.AddInnerJoin<GXDevice, GXObject>(a => a.Id, b => b.Device!);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (objectId != null && objectId != Guid.Empty)
            {
                args.Where.And<GXObject>(q => q.Removed == null && q.Id == objectId);
            }
            else
            {
                args.Where.And<GXObject>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users who can access the objects.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="objectIds">Object IDs.</param>
        public static GXSelectArgs GetUsersByObjects(string userId, IEnumerable<Guid>? objectIds)
        {
            _ = userId;
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id!);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id!, b => b.UserId!);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupDeviceGroup, GXDeviceGroup>(a => a.DeviceGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXDeviceGroup, GXDeviceGroupDevice>(a => a.Id, b => b.DeviceGroupId);
            args.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDevice>(a => a.DeviceId, b => b.Id);
            args.Joins.AddInnerJoin<GXDevice, GXObject>(a => a.Id, b => b.Device!);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            args.Where.And<GXDeviceGroup>(q => q.Removed == null);
            args.Where.And<GXDevice>(q => q.Removed == null);
            if (objectIds != null && objectIds.Any())
            {
                args.Where.And<GXObject>(q => q.Removed == null && objectIds.Contains(q.Id));
            }
            else
            {
                args.Where.And<GXObject>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users who can access the attribute templates.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="attributeIds">Attribute IDs.</param>
        public static GXSelectArgs GetUsersByAttributeTemplates(string userId, IEnumerable<Guid>? attributeIds)
        {
            _ = userId;
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id!);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id!, b => b.UserId!);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceTemplateGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupDeviceTemplateGroup, GXDeviceTemplateGroup>(a => a.DeviceTemplateGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXDeviceTemplateGroup, GXDeviceTemplateGroupDeviceTemplate>(a => a.Id, b => b.DeviceTemplateGroupId);
            args.Joins.AddInnerJoin<GXDeviceTemplateGroupDeviceTemplate, GXDeviceTemplate>(a => a.DeviceTemplateId, b => b.Id);
            args.Joins.AddInnerJoin<GXDeviceTemplate, GXObjectTemplate>(a => a.Id, b => b.DeviceTemplate!);
            args.Joins.AddInnerJoin<GXObjectTemplate, GXAttributeTemplate>(a => a.Id, b => b.ObjectTemplate!);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXDeviceTemplateGroup>(where => where.Removed == null);
            args.Where.And<GXDeviceTemplate>(where => where.Removed == null);
            if (attributeIds != null && attributeIds.Any())
            {
                args.Where.And<GXAttributeTemplate>(q => q.Removed == null && attributeIds.Contains(q.Id));
            }
            else
            {
                args.Where.And<GXAttributeTemplate>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users who can access the attribute templates.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="id">Attribute ID.</param>
        public static GXSelectArgs GetUsersByAttributeTemplate(string userId, Guid? id)
        {
            _ = userId;
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id!);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id!, b => b.UserId!);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceTemplateGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupDeviceTemplateGroup, GXDeviceTemplateGroup>(a => a.DeviceTemplateGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXDeviceTemplateGroup, GXDeviceTemplateGroupDeviceTemplate>(a => a.Id, b => b.DeviceTemplateGroupId);
            args.Joins.AddInnerJoin<GXDeviceTemplateGroupDeviceTemplate, GXDeviceTemplate>(a => a.DeviceTemplateId, b => b.Id);
            args.Joins.AddInnerJoin<GXDeviceTemplate, GXObjectTemplate>(a => a.Id, b => b.DeviceTemplate!);
            args.Joins.AddInnerJoin<GXObjectTemplate, GXAttributeTemplate>(a => a.Id, b => b.ObjectTemplate!);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXDeviceTemplateGroup>(where => where.Removed == null);
            args.Where.And<GXDeviceTemplate>(where => where.Removed == null);
            if (id != null && id != Guid.Empty)
            {
                args.Where.And<GXAttributeTemplate>(q => q.Removed == null && q.Id == id);
            }
            else
            {
                args.Where.And<GXAttributeTemplate>(q => q.Removed == null);
            }
            return args;
        }


        /// <summary>
        /// Returns a collection of users who can access the attributes.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="attributeIds">Attribute IDs.</param>
        public static GXSelectArgs GetUsersByAttributes(string userId, IEnumerable<Guid>? attributeIds)
        {
            _ = userId;
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id!);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id!, b => b.UserId!);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupDeviceGroup, GXDeviceGroup>(a => a.DeviceGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXDeviceGroup, GXDeviceGroupDevice>(a => a.Id, b => b.DeviceGroupId);
            args.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDevice>(a => a.DeviceId, b => b.Id);
            args.Joins.AddInnerJoin<GXDevice, GXObject>(a => a.Id, b => b.Device!);
            args.Joins.AddInnerJoin<GXObject, GXAttribute>(a => a.Id, b => b.Object!);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXDeviceGroup>(where => where.Removed == null);
            args.Where.And<GXDevice>(where => where.Removed == null);
            if (attributeIds != null && attributeIds.Any())
            {
                args.Where.And<GXAttribute>(q => q.Removed == null && attributeIds.Contains(q.Id));
            }
            else
            {
                args.Where.And<GXAttribute>(q => q.Removed == null);
            }
            return args;
        }



        /// <summary>
        /// Returns a collection of devices that can access the attribute.
        /// </summary>
        /// <param name="attributeId">Attribute ID.</param>
        public static GXSelectArgs GetDevicesByAttribute(Guid? attributeId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXDevice>(s => s.Id, w => w.Removed == null);
            args.Where.And<GXAttribute>(w => w.Id == attributeId && w.Removed == null);
            args.Where.And<GXObject>(w => w.Removed == null);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXAttribute, GXObject>(j => j.Object!, j => j.Id);
            args.Joins.AddInnerJoin<GXObject, GXDevice>(j => j.Device!, j => j.Id);
            return args;
        }

        /// <summary>
        /// Returns a collection of devices that can access the attribute.
        /// </summary>
        /// <param name="attributeId">Attribute ID.</param>
        public static GXSelectArgs GetDeviceGroupsByAttribute(Guid? attributeId)
        {
            GXSelectArgs args = GetDevicesByAttribute(attributeId);
            args.Columns.Clear();
            args.Columns.Add<GXDeviceGroup>(s => s.Id);
            args.Where.And<GXDeviceGroup>(w => w.Removed == null);
            args.Joins.AddInnerJoin<GXDevice, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceId);
            args.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDeviceGroup>(j => j.DeviceGroupId, j => j.Id);
            return args;
        }

        /// <summary>
        /// Returns a collection of users who can access the attribute.
        /// </summary>
        /// <param name="attributeId">Attribute ID.</param>
        public static GXSelectArgs GetUsersByAttribute(Guid? attributeId)
        {
            GXSelectArgs deviceGrops = GetDeviceGroupsByAttribute(attributeId);
            GXSelectArgs args = GXSelectArgs.Select<GXUserGroupDeviceGroup>(s => s.UserGroupId, q => q.Removed == null);
            args.Distinct = true;
            args.Columns.Clear();
            args.Columns.Add<GXUser>(s => s.Id!);
            args.Joins.AddInnerJoin<GXUserGroupDeviceGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(j => j.UserId!, j => j.Id!);
            args.Where.And<GXUserGroupDeviceGroup>(q => GXSql.Exists<GXUserGroupDeviceGroup, GXDeviceGroup>(j => j.DeviceGroupId, j => j.Id, deviceGrops));
            args.Where.And<GXUserGroupDeviceGroup>(q => q.Removed == null);
            args.Where.And<GXUser>(q => q.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Get device errors that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param> 
        /// <param name="userId">User Id.</param>
        /// <param name="deviceId">Device Id.</param>
        /// <returns>List of device errors that user can access.</returns>
        public static GXSelectArgs GetDeviceErrorsByUser(
            Expression<Func<Gurux.DLMS.AMI.Shared.DTOs.Log.GXLog, object>> columns,
            string userId,
            Guid? deviceId = null)
        {
            var devices = GetDevicesByUser(d => GXSql.One, userId, deviceId);
            var types = GXSelectArgs.Select<Gurux.DLMS.AMI.Shared.DTOs.Log.GXLogType>(t => GXSql.One,
                t => t.NormalizedName == "BUILTIN:DEVICE-ERROR");
            var args = GXSelectArgs.Select(columns);
            args.Distinct = true;
            args.Where.And<Gurux.DLMS.AMI.Shared.DTOs.Log.GXLog>(l => GXSql.Exists<Gurux.DLMS.AMI.Shared.DTOs.Log.GXLog, Gurux.DLMS.AMI.Shared.DTOs.Log.GXLogType>(l => l.LogType!, t => t.Id, types));
            args.Where.And<Gurux.DLMS.AMI.Shared.DTOs.Log.GXLog>(l => GXSql.Exists<Gurux.DLMS.AMI.Shared.DTOs.Log.GXLog, GXDevice>(l => l.Creator!, d => d.Id, devices));
            return args;
        }
        /// <summary>
        /// Get devices traces that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param> 
        /// <param name="userId">User Id.</param>
        /// <param name="deviceId">Device Id.</param>
        /// <returns>List of devices that user can access.</returns>
        public static GXSelectArgs GetDeviceTracesByUser(
            Expression<Func<GXDeviceTrace, object>> columns,
            string userId,
            Guid? deviceId = null)
        {
            GXSelectArgs devs = GetDevicesByUser(s => s.Id, userId, deviceId);
            GXSelectArgs args = GXSelectArgs.Select(columns);
            args.Joins.AddInnerJoin<GXDeviceTrace, GXDevice>(j => j.Device!, j => j.Id);
            args.Where.And<GXDeviceTrace>(q => GXSql.Exists<GXDeviceTrace, GXDevice>(j => j.Device!, j => j.Id, devs));
            return args;
        }

        /// <summary>
        /// Get devices actions that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param> 
        /// <param name="userId">User Id.</param>
        /// <param name="deviceId">Device Id.</param>
        /// <returns>List of devices that user can access.</returns>
        public static GXSelectArgs GetDeviceActionsByUser(
            Expression<Func<Gurux.DLMS.AMI.Shared.DTOs.Log.GXLog, object>> columns,
            string userId,
            Guid? deviceId = null)
        {
            var devices = GetDevicesByUser(d => GXSql.One, userId, deviceId);
            var types = GXSelectArgs.Select<Gurux.DLMS.AMI.Shared.DTOs.Log.GXLogType>(t => GXSql.One,
                t => t.NormalizedName == "BUILTIN:DEVICE-ACTION");
            var args = GXSelectArgs.Select(columns);
            args.Distinct = true;
            args.Where.And<Gurux.DLMS.AMI.Shared.DTOs.Log.GXLog>(l => GXSql.Exists<Gurux.DLMS.AMI.Shared.DTOs.Log.GXLog, Gurux.DLMS.AMI.Shared.DTOs.Log.GXLogType>(l => l.LogType!, t => t.Id, types));
            args.Where.And<Gurux.DLMS.AMI.Shared.DTOs.Log.GXLog>(l => GXSql.Exists<Gurux.DLMS.AMI.Shared.DTOs.Log.GXLog, GXDevice>(l => l.Creator!, d => d.Id, devices));
            return args;
        }
        /// <summary>
        /// Get objects that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param> 
        /// <param name="userId">User Id.</param>
        /// <param name="objectId">Object Id.</param>
        /// <returns>List of objects that user can access.</returns>
        public static GXSelectArgs GetObjectsByUser(
            Expression<Func<GXObject, object>> columns,
            string userId, Guid? objectId = null)
        {
            GXSelectArgs devs = GetDevicesByUser(s => s.Id, userId);
            GXSelectArgs args = GXSelectArgs.Select(columns);
            args.Joins.AddInnerJoin<GXObject, GXDevice>(j => j.Device!, j => j.Id);
            if (objectId != null)
            {
                args.Where.And<GXObject>(q => q.Id == objectId);
            }
            args.Where.And<GXObject>(q => q.Removed == null);
            args.Where.And<GXObject>(q => GXSql.Exists<GXObject, GXDevice>(j => j.Device!, j => j.Id, devs));
            return args;
            /*
            GXSelectArgs args = GetDevicesByUser(s => s.Id, userId);
            args.Columns.Clear();
            args.Columns.Add<GXObject>();
            args.Joins.AddLeftJoin<GXDevice, GXObject>(j => j.Id, j => j.Device);
            if (objectId != null)
            {
                args.Where.And<GXObject>(q => q.Id == objectId);
            }
            args.Where.And<GXObject>(q => q.Removed == null);
            return args;
            */
        }

        /// <summary>
        /// Get object templates that user can access.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="objectId">Object template Id.</param>
        /// <returns>List of object templates that user can access.</returns>
        public static GXSelectArgs GetObjectTemplatesByUser(string userId,
            Guid? objectId = null)
        {
            GXSelectArgs args = GetDeviceTemplatesByUser(s => s.Id, userId);
            args.Columns.Clear();
            args.Columns.Add<GXObjectTemplate>();
            args.Joins.AddInnerJoin<GXDeviceTemplate, GXObjectTemplate>(j => j.Id, j => j.DeviceTemplate!);
            if (objectId != null)
            {
                args.Where.And<GXObjectTemplate>(q => q.Id == objectId);
            }
            args.Where.And<GXObjectTemplate>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Get object templates that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param> 
        /// <param name="userId">User Id.</param>
        /// <param name="objectId">Object template Id.</param>
        /// <returns>List of object templates that user can access.</returns>
        public static GXSelectArgs GetAttributeTemplatesByUser(
            Expression<Func<GXAttributeTemplate, object>> columns,
            string userId,
            Guid? objectId = null)
        {
            _ = columns;
            GXSelectArgs args = GetDeviceTemplatesByUser(s => s.Id, userId);
            args.Columns.Clear();
            args.Columns.Add<GXAttributeTemplate>();
            args.Joins.AddInnerJoin<GXDeviceTemplate, GXObjectTemplate>(j => j.Id, j => j.DeviceTemplate!);
            args.Joins.AddInnerJoin<GXObjectTemplate, GXAttributeTemplate>(j => j.Id, j => j.ObjectTemplate!);
            if (objectId != null)
            {
                args.Where.And<GXAttributeTemplate>(q => q.Id == objectId);
            }
            args.Where.And<GXObjectTemplate>(q => q.Removed == null);
            args.Where.And<GXAttributeTemplate>(q => q.Removed == null);
            return args;
        }


        /// <summary>
        /// Get attributes that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param> 
        /// <param name="userId">User Id.</param>
        /// <param name="attributeId ">Attribute Id.</param>
        /// <returns>List of attributes that user can access.</returns>
        public static GXSelectArgs GetAttributesByUser(
            Expression<Func<GXAttribute, object>> columns,
            string userId,
            Guid? attributeId = null)
        {
            GXSelectArgs devs = GetDevicesByUser(s => s.Id, userId);
            GXSelectArgs args = GXSelectArgs.Select(columns);
            args.Joins.AddInnerJoin<GXDevice, GXObject>(j => j.Id, j => j.Device!);
            args.Joins.AddInnerJoin<GXObject, GXAttribute>(j => j.Id, j => j.Object!);
            if (attributeId != null)
            {
                args.Where.And<GXAttribute>(q => q.Id == attributeId);
            }
            args.Where.And<GXAttribute>(q => q.Removed == null);
            args.Where.And<GXAttribute>(q => GXSql.Exists<GXObject, GXDevice>(j => j.Device!, j => j.Id, devs));
            return args;
        }

        /// <summary>
        /// Get trigger groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">Trigger group id.</param>
        /// <param name="removed">Remove time.</param>
        /// <param name="exclude">Trigger group Ids to exclude.</param>
        /// <param name="include">Trigger group Ids to include.</param>
        /// <returns>List of Trigger groups that user can access.</returns>
        public static GXSelectArgs GetTriggerGroupsByUser(
            Expression<Func<GXTriggerGroup, object>> columns,
            string userId,
            Guid? groupId = null,
            DateTimeOffset? removed = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupTriggerGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupTriggerGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns);
            if (removed == null)
            {
                args.Where.And<GXTriggerGroup>(q => q.Removed == null);
            }
            else if (removed != DateTimeOffset.MinValue)
            {
                args.Where.And<GXTriggerGroup>(q => q.Removed >= removed);
            }
            args.Where.And<GXTriggerGroup>(q => GXSql.Exists<GXTriggerGroup, GXUserGroupTriggerGroup>(j => j.Id, j => j.TriggerGroupId, map2));
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXTriggerGroup>(q => q.Id == groupId);
            }
            if (exclude?.Any() == true)
            {
                args.Where.And<GXTriggerGroup>(q => !exclude.Contains(q.Id));
            }
            if (include?.Any() == true)
            {
                args.Where.And<GXTriggerGroup>(q => include.Contains(q.Id));
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the trigger group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupId">Trigger group id.</param>
        /// <returns>List of users who can access the trigger group.</returns>
        public static GXSelectArgs GetUsersByTriggerGroup(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXTriggerGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupId != null && groupId != Guid.Empty)
            {
                ag.Where.And<GXTriggerGroup>(q => q.Id == groupId);
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupTriggerGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupTriggerGroup>(q => GXSql.Exists<GXTriggerGroup, GXUserGroupTriggerGroup>(j => j.Id, j => j.TriggerGroupId, ag));
            args.Where.And<GXUserGroupTriggerGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the trigger group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupIds">Trigger group ids.</param>
        /// <returns>List of users who can access the trigger group.</returns>
        public static GXSelectArgs GetUsersByTriggerGroups(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Distinct = true;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXTriggerGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupIds != null && groupIds.Any())
            {
                ag.Where.And<GXTriggerGroup>(q => groupIds.Contains(q.Id));
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupTriggerGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupTriggerGroup>(q => GXSql.Exists<GXTriggerGroup, GXUserGroupTriggerGroup>(j => j.Id, j => j.TriggerGroupId, ag));
            args.Where.And<GXUserGroupTriggerGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Get triggers that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="triggerId">Trigger Id.</param>
        /// <returns>List of triggers that user can access.</returns>
        public static GXSelectArgs GetTriggersByUser(
            Expression<Func<GXTrigger, object>> columns,
            string userId,
            Guid? triggerId = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs groups = GetTriggerGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXTriggerGroupTrigger>(s => GXSql.One, q => GXSql.Exists<GXTriggerGroup, GXTriggerGroupTrigger>(j => j.Id, j => j.TriggerGroupId, groups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXTrigger>(q => GXSql.Exists<GXTrigger, GXTriggerGroupTrigger>(j => j.Id, j => j.TriggerId, map2));
            if (triggerId != null && triggerId != Guid.Empty)
            {
                args.Where.And<GXTrigger>(q => q.Id == triggerId);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the trigger.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="id">Trigger id.</param>
        /// <returns>List of users who can access the trigger.</returns>
        public static GXSelectArgs GetUsersByTrigger(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? id)
        {
            _ = userId;
            GXSelectArgs triggerGroups = GXSelectArgs.Select<GXTriggerGroup>(s => GXSql.One, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXTriggerGroupTrigger>(s => GXSql.One);
            if (id != null && id != Guid.Empty)
            {
                map1.Where.And<GXTriggerGroupTrigger>(q => q.TriggerId == id);
            }
            triggerGroups.Where.And<GXTriggerGroupTrigger>(q => GXSql.Exists<GXTriggerGroup, GXTriggerGroupTrigger>(j => j.Id, j => j.TriggerGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupTriggerGroup>(s => GXSql.One, q => GXSql.Exists<GXTriggerGroup, GXUserGroupTriggerGroup>(j => j.Id, j => j.TriggerGroupId, triggerGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupTriggerGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Returns a collection of users that can access the triggers.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="Ids">Trigger ids.</param>
        /// <returns>List of users who can access the trigger.</returns>
        public static GXSelectArgs GetUsersByTriggers(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? Ids)
        {
            _ = userId;
            GXSelectArgs triggerGroups = GXSelectArgs.Select<GXTriggerGroup>(s => GXSql.One, w => w.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXTriggerGroupTrigger>(s => GXSql.One);
            if (Ids != null && Ids.Any())
            {
                map1.Where.And<GXTriggerGroupTrigger>(w => Ids.Contains(w.TriggerId));
            }
            triggerGroups.Where.And<GXTriggerGroupTrigger>(q => GXSql.Exists<GXTriggerGroup, GXTriggerGroupTrigger>(j => j.Id, j => j.TriggerGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupTriggerGroup>(s => GXSql.One, q => GXSql.Exists<GXTriggerGroup, GXUserGroupTriggerGroup>(j => j.Id, j => j.TriggerGroupId, triggerGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupTriggerGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Get workflow groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">Workflow group id.</param>
        /// <param name="removed">Remove time.</param>
        /// <param name="exclude">Workflow group Ids to exclude.</param>
        /// <param name="include">Workflow group Ids to include.</param>
        /// <returns>List of Workflow groups that user can access.</returns>
        public static GXSelectArgs GetWorkflowGroupsByUser(
            Expression<Func<GXWorkflowGroup, object>> columns,
            string userId,
            Guid? groupId = null,
            DateTimeOffset? removed = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupWorkflowGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupWorkflowGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns);
            if (removed == null)
            {
                args.Where.And<GXWorkflowGroup>(q => q.Removed == null);
            }
            else if (removed != DateTimeOffset.MinValue)
            {
                args.Where.And<GXWorkflowGroup>(q => q.Removed >= removed);
            }
            args.Where.And<GXWorkflowGroup>(q => GXSql.Exists<GXWorkflowGroup, GXUserGroupWorkflowGroup>(j => j.Id, j => j.WorkflowGroupId, map2));
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXWorkflowGroup>(q => q.Id == groupId);
            }
            if (exclude?.Any() == true)
            {
                args.Where.And<GXWorkflowGroup>(q => !exclude.Contains(q.Id));
            }
            if (include?.Any() == true)
            {
                args.Where.And<GXWorkflowGroup>(q => include.Contains(q.Id));
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the workflow group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupId">Workflow group id.</param>
        /// <returns>List of users who can access the workflow group.</returns>
        public static GXSelectArgs GetUsersByWorkflowGroup(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXWorkflowGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupId != null && groupId != Guid.Empty)
            {
                ag.Where.And<GXWorkflowGroup>(q => q.Id == groupId);
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupWorkflowGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupWorkflowGroup>(q => GXSql.Exists<GXWorkflowGroup, GXUserGroupWorkflowGroup>(j => j.Id, j => j.WorkflowGroupId, ag));
            args.Where.And<GXUserGroupWorkflowGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the workflow group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupIds">Workflow group ids.</param>
        /// <returns>List of users who can access the workflow group.</returns>
        public static GXSelectArgs GetUsersByWorkflowGroups(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Distinct = true;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXWorkflowGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupIds != null && groupIds.Any())
            {
                ag.Where.And<GXWorkflowGroup>(q => groupIds.Contains(q.Id));
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupWorkflowGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupWorkflowGroup>(q => GXSql.Exists<GXWorkflowGroup, GXUserGroupWorkflowGroup>(j => j.Id, j => j.WorkflowGroupId, ag));
            args.Where.And<GXUserGroupWorkflowGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Get workflows that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="workflowId">Workflow Id.</param>
        /// <returns>List of workflows that user can access.</returns>
        public static GXSelectArgs GetWorkflowsByUser(
            Expression<Func<GXWorkflow, object>> columns,
            string userId,
            Guid? workflowId = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs groups = GetWorkflowGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXWorkflowGroupWorkflow>(s => GXSql.One, q => GXSql.Exists<GXWorkflowGroup, GXWorkflowGroupWorkflow>(j => j.Id, j => j.WorkflowGroupId, groups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXWorkflow>(q => GXSql.Exists<GXWorkflow, GXWorkflowGroupWorkflow>(j => j.Id, j => j.WorkflowId, map2));
            if (workflowId != null && workflowId != Guid.Empty)
            {
                args.Where.And<GXWorkflow>(q => q.Id == workflowId);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the workflow.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="id">Workflow id.</param>
        /// <returns>List of users who can access the workflow.</returns>
        public static GXSelectArgs GetUsersByWorkflow(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? id)
        {
            _ = userId;
            GXSelectArgs workflowGroups = GXSelectArgs.Select<GXWorkflowGroup>(s => GXSql.One, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXWorkflowGroupWorkflow>(s => GXSql.One);
            if (id != null && id != Guid.Empty)
            {
                map1.Where.And<GXWorkflowGroupWorkflow>(q => q.WorkflowId == id);
            }
            workflowGroups.Where.And<GXWorkflowGroupWorkflow>(q => GXSql.Exists<GXWorkflowGroup, GXWorkflowGroupWorkflow>(j => j.Id, j => j.WorkflowGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupWorkflowGroup>(s => GXSql.One, q => GXSql.Exists<GXWorkflowGroup, GXUserGroupWorkflowGroup>(j => j.Id, j => j.WorkflowGroupId, workflowGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupWorkflowGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Returns a collection of users that can access the workflows.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="Ids">Workflow ids.</param>
        /// <returns>List of users who can access the workflow.</returns>
        public static GXSelectArgs GetUsersByWorkflows(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? Ids)
        {
            _ = userId;
            GXSelectArgs workflowGroups = GXSelectArgs.Select<GXWorkflowGroup>(s => GXSql.One, w => w.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXWorkflowGroupWorkflow>(s => GXSql.One);
            if (Ids != null && Ids.Any())
            {
                map1.Where.And<GXWorkflowGroupWorkflow>(w => Ids.Contains(w.WorkflowId));
            }
            workflowGroups.Where.And<GXWorkflowGroupWorkflow>(q => GXSql.Exists<GXWorkflowGroup, GXWorkflowGroupWorkflow>(j => j.Id, j => j.WorkflowGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupWorkflowGroup>(s => GXSql.One, q => GXSql.Exists<GXWorkflowGroup, GXUserGroupWorkflowGroup>(j => j.Id, j => j.WorkflowGroupId, workflowGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupWorkflowGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Get agent groups where agent belongs.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="agentId">AgentId.</param>
        /// <param name="groupId">Agent group id.</param>
        /// <returns>List of agent groups where agent belongs.</returns>
        public static GXSelectArgs GetAgentGroupsByAgent(
            Expression<Func<GXAgentGroup, object>> columns,
            Guid agentId,
            Guid? groupId = null)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            GXSelectArgs sub = GXSelectArgs.Select<GXAgentGroupAgent>(s => GXSql.One, q => q.AgentId == agentId);
            //Distinct is not needed because values are indexed;
            args.Where.And<GXAgentGroupAgent>(q => GXSql.Exists<GXAgentGroup, GXAgentGroupAgent>(j => j.Id, j => j.AgentGroupId, sub));
            if (groupId != null)
            {
                args.Where.And<GXAgentGroup>(q => q.Id == groupId);
            }
            return args;
        }

        /// <summary>
        /// Get agent groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">Agent group id.</param>
        /// <param name="removed">Remove time.</param>
        /// <param name="exclude">Agent group Ids to exclude.</param>
        /// <param name="include">Agent group Ids to include.</param>
        /// <returns>List of agent groups that user can access.</returns>
        public static GXSelectArgs GetAgentGroupsByUser(
            Expression<Func<GXAgentGroup, object>> columns,
            string userId,
            Guid? groupId = null,
            DateTimeOffset? removed = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupAgentGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupAgentGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns);
            if (removed == null)
            {
                args.Where.And<GXAgentGroup>(q => q.Removed == null);
            }
            else if (removed != DateTimeOffset.MinValue)
            {
                args.Where.And<GXAgentGroup>(q => q.Removed >= removed);
            }
            args.Where.And<GXAgentGroup>(q => GXSql.Exists<GXAgentGroup, GXUserGroupAgentGroup>(j => j.Id, j => j.AgentGroupId, map2));
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXAgentGroup>(q => q.Id == groupId);
            }
            if (exclude?.Any() == true)
            {
                args.Where.And<GXAgentGroup>(q => !exclude.Contains(q.Id));
            }
            if (include?.Any() == true)
            {
                args.Where.And<GXAgentGroup>(q => include.Contains(q.Id));
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the agent group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupId">Agent group id.</param>
        /// <returns>List of users who can access the agent group.</returns>
        public static GXSelectArgs GetUsersByAgentGroup(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXAgentGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupId != null && groupId != Guid.Empty)
            {
                ag.Where.And<GXAgentGroup>(q => q.Id == groupId);
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupAgentGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupAgentGroup>(q => GXSql.Exists<GXAgentGroup, GXUserGroupAgentGroup>(j => j.Id, j => j.AgentGroupId, ag));
            args.Where.And<GXUserGroupAgentGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the agent group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupIds">Agent group ids.</param>
        /// <returns>List of users who can access the agent group.</returns>
        public static GXSelectArgs GetUsersByAgentGroups(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Distinct = true;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXAgentGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupIds != null && groupIds.Any())
            {
                ag.Where.And<GXAgentGroup>(q => groupIds.Contains(q.Id));
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupAgentGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupAgentGroup>(q => GXSql.Exists<GXAgentGroup, GXUserGroupAgentGroup>(j => j.Id, j => j.AgentGroupId, ag));
            args.Where.And<GXUserGroupAgentGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Get agents that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="agentId">Agent Id.</param>
        /// <param name="removed">Remove time.</param>
        /// <param name="exclude">Module group Ids to exclude.</param>
        /// <param name="include">Module group Ids to include.</param>
        /// <returns>List of agents that user can access.</returns>
        public static GXSelectArgs GetAgentsByUser(
            Expression<Func<GXAgent, object>> columns,
            string userId,
            Guid? agentId = null,
            DateTimeOffset? removed = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs groups = GetAgentGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXAgentGroupAgent>(s => GXSql.One, q => GXSql.Exists<GXAgentGroup, GXAgentGroupAgent>(j => j.Id, j => j.AgentGroupId, groups));
            GXSelectArgs args = GXSelectArgs.Select(columns);
            if (removed == null)
            {
                args.Where.And<GXAgent>(q => q.Removed == null);
            }
            else if (removed != DateTimeOffset.MinValue)
            {
                args.Where.And<GXAgent>(q => q.Removed >= removed);
            }
            args.Where.And<GXAgent>(q => GXSql.Exists<GXAgent, GXAgentGroupAgent>(j => j.Id, j => j.AgentId, map2));
            if (agentId != null && agentId != Guid.Empty)
            {
                args.Where.And<GXAgent>(q => q.Id == agentId);
            }
            if (exclude?.Any() == true)
            {
                args.Where.And<GXAgent>(q => !exclude.Contains(q.Id));
            }
            if (include?.Any() == true)
            {
                args.Where.And<GXAgent>(q => include.Contains(q.Id));
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the agent.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="agentId">Agent id.</param>
        /// <returns>List of users who can access the agent.</returns>
        public static GXSelectArgs GetUsersByAgent(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? agentId)
        {
            _ = userId;
            GXSelectArgs agentGroups = GXSelectArgs.Select<GXAgentGroup>(s => GXSql.One, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXAgentGroupAgent>(s => GXSql.One, w => w.AgentId == agentId);
            agentGroups.Where.And<GXAgentGroupAgent>(q => GXSql.Exists<GXAgentGroup, GXAgentGroupAgent>(j => j.Id, j => j.AgentGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupAgentGroup>(s => GXSql.One, q => GXSql.Exists<GXAgentGroup, GXUserGroupAgentGroup>(j => j.Id, j => j.AgentGroupId, agentGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupAgentGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Returns a collection of users that can access the agents.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="agentIds">Agent ids.</param>
        /// <returns>List of users who can access the agent.</returns>
        public static GXSelectArgs GetUsersByAgents(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? agentIds)
        {
            _ = userId;
            GXSelectArgs agentGroups = GXSelectArgs.Select<GXAgentGroup>(s => GXSql.One, w => w.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXAgentGroupAgent>(s => GXSql.One);
            if (agentIds != null && agentIds.Any())
            {
                map1.Where.And<GXAgentGroupAgent>(w => agentIds.Contains(w.AgentId));
            }
            agentGroups.Where.And<GXAgentGroupAgent>(q => GXSql.Exists<GXAgentGroup, GXAgentGroupAgent>(j => j.Id, j => j.AgentGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupAgentGroup>(s => GXSql.One, q => GXSql.Exists<GXAgentGroup, GXUserGroupAgentGroup>(j => j.Id, j => j.AgentGroupId, agentGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupAgentGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }



        /// <summary>
        /// Get module groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">Module group id.</param>
        /// <param name="removed">Remove time.</param>
        /// <param name="exclude">Module group Ids to exclude.</param>
        /// <param name="include">Module group Ids to include.</param>
        /// <returns>List of Module groups that user can access.</returns>
        public static GXSelectArgs GetModuleGroupsByUser(
            Expression<Func<GXModuleGroup, object>> columns,
            string userId,
            Guid? groupId = null,
            DateTimeOffset? removed = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupModuleGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupModuleGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns);
            if (removed == null)
            {
                args.Where.And<GXModuleGroup>(q => q.Removed == null);
            }
            else if (removed != DateTimeOffset.MinValue)
            {
                args.Where.And<GXModuleGroup>(q => q.Removed >= removed);
            }
            args.Where.And<GXModuleGroup>(q => GXSql.Exists<GXModuleGroup, GXUserGroupModuleGroup>(j => j.Id, j => j.ModuleGroupId, map2));
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXModuleGroup>(q => q.Id == groupId);
            }
            if (exclude?.Any() == true)
            {
                args.Where.And<GXModuleGroup>(q => !exclude.Contains(q.Id));
            }
            if (include?.Any() == true)
            {
                args.Where.And<GXModuleGroup>(q => include.Contains(q.Id));
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the module group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupId">Module group id.</param>
        /// <returns>List of users who can access the module group.</returns>
        public static GXSelectArgs GetUsersByModuleGroup(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXModuleGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupId != null && groupId != Guid.Empty)
            {
                ag.Where.And<GXModuleGroup>(q => q.Id == groupId);
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupModuleGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupModuleGroup>(q => GXSql.Exists<GXModuleGroup, GXUserGroupModuleGroup>(j => j.Id, j => j.ModuleGroupId, ag));
            args.Where.And<GXUserGroupModuleGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the module group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupIds">Module group ids.</param>
        /// <returns>List of users who can access the module group.</returns>
        public static GXSelectArgs GetUsersByModuleGroups(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Distinct = true;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXModuleGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupIds != null && groupIds.Any())
            {
                ag.Where.And<GXModuleGroup>(q => groupIds.Contains(q.Id));
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupModuleGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupModuleGroup>(q => GXSql.Exists<GXModuleGroup, GXUserGroupModuleGroup>(j => j.Id, j => j.ModuleGroupId, ag));
            args.Where.And<GXUserGroupModuleGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Get modules that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="moduleId">Module Id.</param>
        /// <returns>List of modules that user can access.</returns>
        public static GXSelectArgs GetModulesByUser(
            Expression<Func<GXModule, object>> columns,
            string userId,
            string? moduleId = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs groups = GetModuleGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXModuleGroupModule>(s => GXSql.One, q => GXSql.Exists<GXModuleGroup, GXModuleGroupModule>(j => j.Id, j => j.ModuleGroupId, groups));
            GXSelectArgs args = GXSelectArgs.Select(columns);
            args.Where.And<GXModule>(q => GXSql.Exists<GXModule, GXModuleGroupModule>(j => j.Id, j => j.ModuleId, map2));
            if (!string.IsNullOrEmpty(moduleId))
            {
                args.Where.And<GXModule>(q => q.Id == moduleId);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the module.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="id">Module id.</param>
        /// <returns>List of users who can access the module.</returns>
        public static GXSelectArgs GetUsersByModule(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            string? id)
        {
            _ = userId;
            GXSelectArgs moduleGroups = GXSelectArgs.Select<GXModuleGroup>(s => GXSql.One, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXModuleGroupModule>(s => GXSql.One);
            if (!string.IsNullOrEmpty(id))
            {
                map1.Where.And<GXModuleGroupModule>(q => q.ModuleId == id);
            }
            moduleGroups.Where.And<GXModuleGroupModule>(q => GXSql.Exists<GXModuleGroup, GXModuleGroupModule>(j => j.Id, j => j.ModuleGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupModuleGroup>(s => GXSql.One, q => GXSql.Exists<GXModuleGroup, GXUserGroupModuleGroup>(j => j.Id, j => j.ModuleGroupId, moduleGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupModuleGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Returns a collection of users that can access the modules.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="moduleIds">Module ids.</param>
        /// <returns>List of users who can access the module.</returns>
        public static GXSelectArgs GetUsersByModules(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<string>? moduleIds)
        {
            _ = userId;
            GXSelectArgs moduleGroups = GXSelectArgs.Select<GXModuleGroup>(s => GXSql.One, w => w.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXModuleGroupModule>(s => GXSql.One);
            if (moduleIds?.Any() == true)
            {
                map1.Where.And<GXModuleGroupModule>(w => moduleIds.Contains(w.ModuleId));
            }
            moduleGroups.Where.And<GXModuleGroupModule>(q => GXSql.Exists<GXModuleGroup, GXModuleGroupModule>(j => j.Id, j => j.ModuleGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupModuleGroup>(s => GXSql.One, q => GXSql.Exists<GXModuleGroup, GXUserGroupModuleGroup>(j => j.Id, j => j.ModuleGroupId, moduleGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupModuleGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Get menu groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">Menu group id.</param>
        /// <param name="removed">Remove time.</param>
        /// <param name="exclude">Menu group Ids to exclude.</param>
        /// <param name="include">Menu group Ids to include.</param>
        /// <returns>List of Menu groups that user can access.</returns>
        public static GXSelectArgs GetMenuGroupsByUser(
            Expression<Func<GXMenuGroup, object>> columns,
            string userId,
            Guid? groupId = null,
            DateTimeOffset? removed = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupMenuGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupMenuGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns);
            if (removed == null)
            {
                args.Where.And<GXMenuGroup>(q => q.Removed == null);
            }
            else if (removed != DateTimeOffset.MinValue)
            {
                args.Where.And<GXMenuGroup>(q => q.Removed >= removed);
            }
            args.Where.And<GXMenuGroup>(q => GXSql.Exists<GXMenuGroup, GXUserGroupMenuGroup>(j => j.Id, j => j.MenuGroupId, map2));
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXMenuGroup>(q => q.Id == groupId);
            }
            if (exclude?.Any() == true)
            {
                args.Where.And<GXMenuGroup>(q => !exclude.Contains(q.Id));
            }
            if (include?.Any() == true)
            {
                args.Where.And<GXMenuGroup>(q => include.Contains(q.Id));
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the menu group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupId">Menu group id.</param>
        /// <returns>List of users who can access the menu group.</returns>
        public static GXSelectArgs GetUsersByMenuGroup(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXMenuGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupId != null && groupId != Guid.Empty)
            {
                ag.Where.And<GXMenuGroup>(q => q.Id == groupId);
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupMenuGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupMenuGroup>(q => GXSql.Exists<GXMenuGroup, GXUserGroupMenuGroup>(j => j.Id, j => j.MenuGroupId, ag));
            args.Where.And<GXUserGroupMenuGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the menu group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupIds">Menu group ids.</param>
        /// <returns>List of users who can access the menu group.</returns>
        public static GXSelectArgs GetUsersByMenuGroups(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Distinct = true;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXMenuGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupIds != null && groupIds.Any())
            {
                ag.Where.And<GXMenuGroup>(q => groupIds.Contains(q.Id));
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupMenuGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupMenuGroup>(q => GXSql.Exists<GXMenuGroup, GXUserGroupMenuGroup>(j => j.Id, j => j.MenuGroupId, ag));
            args.Where.And<GXUserGroupMenuGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Get menus that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="menuId">Menu Id.</param>
        /// <param name="removed">Remove time.</param>
        /// <param name="exclude">Content Ids to exclude.</param>
        /// <param name="include">Content Ids to include.</param>
        /// <returns>List of menus that user can access.</returns>
        public static GXSelectArgs GetMenusByUser(
            Expression<Func<GXMenu, object>> columns,
            string userId,
            Guid? menuId = null,
            DateTimeOffset? removed = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs groups = GetMenuGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXMenuGroupMenu>(s => GXSql.One, q => GXSql.Exists<GXMenuGroup, GXMenuGroupMenu>(j => j.Id, j => j.MenuGroupId, groups));
            GXSelectArgs args = GXSelectArgs.Select(columns);
            if (removed == null)
            {
                args.Where.And<GXMenu>(q => q.Removed == null);
            }
            else if (removed != DateTimeOffset.MinValue)
            {
                args.Where.And<GXMenu>(q => q.Removed >= removed);
            }
            args.Where.And<GXMenu>(q => GXSql.Exists<GXMenu, GXMenuGroupMenu>(j => j.Id, j => j.MenuId, map2));
            if (menuId != null && menuId != Guid.Empty)
            {
                args.Where.And<GXMenu>(q => q.Id == menuId);
            }
            if (exclude?.Any() == true)
            {
                args.Where.And<GXMenu>(q => !exclude.Contains(q.Id));
            }
            if (include?.Any() == true)
            {
                args.Where.And<GXMenu>(q => include.Contains(q.Id));
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the menu.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="id">Menu id.</param>
        /// <returns>List of users who can access the menu.</returns>
        public static GXSelectArgs GetUsersByMenu(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? id)
        {
            _ = userId;
            GXSelectArgs menuGroups = GXSelectArgs.Select<GXMenuGroup>(s => GXSql.One, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXMenuGroupMenu>(s => GXSql.One);
            if (id != null && id != Guid.Empty)
            {
                map1.Where.And<GXMenuGroupMenu>(w => w.MenuId == id);
            }
            menuGroups.Where.And<GXMenuGroupMenu>(q => GXSql.Exists<GXMenuGroup, GXMenuGroupMenu>(j => j.Id, j => j.MenuGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupMenuGroup>(s => GXSql.One, q => GXSql.Exists<GXMenuGroup, GXUserGroupMenuGroup>(j => j.Id, j => j.MenuGroupId, menuGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupMenuGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Returns a collection of users that can access the menus.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="ids">Menu ids.</param>
        /// <returns>List of users who can access the menu.</returns>
        public static GXSelectArgs GetUsersByMenus(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? ids)
        {
            _ = userId;
            GXSelectArgs menuGroups = GXSelectArgs.Select<GXMenuGroup>(s => GXSql.One, w => w.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXMenuGroupMenu>(s => GXSql.One);
            if (ids != null && ids.Any())
            {
                map1.Where.And<GXMenuGroupMenu>(w => ids.Contains(w.MenuId));
            }
            menuGroups.Where.And<GXMenuGroupMenu>(q => GXSql.Exists<GXMenuGroup, GXMenuGroupMenu>(j => j.Id, j => j.MenuGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupMenuGroup>(s => GXSql.One, q => GXSql.Exists<GXMenuGroup, GXUserGroupMenuGroup>(j => j.Id, j => j.MenuGroupId, menuGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupMenuGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Get view groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">View group id.</param>
        /// <param name="removed">Remove time.</param>
        /// <param name="exclude">View group Ids to exclude.</param>
        /// <param name="include">View group Ids to include.</param>
        /// <returns>List of View groups that user can access.</returns>
        public static GXSelectArgs GetViewGroupsByUser(
            Expression<Func<GXViewGroup, object>> columns,
            string userId,
            Guid? groupId = null,
            DateTimeOffset? removed = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupViewGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupViewGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns);
            if (removed == null)
            {
                args.Where.And<GXViewGroup>(q => q.Removed == null);
            }
            else if (removed != DateTimeOffset.MinValue)
            {
                args.Where.And<GXViewGroup>(q => q.Removed >= removed);
            }
            args.Where.And<GXViewGroup>(q => GXSql.Exists<GXViewGroup, GXUserGroupViewGroup>(j => j.Id, j => j.ViewGroupId, map2));
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXViewGroup>(q => q.Id == groupId);
            }
            if (exclude?.Any() == true)
            {
                args.Where.And<GXViewGroup>(q => !exclude.Contains(q.Id));
            }
            if (include?.Any() == true)
            {
                args.Where.And<GXViewGroup>(q => include.Contains(q.Id));
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the view group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupId">View group id.</param>
        /// <returns>List of users who can access the view group.</returns>
        public static GXSelectArgs GetUsersByViewGroup(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXViewGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupId != null && groupId != Guid.Empty)
            {
                ag.Where.And<GXViewGroup>(q => q.Id == groupId);
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupViewGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupViewGroup>(q => GXSql.Exists<GXViewGroup, GXUserGroupViewGroup>(j => j.Id, j => j.ViewGroupId, ag));
            args.Where.And<GXUserGroupViewGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the view group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupIds">View group ids.</param>
        /// <returns>List of users who can access the view group.</returns>
        public static GXSelectArgs GetUsersByViewGroups(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Distinct = true;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXViewGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupIds != null && groupIds.Any())
            {
                ag.Where.And<GXViewGroup>(q => groupIds.Contains(q.Id));
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupViewGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupViewGroup>(q => GXSql.Exists<GXViewGroup, GXUserGroupViewGroup>(j => j.Id, j => j.ViewGroupId, ag));
            args.Where.And<GXUserGroupViewGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Get views that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="viewId">View Id.</param>
        /// <param name="removed">Remove time.</param>
        /// <param name="exclude">Content Ids to exclude.</param>
        /// <param name="include">Content Ids to include.</param>
        /// <returns>List of views that user can access.</returns>
        public static GXSelectArgs GetViewsByUser(
            Expression<Func<GXView, object>> columns,
            string userId,
            Guid? viewId = null,
            DateTimeOffset? removed = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs groups = GetViewGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXViewGroupView>(s => GXSql.One, q => GXSql.Exists<GXViewGroup, GXViewGroupView>(j => j.Id, j => j.ViewGroupId, groups));
            GXSelectArgs args = GXSelectArgs.Select(columns);
            if (removed == null)
            {
                args.Where.And<GXView>(q => q.Removed == null);
            }
            else if (removed != DateTimeOffset.MinValue)
            {
                args.Where.And<GXView>(q => q.Removed >= removed);
            }
            args.Where.And<GXView>(q => GXSql.Exists<GXView, GXViewGroupView>(j => j.Id, j => j.ViewId, map2));
            if (viewId != null && viewId != Guid.Empty)
            {
                args.Where.And<GXView>(q => q.Id == viewId);
            }
            if (exclude?.Any() == true)
            {
                args.Where.And<GXView>(q => !exclude.Contains(q.Id));
            }
            if (include?.Any() == true)
            {
                args.Where.And<GXView>(q => include.Contains(q.Id));
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the view.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="id">View id.</param>
        /// <returns>List of users who can access the view.</returns>
        public static GXSelectArgs GetUsersByView(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? id)
        {
            _ = userId;
            GXSelectArgs viewGroups = GXSelectArgs.Select<GXViewGroup>(s => GXSql.One, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXViewGroupView>(s => GXSql.One);
            if (id != null && id != Guid.Empty)
            {
                map1.Where.And<GXViewGroupView>(w => w.ViewId == id);
            }
            viewGroups.Where.And<GXViewGroupView>(q => GXSql.Exists<GXViewGroup, GXViewGroupView>(j => j.Id, j => j.ViewGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupViewGroup>(s => GXSql.One, q => GXSql.Exists<GXViewGroup, GXUserGroupViewGroup>(j => j.Id, j => j.ViewGroupId, viewGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupViewGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Returns a collection of users that can access the views.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="ids">View ids.</param>
        /// <returns>List of users who can access the view.</returns>
        public static GXSelectArgs GetUsersByViews(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? ids)
        {
            _ = userId;
            GXSelectArgs viewGroups = GXSelectArgs.Select<GXViewGroup>(s => GXSql.One, w => w.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXViewGroupView>(s => GXSql.One);
            if (ids != null && ids.Any())
            {
                map1.Where.And<GXViewGroupView>(w => ids.Contains(w.ViewId));
            }
            viewGroups.Where.And<GXViewGroupView>(q => GXSql.Exists<GXViewGroup, GXViewGroupView>(j => j.Id, j => j.ViewGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupViewGroup>(s => GXSql.One, q => GXSql.Exists<GXViewGroup, GXUserGroupViewGroup>(j => j.Id, j => j.ViewGroupId, viewGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupViewGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Get block groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">Block group id.</param>
        /// <param name="removed">Remove time.</param>
        /// <param name="exclude">Block group Ids to exclude.</param>
        /// <param name="include">Block group Ids to include.</param>
        /// <returns>List of block groups that user can access.</returns>
        public static GXSelectArgs GetBlockGroupsByUser(
            Expression<Func<GXBlockGroup, object>> columns,
            string userId,
            Guid? groupId = null,
            DateTimeOffset? removed = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupBlockGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupBlockGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns);
            if (removed == null)
            {
                args.Where.And<GXBlockGroup>(q => q.Removed == null);
            }
            else if (removed != DateTimeOffset.MinValue)
            {
                args.Where.And<GXBlockGroup>(q => q.Removed >= removed);
            }
            args.Where.And<GXBlockGroup>(q => GXSql.Exists<GXBlockGroup, GXUserGroupBlockGroup>(j => j.Id, j => j.BlockGroupId, map2));
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXBlockGroup>(q => q.Id == groupId);
            }
            if (exclude?.Any() == true)
            {
                args.Where.And<GXBlockGroup>(q => !exclude.Contains(q.Id));
            }
            if (include?.Any() == true)
            {
                args.Where.And<GXBlockGroup>(q => include.Contains(q.Id));
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the block group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupId">Block group id.</param>
        /// <returns>List of users who can access the block group.</returns>
        public static GXSelectArgs GetUsersByBlockGroup(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXBlockGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupId != null && groupId != Guid.Empty)
            {
                ag.Where.And<GXBlockGroup>(q => q.Id == groupId);
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupBlockGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupBlockGroup>(q => GXSql.Exists<GXBlockGroup, GXUserGroupBlockGroup>(j => j.Id, j => j.BlockGroupId, ag));
            args.Where.And<GXUserGroupBlockGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the block group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupIds">Block group ids.</param>
        /// <returns>List of users who can access the block group.</returns>
        public static GXSelectArgs GetUsersByBlockGroups(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Distinct = true;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXBlockGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupIds != null && groupIds.Any())
            {
                ag.Where.And<GXBlockGroup>(q => groupIds.Contains(q.Id));
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupBlockGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupBlockGroup>(q => GXSql.Exists<GXBlockGroup, GXUserGroupBlockGroup>(j => j.Id, j => j.BlockGroupId, ag));
            args.Where.And<GXUserGroupBlockGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Get blocks that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="blockId">Block Id.</param>
        /// <param name="removed">Remove time.</param>
        /// <param name="exclude">Block Ids to exclude.</param>
        /// <param name="include">Block Ids to include.</param>
        /// <returns>List of blocks that user can access.</returns>
        public static GXSelectArgs GetBlocksByUser(
            Expression<Func<GXBlock, object>> columns,
            string userId,
            Guid? blockId = null,
            DateTimeOffset? removed = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs groups = GetBlockGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXBlockGroupBlock>(s => GXSql.One, q => GXSql.Exists<GXBlockGroup, GXBlockGroupBlock>(j => j.Id, j => j.BlockGroupId, groups));
            GXSelectArgs args = GXSelectArgs.Select(columns);
            if (removed == null)
            {
                args.Where.And<GXBlock>(q => q.Removed == null);
            }
            else if (removed != DateTimeOffset.MinValue)
            {
                args.Where.And<GXBlock>(q => q.Removed >= removed);
            }
            args.Where.And<GXBlock>(q => GXSql.Exists<GXBlock, GXBlockGroupBlock>(j => j.Id, j => j.BlockId, map2));
            if (blockId != null && blockId != Guid.Empty)
            {
                args.Where.And<GXBlock>(q => q.Id == blockId);
            }
            if (exclude?.Any() == true)
            {
                args.Where.And<GXBlock>(w => !exclude.Contains(w.Id));
            }
            if (include?.Any() == true)
            {
                args.Where.And<GXBlock>(w => include.Contains(w.Id));
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the block.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="blockId">Block id.</param>
        /// <returns>List of users who can access the block.</returns>
        public static GXSelectArgs GetUsersByBlock(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? blockId)
        {
            _ = userId;
            GXSelectArgs blockGroups = GXSelectArgs.Select<GXBlockGroup>(s => GXSql.One, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXBlockGroupBlock>(s => GXSql.One, w => w.BlockId == blockId);
            blockGroups.Where.And<GXBlockGroupBlock>(q => GXSql.Exists<GXBlockGroup, GXBlockGroupBlock>(j => j.Id, j => j.BlockGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupBlockGroup>(s => GXSql.One, q => GXSql.Exists<GXBlockGroup, GXUserGroupBlockGroup>(j => j.Id, j => j.BlockGroupId, blockGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupBlockGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Returns a collection of users that can access the blocks.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="ids">Block ids.</param>
        /// <returns>List of users who can access the block.</returns>
        public static GXSelectArgs GetUsersByBlocks(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? ids)
        {
            _ = userId;
            GXSelectArgs blockGroups = GXSelectArgs.Select<GXBlockGroup>(s => GXSql.One, w => w.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXBlockGroupBlock>(s => GXSql.One);
            if (ids != null && ids.Any())
            {
                map1.Where.And<GXBlockGroupBlock>(w => ids.Contains(w.BlockId));
            }
            blockGroups.Where.And<GXBlockGroupBlock>(q => GXSql.Exists<GXBlockGroup, GXBlockGroupBlock>(j => j.Id, j => j.BlockGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupBlockGroup>(s => GXSql.One, q => GXSql.Exists<GXBlockGroup, GXUserGroupBlockGroup>(j => j.Id, j => j.BlockGroupId, blockGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupBlockGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Get manufacturer groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">Manufacturer group id.</param>
        /// <param name="removed">Remove time.</param>
        /// <param name="exclude">Manufacturer group Ids to exclude.</param>
        /// <param name="include">Manufacturer group Ids to include.</param>
        /// <returns>List of Manufacturer groups that user can access.</returns>
        public static GXSelectArgs GetManufacturerGroupsByUser(
            Expression<Func<GXManufacturerGroup, object>> columns,
            string userId,
            Guid? groupId = null,
            DateTimeOffset? removed = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupManufacturerGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupManufacturerGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns);
            if (removed == null)
            {
                args.Where.And<GXManufacturerGroup>(q => q.Removed == null);
            }
            else if (removed != DateTimeOffset.MinValue)
            {
                args.Where.And<GXManufacturerGroup>(q => q.Removed >= removed);
            }
            args.Where.And<GXManufacturerGroup>(q => GXSql.Exists<GXManufacturerGroup, GXUserGroupManufacturerGroup>(j => j.Id, j => j.ManufacturerGroupId, map2));
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXManufacturerGroup>(q => q.Id == groupId);
            }
            if (exclude?.Any() == true)
            {
                args.Where.And<GXManufacturerGroup>(q => !exclude.Contains(q.Id));
            }
            if (include?.Any() == true)
            {
                args.Where.And<GXManufacturerGroup>(q => include.Contains(q.Id));
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the manufacturer group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupId">Manufacturer group id.</param>
        /// <returns>List of users who can access the manufacturer group.</returns>
        public static GXSelectArgs GetUsersByManufacturerGroup(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXManufacturerGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupId != null && groupId != Guid.Empty)
            {
                ag.Where.And<GXManufacturerGroup>(q => q.Id == groupId);
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupManufacturerGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupManufacturerGroup>(q => GXSql.Exists<GXManufacturerGroup, GXUserGroupManufacturerGroup>(j => j.Id, j => j.ManufacturerGroupId, ag));
            args.Where.And<GXUserGroupManufacturerGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the manufacturer group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupIds">Manufacturer group ids.</param>
        /// <returns>List of users who can access the manufacturer group.</returns>
        public static GXSelectArgs GetUsersByManufacturerGroups(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Distinct = true;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXManufacturerGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupIds != null && groupIds.Any())
            {
                ag.Where.And<GXManufacturerGroup>(q => groupIds.Contains(q.Id));
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupManufacturerGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupManufacturerGroup>(q => GXSql.Exists<GXManufacturerGroup, GXUserGroupManufacturerGroup>(j => j.Id, j => j.ManufacturerGroupId, ag));
            args.Where.And<GXUserGroupManufacturerGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Get manufacturers that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="manufacturerId">Manufacturer Id.</param>
        /// <returns>List of manufacturers that user can access.</returns>
        public static GXSelectArgs GetManufacturersByUser(
            Expression<Func<GXManufacturer, object>> columns,
            string userId,
            Guid? manufacturerId = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs groups = GetManufacturerGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXManufacturerGroupManufacturer>(s => GXSql.One, q => GXSql.Exists<GXManufacturerGroup, GXManufacturerGroupManufacturer>(j => j.Id, j => j.ManufacturerGroupId, groups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXManufacturer>(q => GXSql.Exists<GXManufacturer, GXManufacturerGroupManufacturer>(j => j.Id, j => j.ManufacturerId, map2));
            if (manufacturerId != null && manufacturerId != Guid.Empty)
            {
                args.Where.And<GXManufacturer>(q => q.Id == manufacturerId);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the manufacturer.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="id">Manufacturer id.</param>
        /// <returns>List of users who can access the manufacturer.</returns>
        public static GXSelectArgs GetUsersByManufacturer(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? id)
        {
            _ = userId;
            GXSelectArgs manufacturerGroups = GXSelectArgs.Select<GXManufacturerGroup>(s => GXSql.One, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXManufacturerGroupManufacturer>(s => GXSql.One);
            if (id != null && id != Guid.Empty)
            {
                map1.Where.And<GXManufacturerGroupManufacturer>(w => w.ManufacturerId == id);
            }
            manufacturerGroups.Where.And<GXManufacturerGroupManufacturer>(q => GXSql.Exists<GXManufacturerGroup, GXManufacturerGroupManufacturer>(j => j.Id, j => j.ManufacturerGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupManufacturerGroup>(s => GXSql.One, q => GXSql.Exists<GXManufacturerGroup, GXUserGroupManufacturerGroup>(j => j.Id, j => j.ManufacturerGroupId, manufacturerGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupManufacturerGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Returns a collection of users that can access the manufacturers.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="ids">Manufacturer ids.</param>
        /// <returns>List of users who can access the manufacturer.</returns>
        public static GXSelectArgs GetUsersByManufacturers(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? ids)
        {
            _ = userId;
            GXSelectArgs manufacturerGroups = GXSelectArgs.Select<GXManufacturerGroup>(s => GXSql.One, w => w.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXManufacturerGroupManufacturer>(s => GXSql.One);
            if (ids != null && ids.Any())
            {
                map1.Where.And<GXManufacturerGroupManufacturer>(w => ids.Contains(w.ManufacturerId));
            }
            manufacturerGroups.Where.And<GXManufacturerGroupManufacturer>(q => GXSql.Exists<GXManufacturerGroup, GXManufacturerGroupManufacturer>(j => j.Id, j => j.ManufacturerGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupManufacturerGroup>(s => GXSql.One, q => GXSql.Exists<GXManufacturerGroup, GXUserGroupManufacturerGroup>(j => j.Id, j => j.ManufacturerGroupId, manufacturerGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupManufacturerGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Get key management groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">KeyManagement group id.</param>
        /// <param name="removed">Remove time.</param>
        /// <param name="exclude">KeyManagement group Ids to exclude.</param>
        /// <param name="include">KeyManagement group Ids to include.</param>
        /// <returns>List of KeyManagement groups that user can access.</returns>
        public static GXSelectArgs GetKeyManagementGroupsByUser(
            Expression<Func<GXKeyManagementGroup, object>> columns,
            string userId,
            Guid? groupId = null,
            DateTimeOffset? removed = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupKeyManagementGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupKeyManagementGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns);
            if (removed == null)
            {
                args.Where.And<GXKeyManagementGroup>(q => q.Removed == null);
            }
            else if (removed != DateTimeOffset.MinValue)
            {
                args.Where.And<GXKeyManagementGroup>(q => q.Removed >= removed);
            }
            args.Where.And<GXKeyManagementGroup>(q => GXSql.Exists<GXKeyManagementGroup, GXUserGroupKeyManagementGroup>(j => j.Id, j => j.KeyManagementGroupId, map2));
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXKeyManagementGroup>(q => q.Id == groupId);
            }
            if (exclude?.Any() == true)
            {
                args.Where.And<GXKeyManagementGroup>(q => !exclude.Contains(q.Id));
            }
            if (include?.Any() == true)
            {
                args.Where.And<GXKeyManagementGroup>(q => include.Contains(q.Id));
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the key management group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupId">KeyManagement group id.</param>
        /// <returns>List of users who can access the key management group.</returns>
        public static GXSelectArgs GetUsersByKeyManagementGroup(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXKeyManagementGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupId != null && groupId != Guid.Empty)
            {
                ag.Where.And<GXKeyManagementGroup>(q => q.Id == groupId);
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupKeyManagementGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupKeyManagementGroup>(q => GXSql.Exists<GXKeyManagementGroup, GXUserGroupKeyManagementGroup>(j => j.Id, j => j.KeyManagementGroupId, ag));
            args.Where.And<GXUserGroupKeyManagementGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the key management group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupIds">KeyManagement group ids.</param>
        /// <returns>List of users who can access the key management group.</returns>
        public static GXSelectArgs GetUsersByKeyManagementGroups(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Distinct = true;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXKeyManagementGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupIds != null && groupIds.Any())
            {
                ag.Where.And<GXKeyManagementGroup>(q => groupIds.Contains(q.Id));
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupKeyManagementGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupKeyManagementGroup>(q => GXSql.Exists<GXKeyManagementGroup, GXUserGroupKeyManagementGroup>(j => j.Id, j => j.KeyManagementGroupId, ag));
            args.Where.And<GXUserGroupKeyManagementGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Get key managements that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="keyManagementId">Key management id.</param>
        /// <returns>List of key managements that user can access.</returns>
        public static GXSelectArgs GetKeyManagementsByUser(
            Expression<Func<GXKeyManagement, object>> columns,
            string userId,
            Guid? keyManagementId = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs groups = GetKeyManagementGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXKeyManagementGroupKeyManagement>(s => GXSql.One, q => GXSql.Exists<GXKeyManagementGroup, GXKeyManagementGroupKeyManagement>(j => j.Id, j => j.KeyManagementGroupId, groups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXKeyManagement>(q => GXSql.Exists<GXKeyManagement, GXKeyManagementGroupKeyManagement>(j => j.Id, j => j.KeyManagementId, map2));
            if (keyManagementId != null && keyManagementId != Guid.Empty)
            {
                args.Where.And<GXKeyManagement>(q => q.Id == keyManagementId);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the key management.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="keyManagementId">Key management Id.</param>
        /// <returns>List of users who can access the key management.</returns>
        public static GXSelectArgs GetUsersByKeyManagement(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid keyManagementId)
        {
            return GetUsersByKeyManagements(
            columns,
            userId,
            [keyManagementId]);
        }

        /// <summary>
        /// Returns a collection of users that can access the key managements.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="ids">KeyManagement ids.</param>
        /// <returns>List of users who can access the key management.</returns>
        public static GXSelectArgs GetUsersByKeyManagements(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? ids)
        {
            _ = userId;
            GXSelectArgs groups = GXSelectArgs.Select<GXKeyManagementGroup>(s => GXSql.One, w => w.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXKeyManagementGroupKeyManagement>(s => GXSql.One);
            if (ids != null && ids.Any())
            {
                map1.Where.And<GXKeyManagementGroupKeyManagement>(w => ids.Contains(w.KeyManagementId));
            }
            groups.Where.And<GXKeyManagementGroupKeyManagement>(q => GXSql.Exists<GXKeyManagementGroup, GXKeyManagementGroupKeyManagement>(j => j.Id, j => j.KeyManagementGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupKeyManagementGroup>(s => GXSql.One, q => GXSql.Exists<GXKeyManagementGroup, GXUserGroupKeyManagementGroup>(j => j.Id, j => j.KeyManagementGroupId, groups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupKeyManagementGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Returns active users that satisfy the named authorization policy.
        /// </summary>
        /// <param name="action">Notification action. Access is determined by the policy, independently of the action.</param>
        /// <param name="permissions">Authorization policy name.</param>
        /// <returns>A query selecting the identifiers of users who satisfy the policy.</returns>
        public static GXSelectArgs GetUsersByPermission(
            NotificationAction action, string permissions)
        {
            _ = action;
            ArgumentException.ThrowIfNullOrWhiteSpace(permissions);
            string normalizedName = permissions.ToUpperInvariant();
            string permissionType = PolicyRequirementType.Permission.ToString();
            string roleType = System.Security.Claims.ClaimTypes.Role;

            // Reserved claims are rebuilt from current membership tables by the principal factory.
            // Persisted copies must not keep revoked grants alive in notification queries.
            GXSelectArgs userClaims = GXSelectArgs.Select<GXUserClaim>(s => GXSql.One);
            // A join keeps outer column references qualified in the ORM SQL translator.
            userClaims.Joins.AddLeftJoin<GXUserClaim, GXUserRole>(c => c.UserId!, m => m.UserId!);
            userClaims.Where.And(CorrelatedPredicate<GXUserClaim, GXUser>((c, u) => c.UserId == u.Id));
            userClaims.Where.And(CorrelatedPredicate<GXUserClaim, GXPolicyRequirement>((c, r) =>
                (r.Type == PolicyRequirementType.Claim && c.ClaimType != permissionType && c.ClaimType != roleType
                    && c.ClaimType == r.ClaimType
                    && (string.IsNullOrWhiteSpace(r.Value) || c.ClaimValue == r.Value))));

            GXSelectArgs roleClaims = GXSelectArgs.Select<GXRoleClaim>(s => GXSql.One);
            roleClaims.Joins.AddInnerJoin<GXRoleClaim, GXUserRole>(c => c.RoleId, u => u.RoleId!);
            roleClaims.Joins.AddInnerJoin<GXUserRole, GXRole>(u => u.RoleId!, r => r.Id);
            roleClaims.Where.And<GXRole>(r => r.Removed == null);
            roleClaims.Where.And(CorrelatedPredicate<GXUserRole, GXUser>((m, u) => m.UserId == u.Id));
            roleClaims.Where.And(CorrelatedPredicate<GXRoleClaim, GXPolicyRequirement>((c, r) =>
                (r.Type == PolicyRequirementType.Claim && c.ClaimType != permissionType && c.ClaimType != roleType
                    && c.ClaimType == r.ClaimType
                    && (string.IsNullOrWhiteSpace(r.Value) || c.ClaimValue == r.Value))));

            GXSelectArgs roles = GXSelectArgs.Select<GXRole>(s => GXSql.One, r => r.Removed == null);
            roles.Joins.AddInnerJoin<GXRole, GXUserRole>(r => r.Id, u => u.RoleId!);
            roles.Where.And(CorrelatedPredicate<GXUserRole, GXUser>((m, u) => m.UserId == u.Id));
            roles.Where.And(CorrelatedPredicate<GXRole, GXPolicyRequirement>((role, r) =>
                (r.Type == PolicyRequirementType.Role && role.Name == r.Value)
                || (r.Type == PolicyRequirementType.Claim && r.ClaimType == roleType
                    && (string.IsNullOrWhiteSpace(r.Value) || role.Name == r.Value))));

            GXSelectArgs directPermissions = GXSelectArgs.Select<GXPermission>(p => GXSql.One);
            directPermissions.Joins.AddInnerJoin<GXPermission, GXUserPermission>(p => p.Id, m => m.PermissionId!);
            directPermissions.Where.And(CorrelatedPredicate<GXUserPermission, GXUser>((m, u) => m.UserId == u.Id));
            directPermissions.Where.And(CorrelatedPredicate<GXPermission, GXPolicyRequirement>((p, r) =>
                (r.Type == PolicyRequirementType.Permission && p.Name == r.Value)
                || (r.Type == PolicyRequirementType.Claim && r.ClaimType == permissionType
                    && (string.IsNullOrWhiteSpace(r.Value) || p.Name == r.Value))));

            GXSelectArgs inheritedPermissions = GXSelectArgs.Select<GXPermission>(p => GXSql.One);
            inheritedPermissions.Joins.AddInnerJoin<GXPermission, GXRolePermission>(p => p.Id, m => m.PermissionId!);
            inheritedPermissions.Joins.AddInnerJoin<GXRolePermission, GXUserRole>(m => m.RoleId!, u => u.RoleId!);
            inheritedPermissions.Joins.AddInnerJoin<GXUserRole, GXRole>(m => m.RoleId!, role => role.Id);
            inheritedPermissions.Where.And<GXRole>(role => role.Removed == null);
            inheritedPermissions.Where.And(CorrelatedPredicate<GXUserRole, GXUser>((m, u) => m.UserId == u.Id));
            inheritedPermissions.Where.And(CorrelatedPredicate<GXPermission, GXPolicyRequirement>((p, r) =>
                (r.Type == PolicyRequirementType.Permission && p.Name == r.Value)
                || (r.Type == PolicyRequirementType.Claim && r.ClaimType == permissionType
                    && (string.IsNullOrWhiteSpace(r.Value) || p.Name == r.Value))));

            Expression<Func<GXPolicyRequirement, bool>> satisfied = r =>
                r.Type == PolicyRequirementType.AnyUser ||
                (((r.Type == PolicyRequirementType.Permission && !string.IsNullOrWhiteSpace(r.Value))
                    || (r.Type == PolicyRequirementType.Role && !string.IsNullOrWhiteSpace(r.Value))
                    || (r.Type == PolicyRequirementType.Claim && !string.IsNullOrWhiteSpace(r.ClaimType)))
                    && (GXSql.Exists(userClaims) || GXSql.Exists(roleClaims) || GXSql.Exists(roles) || GXSql.Exists(directPermissions) || GXSql.Exists(inheritedPermissions)));

            GXSelectArgs matching = GXSelectArgs.Select<GXPolicyRequirement>(s => GXSql.DistinctCount(s.Id));
            // Keep correlation columns qualified; count distinct requirements despite join duplicates.
            matching.Joins.AddLeftJoin<GXPolicyRequirement, GXUserClaim>(r => r.Value!, c => c.ClaimValue!);
            matching.Where.And(CorrelatedPredicate<GXPolicyRequirement, GXPolicy>((r, p) => r.Policy == p));
            matching.Where.And(satisfied);
            GXSelectArgs requirements = GXSelectArgs.Select<GXPolicyRequirement>(s => GXSql.DistinctCount(s.Id));
            requirements.Joins.AddLeftJoin<GXPolicyRequirement, GXUserClaim>(r => r.Value!, c => c.ClaimValue!);
            requirements.Where.And(CorrelatedPredicate<GXPolicyRequirement, GXPolicy>((r, p) => r.Policy == p));
            requirements.Where.And<GXPolicyRequirement>(r => r.Type != PolicyRequirementType.AuthenticatedUser);
            GXSelectArgs authentication = GXSelectArgs.Select<GXPolicyRequirement>(s => GXSql.DistinctCount(s.Id));
            authentication.Joins.AddLeftJoin<GXPolicyRequirement, GXUserClaim>(r => r.Value!, c => c.ClaimValue!);
            authentication.Where.And(CorrelatedPredicate<GXPolicyRequirement, GXPolicy>((r, p) => r.Policy == p));
            authentication.Where.And<GXPolicyRequirement>(r => r.Type == PolicyRequirementType.AuthenticatedUser);

            GXSelectArgs policies = GXSelectArgs.Select<GXPolicy>(s => GXSql.One,
                p => p.NormalizedName == normalizedName && p.Removed == null);
            policies.Where.And<GXPolicy>(p => p.RequirementOperator == PolicyRequirementOperator.Or);
            policies.Where.And<GXPolicy>(p => GXSql.Scalar<int>(matching) > 0
                || (GXSql.Scalar<int>(requirements) == 0 && GXSql.Scalar<int>(authentication) > 0));
            GXSelectArgs andPolicies = GXSelectArgs.Select<GXPolicy>(s => GXSql.One,
                p => p.NormalizedName == normalizedName && p.Removed == null);
            andPolicies.Where.And<GXPolicy>(p => p.RequirementOperator == PolicyRequirementOperator.And);
            andPolicies.Where.And<GXPolicy>(p => GXSql.Scalar<int>(requirements) > 0 || GXSql.Scalar<int>(authentication) > 0);
            andPolicies.Where.And<GXPolicy>(p => GXSql.Scalar<int>(matching) == GXSql.Scalar<int>(requirements));

            GXSelectArgs users = GXSelectArgs.Select<GXUser>(u => u.Id!, u => u.Removed == null);
            users.Where.And<GXUser>(u => GXSql.Exists(policies) || GXSql.Exists(andPolicies));
            return users;
        }

        /// <summary>
        /// Keeps the outer table parameter in a predicate for a correlated subquery.
        /// </summary>
        private static Expression<Func<T, bool>> CorrelatedPredicate<T, TOuter>(
            Expression<Func<T, TOuter, bool>> predicate)
        {
            return Expression.Lambda<Func<T, bool>>(predicate.Body, predicate.Parameters[0]);
        }

        /// <summary>
        /// Get schedule groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">Schedule group id.</param>
        /// <param name="removed">Remove time.</param>
        /// <param name="exclude">Schedule group Ids to exclude.</param>
        /// <param name="include">Schedule group Ids to include.</param>
        /// <returns>List of schedule groups that user can access.</returns>
        public static GXSelectArgs GetScheduleGroupsByUser(
            Expression<Func<GXScheduleGroup, object>> columns,
            string userId,
            Guid? groupId = null,
            DateTimeOffset? removed = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupScheduleGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupScheduleGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns);
            if (removed == null)
            {
                args.Where.And<GXScheduleGroup>(q => q.Removed == null);
            }
            else if (removed != DateTimeOffset.MinValue)
            {
                args.Where.And<GXScheduleGroup>(q => q.Removed >= removed);
            }
            args.Where.And<GXScheduleGroup>(q => GXSql.Exists<GXScheduleGroup, GXUserGroupScheduleGroup>(j => j.Id, j => j.ScheduleGroupId, map2));
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXScheduleGroup>(q => q.Id == groupId);
            }
            if (include != null && include.Any())
            {
                args.Where.And<GXScheduleGroup>(q => include.Contains(q.Id));
            }
            if (exclude != null && exclude.Any())
            {
                args.Where.And<GXScheduleGroup>(q => !exclude.Contains(q.Id));
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the schedule group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupId">Schedule group id.</param>
        /// <returns>List of users who can access the schedule group.</returns>
        public static GXSelectArgs GetUsersByScheduleGroup(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXScheduleGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupId != null && groupId != Guid.Empty)
            {
                ag.Where.And<GXScheduleGroup>(q => q.Id == groupId);
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupScheduleGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupScheduleGroup>(q => GXSql.Exists<GXScheduleGroup, GXUserGroupScheduleGroup>(j => j.Id, j => j.ScheduleGroupId, ag));
            args.Where.And<GXUserGroupScheduleGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the schedule group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupIds">Schedule group ids.</param>
        /// <returns>List of users who can access the schedule group.</returns>
        public static GXSelectArgs GetUsersByScheduleGroups(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Distinct = true;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXScheduleGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupIds != null && groupIds.Any())
            {
                ag.Where.And<GXScheduleGroup>(q => groupIds.Contains(q.Id));
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupScheduleGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupScheduleGroup>(q => GXSql.Exists<GXScheduleGroup, GXUserGroupScheduleGroup>(j => j.Id, j => j.ScheduleGroupId, ag));
            args.Where.And<GXUserGroupScheduleGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Get schedules that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="scheduleId">Schedule Id.</param>
        /// <param name="removed">Remove time.</param>
        /// <param name="exclude">Schedule Ids to exclude.</param>
        /// <param name="include">Schedule Ids to include.</param>
        /// <returns>List of schedules that user can access.</returns>
        public static GXSelectArgs GetSchedulesByUser(
            Expression<Func<GXSchedule, object>> columns,
            string userId,
            Guid? scheduleId = null,
            DateTimeOffset? removed = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs groups = GetScheduleGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXScheduleGroupSchedule>(s => GXSql.One, q => GXSql.Exists<GXScheduleGroup, GXScheduleGroupSchedule>(j => j.Id, j => j.ScheduleGroupId, groups));
            GXSelectArgs args = GXSelectArgs.Select(columns);
            if (removed == null)
            {
                args.Where.And<GXSchedule>(q => q.Removed == null);
            }
            else if (removed != DateTimeOffset.MinValue)
            {
                args.Where.And<GXSchedule>(q => q.Removed >= removed);
            }
            args.Where.And<GXSchedule>(q => GXSql.Exists<GXSchedule, GXScheduleGroupSchedule>(j => j.Id, j => j.ScheduleId, map2));
            if (scheduleId != null && scheduleId != Guid.Empty)
            {
                args.Where.And<GXSchedule>(q => q.Id == scheduleId);
            }
            if (include != null && include.Any())
            {
                args.Where.And<GXSchedule>(q => include.Contains(q.Id));
            }
            if (exclude != null && exclude.Any())
            {
                args.Where.And<GXSchedule>(q => !exclude.Contains(q.Id));
            }
            args.Columns.Exclude<GXUser>(e => e.Schedules!);
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the schedule.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="id">Schedule id.</param>
        /// <returns>List of users who can access the schedule.</returns>
        public static GXSelectArgs GetUsersBySchedule(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? id)
        {
            _ = userId;
            GXSelectArgs scheduleGroups = GXSelectArgs.Select<GXScheduleGroup>(s => GXSql.One, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXScheduleGroupSchedule>(s => GXSql.One);
            if (id != null && id != Guid.Empty)
            {
                map1.Where.And<GXScheduleGroupSchedule>(w => w.ScheduleId == id);
            }
            scheduleGroups.Where.And<GXScheduleGroupSchedule>(q => GXSql.Exists<GXScheduleGroup, GXScheduleGroupSchedule>(j => j.Id, j => j.ScheduleGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupScheduleGroup>(s => GXSql.One, q => GXSql.Exists<GXScheduleGroup, GXUserGroupScheduleGroup>(j => j.Id, j => j.ScheduleGroupId, scheduleGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupScheduleGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Returns a collection of users that can access the schedules.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="ids">Schedule ids.</param>
        /// <returns>List of users who can access the schedule.</returns>
        public static GXSelectArgs GetUsersBySchedules(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? ids)
        {
            _ = userId;
            GXSelectArgs scheduleGroups = GXSelectArgs.Select<GXScheduleGroup>(s => GXSql.One, w => w.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXScheduleGroupSchedule>(s => GXSql.One);
            if (ids != null && ids.Any())
            {
                map1.Where.And<GXScheduleGroupSchedule>(w => ids.Contains(w.ScheduleId));
            }
            scheduleGroups.Where.And<GXScheduleGroupSchedule>(q => GXSql.Exists<GXScheduleGroup, GXScheduleGroupSchedule>(j => j.Id, j => j.ScheduleGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupScheduleGroup>(s => GXSql.One, q => GXSql.Exists<GXScheduleGroup, GXUserGroupScheduleGroup>(j => j.Id, j => j.ScheduleGroupId, scheduleGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupScheduleGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Get script groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">Script group id.</param>
        /// <param name="removed">Remove time.</param>
        /// <param name="exclude">Script group Ids to exclude.</param>
        /// <param name="include">Script group Ids to include.</param>
        /// <returns>List of Script groups that user can access.</returns>
        public static GXSelectArgs GetScriptGroupsByUser(
            Expression<Func<GXScriptGroup, object>> columns,
            string userId,
            Guid? groupId = null,
            DateTimeOffset? removed = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupScriptGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupScriptGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns);
            if (removed == null)
            {
                args.Where.And<GXScriptGroup>(q => q.Removed == null);
            }
            else if (removed != DateTimeOffset.MinValue)
            {
                args.Where.And<GXScriptGroup>(q => q.Removed >= removed);
            }

            args.Where.And<GXScriptGroup>(q => GXSql.Exists<GXScriptGroup, GXUserGroupScriptGroup>(j => j.Id, j => j.ScriptGroupId, map2));
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXScriptGroup>(q => q.Id == groupId);
            }
            if (exclude?.Any() == true)
            {
                args.Where.And<GXScriptGroup>(q => !exclude.Contains(q.Id));
            }
            if (include?.Any() == true)
            {
                args.Where.And<GXScriptGroup>(q => include.Contains(q.Id));
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the script group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupId">Script group id.</param>
        /// <returns>List of users who can access the script group.</returns>
        public static GXSelectArgs GetUsersByScriptGroup(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXScriptGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupId != null && groupId != Guid.Empty)
            {
                ag.Where.And<GXScriptGroup>(q => q.Id == groupId);
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupScriptGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupScriptGroup>(q => GXSql.Exists<GXScriptGroup, GXUserGroupScriptGroup>(j => j.Id, j => j.ScriptGroupId, ag));
            args.Where.And<GXUserGroupScriptGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the script group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupIds">Script group ids.</param>
        /// <returns>List of users who can access the script group.</returns>
        public static GXSelectArgs GetUsersByScriptGroups(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Distinct = true;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXScriptGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupIds != null && groupIds.Any())
            {
                ag.Where.And<GXScriptGroup>(q => groupIds.Contains(q.Id));
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupScriptGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupScriptGroup>(q => GXSql.Exists<GXScriptGroup, GXUserGroupScriptGroup>(j => j.Id, j => j.ScriptGroupId, ag));
            args.Where.And<GXUserGroupScriptGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Get scripts that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="scriptId">Script Id.</param>
        /// <param name="removed">Remove time.</param>
        /// <param name="exclude">Script Ids to exclude.</param>
        /// <param name="include">Script Ids to include.</param>
        /// <returns>List of scripts that user can access.</returns>
        public static GXSelectArgs GetScriptsByUser(
            Expression<Func<GXScript, object>> columns,
            string userId,
            Guid? scriptId = null,
            DateTimeOffset? removed = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs groups = GetScriptGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXScriptGroupScript>(s => GXSql.One, q => GXSql.Exists<GXScriptGroup, GXScriptGroupScript>(j => j.Id, j => j.ScriptGroupId, groups));
            GXSelectArgs args = GXSelectArgs.Select(columns);
            if (removed == null)
            {
                args.Where.And<GXScript>(q => q.Removed == null);
            }
            else if (removed != DateTimeOffset.MinValue)
            {
                args.Where.And<GXScript>(q => q.Removed >= removed);
            }
            args.Where.And<GXScript>(q => GXSql.Exists<GXScript, GXScriptGroupScript>(j => j.Id, j => j.ScriptId, map2));
            if (scriptId != null && scriptId != Guid.Empty)
            {
                args.Where.And<GXScript>(q => q.Id == scriptId);
            }
            if (include?.Any() == true)
            {
                args.Where.And<GXScript>(q => include.Contains(q.Id));
            }
            if (exclude?.Any() == true)
            {
                args.Where.And<GXScript>(q => !exclude.Contains(q.Id));
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the script.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="scriptId">Script id.</param>
        /// <returns>List of users who can access the script.</returns>
        public static GXSelectArgs GetUsersByScript(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid scriptId)
        {
            _ = userId;
            GXSelectArgs scriptGroups = GXSelectArgs.Select<GXScriptGroup>(s => GXSql.One, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXScriptGroupScript>(s => GXSql.One, w => w.ScriptId == scriptId);
            scriptGroups.Where.And<GXScriptGroupScript>(q => GXSql.Exists<GXScriptGroup, GXScriptGroupScript>(j => j.Id, j => j.ScriptGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupScriptGroup>(s => GXSql.One, q => GXSql.Exists<GXScriptGroup, GXUserGroupScriptGroup>(j => j.Id, j => j.ScriptGroupId, scriptGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupScriptGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Returns a collection of users that can access the scripts.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="ids">Script ids.</param>
        /// <returns>List of users who can access the script.</returns>
        public static GXSelectArgs GetUsersByScripts(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? ids)
        {
            _ = userId;
            GXSelectArgs scriptGroups = GXSelectArgs.Select<GXScriptGroup>(s => GXSql.One, w => w.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXScriptGroupScript>(s => GXSql.One);
            if (ids != null && ids.Any())
            {
                map1.Where.And<GXScriptGroupScript>(w => ids.Contains(w.ScriptId));
            }
            scriptGroups.Where.And<GXScriptGroupScript>(q => GXSql.Exists<GXScriptGroup, GXScriptGroupScript>(j => j.Id, j => j.ScriptGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupScriptGroup>(s => GXSql.One, q => GXSql.Exists<GXScriptGroup, GXUserGroupScriptGroup>(j => j.Id, j => j.ScriptGroupId, scriptGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupScriptGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Get component view groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">ComponentView group id.</param>
        /// <param name="removed">Remove time.</param>
        /// <param name="exclude">List of component view group ids to exclude.</param>
        /// <param name="include">List of component view group ids to include.</param>
        /// <returns>List of component view groups that user can access.</returns>
        public static GXSelectArgs GetComponentViewGroupsByUser(
            Expression<Func<GXComponentViewGroup, object>> columns,
            string userId,
            Guid? groupId = null,
            DateTimeOffset? removed = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupComponentViewGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupComponentViewGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns);
            if (removed == null)
            {
                args.Where.And<GXComponentViewGroup>(q => q.Removed == null);
            }
            else if (removed != DateTimeOffset.MinValue)
            {
                args.Where.And<GXComponentViewGroup>(q => q.Removed >= removed);
            }
            args.Where.And<GXComponentViewGroup>(q => GXSql.Exists<GXComponentViewGroup, GXUserGroupComponentViewGroup>(j => j.Id, j => j.ComponentViewGroupId, map2));
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXComponentViewGroup>(q => q.Id == groupId);
            }
            if (exclude?.Any() == true)
            {
                args.Where.And<GXComponentViewGroup>(q => !exclude.Contains(q.Id));
            }
            if (include?.Any() == true)
            {
                args.Where.And<GXComponentViewGroup>(q => include.Contains(q.Id));
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the component view group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupId">ComponentView group id.</param>
        /// <returns>List of users who can access the component view group.</returns>
        public static GXSelectArgs GetUsersByComponentViewGroup(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXComponentViewGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupId != null && groupId != Guid.Empty)
            {
                ag.Where.And<GXComponentViewGroup>(q => q.Id == groupId);
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupComponentViewGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupComponentViewGroup>(q => GXSql.Exists<GXComponentViewGroup, GXUserGroupComponentViewGroup>(j => j.Id, j => j.ComponentViewGroupId, ag));
            args.Where.And<GXUserGroupComponentViewGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the component view group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupIds">ComponentView group ids.</param>
        /// <returns>List of users who can access the component view group.</returns>
        public static GXSelectArgs GetUsersByComponentViewGroups(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Distinct = true;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXComponentViewGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupIds != null && groupIds.Any())
            {
                ag.Where.And<GXComponentViewGroup>(q => groupIds.Contains(q.Id));
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupComponentViewGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupComponentViewGroup>(q => GXSql.Exists<GXComponentViewGroup, GXUserGroupComponentViewGroup>(j => j.Id, j => j.ComponentViewGroupId, ag));
            args.Where.And<GXUserGroupComponentViewGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Get component views that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="id">ComponentView Id.</param>
        /// <returns>List of component views that user can access.</returns>
        public static GXSelectArgs GetComponentViewsByUser(
            Expression<Func<GXComponentView, object>> columns,
            string userId,
            Guid? id = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs groups = GetComponentViewGroupsByUser(s => GXSql.One, userId, id);
            GXSelectArgs map2 = GXSelectArgs.Select<GXComponentViewGroupComponentView>(s => GXSql.One, q => GXSql.Exists<GXComponentViewGroup, GXComponentViewGroupComponentView>(j => j.Id, j => j.ComponentViewGroupId, groups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXComponentView>(q => GXSql.Exists<GXComponentView, GXComponentViewGroupComponentView>(j => j.Id, j => j.ComponentViewId, map2));
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the component view.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="id">ComponentView id.</param>
        /// <returns>List of users who can access the component view.</returns>
        public static GXSelectArgs GetUsersByComponentView(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? id)
        {
            _ = userId;
            GXSelectArgs groups = GXSelectArgs.Select<GXComponentViewGroup>(s => GXSql.One, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXComponentViewGroupComponentView>(s => GXSql.One, w => w.ComponentViewId == id);
            groups.Where.And<GXComponentViewGroupComponentView>(q => GXSql.Exists<GXComponentViewGroup, GXComponentViewGroupComponentView>(j => j.Id, j => j.ComponentViewGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupComponentViewGroup>(s => GXSql.One, q => GXSql.Exists<GXComponentViewGroup, GXUserGroupComponentViewGroup>(j => j.Id, j => j.ComponentViewGroupId, groups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupComponentViewGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Returns a collection of users that can access the component views.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="ids">ComponentView ids.</param>
        /// <returns>List of users who can access the component view.</returns>
        public static GXSelectArgs GetUsersByComponentViews(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? ids)
        {
            _ = userId;
            GXSelectArgs groups = GXSelectArgs.Select<GXComponentViewGroup>(s => GXSql.One, w => w.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXComponentViewGroupComponentView>(s => GXSql.One);
            if (ids != null && ids.Any())
            {
                map1.Where.And<GXComponentViewGroupComponentView>(w => ids.Contains(w.ComponentViewId));
            }
            groups.Where.And<GXComponentViewGroupComponentView>(q => GXSql.Exists<GXComponentViewGroup, GXComponentViewGroupComponentView>(j => j.Id, j => j.ComponentViewGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupComponentViewGroup>(s => GXSql.One, q => GXSql.Exists<GXComponentViewGroup, GXUserGroupComponentViewGroup>(j => j.Id, j => j.ComponentViewGroupId, groups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupComponentViewGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Get device template groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">DeviceTemplate group id.</param>
        /// <param name="removed">Remove time.</param>
        /// <param name="exclude">Device template group Ids to exclude.</param>
        /// <param name="include">Device template group Ids to include.</param>
        /// <returns>List of device template groups that user can access.</returns>
        public static GXSelectArgs GetDeviceTemplateGroupsByUser(
            Expression<Func<GXDeviceTemplateGroup, object>> columns,
            string userId,
            Guid? groupId = null,
            DateTimeOffset? removed = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupDeviceTemplateGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupDeviceTemplateGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns);
            if (removed == null)
            {
                args.Where.And<GXDeviceTemplateGroup>(q => q.Removed == null);
            }
            else if (removed != DateTimeOffset.MinValue)
            {
                args.Where.And<GXDeviceTemplateGroup>(q => q.Removed >= removed);
            }
            args.Where.And<GXDeviceTemplateGroup>(q => GXSql.Exists<GXDeviceTemplateGroup, GXUserGroupDeviceTemplateGroup>(j => j.Id, j => j.DeviceTemplateGroupId, map2));
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXDeviceTemplateGroup>(q => q.Id == groupId);
            }
            if (exclude?.Any() == true)
            {
                args.Where.And<GXDeviceTemplateGroup>(q => !exclude.Contains(q.Id));
            }
            if (include?.Any() == true)
            {
                args.Where.And<GXDeviceTemplateGroup>(q => include.Contains(q.Id));
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the device template group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupId">DeviceTemplate group id.</param>
        /// <returns>List of users who can access the device template group.</returns>
        public static GXSelectArgs GetUsersByDeviceTemplateGroup(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXDeviceTemplateGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupId != null && groupId != Guid.Empty)
            {
                ag.Where.And<GXDeviceTemplateGroup>(q => q.Id == groupId);
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupDeviceTemplateGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupDeviceTemplateGroup>(q => GXSql.Exists<GXDeviceTemplateGroup, GXUserGroupDeviceTemplateGroup>(j => j.Id, j => j.DeviceTemplateGroupId, ag));
            args.Where.And<GXUserGroupDeviceTemplateGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the device template group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupIds">DeviceTemplate group ids.</param>
        /// <returns>List of users who can access the device template group.</returns>
        public static GXSelectArgs GetUsersByDeviceTemplateGroups(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Distinct = true;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXDeviceTemplateGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupIds != null && groupIds.Any())
            {
                ag.Where.And<GXDeviceTemplateGroup>(q => groupIds.Contains(q.Id));
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupDeviceTemplateGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupDeviceTemplateGroup>(q => GXSql.Exists<GXDeviceTemplateGroup, GXUserGroupDeviceTemplateGroup>(j => j.Id, j => j.DeviceTemplateGroupId, ag));
            args.Where.And<GXUserGroupDeviceTemplateGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Get device templates that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="id">Device template Id.</param>
        /// <param name="removed">Remove time.</param>
        /// <param name="exclude">User group Ids to exclude.</param>
        /// <param name="include">User group Ids to include.</param>
        /// <returns>List of device templates that user can access.</returns>
        public static GXSelectArgs GetDeviceTemplatesByUser(
            Expression<Func<GXDeviceTemplate, object>> columns,
            string userId,
            Guid? id = null,
            DateTimeOffset? removed = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs groups = GetDeviceTemplateGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXDeviceTemplateGroupDeviceTemplate>(s => GXSql.One, q => GXSql.Exists<GXDeviceTemplateGroup, GXDeviceTemplateGroupDeviceTemplate>(j => j.Id, j => j.DeviceTemplateGroupId, groups));
            GXSelectArgs args = GXSelectArgs.Select(columns);
            if (removed == null)
            {
                args.Where.And<GXDeviceTemplate>(q => q.Removed == null);
            }
            else if (removed != DateTimeOffset.MinValue)
            {
                args.Where.And<GXDeviceTemplate>(q => q.Removed >= removed);
            }
            args.Where.And<GXDeviceTemplate>(q => GXSql.Exists<GXDeviceTemplate, GXDeviceTemplateGroupDeviceTemplate>(j => j.Id, j => j.DeviceTemplateId, map2));
            if (id != null && id != Guid.Empty)
            {
                args.Where.And<GXDeviceTemplate>(q => q.Id == id);
            }
            if (exclude?.Any() == true)
            {
                args.Where.And<GXDeviceTemplate>(q => !exclude.Contains(q.Id));
            }
            if (include?.Any() == true)
            {
                args.Where.And<GXDeviceTemplate>(q => include.Contains(q.Id));
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the device template.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="id">Device template id.</param>
        /// <returns>List of users who can access the device template.</returns>
        public static GXSelectArgs GetUsersByDeviceTemplate(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? id)
        {
            _ = userId;
            GXSelectArgs groups = GXSelectArgs.Select<GXDeviceTemplateGroup>(s => GXSql.One, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXDeviceTemplateGroupDeviceTemplate>(s => GXSql.One);
            if (id != null && id != Guid.Empty)
            {
                map1.Where.And<GXDeviceTemplateGroupDeviceTemplate>(w => w.DeviceTemplateId == id);
            }
            groups.Where.And<GXDeviceTemplateGroupDeviceTemplate>(q => GXSql.Exists<GXDeviceTemplateGroup, GXDeviceTemplateGroupDeviceTemplate>(j => j.Id, j => j.DeviceTemplateGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupDeviceTemplateGroup>(s => GXSql.One, q => GXSql.Exists<GXDeviceTemplateGroup, GXUserGroupDeviceTemplateGroup>(j => j.Id, j => j.DeviceTemplateGroupId, groups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupDeviceTemplateGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Returns a collection of users that can access the device templates.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="ids">Device template ids.</param>
        /// <returns>List of users who can access the device template.</returns>
        public static GXSelectArgs GetUsersByDeviceTemplates(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? ids)
        {
            _ = userId;
            GXSelectArgs groups = GXSelectArgs.Select<GXDeviceTemplateGroup>(s => GXSql.One, w => w.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXDeviceTemplateGroupDeviceTemplate>(s => GXSql.One);
            if (ids != null && ids.Any())
            {
                map1.Where.And<GXDeviceTemplateGroupDeviceTemplate>(w => ids.Contains(w.DeviceTemplateId));
            }
            groups.Where.And<GXDeviceTemplateGroupDeviceTemplate>(q => GXSql.Exists<GXDeviceTemplateGroup, GXDeviceTemplateGroupDeviceTemplate>(j => j.Id, j => j.DeviceTemplateGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupDeviceTemplateGroup>(s => GXSql.One, q => GXSql.Exists<GXDeviceTemplateGroup, GXUserGroupDeviceTemplateGroup>(j => j.Id, j => j.DeviceTemplateGroupId, groups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupDeviceTemplateGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Get gateway groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">Gateway group id.</param>
        /// <param name="removed">Remove time.</param>
        /// <param name="exclude">Gateway group Ids to exclude.</param>
        /// <param name="include">Gateway group Ids to include.</param>
        /// <returns>List of Gateway groups that user can access.</returns>
        public static GXSelectArgs GetGatewayGroupsByUser(
            Expression<Func<GXGatewayGroup, object>> columns,
            string userId,
            Guid? groupId = null,
            DateTimeOffset? removed = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupGatewayGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupGatewayGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns);
            if (removed == null)
            {
                args.Where.And<GXGatewayGroup>(q => q.Removed == null);
            }
            else if (removed != DateTimeOffset.MinValue)
            {
                args.Where.And<GXGatewayGroup>(q => q.Removed >= removed);
            }
            args.Where.And<GXGatewayGroup>(q => GXSql.Exists<GXGatewayGroup, GXUserGroupGatewayGroup>(j => j.Id, j => j.GatewayGroupId, map2));
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXGatewayGroup>(q => q.Id == groupId);
            }
            if (exclude?.Any() == true)
            {
                args.Where.And<GXGatewayGroup>(q => !exclude.Contains(q.Id));
            }
            if (include?.Any() == true)
            {
                args.Where.And<GXGatewayGroup>(q => include.Contains(q.Id));
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the gateway group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupId">Gateway group id.</param>
        /// <returns>List of users who can access the gateway group.</returns>
        public static GXSelectArgs GetUsersByGatewayGroup(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXGatewayGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupId != null && groupId != Guid.Empty)
            {
                ag.Where.And<GXGatewayGroup>(q => q.Id == groupId);
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupGatewayGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupGatewayGroup>(q => GXSql.Exists<GXGatewayGroup, GXUserGroupGatewayGroup>(j => j.Id, j => j.GatewayGroupId, ag));
            args.Where.And<GXUserGroupGatewayGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the gateway group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupIds">Gateway group ids.</param>
        /// <returns>List of users who can access the gateway group.</returns>
        public static GXSelectArgs GetUsersByGatewayGroups(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Distinct = true;
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(q => q.Id == userId);
            }
            GXSelectArgs ag = GXSelectArgs.Select<GXGatewayGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupIds != null && groupIds.Any())
            {
                ag.Where.And<GXGatewayGroup>(q => groupIds.Contains(q.Id));
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupGatewayGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupGatewayGroup>(q => GXSql.Exists<GXGatewayGroup, GXUserGroupGatewayGroup>(j => j.Id, j => j.GatewayGroupId, ag));
            args.Where.And<GXUserGroupGatewayGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Get gateways that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="gatewayId">Gateway Id.</param>
        /// <returns>List of gateways that user can access.</returns>
        public static GXSelectArgs GetGatewaysByUser(
            Expression<Func<GXGateway, object>> columns,
            string userId,
            Guid? gatewayId = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs groups = GetGatewayGroupsByUser(s => GXSql.One, userId, gatewayId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXGatewayGroupGateway>(s => GXSql.One, q => GXSql.Exists<GXGatewayGroup, GXGatewayGroupGateway>(j => j.Id, j => j.GatewayGroupId, groups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXGateway>(q => GXSql.Exists<GXGateway, GXGatewayGroupGateway>(j => j.Id, j => j.GatewayId, map2));
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the gateway.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="id">Gateway id.</param>
        /// <returns>List of users who can access the gateway.</returns>
        public static GXSelectArgs GetUsersByGateway(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? id)
        {
            _ = userId;
            GXSelectArgs gatewayGroups = GXSelectArgs.Select<GXGatewayGroup>(s => GXSql.One, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXGatewayGroupGateway>(s => GXSql.One);
            if (id != null && id != Guid.Empty)
            {
                map1.Where.And<GXGatewayGroupGateway>(w => w.GatewayId == id);
            }
            gatewayGroups.Where.And<GXGatewayGroupGateway>(q => GXSql.Exists<GXGatewayGroup, GXGatewayGroupGateway>(j => j.Id, j => j.GatewayGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupGatewayGroup>(s => GXSql.One, q => GXSql.Exists<GXGatewayGroup, GXUserGroupGatewayGroup>(j => j.Id, j => j.GatewayGroupId, gatewayGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupGatewayGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Returns a collection of users that can access the gateways.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="ids">Gateway ids.</param>
        /// <returns>List of users who can access the gateway.</returns>
        public static GXSelectArgs GetUsersByGateways(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? ids)
        {
            _ = userId;
            GXSelectArgs gatewayGroups = GXSelectArgs.Select<GXGatewayGroup>(s => GXSql.One, w => w.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXGatewayGroupGateway>(s => GXSql.One);
            if (ids != null && ids.Any())
            {
                map1.Where.And<GXGatewayGroupGateway>(w => ids.Contains(w.GatewayId));
            }
            gatewayGroups.Where.And<GXGatewayGroupGateway>(q => GXSql.Exists<GXGatewayGroup, GXGatewayGroupGateway>(j => j.Id, j => j.GatewayGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupGatewayGroup>(s => GXSql.One, q => GXSql.Exists<GXGatewayGroup, GXUserGroupGatewayGroup>(j => j.Id, j => j.GatewayGroupId, gatewayGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupGatewayGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id!, j => j.UserId!, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }



        /// <summary>
        /// Get notifications that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="notificationId">Notification Id.</param>
        /// <returns>List of notifications that user can access.</returns>
        public static GXSelectArgs GetNotificationsByUser(
            Expression<Func<GXNotification, object>> columns,
            string? userId,
            Guid? notificationId = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Joins.AddInnerJoin<GXNotification, GXUser>(j => j.Creator!, j => j.Id!);
            if (!string.IsNullOrEmpty(userId))
            {
                args.Where.And<GXUser>(w => w.Id == userId);
            }
            if (notificationId != null && notificationId != Guid.Empty)
            {
                args.Where.And<GXNotification>(w => w.Id == notificationId);
            }
            return args;
        }

        /// <summary>
        /// Get tasks that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="agentIds">Agent Ids.</param>
        /// <param name="deviceIds">Agent Ids.</param>
        /// <param name="Ids">Gateway Ids.</param>
        /// <returns>List of tasks that user can access.</returns>
        public static GXSelectArgs GetTasksByUser(
            Expression<Func<GXTask, object>> columns,
            string userId,
            IEnumerable<Guid>? agentIds = null,
            IEnumerable<Guid>? deviceIds = null,
            IEnumerable<Guid>? Ids = null)
        {
            _ = deviceIds;
            _ = Ids;
            _ = agentIds;
            var user = new GXUser() { Id = userId };
            GXSelectArgs args = GXSelectArgs.Select(columns, w => w.Creator == user);
            args.OrderBy.Add<GXTask>(q => q.Start!);
            return args;
        }

        /// <summary>
        /// Get all pending tasks for the oldest eligible device that has no running tasks.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="agentId">Agent Id.</param>
        /// <param name="deviceId">Restrict the batch to this device.</param>
        /// <param name="gatewayId">Gateway Id.</param>
        /// <param name="all">Select accessible devices without agent or gateway mappings instead of mapped devices.</param>
        /// <param name="listener">Restrict generic listener polls to dynamic devices.</param>
        /// <returns>A query selecting one device's pending task batch in creation order.</returns>
        public static GXSelectArgs GetNextTasks(
            Expression<Func<GXTask, object>> columns,
            string userId,
            Guid agentId,
            Guid? deviceId = null,
            Guid? gatewayId = null,
            bool all = false,
            bool listener = false)
        {
            // Choose the oldest eligible meter, then return its entire pending batch.
            GXSelectArgs nextDevice = GXSelectArgs.Select<GXTask>(t => t.TargetDevice!,
                t => t.Start == null && t.Ready == null);
            nextDevice.Joins.AddInnerJoin<GXTask, GXDevice>(t => t.TargetDevice!, d => d.Id);
            nextDevice.Where.And<GXDevice>(d => d.Removed == null);
            var currentAgent = new GXAgent { Id = agentId };
            nextDevice.Where.And<GXTask>(t => t.OperatingAgent == null || t.OperatingAgent == currentAgent);
            var running = GXSelectArgs.Select<GXTask>(t => GXSql.One, t => t.Start != null && t.Ready == null);
            nextDevice.Where.And<GXDevice>(d => !GXSql.Exists<GXDevice, GXTask>(d => d.Id, t => t.TargetDevice!, running));
            if (deviceId != null && deviceId != Guid.Empty)
            {
                nextDevice.Where.And<GXTask>(t => t.TargetDevice == deviceId.Value);
            }
            if (listener && (deviceId == null || deviceId == Guid.Empty) && (gatewayId == null || gatewayId == Guid.Empty))
            {
                nextDevice.Where.And<GXDevice>(d => d.Dynamic == true);
            }

            // TargetAgent and TargetGateway are notification-only properties. Use persisted mappings.
            GXSelectArgs agentGroups = all
                ? GXSelectArgs.Select<GXAgentGroup>(g => GXSql.One, g => g.Removed == null)
                : GetAgentGroupsByAgent(g => GXSql.One, agentId);
            var agentMap = GXSelectArgs.Select<GXAgentGroupDeviceGroup>(m => GXSql.One,
                m => GXSql.Exists<GXAgentGroup, GXAgentGroupDeviceGroup>(g => g.Id, m => m.AgentGroupId, agentGroups));
            var agentDeviceGroups = GXSelectArgs.Select<GXDeviceGroup>(g => GXSql.One, g => g.Removed == null);
            agentDeviceGroups.Where.And<GXDeviceGroup>(g => GXSql.Exists<GXDeviceGroup, GXAgentGroupDeviceGroup>(g => g.Id, m => m.DeviceGroupId, agentMap));
            var agentDevices = GXSelectArgs.Select<GXDeviceGroupDevice>(m => GXSql.One,
                m => GXSql.Exists<GXDeviceGroup, GXDeviceGroupDevice>(g => g.Id, m => m.DeviceGroupId, agentDeviceGroups));

            var gateways = GXSelectArgs.Select<GXGateway>(g => GXSql.One, g => g.Removed == null && g.Enabled == true);
            if (!all)
            {
                gateways.Where.And<GXGateway>(g => g.Agent == currentAgent);
            }
            if (!all && gatewayId != null && gatewayId != Guid.Empty)
            {
                gateways.Where.And<GXGateway>(g => g.Id == gatewayId.Value);
            }
            var gatewayDevices = GXSelectArgs.Select<GXGatewayDevice>(m => GXSql.One,
                m => GXSql.Exists<GXGateway, GXGatewayDevice>(g => g.Id, m => m.GatewayId, gateways));
            var gatewayGroups = GXSelectArgs.Select<GXGatewayDeviceGroup>(m => GXSql.One,
                m => GXSql.Exists<GXGateway, GXGatewayDeviceGroup>(g => g.Id, m => m.GatewayId, gateways));
            var gatewayDeviceGroups = GXSelectArgs.Select<GXDeviceGroup>(g => GXSql.One, g => g.Removed == null);
            gatewayDeviceGroups.Where.And<GXDeviceGroup>(g => GXSql.Exists<GXDeviceGroup, GXGatewayDeviceGroup>(g => g.Id, m => m.DeviceGroupId, gatewayGroups));
            var gatewayGroupDevices = GXSelectArgs.Select<GXDeviceGroupDevice>(m => GXSql.One,
                m => GXSql.Exists<GXDeviceGroup, GXDeviceGroupDevice>(g => g.Id, m => m.DeviceGroupId, gatewayDeviceGroups));

            if (all)
            {
                var accessibleGroups = GetDeviceGroupsByUser(g => GXSql.One, userId);
                var accessibleDevices = GXSelectArgs.Select<GXDeviceGroupDevice>(m => GXSql.One,
                    m => GXSql.Exists<GXDeviceGroup, GXDeviceGroupDevice>(g => g.Id, m => m.DeviceGroupId, accessibleGroups));
                nextDevice.Where.And<GXDevice>(d => GXSql.Exists<GXDevice, GXDeviceGroupDevice>(d => d.Id, m => m.DeviceId, accessibleDevices));
                nextDevice.Where.And<GXDevice>(d =>
                    !GXSql.Exists<GXDevice, GXDeviceGroupDevice>(d => d.Id, m => m.DeviceId, agentDevices)
                    && !GXSql.Exists<GXDevice, GXGatewayDevice>(d => d.Id, m => m.DeviceId, gatewayDevices)
                    && !GXSql.Exists<GXDevice, GXDeviceGroupDevice>(d => d.Id, m => m.DeviceId, gatewayGroupDevices));
            }
            else if (gatewayId != null && gatewayId != Guid.Empty)
            {
                nextDevice.Where.And<GXDevice>(d =>
                    GXSql.Exists<GXDevice, GXGatewayDevice>(d => d.Id, m => m.DeviceId, gatewayDevices)
                    || GXSql.Exists<GXDevice, GXDeviceGroupDevice>(d => d.Id, m => m.DeviceId, gatewayGroupDevices));
            }
            else
            {
                nextDevice.Where.And<GXDevice>(d =>
                    GXSql.Exists<GXDevice, GXDeviceGroupDevice>(d => d.Id, m => m.DeviceId, agentDevices)
                    || GXSql.Exists<GXDevice, GXGatewayDevice>(d => d.Id, m => m.DeviceId, gatewayDevices)
                    || GXSql.Exists<GXDevice, GXDeviceGroupDevice>(d => d.Id, m => m.DeviceId, gatewayGroupDevices));
            }
            nextDevice.OrderBy.Add<GXTask>(t => t.CreationTime!);
            nextDevice.OrderBy.Add<GXTask>(t => t.Id);
            nextDevice.Count = 1;

            var args = GXSelectArgs.Select(columns);
            args.Joins.Append(nextDevice.Joins);
            args.Where.Append(nextDevice.Where);
            args.Where.And<GXTask>(t => t.TargetDevice == GXSql.Scalar<Guid?>(nextDevice));
            args.OrderBy.Add<GXTask>(t => t.CreationTime!);
            args.OrderBy.Add<GXTask>(t => t.Id);
            return args;
        }
        public static GXSelectArgs GetDefaultRoles(Expression<Func<GXRole, object>> columns)
        {
            return GXSelectArgs.Select(columns, w => w.Removed == null && w.Default == true);
        }
    }
}


