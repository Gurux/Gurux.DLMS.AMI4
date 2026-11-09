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

using System.ComponentModel;

namespace Gurux.DLMS.AMI.Client.Shared
{
    /// <summary>Administrative update status and manual checks.</summary>
    public static class GXUpdatePolicies
    {
        [Description("Administrators can view and check available updates.")]
        public const string Admin = "updates.admin";
    }

    /// <summary>
    /// System log policies.
    /// </summary>
    [Description("User can control system logs.")]
    public static class GXSystemLogPolicies
    {
        /// <summary>
        /// User can view the system logs.
        /// </summary>
        [Description("User can view the system logs.")]
        public const string View = "system-log.view";
        /// <summary>
        /// User can add new system logs.
        /// </summary>
        [Description("User can add new system logs.")]
        public const string Add = "system-log.add";
        /// <summary>
        /// User can clear system logs.
        /// </summary>
        [Description("User can clear system logs.")]
        public const string Clear = "system-log.clear";
        /// <summary>
        /// User can close system logs.
        /// </summary>
        [Description("User can close system logs.")]
        public const string Close = "system-log.close";
    }

    /// <summary>
    /// Device log policies.
    /// </summary>
    [Description("User can control device logs.")]
    public static class GXDeviceLogPolicies
    {
        /// <summary>
        /// User can view the device logs.
        /// </summary>
        [Description("User can view the device logs.")]
        public const string View = "device-log.view";
        /// <summary>
        /// User can add new device logs.
        /// </summary>
        [Description("User can add new device logs.")]
        public const string Add = "device-log.add";
        /// <summary>
        /// User can clear device logs.
        /// </summary>
        [Description("User can clear device logs.")]
        public const string Clear = "device-log.clear";
        /// <summary>
        /// User can close device logs.
        /// </summary>
        [Description("User can close device logs.")]
        public const string Close = "device-log.close";
    }

    /// <summary>
    /// Workflow log policies.
    /// </summary>
    [Description("User can control workflow logs.")]
    public static class GXWorkflowLogPolicies
    {
        /// <summary>
        /// User can view the workflow logs.
        /// </summary>
        [Description("User can view the workflow logs.")]
        public const string View = "workflow-log.view";
        /// <summary>
        /// User can add new workflow logs.
        /// </summary>
        [Description("User can add new workflow logs.")]
        public const string Add = "workflow-log.add";
        /// <summary>
        /// User can clear workflow logs.
        /// </summary>
        [Description("User can clear workflow logs.")]
        public const string Clear = "workflow-log.clear";
        /// <summary>
        /// User can close workflow logs.
        /// </summary>
        [Description("User can close workflow logs.")]
        public const string Close = "workflow-log.close";
    }

    /// <summary>
    /// Schedule log policies.
    /// </summary>
    [Description("User can control schedule logs.")]
    public static class GXScheduleLogPolicies
    {
        /// <summary>
        /// User can view the schedule logs.
        /// </summary>
        [Description("User can view the schedule logs.")]
        public const string View = "schedule-log.view";
        /// <summary>
        /// User can add new schedule logs.
        /// </summary>
        [Description("User can add new schedule logs.")]
        public const string Add = "schedule-log.add";
        /// <summary>
        /// User can clear schedule logs.
        /// </summary>
        [Description("User can clear schedule logs.")]
        public const string Clear = "schedule-log.clear";
        /// <summary>
        /// User can close schedule logs.
        /// </summary>
        [Description("User can close schedule logs.")]
        public const string Close = "schedule-log.close";
    }

    /// <summary>
    /// Script log policies.
    /// </summary>
    [Description("User can control script logs.")]
    public static class GXScriptLogPolicies
    {
        /// <summary>
        /// User can view the script logs.
        /// </summary>
        [Description("User can view the script logs.")]
        public const string View = "script-log.view";
        /// <summary>
        /// User can add new script logs.
        /// </summary>
        [Description("User can add new script logs.")]
        public const string Add = "script-log.add";
        /// <summary>
        /// User can clear script logs.
        /// </summary>
        [Description("User can clear script logs.")]
        public const string Clear = "script-log.clear";
        /// <summary>
        /// User can close script logs.
        /// </summary>
        [Description("User can close script logs.")]
        public const string Close = "script-log.close";
    }

    /// <summary>
    /// User error policies.
    /// </summary>
    [Description("User can control user errors.")]
    public static class GXUserErrorPolicies
    {
        /// <summary>
        /// User can view the user errors.
        /// </summary>
        [Description("User can view the user errors.")]
        public const string View = "user-error.view";
        /// <summary>
        /// User can add new user errors.
        /// </summary>
        [Description("User can add new user errors.")]
        public const string Add = "user-error.add";
        /// <summary>
        /// User can clear user errors.
        /// </summary>
        [Description("User can clear user errors.")]
        public const string Clear = "user-error.clear";
        /// <summary>
        /// User can close user error.
        /// </summary>
        [Description("User can close user errors.")]
        public const string Close = "user-error.close";
    }

    /// <summary>
    /// Module log policies.
    /// </summary>
    [Description("User can control module logs.")]
    public static class GXModuleLogPolicies
    {
        /// <summary>
        /// User can view the module logs.
        /// </summary>
        [Description("User can view the module logs.")]
        public const string View = "module-log.view";
        /// <summary>
        /// User can add new module logs.
        /// </summary>
        [Description("User can add new module logs.")]
        public const string Add = "module-log.add";
        /// <summary>
        /// User can clear module logs.
        /// </summary>
        [Description("User can clear module logs.")]
        public const string Clear = "module-log.clear";
        /// <summary>
        /// User can close module log.
        /// </summary>
        [Description("User can close module logs.")]
        public const string Close = "module-log.close";
    }

    /// <summary>
    /// Agent log policies.
    /// </summary>
    [Description("User can control agent logs.")]
    public static class GXAgentLogPolicies
    {
        /// <summary>
        /// User can view the agent logs.
        /// </summary>
        [Description("User can view the agent logs.")]
        public const string View = "agent-log.view";
        /// <summary>
        /// User can add new agent logs.
        /// </summary>
        [Description("User can add new agent logs.")]
        public const string Add = "agent-log.add";
        /// <summary>
        /// User can clear agent logs.
        /// </summary>
        [Description("User can clear agent logs.")]
        public const string Clear = "agent-log.clear";
        /// <summary>
        /// User can close agent logs.
        /// </summary>
        [Description("User can close agent logs.")]
        public const string Close = "agent-log.close";
    }

    /// <summary>
    /// Agent setting policies.
    /// </summary>
    [Description("Agent can control agent properties.")]
    public static class GXAgentPropertiesPolicies
    {
        /// <summary>
        /// Agent can view the agent properties.
        /// </summary>
        [Description("Agent can view the agent properties.")]
        public const string View = "agent-property.view";
        /// <summary>
        /// Agent can add new agent properties.
        /// </summary>
        [Description("Agent can add new agent properties.")]
        public const string Add = "agent-property.add";
        /// <summary>
        /// Agent can edit agent properties.
        /// </summary>
        [Description("Agent can edit agent properties.")]
        public const string Edit = "agent-property.edit";
        /// <summary>
        /// Agent can delete agent properties.
        /// </summary>
        [Description("Agent can delete agent properties.")]
        public const string Delete = "agent-property.delete";
    }

    /// <summary>
    /// User policies.
    /// </summary>
    [Description("User can control users.")]
    public static class GXUserPolicies
    {
        /// <summary>User can use login events.</summary>
        [Description("User can use login events.")]
        public const string Login = "user.login";
        /// <summary>User can use logout events.</summary>
        [Description("User can use logout events.")]
        public const string Logout = "user.logout";
        /// <summary>User can use account lockout events.</summary>
        [Description("User can use account lockout events.")]
        public const string LockedOut = "user.locked-out";
        /// <summary>
        /// User can view the user information.
        /// </summary>
        [Description("User can view the users.")]
        public const string View = "user.view";
        /// <summary>
        /// User can add a new user.
        /// </summary>
        [Description("User can add new users.")]
        public const string Add = "user.add";
        /// <summary>
        /// User can edit user information.
        /// </summary>
        [Description("User can edit users.")]
        public const string Edit = "user.edit";
        /// <summary>
        /// User can delete user information.
        /// </summary>
        [Description("User can delete users.")]
        public const string Delete = "user.delete";
        /// <summary>
        /// User can customize the UI.
        /// </summary>
        [Description("User can customize the UI.")]
        public const string CustomizeUI = "user.customize-ui";
    }

    /// <summary>
    /// User group policies.
    /// </summary>
    [Description("User can control user groups.")]
    public static class GXUserGroupPolicies
    {
        /// <summary>
        /// User can view the user group information.
        /// </summary>
        [Description("User can view the user groups.")]
        public const string View = "user-group.view";
        /// <summary>
        /// User can add a new user group.
        /// </summary>
        [Description("User can add new user groups.")]
        public const string Add = "user-group.add";
        /// <summary>
        /// User can edit user group.
        /// </summary>
        [Description("User can edit user groups.")]
        public const string Edit = "user-group.edit";
        /// <summary>
        /// User can delete user group.
        /// </summary>
        [Description("User can delete user groups.")]
        public const string Delete = "user-group.delete";
    }

    /// <summary>
    /// Device policies.
    /// </summary>
    [Description("User can control devices.")]
    public static class GXDevicePolicies
    {
        /// <summary>Allows use of device.read-started workflow events.</summary>
        [Description("Allows use of device.read-started workflow events.")]
        public const string ReadStarted = "device.read-started";

        /// <summary>Allows use of device.read-completed workflow events.</summary>
        [Description("Allows use of device.read-completed workflow events.")]
        public const string ReadCompleted = "device.read-completed";

        /// <summary>Allows use of device.read-failed workflow events.</summary>
        [Description("Allows use of device.read-failed workflow events.")]
        public const string ReadFailed = "device.read-failed";

        /// <summary>Allows use of device.read-cancelled workflow events.</summary>
        [Description("Allows use of device.read-cancelled workflow events.")]
        public const string ReadCancelled = "device.read-cancelled";

        /// <summary>Allows use of device.read-finished workflow events.</summary>
        [Description("Allows use of device.read-finished workflow events.")]
        public const string ReadFinished = "device.read-finished";

        /// <summary>Allows use of device.connected workflow events.</summary>
        [Description("Allows use of device.connected workflow events.")]
        public const string Connected = "device.connected";

        /// <summary>Allows use of device.disconnected workflow events.</summary>
        [Description("Allows use of device.disconnected workflow events.")]
        public const string Disconnected = "device.disconnected";

        /// <summary>
        /// User can view the device information.
        /// </summary>
        [Description("User can view the devices.")]
        public const string View = "device.view";
        /// <summary>
        /// User can add a new device.
        /// </summary>
        [Description("User can add new devices.")]
        public const string Add = "device.add";
        /// <summary>
        /// User can edit devices.
        /// </summary>
        [Description("User can edit devices.")]
        public const string Edit = "device.edit";
        /// <summary>
        /// User can delete device.
        /// </summary>
        [Description("User can delete devices.")]
        public const string Delete = "device.delete";
    }

    /// <summary>
    /// Device group policies.
    /// </summary>
    [Description("User can control device groups.")]
    public static class GXDeviceGroupPolicies
    {
        /// <summary>
        /// User can view the device group information.
        /// </summary>
        [Description("User can view the device groups.")]
        public const string View = "device-group.view";
        /// <summary>
        /// User can add a new device group.
        /// </summary>
        [Description("User can add new device groups.")]
        public const string Add = "device-group.add";
        /// <summary>
        /// User can edit device group.
        /// </summary>
        [Description("User can edit device groups.")]
        public const string Edit = "device-group.edit";
        /// <summary>
        /// User can delete device group.
        /// </summary>
        [Description("User can delete device groups.")]
        public const string Delete = "device-group.delete";
    }

    /// <summary>
    /// Schedule policies.
    /// </summary>
    [Description("User can control schedules.")]
    public static class GXSchedulePolicies
    {
        /// <summary>Allows use of schedule.started workflow events.</summary>
        [Description("Allows use of schedule.started workflow events.")]
        public const string Started = "schedule.started";

        /// <summary>Allows use of schedule.completed workflow events.</summary>
        [Description("Allows use of schedule.completed workflow events.")]
        public const string Completed = "schedule.completed";

        /// <summary>Allows use of schedule.failed workflow events.</summary>
        [Description("Allows use of schedule.failed workflow events.")]
        public const string Failed = "schedule.failed";

        /// <summary>
        /// User can view the schedules.
        /// </summary>
        [Description("User can view the schedules.")]
        public const string View = "schedule.view";
        /// <summary>
        /// User can add new schedules.
        /// </summary>
        [Description("User can add new schedules.")]
        public const string Add = "schedule.add";
        /// <summary>
        /// User can edit schedules.
        /// </summary>
        [Description("User can edit schedules.")]
        public const string Edit = "schedule.edit";
        /// <summary>
        /// User can delete schedules.
        /// </summary>
        [Description("User can delete schedules.")]
        public const string Delete = "schedule.delete";
    }

    /// <summary>
    /// Schedule group policies.
    /// </summary>
    [Description("User can control schedule groups.")]
    public static class GXScheduleGroupPolicies
    {
        /// <summary>
        /// User can view the schedule group information.
        /// </summary>
        [Description("User can view the schedule groups.")]
        public const string View = "schedule-group.view";
        /// <summary>
        /// User can add a new schedule group.
        /// </summary>
        [Description("User can add new schedule groups.")]
        public const string Add = "schedule-group.add";
        /// <summary>
        /// User can edit schedule group.
        /// </summary>
        [Description("User can edit schedule groups.")]
        public const string Edit = "schedule-group.edit";
        /// <summary>
        /// User can delete schedule group.
        /// </summary>
        [Description("User can delete schedule groups.")]
        public const string Delete = "schedule-group.delete";
    }

    /// <summary>
    /// Agent policies.
    /// </summary>
    [Description("User can control agents.")]
    public static class GXAgentPolicies
    {
        /// <summary>Requests a software update on the selected agent's registered connections.</summary>
        [Description("Requests a software update on the selected agent.")]
        public const string Update = "agent.update";
        /// <summary>Allows use of agent.connected workflow events.</summary>
        [Description("Allows use of agent.connected workflow events.")]
        public const string Connected = "agent.connected";

        /// <summary>Allows use of agent.disconnected workflow events.</summary>
        [Description("Allows use of agent.disconnected workflow events.")]
        public const string Disconnected = "agent.disconnected";

        /// <summary>
        /// User can view the agents.
        /// </summary>
        [Description("User can view the agents.")]
        public const string View = "agent.view";
        /// <summary>
        /// User can add new agents.
        /// </summary>
        [Description("User can add new agents.")]
        public const string Add = "agent.add";
        /// <summary>
        /// User can edit agents.
        /// </summary>
        [Description("User can edit agents.")]
        public const string Edit = "agent.edit";
        /// <summary>
        /// User can delete agents.
        /// </summary>
        [Description("User can delete agents.")]
        public const string Delete = "agent.delete";
    }

    /// <summary>
    /// Agent group policies.
    /// </summary>
    [Description("User can control agent groups.")]
    public static class GXAgentGroupPolicies
    {
        /// <summary>
        /// User can view the agent group information.
        /// </summary>
        [Description("User can view the agent groups.")]
        public const string View = "agent-group.view";
        /// <summary>
        /// User can add a new agent group.
        /// </summary>
        [Description("User can add new agent groups.")]
        public const string Add = "agent-group.add";
        /// <summary>
        /// User can edit agent group.
        /// </summary>
        [Description("User can edit agent groups.")]
        public const string Edit = "agent-group.edit";
        /// <summary>
        /// User can delete agent group.
        /// </summary>
        [Description("User can delete agent groups.")]
        public const string Delete = "agent-group.delete";
    }

    /// <summary>
    /// Block policies.
    /// </summary>
    [Description("User can control blocks.")]
    public static class GXBlockPolicies
    {
        /// <summary>
        /// User can view the blocks.
        /// </summary>
        [Description("User can view the blocks.")]
        public const string View = "block.view";
        /// <summary>
        /// User can add new blocks.
        /// </summary>
        [Description("User can add new blocks.")]
        public const string Add = "block.add";
        /// <summary>
        /// User can edit blocks.
        /// </summary>
        [Description("User can edit blocks.")]
        public const string Edit = "block.edit";
        /// <summary>
        /// User can delete blocks.
        /// </summary>
        [Description("User can delete blocks.")]
        public const string Delete = "block.delete";
        /// <summary>
        /// User can close blocks.
        /// </summary>
        [Description("User can close blocks.")]
        public const string Close = "block.close";
        /// <summary>
        /// User can regenerate blocks.
        /// </summary>
        [Description("User can regenerate blocks.")]
        public const string Regenerate = "block.regenerate";
    }

    /// <summary>
    /// Block group policies.
    /// </summary>
    [Description("User can control block groups.")]
    public static class GXBlockGroupPolicies
    {
        /// <summary>
        /// User can view the block group information.
        /// </summary>
        [Description("User can view the block groups.")]
        public const string View = "block-group.view";
        /// <summary>
        /// User can add a new block group.
        /// </summary>
        [Description("User can add new block groups.")]
        public const string Add = "block-group.add";
        /// <summary>
        /// User can edit block group.
        /// </summary>
        [Description("User can edit block groups.")]
        public const string Edit = "block-group.edit";
        /// <summary>
        /// User can delete block group.
        /// </summary>
        [Description("User can delete block groups.")]
        public const string Delete = "block-group.delete";
    }

    /// <summary>
    /// Menu policies.
    /// </summary>
    [Description("User can control menus.")]
    public static class GXMenuPolicies
    {
        /// <summary>
        /// User can view the menus.
        /// </summary>
        [Description("User can view the menus.")]
        public const string View = "menu.view";
        /// <summary>
        /// User can add new menus.
        /// </summary>
        [Description("User can add new menus.")]
        public const string Add = "menu.add";
        /// <summary>
        /// User can edit menus.
        /// </summary>
        [Description("User can edit menus.")]
        public const string Edit = "menu.edit";
        /// <summary>
        /// User can delete menus.
        /// </summary>
        [Description("User can delete menus.")]
        public const string Delete = "menu.delete";
    }

    /// <summary>
    /// Menu group policies.
    /// </summary>
    [Description("User can control menu groups.")]
    public static class GXMenuGroupPolicies
    {
        /// <summary>
        /// User can view the menu group information.
        /// </summary>
        [Description("User can view the menu groups.")]
        public const string View = "menu-group.view";
        /// <summary>
        /// User can add a new menu group.
        /// </summary>
        [Description("User can add new menu groups.")]
        public const string Add = "menu-group.add";
        /// <summary>
        /// User can edit menu group.
        /// </summary>
        [Description("User can edit menu groups.")]
        public const string Edit = "menu-group.edit";
        /// <summary>
        /// User can delete menu group.
        /// </summary>
        [Description("User can delete menu groups.")]
        public const string Delete = "menu-group.delete";
    }

    /// <summary>
    /// Component view policies.
    /// </summary>
    [Description("User can control component views.")]
    public static class GXComponentViewPolicies
    {
        /// <summary>
        /// User can view the component views.
        /// </summary>
        [Description("User can view the component views.")]
        public const string View = "component-view.view";
        /// <summary>
        /// User can add new component views.
        /// </summary>
        [Description("User can add new component views.")]
        public const string Add = "component-view.add";
        /// <summary>
        /// User can edit component views.
        /// </summary>
        [Description("User can edit component views.")]
        public const string Edit = "component-view.edit";
        /// <summary>
        /// User can delete component views.
        /// </summary>
        [Description("User can delete component views.")]
        public const string Delete = "component-view.delete";
        /// <summary>
        /// User can refresh component views.
        /// </summary>
        [Description("User can refresh component views.")]
        public const string Refresh = "component-view.refresh";
    }

    /// <summary>
    /// Component view group policies.
    /// </summary>
    [Description("User can control component view groups.")]
    public static class GXComponentViewGroupPolicies
    {
        /// <summary>
        /// User can view the component view group information.
        /// </summary>
        [Description("User can view the component view groups.")]
        public const string View = "component-view-group.view";
        /// <summary>
        /// User can add a new component view group.
        /// </summary>
        [Description("User can add new component view groups.")]
        public const string Add = "component-view-group.add";
        /// <summary>
        /// User can edit component view group.
        /// </summary>
        [Description("User can edit component view groups.")]
        public const string Edit = "component-view-group.edit";
        /// <summary>
        /// User can delete component view group.
        /// </summary>
        [Description("User can delete component view groups.")]
        public const string Delete = "component-view-group.delete";
    }

    /// <summary>
    /// Configuration policies.
    /// </summary>
    [Description("User can control configurations.")]
    public static class GXConfigurationPolicies
    {
        /// <summary>
        /// User can view the configuration information.
        /// </summary>
        [Description("User can view the configurations.")]
        public const string View = "configuration.view";
        /// <summary>
        /// User can add a new configuration information.
        /// </summary>
        [Description("User can add new configurations.")]
        public const string Add = "configuration.add";
        /// <summary>
        /// User can edit configuration information.
        /// </summary>
        [Description("User can edit configurations.")]
        public const string Edit = "configuration.edit";
        /// <summary>
        /// User can delete configuration information.
        /// </summary>
        [Description("User can delete configurations.")]
        public const string Delete = "configuration.delete";
        /// <summary>
        /// User can run cron.
        /// </summary>
        [Description("User can run cron.")]
        public const string Cron = "configuration.cron";
        /// <summary>
        /// User can run prune.
        /// </summary>
        [Description("User can run prune.")]
        public const string Prune = "configuration.prune";
        /// <summary>
        /// User can check if the database has been changed.
        /// </summary>
        [Description("User can check if the database has been changed.")]
        public const string GetTablesWithSchemaChanges = "configuration.get-tables-with-schema-changes";
    }

    /// <summary>
    /// Device template policies.
    /// </summary>
    [Description("User can control device templates.")]
    public static class GXDeviceTemplatePolicies
    {
        /// <summary>
        /// User can view the device templates.
        /// </summary>
        [Description("User can view the device templates.")]
        public const string View = "device-template.view";
        /// <summary>
        /// User can add new device templates.
        /// </summary>
        [Description("User can add new device templates.")]
        public const string Add = "device-template.add";
        /// <summary>
        /// User can edit device templates.
        /// </summary>
        [Description("User can edit device templates.")]
        public const string Edit = "device-template.edit";
        /// <summary>
        /// User can delete device templates.
        /// </summary>
        [Description("User can delete device templates.")]
        public const string Delete = "device-template.delete";
    }

    /// <summary>
    /// Device template group policies.
    /// </summary>
    [Description("User can control device template groups.")]
    public static class GXDeviceTemplateGroupPolicies
    {
        /// <summary>
        /// User can view the device template group information.
        /// </summary>
        [Description("User can view the device template groups.")]
        public const string View = "device-template-group.view";
        /// <summary>
        /// User can add a new device template group.
        /// </summary>
        [Description("User can add new device template groups.")]
        public const string Add = "device-template-group.add";
        /// <summary>
        /// User can edit device template group.
        /// </summary>
        [Description("User can edit device template groups.")]
        public const string Edit = "device-template-group.edit";
        /// <summary>
        /// User can delete device template group.
        /// </summary>
        [Description("User can delete device template groups.")]
        public const string Delete = "device-template-group.delete";
    }

    /// <summary>
    /// Localization policies.
    /// </summary>
    [Description("User can control localizations.")]
    public static class GXLocalizationPolicies
    {
        /// <summary>
        /// User can view the localized strings.
        /// </summary>
        [Description("User can view the localizations.")]
        public const string View = "localization.view";
        /// <summary>
        /// User can add a new localized string.
        /// </summary>
        [Description("User can add new localizations.")]
        public const string Add = "localization.add";
        /// <summary>
        /// User can edit localized strings.
        /// </summary>
        [Description("User can edit localizations.")]
        public const string Edit = "localization.edit";
        /// <summary>
        /// User can delete localized strings.
        /// </summary>
        [Description("User can delete localizations.")]
        public const string Delete = "localization.delete";
        /// <summary>
        /// User can refresh localized strings.
        /// </summary>
        [Description("User can refresh localizations.")]
        public const string Refresh = "localization.refresh";

    }

    /// <summary>
    /// Module policies.
    /// </summary>
    [Description("User can control modules.")]
    public static class GXModulePolicies
    {
        /// <summary>
        /// User can view the modules.
        /// </summary>
        [Description("User can view the modules.")]
        public const string View = "module.view";
        /// <summary>
        /// User can add new modules.
        /// </summary>
        [Description("User can add new modules.")]
        public const string Add = "module.add";
        /// <summary>
        /// User can edit modules.
        /// </summary>
        [Description("User can edit modules.")]
        public const string Edit = "module.edit";
        /// <summary>
        /// User can delete modules.
        /// </summary>
        [Description("User can delete modules.")]
        public const string Delete = "module.delete";
    }

    /// <summary>
    /// Module group policies.
    /// </summary>
    [Description("User can control module groups.")]
    public static class GXModuleGroupPolicies
    {
        /// <summary>
        /// User can view the module group information.
        /// </summary>
        [Description("User can view the module groups.")]
        public const string View = "module-group.view";
        /// <summary>
        /// User can add a new module group.
        /// </summary>
        [Description("User can add new module groups.")]
        public const string Add = "module-group.add";
        /// <summary>
        /// User can edit module group.
        /// </summary>
        [Description("User can edit module groups.")]
        public const string Edit = "module-group.edit";
        /// <summary>
        /// User can delete module group.
        /// </summary>
        [Description("User can delete module groups.")]
        public const string Delete = "module-group.delete";
    }

    /// <summary>
    /// User setting policies.
    /// </summary>
    [Description("User can control user properties.")]
    public static class GXUserPropertiesPolicies
    {
        /// <summary>
        /// User can view the user properties.
        /// </summary>
        [Description("User can view the user properties.")]
        public const string View = "user-property.view";
        /// <summary>
        /// User can add new user properties.
        /// </summary>
        [Description("User can add new user properties.")]
        public const string Add = "user-property.add";
        /// <summary>
        /// User can edit user properties.
        /// </summary>
        [Description("User can edit user properties.")]
        public const string Edit = "user-property.edit";
        /// <summary>
        /// User can delete user properties.
        /// </summary>
        [Description("User can delete user properties.")]
        public const string Delete = "user-property.delete";
    }

    /// <summary>
    /// Script policies.
    /// </summary>
    [Description("User can control scripts.")]
    public static class GXScriptPolicies
    {
        /// <summary>
        /// User can view the scripts.
        /// </summary>
        [Description("User can view the scripts.")]
        public const string View = "script.view";
        /// <summary>
        /// User can add new scripts.
        /// </summary>
        [Description("User can add new scripts.")]
        public const string Add = "script.add";
        /// <summary>
        /// User can edit scripts.
        /// </summary>
        [Description("User can edit scripts.")]
        public const string Edit = "script.edit";
        /// <summary>
        /// User can delete scripts.
        /// </summary>
        [Description("User can delete scripts.")]
        public const string Delete = "script.delete";
        /// <summary>
        /// User can run scripts.
        /// </summary>
        [Description("User can run scripts.")]
        public const string Run = "script.run";
    }

    /// <summary>
    /// Manufacturer policies.
    /// </summary>
    [Description("User can control manufacturers.")]
    public static class GXManufacturerPolicies
    {
        /// <summary>
        /// User can view the manufacturers.
        /// </summary>
        [Description("User can view the manufacturers.")]
        public const string View = "manufacturer.view";
        /// <summary>
        /// User can add new manufacturers.
        /// </summary>
        [Description("User can add new manufacturers.")]
        public const string Add = "manufacturer.add";
        /// <summary>
        /// User can edit manufacturers.
        /// </summary>
        [Description("User can edit manufacturers.")]
        public const string Edit = "manufacturer.edit";
        /// <summary>
        /// User can delete manufacturers.
        /// </summary>
        [Description("User can delete manufacturers.")]
        public const string Delete = "manufacturer.delete";
    }

    /// <summary>
    /// Manufacturer group policies.
    /// </summary>
    [Description("User can control manufacturer groups.")]
    public static class GXManufacturerGroupPolicies
    {
        /// <summary>
        /// User can view the manufacturer groups.
        /// </summary>
        [Description("User can view the manufacturer groups.")]
        public const string View = "manufacturer-group.view";
        /// <summary>
        /// User can add new manufacturer groups.
        /// </summary>
        [Description("User can add new manufacturer groups.")]
        public const string Add = "manufacturer-group.add";
        /// <summary>
        /// User can edit manufacturer groups.
        /// </summary>
        [Description("User can edit manufacturer groups.")]
        public const string Edit = "manufacturer-group.edit";
        /// <summary>
        /// User can delete manufacturer groups.
        /// </summary>
        [Description("User can delete manufacturer groups.")]
        public const string Delete = "manufacturer-group.delete";
    }

    /// <summary>
    /// KeyManagement policies.
    /// </summary>
    [Description("User can control key managements.")]
    public static class GXKeyManagementPolicies
    {
        /// <summary>
        /// User can view the key managements.
        /// </summary>
        [Description("User can view the key managements.")]
        public const string View = "key-management.view";
        /// <summary>
        /// User can add new key managements.
        /// </summary>
        [Description("User can add new key managements.")]
        public const string Add = "key-management.add";
        /// <summary>
        /// User can edit key managements.
        /// </summary>
        [Description("User can edit key managements.")]
        public const string Edit = "key-management.edit";
        /// <summary>
        /// User can delete key managements.
        /// </summary>
        [Description("User can delete key managements.")]
        public const string Delete = "key-management.delete";
    }

    /// <summary>
    /// KeyManagement group policies.
    /// </summary>
    [Description("User can control key management groups.")]
    public static class GXKeyManagementGroupPolicies
    {
        /// <summary>
        /// User can view the key management group information.
        /// </summary>
        [Description("User can view the key management groups.")]
        public const string View = "key-management-group.view";
        /// <summary>
        /// User can add a new key management group.
        /// </summary>
        [Description("User can add new key management groups.")]
        public const string Add = "key-management-group.add";
        /// <summary>
        /// User can edit key management group.
        /// </summary>
        [Description("User can edit key management groups.")]
        public const string Edit = "key-management-group.edit";
        /// <summary>
        /// User can delete key management group.
        /// </summary>
        [Description("User can delete key management groups.")]
        public const string Delete = "key-management-group.delete";
    }


    /// <summary>
    /// Key management log policies.
    /// </summary>
    [Description("User can control key management logs.")]
    public static class GXKeyManagementLogPolicies
    {
        /// <summary>
        /// User can view the key management logs.
        /// </summary>
        [Description("User can view the key management logs.")]
        public const string View = "key-management-log.view";
        /// <summary>
        /// User can add new key management logs.
        /// </summary>
        [Description("User can add new key management logs.")]
        public const string Add = "key-management-log.add";
        /// <summary>
        /// User can clear key management logs.
        /// </summary>
        [Description("User can clear key management logs.")]
        public const string Clear = "key-management-log.clear";
        /// <summary>
        /// User can close key management logs.
        /// </summary>
        [Description("User can close key management logs.")]
        public const string Close = "key-management-log.close";
    }

    /// <summary>
    /// Script group policies.
    /// </summary>
    [Description("User can control script groups.")]
    public static class GXScriptGroupPolicies
    {
        /// <summary>
        /// User can view the script group information.
        /// </summary>
        [Description("User can view the script groups.")]
        public const string View = "script-group.view";
        /// <summary>
        /// User can add a new script group.
        /// </summary>
        [Description("User can add new script groups.")]
        public const string Add = "script-group.add";
        /// <summary>
        /// User can edit script group.
        /// </summary>
        [Description("User can edit script groups.")]
        public const string Edit = "script-group.edit";
        /// <summary>
        /// User can delete script group.
        /// </summary>
        [Description("User can delete script groups.")]
        public const string Delete = "script-group.delete";
    }
    /// <summary>
    /// Task policies.
    /// </summary>
    [Description("User can control tasks.")]
    public static class GXTaskPolicies
    {
        /// <summary>
        /// User can view the tasks.
        /// </summary>
        [Description("User can view the tasks.")]
        public const string View = "task.view";
        /// <summary>
        /// User can add new tasks.
        /// </summary>
        [Description("User can add new tasks.")]
        public const string Add = "task.add";
        /// <summary>
        /// User can edit tasks.
        /// </summary>
        [Description("User can edit tasks.")]
        public const string Edit = "task.edit";
        /// <summary>
        /// User can delete tasks.
        /// </summary>
        [Description("User can delete tasks.")]
        public const string Delete = "task.delete";
        /// <summary>
        /// User can clear tasks.
        /// </summary>
        [Description("User can clear tasks.")]
        public const string Clear = "task.clear";
    }

    /// <summary>
    /// Token policies.
    /// </summary>
    [Description("User can control tokens.")]
    public static class GXTokenPolicies
    {
        /// <summary>
        /// User can view the tokens.
        /// </summary>
        [Description("User can view the tokens.")]
        public const string View = "token.view";
        /// <summary>
        /// User can add new tokens.
        /// </summary>
        [Description("User can add new tokens.")]
        public const string Add = "token.add";
        /// <summary>
        /// User can edit tokens.
        /// </summary>
        [Description("User can edit tokens.")]
        public const string Edit = "token.edit";
        /// <summary>
        /// User can delete tokens.
        /// </summary>
        [Description("User can delete tokens.")]
        public const string Delete = "token.delete";
    }

    /// <summary>
    /// Trigger policies.
    /// </summary>
    [Description("User can control triggers.")]
    public static class GXTriggerPolicies
    {
        /// <summary>User can use service start events.</summary>
        [Description("User can use service start events.")]
        public const string Start = "trigger.start";
        /// <summary>
        /// User can view the triggers.
        /// </summary>
        [Description("User can view the triggers.")]
        public const string View = "trigger.view";
        /// <summary>
        /// User can add new triggers.
        /// </summary>
        [Description("User can add new triggers.")]
        public const string Add = "trigger.add";
        /// <summary>
        /// User can edit triggers.
        /// </summary>
        [Description("User can edit triggers.")]
        public const string Edit = "trigger.edit";
        /// <summary>
        /// User can delete triggers.
        /// </summary>
        [Description("User can delete triggers.")]
        public const string Delete = "trigger.delete";
    }

    /// <summary>
    /// Trigger group policies.
    /// </summary>
    [Description("User can control trigger groups.")]
    public static class GXTriggerGroupPolicies
    {
        /// <summary>
        /// User can view the trigger group information.
        /// </summary>
        [Description("User can view the trigger groups.")]
        public const string View = "trigger-group.view";
        /// <summary>
        /// User can add a new trigger group.
        /// </summary>
        [Description("User can add new trigger groups.")]
        public const string Add = "trigger-group.add";
        /// <summary>
        /// User can edit trigger group.
        /// </summary>
        [Description("User can edit trigger groups.")]
        public const string Edit = "trigger-group.edit";
        /// <summary>
        /// User can delete trigger group.
        /// </summary>
        [Description("User can delete trigger groups.")]
        public const string Delete = "trigger-group.delete";
    }

    /// <summary>
    /// Workflow policies.
    /// </summary>
    [Description("User can control workflows.")]
    public static class GXWorkflowPolicies
    {
        /// <summary>
        /// User can view the workflows.
        /// </summary>
        [Description("User can view the workflows.")]
        public const string View = "workflow.view";
        /// <summary>User can execute workflows manually.</summary>
        [Description("User can execute workflows manually.")]
        public const string Run = "workflow.execute";
        /// <summary>
        /// User can add new workflows.
        /// </summary>
        [Description("User can add new workflows.")]
        public const string Add = "workflow.add";
        /// <summary>
        /// User can edit workflows.
        /// </summary>
        [Description("User can edit workflows.")]
        public const string Edit = "workflow.edit";
        /// <summary>
        /// User can delete workflows.
        /// </summary>
        [Description("User can delete workflows.")]
        public const string Delete = "workflow.delete";
    }

    /// <summary>
    /// Workflow group policies.
    /// </summary>
    [Description("User can control workflow groups.")]
    public static class GXWorkflowGroupPolicies
    {
        /// <summary>
        /// User can view the workflow group information.
        /// </summary>
        [Description("User can view the workflow groups.")]
        public const string View = "workflow-group.view";
        /// <summary>
        /// User can add a new workflow group.
        /// </summary>
        [Description("User can add new workflow groups.")]
        public const string Add = "workflow-group.add";
        /// <summary>
        /// User can edit workflow group.
        /// </summary>
        [Description("User can edit workflow groups.")]
        public const string Edit = "workflow-group.edit";
        /// <summary>
        /// User can delete workflow group.
        /// </summary>
        [Description("User can delete workflow groups.")]
        public const string Delete = "workflow-group.delete";
    }

    /// <summary>
    /// Device trace policies.
    /// </summary>
    [Description("User can control device traces.")]
    public static class GXDeviceTracePolicies
    {
        /// <summary>
        /// User can view the device traces.
        /// </summary>
        [Description("User can view the device traces.")]
        public const string View = "device-trace.view";
        /// <summary>
        /// User can add new device traces.
        /// </summary>
        [Description("User can add new device traces.")]
        public const string Add = "device-trace.add";
        /// <summary>
        /// User can clear device traces.
        /// </summary>
        [Description("User can clear device traces.")]
        public const string Clear = "device-trace.clear";
    }

    /// <summary>
    /// Device action policies.
    /// </summary>
    [Description("User can control device actions.")]
    public static class GXDeviceActionPolicies
    {
        /// <summary>
        /// User can view the device actions.
        /// </summary>
        [Description("User can view the device actions.")]
        public const string View = "device-action.view";
        /// <summary>
        /// User can add new device actions.
        /// </summary>
        [Description("User can add new device actions.")]
        public const string Add = "device-action.add";
        /// <summary>
        /// User can clear device actions.
        /// </summary>
        [Description("User can clear device actions.")]
        public const string Clear = "device-action.clear";
    }

    /// <summary>
    /// Device setting policies.
    /// </summary>
    [Description("Device can control device properties.")]
    public static class GXDevicePropertiesPolicies
    {
        /// <summary>
        /// Device can view the device properties.
        /// </summary>
        [Description("Device can view the device properties.")]
        public const string View = "device-property.view";
        /// <summary>
        /// Device can add new device properties.
        /// </summary>
        [Description("Device can add new device properties.")]
        public const string Add = "device-property.add";
        /// <summary>
        /// Device can edit device properties.
        /// </summary>
        [Description("Device can edit device properties.")]
        public const string Edit = "device-property.edit";
        /// <summary>
        /// Device can delete device properties.
        /// </summary>
        [Description("Device can delete device properties.")]
        public const string Delete = "device-property.delete";
    }


    /// <summary>
    /// User action policies.
    /// </summary>
    [Description("User can control user actions.")]
    public static class GXUserActionPolicies
    {
        /// <summary>
        /// User can view the user actions.
        /// </summary>
        [Description("User can view the user actions.")]
        public const string View = "user-action.view";
        /// <summary>
        /// User can add new user actions.
        /// </summary>
        [Description("User can add new user actions.")]
        public const string Add = "user-action.add";
        /// <summary>
        /// User can clear user actions.
        /// </summary>
        [Description("User can clear user actions.")]
        public const string Clear = "user-action.clear";
    }

    /// <summary>
    /// Value policies.
    /// </summary>
    [Description("User can control values.")]
    public static class GXValuePolicies
    {
        /// <summary>Allows use of value.received workflow events.</summary>
        [Description("Allows use of value.received workflow events.")]
        public const string Received = "value.received";

        /// <summary>Allows use of value.changed workflow events.</summary>
        [Description("Allows use of value.changed workflow events.")]
        public const string Changed = "value.changed";

        /// <summary>
        /// User can view the value.
        /// </summary>
        [Description("User can view the values.")]
        public const string View = "value.view";
        /// <summary>
        /// User can add new value.
        /// </summary>
        [Description("User can add new values.")]
        public const string Add = "value.add";
        /// <summary>
        /// User can clear values.
        /// </summary>
        [Description("User can clear values.")]
        public const string Clear = "value.clear";
    }

    /// <summary>
    /// Role policies.
    /// </summary>
    [Description("User can control roles.")]
    public static class GXRolePolicies
    {
        /// <summary>
        /// User can view the role.
        /// </summary>
        [Description("User can view the roles.")]
        public const string View = "role.view";
        /// <summary>
        /// User can add new role.
        /// </summary>
        [Description("User can add new roles.")]
        public const string Add = "role.add";
        /// <summary>
        /// User can edit role.
        /// </summary>
        [Description("User can edit roles.")]
        public const string Edit = "role.edit";
        /// <summary>
        /// User can delete roles.
        /// </summary>
        [Description("User can delete roles.")]
        public const string Delete = "role.delete";
    }

    /// <summary>
    /// Object policies.
    /// </summary>
    [Description("User can control objects.")]
    public static class GXObjectPolicies
    {
        /// <summary>
        /// User can view the object.
        /// </summary>
        [Description("User can view the objects.")]
        public const string View = "object.view";
        /// <summary>
        /// User can add new object.
        /// </summary>
        [Description("User can add new objects.")]
        public const string Add = "object.add";
        /// <summary>
        /// User can edit object.
        /// </summary>
        [Description("User can edit objects.")]
        public const string Edit = "object.edit";
        /// <summary>
        /// User can delete objects.
        /// </summary>
        [Description("User can delete objects.")]
        public const string Delete = "object.delete";
        /// <summary>
        /// User can clear objects.
        /// </summary>
        [Description("User can clear objects.")]
        public const string Clear = "object.clear";
    }

    /// <summary>
    /// Object template policies.
    /// </summary>
    [Description("User can control object templates.")]
    public static class GXObjectTemplatePolicies
    {
        /// <summary>
        /// User can view the object template.
        /// </summary>
        [Description("User can view the object templates.")]
        public const string View = "object-template.view";
        /// <summary>
        /// User can add new object template.
        /// </summary>
        [Description("User can add new object templates.")]
        public const string Add = "object-template.add";
        /// <summary>
        /// User can edit object template.
        /// </summary>
        [Description("User can edit object templates.")]
        public const string Edit = "object-template.edit";
        /// <summary>
        /// User can delete objects template.
        /// </summary>
        [Description("User can delete object templates.")]
        public const string Delete = "object-template.delete";
        /// <summary>
        /// User can clear objects template.
        /// </summary>
        [Description("User can clear object templates.")]
        public const string Clear = "object-template.clear";
    }

    /// <summary>
    /// Attribute template policies.
    /// </summary>
    [Description("User can control attribute templates.")]
    public static class GXAttributeTemplatePolicies
    {
        /// <summary>
        /// User can view the attribute template.
        /// </summary>
        [Description("User can view the attribute templates.")]
        public const string View = "attribute-template.view";
        /// <summary>
        /// User can add new attribute template.
        /// </summary>
        [Description("User can add new attribute templates.")]
        public const string Add = "attribute-template.add";
        /// <summary>
        /// User can edit attribute template.
        /// </summary>
        [Description("User can edit attribute templates.")]
        public const string Edit = "attribute-template.edit";
        /// <summary>
        /// User can delete attributes template.
        /// </summary>
        [Description("User can delete attribute templates.")]
        public const string Delete = "attribute-template.delete";
        /// <summary>
        /// User can clear attributes template.
        /// </summary>
        [Description("User can clear attribute templates.")]
        public const string Clear = "attribute-template.clear";
    }

    /// <summary>
    /// Attribute policies.
    /// </summary>
    [Description("User can control attributes.")]
    public static class GXAttributePolicies
    {
        /// <summary>
        /// User can view the attribute.
        /// </summary>
        [Description("User can view the attributes.")]
        public const string View = "attribute.view";
        /// <summary>
        /// User can add new attribute.
        /// </summary>
        [Description("User can add new attributes.")]
        public const string Add = "attribute.add";
        /// <summary>
        /// User can edit attribute.
        /// </summary>
        [Description("User can edit attributes.")]
        public const string Edit = "attribute.edit";
        /// <summary>
        /// User can delete attributes.
        /// </summary>
        [Description("User can delete attributes.")]
        public const string Delete = "attribute.delete";
        /// <summary>
        /// User can clear attributes.
        /// </summary>
        [Description("User can clear attributes.")]
        public const string Clear = "attribute.clear";
    }

    /// <summary>
    /// Option policies.
    /// </summary>
    [Description("User can control options.")]
    public static class GXOptionPolicies
    {
        /// <summary>
        /// User can view the option.
        /// </summary>
        [Description("User can view the options.")]
        public const string View = "option.view";
        /// <summary>
        /// User can add new option.
        /// </summary>
        [Description("User can add new options.")]
        public const string Add = "option.add";
        /// <summary>
        /// User can edit option.
        /// </summary>
        [Description("User can edit options.")]
        public const string Edit = "option.edit";
        /// <summary>
        /// User can delete options.
        /// </summary>
        [Description("User can delete options.")]
        public const string Delete = "option.delete";
        /// <summary>
        /// User can clear options.
        /// </summary>
        [Description("User can clear options.")]
        public const string Clear = "option.clear";
    }

    /// <summary>
    /// Gateway policies.
    /// </summary>
    [Description("User can control gateways.")]
    public static class GXGatewayPolicies
    {
        /// <summary>
        /// User can view the gateways.
        /// </summary>
        [Description("User can view the gateways.")]
        public const string View = "gateway.view";
        /// <summary>
        /// User can add new gateways.
        /// </summary>
        [Description("User can add new gateways.")]
        public const string Add = "gateway.add";
        /// <summary>
        /// User can edit gateways.
        /// </summary>
        [Description("User can edit gateways.")]
        public const string Edit = "gateway.edit";
        /// <summary>
        /// User can delete gateways.
        /// </summary>
        [Description("User can delete gateways.")]
        public const string Delete = "gateway.delete";
    }

    /// <summary>
    /// Gateway group policies.
    /// </summary>
    [Description("User can control gateway groups.")]
    public static class GXGatewayGroupPolicies
    {
        /// <summary>
        /// User can view the gateway group information.
        /// </summary>
        [Description("User can view the gateway groups.")]
        public const string View = "gateway-group.view";
        /// <summary>
        /// User can add a new gateway group.
        /// </summary>
        [Description("User can add new gateway groups.")]
        public const string Add = "gateway-group.add";
        /// <summary>
        /// User can edit gateway group.
        /// </summary>
        [Description("User can edit gateway groups.")]
        public const string Edit = "gateway-group.edit";
        /// <summary>
        /// User can delete gateway group.
        /// </summary>
        [Description("User can delete gateway groups.")]
        public const string Delete = "gateway-group.delete";
    }

    /// <summary>
    /// Gateway log policies.
    /// </summary>
    [Description("User can control gateway logs.")]
    public static class GXGatewayLogPolicies
    {
        /// <summary>
        /// User can view the gateway log.
        /// </summary>
        [Description("User can view the gateway logs.")]
        public const string View = "gateway-log.view";
        /// <summary>
        /// User can add new gateway log.
        /// </summary>
        [Description("User can add new gateway logs.")]
        public const string Add = "gateway-log.add";
        /// <summary>
        /// User can clear gateway log.
        /// </summary>
        [Description("User can clear gateway logs.")]
        public const string Clear = "gateway-log.clear";
        /// <summary>
        /// User can close gateway log.
        /// </summary>
        [Description("User can close gateway logs.")]
        public const string Close = "gateway-log.close";
    }

    /// <summary>
    /// Subtotal policies.
    /// </summary>
    [Description("User can control subtotals.")]
    public static class GXSubtotalPolicies
    {
        /// <summary>
        /// User can view the subtotals.
        /// </summary>
        [Description("User can view the subtotals.")]
        public const string View = "subtotal.view";
        /// <summary>
        /// User can add new subtotals.
        /// </summary>
        [Description("User can add new subtotals.")]
        public const string Add = "subtotal.add";
        /// <summary>
        /// User can edit subtotals.
        /// </summary>
        [Description("User can edit subtotals.")]
        public const string Edit = "subtotal.edit";
        /// <summary>
        /// User can delete subtotals.
        /// </summary>
        [Description("User can delete subtotals.")]
        public const string Delete = "subtotal.delete";
        /// <summary>
        /// User can calculate subtotals.
        /// </summary>
        [Description("User can calculate subtotals.")]
        public const string Calculate = "subtotal.calculate";
        /// <summary>
        /// User can clear subtotals.
        /// </summary>
        [Description("User can clear subtotals.")]
        public const string Clear = "subtotal.clear";
    }

    /// <summary>
    /// Subtotal group policies.
    /// </summary>
    [Description("User can control subtotal groups.")]
    public static class GXSubtotalGroupPolicies
    {
        /// <summary>
        /// User can view the subtotal group information.
        /// </summary>
        [Description("User can view the subtotal groups.")]
        public const string View = "subtotal-group.view";
        /// <summary>
        /// User can add a new subtotal group.
        /// </summary>
        [Description("User can add new subtotal groups.")]
        public const string Add = "subtotal-group.add";
        /// <summary>
        /// User can edit subtotal group.
        /// </summary>
        [Description("User can edit subtotal groups.")]
        public const string Edit = "subtotal-group.edit";
        /// <summary>
        /// User can delete subtotal group.
        /// </summary>
        [Description("User can delete subtotal groups.")]
        public const string Delete = "subtotal-group.delete";
    }

    /// <summary>
    /// Report policies.
    /// </summary>
    [Description("User can control reports.")]
    public static class GXReportPolicies
    {
        /// <summary>
        /// User can view the reports.
        /// </summary>
        [Description("User can view the reports.")]
        public const string View = "report.view";
        /// <summary>
        /// User can add new reports.
        /// </summary>
        [Description("User can add new reports.")]
        public const string Add = "report.add";
        /// <summary>
        /// User can edit reports.
        /// </summary>
        [Description("User can edit reports.")]
        public const string Edit = "report.edit";
        /// <summary>
        /// User can delete reports.
        /// </summary>
        [Description("User can delete reports.")]
        public const string Delete = "report.delete";
        /// <summary>
        /// User can send reports.
        /// </summary>
        [Description("User can send reports.")]
        public const string Send = "report.send";
    }

    /// <summary>
    /// Report group policies.
    /// </summary>
    [Description("User can control report groups.")]
    public static class GXReportGroupPolicies
    {
        /// <summary>
        /// User can view the report group information.
        /// </summary>
        [Description("User can view the report groups.")]
        public const string View = "report-group.view";
        /// <summary>
        /// User can add a new report group.
        /// </summary>
        [Description("User can add new report groups.")]
        public const string Add = "report-group.add";
        /// <summary>
        /// User can edit report group.
        /// </summary>
        [Description("User can edit report groups.")]
        public const string Edit = "report-group.edit";
        /// <summary>
        /// User can delete report group.
        /// </summary>
        [Description("User can delete report groups.")]
        public const string Delete = "report-group.delete";
    }

    /// <summary>
    /// Notification policies.
    /// </summary>
    [Description("User can control notifications.")]
    public static class GXNotificationPolicies
    {
        /// <summary>
        /// User can view the notifications.
        /// </summary>
        [Description("User can view the notifications.")]
        public const string View = GXUserPolicies.View;
        /// <summary>
        /// User can add new notifications.
        /// </summary>
        [Description("User can add new notifications.")]
        public const string Add = "notification.add";
        /// <summary>
        /// User can edit notifications.
        /// </summary>
        [Description("User can edit notifications.")]
        public const string Edit = "notification.edit";
        /// <summary>
        /// User can delete notifications.
        /// </summary>
        [Description("User can delete notifications.")]
        public const string Delete = "notification.delete";
    }

    /// <summary>
    /// Notification type policies.
    /// </summary>
    [Description("User can control notification types.")]
    public static class GXNotificationTypePolicies
    {
        /// <summary>
        /// User can view the notification types.
        /// </summary>
        [Description("User can view the notification types.")]
        public const string View = "notification-type.view";
        /// <summary>
        /// User can add new notification types.
        /// </summary>
        [Description("User can add new notification types.")]
        public const string Add = "notification-type.add";
        /// <summary>
        /// User can edit notification types.
        /// </summary>
        [Description("User can edit notification types.")]
        public const string Edit = "notification-type.edit";
        /// <summary>
        /// User can delete notification types.
        /// </summary>
        [Description("User can delete notification types.")]
        public const string Delete = "notification-type.delete";
    }

    /// <summary>
    /// Appearance policies.
    /// </summary>
    [Description("User can manage appearances.")]
    public static class GXAppearancePolicies
    {
        /// <summary>
        /// User can view appearances.
        /// </summary>
        [Description("User can view appearances.")]
        public const string View = "appearance.view";
        /// <summary>
        /// User can add new appearances.
        /// </summary>
        [Description("User can add new appearances.")]
        public const string Add = "appearance.add";
        /// <summary>
        /// User can edit appearances.
        /// </summary>
        [Description("User can edit appearances.")]
        public const string Edit = "appearance.edit";
        /// <summary>
        /// User can delete appearances.
        /// </summary>
        [Description("User can delete appearances.")]
        public const string Delete = "appearance.delete";
        /// <summary>
        /// User can Regenerate appearances.
        /// </summary>
        [Description("User can regenerate appearances.")]
        public const string Regenerate = "appearance.regenerate";
    }

    /// <summary>
    /// Localized resource policies.
    /// </summary>
    [Description("User can control localized resources.")]
    public static class GXLocalizedResourcePolicies
    {
        /// <summary>
        /// User can view localized resources.
        /// </summary>
        [Description("User can view the localized resources.")]
        public const string View = "localized-resource.view";
        /// <summary>
        /// User can add new localized resources.
        /// </summary>
        [Description("User can add new localized resources.")]
        public const string Add = "localized-resource.add";
        /// <summary>
        /// User can edit localized resources.
        /// </summary>
        [Description("User can edit localized resources.")]
        public const string Edit = "localized-resource.edit";
        /// <summary>
        /// User can delete localized resources.
        /// </summary>
        [Description("User can delete localized resources.")]
        public const string Delete = "localized-resource.delete";
    }

    /// <summary>
    /// Announcement policies.
    /// </summary>
    [Description("User can control announcements.")]
    public static class GXAnnouncementPolicies
    {
        /// <summary>
        /// User can view the announcements.
        /// </summary>
        [Description("User can view the announcements.")]
        public const string View = "annoucement.view";
        /// <summary>
        /// User can add new announcements.
        /// </summary>
        [Description("User can add new announcements.")]
        public const string Add = "annoucement.add";
        /// <summary>
        /// User can edit announcements.
        /// </summary>
        [Description("User can edit announcements.")]
        public const string Edit = "annoucement.edit";
        /// <summary>
        /// User can delete announcements.
        /// </summary>
        [Description("User can delete announcements.")]
        public const string Delete = "annoucement.delete";
        /// <summary>
        /// User can close announcements.
        /// </summary>
        [Description("User can close announcements.")]
        public const string Close = "annoucement.close";
    }

    /// <summary>
    /// Favorite policies.
    /// </summary>
    [Description("User can control favorites.")]
    public static class GXFavoritePolicies
    {
        /// <summary>
        /// User can view the favorites.
        /// </summary>
        [Description("User can view the favorites.")]
        public const string View = GXUserPolicies.View;
        /// <summary>
        /// User can add new favorites.
        /// </summary>
        [Description("User can add new favorites.")]
        public const string Add = "favorite.add";
        /// <summary>
        /// User can delete favorites.
        /// </summary>
        [Description("User can delete favorites.")]
        public const string Delete = "favorite.delete";
    }

    /// <summary>
    /// Language policies.
    /// </summary>
    [Description("User can control languages.")]
    public static class GXLanguagePolicies
    {
        /// <summary>
        /// User can view the languages.
        /// </summary>
        [Description("User can view the languages.")]
        public const string View = "language.view";
        /// <summary>
        /// User can add new languages.
        /// </summary>
        [Description("User can add new languages.")]
        public const string Add = "language.add";
        /// <summary>
        /// User can edit languages.
        /// </summary>
        [Description("User can edit languages.")]
        public const string Edit = "language.edit";
        /// <summary>
        /// User can delete languages.
        /// </summary>
        [Description("User can delete languages.")]
        public const string Delete = "language.delete";
    }

    /// <summary>
    /// Performance policies.
    /// </summary>
    [Description("User can control performance.")]
    public static class GXPerformancePolicies
    {
        /// <summary>
        /// User can view the performance.
        /// </summary>
        [Description("User can view the performance.")]
        public const string View = "performance.view";
        /// <summary>
        /// User can add new performance.
        /// </summary>
        [Description("User can add new performance.")]
        public const string Add = "performance.add";
        /// <summary>
        /// User can edit performance.
        /// </summary>
        [Description("User can edit performance.")]
        public const string Edit = "performance.edit";
        /// <summary>
        /// User can delete performance.
        /// </summary>
        [Description("User can delete performance.")]
        public const string Delete = "performance.delete";
        /// <summary>
        /// User can clear performance.
        /// </summary>
        [Description("User can clear performance.")]
        public const string Clear = "performance.clear";
    }

    /// <summary>
    /// User stamp policies.
    /// </summary>
    [Description("User can control user stamps.")]
    public static class GXUserStampPolicies
    {
        /// <summary>
        /// User can view the user stamps.
        /// </summary>
        [Description("User can view the user stamps.")]
        public const string View = GXUserPolicies.View;
        /// <summary>
        /// User can add new user stamps.
        /// </summary>
        [Description("User can add new user stamps.")]
        public const string Add = "user-stamp.add";
        /// <summary>
        /// User can edit user stamps.
        /// </summary>
        [Description("User can edit user stamps.")]
        public const string Edit = "user-stamp.edit";
        /// <summary>
        /// User can delete user stamps.
        /// </summary>
        [Description("User can delete user stamps.")]
        public const string Delete = "user-stamp.delete";
        /// <summary>
        /// User can clear user stamps.
        /// </summary>
        [Description("User can clear user stamps.")]
        public const string Clear = "user-stamp.clear";
    }

    /// <summary>
    /// Log policies.
    /// </summary>
    [Description("User can control logs.")]
    public static class GXLogPolicies
    {
        /// <summary>
        /// User can view the logs.
        /// </summary>
        [Description("User can view the logs.")]
        public const string View = "log.view";
        /// <summary>
        /// User can add new logs.
        /// </summary>
        [Description("User can add new logs.")]
        public const string Add = "log.add";
        /// <summary>
        /// User can edit logs.
        /// </summary>
        [Description("User can edit logs.")]
        public const string Edit = "log.edit";
        /// <summary>
        /// User can clear logs.
        /// </summary>
        [Description("User can clear logs.")]
        public const string Clear = "log.clear";

        /// <summary>
        /// User can delete logs.
        /// </summary>
        [Description("User can delete logs.")]
        public const string Delete = "log.delete";
        /// <summary>
        /// User can close logs.
        /// </summary>
        [Description("User can close logs.")]
        public const string Close = "log.close";
    }

    /// <summary>
    /// Log type policies.
    /// </summary>
    [Description("User can control log types.")]
    public static class GXLogTypePolicies
    {
        /// <summary>
        /// User can view the log type information.
        /// </summary>
        [Description("User can view the log types.")]
        public const string View = "log-type.view";
        /// <summary>
        /// User can add a new log type.
        /// </summary>
        [Description("User can add new log types.")]
        public const string Add = "log-type.add";
        /// <summary>
        /// User can edit log type.
        /// </summary>
        [Description("User can edit log types.")]
        public const string Edit = "log-type.edit";
        /// <summary>
        /// User can delete log type.
        /// </summary>
        [Description("User can delete log types.")]
        public const string Delete = "log-type.delete";
    }

    /// <summary>
    /// Policy policies.
    /// </summary>
    [Description("User can control policies.")]
    public static class GXPolicyPolicies
    {
        /// <summary>
        /// User can view the policy.
        /// </summary>
        [Description("User can view the policies.")]
        public const string View = "policy.view";
        /// <summary>
        /// User can add new policy.
        /// </summary>
        [Description("User can add new policies.")]
        public const string Add = "policy.add";
        /// <summary>
        /// User can edit policies.
        /// </summary>
        [Description("User can edit policies.")]
        public const string Edit = "policy.edit";
        /// <summary>
        /// User can delete policies.
        /// </summary>
        [Description("User can delete policies.")]
        public const string Delete = "policy.delete";
    }

    /// <summary>Named rights for saved views.</summary>
    [Description("User can control views.")]
    public static class GXViewPolicies
    {
        /// <summary>
        /// User can view the view.
        /// </summary>
        [Description("User can view the view.")]
        public const string View = "view.view";
        /// <summary>
        /// User can add the view.
        /// </summary>
        [Description("User can add the view.")]
        public const string Add = "view.add";
        /// <summary>
        /// User can edit the view.
        /// </summary>
        [Description("User can edit the view.")]
        public const string Edit = "view.edit";
        /// <summary>
        /// User can remove the view.
        /// </summary>
        [Description("User can remove the view.")]
        public const string Delete = "view.delete";
        /// <summary>
        /// User can regenerate the views.
        /// </summary>
        [Description("User can regenerate the views.")]
        public const string Regenerate = "view.regenerate";
    }

    /// <summary>
    /// View group policies.
    /// </summary>
    [Description("User can control view groups.")]
    public static class GXViewGroupPolicies
    {
        /// <summary>
        /// User can view the view group information.
        /// </summary>
        [Description("User can view the view groups.")]
        public const string View = "view-group.view";
        /// <summary>
        /// User can add a new view group.
        /// </summary>
        [Description("User can add new view groups.")]
        public const string Add = "view-group.add";
        /// <summary>
        /// User can edit view group.
        /// </summary>
        [Description("User can edit view groups.")]
        public const string Edit = "view-group.edit";
        /// <summary>
        /// User can delete view group.
        /// </summary>
        [Description("User can delete view groups.")]
        public const string Delete = "view-group.delete";
    }

    /// <summary>
    /// Workflow operation policies.
    /// </summary>
    [Description("User can control workflow operation.")]
    public static class GXWorkflowOperationPolicies
    {
        /// <summary>
        /// User can view the device information.
        /// </summary>
        [Description("User can view workflow operation.")]
        public const string View = "workflow-operation.view";
        /// <summary>
        /// User can workflow operation.
        /// </summary>
        [Description("User can delete workflow operation.")]
        public const string Delete = "workflow-operation.delete";
    }
}
