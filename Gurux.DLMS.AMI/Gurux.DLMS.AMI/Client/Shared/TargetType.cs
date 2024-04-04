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

namespace Gurux.DLMS.AMI.Client.Shared
{
    /// <summary>
    /// Enumerated changed items.
    /// </summary>
    public static class TargetType
    {
        /// <summary>
        /// Devices are changed.
        /// </summary>
        public const string Device = "Device";
        /// <summary>
        /// Objects are changed.
        /// </summary>
        public const string Object = "Object";
        /// <summary>
        /// Attributes are changed.
        /// </summary>
        public const string Attribute = "Attribute";
        /// <summary>
        /// Values are changed.
        /// </summary>
        public const string Value = "Value";
        /// <summary>
        /// Tasks are changed.
        /// </summary>
        public const string Task = "Task";
        /// <summary>
        /// Device errors are changed.
        /// </summary>
        public const string DeviceError = "DeviceError";
        /// <summary>
        /// System errors are changed.
        /// </summary>
        public const string SystemLog = "SystemLog";
        /// <summary>
        /// Schedules are changed.
        /// </summary>
        public const string Schedule = "Schedule";
        /// <summary>
        /// Agent are changed.
        /// </summary>
        public const string Agent = "Agent";
        /// <summary>
        /// Device templates are changed.
        /// </summary>
        public const string DeviceTemplate = "DeviceTemplate";
        /// <summary>
        /// Object templates are changed.
        /// </summary>
        public const string ObjectTemplate = "ObjectTemplate";
        /// <summary>
        /// Attribute templates are changed.
        /// </summary>
        public const string AttributeTemplate = "AttributeTemplate";
        /// <summary>
        /// Device Log is changed.
        /// </summary>
        public const string DeviceLog = "DeviceLog";
        /// <summary>
        /// User group is changed.
        /// </summary>
        public const string UserGroup = "UserGroup";
        /// <summary>
        /// User is changed.
        /// </summary>
        public const string User = "User";
        /// <summary>
        /// User Log is changed.
        /// </summary>
        public const string UserAction = "UserAction";
        /// <summary>
        /// Configuration is changed.
        /// </summary>
        public const string Configuration = "Configuration";
        /// <summary>
        /// Token is changed.
        /// </summary>
        public const string Token = "Token";
        /// <summary>
        /// Module is changed.
        /// </summary>
        public const string Module = "Module";
        /// <summary>
        /// WorkFlow is changed.
        /// </summary>
        public const string Workflow = "Workflow";
        /// <summary>
        /// Device action is changed.
        /// </summary>
        public const string DeviceAction = "DeviceAction";
        /// <summary>
        /// Script is changed.
        /// </summary>
        public const string Script = "Script";
        /// <summary>
        /// Script log is changed.
        /// </summary>
        public const string ScriptLog = "ScriptLog";
        /// <summary>
        /// Module log is changed.
        /// </summary>
        public const string ModuleLog = "ModuleLog";
        /// <summary>
        /// Agent log is changed.
        /// </summary>
        public const string AgentLog = "AgentLog";
        /// <summary>
        /// Schedule log is changed.
        /// </summary>
        public const string ScheduleLog = "ScheduleLog";
        /// <summary>
        /// Workflow log is changed.
        /// </summary>
        public const string WorkflowLog = "WorkflowLog";
        /// <summary>
        /// Block is changed.
        /// </summary>
        public const string Block = "Block";
        /// <summary>
        /// User error is changed.
        /// </summary>
        public const string UserError = "UserError";
        /// <summary>
        /// Trigger is changed.
        /// </summary>
        public const string Trigger = "Trigger";
        /// <summary>
        /// Trigger group is changed.
        /// </summary>
        public const string TriggerGroup = "TriggerGroup";
        /// <summary>
        /// User group is changed.
        /// </summary>
        public const string DeviceGroup = "DeviceGroup";
        /// <summary>
        /// User group is changed.
        /// </summary>
        public const string ScheduleGroup = "ScheduleGroup";
        /// <summary>
        /// Agent group is changed.
        /// </summary>
        public const string AgentGroup = "AgentGroup";
        /// <summary>
        /// Script group is changed.
        /// </summary>
        public const string ScriptGroup = "ScriptGroup";
        /// <summary>
        /// Device template group is changed.
        /// </summary>
        public const string DeviceTemplateGroup = "DeviceTemplateGroup";
        /// <summary>
        /// Component view is changed.
        /// </summary>
        public const string ComponentView = "ComponentView";
        /// <summary>
        /// Component view group is changed.
        /// </summary>
        public const string ComponentViewGroup = "ComponentViewGroup";
        /// <summary>
        /// Device trace is changed.
        /// </summary>
        public const string DeviceTrace = "DeviceTrace";
        /// <summary>
        /// Cron is changed.
        /// </summary>
        public const string Cron = "Cron";
        /// <summary>
        /// Role is changed.
        /// </summary>
        public const string Role = "Role";
        /// <summary>
        /// Manufacturer is changed.
        /// </summary>
        public const string Manufacturer = "Manufacturer";
        /// <summary>
        /// Manufacturer group is changed.
        /// </summary>
        public const string ManufacturerGroup = "ManufacturerGroup";
        /// <summary>
        /// Favorite is changed.
        /// </summary>
        public const string Favorite = "Favorite";
        /// <summary>
        /// Manufacturer model is changed.
        /// </summary>
        public const string Model = "Model";
        /// <summary>
        /// Manufacturer version is changed.
        /// </summary>
        public const string Version = "Version";
        /// <summary>
        /// Key management is changed.
        /// </summary>
        public const string KeyManagement = "KeyManagement";
        /// <summary>
        /// Key management group is changed.
        /// </summary>
        public const string KeyManagementGroup = "KeyManagementGroup";
        /// <summary>
        /// Key management key is changed.
        /// </summary>
        public const string KeyManagementKey = "KeyManagementKey";
        /// <summary>
        /// Gateway is changed.
        /// </summary>
        public const string Gateway = "Gateway";
        /// <summary>
        /// Gateway group is changed.
        /// </summary>
        public const string GatewayGroup = "GatewayGroup";
        /// <summary>
        /// Rest statistic is changed.
        /// </summary>
        public const string RestStatistic = "RestStatistic";
        /// <summary>
        /// Langage is changed.
        /// </summary>
        public const string Language = "Language";
        /// <summary>
        /// Performance is changed.
        /// </summary>
        public const string Performance = "Performance";
        /// <summary>
        /// Subtotal group is changed.
        /// </summary>
        public const string SubtotalGroup = "SubtotalGroup";
        /// <summary>
        /// Subtotal is changed.
        /// </summary>
        public const string Subtotal = "Subtotal";
        /// <summary>
        /// Subtotal log is changed.
        /// </summary>
        public const string SubtotalLog = "SubtotalLog";
        /// <summary>
        /// Subtotal value is changed.
        /// </summary>
        public const string SubtotalValue = "SubtotalValue";
        /// <summary>
        /// Report group is changed.
        /// </summary>
        public const string ReportGroup = "ReportGroup";
        /// <summary>
        /// Report is changed.
        /// </summary>
        public const string Report = "Report";
        /// <summary>
        /// Report log is changed.
        /// </summary>
        public const string ReportLog = "ReportLog";
        /// <summary>
        /// Report value is changed.
        /// </summary>
        public const string ReportValue = "ReportValue";
        /// <summary>
        /// User stamp is changed.
        /// </summary>
        public const string UserStamp = "UserStamp";
        /// <summary>
        /// Gateway log is changed.
        /// </summary>
        public const string GatewayLog = "GatewayLog";
        /// <summary>
        /// Key management log is changed.
        /// </summary>
        public const string KeyManagementLog = "KeyManagementLog";
    }
}
