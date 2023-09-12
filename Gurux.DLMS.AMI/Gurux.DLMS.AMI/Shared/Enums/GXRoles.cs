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
namespace Gurux.DLMS.AMI.Shared.Enums
{
    /// <summary>
    /// Default User roles.
    /// </summary>
    public class GXRoles
    {
        /// <summary>
        /// User is admin.
        /// </summary>
        public const string Admin = "Admin";
        /// <summary>
        /// Default user.
        /// </summary>
        public const string User = "User";
        /// <summary>
        /// User is user manager.
        /// </summary>
        public const string UserManager = "UserManager";
        /// <summary>
        /// User is user action .
        /// </summary>
        public const string UserAction = "UserAction";
        /// <summary>
        /// User is user action manager.
        /// </summary>
        public const string UserActionManager = "UserActionManager";

        /// <summary>
        /// User can access devices.
        /// </summary>
        public const string Device = "Device";
        /// <summary>
        /// User is device manager.
        /// </summary>
        public const string DeviceManager = "DeviceManager";
        /// <summary>
        /// User can access schedules.
        /// </summary>
        public const string Schedule = "Schedule";
        /// <summary>
        /// User is schedule manager.
        /// </summary>
        public const string ScheduleManager = "ScheduleManager";
        /// <summary>
        /// User can access schedule groups.
        /// </summary>
        public const string ScheduleGroup = "ScheduleGroup";
        /// <summary>
        /// User is schedule group manager.
        /// </summary>
        public const string ScheduleGroupManager = "ScheduleGroupManager";
        /// <summary>
        /// User can access device trace information.
        /// </summary>
        public const string DeviceTrace = "deviceatrace";
        /// <summary>
        /// User is device trace manager.
        /// </summary>
        public const string DeviceTraceManager = "DeviceTraceManager";
        /// <summary>
        /// User can access device action information.
        /// </summary>
        public const string DeviceAction = "deviceaction";
        /// <summary>
        /// User is device action manager.
        /// </summary>
        public const string DeviceActionManager = "DeviceActionManager";
        /// <summary>
        /// User can access templates.
        /// </summary>
        public const string Template = "Template";
        /// <summary>
        /// User is template manager.
        /// </summary>
        public const string TemplateManager = "TemplateManager";
        /// <summary>
        /// User can access user groups.
        /// </summary>
        public const string UserGroup = "UserGroup";
        /// <summary>
        /// User is user group manager.
        /// </summary>
        public const string UserGroupManager = "UserGroupManager";
        /// <summary>
        /// User can access agents.
        /// </summary>
        public const string Agent = "Agent";
        /// <summary>
        /// User is agent manager.
        /// </summary>
        public const string AgentManager = "AgentManager";
        /// <summary>
        /// User can access agent groups.
        /// </summary>
        public const string AgentGroup = "AgentGroup";
        /// <summary>
        /// User is agent group manager.
        /// </summary>
        public const string AgentGroupManager = "AgentGroupManager";
        /// <summary>
        /// User can access agent log.
        /// </summary>
        public const string AgentLog = "AgentLog";
        /// <summary>
        /// User is agent log manager.
        /// </summary>
        public const string AgentLogManager = "AgentLogManager";
        /// <summary>
        /// User can access device log.
        /// </summary>
        public const string DeviceLog = "DeviceLog";
        /// <summary>
        /// User is device log manager.
        /// </summary>
        public const string DeviceLogManager = "DeviceLogManager";
        /// <summary>
        /// User can access user errors.
        /// </summary>
        public const string UserError = "UserError";
        /// <summary>
        /// User is user error manager.
        /// </summary>
        public const string UserErrorManager = "UserErrorManager";
        /// <summary>
        /// User can access script logs.
        /// </summary>
        public const string ScriptLog = "ScriptLog";
        /// <summary>
        /// User is script log manager.
        /// </summary>
        public const string ScriptLogManager = "ScriptLogManager";
        /// <summary>
        /// User can access workflow logs.
        /// </summary>
        public const string WorkflowLog = "WorkflowLog";
        /// <summary>
        /// User is workflow log manager.
        /// </summary>
        public const string WorkflowLogManager = "WorkflowLogManager";
        /// <summary>
        /// User can access schedule log.
        /// </summary>
        public const string ScheduleLog = "ScheduleLog";
        /// <summary>
        /// User is schedule log manager.
        /// </summary>
        public const string ScheduleLogManager = "ScheduleLogManager";

        /// <summary>
        /// User can access system log.
        /// </summary>
        public const string SystemLog = "SystemLog";
        /// <summary>
        /// User is system log manager.
        /// </summary>
        public const string SystemLogManager = "SystemLogManager";
        /// <summary>
        /// User can access device templates.
        /// </summary>
        public const string DeviceTemplate = "DeviceTemplate";
        /// <summary>
        /// User is device template manager.
        /// </summary>
        public const string DeviceTemplateManager = "DeviceTemplateManager";
        /// <summary>
        /// User can access device template groups.
        /// </summary>
        public const string DeviceTemplateGroup = "DeviceTemplateGroup";
        /// <summary>
        /// User is device template group manager.
        /// </summary>
        public const string DeviceTemplateGroupManager = "DeviceTemplateGroupManager";
        /// <summary>
        /// User can access device groups.
        /// </summary>
        public const string DeviceGroup = "DeviceGroup";
        /// <summary>
        /// User is device group manager.
        /// </summary>
        public const string DeviceGroupManager = "DeviceGroupManager";

        /// <summary>
        /// User can access module groups.
        /// </summary>
        public const string ModuleGroup = "ModuleGroup";
        /// <summary>
        /// User is module manager.
        /// </summary>
        public const string ModuleManager = "ModuleManager";
        /// <summary>
        /// User is module group manager.
        /// </summary>
        public const string ModuleGroupManager = "ModuleGroupManager";

        /// <summary>
        /// User can access module log.
        /// </summary>
        public const string ModuleLog = "ModuleLog";
        /// <summary>
        /// User is device module manager.
        /// </summary>
        public const string ModuleLogManager = "ModuleLogManager";

        /// <summary>
        /// User can access workflows.
        /// </summary>
        public const string Workflow = "Workflow";
        /// <summary>
        /// User can access workflow groups.
        /// </summary>
        public const string WorkflowGroup = "WorkflowGroup";
        /// <summary>
        /// User is workflow manager.
        /// </summary>
        public const string WorkflowManager = "WorkflowManager";
        /// <summary>
        /// User is workflow group manager.
        /// </summary>
        public const string WorkflowGroupManager = "WorkflowGroupManager";
        /// <summary>
        /// User can access trigger groups.
        /// </summary>
        public const string TriggerGroup = "TriggerGroup";
        /// <summary>
        /// User is trigger.
        /// </summary>
        public const string Trigger = "Trigger";

        /// <summary>
        /// User is trigger manager.
        /// </summary>
        public const string TriggerManager = "TriggerManager";

        /// <summary>
        /// User is trigger group manager.
        /// </summary>
        public const string TriggerGroupManager = "TriggerGroupManager";

        /// <summary>
        /// User is token manager.
        /// </summary>
        public const string TokenManager = "TokenManager";

        /// <summary>
        /// User is task manager.
        /// </summary>
        public const string TaskManager = "TaskManager";

        /// <summary>
        /// User is block manager.
        /// </summary>
        public const string BlockManager = "BlockManager";

        /// <summary>
        /// User is block group manager.
        /// </summary>
        public const string BlockGroupManager = "BlockGroupManager";

        /// <summary>
        /// User is component view manager.
        /// </summary>
        public const string ComponentViewManager = "ComponentViewManager";

        /// <summary>
        /// User is component view group manager.
        /// </summary>
        public const string ComponentViewGroupManager = "ComponentViewGroupManager";

        /// <summary>
        /// User is localization manager.
        /// </summary>
        public const string LocalizationManager = "LocalizationManager";

        /// <summary>
        /// User can access scripts.
        /// </summary>
        public const string Script = "Script";
        /// <summary>
        /// User can access script groups.
        /// </summary>
        public const string ScriptGroup = "ScriptGroup";

        /// <summary>
        /// User is script manager.
        /// </summary>
        public const string ScriptManager = "ScriptManager";

        /// <summary>
        /// User is script group manager.
        /// </summary>
        public const string ScriptGroupManager = "ScriptGroupManager";

        /// <summary>
        /// User can access manufacturers.
        /// </summary>
        public const string Manufacturer = "Manufacturer";

        /// <summary>
        /// User is manufacturer manager.
        /// </summary>
        public const string ManufacturerManager = "ManufacturerManager";

        /// <summary>
        /// User can access manufacturer groups.
        /// </summary>
        public const string ManufacturerGroup = "ManufacturerGroup";

        /// <summary>
        /// User is manufacturer group manager.
        /// </summary>
        public const string ManufacturerGroupManager = "ManufacturerGroupManager";

        /// <summary>
        /// User can access key managements.
        /// </summary>
        public const string KeyManagement = "KeyManagement";
        /// <summary>
        /// User is key management manager.
        /// </summary>
        public const string KeyManagementManager = "KeyManagementManager";
        /// <summary>
        /// User can access key management groups.
        /// </summary>
        public const string KeyManagementGroup = "KeyManagementGroup";
        /// <summary>
        /// User is key management group manager.
        /// </summary>
        public const string KeyManagementGroupManager = "KeyManagementGroupManager";

        /// <summary>
        /// User can access key management logs.
        /// </summary>
        public const string KeyManagementLog = "KeyManagementLog";
        /// <summary>
        /// User is key management log manager.
        /// </summary>
        public const string KeyManagementLogManager = "KeyManagementLogManager";
        /// <summary>
        /// User can access gateway groups.
        /// </summary>
        public const string GatewayGroup = "GatewayGroup";
        /// <summary>
        /// User is gateway group manager.
        /// </summary>
        public const string GatewayGroupManager = "GatewayGroupManager";
        /// <summary>
        /// User can access gateways.
        /// </summary>
        public const string Gateway = "Gateway";
        /// <summary>
        /// User is gateway manager.
        /// </summary>
        public const string GatewayManager = "GatewayManager";
        /// <summary>
        /// User can access gateway logs.
        /// </summary>
        public const string GatewayLog = "GatewayLog";

        /// <summary>
        /// User is gateway log manager.
        /// </summary>
        public const string GatewayLogManager = "GatewayLogManager";

        public static string ToString(string param1)
        {
            return param1;
        }
        public static string ToString(string param1, string param2)
        {
            return param1 + "," + param2;
        }
        public static string ToString(string param1, string param2, string param3)
        {
            return param1 + "," + param2 + "," + param3;
        }
        public static string ToString(string param1, string param2, string param3, string param4)
        {
            return param1 + "," + param2 + "," + param3 + "," + param4;
        }
    }
}
