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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Internal
{
    static class GXQuery
    {
        /// <summary>
        /// Get user groups where user belongs.
        /// </summary>
        /// <param name="userId">UserId</param>
        /// <param name="groupId">User group id.</param>
        /// <returns>List of user groups where user belongs.</returns>
        public static GXSelectArgs GetUserGroupsByUser(string userId, Guid? groupId = null)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUserGroup>(s => s.Id, q => q.Removed == null);
            args.Distinct = true;
            args.Where.And<GXUser>(q => q.Removed == null && q.Id == userId);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(j => j.UserId, j => j.Id);
            if (groupId != null)
            {
                args.Where.And<GXUserGroup>(q => q.Id == groupId);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users who belong this user group.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="groupId">User group ID.</param>
        public static GXSelectArgs GetUsersByUserGroup(string? userId, Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id, q => q.Removed == null);
            args.Distinct = true;
            args.Where.And<GXUserGroupUser>(q => q.Removed == null);
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(j => j.UserGroupId, j => j.Id);
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXUserGroup>(q => q.Removed == null && q.Id == groupId);
            }
            else
            {
                args.Where.And<GXUserGroup>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users who belong user groups.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="groupIds">User group IDs.</param>
        public static GXSelectArgs GetUsersByUserGroups(string? userId, IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id, q => q.Removed == null);
            args.Distinct = true;
            args.Where.And<GXUserGroupUser>(q => q.Removed == null);
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(j => j.Id, j => j.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(j => j.UserGroupId, j => j.Id);
            if (groupIds != null && groupIds.Any())
            {
                args.Where.And<GXUserGroup>(q => q.Removed == null && groupIds.Contains(q.Id));
            }
            else
            {
                args.Where.And<GXUserGroup>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Get users that belong for the same group(s) with the user.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <returns>List of users that user can access.</returns>
        public static GXSelectArgs GetUsersByUser(string userId)
        {
            GXSelectArgs userGroups = GetUserGroupsByUser(userId);
            GXSelectArgs args = GXSelectArgs.Select<GXUserGroupUser>(s => s.UserGroupId, q => q.Removed == null);
            args.Distinct = true;
            args.Columns.Clear();
            args.Columns.Add<GXUser>();
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(j => j.UserGroupId, j => j.Id);
            args.Where.And<GXUserGroupUser>(q => GXSql.Exists<GXUserGroupUser, GXUser>(j => j.UserGroupId, j => j.Id, userGroups));
            args.Where.And<GXUser>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Get users errors by the user.
        /// </summary>
        /// <param name="userId">User Id</param>
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
        /// <param name="userId">User Id</param>
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
        /// <param name="userId">UserId</param>
        /// <param name="groupId">Device group id.</param>
        /// <returns>List of device groups that user can access.</returns>
        public static GXSelectArgs GetDeviceGroupsByUser(string userId, Guid? groupId = null)
        {
            GXSelectArgs userGroups = GetUserGroupsByUser(userId);
            GXSelectArgs args = GXSelectArgs.Select<GXUserGroupDeviceGroup>(s => s.DeviceGroupId, q => q.Removed == null);
            args.Distinct = true;
            args.Columns.Clear();
            args.Columns.Add<GXDeviceGroup>();
            args.Joins.AddInnerJoin<GXUserGroupDeviceGroup, GXDeviceGroup>(j => j.DeviceGroupId, j => j.Id);
            args.Where.And<GXUserGroupDeviceGroup>(q => GXSql.Exists<GXUserGroupDeviceGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id, userGroups));
            if (groupId != null)
            {
                args.Where.And<GXDeviceGroup>(q => q.Id == groupId);
            }
            args.Where.And<GXDeviceGroup>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the device group.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="groupId">Device group ID.</param>
        public static GXSelectArgs GetUsersByDeviceGroup(string userId, Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupDeviceGroup, GXDeviceGroup>(a => a.DeviceGroupId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXDeviceGroup>(q => q.Removed == null && q.Id == groupId);
            }
            else
            {
                args.Where.And<GXDeviceGroup>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the device group.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="groupIds">Device group IDs.</param>
        public static GXSelectArgs GetUsersByDeviceGroups(string userId, IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupDeviceGroup, GXDeviceGroup>(a => a.DeviceGroupId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (groupIds != null && groupIds.Any())
            {
                args.Where.And<GXDeviceGroup>(q => q.Removed == null && groupIds.Contains(q.Id));
            }
            else
            {
                args.Where.And<GXDeviceGroup>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Get devices that user can access.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="deviceId">Device Id.</param>
        /// <returns>List of devices that user can access.</returns>
        public static GXSelectArgs GetDevicesByUser(string userId, Guid? deviceId = null)
        {
            GXSelectArgs deviceGroups = GetDeviceGroupsByUser(userId);
            GXSelectArgs args = GXSelectArgs.Select<GXDeviceGroupDevice>(s => s.DeviceId, q => q.Removed == null);
            args.Distinct = true;
            args.Columns.Clear();
            args.Columns.Add<GXDevice>();
            args.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDevice>(j => j.DeviceId, j => j.Id);
            args.Where.And<GXDeviceGroupDevice>(q => GXSql.Exists<GXDeviceGroupDevice, GXDeviceGroup>(j => j.DeviceGroupId, j => j.Id, deviceGroups));
            if (deviceId != null && deviceId != Guid.Empty)
            {
                args.Where.And<GXDevice>(q => q.Id == deviceId);
            }
            args.Where.And<GXDevice>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Get devices that user can access.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="deviceIds">Device Id.</param>
        /// <returns>List of devices that user can access.</returns>
        public static GXSelectArgs GetDevicesByUser(string userId, IEnumerable<Guid>? deviceIds)
        {
            GXSelectArgs deviceGroups = GetDeviceGroupsByUser(userId);
            GXSelectArgs args = GXSelectArgs.Select<GXDeviceGroupDevice>(s => s.DeviceId, q => q.Removed == null);
            args.Distinct = true;
            args.Columns.Clear();
            args.Columns.Add<GXDevice>();
            args.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDevice>(j => j.DeviceId, j => j.Id);
            args.Where.And<GXDeviceGroupDevice>(q => GXSql.Exists<GXDeviceGroupDevice, GXDeviceGroup>(j => j.DeviceGroupId, j => j.Id, deviceGroups));
            if (deviceIds != null && deviceIds.Any())
            {
                args.Where.And<GXDevice>(q => deviceIds.Contains(q.Id));
            }
            args.Where.And<GXDevice>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Returns a collection of device groups where device belongs.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="deviceId">Device ID.</param>
        public static GXSelectArgs GetDeviceGroupsByDevice(string userId, Guid? deviceId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXDeviceGroup>(s => s.Id, w => w.Removed == null);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXDeviceGroup, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceGroupId);
            args.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDevice>(j => j.DeviceId, j => j.Id);
            args.Where.And<GXDevice>(q => q.Removed == null && q.Id == deviceId);
            return args;
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
        /// <param name="user">User.</param>
        /// <param name="deviceId">Device ID.</param>
        public static GXSelectArgs GetUsersByDevice(ClaimsPrincipal user, Guid? deviceId)
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
            if (deviceId != null)
            {
                args.Where.And<GXDevice>(where => where.Id == deviceId);
            }
            if (user != null && !user.IsInRole(GXRoles.Admin))
            {
                //Check that user can access group.
                // Get user's identity.
                var id = ServerHelpers.GetUserId(user);
                args.Where.And<GXUser>(where => where.Id == id);
            }
            return args;
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
        /// Get devices errors that user can access.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="deviceId">Device Id</param>
        /// <returns>List of devices that user can access.</returns>
        public static GXSelectArgs GetDeviceErrorsByUser(string userId, Guid? deviceId = null)
        {
            GXSelectArgs args = GetDevicesByUser(userId, deviceId);
            //Get only device error columns.
            args.Columns.Clear();
            args.Columns.Add<GXDeviceError>();
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXDevice, GXDeviceError>(j => j.Id, j => j.Device);
            return args;
        }

        /// <summary>
        /// Get devices traces that user can access.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="deviceId">Device Id</param>
        /// <returns>List of devices that user can access.</returns>
        public static GXSelectArgs GetDeviceTracesByUser(string userId, Guid? deviceId = null)
        {
            GXSelectArgs args = GetDevicesByUser(userId, deviceId);
            //Get only device trace columns.
            args.Columns.Clear();
            args.Columns.Add<GXDeviceTrace>();
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXDevice, GXDeviceTrace>(j => j.Id, j => j.Device);
            return args;
        }

        /// <summary>
        /// Get devices actions that user can access.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="deviceId">Device Id</param>
        /// <returns>List of devices that user can access.</returns>
        public static GXSelectArgs GetDeviceActionsByUser(string userId, Guid? deviceId = null)
        {
            GXSelectArgs args = GetDevicesByUser(userId, deviceId);
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
        /// <param name="userId">User Id</param>
        /// <param name="objectId">Object Id</param>
        /// <returns>List of objects that user can access.</returns>
        public static GXSelectArgs GetObjectsByUser(string userId, Guid? objectId = null)
        {
            GXSelectArgs args = GetDevicesByUser(userId);
            args.Columns.Clear();
            args.Columns.Add<GXObject>();
            args.Joins.AddInnerJoin<GXDevice, GXObject>(j => j.Id, j => j.Device);
            if (objectId != null)
            {
                args.Where.And<GXObject>(q => q.Id == objectId);
            }
            args.Where.And<GXObject>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Get object templates that user can access.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="objectId">Object template Id</param>
        /// <returns>List of object templates that user can access.</returns>
        public static GXSelectArgs GetObjectTemplatesByUser(string userId, Guid? objectId = null)
        {
            GXSelectArgs args = GetDeviceTemplatesByUser(userId);
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
        /// <param name="userId">User Id</param>
        /// <param name="objectId">Object template Id</param>
        /// <returns>List of object templates that user can access.</returns>
        public static GXSelectArgs GetAttributeTemplatesByUser(string userId, Guid? objectId = null)
        {
            GXSelectArgs args = GetDeviceTemplatesByUser(userId);
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
        /// <param name="userId">User Id</param>
        /// <param name="attributeId ">Attribute Id</param>
        /// <returns>List of attributes that user can access.</returns>
        public static GXSelectArgs GetAttributesByUser(string userId, Guid? attributeId = null)
        {
            GXSelectArgs args = GetDevicesByUser(userId);
            args.Columns.Clear();
            args.Columns.Add<GXAttribute>();
            args.Joins.AddInnerJoin<GXDevice, GXObject>(j => j.Id, j => j.Device);
            args.Joins.AddInnerJoin<GXObject, GXAttribute>(j => j.Id, j => j.Object);
            if (attributeId != null)
            {
                args.Where.And<GXAttribute>(q => q.Id == attributeId);
            }
            args.Where.And<GXObject>(q => q.Removed == null);
            args.Where.And<GXAttribute>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Returns a collection of users who can access the trigger group.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="groupId">Workflow group ID.</param>
        public static GXSelectArgs GetUsersByTriggerGroup(string userId, Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupTriggerGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupTriggerGroup, GXTriggerGroup>(a => a.TriggerGroupId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXTriggerGroup>(q => q.Removed == null && q.Id == groupId);
            }
            else
            {
                args.Where.And<GXTriggerGroup>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users who can access the trigger group.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="groupIds">Workflow group IDs.</param>
        public static GXSelectArgs GetUsersByTriggerGroups(string userId, IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupTriggerGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupTriggerGroup, GXTriggerGroup>(a => a.TriggerGroupId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (groupIds != null && groupIds.Any())
            {
                args.Where.And<GXTriggerGroup>(q => q.Removed == null && groupIds.Contains(q.Id));
            }
            else
            {
                args.Where.And<GXTriggerGroup>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Get trigger groups that user can access.
        /// </summary>
        /// <param name="userId">UserId</param>
        /// <param name="groupId">Trigger group id.</param>
        /// <returns>List of device groups that user can access.</returns>
        public static GXSelectArgs GetTriggerGroupsByUser(string userId, Guid? groupId = null)
        {
            GXSelectArgs userGroups = GetUserGroupsByUser(userId);
            GXSelectArgs args = GXSelectArgs.Select<GXUserGroupTriggerGroup>(s => s.TriggerGroupId, q => q.Removed == null);
            args.Distinct = true;
            args.Columns.Clear();
            args.Columns.Add<GXTriggerGroup>();
            args.Joins.AddInnerJoin<GXUserGroupTriggerGroup, GXTriggerGroup>(j => j.TriggerGroupId, j => j.Id);
            args.Where.And<GXUserGroupTriggerGroup>(q => GXSql.Exists<GXUserGroupTriggerGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id, userGroups));
            if (groupId != null)
            {
                args.Where.And<GXTriggerGroup>(q => q.Id == groupId);
            }
            args.Where.And<GXTriggerGroup>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Get triggers that user can access.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="deviceId">Device Id</param>
        /// <returns>List of devices that user can access.</returns>
        public static GXSelectArgs GetTriggersByUser(string userId, Guid? deviceId = null)
        {
            GXSelectArgs triggerGroups = GetTriggerGroupsByUser(userId);
            GXSelectArgs args = GXSelectArgs.Select<GXTriggerGroupTrigger>(s => s.TriggerId, q => q.Removed == null);
            args.Distinct = true;
            args.Columns.Clear();
            args.Columns.Add<GXTrigger>();
            args.Joins.AddInnerJoin<GXTriggerGroupTrigger, GXTrigger>(j => j.TriggerId, j => j.Id);
            args.Where.And<GXTriggerGroupTrigger>(q => GXSql.Exists<GXTriggerGroupTrigger, GXTriggerGroup>(j => j.TriggerGroupId, j => j.Id, triggerGroups));
            if (deviceId != null)
            {
                args.Where.And<GXTrigger>(q => q.Id == deviceId);
            }
            args.Where.And<GXTrigger>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Returns a collection of users who can access the trigger.
        /// </summary>
        /// <param name="userId">UserId</param>
        /// <param name="triggerId">Trigger ID.</param>
        public static GXSelectArgs GetUsersByTrigger(string userId, Guid? triggerId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupTriggerGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupTriggerGroup, GXTriggerGroup>(a => a.TriggerGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXTriggerGroup, GXTriggerGroupTrigger>(a => a.Id, b => b.TriggerGroupId);
            args.Joins.AddInnerJoin<GXTriggerGroupTrigger, GXTrigger>(a => a.TriggerId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXTriggerGroup>(where => where.Removed == null);
            if (triggerId != null && triggerId != Guid.Empty)
            {
                args.Where.And<GXTrigger>(where => where.Removed == null && where.Id == triggerId);
            }
            else
            {
                args.Where.And<GXTrigger>(where => where.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users who can access the triggers.
        /// </summary>
        /// <param name="userId">UserId</param>
        /// <param name="triggerIds">Trigger IDs.</param>
        public static GXSelectArgs GetUsersByTriggers(string userId, IEnumerable<Guid>? triggerIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupTriggerGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupTriggerGroup, GXTriggerGroup>(a => a.TriggerGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXTriggerGroup, GXTriggerGroupTrigger>(a => a.Id, b => b.TriggerGroupId);
            args.Joins.AddInnerJoin<GXTriggerGroupTrigger, GXTrigger>(a => a.TriggerId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXTriggerGroup>(where => where.Removed == null);
            if (triggerIds != null && triggerIds.Any())
            {
                args.Where.And<GXTrigger>(where => where.Removed == null && triggerIds.Contains(where.Id));
            }
            else
            {
                args.Where.And<GXTrigger>(where => where.Removed == null);
            }
            return args;
        }
        /// <summary>
        /// Get workflow groups that user can access.
        /// </summary>
        /// <param name="userId">UserId</param>
        /// <param name="groupId">Workflow group ID.</param>
        /// <returns>List of workflow groups that user can access.</returns>
        public static GXSelectArgs GetWorkflowGroupsByUser(string userId, Guid? groupId = null)
        {
            GXSelectArgs userGroups = GetUserGroupsByUser(userId);
            GXSelectArgs args = GXSelectArgs.Select<GXUserGroupWorkflowGroup>(s => s.WorkflowGroupId, q => q.Removed == null);
            args.Distinct = true;
            args.Columns.Clear();
            args.Columns.Add<GXWorkflowGroup>();
            args.Joins.AddInnerJoin<GXUserGroupWorkflowGroup, GXWorkflowGroup>(j => j.WorkflowGroupId, j => j.Id);
            args.Where.And<GXUserGroupWorkflowGroup>(q => GXSql.Exists<GXUserGroupWorkflowGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id, userGroups));
            if (groupId != null)
            {
                args.Where.And<GXWorkflowGroup>(q => q.Id == groupId);
            }
            args.Where.And<GXWorkflowGroup>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Returns a collection of users who can access the workflow group.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="groupId">Workflow group ID</param>
        public static GXSelectArgs GetUsersByWorkflowGroup(string userId, Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupWorkflowGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupWorkflowGroup, GXWorkflowGroup>(a => a.WorkflowGroupId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXWorkflowGroup>(q => q.Removed == null && q.Id == groupId);
            }
            else
            {
                args.Where.And<GXWorkflowGroup>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users who can access the workflow groups.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="groupIds">Workflow group IDs</param>
        public static GXSelectArgs GetUsersByWorkflowGroups(string userId, IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupWorkflowGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupWorkflowGroup, GXWorkflowGroup>(a => a.WorkflowGroupId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (groupIds != null && groupIds.Any())
            {
                args.Where.And<GXWorkflowGroup>(q => q.Removed == null && groupIds.Contains(q.Id));
            }
            else
            {
                args.Where.And<GXWorkflowGroup>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Get workflows that user can access.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="workflowId">Workflow Id</param>
        /// <returns>List of workflows that user can access.</returns>
        public static GXSelectArgs GetWorkflowsByUser(string userId, Guid? workflowId = null)
        {
            GXSelectArgs workflowGroups = GetWorkflowGroupsByUser(userId);
            GXSelectArgs args = GXSelectArgs.Select<GXWorkflowGroupWorkflow>(s => s.WorkflowId, q => q.Removed == null);
            args.Distinct = true;
            args.Columns.Clear();
            args.Columns.Add<GXWorkflow>();
            args.Joins.AddInnerJoin<GXWorkflowGroupWorkflow, GXWorkflow>(j => j.WorkflowId, j => j.Id);
            args.Where.And<GXWorkflowGroupWorkflow>(q => GXSql.Exists<GXWorkflowGroupWorkflow, GXWorkflowGroup>(j => j.WorkflowGroupId, j => j.Id, workflowGroups));
            if (workflowId != null)
            {
                args.Where.And<GXWorkflow>(q => q.Id == workflowId);
            }
            args.Where.And<GXWorkflow>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Returns a collection of users who can access the workflow.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="workflowId">Workflow ID.</param>
        public static GXSelectArgs GetUsersByWorkflow(string userId, Guid? workflowId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupWorkflowGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupWorkflowGroup, GXWorkflowGroup>(a => a.WorkflowGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXWorkflowGroup, GXWorkflowGroupWorkflow>(a => a.Id, b => b.WorkflowGroupId);
            args.Joins.AddInnerJoin<GXWorkflowGroupWorkflow, GXWorkflow>(a => a.WorkflowId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXWorkflowGroup>(where => where.Removed == null);
            if (workflowId != null)
            {
                args.Where.And<GXWorkflow>(where => where.Removed == null && where.Id == workflowId);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users who can access the workflows.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="workflowIds">Workflow IDs.</param>
        public static GXSelectArgs GetUsersByWorkflows(string userId, IEnumerable<Guid>? workflowIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupWorkflowGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupWorkflowGroup, GXWorkflowGroup>(a => a.WorkflowGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXWorkflowGroup, GXWorkflowGroupWorkflow>(a => a.Id, b => b.WorkflowGroupId);
            args.Joins.AddInnerJoin<GXWorkflowGroupWorkflow, GXWorkflow>(a => a.WorkflowId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXWorkflowGroup>(where => where.Removed == null);
            if (workflowIds != null)
            {
                args.Where.And<GXWorkflow>(where => where.Removed == null && workflowIds.Contains(where.Id));
            }
            return args;
        }
        /// <summary>
        /// Get workflows that user can access.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="workflowId">Workflow Id</param>
        /// <returns>List of workflows that user can access.</returns>
        public static GXSelectArgs GetWorkflowLogsByUser(string userId, Guid? workflowId = null)
        {
            GXSelectArgs args = GetWorkflowsByUser(userId);
            args.Columns.Clear();
            args.Columns.Add<GXWorkflowLog>();
            args.Joins.AddInnerJoin<GXWorkflow, GXWorkflowLog>(j => j.Id, j => j.Workflow);
            return args;
        }

        /// <summary>
        /// Get agent groups that user can access.
        /// </summary>
        /// <param name="userId">UserId</param>
        /// <param name="groupId">Agent group id.</param>
        /// <returns>List of agent groups that user can access.</returns>
        public static GXSelectArgs GetAgentGroupsByUser(string userId, Guid? groupId = null)
        {
            GXSelectArgs userGroups = GetUserGroupsByUser(userId);
            GXSelectArgs args = GXSelectArgs.Select<GXUserGroupAgentGroup>(s => s.AgentGroupId, q => q.Removed == null);
            args.Distinct = true;
            args.Columns.Clear();
            args.Columns.Add<GXAgentGroup>();
            args.Joins.AddInnerJoin<GXUserGroupAgentGroup, GXAgentGroup>(j => j.AgentGroupId, j => j.Id);
            args.Where.And<GXUserGroupAgentGroup>(q => GXSql.Exists<GXUserGroupAgentGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id, userGroups));
            if (groupId != null)
            {
                args.Where.And<GXAgentGroup>(q => q.Id == groupId);
            }
            args.Where.And<GXAgentGroup>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the agent group.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="groupId">Agent group id.</param>
        /// <returns>List of users who can access the agent group.</returns>
        public static GXSelectArgs GetUsersByAgentGroup(string userId, Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupAgentGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupAgentGroup, GXAgentGroup>(a => a.AgentGroupId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (groupId != null)
            {
                args.Where.And<GXAgentGroup>(q => q.Removed == null && q.Id == groupId);
            }
            else
            {
                args.Where.And<GXAgentGroup>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the agent group.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="groupIds">Agent group ids.</param>
        /// <returns>List of users who can access the agent group.</returns>
        public static GXSelectArgs GetUsersByAgentGroups(string userId, IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupAgentGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupAgentGroup, GXAgentGroup>(a => a.AgentGroupId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (groupIds != null)
            {
                args.Where.And<GXAgentGroup>(q => q.Removed == null && groupIds.Contains(q.Id));
            }
            else
            {
                args.Where.And<GXAgentGroup>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Get agents that user can access.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="agentId">Device Id</param>
        /// <returns>List of agents that user can access.</returns>
        public static GXSelectArgs GetAgentsByUser(string userId, Guid? agentId = null)
        {
            GXSelectArgs agentGroups = GetAgentGroupsByUser(userId);
            GXSelectArgs args = GXSelectArgs.Select<GXAgentGroupAgent>(s => s.AgentId, q => q.Removed == null);
            args.Distinct = true;
            args.Columns.Clear();
            args.Columns.Add<GXAgent>();
            args.Joins.AddInnerJoin<GXAgentGroupAgent, GXAgent>(j => j.AgentId, j => j.Id);
            args.Where.And<GXAgentGroupAgent>(q => GXSql.Exists<GXAgentGroupAgent, GXAgentGroup>(j => j.AgentGroupId, j => j.Id, agentGroups));
            if (agentId != null)
            {
                args.Where.And<GXAgent>(q => q.Id == agentId);
            }
            args.Where.And<GXAgent>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the agent.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="agentId">Agent id.</param>
        /// <returns>List of users who can access the agent.</returns>
        public static GXSelectArgs GetUsersByAgent(string userId, Guid? agentId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupAgentGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupAgentGroup, GXAgentGroup>(a => a.AgentGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXAgentGroup, GXAgentGroupAgent>(a => a.Id, b => b.AgentGroupId);
            args.Joins.AddInnerJoin<GXAgentGroupAgent, GXAgent>(a => a.AgentId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXAgentGroup>(where => where.Removed == null);
            args.Where.And<GXAgent>(where => where.Removed == null);
            if (agentId != null && agentId != Guid.Empty)
            {
                args.Where.And<GXAgent>(where => where.Id == agentId);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the agents.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="agentIds">Agent ids.</param>
        /// <returns>List of users who can access the agent.</returns>
        public static GXSelectArgs GetUsersByAgents(string userId, IEnumerable<Guid>? agentIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupAgentGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupAgentGroup, GXAgentGroup>(a => a.AgentGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXAgentGroup, GXAgentGroupAgent>(a => a.Id, b => b.AgentGroupId);
            args.Joins.AddInnerJoin<GXAgentGroupAgent, GXAgent>(a => a.AgentId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXAgentGroup>(where => where.Removed == null);
            args.Where.And<GXAgent>(where => where.Removed == null);
            if (agentIds != null)
            {
                args.Where.And<GXAgent>(where => agentIds.Contains(where.Id));
            }
            return args;
        }

        /// <summary>
        /// Get agent errors that user can access.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="agentId">Device Id</param>
        /// <returns>List of agents that user can access.</returns>
        public static GXSelectArgs GetAgentErrorsByUser(string userId, Guid? agentId = null)
        {
            GXSelectArgs args = GetAgentsByUser(userId);
            args.Columns.Clear();
            args.Columns.Add<GXAgentLog>();
            args.Joins.AddInnerJoin<GXAgent, GXAgentLog>(j => j.Id, j => j.Agent);
            return args;
        }

        /// <summary>
        /// Get module groups that user can access.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="groupId">Module group id.</param>
        /// <returns>List of module groups that user can access.</returns>
        public static GXSelectArgs GetModuleGroupsByUser(string userId, Guid? groupId = null)
        {
            GXSelectArgs userGroups = GetUserGroupsByUser(userId);
            GXSelectArgs args = GXSelectArgs.Select<GXUserGroupModuleGroup>(s => s.ModuleGroupId, q => q.Removed == null);
            args.Distinct = true;
            args.Columns.Clear();
            args.Columns.Add<GXModuleGroup>();
            args.Joins.AddInnerJoin<GXUserGroupModuleGroup, GXModuleGroup>(j => j.ModuleGroupId, j => j.Id);
            args.Where.And<GXUserGroupModuleGroup>(q => GXSql.Exists<GXUserGroupModuleGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id, userGroups));
            if (groupId != null)
            {
                args.Where.And<GXModuleGroup>(q => q.Id == groupId);
            }
            args.Where.And<GXModuleGroup>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the module group.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="groupId">Group ID.</param>
        public static GXSelectArgs GetUsersByModuleGroup(string userId, Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupModuleGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupModuleGroup, GXModuleGroup>(a => a.ModuleGroupId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXModuleGroup>(q => q.Removed == null && q.Id == groupId);
            }
            else
            {
                args.Where.And<GXModuleGroup>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the module group.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="groupIds">Group IDs.</param>
        public static GXSelectArgs GetUsersByModuleGroups(string userId, IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupModuleGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupModuleGroup, GXModuleGroup>(a => a.ModuleGroupId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (groupIds != null && groupIds.Any())
            {
                args.Where.And<GXModuleGroup>(q => q.Removed == null && groupIds.Contains(q.Id));
            }
            else
            {
                args.Where.And<GXModuleGroup>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Get modules that user can access the module.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="moduleId">module Id</param>
        /// <returns>List of module that user can access.</returns>
        public static GXSelectArgs GetModulesByUser(string userId, string? moduleId = null)
        {
            GXSelectArgs moduleGroups = GetModuleGroupsByUser(userId);
            GXSelectArgs args = GXSelectArgs.Select<GXModuleGroupModule>(s => s.ModuleId, q => q.Removed == null);
            args.Distinct = true;
            args.Columns.Clear();
            args.Columns.Add<GXModule>();
            args.Joins.AddInnerJoin<GXModuleGroupModule, GXModule>(j => j.ModuleId, j => j.Id);
            args.Where.And<GXModuleGroupModule>(q => GXSql.Exists<GXModuleGroupModule, GXModuleGroup>(j => j.ModuleGroupId, j => j.Id, moduleGroups));
            if (moduleId != null)
            {
                args.Where.And<GXModule>(q => q.Id == moduleId);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the module.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="moduleId">Module ID.</param>
        public static GXSelectArgs GetUsersByModule(string userId, string? moduleId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupModuleGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupModuleGroup, GXModuleGroup>(a => a.ModuleGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXModuleGroup, GXModuleGroupModule>(a => a.Id, b => b.ModuleGroupId);
            args.Joins.AddInnerJoin<GXModuleGroupModule, GXModule>(a => a.ModuleId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXModuleGroup>(where => where.Removed == null);
            if (moduleId != null)
            {
                args.Where.And<GXModule>(where => where.Id == moduleId);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the module.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="moduleIds">Module IDs.</param>
        public static GXSelectArgs GetUsersByModules(string userId, IEnumerable<string>? moduleIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupModuleGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupModuleGroup, GXModuleGroup>(a => a.ModuleGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXModuleGroup, GXModuleGroupModule>(a => a.Id, b => b.ModuleGroupId);
            args.Joins.AddInnerJoin<GXModuleGroupModule, GXModule>(a => a.ModuleId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXModuleGroup>(where => where.Removed == null);
            if (moduleIds != null && moduleIds.Any())
            {
                args.Where.And<GXModule>(where => moduleIds.Contains(where.Id));
            }
            return args;
        }

        /// <summary>
        /// Get modules that user can access.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="moduleId">module Id</param>
        /// <returns>List of module that user can access.</returns>
        public static GXSelectArgs GetModuleLogsByUser(string userId, string? moduleId = null)
        {
            GXSelectArgs args = GetModulesByUser(userId);
            args.Columns.Clear();
            args.Columns.Add<GXModuleLog>();
            args.Joins.AddInnerJoin<GXModule, GXModuleLog>(j => j.Id, j => j.Module);
            return args;
        }

        /// <summary>
        /// Get block groups that user can access.
        /// </summary>
        /// <param name="userId">UserId</param>
        /// <param name="groupId">Block group id.</param>
        /// <returns>List of block groups that user can access.</returns>
        public static GXSelectArgs GetBlockGroupsByUser(string userId, Guid? groupId = null)
        {
            GXSelectArgs userGroups = GetUserGroupsByUser(userId);
            GXSelectArgs args = GXSelectArgs.Select<GXUserGroupBlockGroup>(s => s.BlockGroupId, q => q.Removed == null);
            args.Distinct = true;
            args.Columns.Clear();
            args.Columns.Add<GXBlockGroup>();
            args.Joins.AddInnerJoin<GXUserGroupBlockGroup, GXBlockGroup>(j => j.BlockGroupId, j => j.Id);
            args.Where.And<GXUserGroupBlockGroup>(q => GXSql.Exists<GXUserGroupBlockGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id, userGroups));
            if (groupId != null)
            {
                args.Where.And<GXBlockGroup>(q => q.Id == groupId);
            }
            args.Where.And<GXBlockGroup>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the block group.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="groupId">Block group id.</param>
        /// <returns>List of users who can access the block group.</returns>
        public static GXSelectArgs GetUsersByBlockGroup(string userId, Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupBlockGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupBlockGroup, GXBlockGroup>(a => a.BlockGroupId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (groupId != null)
            {
                args.Where.And<GXBlockGroup>(q => q.Removed == null && q.Id == groupId);
            }
            else
            {
                args.Where.And<GXBlockGroup>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the block group.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="groupIds">Block group ids.</param>
        /// <returns>List of users who can access the block group.</returns>
        public static GXSelectArgs GetUsersByBlockGroups(string userId, IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupBlockGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupBlockGroup, GXBlockGroup>(a => a.BlockGroupId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (groupIds != null)
            {
                args.Where.And<GXBlockGroup>(q => q.Removed == null && groupIds.Contains(q.Id));
            }
            else
            {
                args.Where.And<GXBlockGroup>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Get blocks that user can access.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="blockId">Device Id</param>
        /// <returns>List of blocks that user can access.</returns>
        public static GXSelectArgs GetBlocksByUser(string userId, Guid? blockId = null)
        {
            GXSelectArgs blockGroups = GetBlockGroupsByUser(userId);
            GXSelectArgs args = GXSelectArgs.Select<GXBlockGroupBlock>(s => s.BlockId, q => q.Removed == null);
            args.Distinct = true;
            args.Columns.Clear();
            args.Columns.Add<GXBlock>();
            args.Joins.AddInnerJoin<GXBlockGroupBlock, GXBlock>(j => j.BlockId, j => j.Id);
            args.Where.And<GXBlockGroupBlock>(q => GXSql.Exists<GXBlockGroupBlock, GXBlockGroup>(j => j.BlockGroupId, j => j.Id, blockGroups));
            if (blockId != null)
            {
                args.Where.And<GXBlock>(q => q.Id == blockId);
            }
            args.Where.And<GXBlock>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the block.
        /// </summary>
        /// <param name="userId">UserId</param>
        /// <param name="blockId">Block id.</param>
        /// <returns>List of users who can access the block group.</returns>
        public static GXSelectArgs GetUsersByBlock(string userId, Guid? blockId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupBlockGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupBlockGroup, GXBlockGroup>(a => a.BlockGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXBlockGroup, GXBlockGroupBlock>(a => a.Id, b => b.BlockGroupId);
            args.Joins.AddInnerJoin<GXBlockGroupBlock, GXBlock>(a => a.BlockId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXBlockGroup>(where => where.Removed == null);
            if (blockId != null && blockId != Guid.Empty)
            {
                args.Where.And<GXBlock>(where => where.Removed == null && where.Id == blockId);
            }
            else
            {
                args.Where.And<GXBlock>(where => where.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the block.
        /// </summary>
        /// <param name="userId">UserId</param>
        /// <param name="blockIds">Block ids.</param>
        /// <returns>List of users who can access the block group.</returns>
        public static GXSelectArgs GetUsersByBlocks(string userId, IEnumerable<Guid>? blockIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupBlockGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupBlockGroup, GXBlockGroup>(a => a.BlockGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXBlockGroup, GXBlockGroupBlock>(a => a.Id, b => b.BlockGroupId);
            args.Joins.AddInnerJoin<GXBlockGroupBlock, GXBlock>(a => a.BlockId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXBlockGroup>(where => where.Removed == null);
            if (blockIds != null && blockIds.Any())
            {
                args.Where.And<GXBlock>(where => where.Removed == null && blockIds.Contains(where.Id));
            }
            else
            {
                args.Where.And<GXBlock>(where => where.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Get manufacturer groups that user can access.
        /// </summary>
        /// <param name="userId">UserId</param>
        /// <param name="groupId">Manufacturer group id.</param>
        /// <returns>List of manufacturer groups that user can access.</returns>
        public static GXSelectArgs GetManufacturerGroupsByUser(string userId, Guid? groupId = null)
        {
            GXSelectArgs userGroups = GetUserGroupsByUser(userId);
            GXSelectArgs args = GXSelectArgs.Select<GXUserGroupManufacturerGroup>(s => s.ManufacturerGroupId, q => q.Removed == null);
            args.Distinct = true;
            args.Columns.Clear();
            args.Columns.Add<GXManufacturerGroup>();
            args.Joins.AddInnerJoin<GXUserGroupManufacturerGroup, GXManufacturerGroup>(j => j.ManufacturerGroupId, j => j.Id);
            args.Where.And<GXUserGroupManufacturerGroup>(q => GXSql.Exists<GXUserGroupManufacturerGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id, userGroups));
            if (groupId != null)
            {
                args.Where.And<GXManufacturerGroup>(q => q.Id == groupId);
            }
            args.Where.And<GXManufacturerGroup>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the manufacturer group.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="groupId">Manufacturer group id.</param>
        /// <returns>List of users who can access the manufacturer group.</returns>
        public static GXSelectArgs GetUsersByManufacturerGroup(string userId, Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupManufacturerGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupManufacturerGroup, GXManufacturerGroup>(a => a.ManufacturerGroupId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (groupId != null)
            {
                args.Where.And<GXManufacturerGroup>(q => q.Removed == null && q.Id == groupId);
            }
            else
            {
                args.Where.And<GXManufacturerGroup>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the manufacturer group.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="groupIds">Manufacturer group ids.</param>
        /// <returns>List of users who can access the manufacturer group.</returns>
        public static GXSelectArgs GetUsersByManufacturerGroups(string userId, IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupManufacturerGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupManufacturerGroup, GXManufacturerGroup>(a => a.ManufacturerGroupId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (groupIds != null)
            {
                args.Where.And<GXManufacturerGroup>(q => q.Removed == null && groupIds.Contains(q.Id));
            }
            else
            {
                args.Where.And<GXManufacturerGroup>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Get manufacturers that user can access.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="manufacturerId">Device Id</param>
        /// <returns>List of manufacturers that user can access.</returns>
        public static GXSelectArgs GetManufacturersByUser(string userId, Guid? manufacturerId = null)
        {
            GXSelectArgs manufacturerGroups = GetManufacturerGroupsByUser(userId);
            GXSelectArgs args = GXSelectArgs.Select<GXManufacturerGroupManufacturer>(s => s.ManufacturerId, q => q.Removed == null);
            args.Distinct = true;
            args.Columns.Clear();
            args.Columns.Add<GXManufacturer>();
            args.Joins.AddInnerJoin<GXManufacturerGroupManufacturer, GXManufacturer>(j => j.ManufacturerId, j => j.Id);
            args.Where.And<GXManufacturerGroupManufacturer>(q => GXSql.Exists<GXManufacturerGroupManufacturer, GXManufacturerGroup>(j => j.ManufacturerGroupId, j => j.Id, manufacturerGroups));
            if (manufacturerId != null)
            {
                args.Where.And<GXManufacturer>(q => q.Id == manufacturerId);
            }
            args.Where.And<GXManufacturer>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the manufacturer.
        /// </summary>
        /// <param name="userId">UserId</param>
        /// <param name="manufacturerId">Manufacturer id.</param>
        /// <returns>List of users who can access the manufacturer group.</returns>
        public static GXSelectArgs GetUsersByManufacturer(string userId, Guid? manufacturerId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupManufacturerGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupManufacturerGroup, GXManufacturerGroup>(a => a.ManufacturerGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXManufacturerGroup, GXManufacturerGroupManufacturer>(a => a.Id, b => b.ManufacturerGroupId);
            args.Joins.AddInnerJoin<GXManufacturerGroupManufacturer, GXManufacturer>(a => a.ManufacturerId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXManufacturerGroup>(where => where.Removed == null);
            if (manufacturerId != null && manufacturerId != Guid.Empty)
            {
                args.Where.And<GXManufacturer>(where => where.Removed == null && where.Id == manufacturerId);
            }
            else
            {
                args.Where.And<GXManufacturer>(where => where.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the manufacturer.
        /// </summary>
        /// <param name="userId">UserId</param>
        /// <param name="manufacturerIds">Manufacturer ids.</param>
        /// <returns>List of users who can access the manufacturer group.</returns>
        public static GXSelectArgs GetUsersByManufacturers(string userId, IEnumerable<Guid>? manufacturerIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupManufacturerGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupManufacturerGroup, GXManufacturerGroup>(a => a.ManufacturerGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXManufacturerGroup, GXManufacturerGroupManufacturer>(a => a.Id, b => b.ManufacturerGroupId);
            args.Joins.AddInnerJoin<GXManufacturerGroupManufacturer, GXManufacturer>(a => a.ManufacturerId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXManufacturerGroup>(where => where.Removed == null);
            if (manufacturerIds != null && manufacturerIds.Any())
            {
                args.Where.And<GXManufacturer>(where => where.Removed == null && manufacturerIds.Contains(where.Id));
            }
            else
            {
                args.Where.And<GXManufacturer>(where => where.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Get key management groups that user can access.
        /// </summary>
        /// <param name="userId">UserId</param>
        /// <param name="groupId">KeyManagement group id.</param>
        /// <returns>List of key management groups that user can access.</returns>
        public static GXSelectArgs GetKeyManagementGroupsByUser(string userId, Guid? groupId = null)
        {
            GXSelectArgs userGroups = GetUserGroupsByUser(userId);
            GXSelectArgs args = GXSelectArgs.Select<GXUserGroupKeyManagementGroup>(s => s.KeyManagementGroupId, q => q.Removed == null);
            args.Distinct = true;
            args.Columns.Clear();
            args.Columns.Add<GXKeyManagementGroup>();
            args.Joins.AddInnerJoin<GXUserGroupKeyManagementGroup, GXKeyManagementGroup>(j => j.KeyManagementGroupId, j => j.Id);
            args.Where.And<GXUserGroupKeyManagementGroup>(q => GXSql.Exists<GXUserGroupKeyManagementGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id, userGroups));
            if (groupId != null)
            {
                args.Where.And<GXKeyManagementGroup>(q => q.Id == groupId);
            }
            args.Where.And<GXKeyManagementGroup>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the key management group.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="groupId">KeyManagement group id.</param>
        /// <returns>List of users who can access the key management group.</returns>
        public static GXSelectArgs GetUsersByKeyManagementGroup(string userId, Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupKeyManagementGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupKeyManagementGroup, GXKeyManagementGroup>(a => a.KeyManagementGroupId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (groupId != null)
            {
                args.Where.And<GXKeyManagementGroup>(q => q.Removed == null && q.Id == groupId);
            }
            else
            {
                args.Where.And<GXKeyManagementGroup>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the key management group.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="groupIds">KeyManagement group ids.</param>
        /// <returns>List of users who can access the key management group.</returns>
        public static GXSelectArgs GetUsersByKeyManagementGroups(string userId, IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupKeyManagementGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupKeyManagementGroup, GXKeyManagementGroup>(a => a.KeyManagementGroupId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (groupIds != null)
            {
                args.Where.And<GXKeyManagementGroup>(q => q.Removed == null && groupIds.Contains(q.Id));
            }
            else
            {
                args.Where.And<GXKeyManagementGroup>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Get key managements that user can access.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="keyId">Device Id</param>
        /// <returns>List of key managements that user can access.</returns>
        public static GXSelectArgs GetKeyManagementsByUser(string userId, Guid? keyId = null)
        {
            GXSelectArgs keyGroups = GetKeyManagementGroupsByUser(userId);
            keyGroups.Columns.Clear();
            keyGroups.Columns.Add<GXKeyManagementGroup>(s => s.Id);
            GXSelectArgs args = GXSelectArgs.Select<GXKeyManagementGroupKeyManagement>(s => s.KeyManagementId, q => q.Removed == null);
            args.Distinct = true;
            args.Columns.Clear();
            args.Columns.Add<GXKeyManagement>();
            args.Joins.AddInnerJoin<GXKeyManagementGroupKeyManagement, GXKeyManagement>(j => j.KeyManagementId, j => j.Id);
            args.Where.And<GXKeyManagementGroupKeyManagement>(q => GXSql.Exists<GXKeyManagementGroupKeyManagement, GXKeyManagementGroup>(j => j.KeyManagementGroupId, j => j.Id, keyGroups));
            if (keyId != null)
            {
                args.Where.And<GXKeyManagement>(q => q.Id == keyId);
            }
            args.Where.And<GXKeyManagement>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the key management.
        /// </summary>
        /// <param name="userId">UserId</param>
        /// <param name="keyId">KeyManagement id.</param>
        /// <returns>List of users who can access the key management group.</returns>
        public static GXSelectArgs GetUsersByKeyManagement(string userId, Guid? keyId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupKeyManagementGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupKeyManagementGroup, GXKeyManagementGroup>(a => a.KeyManagementGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXKeyManagementGroup, GXKeyManagementGroupKeyManagement>(a => a.Id, b => b.KeyManagementGroupId);
            args.Joins.AddInnerJoin<GXKeyManagementGroupKeyManagement, GXKeyManagement>(a => a.KeyManagementId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXKeyManagementGroup>(where => where.Removed == null);
            if (keyId != null && keyId != Guid.Empty)
            {
                args.Where.And<GXKeyManagement>(where => where.Removed == null && where.Id == keyId);
            }
            else
            {
                args.Where.And<GXKeyManagement>(where => where.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the key management.
        /// </summary>
        /// <param name="userId">UserId</param>
        /// <param name="keyIds">KeyManagement ids.</param>
        /// <returns>List of users who can access the key management group.</returns>
        public static GXSelectArgs GetUsersByKeyManagements(string userId, IEnumerable<Guid>? keyIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupKeyManagementGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupKeyManagementGroup, GXKeyManagementGroup>(a => a.KeyManagementGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXKeyManagementGroup, GXKeyManagementGroupKeyManagement>(a => a.Id, b => b.KeyManagementGroupId);
            args.Joins.AddInnerJoin<GXKeyManagementGroupKeyManagement, GXKeyManagement>(a => a.KeyManagementId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXKeyManagementGroup>(where => where.Removed == null);
            if (keyIds != null && keyIds.Any())
            {
                args.Where.And<GXKeyManagement>(where => where.Removed == null && keyIds.Contains(where.Id));
            }
            else
            {
                args.Where.And<GXKeyManagement>(where => where.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Get schedule groups that user can access.
        /// </summary>
        /// <param name="userId">UserId</param>
        /// <param name="groupId">Schedule group id.</param>
        /// <returns>List of schedule groups that user can access.</returns>
        public static GXSelectArgs GetScheduleGroupsByUser(string userId, Guid? groupId = null)
        {
            GXSelectArgs userGroups = GetUserGroupsByUser(userId);
            GXSelectArgs args = GXSelectArgs.Select<GXUserGroupScheduleGroup>(s => s.ScheduleGroupId, q => q.Removed == null);
            args.Distinct = true;
            args.Columns.Clear();
            args.Columns.Add<GXScheduleGroup>();
            args.Joins.AddInnerJoin<GXUserGroupScheduleGroup, GXScheduleGroup>(j => j.ScheduleGroupId, j => j.Id);
            args.Where.And<GXUserGroupScheduleGroup>(q => GXSql.Exists<GXUserGroupScheduleGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id, userGroups));
            if (groupId != null)
            {
                args.Where.And<GXScheduleGroup>(q => q.Id == groupId);
            }
            args.Where.And<GXScheduleGroup>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Returns a collection of users who can access the workflow group.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="groupId">Workflow group ID.</param>
        public static GXSelectArgs GetUsersByScheduleGroup(string userId, Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupScheduleGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupScheduleGroup, GXScheduleGroup>(a => a.ScheduleGroupId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXScheduleGroup>(q => q.Removed == null && q.Id == groupId);
            }
            else
            {
                args.Where.And<GXScheduleGroup>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users who can access the workflow group.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="groupIds">Workflow group IDs.</param>
        public static GXSelectArgs GetUsersByScheduleGroups(string userId, IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupScheduleGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupScheduleGroup, GXScheduleGroup>(a => a.ScheduleGroupId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (groupIds != null && groupIds.Any())
            {
                args.Where.And<GXScheduleGroup>(q => q.Removed == null && groupIds.Contains(q.Id));
            }
            else
            {
                args.Where.And<GXScheduleGroup>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Get schedules that user can access.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="scheduleId">Device Id</param>
        /// <returns>List of schedules that user can access.</returns>
        public static GXSelectArgs GetSchedulesByUser(string userId, Guid? scheduleId = null)
        {
            GXSelectArgs scheduleGroups = GetScheduleGroupsByUser(userId);
            GXSelectArgs args = GXSelectArgs.Select<GXScheduleGroupSchedule>(s => s.ScheduleId, q => q.Removed == null);
            args.Distinct = true;
            args.Columns.Clear();
            args.Columns.Add<GXSchedule>();
            args.Joins.AddInnerJoin<GXScheduleGroupSchedule, GXSchedule>(j => j.ScheduleId, j => j.Id);
            args.Where.And<GXScheduleGroupSchedule>(q => GXSql.Exists<GXScheduleGroupSchedule, GXScheduleGroup>(j => j.ScheduleGroupId, j => j.Id, scheduleGroups));
            if (scheduleId != null)
            {
                args.Where.And<GXSchedule>(q => q.Id == scheduleId);
            }
            args.Where.And<GXSchedule>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the schedule.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="scheduleId">Schedule ID.</param>
        public static GXSelectArgs GetUsersBySchedule(string userId, Guid? scheduleId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupScheduleGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupScheduleGroup, GXScheduleGroup>(a => a.ScheduleGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXScheduleGroup, GXScheduleGroupSchedule>(a => a.Id, b => b.ScheduleGroupId);
            args.Joins.AddInnerJoin<GXScheduleGroupSchedule, GXSchedule>(a => a.ScheduleId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXScheduleGroup>(where => where.Removed == null);
            if (scheduleId != null && scheduleId != Guid.Empty)
            {
                args.Where.And<GXSchedule>(where => where.Removed == null && where.Id == scheduleId);
            }
            else
            {
                args.Where.And<GXSchedule>(where => where.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the schedule.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="scheduleIds">Schedule IDs.</param>
        public static GXSelectArgs GetUsersBySchedules(string userId, IEnumerable<Guid>? scheduleIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupScheduleGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupScheduleGroup, GXScheduleGroup>(a => a.ScheduleGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXScheduleGroup, GXScheduleGroupSchedule>(a => a.Id, b => b.ScheduleGroupId);
            args.Joins.AddInnerJoin<GXScheduleGroupSchedule, GXSchedule>(a => a.ScheduleId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXScheduleGroup>(where => where.Removed == null);
            if (scheduleIds != null && scheduleIds.Any())
            {
                args.Where.And<GXSchedule>(where => where.Removed == null && scheduleIds.Contains(where.Id));
            }
            else
            {
                args.Where.And<GXSchedule>(where => where.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users who can access the schedule.
        /// </summary>
        /// <param name="scheduleIds">Schedule IDs</param>
        public static GXSelectArgs GetUsersBySchedules(IEnumerable<Guid> scheduleIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupScheduleGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupScheduleGroup, GXScheduleGroup>(a => a.ScheduleGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXScheduleGroup, GXScheduleGroupSchedule>(a => a.Id, b => b.ScheduleGroupId);
            args.Joins.AddInnerJoin<GXScheduleGroupSchedule, GXSchedule>(a => a.ScheduleId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXScheduleGroup>(where => where.Removed == null);
            args.Where.And<GXSchedule>(where => where.Removed == null && scheduleIds.Contains(where.Id));
            return args;
        }

        /// <summary>
        /// Get schedules logs that user can access.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="scheduleId">Device Id</param>
        /// <returns>List of schedules that user can access.</returns>
        public static GXSelectArgs GetScheduleLogsByUser(string userId, Guid? scheduleId = null)
        {
            GXSelectArgs args = GetSchedulesByUser(userId);
            args.Columns.Clear();
            args.Columns.Add<GXScheduleLog>();
            args.Joins.AddInnerJoin<GXSchedule, GXScheduleLog>(j => j.Id, j => j.Schedule);
            return args;
        }
        /// <summary>
        /// Get script groups that user can access.
        /// </summary>
        /// <param name="userId">UserId</param>
        /// <param name="groupId">Script group id.</param>
        /// <returns>List of script groups that user can access.</returns>
        public static GXSelectArgs GetScriptGroupsByUser(string userId, Guid? groupId = null)
        {
            GXSelectArgs userGroups = GetUserGroupsByUser(userId);
            GXSelectArgs args = GXSelectArgs.Select<GXUserGroupScriptGroup>(s => s.ScriptGroupId, q => q.Removed == null);
            args.Distinct = true;
            args.Columns.Clear();
            args.Columns.Add<GXScriptGroup>();
            args.Joins.AddInnerJoin<GXUserGroupScriptGroup, GXScriptGroup>(j => j.ScriptGroupId, j => j.Id);
            args.Where.And<GXUserGroupScriptGroup>(q => GXSql.Exists<GXUserGroupScriptGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id, userGroups));
            if (groupId != null)
            {
                args.Where.And<GXScriptGroup>(q => q.Id == groupId);
            }
            args.Where.And<GXScriptGroup>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Returns a collection of users who can access the script group.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="groupId">Script group ID.</param>
        public static GXSelectArgs GetUsersByScriptGroup(string userId, Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupScriptGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupScriptGroup, GXScriptGroup>(a => a.ScriptGroupId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXScriptGroup>(q => q.Removed == null && q.Id == groupId);
            }
            else
            {
                args.Where.And<GXScriptGroup>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users who can access the script group.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="groupIds">Script group ID.</param>
        public static GXSelectArgs GetUsersByScriptGroups(string userId, IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupScriptGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupScriptGroup, GXScriptGroup>(a => a.ScriptGroupId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (groupIds != null && groupIds.Any())
            {
                args.Where.And<GXScriptGroup>(q => q.Removed == null && groupIds.Contains(q.Id));
            }
            else
            {
                args.Where.And<GXScriptGroup>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Get scripts that user can access.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="scriptId">Device Id</param>
        /// <returns>List of scripts that user can access.</returns>
        public static GXSelectArgs GetScriptsByUser(string userId, Guid? scriptId = null)
        {
            GXSelectArgs scriptGroups = GetScriptGroupsByUser(userId);
            GXSelectArgs args = GXSelectArgs.Select<GXScriptGroupScript>(s => s.ScriptId, q => q.Removed == null);
            args.Distinct = true;
            args.Columns.Clear();
            args.Columns.Add<GXScript>();
            args.Joins.AddInnerJoin<GXScriptGroupScript, GXScript>(j => j.ScriptId, j => j.Id);
            args.Where.And<GXScriptGroupScript>(q => GXSql.Exists<GXScriptGroupScript, GXScriptGroup>(j => j.ScriptGroupId, j => j.Id, scriptGroups));
            if (scriptId != null)
            {
                args.Where.And<GXScript>(q => q.Id == scriptId);
            }
            args.Where.And<GXScript>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Returns a collection of users who can access the script.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="scriptId">Script ID.</param>
        public static GXSelectArgs GetUsersByScript(string userId, Guid? scriptId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupScriptGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupScriptGroup, GXScriptGroup>(a => a.ScriptGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXScriptGroup, GXScriptGroupScript>(a => a.Id, b => b.ScriptGroupId);
            args.Joins.AddInnerJoin<GXScriptGroupScript, GXScript>(a => a.ScriptId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXScriptGroup>(where => where.Removed == null);
            args.Where.And<GXScript>(where => where.Removed == null);
            if (scriptId != null)
            {
                args.Where.And<GXScript>(where => where.Id == scriptId);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users who can access the scripts.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="scriptIds">Script IDs.</param>
        public static GXSelectArgs GetUsersByScripts(string userId, IEnumerable<Guid>? scriptIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupScriptGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupScriptGroup, GXScriptGroup>(a => a.ScriptGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXScriptGroup, GXScriptGroupScript>(a => a.Id, b => b.ScriptGroupId);
            args.Joins.AddInnerJoin<GXScriptGroupScript, GXScript>(a => a.ScriptId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXScriptGroup>(where => where.Removed == null);
            args.Where.And<GXScript>(where => where.Removed == null);
            if (scriptIds != null && scriptIds.Any())
            {
                args.Where.And<GXScript>(where => scriptIds.Contains(where.Id));
            }
            return args;
        }

        /// <summary>
        /// Get scripts logs by user.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="scriptId">Device Id</param>
        /// <returns>List of scripts user that user can access.</returns>
        public static GXSelectArgs GetScriptLogsByUser(string userId, Guid? scriptId = null)
        {
            GXSelectArgs args = GetScriptsByUser(userId);
            args.Columns.Clear();
            args.Columns.Add<GXScriptLog>();
            args.Joins.AddInnerJoin<GXScript, GXScriptLog>(j => j.Id, j => j.Script);
            return args;
        }

        /// <summary>
        /// Get key managements errors by user.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="keyId">Key management Id</param>
        /// <returns>List of key managements user that user can access.</returns>
        public static GXSelectArgs GetKeyManagementLogsByUser(string userId, Guid? keyId = null)
        {
            GXSelectArgs args = GetKeyManagementsByUser(userId);
            args.Columns.Clear();
            args.Columns.Add<GXKeyManagementLog>();
            args.Joins.AddInnerJoin<GXKeyManagement, GXKeyManagementLog>(j => j.Id, j => j.KeyManagement);
            return args;
        }

        /// <summary>
        /// Get component view groups that user can access.
        /// </summary>
        /// <param name="userId">UserId</param>
        /// <param name="groupId">Component view group id.</param>
        /// <returns>List of component view groups that user can access.</returns>
        public static GXSelectArgs GetComponentViewGroupsByUser(string userId, Guid? groupId = null)
        {
            GXSelectArgs userGroups = GetUserGroupsByUser(userId);
            GXSelectArgs args = GXSelectArgs.Select<GXUserGroupComponentViewGroup>(s => s.ComponentViewGroupId, q => q.Removed == null);
            args.Distinct = true;
            args.Columns.Clear();
            args.Columns.Add<GXComponentViewGroup>();
            args.Joins.AddInnerJoin<GXUserGroupComponentViewGroup, GXComponentViewGroup>(j => j.ComponentViewGroupId, j => j.Id);
            args.Where.And<GXUserGroupComponentViewGroup>(q => GXSql.Exists<GXUserGroupComponentViewGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id, userGroups));
            if (groupId != null)
            {
                args.Where.And<GXComponentViewGroup>(q => q.Id == groupId);
            }
            args.Where.And<GXComponentViewGroup>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the component view group.
        /// </summary>
        /// <param name="userId">UserId</param>
        /// <param name="groupId">Component view group id.</param>
        /// <returns>List of users who can access the component view group.</returns>
        public static GXSelectArgs GetUsersByComponentViewGroup(string userId, Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupComponentViewGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupComponentViewGroup, GXComponentViewGroup>(a => a.ComponentViewGroupId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXComponentViewGroup>(q => q.Removed == null && q.Id == groupId);
            }
            else
            {
                args.Where.And<GXComponentViewGroup>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the component view group.
        /// </summary>
        /// <param name="userId">UserId</param>
        /// <param name="groupIds">Component view group ids.</param>
        /// <returns>List of users who can access the component view group.</returns>
        public static GXSelectArgs GetUsersByComponentViewGroups(string userId, IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupComponentViewGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupComponentViewGroup, GXComponentViewGroup>(a => a.ComponentViewGroupId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (groupIds != null && groupIds.Any())
            {
                args.Where.And<GXComponentViewGroup>(q => q.Removed == null && groupIds.Contains(q.Id));
            }
            else
            {
                args.Where.And<GXComponentViewGroup>(q => q.Removed == null);
            }
            return args;
        }
        /// <summary>
        /// Get component views that user can access.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="ComponentViewId">Component view Id</param>
        /// <returns>List of component views that user can access.</returns>
        public static GXSelectArgs GetComponentViewsByUser(string userId, Guid? ComponentViewId = null)
        {
            GXSelectArgs ComponentViewGroups = GetComponentViewGroupsByUser(userId);
            GXSelectArgs args = GXSelectArgs.Select<GXComponentViewGroupComponentView>(s => s.ComponentViewId, q => q.Removed == null);
            args.Distinct = true;
            args.Columns.Clear();
            args.Columns.Add<GXComponentView>();
            args.Joins.AddInnerJoin<GXComponentViewGroupComponentView, GXComponentView>(j => j.ComponentViewId, j => j.Id);
            args.Where.And<GXComponentViewGroupComponentView>(q => GXSql.Exists<GXComponentViewGroupComponentView, GXComponentViewGroup>(j => j.ComponentViewGroupId, j => j.Id, ComponentViewGroups));
            if (ComponentViewId != null)
            {
                args.Where.And<GXComponentView>(q => q.Id == ComponentViewId);
            }
            args.Where.And<GXComponentView>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the component view.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="componentViewId">Component view id.</param>
        /// <returns>List of users who can access the component view.</returns>
        public static GXSelectArgs GetUsersByComponentView(string userId, Guid? componentViewId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupComponentViewGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupComponentViewGroup, GXComponentViewGroup>(a => a.ComponentViewGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXComponentViewGroup, GXComponentViewGroupComponentView>(a => a.Id, b => b.ComponentViewGroupId);
            args.Joins.AddInnerJoin<GXComponentViewGroupComponentView, GXComponentView>(a => a.ComponentViewId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXComponentViewGroup>(where => where.Removed == null);
            if (componentViewId != null && componentViewId != Guid.Empty)
            {
                args.Where.And<GXComponentView>(where => where.Removed == null && where.Id == componentViewId);
            }
            else
            {
                args.Where.And<GXComponentView>(where => where.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the component view.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="componentViewIds">Component view id.</param>
        /// <returns>List of users who can access the component view.</returns>
        public static GXSelectArgs GetUsersByComponentViews(string userId, IEnumerable<Guid>? componentViewIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupComponentViewGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupComponentViewGroup, GXComponentViewGroup>(a => a.ComponentViewGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXComponentViewGroup, GXComponentViewGroupComponentView>(a => a.Id, b => b.ComponentViewGroupId);
            args.Joins.AddInnerJoin<GXComponentViewGroupComponentView, GXComponentView>(a => a.ComponentViewId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXComponentViewGroup>(where => where.Removed == null);
            if (componentViewIds != null && componentViewIds.Any())
            {
                args.Where.And<GXComponentView>(where => where.Removed == null && componentViewIds.Contains(where.Id));
            }
            else
            {
                args.Where.And<GXComponentView>(where => where.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Get device template groups that user can access.
        /// </summary>
        /// <param name="userId">UserId</param>
        /// <param name="groupId">Device template group id.</param>
        /// <returns>List of device template groups that user can access.</returns>
        public static GXSelectArgs GetDeviceTemplateGroupsByUser(string userId, Guid? groupId = null)
        {
            GXSelectArgs userGroups = GetUserGroupsByUser(userId);
            GXSelectArgs args = GXSelectArgs.Select<GXUserGroupDeviceTemplateGroup>(s => s.DeviceTemplateGroupId, q => q.Removed == null);
            args.Columns.Clear();
            args.Columns.Add<GXDeviceTemplateGroup>();
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUserGroupDeviceTemplateGroup, GXDeviceTemplateGroup>(j => j.DeviceTemplateGroupId, j => j.Id);
            args.Where.And<GXUserGroupDeviceTemplateGroup>(q => GXSql.Exists<GXUserGroupDeviceTemplateGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id, userGroups));
            if (groupId != null)
            {
                args.Where.And<GXDeviceTemplateGroup>(q => q.Id == groupId);
            }
            args.Where.And<GXDeviceTemplateGroup>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the device template group.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="groupId">Device template group ID.</param>
        public static GXSelectArgs GetUsersByDeviceTemplateGroup(string userId, Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceTemplateGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupDeviceTemplateGroup, GXDeviceTemplateGroup>(a => a.DeviceTemplateGroupId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (groupId != null && groupId != Guid.Empty)
            {
                args.Where.And<GXDeviceTemplateGroup>(q => q.Removed == null && q.Id == groupId);
            }
            else
            {
                args.Where.And<GXDeviceTemplateGroup>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the device template groups.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="groupIds">Device template group IDs.</param>
        public static GXSelectArgs GetUsersByDeviceTemplateGroups(string userId, IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceTemplateGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupDeviceTemplateGroup, GXDeviceTemplateGroup>(a => a.DeviceTemplateGroupId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (groupIds != null && groupIds.Any())
            {
                args.Where.And<GXDeviceTemplateGroup>(q => q.Removed == null && groupIds.Contains(q.Id));
            }
            else
            {
                args.Where.And<GXDeviceTemplateGroup>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Get device templates that user can access.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="deviceTemplateId">Device template Id</param>
        /// <returns>List of device templates that user can access.</returns>
        public static GXSelectArgs GetDeviceTemplatesByUser(string userId, Guid? deviceTemplateId = null)
        {
            GXSelectArgs groups = GetDeviceTemplateGroupsByUser(userId);
            GXSelectArgs args = GXSelectArgs.Select<GXDeviceTemplateGroupDeviceTemplate>(s => s.DeviceTemplateId, q => q.Removed == null);
            args.Distinct = true;
            args.Columns.Clear();
            args.Columns.Add<GXDeviceTemplate>();
            args.Joins.AddInnerJoin<GXDeviceTemplateGroupDeviceTemplate, GXDeviceTemplate>(j => j.DeviceTemplateId, j => j.Id);
            args.Where.And<GXDeviceTemplateGroupDeviceTemplate>(q => GXSql.Exists<GXDeviceTemplateGroupDeviceTemplate, GXDeviceTemplateGroup>(j => j.DeviceTemplateGroupId, j => j.Id, groups));
            if (deviceTemplateId != null)
            {
                args.Where.And<GXDeviceTemplate>(q => q.Id == deviceTemplateId);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the device template.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="templateId">Device template ID.</param>
        public static GXSelectArgs GetUsersByDeviceTemplate(string userId, Guid? templateId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceTemplateGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupDeviceTemplateGroup, GXDeviceTemplateGroup>(a => a.DeviceTemplateGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXDeviceTemplateGroup, GXDeviceTemplateGroupDeviceTemplate>(a => a.Id, b => b.DeviceTemplateGroupId);
            args.Joins.AddInnerJoin<GXDeviceTemplateGroupDeviceTemplate, GXDeviceTemplate>(a => a.DeviceTemplateId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXDeviceTemplateGroup>(where => where.Removed == null);
            if (templateId != null && templateId != Guid.Empty)
            {
                args.Where.And<GXDeviceTemplate>(where => where.Removed == null && where.Id == templateId);
            }
            else
            {
                args.Where.And<GXDeviceTemplate>(where => where.Removed == null);
            }
            return args;
        }
        /// <summary>
        /// Returns a collection of users that can access the device template.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="templateIds">Device template IDs.</param>
        public static GXSelectArgs GetUsersByDeviceTemplates(string userId, IEnumerable<Guid>? templateIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceTemplateGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupDeviceTemplateGroup, GXDeviceTemplateGroup>(a => a.DeviceTemplateGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXDeviceTemplateGroup, GXDeviceTemplateGroupDeviceTemplate>(a => a.Id, b => b.DeviceTemplateGroupId);
            args.Joins.AddInnerJoin<GXDeviceTemplateGroupDeviceTemplate, GXDeviceTemplate>(a => a.DeviceTemplateId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXDeviceTemplateGroup>(where => where.Removed == null);
            if (templateIds != null && templateIds.Any())
            {
                args.Where.And<GXDeviceTemplate>(where => where.Removed == null && templateIds.Contains(where.Id));
            }
            else
            {
                args.Where.And<GXDeviceTemplate>(where => where.Removed == null);
            }
            return args;
        }
        /// <summary>
        /// Get gateway groups that user can access.
        /// </summary>
        /// <param name="userId">UserId</param>
        /// <param name="groupId">Gateway group id.</param>
        /// <returns>List of gateway groups that user can access.</returns>
        public static GXSelectArgs GetGatewayGroupsByUser(string userId, Guid? groupId = null)
        {
            GXSelectArgs userGroups = GetUserGroupsByUser(userId);
            GXSelectArgs args = GXSelectArgs.Select<GXUserGroupGatewayGroup>(s => s.GatewayGroupId, q => q.Removed == null);
            args.Distinct = true;
            args.Columns.Clear();
            args.Columns.Add<GXGatewayGroup>();
            args.Joins.AddInnerJoin<GXUserGroupGatewayGroup, GXGatewayGroup>(j => j.GatewayGroupId, j => j.Id);
            args.Where.And<GXUserGroupGatewayGroup>(q => GXSql.Exists<GXUserGroupGatewayGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id, userGroups));
            if (groupId != null)
            {
                args.Where.And<GXGatewayGroup>(q => q.Id == groupId);
            }
            args.Where.And<GXGatewayGroup>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the gateway group.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="groupId">Gateway group id.</param>
        /// <returns>List of users who can access the gateway group.</returns>
        public static GXSelectArgs GetUsersByGatewayGroup(string userId, Guid? groupId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupGatewayGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupGatewayGroup, GXGatewayGroup>(a => a.GatewayGroupId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (groupId != null)
            {
                args.Where.And<GXGatewayGroup>(q => q.Removed == null && q.Id == groupId);
            }
            else
            {
                args.Where.And<GXGatewayGroup>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Get gateways that user can access.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="gatewayId">Device Id</param>
        /// <returns>List of gateways that user can access.</returns>
        public static GXSelectArgs GetGatewaysByUser(string userId, Guid? gatewayId = null)
        {
            GXSelectArgs gatewayGroups = GetGatewayGroupsByUser(userId);
            GXSelectArgs args = GXSelectArgs.Select<GXGatewayGroupGateway>(s => s.GatewayId, q => q.Removed == null);
            args.Distinct = true;
            args.Columns.Clear();
            args.Columns.Add<GXGateway>();
            args.Joins.AddInnerJoin<GXGatewayGroupGateway, GXGateway>(j => j.GatewayId, j => j.Id);
            args.Where.And<GXGatewayGroupGateway>(q => GXSql.Exists<GXGatewayGroupGateway, GXGatewayGroup>(j => j.GatewayGroupId, j => j.Id, gatewayGroups));
            if (gatewayId != null)
            {
                args.Where.And<GXGateway>(q => q.Id == gatewayId);
            }
            args.Where.And<GXGateway>(q => q.Removed == null);
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the gateway group.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="groupIds">Gateway group ids.</param>
        /// <returns>List of users who can access the gateway group.</returns>
        public static GXSelectArgs GetUsersByGatewayGroups(string userId, IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupGatewayGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupGatewayGroup, GXGatewayGroup>(a => a.GatewayGroupId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(q => q.Removed == null);
            if (groupIds != null)
            {
                args.Where.And<GXGatewayGroup>(q => q.Removed == null && groupIds.Contains(q.Id));
            }
            else
            {
                args.Where.And<GXGatewayGroup>(q => q.Removed == null);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the gateway.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="gatewayId">Gateway id.</param>
        /// <returns>List of users who can access the gateway.</returns>
        public static GXSelectArgs GetUsersByGateway(string userId, Guid? gatewayId)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupGatewayGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupGatewayGroup, GXGatewayGroup>(a => a.GatewayGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXGatewayGroup, GXGatewayGroupGateway>(a => a.Id, b => b.GatewayGroupId);
            args.Joins.AddInnerJoin<GXGatewayGroupGateway, GXGateway>(a => a.GatewayId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXGatewayGroup>(where => where.Removed == null);
            args.Where.And<GXGateway>(where => where.Removed == null);
            if (gatewayId != null && gatewayId != Guid.Empty)
            {
                args.Where.And<GXGateway>(where => where.Id == gatewayId);
            }
            return args;
        }

        /// <summary>
        /// Returns a collection of users that can access the gateways.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="gatewayIds">Gateway ids.</param>
        /// <returns>List of users who can access the gateway.</returns>
        public static GXSelectArgs GetUsersByGateways(string userId, IEnumerable<Guid>? gatewayIds)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupGatewayGroup>(a => a.Id, b => b.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupGatewayGroup, GXGatewayGroup>(a => a.GatewayGroupId, b => b.Id);
            args.Joins.AddInnerJoin<GXGatewayGroup, GXGatewayGroupGateway>(a => a.Id, b => b.GatewayGroupId);
            args.Joins.AddInnerJoin<GXGatewayGroupGateway, GXGateway>(a => a.GatewayId, b => b.Id);
            args.Where.And<GXUser>(where => where.Removed == null);
            args.Where.And<GXUserGroup>(where => where.Removed == null);
            args.Where.And<GXGatewayGroup>(where => where.Removed == null);
            args.Where.And<GXGateway>(where => where.Removed == null);
            if (gatewayIds != null)
            {
                args.Where.And<GXGateway>(where => gatewayIds.Contains(where.Id));
            }
            return args;
        }

        /// <summary>
        /// Get gateway logs that user can access.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="gatewayId">Device Id</param>
        /// <returns>List of gateways that user can access.</returns>
        public static GXSelectArgs GetGatewayErrorsByUser(string userId, Guid? gatewayId = null)
        {
            GXSelectArgs args = GetGatewaysByUser(userId);
            args.Columns.Clear();
            args.Columns.Add<GXGatewayLog>();
            args.Joins.AddInnerJoin<GXGateway, GXGatewayLog>(j => j.Id, j => j.Gateway);
            return args;
        }
    }
}
