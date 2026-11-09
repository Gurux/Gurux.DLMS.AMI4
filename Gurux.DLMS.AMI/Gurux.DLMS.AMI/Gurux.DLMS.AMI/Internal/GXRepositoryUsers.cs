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

using System.Data;
using Gurux.DLMS.AMI.Internal;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.View;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Resolves resource users through active group memberships without an administrator bypass.</summary>
internal static class GXRepositoryUsers
{
    /// <summary>Builds the shared membership query used by repositories and notification routing.</summary>
    internal static GXSelectArgs Query(Type resourceType, object? id, bool includeRemovedResource = false)
    {
        var args = GXSelectArgs.Select<GXUser>(u => u.Id!, u => u.Removed == null);
        args.Distinct = true;
        args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(u => u.Id!, m => m.UserId!);
        args.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(m => m.UserGroupId, g => g.Id);
        args.Where.And<GXUserGroupUser>(m => m.Removed == null);
        args.Where.And<GXUserGroup>(g => g.Removed == null);
        if (id is Guid empty && empty == Guid.Empty || id is string text && string.IsNullOrWhiteSpace(text))
        {
            args.Where.And<GXUser>(u => false);
            return args;
        }
        if (resourceType == typeof(GXUserGroup))
        {
            if (id != null)
            { var groupId = (Guid)id; args.Where.And<GXUserGroup>(g => g.Id == groupId); }
            return args;
        }
        if (resourceType == typeof(GXUser))
        {
            ArgumentNullException.ThrowIfNull(id);

            var userId = (string)id;
            var groups = GXSelectArgs.Select<GXUserGroupUser>(m => GXSql.One, m => m.UserId == userId && m.Removed == null);
            args.Where.And<GXUserGroup>(g => GXSql.Exists<GXUserGroup, GXUserGroupUser>(g => g.Id, m => m.UserGroupId, groups));
            return args;
        }
        if (resourceType == typeof(GXAgent) || resourceType == typeof(GXAgentGroup))
        {
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupAgentGroup>(g => g.Id, m => m.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupAgentGroup, GXAgentGroup>(m => m.AgentGroupId, g => g.Id);
            args.Where.And<GXUserGroupAgentGroup>(m => m.Removed == null);
            if (resourceType != typeof(GXAgentGroup) || !includeRemovedResource)
            {
                args.Where.And<GXAgentGroup>(g => g.Removed == null);
            }
        
            if (resourceType == typeof(GXAgentGroup))
            {
                if (id != null)
                {
                    var groupId = (Guid)id;
                    args.Where.And<GXAgentGroup>(g => g.Id == groupId);
                }
        
                return args;
            }
        
            args.Joins.AddInnerJoin<GXAgentGroup, GXAgentGroupAgent>(g => g.Id, m => m.AgentGroupId);
            args.Joins.AddInnerJoin<GXAgentGroupAgent, GXAgent>(m => m.AgentId, r => r.Id);
            args.Where.And<GXAgentGroupAgent>(m => m.Removed == null);
            if (!includeRemovedResource)
            {
                args.Where.And<GXAgent>(r => r.Removed == null);
            }
        
            if (id != null)
            {
                var resourceId = (Guid)id;
                args.Where.And<GXAgent>(r => r.Id == resourceId);
            }
        
            return args;
        }
        if (resourceType == typeof(GXDevice) || resourceType == typeof(GXDeviceGroup) || resourceType == typeof(GXObject) || resourceType == typeof(GXAttribute) || resourceType == typeof(GXDeviceTrace))
        {
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceGroup>(g => g.Id, m => m.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupDeviceGroup, GXDeviceGroup>(m => m.DeviceGroupId, g => g.Id);
            args.Where.And<GXUserGroupDeviceGroup>(m => m.Removed == null);
            if (resourceType != typeof(GXDeviceGroup) || !includeRemovedResource)
            {
                args.Where.And<GXDeviceGroup>(g => g.Removed == null);
            }
        
            if (resourceType == typeof(GXDeviceGroup))
            {
                if (id != null)
                {
                    var groupId = (Guid)id;
                    args.Where.And<GXDeviceGroup>(g => g.Id == groupId);
                }
        
                return args;
            }
        
            args.Joins.AddInnerJoin<GXDeviceGroup, GXDeviceGroupDevice>(g => g.Id, m => m.DeviceGroupId);
            args.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDevice>(m => m.DeviceId, r => r.Id);
            args.Where.And<GXDeviceGroupDevice>(m => m.Removed == null);
            if (resourceType != typeof(GXDevice) || !includeRemovedResource)
            {
                args.Where.And<GXDevice>(r => r.Removed == null);
            }
        
            if (resourceType == typeof(GXObject) || resourceType == typeof(GXAttribute))
            {
                args.Joins.AddInnerJoin<GXDevice, GXObject>(r => r.Id, c => c.Device!);
                if (resourceType != typeof(GXObject) || !includeRemovedResource)
                {
                    args.Where.And<GXObject>(c => c.Removed == null);
                }
            }
        
            if (resourceType == typeof(GXAttribute))
            {
                args.Joins.AddInnerJoin<GXObject, GXAttribute>(r => r.Id, c => c.Object!);
                if (resourceType != typeof(GXAttribute) || !includeRemovedResource)
                {
                    args.Where.And<GXAttribute>(c => c.Removed == null);
                }
            }
        
            if (resourceType == typeof(GXDeviceTrace))
            {
                args.Joins.AddInnerJoin<GXDevice, GXDeviceTrace>(r => r.Id, c => c.Device!);
            }
        
            if (resourceType == typeof(GXObject))
            {
                if (id != null)
                {
                    var resourceId = (Guid)id;
                    args.Where.And<GXObject>(r => r.Id == resourceId);
                }
        
                return args;
            }
        
            if (resourceType == typeof(GXAttribute))
            {
                if (id != null)
                {
                    var resourceId = (Guid)id;
                    args.Where.And<GXAttribute>(r => r.Id == resourceId);
                }
        
                return args;
            }
        
            if (resourceType == typeof(GXDeviceTrace))
            {
                if (id != null)
                {
                    var resourceId = (Guid)id;
                    args.Where.And<GXDeviceTrace>(r => r.Id == resourceId);
                }
        
                return args;
            }
        
            if (id != null)
            {
                var resourceId = (Guid)id;
                args.Where.And<GXDevice>(r => r.Id == resourceId);
            }
        
            return args;
        }
        if (resourceType == typeof(GXDeviceTemplate) || resourceType == typeof(GXDeviceTemplateGroup) || resourceType == typeof(GXObjectTemplate) || resourceType == typeof(GXAttributeTemplate))
        {
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupDeviceTemplateGroup>(g => g.Id, m => m.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupDeviceTemplateGroup, GXDeviceTemplateGroup>(m => m.DeviceTemplateGroupId, g => g.Id);
            args.Where.And<GXUserGroupDeviceTemplateGroup>(m => m.Removed == null);
            if (resourceType != typeof(GXDeviceTemplateGroup) || !includeRemovedResource)
            {
                args.Where.And<GXDeviceTemplateGroup>(g => g.Removed == null);
            }
        
            if (resourceType == typeof(GXDeviceTemplateGroup))
            {
                if (id != null)
                {
                    var groupId = (Guid)id;
                    args.Where.And<GXDeviceTemplateGroup>(g => g.Id == groupId);
                }
        
                return args;
            }
        
            args.Joins.AddInnerJoin<GXDeviceTemplateGroup, GXDeviceTemplateGroupDeviceTemplate>(g => g.Id, m => m.DeviceTemplateGroupId);
            args.Joins.AddInnerJoin<GXDeviceTemplateGroupDeviceTemplate, GXDeviceTemplate>(m => m.DeviceTemplateId, r => r.Id);
            args.Where.And<GXDeviceTemplateGroupDeviceTemplate>(m => m.Removed == null);
            if (resourceType != typeof(GXDeviceTemplate) || !includeRemovedResource)
            {
                args.Where.And<GXDeviceTemplate>(r => r.Removed == null);
            }
        
            if (resourceType == typeof(GXObjectTemplate) || resourceType == typeof(GXAttributeTemplate))
            {
                args.Joins.AddInnerJoin<GXDeviceTemplate, GXObjectTemplate>(r => r.Id, c => c.DeviceTemplate!);
                if (resourceType != typeof(GXObjectTemplate) || !includeRemovedResource)
                {
                    args.Where.And<GXObjectTemplate>(c => c.Removed == null);
                }
            }
        
            if (resourceType == typeof(GXAttributeTemplate))
            {
                args.Joins.AddInnerJoin<GXObjectTemplate, GXAttributeTemplate>(r => r.Id, c => c.ObjectTemplate!);
                if (resourceType != typeof(GXAttributeTemplate) || !includeRemovedResource)
                {
                    args.Where.And<GXAttributeTemplate>(c => c.Removed == null);
                }
            }
        
            if (resourceType == typeof(GXObjectTemplate))
            {
                if (id != null)
                {
                    var resourceId = (Guid)id;
                    args.Where.And<GXObjectTemplate>(r => r.Id == resourceId);
                }
        
                return args;
            }
        
            if (resourceType == typeof(GXAttributeTemplate))
            {
                if (id != null)
                {
                    var resourceId = (Guid)id;
                    args.Where.And<GXAttributeTemplate>(r => r.Id == resourceId);
                }
        
                return args;
            }
        
            if (id != null)
            {
                var resourceId = (Guid)id;
                args.Where.And<GXDeviceTemplate>(r => r.Id == resourceId);
            }
        
            return args;
        }
        if (resourceType == typeof(GXGateway) || resourceType == typeof(GXGatewayGroup))
        {
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupGatewayGroup>(g => g.Id, m => m.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupGatewayGroup, GXGatewayGroup>(m => m.GatewayGroupId, g => g.Id);
            args.Where.And<GXUserGroupGatewayGroup>(m => m.Removed == null);
            if (resourceType != typeof(GXGatewayGroup) || !includeRemovedResource)
            {
                args.Where.And<GXGatewayGroup>(g => g.Removed == null);
            }
        
            if (resourceType == typeof(GXGatewayGroup))
            {
                if (id != null)
                {
                    var groupId = (Guid)id;
                    args.Where.And<GXGatewayGroup>(g => g.Id == groupId);
                }
        
                return args;
            }
        
            args.Joins.AddInnerJoin<GXGatewayGroup, GXGatewayGroupGateway>(g => g.Id, m => m.GatewayGroupId);
            args.Joins.AddInnerJoin<GXGatewayGroupGateway, GXGateway>(m => m.GatewayId, r => r.Id);
            args.Where.And<GXGatewayGroupGateway>(m => m.Removed == null);
            if (!includeRemovedResource)
            {
                args.Where.And<GXGateway>(r => r.Removed == null);
            }
        
            if (id != null)
            {
                var resourceId = (Guid)id;
                args.Where.And<GXGateway>(r => r.Id == resourceId);
            }
        
            return args;
        }
        if (resourceType == typeof(GXKeyManagement) || resourceType == typeof(GXKeyManagementGroup))
        {
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupKeyManagementGroup>(g => g.Id, m => m.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupKeyManagementGroup, GXKeyManagementGroup>(m => m.KeyManagementGroupId, g => g.Id);
            args.Where.And<GXUserGroupKeyManagementGroup>(m => m.Removed == null);
            if (resourceType != typeof(GXKeyManagementGroup) || !includeRemovedResource)
            {
                args.Where.And<GXKeyManagementGroup>(g => g.Removed == null);
            }
        
            if (resourceType == typeof(GXKeyManagementGroup))
            {
                if (id != null)
                {
                    var groupId = (Guid)id;
                    args.Where.And<GXKeyManagementGroup>(g => g.Id == groupId);
                }
        
                return args;
            }
        
            args.Joins.AddInnerJoin<GXKeyManagementGroup, GXKeyManagementGroupKeyManagement>(g => g.Id, m => m.KeyManagementGroupId);
            args.Joins.AddInnerJoin<GXKeyManagementGroupKeyManagement, GXKeyManagement>(m => m.KeyManagementId, r => r.Id);
            args.Where.And<GXKeyManagementGroupKeyManagement>(m => m.Removed == null);
            if (!includeRemovedResource)
            {
                args.Where.And<GXKeyManagement>(r => r.Removed == null);
            }
        
            if (id != null)
            {
                var resourceId = (Guid)id;
                args.Where.And<GXKeyManagement>(r => r.Id == resourceId);
            }
        
            return args;
        }
        if (resourceType == typeof(GXManufacturer) || resourceType == typeof(GXManufacturerGroup))
        {
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupManufacturerGroup>(g => g.Id, m => m.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupManufacturerGroup, GXManufacturerGroup>(m => m.ManufacturerGroupId, g => g.Id);
            args.Where.And<GXUserGroupManufacturerGroup>(m => m.Removed == null);
            if (resourceType != typeof(GXManufacturerGroup) || !includeRemovedResource)
            {
                args.Where.And<GXManufacturerGroup>(g => g.Removed == null);
            }
        
            if (resourceType == typeof(GXManufacturerGroup))
            {
                if (id != null)
                {
                    var groupId = (Guid)id;
                    args.Where.And<GXManufacturerGroup>(g => g.Id == groupId);
                }
        
                return args;
            }
        
            args.Joins.AddInnerJoin<GXManufacturerGroup, GXManufacturerGroupManufacturer>(g => g.Id, m => m.ManufacturerGroupId);
            args.Joins.AddInnerJoin<GXManufacturerGroupManufacturer, GXManufacturer>(m => m.ManufacturerId, r => r.Id);
            args.Where.And<GXManufacturerGroupManufacturer>(m => m.Removed == null);
            if (!includeRemovedResource)
            {
                args.Where.And<GXManufacturer>(r => r.Removed == null);
            }
        
            if (id != null)
            {
                var resourceId = (Guid)id;
                args.Where.And<GXManufacturer>(r => r.Id == resourceId);
            }
        
            return args;
        }
        if (resourceType == typeof(GXMenu) || resourceType == typeof(GXMenuGroup))
        {
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupMenuGroup>(g => g.Id, m => m.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupMenuGroup, GXMenuGroup>(m => m.MenuGroupId, g => g.Id);
            args.Where.And<GXUserGroupMenuGroup>(m => m.Removed == null);
            if (resourceType != typeof(GXMenuGroup) || !includeRemovedResource)
            {
                args.Where.And<GXMenuGroup>(g => g.Removed == null);
            }
        
            if (resourceType == typeof(GXMenuGroup))
            {
                if (id != null)
                {
                    var groupId = (Guid)id;
                    args.Where.And<GXMenuGroup>(g => g.Id == groupId);
                }
        
                return args;
            }
        
            args.Joins.AddInnerJoin<GXMenuGroup, GXMenuGroupMenu>(g => g.Id, m => m.MenuGroupId);
            args.Joins.AddInnerJoin<GXMenuGroupMenu, GXMenu>(m => m.MenuId, r => r.Id);
            args.Where.And<GXMenuGroupMenu>(m => m.Removed == null);
            if (!includeRemovedResource)
            {
                args.Where.And<GXMenu>(r => r.Removed == null);
            }
        
            if (id != null)
            {
                var resourceId = (Guid)id;
                args.Where.And<GXMenu>(r => r.Id == resourceId);
            }
        
            return args;
        }
        if (resourceType == typeof(GXModule) || resourceType == typeof(GXModuleGroup))
        {
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupModuleGroup>(g => g.Id, m => m.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupModuleGroup, GXModuleGroup>(m => m.ModuleGroupId, g => g.Id);
            args.Where.And<GXUserGroupModuleGroup>(m => m.Removed == null);
            if (resourceType != typeof(GXModuleGroup) || !includeRemovedResource)
            {
                args.Where.And<GXModuleGroup>(g => g.Removed == null);
            }
        
            if (resourceType == typeof(GXModuleGroup))
            {
                if (id != null)
                {
                    var groupId = (Guid)id;
                    args.Where.And<GXModuleGroup>(g => g.Id == groupId);
                }
        
                return args;
            }
        
            args.Joins.AddInnerJoin<GXModuleGroup, GXModuleGroupModule>(g => g.Id, m => m.ModuleGroupId);
            args.Joins.AddInnerJoin<GXModuleGroupModule, GXModule>(m => m.ModuleId, r => r.Id);
            args.Where.And<GXModuleGroupModule>(m => m.Removed == null);
            if (id != null)
            {
                var resourceId = (string)id;
                args.Where.And<GXModule>(r => r.Id == resourceId);
            }
        
            return args;
        }
        if (resourceType == typeof(GXSchedule) || resourceType == typeof(GXScheduleGroup))
        {
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupScheduleGroup>(g => g.Id, m => m.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupScheduleGroup, GXScheduleGroup>(m => m.ScheduleGroupId, g => g.Id);
            args.Where.And<GXUserGroupScheduleGroup>(m => m.Removed == null);
            if (resourceType != typeof(GXScheduleGroup) || !includeRemovedResource)
            {
                args.Where.And<GXScheduleGroup>(g => g.Removed == null);
            }
        
            if (resourceType == typeof(GXScheduleGroup))
            {
                if (id != null)
                {
                    var groupId = (Guid)id;
                    args.Where.And<GXScheduleGroup>(g => g.Id == groupId);
                }
        
                return args;
            }
        
            args.Joins.AddInnerJoin<GXScheduleGroup, GXScheduleGroupSchedule>(g => g.Id, m => m.ScheduleGroupId);
            args.Joins.AddInnerJoin<GXScheduleGroupSchedule, GXSchedule>(m => m.ScheduleId, r => r.Id);
            args.Where.And<GXScheduleGroupSchedule>(m => m.Removed == null);
            if (!includeRemovedResource)
            {
                args.Where.And<GXSchedule>(r => r.Removed == null);
            }
        
            if (id != null)
            {
                var resourceId = (Guid)id;
                args.Where.And<GXSchedule>(r => r.Id == resourceId);
            }
        
            return args;
        }
        if (resourceType == typeof(GXScript) || resourceType == typeof(GXScriptGroup))
        {
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupScriptGroup>(g => g.Id, m => m.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupScriptGroup, GXScriptGroup>(m => m.ScriptGroupId, g => g.Id);
            args.Where.And<GXUserGroupScriptGroup>(m => m.Removed == null);
            if (resourceType != typeof(GXScriptGroup) || !includeRemovedResource)
            {
                args.Where.And<GXScriptGroup>(g => g.Removed == null);
            }
        
            if (resourceType == typeof(GXScriptGroup))
            {
                if (id != null)
                {
                    var groupId = (Guid)id;
                    args.Where.And<GXScriptGroup>(g => g.Id == groupId);
                }
        
                return args;
            }
        
            args.Joins.AddInnerJoin<GXScriptGroup, GXScriptGroupScript>(g => g.Id, m => m.ScriptGroupId);
            args.Joins.AddInnerJoin<GXScriptGroupScript, GXScript>(m => m.ScriptId, r => r.Id);
            args.Where.And<GXScriptGroupScript>(m => m.Removed == null);
            if (!includeRemovedResource)
            {
                args.Where.And<GXScript>(r => r.Removed == null);
            }
        
            if (id != null)
            {
                var resourceId = (Guid)id;
                args.Where.And<GXScript>(r => r.Id == resourceId);
            }
        
            return args;
        }
        if (resourceType == typeof(GXTrigger) || resourceType == typeof(GXTriggerGroup))
        {
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupTriggerGroup>(g => g.Id, m => m.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupTriggerGroup, GXTriggerGroup>(m => m.TriggerGroupId, g => g.Id);
            args.Where.And<GXUserGroupTriggerGroup>(m => m.Removed == null);
            if (resourceType != typeof(GXTriggerGroup) || !includeRemovedResource)
            {
                args.Where.And<GXTriggerGroup>(g => g.Removed == null);
            }
        
            if (resourceType == typeof(GXTriggerGroup))
            {
                if (id != null)
                {
                    var groupId = (Guid)id;
                    args.Where.And<GXTriggerGroup>(g => g.Id == groupId);
                }
        
                return args;
            }
        
            args.Joins.AddInnerJoin<GXTriggerGroup, GXTriggerGroupTrigger>(g => g.Id, m => m.TriggerGroupId);
            args.Joins.AddInnerJoin<GXTriggerGroupTrigger, GXTrigger>(m => m.TriggerId, r => r.Id);
            args.Where.And<GXTriggerGroupTrigger>(m => m.Removed == null);
            if (!includeRemovedResource)
            {
                args.Where.And<GXTrigger>(r => r.Removed == null);
            }
        
            if (id != null)
            {
                var resourceId = (Guid)id;
                args.Where.And<GXTrigger>(r => r.Id == resourceId);
            }
        
            return args;
        }
        if (resourceType == typeof(GXWorkflow) || resourceType == typeof(GXWorkflowGroup))
        {
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupWorkflowGroup>(g => g.Id, m => m.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupWorkflowGroup, GXWorkflowGroup>(m => m.WorkflowGroupId, g => g.Id);
            args.Where.And<GXUserGroupWorkflowGroup>(m => m.Removed == null);
            if (resourceType != typeof(GXWorkflowGroup) || !includeRemovedResource)
            {
                args.Where.And<GXWorkflowGroup>(g => g.Removed == null);
            }
        
            if (resourceType == typeof(GXWorkflowGroup))
            {
                if (id != null)
                {
                    var groupId = (Guid)id;
                    args.Where.And<GXWorkflowGroup>(g => g.Id == groupId);
                }
        
                return args;
            }
        
            args.Joins.AddInnerJoin<GXWorkflowGroup, GXWorkflowGroupWorkflow>(g => g.Id, m => m.WorkflowGroupId);
            args.Joins.AddInnerJoin<GXWorkflowGroupWorkflow, GXWorkflow>(m => m.WorkflowId, r => r.Id);
            args.Where.And<GXWorkflowGroupWorkflow>(m => m.Removed == null);
            if (!includeRemovedResource)
            {
                args.Where.And<GXWorkflow>(r => r.Removed == null);
            }
        
            if (id != null)
            {
                var resourceId = (Guid)id;
                args.Where.And<GXWorkflow>(r => r.Id == resourceId);
            }
        
            return args;
        }
        if (resourceType == typeof(GXView) || resourceType == typeof(GXViewGroup))
        {
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupViewGroup>(g => g.Id, m => m.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupViewGroup, GXViewGroup>(m => m.ViewGroupId, g => g.Id);
            args.Where.And<GXUserGroupViewGroup>(m => m.Removed == null);
            if (resourceType != typeof(GXViewGroup) || !includeRemovedResource)
            {
                args.Where.And<GXViewGroup>(g => g.Removed == null);
            }
        
            if (resourceType == typeof(GXViewGroup))
            {
                if (id != null)
                {
                    var groupId = (Guid)id;
                    args.Where.And<GXViewGroup>(g => g.Id == groupId);
                }
        
                return args;
            }
        
            args.Joins.AddInnerJoin<GXViewGroup, GXViewGroupView>(g => g.Id, m => m.ViewGroupId);
            args.Joins.AddInnerJoin<GXViewGroupView, GXView>(m => m.ViewId, r => r.Id);
            args.Where.And<GXViewGroupView>(m => m.Removed == null);
            if (!includeRemovedResource)
            {
                args.Where.And<GXView>(r => r.Removed == null);
            }
        
            if (id != null)
            {
                var resourceId = (Guid)id;
                args.Where.And<GXView>(r => r.Id == resourceId);
            }
        
            return args;
        }
        if (resourceType == typeof(GXBlock) || resourceType == typeof(GXBlockGroup))
        {
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupBlockGroup>(g => g.Id, m => m.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupBlockGroup, GXBlockGroup>(m => m.BlockGroupId, g => g.Id);
            args.Where.And<GXUserGroupBlockGroup>(m => m.Removed == null);
            if (resourceType != typeof(GXBlockGroup) || !includeRemovedResource)
            {
                args.Where.And<GXBlockGroup>(g => g.Removed == null);
            }
        
            if (resourceType == typeof(GXBlockGroup))
            {
                if (id != null)
                {
                    var groupId = (Guid)id;
                    args.Where.And<GXBlockGroup>(g => g.Id == groupId);
                }
        
                return args;
            }
        
            args.Joins.AddInnerJoin<GXBlockGroup, GXBlockGroupBlock>(g => g.Id, m => m.BlockGroupId);
            args.Joins.AddInnerJoin<GXBlockGroupBlock, GXBlock>(m => m.BlockId, r => r.Id);
            args.Where.And<GXBlockGroupBlock>(m => m.Removed == null);
            if (!includeRemovedResource)
            {
                args.Where.And<GXBlock>(r => r.Removed == null);
            }
        
            if (id != null)
            {
                var resourceId = (Guid)id;
                args.Where.And<GXBlock>(r => r.Id == resourceId);
            }
        
            return args;
        }
        if (resourceType == typeof(GXComponentView) || resourceType == typeof(GXComponentViewGroup))
        {
            args.Joins.AddInnerJoin<GXUserGroup, GXUserGroupComponentViewGroup>(g => g.Id, m => m.UserGroupId);
            args.Joins.AddInnerJoin<GXUserGroupComponentViewGroup, GXComponentViewGroup>(m => m.ComponentViewGroupId, g => g.Id);
            args.Where.And<GXUserGroupComponentViewGroup>(m => m.Removed == null);
            if (resourceType != typeof(GXComponentViewGroup) || !includeRemovedResource)
            {
                args.Where.And<GXComponentViewGroup>(g => g.Removed == null);
            }
        
            if (resourceType == typeof(GXComponentViewGroup))
            {
                if (id != null)
                {
                    var groupId = (Guid)id;
                    args.Where.And<GXComponentViewGroup>(g => g.Id == groupId);
                }
        
                return args;
            }
        
            args.Joins.AddInnerJoin<GXComponentViewGroup, GXComponentViewGroupComponentView>(g => g.Id, m => m.ComponentViewGroupId);
            args.Joins.AddInnerJoin<GXComponentViewGroupComponentView, GXComponentView>(m => m.ComponentViewId, r => r.Id);
            args.Where.And<GXComponentViewGroupComponentView>(m => m.Removed == null);
            if (!includeRemovedResource)
            {
                args.Where.And<GXComponentView>(r => r.Removed == null);
            }
        
            if (id != null)
            {
                var resourceId = (Guid)id;
                args.Where.And<GXComponentView>(r => r.Id == resourceId);
            }
        
            return args;
        }
        throw new ArgumentException("The resource has no group membership mapping.", nameof(resourceType));
    }

    /// <summary>Reads the group members for a resource using the caller's transaction.</summary>
    internal static async Task<List<string>> GetUsersAsync(GXDbConnection connection, Type resourceType, object? id,
        IDbTransaction? transaction = null, bool includeRemovedResource = false, CancellationToken token = default)
    {
        if (id is IEnumerable<Guid> identifiers)
        {
            var users = new HashSet<string>(StringComparer.Ordinal);
            foreach (var identifier in identifiers.Distinct())
            {
                users.UnionWith(await GetUsersAsync(connection, resourceType, identifier, transaction, includeRemovedResource, token));
            }

            return [.. users];
        }
        if (resourceType == typeof(GXTask))
        {
            if (id is not Guid taskId || taskId == Guid.Empty)
            {
                return[];
            }
        
            var task = await connection.SingleOrDefaultAsync<GXTask>(transaction, GXSelectArgs.SelectAll<GXTask>(t => t.Id == taskId), token);
            if (task == null)
            {
                return[];
            }
        
            var users = new HashSet<string>(StringComparer.Ordinal);
            foreach (var target in new (Type Type, object? Id)[]
            {
                (typeof(GXDevice), task.Device?.Id),
                (typeof(GXDeviceGroup), task.DeviceGroup?.Id),
                (typeof(GXDevice), task.TargetDevice),
                (typeof(GXAgent), task.TargetAgent),
                (typeof(GXGateway), task.TargetGateway)
            }
        
            )
            {
                if (target.Id != null)
                {
                    users.UnionWith(await GetUsersAsync(connection, target.Type, target.Id, transaction, includeRemovedResource, token));
                }
            }

            return [.. users];
        }
        var query = Query(resourceType, id, includeRemovedResource);
        var rows = await connection.SelectAsync<GXUser>(transaction, query, token);
        return [.. rows.Select(u => u.Id!).Distinct(StringComparer.Ordinal)];
    }

    /// <summary>
    /// Intersects view-authorized recipients with active group membership, including for administrators.
    /// </summary>
    internal static async Task<List<string>> GetNotifiedUsersAsync(IDbConnectionFactory factory, Type resourceType, object id,
        NotificationAction action, string policy, GXDbConnection? connection = null, IDbTransaction? transaction = null, Gurux.DLMS.AMI.Hubs.NotificationCache? notifications = null)
    {
        string operation = action == NotificationAction.Remove ? "delete" : action == NotificationAction.StatusChange ? "edit" : action.ToString().ToLowerInvariant();
        string eventName = policy[..policy.LastIndexOf('.')] + "." + operation;
        if (notifications?.IsEnabled(eventName) == false)
        {
            return [];
        }
        if (connection == null)
        {
            await using var owned = await factory.GetConnectionAsync();
            return await GetNotifiedUsersAsync(factory, resourceType, id, action, policy, owned, notifications: notifications);
        }
        var members = await GetUsersAsync(connection, resourceType, id, transaction, action == NotificationAction.Remove);
        if (members.Count == 0)
        {
            return [];
        }
        if (id is Guid resourceId && (resourceType == typeof(GXScript) || resourceType == typeof(GXTrigger) || resourceType == typeof(GXWorkflow)))
        {
            var names = await GXResourcePolicyStorage.ReadAsync(connection, transaction, resourceType, resourceId);
            if (names.Count != 0)
            {
                var allowed = new HashSet<string>(StringComparer.Ordinal);
                foreach (var name in names)
                {
                    // A missing policy reference is deliberately represented by an invalid name on reads.
                    // It cannot grant recipients, and must never make a restricted resource public.
                    if (name.Length > GXPolicyNames.MaximumNameLength) {
                        continue;
                    }
                    var permitted = GXQuery.GetUsersByPermission(action, name);
                    permitted.Where.And<GXUser>(u => members.Contains(u.Id!));
                    allowed.UnionWith((await connection.SelectAsync<GXUser>(transaction, permitted, CancellationToken.None)).Select(u => u.Id!));
                }
                members = [.. members.Where(allowed.Contains)];
                if (members.Count == 0) {
                    return [];
                }
            }
        }
        var query = GXQuery.GetUsersByPermission(action, policy);
        query.Where.And<GXUser>(u => members.Contains(u.Id!));
        return [.. (await connection.SelectAsync<GXUser>(transaction, query, CancellationToken.None)).Select(u => u.Id!).Distinct(StringComparer.Ordinal)];
    }
}
