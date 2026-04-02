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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Content;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Report;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm;
using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs.Subtotal;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.DLMS.AMI.Shared.DTOs.ContentType;
using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using System.Linq.Expressions;

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
        /// <param name="exclude">User group Ids to exclude.</param>
        /// <param name="include">User group Ids to include.</param>
        /// <returns>List of user groups where user belongs.</returns>
        public static GXSelectArgs GetUserGroupsByUser(
            Expression<Func<GXUserGroup, object>> columns,
            string userId,
            Guid? id = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
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
            args.Where.And<GXUserGroupUser>(q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, sub));
            return args;
        }

        /// <summary>
        /// Returns a collection of users who belong user groups.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="groupIds">User group IDs.</param>
        public static GXSelectArgs GetUsersByUserGroups(string? userId, IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => GXSql.One, q => q.Removed == null);
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
            args.Where.And<GXUserGroupUser>(q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, sub));
            return args;
        }

        /// <summary>
        /// Get users that belong for the same group(s) with the user.
        /// </summary>
        /// <param name="columns">Selected columns from the user table.</param>
        /// <param name="userId">User Id.</param>
        /// <returns>List of users that user can access.</returns>
        public static GXSelectArgs GetUsersByUser(
            Expression<Func<GXUser, object>> columns,
            string userId)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Id == userId && q.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => s.Id, userId);
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One);
            sub.Where.And<GXUserGroupUser>(q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            args.Where.And<GXUserGroupUser>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Get users errors by the user.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <returns>User errors by the user.</returns>
        public static GXSelectArgs GetUserErrorsByUser(string userId)
        {
            GXSelectArgs args = GXSelectArgs.SelectAll<GXUserError>();
            args.Distinct = true;
            args.Where.And<GXUser>(q => q.Removed == null && q.Id == userId);
            args.Joins.AddInnerJoin<GXUserError, GXUser>(j => j.User, j => j.Id);
            return args;
        }

        /// <summary>
        /// Get users actions by the user.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <returns>User actions by the user.</returns>
        public static GXSelectArgs GetUserActionsByUser(string userId)
        {
            GXSelectArgs args = GXSelectArgs.SelectAll<GXUserAction>();
            args.Distinct = true;
            args.Where.And<GXUser>(q => q.Id == userId && q.Removed == null);
            args.Joins.AddInnerJoin<GXUserAction, GXUser>(j => j.User, j => j.Id);
            return args;
        }

        /// <summary>
        /// Get device groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="id">Device group id.</param>
        /// <returns>List of device groups that user can access.</returns>
        public static GXSelectArgs GetDeviceGroupsByUser(
            Expression<Func<GXDeviceGroup, object>> columns,
            string userId,
            Guid? id = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId, id);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupDeviceGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupDeviceGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXDeviceGroup>(q => GXSql.Exists<GXDeviceGroup, GXUserGroupDeviceGroup>(j => j.Id, j => j.DeviceGroupId, map2));
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
            GXSelectArgs deviceGroups = GXSelectArgs.Select<GXDeviceGroup>(s => GXSql.One, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXDeviceGroupDevice>(s => GXSql.One, w => w.DeviceId == deviceId);
            deviceGroups.Where.And<GXDeviceGroupDevice>(q => GXSql.Exists<GXDeviceGroup, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupDeviceGroup>(s => GXSql.One, q => GXSql.Exists<GXDeviceGroup, GXUserGroupDeviceGroup>(j => j.Id, j => j.DeviceGroupId, deviceGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupDeviceGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
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
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
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
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
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
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
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
            GXSelectArgs deviceGroups = GXSelectArgs.Select<GXDeviceGroup>(s => GXSql.One, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXDeviceGroupDevice>(s => GXSql.One, w => w.DeviceId == deviceId);
            deviceGroups.Where.And<GXDeviceGroupDevice>(q => GXSql.Exists<GXDeviceGroup, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupDeviceGroup>(s => GXSql.One, q => GXSql.Exists<GXDeviceGroup, GXUserGroupDeviceGroup>(j => j.Id, j => j.DeviceGroupId, deviceGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupDeviceGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
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
            args.Joins.AddInnerJoin<GXDevice, GXObject>(j => j.Id, j => j.Device);
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
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceTemplateGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupDeviceTemplateGroup, GXDeviceTemplateGroup>(a => a.DeviceTemplateGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXDeviceTemplateGroup, GXDeviceTemplateGroupDeviceTemplate>(a => a.Id, b => b.DeviceTemplateId);
            args.Joins.AddInnerJoin<GXDeviceTemplateGroupDeviceTemplate, GXDeviceTemplate>(j => j.DeviceTemplateId, j => j.Id);
            args.Joins.AddInnerJoin<GXDeviceTemplate, GXObjectTemplate>(a => a.Id, b => b.DeviceTemplate);
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
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceTemplateGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupDeviceTemplateGroup, GXDeviceTemplateGroup>(a => a.DeviceTemplateGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXDeviceTemplateGroup, GXDeviceTemplateGroupDeviceTemplate>(a => a.Id, b => b.DeviceTemplateId);
            args.Joins.AddInnerJoin<GXDeviceTemplateGroupDeviceTemplate, GXDeviceTemplate>(a => a.DeviceTemplateId, b => b.Id);
            args.Joins.AddInnerJoin<GXDeviceTemplate, GXObjectTemplate>(a => a.Id, b => b.DeviceTemplate);
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
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupDeviceGroup, GXDeviceGroup>(a => a.DeviceGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXDeviceGroup, GXDeviceGroupDevice>(a => a.Id, b => b.DeviceId);
            args.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDevice>(j => j.DeviceId, j => j.Id);
            args.Joins.AddInnerJoin<GXDevice, GXObject>(a => a.Id, b => b.Device);
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
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupDeviceGroup, GXDeviceGroup>(a => a.DeviceGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXDeviceGroup, GXDeviceGroupDevice>(a => a.Id, b => b.DeviceGroupId);
            args.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDevice>(a => a.DeviceId, b => b.Id);
            args.Joins.AddInnerJoin<GXDevice, GXObject>(a => a.Id, b => b.Device);
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
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceTemplateGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupDeviceTemplateGroup, GXDeviceTemplateGroup>(a => a.DeviceTemplateGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXDeviceTemplateGroup, GXDeviceTemplateGroupDeviceTemplate>(a => a.Id, b => b.DeviceTemplateGroupId);
            args.Joins.AddInnerJoin<GXDeviceTemplateGroupDeviceTemplate, GXDeviceTemplate>(a => a.DeviceTemplateId, b => b.Id);
            args.Joins.AddInnerJoin<GXDeviceTemplate, GXObjectTemplate>(a => a.Id, b => b.DeviceTemplate);
            args.Joins.AddInnerJoin<GXObjectTemplate, GXAttributeTemplate>(a => a.Id, b => b.ObjectTemplate);
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
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceTemplateGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupDeviceTemplateGroup, GXDeviceTemplateGroup>(a => a.DeviceTemplateGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXDeviceTemplateGroup, GXDeviceTemplateGroupDeviceTemplate>(a => a.Id, b => b.DeviceTemplateGroupId);
            args.Joins.AddInnerJoin<GXDeviceTemplateGroupDeviceTemplate, GXDeviceTemplate>(a => a.DeviceTemplateId, b => b.Id);
            args.Joins.AddInnerJoin<GXDeviceTemplate, GXObjectTemplate>(a => a.Id, b => b.DeviceTemplate);
            args.Joins.AddInnerJoin<GXObjectTemplate, GXAttributeTemplate>(a => a.Id, b => b.ObjectTemplate);
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
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupDeviceGroup, GXDeviceGroup>(a => a.DeviceGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXDeviceGroup, GXDeviceGroupDevice>(a => a.Id, b => b.DeviceGroupId);
            args.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDevice>(a => a.DeviceId, b => b.Id);
            args.Joins.AddInnerJoin<GXDevice, GXObject>(a => a.Id, b => b.Device);
            args.Joins.AddInnerJoin<GXObject, GXAttribute>(a => a.Id, b => b.Object);
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
            args.Joins.AddInnerJoin<GXAttribute, GXObject>(j => j.Object, j => j.Id);
            args.Joins.AddInnerJoin<GXObject, GXDevice>(j => j.Device, j => j.Id);
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
            args.Columns.Add<GXUser>(s => s.Id);
            args.Joins.AddInnerJoin<GXUserGroupDeviceGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(j => j.UserId, j => j.Id);
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
            Expression<Func<GXDeviceError, object>> columns,
            string userId,
            Guid? deviceId = null)
        {
            GXSelectArgs devs = GetDevicesByUser(s => s.Id, userId, deviceId);
            GXSelectArgs args = GXSelectArgs.Select(columns);
            args.Joins.AddInnerJoin<GXDeviceError, GXDevice>(j => j.Device, j => j.Id);
            args.Where.And<GXDeviceError>(q => GXSql.Exists<GXDeviceError, GXDevice>(j => j.Device, j => j.Id, devs));
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
            args.Joins.AddInnerJoin<GXDeviceTrace, GXDevice>(j => j.Device, j => j.Id);
            args.Where.And<GXDeviceTrace>(q => GXSql.Exists<GXDeviceTrace, GXDevice>(j => j.Device, j => j.Id, devs));
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
            Expression<Func<GXDevice, object>> columns,
            string userId,
            Guid? deviceId = null)
        {
            GXSelectArgs args = GetDevicesByUser(s => s.Id, userId, deviceId);
            //Get only device action columns.
            args.Columns.Clear();
            args.Columns.Add<GXDeviceAction>();
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXDevice, GXDeviceAction>(j => j.Id, j => j.Device);
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
            args.Joins.AddInnerJoin<GXObject, GXDevice>(j => j.Device, j => j.Id);
            if (objectId != null)
            {
                args.Where.And<GXObject>(q => q.Id == objectId);
            }
            args.Where.And<GXObject>(q => q.Removed == null);
            args.Where.And<GXObject>(q => GXSql.Exists<GXObject, GXDevice>(j => j.Device, j => j.Id, devs));
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
            args.Joins.AddInnerJoin<GXDeviceTemplate, GXObjectTemplate>(j => j.Id, j => j.DeviceTemplate);
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
            GXSelectArgs args = GetDeviceTemplatesByUser(s => s.Id, userId);
            args.Columns.Clear();
            args.Columns.Add<GXAttributeTemplate>();
            args.Joins.AddInnerJoin<GXDeviceTemplate, GXObjectTemplate>(j => j.Id, j => j.DeviceTemplate);
            args.Joins.AddInnerJoin<GXObjectTemplate, GXAttributeTemplate>(j => j.Id, j => j.ObjectTemplate);
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
            args.Joins.AddInnerJoin<GXDevice, GXObject>(j => j.Id, j => j.Device);
            args.Joins.AddInnerJoin<GXObject, GXAttribute>(j => j.Id, j => j.Object);
            if (attributeId != null)
            {
                args.Where.And<GXAttribute>(q => q.Id == attributeId);
            }
            args.Where.And<GXAttribute>(q => q.Removed == null);
            args.Where.And<GXAttribute>(q => GXSql.Exists<GXObject, GXDevice>(j => j.Device, j => j.Id, devs));
            return args;
        }

        /// <summary>
        /// Get trigger groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">Trigger group id.</param>
        /// <returns>List of trigger groups that user can access.</returns>
        public static GXSelectArgs GetTriggerGroupsByUser(
            Expression<Func<GXTriggerGroup, object>> columns,
            string userId,
            Guid? groupId = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupTriggerGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupTriggerGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXTriggerGroup>(q => GXSql.Exists<GXTriggerGroup, GXUserGroupTriggerGroup>(j => j.Id, j => j.TriggerGroupId, map2));
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXTriggerGroup>(q => q.Id == groupId);
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
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Returns a collection of users that can access the triggers.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="triggerIds">Trigger ids.</param>
        /// <returns>List of users who can access the trigger.</returns>
        public static GXSelectArgs GetUsersByTriggers(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? triggerIds)
        {
            GXSelectArgs triggerGroups = GXSelectArgs.Select<GXTriggerGroup>(s => GXSql.One, w => w.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXTriggerGroupTrigger>(s => GXSql.One);
            if (triggerIds != null && triggerIds.Any())
            {
                map1.Where.And<GXTriggerGroupTrigger>(w => triggerIds.Contains(w.TriggerId));
            }
            triggerGroups.Where.And<GXTriggerGroupTrigger>(q => GXSql.Exists<GXTriggerGroup, GXTriggerGroupTrigger>(j => j.Id, j => j.TriggerGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupTriggerGroup>(s => GXSql.One, q => GXSql.Exists<GXTriggerGroup, GXUserGroupTriggerGroup>(j => j.Id, j => j.TriggerGroupId, triggerGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupTriggerGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Get workflow groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">Workflow group id.</param>
        /// <returns>List of workflow groups that user can access.</returns>
        public static GXSelectArgs GetWorkflowGroupsByUser(
            Expression<Func<GXWorkflowGroup, object>> columns,
            string userId,
            Guid? groupId = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupWorkflowGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupWorkflowGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXWorkflowGroup>(q => GXSql.Exists<GXWorkflowGroup, GXUserGroupWorkflowGroup>(j => j.Id, j => j.WorkflowGroupId, map2));
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXWorkflowGroup>(q => q.Id == groupId);
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
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Returns a collection of users that can access the workflows.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="workflowIds">Workflow ids.</param>
        /// <returns>List of users who can access the workflow.</returns>
        public static GXSelectArgs GetUsersByWorkflows(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? workflowIds)
        {
            GXSelectArgs workflowGroups = GXSelectArgs.Select<GXWorkflowGroup>(s => GXSql.One, w => w.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXWorkflowGroupWorkflow>(s => GXSql.One);
            if (workflowIds != null && workflowIds.Any())
            {
                map1.Where.And<GXWorkflowGroupWorkflow>(w => workflowIds.Contains(w.WorkflowId));
            }
            workflowGroups.Where.And<GXWorkflowGroupWorkflow>(q => GXSql.Exists<GXWorkflowGroup, GXWorkflowGroupWorkflow>(j => j.Id, j => j.WorkflowGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupWorkflowGroup>(s => GXSql.One, q => GXSql.Exists<GXWorkflowGroup, GXUserGroupWorkflowGroup>(j => j.Id, j => j.WorkflowGroupId, workflowGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupWorkflowGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Get workflow logs that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param> 
        /// <param name="userId">User Id.</param>
        /// <param name="id">Workflow Id.</param>
        /// <returns>List of workflows that user can access.</returns>
        public static GXSelectArgs GetWorkflowLogsByUser(
            Expression<Func<GXWorkflow, object>> columns,
            string userId,
            Guid? id = null)
        {
            GXSelectArgs args = GetWorkflowsByUser(s => s.Id, userId);
            args.Columns.Clear();
            args.Columns.Add<GXWorkflowLog>();
            args.Joins.AddInnerJoin<GXWorkflow, GXWorkflowLog>(j => j.Id, j => j.Workflow);
            if (id != null)
            {
                args.Where.And<GXWorkflowLog>(q => q.Id == id);
            }
            return args;
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
        /// <returns>List of agent groups that user can access.</returns>
        public static GXSelectArgs GetAgentGroupsByUser(
            Expression<Func<GXAgentGroup, object>> columns,
            string userId,
            Guid? groupId = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupAgentGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupAgentGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXAgentGroup>(q => GXSql.Exists<GXAgentGroup, GXUserGroupAgentGroup>(j => j.Id, j => j.AgentGroupId, map2));
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXAgentGroup>(q => q.Id == groupId);
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
        /// <returns>List of agents that user can access.</returns>
        public static GXSelectArgs GetAgentsByUser(
            Expression<Func<GXAgent, object>> columns,
            string userId,
            Guid? agentId = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs groups = GetAgentGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXAgentGroupAgent>(s => GXSql.One, q => GXSql.Exists<GXAgentGroup, GXAgentGroupAgent>(j => j.Id, j => j.AgentGroupId, groups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXAgent>(q => GXSql.Exists<GXAgent, GXAgentGroupAgent>(j => j.Id, j => j.AgentId, map2));
            if (agentId != null && agentId != Guid.Empty)
            {
                args.Where.And<GXAgent>(q => q.Id == agentId);
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
            GXSelectArgs agentGroups = GXSelectArgs.Select<GXAgentGroup>(s => GXSql.One, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXAgentGroupAgent>(s => GXSql.One, w => w.AgentId == agentId);
            agentGroups.Where.And<GXAgentGroupAgent>(q => GXSql.Exists<GXAgentGroup, GXAgentGroupAgent>(j => j.Id, j => j.AgentGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupAgentGroup>(s => GXSql.One, q => GXSql.Exists<GXAgentGroup, GXUserGroupAgentGroup>(j => j.Id, j => j.AgentGroupId, agentGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupAgentGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
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
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Get agent errors that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="agentId">Agent Id.</param>
        /// <returns>List of agents that user can access.</returns>
        public static GXSelectArgs GetAgentErrorsByUser(
            Expression<Func<GXUserGroup, object>> columns,
            string userId,
            Guid? agentId = null)
        {
            GXSelectArgs args = GetAgentsByUser(s => GXSql.One, userId, agentId);
            args.Columns.Clear();
            args.Columns.Add<GXAgentLog>();
            args.Joins.AddInnerJoin<GXAgent, GXAgentLog>(j => j.Id, j => j.Agent);
            return args;
        }
        /// <summary>
        /// Get subtotal errors that user can access.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="subtotalId">Subtotal Id.</param>
        /// <returns>List of subtotals that user can access.</returns>
        public static GXSelectArgs GetSubtotalErrorsByUser(
            string userId,
            Guid? subtotalId = null)
        {
            GXSelectArgs args = GetSubtotalsByUser(s => s.Id, userId, subtotalId);
            args.Columns.Clear();
            args.Columns.Add<GXSubtotalLog>();
            args.Joins.AddInnerJoin<GXSubtotal, GXSubtotalLog>(j => j.Id, j => j.Subtotal);
            return args;
        }


        /// <summary>
        /// Get report logs that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param> 
        /// <param name="userId">User Id.</param>
        /// <param name="reportId">Report Id.</param>
        /// <returns>List of reports that user can access.</returns>
        public static GXSelectArgs GetReportErrorsByUser(
            Expression<Func<GXDevice, object>> columns,
            string userId,
            Guid? reportId = null)
        {
            GXSelectArgs args = GetReportsByUser(s => s.Id, userId, reportId);
            args.Columns.Clear();
            args.Columns.Add<GXReportLog>();
            args.Joins.AddInnerJoin<GXReport, GXReportLog>(j => j.Id, j => j.Report);
            return args;
        }

        /// <summary>
        /// Get notification logs that user can access.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="notificationId">Notification Id.</param>
        /// <returns>List of notifications that user can access.</returns>
        public static GXSelectArgs GetNotificationErrorsByUser(string userId,
            Guid? notificationId = null)
        {
            GXSelectArgs args = GetNotificationsByUser(s => s.Id, userId, notificationId);
            args.Columns.Clear();
            args.Columns.Add<GXNotificationLog>();
            args.Joins.AddInnerJoin<GXNotification, GXNotificationLog>(j => j.Id, j => j.Notification);
            return args;
        }

        /// <summary>
        /// Get module groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">Module group id.</param>
        /// <returns>List of module groups that user can access.</returns>
        public static GXSelectArgs GetModuleGroupsByUser(
            Expression<Func<GXModuleGroup, object>> columns,
            string userId,
            Guid? groupId = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupModuleGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupModuleGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXModuleGroup>(q => GXSql.Exists<GXModuleGroup, GXUserGroupModuleGroup>(j => j.Id, j => j.ModuleGroupId, map2));
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXModuleGroup>(q => q.Id == groupId);
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
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
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
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Get module logs that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param> 
        /// <param name="userId">User Id.</param>
        /// <param name="id">Module Id.</param>
        /// <returns>List of module logs that user can access.</returns>
        public static GXSelectArgs GetModuleLogsByUser(
            Expression<Func<GXWorkflow, object>> columns,
            string userId,
            string? id = null)
        {
            GXSelectArgs args = GetModulesByUser(s => s.Id, userId, id);
            args.Columns.Clear();
            args.Columns.Add<GXModuleLog>();
            args.Joins.AddInnerJoin<GXModule, GXModuleLog>(j => j.Id, j => j.Module);
            return args;
        }

        /// <summary>
        /// Get content groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">Content group id.</param>
        /// <returns>List of content groups that user can access.</returns>
        public static GXSelectArgs GetContentGroupsByUser(
            Expression<Func<GXContentGroup, object>> columns,
            string userId,
            Guid? groupId = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupContentGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupContentGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXContentGroup>(q => GXSql.Exists<GXContentGroup, GXUserGroupContentGroup>(j => j.Id, j => j.ContentGroupId, map2));
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXContentGroup>(q => q.Id == groupId);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the content group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupId">Content group id.</param>
        /// <returns>List of users who can access the content group.</returns>
        public static GXSelectArgs GetUsersByContentGroup(
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
            GXSelectArgs ag = GXSelectArgs.Select<GXContentGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupId != null && groupId != Guid.Empty)
            {
                ag.Where.And<GXContentGroup>(q => q.Id == groupId);
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupContentGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupContentGroup>(q => GXSql.Exists<GXContentGroup, GXUserGroupContentGroup>(j => j.Id, j => j.ContentGroupId, ag));
            args.Where.And<GXUserGroupContentGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the content group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupIds">Content group ids.</param>
        /// <returns>List of users who can access the content group.</returns>
        public static GXSelectArgs GetUsersByContentGroups(
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
            GXSelectArgs ag = GXSelectArgs.Select<GXContentGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupIds != null && groupIds.Any())
            {
                ag.Where.And<GXContentGroup>(q => groupIds.Contains(q.Id));
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupContentGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupContentGroup>(q => GXSql.Exists<GXContentGroup, GXUserGroupContentGroup>(j => j.Id, j => j.ContentGroupId, ag));
            args.Where.And<GXUserGroupContentGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Get contents that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="contentId">Content Id.</param>
        /// <param name="exclude">Content Ids to exclude.</param>
        /// <param name="include">Content Ids to include.</param>
        /// <returns>List of contents that user can access.</returns>
        public static GXSelectArgs GetContentsByUser(
            Expression<Func<GXContent, object>> columns,
            string userId,
            Guid? contentId = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs groups = GetContentGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXContentGroupContent>(s => GXSql.One, q => GXSql.Exists<GXContentGroup, GXContentGroupContent>(j => j.Id, j => j.ContentGroupId, groups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXContent>(q => GXSql.Exists<GXContent, GXContentGroupContent>(j => j.Id, j => j.ContentId, map2));
            if (contentId != null && contentId != Guid.Empty)
            {
                args.Where.And<GXContent>(q => q.Id == contentId);
            }
            if (exclude?.Any() == true)
            {
                args.Where.And<GXContent>(q => !exclude.Contains(q.Id));
            }
            if (include?.Any() == true)
            {
                args.Where.And<GXContent>(q => include.Contains(q.Id));
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the content.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="contentId">Content id.</param>
        /// <returns>List of users who can access the content.</returns>
        public static GXSelectArgs GetUsersByContent(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? contentId)
        {
            GXSelectArgs contentGroups = GXSelectArgs.Select<GXContentGroup>(s => GXSql.One, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXContentGroupContent>(s => GXSql.One, w => w.ContentId == contentId);
            contentGroups.Where.And<GXContentGroupContent>(q => GXSql.Exists<GXContentGroup, GXContentGroupContent>(j => j.Id, j => j.ContentGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupContentGroup>(s => GXSql.One, q => GXSql.Exists<GXContentGroup, GXUserGroupContentGroup>(j => j.Id, j => j.ContentGroupId, contentGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupContentGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Returns a collection of users that can access the contents.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="contentIds">Content ids.</param>
        /// <returns>List of users who can access the content.</returns>
        public static GXSelectArgs GetUsersByContents(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? contentIds)
        {
            GXSelectArgs contentGroups = GXSelectArgs.Select<GXContentGroup>(s => GXSql.One, w => w.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXContentGroupContent>(s => GXSql.One);
            if (contentIds?.Any() == true)
            {
                map1.Where.And<GXContentGroupContent>(w => contentIds.Contains(w.ContentId));
            }
            contentGroups.Where.And<GXContentGroupContent>(q => GXSql.Exists<GXContentGroup, GXContentGroupContent>(j => j.Id, j => j.ContentGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupContentGroup>(s => GXSql.One, q => GXSql.Exists<GXContentGroup, GXUserGroupContentGroup>(j => j.Id, j => j.ContentGroupId, contentGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupContentGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Get content type groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">ContentType group id.</param>
        /// <returns>List of content type groups that user can access.</returns>
        public static GXSelectArgs GetContentTypeGroupsByUser(
            Expression<Func<GXContentTypeGroup, object>> columns,
            string userId,
            Guid? groupId = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupContentTypeGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupContentTypeGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXContentTypeGroup>(q => GXSql.Exists<GXContentTypeGroup, GXUserGroupContentTypeGroup>(j => j.Id, j => j.ContentTypeGroupId, map2));
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXContentTypeGroup>(q => q.Id == groupId);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the content type group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupId">ContentType group id.</param>
        /// <returns>List of users who can access the content type group.</returns>
        public static GXSelectArgs GetUsersByContentTypeGroup(
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
            GXSelectArgs ag = GXSelectArgs.Select<GXContentTypeGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupId != null && groupId != Guid.Empty)
            {
                ag.Where.And<GXContentTypeGroup>(q => q.Id == groupId);
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupContentTypeGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupContentTypeGroup>(q => GXSql.Exists<GXContentTypeGroup, GXUserGroupContentTypeGroup>(j => j.Id, j => j.ContentTypeGroupId, ag));
            args.Where.And<GXUserGroupContentTypeGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the content type group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupIds">ContentType group ids.</param>
        /// <returns>List of users who can access the content type group.</returns>
        public static GXSelectArgs GetUsersByContentTypeGroups(
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
            GXSelectArgs ag = GXSelectArgs.Select<GXContentTypeGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupIds != null && groupIds.Any())
            {
                ag.Where.And<GXContentTypeGroup>(q => groupIds.Contains(q.Id));
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupContentTypeGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupContentTypeGroup>(q => GXSql.Exists<GXContentTypeGroup, GXUserGroupContentTypeGroup>(j => j.Id, j => j.ContentTypeGroupId, ag));
            args.Where.And<GXUserGroupContentTypeGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Get content types that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="id">ContentType Id.</param>
        /// <param name="exclude">Content Ids to exclude.</param>
        /// <param name="include">Content Ids to include.</param>
        /// <returns>List of content types that user can access.</returns>
        public static GXSelectArgs GetContentTypesByUser(
            Expression<Func<GXContentType, object>> columns,
            string userId,
            Guid? id = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs groups = GetContentTypeGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXContentTypeGroupContentType>(s => GXSql.One, q => GXSql.Exists<GXContentTypeGroup, GXContentTypeGroupContentType>(j => j.Id, j => j.ContentTypeGroupId, groups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXContentType>(q => GXSql.Exists<GXContentType, GXContentTypeGroupContentType>(j => j.Id, j => j.ContentTypeId, map2));
            if (id != null && id != Guid.Empty)
            {
                args.Where.And<GXContentType>(q => q.Id == id);
            }
            if (exclude?.Any() == true)
            {
                args.Where.And<GXContentType>(q => !exclude.Contains(q.Id));
            }
            if (include?.Any() == true)
            {
                args.Where.And<GXContentType>(q => include.Contains(q.Id));
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the content type.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="contentTypeId">ContentType id.</param>
        /// <returns>List of users who can access the content type.</returns>
        public static GXSelectArgs GetUsersByContentType(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? contentTypeId)
        {
            GXSelectArgs groups = GXSelectArgs.Select<GXContentTypeGroup>(s => GXSql.One, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXContentTypeGroupContentType>(s => GXSql.One, w => w.ContentTypeId == contentTypeId);
            groups.Where.And<GXContentTypeGroupContentType>(q => GXSql.Exists<GXContentTypeGroup, GXContentTypeGroupContentType>(j => j.Id, j => j.ContentTypeGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupContentTypeGroup>(s => GXSql.One, q => GXSql.Exists<GXContentTypeGroup, GXUserGroupContentTypeGroup>(j => j.Id, j => j.ContentTypeGroupId, groups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupContentTypeGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Returns a collection of users that can access the content types.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="ids">ContentType ids.</param>
        /// <returns>List of users who can access the content type.</returns>
        public static GXSelectArgs GetUsersByContentTypes(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? ids)
        {
            GXSelectArgs groups = GXSelectArgs.Select<GXContentTypeGroup>(s => GXSql.One, w => w.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXContentTypeGroupContentType>(s => GXSql.One);
            if (ids != null && ids.Any())
            {
                map1.Where.And<GXContentTypeGroupContentType>(w => ids.Contains(w.ContentTypeId));
            }
            groups.Where.And<GXContentTypeGroupContentType>(q => GXSql.Exists<GXContentTypeGroup, GXContentTypeGroupContentType>(j => j.Id, j => j.ContentTypeGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupContentTypeGroup>(s => GXSql.One, q => GXSql.Exists<GXContentTypeGroup, GXUserGroupContentTypeGroup>(j => j.Id, j => j.ContentTypeGroupId, groups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupContentTypeGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Get menu groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">Menu group id.</param>
        /// <returns>List of menu groups that user can access.</returns>
        public static GXSelectArgs GetMenuGroupsByUser(
            Expression<Func<GXMenuGroup, object>> columns,
            string userId,
            Guid? groupId = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupMenuGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupMenuGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXMenuGroup>(q => GXSql.Exists<GXMenuGroup, GXUserGroupMenuGroup>(j => j.Id, j => j.MenuGroupId, map2));
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXMenuGroup>(q => q.Id == groupId);
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
        /// <param name="exclude">Content Ids to exclude.</param>
        /// <param name="include">Content Ids to include.</param>
        /// <returns>List of menus that user can access.</returns>
        public static GXSelectArgs GetMenusByUser(
            Expression<Func<GXMenu, object>> columns,
            string userId,
            Guid? menuId = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs groups = GetMenuGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXMenuGroupMenu>(s => GXSql.One, q => GXSql.Exists<GXMenuGroup, GXMenuGroupMenu>(j => j.Id, j => j.MenuGroupId, groups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
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
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
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
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Get block groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">Block group id.</param>
        /// <returns>List of block groups that user can access.</returns>
        public static GXSelectArgs GetBlockGroupsByUser(
            Expression<Func<GXBlockGroup, object>> columns,
            string userId,
            Guid? groupId = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupBlockGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupBlockGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXBlockGroup>(q => GXSql.Exists<GXBlockGroup, GXUserGroupBlockGroup>(j => j.Id, j => j.BlockGroupId, map2));
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXBlockGroup>(q => q.Id == groupId);
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
        /// <param name="exclude">Block Ids to exclude.</param>
        /// <param name="include">Block Ids to include.</param>
        /// <returns>List of blocks that user can access.</returns>
        public static GXSelectArgs GetBlocksByUser(
            Expression<Func<GXBlock, object>> columns,
            string userId,
            Guid? blockId = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs groups = GetBlockGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXBlockGroupBlock>(s => GXSql.One, q => GXSql.Exists<GXBlockGroup, GXBlockGroupBlock>(j => j.Id, j => j.BlockGroupId, groups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
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
            GXSelectArgs blockGroups = GXSelectArgs.Select<GXBlockGroup>(s => GXSql.One, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXBlockGroupBlock>(s => GXSql.One, w => w.BlockId == blockId);
            blockGroups.Where.And<GXBlockGroupBlock>(q => GXSql.Exists<GXBlockGroup, GXBlockGroupBlock>(j => j.Id, j => j.BlockGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupBlockGroup>(s => GXSql.One, q => GXSql.Exists<GXBlockGroup, GXUserGroupBlockGroup>(j => j.Id, j => j.BlockGroupId, blockGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupBlockGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
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
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Get manufacturer groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">Manufacturer group id.</param>
        /// <returns>List of manufacturer groups that user can access.</returns>
        public static GXSelectArgs GetManufacturerGroupsByUser(
            Expression<Func<GXManufacturerGroup, object>> columns,
            string userId,
            Guid? groupId = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupManufacturerGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupManufacturerGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXManufacturerGroup>(q => GXSql.Exists<GXManufacturerGroup, GXUserGroupManufacturerGroup>(j => j.Id, j => j.ManufacturerGroupId, map2));
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXManufacturerGroup>(q => q.Id == groupId);
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
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
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
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Get key management groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">KeyManagement group id.</param>
        /// <returns>List of key management groups that user can access.</returns>
        public static GXSelectArgs GetKeyManagementGroupsByUser(
            Expression<Func<GXKeyManagementGroup, object>> columns,
            string userId,
            Guid? groupId = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupKeyManagementGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupKeyManagementGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXKeyManagementGroup>(q => GXSql.Exists<GXKeyManagementGroup, GXUserGroupKeyManagementGroup>(j => j.Id, j => j.KeyManagementGroupId, map2));
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXKeyManagementGroup>(q => q.Id == groupId);
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
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Get schedule groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">Schedule group id.</param>
        /// <param name="exclude">Schedule group Ids to exclude.</param>
        /// <param name="include">Schedule group Ids to include.</param>
        /// <returns>List of schedule groups that user can access.</returns>
        public static GXSelectArgs GetScheduleGroupsByUser(
            Expression<Func<GXScheduleGroup, object>> columns,
            string userId,
            Guid? groupId = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupScheduleGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupScheduleGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
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
        /// <param name="exclude">Schedule Ids to exclude.</param>
        /// <param name="include">Schedule Ids to include.</param>
        /// <returns>List of schedules that user can access.</returns>
        public static GXSelectArgs GetSchedulesByUser(
            Expression<Func<GXSchedule, object>> columns,
            string userId,
            Guid? scheduleId = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs groups = GetScheduleGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXScheduleGroupSchedule>(s => GXSql.One, q => GXSql.Exists<GXScheduleGroup, GXScheduleGroupSchedule>(j => j.Id, j => j.ScheduleGroupId, groups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
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
            args.Columns.Exclude<GXUser>(e => e.Schedules);
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
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
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
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Get schedules logs that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param> 
        /// <param name="userId">User Id.</param>
        /// <param name="scheduleId">Device Id.</param>
        /// <returns>List of schedules that user can access.</returns>
        public static GXSelectArgs GetScheduleLogsByUser(
            Expression<Func<GXDevice, object>> columns,
            string userId,
            Guid? scheduleId = null)
        {
            GXSelectArgs args = GetSchedulesByUser(s => s.Id, userId);
            args.Columns.Clear();
            args.Columns.Add<GXScheduleLog>();
            args.Joins.AddInnerJoin<GXSchedule, GXScheduleLog>(j => j.Id, j => j.Schedule);
            if (scheduleId != null && scheduleId != Guid.Empty)
            {
                args.Where.And<GXSchedule>(q => q.Id == scheduleId);
            }
            return args;
        }

        /// <summary>
        /// Get script groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">Script group id.</param>
        /// <returns>List of script groups that user can access.</returns>
        public static GXSelectArgs GetScriptGroupsByUser(
            Expression<Func<GXScriptGroup, object>> columns,
            string userId,
            Guid? groupId = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupScriptGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupScriptGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXScriptGroup>(q => GXSql.Exists<GXScriptGroup, GXUserGroupScriptGroup>(j => j.Id, j => j.ScriptGroupId, map2));
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXScriptGroup>(q => q.Id == groupId);
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
        /// <param name="exclude">Script Ids to exclude.</param>
        /// <param name="include">Script Ids to include.</param>
        /// <returns>List of scripts that user can access.</returns>
        public static GXSelectArgs GetScriptsByUser(
            Expression<Func<GXScript, object>> columns,
            string userId,
            Guid? scriptId = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs groups = GetScriptGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXScriptGroupScript>(s => GXSql.One, q => GXSql.Exists<GXScriptGroup, GXScriptGroupScript>(j => j.Id, j => j.ScriptGroupId, groups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
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
            GXSelectArgs scriptGroups = GXSelectArgs.Select<GXScriptGroup>(s => GXSql.One, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXScriptGroupScript>(s => GXSql.One, w => w.ScriptId == scriptId);
            scriptGroups.Where.And<GXScriptGroupScript>(q => GXSql.Exists<GXScriptGroup, GXScriptGroupScript>(j => j.Id, j => j.ScriptGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupScriptGroup>(s => GXSql.One, q => GXSql.Exists<GXScriptGroup, GXUserGroupScriptGroup>(j => j.Id, j => j.ScriptGroupId, scriptGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupScriptGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
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
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Get scripts logs by user.
        /// </summary>
        /// <param name="columns">Selected columns.</param> 
        /// <param name="userId">User Id.</param>
        /// <param name="scriptId">Device Id.</param>
        /// <returns>List of scripts user that user can access.</returns>
        public static GXSelectArgs GetScriptLogsByUser(
            Expression<Func<GXDevice, object>> columns,
            string userId,
            Guid? scriptId = null)
        {
            GXSelectArgs args = GetScriptsByUser(s => s.Id, userId, scriptId);
            args.Columns.Clear();
            args.Columns.Add<GXScriptLog>();
            args.Joins.AddInnerJoin<GXScript, GXScriptLog>(j => j.Id, j => j.Script);
            return args;
        }

        /// <summary>
        /// Get key managements errors by user.
        /// </summary>
        /// <param name="columns">Selected columns.</param> 
        /// <param name="userId">User Id.</param>
        /// <param name="keyId">Key management Id.</param>
        /// <returns>List of key managements user that user can access.</returns>
        public static GXSelectArgs GetKeyManagementLogsByUser(
            Expression<Func<GXDevice, object>> columns,
            string userId,
            Guid? keyId = null)
        {
            GXSelectArgs args = GetKeyManagementsByUser(s => s.Id, userId, keyId);
            args.Columns.Clear();
            args.Columns.Add<GXKeyManagementLog>();
            args.Joins.AddInnerJoin<GXKeyManagement, GXKeyManagementLog>(j => j.Id, j => j.KeyManagement);
            return args;
        }

        /// <summary>
        /// Get component view groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">ComponentView group id.</param>
        /// <returns>List of component view groups that user can access.</returns>
        public static GXSelectArgs GetComponentViewGroupsByUser(
            Expression<Func<GXComponentViewGroup, object>> columns,
            string userId,
            Guid? groupId = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId, groupId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupComponentViewGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupComponentViewGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXComponentViewGroup>(q => GXSql.Exists<GXComponentViewGroup, GXUserGroupComponentViewGroup>(j => j.Id, j => j.ComponentViewGroupId, map2));
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
            GXSelectArgs groups = GXSelectArgs.Select<GXComponentViewGroup>(s => GXSql.One, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXComponentViewGroupComponentView>(s => GXSql.One, w => w.ComponentViewId == id);
            groups.Where.And<GXComponentViewGroupComponentView>(q => GXSql.Exists<GXComponentViewGroup, GXComponentViewGroupComponentView>(j => j.Id, j => j.ComponentViewGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupComponentViewGroup>(s => GXSql.One, q => GXSql.Exists<GXComponentViewGroup, GXUserGroupComponentViewGroup>(j => j.Id, j => j.ComponentViewGroupId, groups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupComponentViewGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
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
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Get device template groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">DeviceTemplate group id.</param>
        /// <param name="exclude">Device template group Ids to exclude.</param>
        /// <param name="include">Device template group Ids to include.</param>
        /// <returns>List of device template groups that user can access.</returns>
        public static GXSelectArgs GetDeviceTemplateGroupsByUser(
            Expression<Func<GXDeviceTemplateGroup, object>> columns,
            string userId,
            Guid? groupId = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId, groupId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupDeviceTemplateGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupDeviceTemplateGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXDeviceTemplateGroup>(q => GXSql.Exists<GXDeviceTemplateGroup, GXUserGroupDeviceTemplateGroup>(j => j.Id, j => j.DeviceTemplateGroupId, map2));
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
        /// <param name="exclude">User group Ids to exclude.</param>
        /// <param name="include">User group Ids to include.</param>
        /// <returns>List of device templates that user can access.</returns>
        public static GXSelectArgs GetDeviceTemplatesByUser(
            Expression<Func<GXDeviceTemplate, object>> columns,
            string userId,
            Guid? id = null,
            IEnumerable<Guid>? exclude = null,
            IEnumerable<Guid>? include = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs groups = GetDeviceTemplateGroupsByUser(s => GXSql.One, userId, id);
            GXSelectArgs map2 = GXSelectArgs.Select<GXDeviceTemplateGroupDeviceTemplate>(s => GXSql.One, q => GXSql.Exists<GXDeviceTemplateGroup, GXDeviceTemplateGroupDeviceTemplate>(j => j.Id, j => j.DeviceTemplateGroupId, groups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXDeviceTemplate>(q => GXSql.Exists<GXDeviceTemplate, GXDeviceTemplateGroupDeviceTemplate>(j => j.Id, j => j.DeviceTemplateId, map2));
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
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
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
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Get gateway groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">Gateway group id.</param>
        /// <returns>List of gateway groups that user can access.</returns>
        public static GXSelectArgs GetGatewayGroupsByUser(
            Expression<Func<GXGatewayGroup, object>> columns,
            string userId,
            Guid? groupId = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId, groupId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupGatewayGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupGatewayGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXGatewayGroup>(q => GXSql.Exists<GXGatewayGroup, GXUserGroupGatewayGroup>(j => j.Id, j => j.GatewayGroupId, map2));
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
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
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
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Get gateway logs that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param> 
        /// <param name="userId">User Id.</param>
        /// <param name="gatewayId">Device Id.</param>
        /// <returns>List of gateways that user can access.</returns>
        public static GXSelectArgs GetGatewayErrorsByUser(
            Expression<Func<GXDevice, object>> columns,
            string userId,
            Guid? gatewayId = null)
        {
            GXSelectArgs args = GetGatewaysByUser(s => s.Id, userId, gatewayId);
            args.Columns.Clear();
            args.Columns.Add<GXGatewayLog>();
            args.Joins.AddInnerJoin<GXGateway, GXGatewayLog>(j => j.Id, j => j.Gateway);
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the subtotal group.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="groupId">Subtotal group id.</param>
        /// <returns>List of users who can access the subtotal group.</returns>
        public static GXSelectArgs GetUsersBySubtotalGroup(string userId, Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupSubtotalGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupSubtotalGroup, GXSubtotalGroup>(a => a.SubtotalGroupId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (groupId != null)
            {
                args.Where.And<GXSubtotalGroup>(q => q.Removed == null && q.Id == groupId);
            }
            else
            {
                args.Where.And<GXSubtotalGroup>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the subtotal group.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="groupIds">Subtotal group ids.</param>
        /// <returns>List of users who can access the subtotal group.</returns>
        public static GXSelectArgs GetUsersBySubtotalGroups(string userId, IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupSubtotalGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupSubtotalGroup, GXSubtotalGroup>(a => a.SubtotalGroupId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (groupIds != null)
            {
                args.Where.And<GXSubtotalGroup>(q => q.Removed == null && groupIds.Contains(q.Id));
            }
            else
            {
                args.Where.And<GXSubtotalGroup>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Get subtotals that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="subtotalId">Device Id.</param>
        /// <returns>List of subtotals that user can access.</returns>
        public static GXSelectArgs GetSubtotalsByUser(
            Expression<Func<GXSubtotal, object>> columns,
            string userId,
            Guid? subtotalId = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs groups = GetSubtotalGroupsByUser(s => GXSql.One, userId, subtotalId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXSubtotalGroupSubtotal>(s => GXSql.One, q => GXSql.Exists<GXSubtotalGroup, GXSubtotalGroupSubtotal>(j => j.Id, j => j.SubtotalGroupId, groups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXSubtotal>(q => GXSql.Exists<GXSubtotal, GXSubtotalGroupSubtotal>(j => j.Id, j => j.SubtotalId, map2));
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the subtotal.
        /// </summary>
        /// <param name="userId">UserId.</param>
        /// <param name="subtotalId">Subtotal id.</param>
        /// <returns>List of users who can access the subtotal group.</returns>
        public static GXSelectArgs GetUsersBySubtotal(string userId, Guid? subtotalId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupSubtotalGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupSubtotalGroup, GXSubtotalGroup>(a => a.SubtotalGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXSubtotalGroup, GXSubtotalGroupSubtotal>(a => a.Id, b => b.SubtotalGroupId);
            args.Joins.AddInnerJoin<GXSubtotalGroupSubtotal, GXSubtotal>(a => a.SubtotalId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXSubtotalGroup>(where => where.Removed == null);
            if (subtotalId != null && subtotalId != Guid.Empty)
            {
                args.Where.And<GXSubtotal>(where => where.Removed == null && where.Id == subtotalId);
            }
            else
            {
                args.Where.And<GXSubtotal>(where => where.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the subtotal.
        /// </summary>
        /// <param name="userId">UserId.</param>
        /// <param name="subtotalIds">Subtotal ids.</param>
        /// <returns>List of users who can access the subtotal group.</returns>
        public static GXSelectArgs GetUsersBySubtotals(string userId, IEnumerable<Guid>? subtotalIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupSubtotalGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupSubtotalGroup, GXSubtotalGroup>(a => a.SubtotalGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXSubtotalGroup, GXSubtotalGroupSubtotal>(a => a.Id, b => b.SubtotalGroupId);
            args.Joins.AddInnerJoin<GXSubtotalGroupSubtotal, GXSubtotal>(a => a.SubtotalId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXSubtotalGroup>(where => where.Removed == null);
            if (subtotalIds != null && subtotalIds.Any())
            {
                args.Where.And<GXSubtotal>(where => where.Removed == null && subtotalIds.Contains(where.Id));
            }
            else
            {
                args.Where.And<GXSubtotal>(where => where.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Get report groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">Report group id.</param>
        /// <returns>List of report groups that user can access.</returns>
        public static GXSelectArgs GetReportGroupsByUser(
            Expression<Func<GXReportGroup, object>> columns,
            string userId,
            Guid? groupId = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId, groupId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupReportGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupReportGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXReportGroup>(q => GXSql.Exists<GXReportGroup, GXUserGroupReportGroup>(j => j.Id, j => j.ReportGroupId, map2));
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the report group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupId">Report group id.</param>
        /// <returns>List of users who can access the report group.</returns>
        public static GXSelectArgs GetUsersByReportGroup(
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
            GXSelectArgs ag = GXSelectArgs.Select<GXReportGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupId != null && groupId != Guid.Empty)
            {
                ag.Where.And<GXReportGroup>(q => q.Id == groupId);
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupReportGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupReportGroup>(q => GXSql.Exists<GXReportGroup, GXUserGroupReportGroup>(j => j.Id, j => j.ReportGroupId, ag));
            args.Where.And<GXUserGroupReportGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the report group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupIds">Report group ids.</param>
        /// <returns>List of users who can access the report group.</returns>
        public static GXSelectArgs GetUsersByReportGroups(
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
            GXSelectArgs ag = GXSelectArgs.Select<GXReportGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupIds != null && groupIds.Any())
            {
                ag.Where.And<GXReportGroup>(q => groupIds.Contains(q.Id));
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupReportGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupReportGroup>(q => GXSql.Exists<GXReportGroup, GXUserGroupReportGroup>(j => j.Id, j => j.ReportGroupId, ag));
            args.Where.And<GXUserGroupReportGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Get reports that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="reportId">Report Id.</param>
        /// <returns>List of reports that user can access.</returns>
        public static GXSelectArgs GetReportsByUser(
            Expression<Func<GXReport, object>> columns,
            string userId,
            Guid? reportId = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs groups = GetReportGroupsByUser(s => GXSql.One, userId, reportId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXReportGroupReport>(s => GXSql.One, q => GXSql.Exists<GXReportGroup, GXReportGroupReport>(j => j.Id, j => j.ReportGroupId, groups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXReport>(q => GXSql.Exists<GXReport, GXReportGroupReport>(j => j.Id, j => j.ReportId, map2));
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the report.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="id">Report id.</param>
        /// <returns>List of users who can access the report.</returns>
        public static GXSelectArgs GetUsersByReport(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? id)
        {
            GXSelectArgs reportGroups = GXSelectArgs.Select<GXReportGroup>(s => GXSql.One, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXReportGroupReport>(s => GXSql.One);
            if (id != null && id != Guid.Empty)
            {
                map1.Where.And<GXReportGroupReport>(w => w.ReportId == id);
            }
            reportGroups.Where.And<GXReportGroupReport>(q => GXSql.Exists<GXReportGroup, GXReportGroupReport>(j => j.Id, j => j.ReportGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupReportGroup>(s => GXSql.One, q => GXSql.Exists<GXReportGroup, GXUserGroupReportGroup>(j => j.Id, j => j.ReportGroupId, reportGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupReportGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Returns a collection of users that can access the reports.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="reportIds">Report ids.</param>
        /// <returns>List of users who can access the report.</returns>
        public static GXSelectArgs GetUsersByReports(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? reportIds)
        {
            GXSelectArgs reportGroups = GXSelectArgs.Select<GXReportGroup>(s => GXSql.One, w => w.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXReportGroupReport>(s => GXSql.One);
            if (reportIds != null && reportIds.Any())
            {
                map1.Where.And<GXReportGroupReport>(w => reportIds.Contains(w.ReportId));
            }
            reportGroups.Where.And<GXReportGroupReport>(q => GXSql.Exists<GXReportGroup, GXReportGroupReport>(j => j.Id, j => j.ReportGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupReportGroup>(s => GXSql.One, q => GXSql.Exists<GXReportGroup, GXUserGroupReportGroup>(j => j.Id, j => j.ReportGroupId, reportGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupReportGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Get notification groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">Notification group id.</param>
        /// <returns>List of notification groups that user can access.</returns>
        public static GXSelectArgs GetNotificationGroupsByUser(
            Expression<Func<GXNotificationGroup, object>> columns,
            string userId,
            Guid? groupId = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId, groupId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupNotificationGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupNotificationGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXNotificationGroup>(q => GXSql.Exists<GXNotificationGroup, GXUserGroupNotificationGroup>(j => j.Id, j => j.NotificationGroupId, map2));
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the notification group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupId">Notification group id.</param>
        /// <returns>List of users who can access the notification group.</returns>
        public static GXSelectArgs GetUsersByNotificationGroup(
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
            GXSelectArgs ag = GXSelectArgs.Select<GXNotificationGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupId != null && groupId != Guid.Empty)
            {
                ag.Where.And<GXNotificationGroup>(q => q.Id == groupId);
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupNotificationGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupNotificationGroup>(q => GXSql.Exists<GXNotificationGroup, GXUserGroupNotificationGroup>(j => j.Id, j => j.NotificationGroupId, ag));
            args.Where.And<GXUserGroupNotificationGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the notification group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupIds">Notification group ids.</param>
        /// <returns>List of users who can access the notification group.</returns>
        public static GXSelectArgs GetUsersByNotificationGroups(
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
            GXSelectArgs ag = GXSelectArgs.Select<GXNotificationGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupIds != null && groupIds.Any())
            {
                ag.Where.And<GXNotificationGroup>(q => groupIds.Contains(q.Id));
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupNotificationGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupNotificationGroup>(q => GXSql.Exists<GXNotificationGroup, GXUserGroupNotificationGroup>(j => j.Id, j => j.NotificationGroupId, ag));
            args.Where.And<GXUserGroupNotificationGroup>(q => GXSql.Exists(sub));
            return args;
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
            string userId,
            Guid? notificationId = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs groups = GetNotificationGroupsByUser(s => GXSql.One, userId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXNotificationGroupNotification>(s => GXSql.One, q => GXSql.Exists<GXNotificationGroup, GXNotificationGroupNotification>(j => j.Id, j => j.NotificationGroupId, groups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXNotification>(q => GXSql.Exists<GXNotification, GXNotificationGroupNotification>(j => j.Id, j => j.NotificationId, map2));
            if (notificationId != null && notificationId != Guid.Empty)
            {
                args.Where.And<GXNotification>(w => w.Id == notificationId);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the notification.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="id">Notification id.</param>
        /// <returns>List of users who can access the notification.</returns>
        public static GXSelectArgs GetUsersByNotification(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid? id)
        {
            GXSelectArgs notificationGroups = GXSelectArgs.Select<GXNotificationGroup>(s => GXSql.One, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXNotificationGroupNotification>(s => GXSql.One);
            if (id != null && id != Guid.Empty)
            {
                map1.Where.And<GXNotificationGroupNotification>(w => w.NotificationId == id);
            }
            notificationGroups.Where.And<GXNotificationGroupNotification>(q => GXSql.Exists<GXNotificationGroup, GXNotificationGroupNotification>(j => j.Id, j => j.NotificationGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupNotificationGroup>(s => GXSql.One, q => GXSql.Exists<GXNotificationGroup, GXUserGroupNotificationGroup>(j => j.Id, j => j.NotificationGroupId, notificationGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupNotificationGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Returns a collection of users that can access the notifications.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="notificationIds">Notification ids.</param>
        /// <returns>List of users who can access the notification.</returns>
        public static GXSelectArgs GetUsersByNotifications(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? notificationIds)
        {
            GXSelectArgs notificationGroups = GXSelectArgs.Select<GXNotificationGroup>(s => GXSql.One, w => w.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXNotificationGroupNotification>(s => GXSql.One);
            if (notificationIds != null && notificationIds.Any())
            {
                map1.Where.And<GXNotificationGroupNotification>(w => notificationIds.Contains(w.NotificationId));
            }
            notificationGroups.Where.And<GXNotificationGroupNotification>(q => GXSql.Exists<GXNotificationGroup, GXNotificationGroupNotification>(j => j.Id, j => j.NotificationGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupNotificationGroup>(s => GXSql.One, q => GXSql.Exists<GXNotificationGroup, GXUserGroupNotificationGroup>(j => j.Id, j => j.NotificationGroupId, notificationGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupNotificationGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Get subtotal groups that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">UserId.</param>
        /// <param name="groupId">Subtotal group id.</param>
        /// <returns>List of subtotal groups that user can access.</returns>
        public static GXSelectArgs GetSubtotalGroupsByUser(
            Expression<Func<GXSubtotalGroup, object>> columns,
            string userId,
            Guid? groupId = null)
        {
            //Distinct is not needed because values are indexed;
            GXSelectArgs usergroups = GetUserGroupsByUser(s => GXSql.One, userId, groupId);
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupSubtotalGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupSubtotalGroup>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs args = GXSelectArgs.Select(columns, q => q.Removed == null);
            args.Where.And<GXSubtotalGroup>(q => GXSql.Exists<GXSubtotalGroup, GXUserGroupSubtotalGroup>(j => j.Id, j => j.SubtotalGroupId, map2));
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the subtotal group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupId">Subtotal group id.</param>
        /// <returns>List of users who can access the subtotal group.</returns>
        public static GXSelectArgs GetUsersBySubtotalGroup(
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
            GXSelectArgs ag = GXSelectArgs.Select<GXSubtotalGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupId != null && groupId != Guid.Empty)
            {
                ag.Where.And<GXSubtotalGroup>(q => q.Id == groupId);
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupSubtotalGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupSubtotalGroup>(q => GXSql.Exists<GXSubtotalGroup, GXUserGroupSubtotalGroup>(j => j.Id, j => j.SubtotalGroupId, ag));
            args.Where.And<GXUserGroupSubtotalGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the subtotal group.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="groupIds">Subtotal group ids.</param>
        /// <returns>List of users who can access the subtotal group.</returns>
        public static GXSelectArgs GetUsersBySubtotalGroups(
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
            GXSelectArgs ag = GXSelectArgs.Select<GXSubtotalGroup>(s => GXSql.One, w => w.Removed == null);
            if (groupIds != null && groupIds.Any())
            {
                ag.Where.And<GXSubtotalGroup>(q => groupIds.Contains(q.Id));
            }
            GXSelectArgs sub = GXSelectArgs.Select<GXUserGroupSubtotalGroup>(s => GXSql.One);
            sub.Where.And<GXUserGroupSubtotalGroup>(q => GXSql.Exists<GXSubtotalGroup, GXUserGroupSubtotalGroup>(j => j.Id, j => j.SubtotalGroupId, ag));
            args.Where.And<GXUserGroupSubtotalGroup>(q => GXSql.Exists(sub));
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the subtotal.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="subtotalId">Subtotal id.</param>
        /// <returns>List of users who can access the subtotal.</returns>
        public static GXSelectArgs GetUsersBySubtotal(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            Guid subtotalId)
        {
            GXSelectArgs subtotalGroups = GXSelectArgs.Select<GXSubtotalGroup>(s => GXSql.One, q => q.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXSubtotalGroupSubtotal>(s => GXSql.One, w => w.SubtotalId == subtotalId);
            subtotalGroups.Where.And<GXSubtotalGroupSubtotal>(q => GXSql.Exists<GXSubtotalGroup, GXSubtotalGroupSubtotal>(j => j.Id, j => j.SubtotalGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupSubtotalGroup>(s => GXSql.One, q => GXSql.Exists<GXSubtotalGroup, GXUserGroupSubtotalGroup>(j => j.Id, j => j.SubtotalGroupId, subtotalGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupSubtotalGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Returns a collection of users that can access the subtotals.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="subtotalIds">Subtotal ids.</param>
        /// <returns>List of users who can access the subtotal.</returns>
        public static GXSelectArgs GetUsersBySubtotals(
            Expression<Func<GXUser, object>> columns,
            string? userId,
            IEnumerable<Guid>? subtotalIds)
        {
            GXSelectArgs subtotalGroups = GXSelectArgs.Select<GXSubtotalGroup>(s => GXSql.One, w => w.Removed == null);
            //Distinct is not needed because values are indexed;
            GXSelectArgs map1 = GXSelectArgs.Select<GXSubtotalGroupSubtotal>(s => GXSql.One);
            if (subtotalIds != null && subtotalIds.Any())
            {
                map1.Where.And<GXSubtotalGroupSubtotal>(w => subtotalIds.Contains(w.SubtotalId));
            }
            subtotalGroups.Where.And<GXSubtotalGroupSubtotal>(q => GXSql.Exists<GXSubtotalGroup, GXSubtotalGroupSubtotal>(j => j.Id, j => j.SubtotalGroupId, map1));
            GXSelectArgs map2 = GXSelectArgs.Select<GXUserGroupSubtotalGroup>(s => GXSql.One, q => GXSql.Exists<GXSubtotalGroup, GXUserGroupSubtotalGroup>(j => j.Id, j => j.SubtotalGroupId, subtotalGroups));
            GXSelectArgs usergroups = GXSelectArgs.Select<GXUserGroup>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupSubtotalGroup>(j => j.Id, j => j.UserGroupId, map2));
            usergroups.Where.And<GXUserGroup>(q => q.Removed == null);
            GXSelectArgs map3 = GXSelectArgs.Select<GXUserGroupUser>(s => GXSql.One, q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId, usergroups));
            GXSelectArgs users = GXSelectArgs.Select(columns, q => GXSql.Exists<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId, map3));
            users.Where.And<GXUser>(q => q.Removed == null);
            return users;
        }

        /// <summary>
        /// Get tasks that user can access.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="agentIds">Agent Ids.</param>
        /// <param name="deviceIds">Agent Ids.</param>
        /// <param name="gatewayIds">Gateway Ids.</param>
        /// <returns>List of tasks that user can access.</returns>
        public static GXSelectArgs GetTasksByUser(
            Expression<Func<GXTask, object>> columns,
            string userId,
            IEnumerable<Guid>? agentIds = null,
            IEnumerable<Guid>? deviceIds = null,
            IEnumerable<Guid>? gatewayIds = null)
        {
            var user = new GXUser() { Id = userId };
            GXSelectArgs args = GXSelectArgs.Select(columns, w => w.Creator == user);
            args.Joins.AddInnerJoin<GXTask, GXUser>(j => j.Creator, j => j.Id);
            args.OrderBy.Add<GXTask>(q => q.Start);
            return args;
        }

        /// <summary>
        /// Get next device tasks that are not read yet.
        /// </summary>
        /// <param name="columns">Selected columns.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="agentId">Agent Id.</param>
        /// <param name="deviceId">Agent Id.</param>
        /// <param name="gatewayId">Gateway Id.</param>
        /// <returns>List of tasks that can be executed.</returns>
        public static GXSelectArgs GetNextTasks(
            Expression<Func<GXTask, object>> columns,
            string userId,
            Guid agentId,
            Guid? deviceId = null,
            Guid? gatewayId = null,
            bool all = false,
            bool listener = false)
        {
            GXSelectArgs args = GXSelectArgs.Select(columns, w => w.Start == null);
            //Distinct is not needed.
            args.Joins.AddInnerJoin<GXTask, GXDevice>(j => j.Device, j => j.Id);
            //Selected tasks that are mapped to the device.
            if (deviceId is Guid id)
            {
                GXDevice dev = new GXDevice() { Id = id };
                args.Where.And<GXTask>(q => q.Device == dev);
            }
            //Selected tasks that are mapped to the gateway.
            else if (gatewayId is Guid gateway)
            {
                args.Where.And<GXTask>(q => q.TargetGateway == gateway);
            }
            //Selected tasks that are mapped to the called agent.
            else if (agentId is Guid agent)
            {
                if (all)
                {
                    //Distinct is not needed because values are indexed;
                    GXSelectArgs map4 = GXSelectArgs.Select<GXAgentGroupDeviceGroup>(s => GXSql.One);
                    GXSelectArgs deviceGroups = GetDeviceGroupsByUser(s => GXSql.One, userId);
                    deviceGroups.Where.And<GXAgentGroupDeviceGroup>(q => !GXSql.Exists<GXDeviceGroup, GXAgentGroupDeviceGroup>(j => j.Id, j => j.DeviceGroupId, map4));
                    GXSelectArgs map3 = GXSelectArgs.Select<GXDeviceGroupDevice>(s => GXSql.One, q => GXSql.Exists<GXDeviceGroup, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceGroupId, deviceGroups));
                    args.Where.And<GXDevice>(q => GXSql.Exists<GXDevice, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceId, map3));
                }
                else
                {
                    //Distinct is not needed because values are indexed;
                    GXSelectArgs deviceGroups = GetDeviceGroupsByAgent(s => GXSql.One, userId, agent);
                    GXSelectArgs map3 = GXSelectArgs.Select<GXDeviceGroupDevice>(s => GXSql.One, q => GXSql.Exists<GXDeviceGroup, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceGroupId, deviceGroups));
                    args.Where.And<GXDevice>(q => GXSql.Exists<GXDevice, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceId, map3));
                }
            }
            else
            {
                throw new NotImplementedException();
            }
            return args;
        }
    }
}
