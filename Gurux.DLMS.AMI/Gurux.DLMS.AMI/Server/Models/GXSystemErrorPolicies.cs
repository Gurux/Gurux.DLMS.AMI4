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

namespace Gurux.DLMS.AMI.Server.Models
{
    /// <summary>
    /// System error policies.
    /// </summary>
    public static class GXSystemLogPolicies
    {
        /// <summary>
        /// User can view the system errors.
        /// </summary>
        public const string View = "systemerror.view";
        /// <summary>
        /// User can add new system errors.
        /// </summary>
        public const string Add = "systemerror.add";
        /// <summary>
        /// User can clear system errors.
        /// </summary>
        public const string Clear = "systemerror.clear";
        /// <summary>
        /// User can close system error.
        /// </summary>
        public const string Close = "systemerror.close";
    }

    /// <summary>
    /// Device error policies.
    /// </summary>
    public static class GXDeviceErrorPolicies
    {
        /// <summary>
        /// User can view the device errors.
        /// </summary>
        public const string View = "device-error.view";
        /// <summary>
        /// User can add new device errors.
        /// </summary>
        public const string Add = "device-error.add";
        /// <summary>
        /// User can clear device errors.
        /// </summary>
        public const string Clear = "device-error.clear";
        /// <summary>
        /// User can close device error.
        /// </summary>
        public const string Close = "device-error.close";
    }

    /// <summary>
    /// Workflow log policies.
    /// </summary>
    public static class GXWorkflowLogPolicies
    {
        /// <summary>
        /// User can view the workflow log.
        /// </summary>
        public const string View = "workflow-log.view";
        /// <summary>
        /// User can add new workflow log.
        /// </summary>
        public const string Add = "workflow-log.add";
        /// <summary>
        /// User can clear workflow logs.
        /// </summary>
        public const string Clear = "workflow-log.clear";
        /// <summary>
        /// User can close workflow log.
        /// </summary>
        public const string Close = "workflow-log.close";
    }

    /// <summary>
    /// Schedule error policies.
    /// </summary>
    public static class GXScheduleLogPolicies
    {
        /// <summary>
        /// User can view the schedule errors.
        /// </summary>
        public const string View = "schedule-error.view";
        /// <summary>
        /// User can add new schedule errors.
        /// </summary>
        public const string Add = "schedule-error.add";
        /// <summary>
        /// User can clear schedule errors.
        /// </summary>
        public const string Clear = "schedule-error.clear";
        /// <summary>
        /// User can close schedule error.
        /// </summary>
        public const string Close = "schedule-error.close";
    }

    /// <summary>
    /// Script log policies.
    /// </summary>
    public static class GXScriptLogPolicies
    {
        /// <summary>
        /// User can view the script logs.
        /// </summary>
        public const string View = "script-log.view";
        /// <summary>
        /// User can add new script logs.
        /// </summary>
        public const string Add = "script-log.add";
        /// <summary>
        /// User can clear script logs.
        /// </summary>
        public const string Clear = "script-log.clear";
        /// <summary>
        /// User can close script logs.
        /// </summary>
        public const string Close = "script-log.close";
    }

    /// <summary>
    /// User error policies.
    /// </summary>
    public static class GXUserErrorPolicies
    {
        /// <summary>
        /// User can view the user errors.
        /// </summary>
        public const string View = "user-error.view";
        /// <summary>
        /// User can add new user errors.
        /// </summary>
        public const string Add = "user-error.add";
        /// <summary>
        /// User can clear user errors.
        /// </summary>
        public const string Clear = "user-error.clear";
        /// <summary>
        /// User can close user error.
        /// </summary>
        public const string Close = "user-error.close";
    }

    /// <summary>
    /// Module log policies.
    /// </summary>
    public static class GXModuleLogPolicies
    {
        /// <summary>
        /// User can view the module logs.
        /// </summary>
        public const string View = "module-log.view";
        /// <summary>
        /// User can add new module logs.
        /// </summary>
        public const string Add = "module-log.add";
        /// <summary>
        /// User can clear module logs.
        /// </summary>
        public const string Clear = "module-log.clear";
        /// <summary>
        /// User can close module log.
        /// </summary>
        public const string Close = "module-log.close";
    }

    /// <summary>
    /// Agent log policies.
    /// </summary>
    public static class GXAgentLogPolicies
    {
        /// <summary>
        /// User can view the agent log.
        /// </summary>
        public const string View = "agent-log.view";
        /// <summary>
        /// User can add new agent log.
        /// </summary>
        public const string Add = "agent-log.add";
        /// <summary>
        /// User can clear agent log.
        /// </summary>
        public const string Clear = "agent-log.clear";
        /// <summary>
        /// User can close agent log.
        /// </summary>
        public const string Close = "agent-log.close";
    }

    /// <summary>
    /// User policies.
    /// </summary>
    public static class GXUserPolicies
    {
        /// <summary>
        /// User can view the user information.
        /// </summary>
        public const string View = "user.view";
        /// <summary>
        /// User can add a new user.
        /// </summary>
        public const string Add = "user.add";
        /// <summary>
        /// User can edit user information.
        /// </summary>
        public const string Edit = "user.edit";
        /// <summary>
        /// User can delete user information.
        /// </summary>
        public const string Delete = "user.delete";
    }

    /// <summary>
    /// User group policies.
    /// </summary>
    public static class GXUserGroupPolicies
    {
        /// <summary>
        /// User can view the user group information.
        /// </summary>
        public const string View = "user-group.view";
        /// <summary>
        /// User can add a new user group.
        /// </summary>
        public const string Add = "user-group.add";
        /// <summary>
        /// User can edit user group.
        /// </summary>
        public const string Edit = "user-group.edit";
        /// <summary>
        /// User can delete user group.
        /// </summary>
        public const string Delete = "user-group.delete";
    }

    /// <summary>
    /// Device policies.
    /// </summary>
    public static class GXDevicePolicies
    {
        /// <summary>
        /// User can view the device information.
        /// </summary>
        public const string View = "device.view";
        /// <summary>
        /// User can add a new device.
        /// </summary>
        public const string Add = "device.add";
        /// <summary>
        /// User can edit device .
        /// </summary>
        public const string Edit = "device.edit";
        /// <summary>
        /// User can delete device.
        /// </summary>
        public const string Delete = "device.delete";
    }

    /// <summary>
    /// Device group policies.
    /// </summary>
    public static class GXDeviceGroupPolicies
    {
        /// <summary>
        /// User can view the device group information.
        /// </summary>
        public const string View = "device-group.view";
        /// <summary>
        /// User can add a new device group.
        /// </summary>
        public const string Add = "device-group.add";
        /// <summary>
        /// User can edit device group.
        /// </summary>
        public const string Edit = "device-group.edit";
        /// <summary>
        /// User can delete device group.
        /// </summary>
        public const string Delete = "device-group.delete";
    }

    /// <summary>
    /// Schedule policies.
    /// </summary>
    public static class GXSchedulePolicies
    {
        /// <summary>
        /// User can view the schedule(s).
        /// </summary>
        public const string View = "schedule.view";
        /// <summary>
        /// User can add new schedule(s).
        /// </summary>
        public const string Add = "schedule.add";
        /// <summary>
        /// User can edit schedule(s).
        /// </summary>
        public const string Edit = "schedule.edit";
        /// <summary>
        /// User can delete schedule(s).
        /// </summary>
        public const string Delete = "schedule.delete";
    }

    /// <summary>
    /// Schedule group policies.
    /// </summary>
    public static class GXScheduleGroupPolicies
    {
        /// <summary>
        /// User can view the schedule group information.
        /// </summary>
        public const string View = "schedule-group.view";
        /// <summary>
        /// User can add a new schedule group.
        /// </summary>
        public const string Add = "schedule-group.add";
        /// <summary>
        /// User can edit schedule group.
        /// </summary>
        public const string Edit = "schedule-group.edit";
        /// <summary>
        /// User can delete schedule group.
        /// </summary>
        public const string Delete = "schedule-group.delete";
    }

    /// <summary>
    /// Agent policies.
    /// </summary>
    public static class GXAgentPolicies
    {
        /// <summary>
        /// User can view the agent(s).
        /// </summary>
        public const string View = "agent.view";
        /// <summary>
        /// User can add new agent(s).
        /// </summary>
        public const string Add = "agent.add";
        /// <summary>
        /// User can edit agent(s).
        /// </summary>
        public const string Edit = "agent.edit";
        /// <summary>
        /// User can delete agent(s).
        /// </summary>
        public const string Delete = "agent.delete";
    }

    /// <summary>
    /// Agent group policies.
    /// </summary>
    public static class GXAgentGroupPolicies
    {
        /// <summary>
        /// User can view the agent group information.
        /// </summary>
        public const string View = "agent-group.view";
        /// <summary>
        /// User can add a new agent group.
        /// </summary>
        public const string Add = "agent-group.add";
        /// <summary>
        /// User can edit agent group.
        /// </summary>
        public const string Edit = "agent-group.edit";
        /// <summary>
        /// User can delete agent group.
        /// </summary>
        public const string Delete = "agent-group.delete";
    }

    /// <summary>
    /// Block policies.
    /// </summary>
    public static class GXBlockPolicies
    {
        /// <summary>
        /// User can view the block(s).
        /// </summary>
        public const string View = "block.view";
        /// <summary>
        /// User can add new block(s).
        /// </summary>
        public const string Add = "block.add";
        /// <summary>
        /// User can edit block(s).
        /// </summary>
        public const string Edit = "block.edit";
        /// <summary>
        /// User can delete block(s).
        /// </summary>
        public const string Delete = "block.delete";
        /// <summary>
        /// User can close block(s).
        /// </summary>
        public const string Close = "block.close";
    }

    /// <summary>
    /// Block group policies.
    /// </summary>
    public static class GXBlockGroupPolicies
    {
        /// <summary>
        /// User can view the block group information.
        /// </summary>
        public const string View = "block-group.view";
        /// <summary>
        /// User can add a new block group.
        /// </summary>
        public const string Add = "block-group.add";
        /// <summary>
        /// User can edit block group.
        /// </summary>
        public const string Edit = "block-group.edit";
        /// <summary>
        /// User can delete block group.
        /// </summary>
        public const string Delete = "block-group.delete";
    }

    /// <summary>
    /// ComponentView policies.
    /// </summary>
    public static class GXComponentViewPolicies
    {
        /// <summary>
        /// User can view the component view(s).
        /// </summary>
        public const string View = "componentview.view";
        /// <summary>
        /// User can add new component view(s).
        /// </summary>
        public const string Add = "componentview.add";
        /// <summary>
        /// User can edit component view(s).
        /// </summary>
        public const string Edit = "componentview.edit";
        /// <summary>
        /// User can delete component view(s).
        /// </summary>
        public const string Delete = "componentview.delete";
        /// <summary>
        /// User can refresh component view(s).
        /// </summary>
        public const string Refresh = "componentview.refresh";
    }

    /// <summary>
    /// ComponentView group policies.
    /// </summary>
    public static class GXComponentViewGroupPolicies
    {
        /// <summary>
        /// User can view the componentView group information.
        /// </summary>
        public const string View = "componentview-group.view";
        /// <summary>
        /// User can add a new componentView group.
        /// </summary>
        public const string Add = "componentview-group.add";
        /// <summary>
        /// User can edit componentView group.
        /// </summary>
        public const string Edit = "componentview-group.edit";
        /// <summary>
        /// User can delete componentView group.
        /// </summary>
        public const string Delete = "componentview-group.delete";
    }

    /// <summary>
    /// Configuration policies.
    /// </summary>
    public static class GXConfigurationPolicies
    {
        /// <summary>
        /// User can view the configuration information.
        /// </summary>
        public const string View = "configuration.view";
        /// <summary>
        /// User can add a new configuration information.
        /// </summary>
        public const string Add = "configuration.add";
        /// <summary>
        /// User can edit configuration information.
        /// </summary>
        public const string Edit = "configuration.edit";
        /// <summary>
        /// User can delete configuration information.
        /// </summary>
        public const string Delete = "configuration.delete";
        /// <summary>
        /// User can run Cron.
        /// </summary>
        public const string Cron = "configuration.cron";
    }

    /// <summary>
    /// Device template policies.
    /// </summary>
    public static class GXDeviceTemplatePolicies
    {
        /// <summary>
        /// User can view the deviceTemplate(s).
        /// </summary>
        public const string View = "device-template.view";
        /// <summary>
        /// User can add new deviceTemplate(s).
        /// </summary>
        public const string Add = "device-template.add";
        /// <summary>
        /// User can edit deviceTemplate(s).
        /// </summary>
        public const string Edit = "device-template.edit";
        /// <summary>
        /// User can delete deviceTemplate(s).
        /// </summary>
        public const string Delete = "device-template.delete";
    }

    /// <summary>
    /// DeviceTemplate group policies.
    /// </summary>
    public static class GXDeviceTemplateGroupPolicies
    {
        /// <summary>
        /// User can view the deviceTemplate group information.
        /// </summary>
        public const string View = "device-template-group.view";
        /// <summary>
        /// User can add a new deviceTemplate group.
        /// </summary>
        public const string Add = "device-template-group.add";
        /// <summary>
        /// User can edit deviceTemplate group.
        /// </summary>
        public const string Edit = "device-template-group.edit";
        /// <summary>
        /// User can delete deviceTemplate group.
        /// </summary>
        public const string Delete = "device-template-group.delete";
    }

    /// <summary>
    /// Localization policies.
    /// </summary>
    public static class GXLocalizationPolicies
    {
        /// <summary>
        /// User can view the localized strings.
        /// </summary>
        public const string View = "localization.view";
        /// <summary>
        /// User can add a new localized string.
        /// </summary>
        public const string Add = "localization.add";
        /// <summary>
        /// User can edit localized strings.
        /// </summary>
        public const string Edit = "localization.edit";
        /// <summary>
        /// User can delete localized strings.
        /// </summary>
        public const string Delete = "localization.delete";
        /// <summary>
        /// User can refresh localized strings.
        /// </summary>
        public const string Refresh = "localization.refresh";

    }

    /// <summary>
    /// Module policies.
    /// </summary>
    public static class GXModulePolicies
    {
        /// <summary>
        /// User can view the module(s).
        /// </summary>
        public const string View = "module.view";
        /// <summary>
        /// User can add new module(s).
        /// </summary>
        public const string Add = "module.add";
        /// <summary>
        /// User can edit module(s).
        /// </summary>
        public const string Edit = "module.edit";
        /// <summary>
        /// User can delete module(s).
        /// </summary>
        public const string Delete = "module.delete";
    }

    /// <summary>
    /// Module group policies.
    /// </summary>
    public static class GXModuleGroupPolicies
    {
        /// <summary>
        /// User can view the module group information.
        /// </summary>
        public const string View = "module-group.view";
        /// <summary>
        /// User can add a new module group.
        /// </summary>
        public const string Add = "module-group.add";
        /// <summary>
        /// User can edit module group.
        /// </summary>
        public const string Edit = "module-group.edit";
        /// <summary>
        /// User can delete module group.
        /// </summary>
        public const string Delete = "module-group.delete";
    }

    /// <summary>
    /// User setting policies.
    /// </summary>
    public static class GXUserSettingPolicies
    {
        /// <summary>
        /// User can view the user settings.
        /// </summary>
        public const string View = "user-setting.view";
        /// <summary>
        /// User can add new user settings.
        /// </summary>
        public const string Add = "user-setting.add";
        /// <summary>
        /// User can edit user settings.
        /// </summary>
        public const string Edit = "user-setting.edit";
        /// <summary>
        /// User can delete user settings.
        /// </summary>
        public const string Delete = "user-setting.delete";
    }

    /// <summary>
    /// Script policies.
    /// </summary>
    public static class GXScriptPolicies
    {
        /// <summary>
        /// User can view the script(s).
        /// </summary>
        public const string View = "script.view";
        /// <summary>
        /// User can add new script(s).
        /// </summary>
        public const string Add = "script.add";
        /// <summary>
        /// User can edit script(s).
        /// </summary>
        public const string Edit = "script.edit";
        /// <summary>
        /// User can delete script(s).
        /// </summary>
        public const string Delete = "script.delete";
    }

    /// <summary>
    /// Manufacturer policies.
    /// </summary>
    public static class GXManufacturerPolicies
    {
        /// <summary>
        /// User can view the manufacturer(s).
        /// </summary>
        public const string View = "manufacturer.view";
        /// <summary>
        /// User can add new manufacturer(s).
        /// </summary>
        public const string Add = "manufacturer.add";
        /// <summary>
        /// User can edit manufacturer(s).
        /// </summary>
        public const string Edit = "manufacturer.edit";
        /// <summary>
        /// User can delete manufacturer(s).
        /// </summary>
        public const string Delete = "manufacturer.delete";
    }

    /// <summary>
    /// Manufacturer group policies.
    /// </summary>
    public static class GXManufacturerGroupPolicies
    {
        /// <summary>
        /// User can view the manufacturer group(s).
        /// </summary>
        public const string View = "manufacturer-group.view";
        /// <summary>
        /// User can add new manufacturer group(s).
        /// </summary>
        public const string Add = "manufacturer-group.add";
        /// <summary>
        /// User can edit manufacturer group(s).
        /// </summary>
        public const string Edit = "manufacturer-group.edit";
        /// <summary>
        /// User can delete manufacturer group(s).
        /// </summary>
        public const string Delete = "manufacturer-group.delete";
    }

    /// <summary>
    /// KeyManagement policies.
    /// </summary>
    public static class GXKeyManagementPolicies
    {
        /// <summary>
        /// User can view the key management(s).
        /// </summary>
        public const string View = "key-management.view";
        /// <summary>
        /// User can add new key management(s).
        /// </summary>
        public const string Add = "key-management.add";
        /// <summary>
        /// User can edit key management(s).
        /// </summary>
        public const string Edit = "key-management.edit";
        /// <summary>
        /// User can delete key management(s).
        /// </summary>
        public const string Delete = "key-management.delete";
    }

    /// <summary>
    /// KeyManagement group policies.
    /// </summary>
    public static class GXKeyManagementGroupPolicies
    {
        /// <summary>
        /// User can view the key management group information.
        /// </summary>
        public const string View = "key-management-group.view";
        /// <summary>
        /// User can add a new key management group.
        /// </summary>
        public const string Add = "key-management-group.add";
        /// <summary>
        /// User can edit key management group.
        /// </summary>
        public const string Edit = "key-management-group.edit";
        /// <summary>
        /// User can delete key management group.
        /// </summary>
        public const string Delete = "key-management-group.delete";
    }


    /// <summary>
    /// Key management log policies.
    /// </summary>
    public static class GXKeyManagementLogPolicies
    {
        /// <summary>
        /// User can view the key management logs.
        /// </summary>
        public const string View = "key-management-log.view";
        /// <summary>
        /// User can add new key management logs.
        /// </summary>
        public const string Add = "key-management-log.add";
        /// <summary>
        /// User can clear key management logs.
        /// </summary>
        public const string Clear = "key-management-log.clear";
        /// <summary>
        /// User can close key management logs.
        /// </summary>
        public const string Close = "key-management-log.close";
    }

    /// <summary>
    /// Script group policies.
    /// </summary>
    public static class GXScriptGroupPolicies
    {
        /// <summary>
        /// User can view the script group information.
        /// </summary>
        public const string View = "script-group.view";
        /// <summary>
        /// User can add a new script group.
        /// </summary>
        public const string Add = "script-group.add";
        /// <summary>
        /// User can edit script group.
        /// </summary>
        public const string Edit = "script-group.edit";
        /// <summary>
        /// User can delete script group.
        /// </summary>
        public const string Delete = "script-group.delete";
    }
    /// <summary>
    /// Task policies.
    /// </summary>
    public static class GXTaskPolicies
    {
        /// <summary>
        /// User can view the task(s).
        /// </summary>
        public const string View = "task.view";
        /// <summary>
        /// User can add new task(s).
        /// </summary>
        public const string Add = "task.add";
        /// <summary>
        /// User can edit task(s).
        /// </summary>
        public const string Edit = "task.edit";
        /// <summary>
        /// User can delete task(s).
        /// </summary>
        public const string Delete = "task.delete";
    }

    /// <summary>
    /// Token policies.
    /// </summary>
    public static class GXTokenPolicies
    {
        /// <summary>
        /// User can view the token(s).
        /// </summary>
        public const string View = "token.view";
        /// <summary>
        /// User can add new token(s).
        /// </summary>
        public const string Add = "token.add";
        /// <summary>
        /// User can edit token(s).
        /// </summary>
        public const string Edit = "token.edit";
        /// <summary>
        /// User can delete token(s).
        /// </summary>
        public const string Delete = "token.delete";
    }

    /// <summary>
    /// Trigger policies.
    /// </summary>
    public static class GXTriggerPolicies
    {
        /// <summary>
        /// User can view the trigger(s).
        /// </summary>
        public const string View = "trigger.view";
        /// <summary>
        /// User can add new trigger(s).
        /// </summary>
        public const string Add = "trigger.add";
        /// <summary>
        /// User can edit trigger(s).
        /// </summary>
        public const string Edit = "trigger.edit";
        /// <summary>
        /// User can delete trigger(s).
        /// </summary>
        public const string Delete = "trigger.delete";
    }

    /// <summary>
    /// Trigger group policies.
    /// </summary>
    public static class GXTriggerGroupPolicies
    {
        /// <summary>
        /// User can view the trigger group information.
        /// </summary>
        public const string View = "trigger-group.view";
        /// <summary>
        /// User can add a new trigger group.
        /// </summary>
        public const string Add = "trigger-group.add";
        /// <summary>
        /// User can edit trigger group.
        /// </summary>
        public const string Edit = "trigger-group.edit";
        /// <summary>
        /// User can delete trigger group.
        /// </summary>
        public const string Delete = "trigger-group.delete";
    }

    /// <summary>
    /// Workflow policies.
    /// </summary>
    public static class GXWorkflowPolicies
    {
        /// <summary>
        /// User can view the workflow(s).
        /// </summary>
        public const string View = "workflow.view";
        /// <summary>
        /// User can add new workflow(s).
        /// </summary>
        public const string Add = "workflow.add";
        /// <summary>
        /// User can edit workflow(s).
        /// </summary>
        public const string Edit = "workflow.edit";
        /// <summary>
        /// User can delete workflow(s).
        /// </summary>
        public const string Delete = "workflow.delete";
    }

    /// <summary>
    /// Workflow group policies.
    /// </summary>
    public static class GXWorkflowGroupPolicies
    {
        /// <summary>
        /// User can view the workflow group information.
        /// </summary>
        public const string View = "workflow-group.view";
        /// <summary>
        /// User can add a new workflow group.
        /// </summary>
        public const string Add = "workflow-group.add";
        /// <summary>
        /// User can edit workflow group.
        /// </summary>
        public const string Edit = "workflow-group.edit";
        /// <summary>
        /// User can delete workflow group.
        /// </summary>
        public const string Delete = "workflow-group.delete";
    }

    /// <summary>
    /// Device trace policies.
    /// </summary>
    public static class GXDeviceTracePolicies
    {
        /// <summary>
        /// Device can view the device traces.
        /// </summary>
        public const string View = "device-trace.view";
        /// <summary>
        /// Device can add new device traces.
        /// </summary>
        public const string Add = "device-trace.add";
        /// <summary>
        /// Device can clear device traces.
        /// </summary>
        public const string Clear = "device-trace.clear";
    }

    /// <summary>
    /// Device action policies.
    /// </summary>
    public static class GXDeviceActionPolicies
    {
        /// <summary>
        /// Device can view the device actions.
        /// </summary>
        public const string View = "device-action.view";
        /// <summary>
        /// Device can add new device actions.
        /// </summary>
        public const string Add = "device-action.add";
        /// <summary>
        /// Device can clear device actions.
        /// </summary>
        public const string Clear = "device-action.clear";
    }

    /// <summary>
    /// User action policies.
    /// </summary>
    public static class GXUserActionPolicies
    {
        /// <summary>
        /// User can view the user actions.
        /// </summary>
        public const string View = "user-action.view";
        /// <summary>
        /// User can add new user actions.
        /// </summary>
        public const string Add = "user-action.add";
        /// <summary>
        /// User can clear user actions.
        /// </summary>
        public const string Clear = "user-action.clear";
    }

    /// <summary>
    /// Value policies.
    /// </summary>
    public static class GXValuePolicies
    {
        /// <summary>
        /// User can view the value.
        /// </summary>
        public const string View = "value.view";
        /// <summary>
        /// User  can add new value.
        /// </summary>
        public const string Add = "value.add";
        /// <summary>
        /// User  can clear values.
        /// </summary>
        public const string Clear = "value.clear";
    }

    /// <summary>
    /// Subtotal value policies.
    /// </summary>
    public static class GXSubtotalValuePolicies
    {
        /// <summary>
        /// User can view the subtotal value.
        /// </summary>
        public const string View = "SubtotalValue.view";
        /// <summary>
        /// User can add new subtotal value.
        /// </summary>
        public const string Add = "SubtotalValue.add";
        /// <summary>
        /// User can clear subtotal values.
        /// </summary>
        public const string Clear = "SubtotalValue.clear";
    }

    /// <summary>
    /// Subtotal log policies.
    /// </summary>
    public static class GXSubtotalLogPolicies
    {
        /// <summary>
        /// User can view the subtotal log.
        /// </summary>
        public const string View = "subtotal-log.view";
        /// <summary>
        /// User can add new subtotal log.
        /// </summary>
        public const string Add = "subtotal-log.add";
        /// <summary>
        /// User can clear subtotal log.
        /// </summary>
        public const string Clear = "subtotal-log.clear";
        /// <summary>
        /// User can close subtotal log.
        /// </summary>
        public const string Close = "subtotal-log.close";
    }

    /// <summary>
    /// Role policies.
    /// </summary>
    public static class GXRolePolicies
    {
        /// <summary>
        /// User can view the role.
        /// </summary>
        public const string View = "role.view";
        /// <summary>
        /// User  can add new role.
        /// </summary>
        public const string Add = "role.add";
        /// <summary>
        /// User can edit role.
        /// </summary>
        public const string Edit = "role.edit";
        /// <summary>
        /// User can delete roles.
        /// </summary>
        public const string Delete = "role.delete";
    }

    /// <summary>
    /// Object policies.
    /// </summary>
    public static class GXObjectPolicies
    {
        /// <summary>
        /// User can view the object.
        /// </summary>
        public const string View = "object.view";
        /// <summary>
        /// User  can add new object.
        /// </summary>
        public const string Add = "object.add";
        /// <summary>
        /// User can edit object.
        /// </summary>
        public const string Edit = "object.edit";
        /// <summary>
        /// User can delete objects.
        /// </summary>
        public const string Delete = "object.delete";
        /// <summary>
        /// User can clear objects.
        /// </summary>
        public const string Clear = "object.clear";
    }

    /// <summary>
    /// Object template policies.
    /// </summary>
    public static class GXObjectTemplatePolicies
    {
        /// <summary>
        /// User can view the object template.
        /// </summary>
        public const string View = "object-template.view";
        /// <summary>
        /// User  can add new object template.
        /// </summary>
        public const string Add = "object-template.add";
        /// <summary>
        /// User can edit object template.
        /// </summary>
        public const string Edit = "object-template.edit";
        /// <summary>
        /// User can delete objects template.
        /// </summary>
        public const string Delete = "object-template.delete";
        /// <summary>
        /// User can clear objects template.
        /// </summary>
        public const string Clear = "object-template.clear";
    }

    /// <summary>
    /// Attribute template policies.
    /// </summary>
    public static class GXAttributeTemplatePolicies
    {
        /// <summary>
        /// User can view the attribute template.
        /// </summary>
        public const string View = "attribute-template.view";
        /// <summary>
        /// User  can add new attribute template.
        /// </summary>
        public const string Add = "attribute-template.add";
        /// <summary>
        /// User can edit attribute template.
        /// </summary>
        public const string Edit = "attribute-template.edit";
        /// <summary>
        /// User can delete attributes template.
        /// </summary>
        public const string Delete = "attribute-template.delete";
        /// <summary>
        /// User can clear attributes template.
        /// </summary>
        public const string Clear = "attribute-template.clear";
    }

    /// <summary>
    /// Attribute policies.
    /// </summary>
    public static class GXAttributePolicies
    {
        /// <summary>
        /// User can view the attribute.
        /// </summary>
        public const string View = "attribute.view";
        /// <summary>
        /// User  can add new attribute.
        /// </summary>
        public const string Add = "attribute.add";
        /// <summary>
        /// User can edit attribute.
        /// </summary>
        public const string Edit = "attribute.edit";
        /// <summary>
        /// User can delete attributes.
        /// </summary>
        public const string Delete = "attribute.delete";
        /// <summary>
        /// User can clear attributes.
        /// </summary>
        public const string Clear = "attribute.clear";
    }

    /// <summary>
    /// Option policies.
    /// </summary>
    public static class GXOptionPolicies
    {
        /// <summary>
        /// User can view the option.
        /// </summary>
        public const string View = "option.view";
        /// <summary>
        /// User can add new option.
        /// </summary>
        public const string Add = "option.add";
        /// <summary>
        /// User can edit option.
        /// </summary>
        public const string Edit = "option.edit";
        /// <summary>
        /// User can delete options.
        /// </summary>
        public const string Delete = "option.delete";
        /// <summary>
        /// User can clear options.
        /// </summary>
        public const string Clear = "option.clear";
    }

    /// <summary>
    /// Gateway policies.
    /// </summary>
    public static class GXGatewayPolicies
    {
        /// <summary>
        /// User can view the gateway(s).
        /// </summary>
        public const string View = "gateway.view";
        /// <summary>
        /// User can add new gateway(s).
        /// </summary>
        public const string Add = "gateway.add";
        /// <summary>
        /// User can edit gateway(s).
        /// </summary>
        public const string Edit = "gateway.edit";
        /// <summary>
        /// User can delete gateway(s).
        /// </summary>
        public const string Delete = "gateway.delete";
    }

    /// <summary>
    /// Gateway group policies.
    /// </summary>
    public static class GXGatewayGroupPolicies
    {
        /// <summary>
        /// User can view the gateway group information.
        /// </summary>
        public const string View = "gateway-group.view";
        /// <summary>
        /// User can add a new gateway group.
        /// </summary>
        public const string Add = "gateway-group.add";
        /// <summary>
        /// User can edit gateway group.
        /// </summary>
        public const string Edit = "gateway-group.edit";
        /// <summary>
        /// User can delete gateway group.
        /// </summary>
        public const string Delete = "gateway-group.delete";
    }

    /// <summary>
    /// Gateway log policies.
    /// </summary>
    public static class GXGatewayLogPolicies
    {
        /// <summary>
        /// User can view the gateway log.
        /// </summary>
        public const string View = "gateway-log.view";
        /// <summary>
        /// User can add new gateway log.
        /// </summary>
        public const string Add = "gateway-log.add";
        /// <summary>
        /// User can clear gateway log.
        /// </summary>
        public const string Clear = "gateway-log.clear";
        /// <summary>
        /// User can close gateway log.
        /// </summary>
        public const string Close = "gateway-log.close";
    }

    /// <summary>
    /// Subtotal policies.
    /// </summary>
    public static class GXSubtotalPolicies
    {
        /// <summary>
        /// User can view the subtotal(s).
        /// </summary>
        public const string View = "subtotal.view";
        /// <summary>
        /// User can add new subtotal(s).
        /// </summary>
        public const string Add = "subtotal.add";
        /// <summary>
        /// User can edit subtotal(s).
        /// </summary>
        public const string Edit = "subtotal.edit";
        /// <summary>
        /// User can delete subtotal(s).
        /// </summary>
        public const string Delete = "subtotal.delete";
        /// <summary>
        /// User can calculate subtotal(s).
        /// </summary>
        public const string Calculate = "subtotal.calculate";
        /// <summary>
        /// User can clear subtotal(s).
        /// </summary>
        public const string Clear = "subtotal.clear";
    }

    /// <summary>
    /// Subtotal group policies.
    /// </summary>
    public static class GXSubtotalGroupPolicies
    {
        /// <summary>
        /// User can view the subtotal group information.
        /// </summary>
        public const string View = "subtotal-group.view";
        /// <summary>
        /// User can add a new subtotal group.
        /// </summary>
        public const string Add = "subtotal-group.add";
        /// <summary>
        /// User can edit subtotal group.
        /// </summary>
        public const string Edit = "subtotal-group.edit";
        /// <summary>
        /// User can delete subtotal group.
        /// </summary>
        public const string Delete = "subtotal-group.delete";
    }
}
