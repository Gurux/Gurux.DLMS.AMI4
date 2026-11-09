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

namespace Gurux.DLMS.AMI.Client.Shared;

public partial class GXViews
{
    private static readonly HashSet<string> BuiltInNames = new(StringComparer.Ordinal)
    {
        Schedule, ScheduleGroup, ScheduleLog,
        "Admin.LocalizationUpdates", "Admin.SystemLog", "Admin.Tasks", "Admin.Updates", "Admin.UpdateTable",
        "Agent.AgentGroups", "Agent.AgentLogs", "Agent.Agents",
        "Appearance.IconPack", "Appearance.IconPack.Icons", "Appearance.Images", "Appearance.Links", "Appearance.Themes",
        "Block.BlockGroups", "Block.Blocks",
        "ComponentView.ComponentViewGroups", "ComponentView.ComponentViews",
        "Config.AgentInstallers", "Config.Authentications", "Config.ConfigurationList", "Config.DataExchange",
        "Config.Language", "Config.Languages", "Config.Localization", "Config.PolicyList", "Config.RoleEditor",
        "Device.DeviceActions", "Device.DeviceGroups", "Device.DeviceLogs", "Device.Devices", "Device.DeviceTraces",
        "DeviceTemplate.AttributeTemplates", "DeviceTemplate.DeviceTemplateGroups", "DeviceTemplate.DeviceTemplates", "DeviceTemplate.ObjectTemplates", "DeviceTemplate.Profiles",
        "Gateway.GatewayGroups", "Gateway.GatewayLogs", "Gateway.Gateways",
        "KeyManagement.KeyManagementGroups", "KeyManagement.KeyManagementKeys", "KeyManagement.KeyManagementLogs", "KeyManagement.KeyManagements",
        "Manufacturer.Manufacturers", "Manufacturer.Models", "Manufacturer.Settings", "Manufacturer.Versions",
        "Menu.Links", "Menu.MenuGroups", "Menu.Menus",
        "Module.ModuleGroups", "Module.ModuleLogs", "Module.Modules", "Module.ModulesManage",
        "Notification.NotificationGeneral", "Notification.Notifications",
        "Script.ScriptGroups", "Script.ScriptLogs", "Script.Scripts",
        "Trigger.TriggerGroups", "Trigger.Triggers",
        "User.Favorites", "User.UserActions", "User.UserErrors", "User.UserGroups", "User.UserPermissionEditor", "User.UserRoleEditor", "User.Users",
        "View.Columns", "View.ViewGroups", "View.Views",
        "Workflow.WorkflowGroups", "Workflow.WorkflowLogs", "Workflow.Workflows"
    };

    /// <summary>Whether a view has code-defined defaults that can be restored by regeneration.</summary>
    public static bool IsBuiltIn(string? name) => name != null && BuiltInNames.Contains(name);
}
