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

namespace Gurux.DLMS.AMI.Client.Shared.Enums
{
    /// <summary>
    /// Enumerated changed items.
    /// </summary>
    public static class TargetType
    {
        /// <summary>
        /// Devices are changed.
        /// </summary>
        public const string Device = "device";
        /// <summary>
        /// Objects are changed.
        /// </summary>
        public const string Object = "object";
        /// <summary>
        /// Attributes are changed.
        /// </summary>
        public const string Attribute = "attribute";
        /// <summary>
        /// Values are changed.
        /// </summary>
        public const string Value = "value";
        /// <summary>
        /// Tasks are changed.
        /// </summary>
        public const string Task = "task";
        /// <summary>
        /// Device errors are changed.
        /// </summary>
        public const string DeviceLog = "device-log";
        /// <summary>
        /// System errors are changed.
        /// </summary>
        public const string SystemLog = "system-log";
        /// <summary>
        /// System errors are changed.
        /// </summary>
        public const string System = "system";
        /// <summary>
        /// Schedules are changed.
        /// </summary>
        public const string Schedule = "schedule";
        /// <summary>
        /// Agent are changed.
        /// </summary>
        public const string Agent = "agent";
        /// <summary>Agent connection and execution states, separate from log event types.</summary>
        public const string AgentStatus = "agent-status";
        /// <summary>
        /// Device templates are changed.
        /// </summary>
        public const string DeviceTemplate = "device-template";
        /// <summary>
        /// Object templates are changed.
        /// </summary>
        public const string ObjectTemplate = "object-template";
        /// <summary>
        /// Attribute templates are changed.
        /// </summary>
        public const string AttributeTemplate = "attribute-template";
        /// <summary>
        /// User group is changed.
        /// </summary>
        public const string UserGroup = "user-group";
        /// <summary>
        /// User is changed.
        /// </summary>
        public const string User = "user";
        /// <summary>
        /// User Log is changed.
        /// </summary>
        public const string UserAction = "user-action";
        /// <summary>
        /// Configuration is changed.
        /// </summary>
        public const string Configuration = "configuration";
        /// <summary>
        /// Token is changed.
        /// </summary>
        public const string Token = "token";
        /// <summary>
        /// Module is changed.
        /// </summary>
        public const string Module = "module";
        /// <summary>
        /// Module group is changed.
        /// </summary>
        public const string ModuleGroup = "module-group";
        /// <summary>
        /// Workflow is changed.
        /// </summary>
        public const string Workflow = "workflow";
        /// <summary>
        /// Workflow group is the changed.
        /// </summary>
        public const string WorkflowGroup = "workflow-group";
        /// <summary>
        /// Notification is changed.
        /// </summary>
        public const string Notification = "notification";
        /// <summary>
        /// Device action is changed.
        /// </summary>
        public const string DeviceAction = "device-action";
        /// <summary>
        /// Script is changed.
        /// </summary>
        public const string Script = "script";
        /// <summary>
        /// Script log is changed.
        /// </summary>
        public const string ScriptLog = "script-log";
        /// <summary>
        /// Module log is changed.
        /// </summary>
        public const string ModuleLog = "module-log";
        /// <summary>
        /// Agent log is changed.
        /// </summary>
        public const string AgentLog = "agent-log";
        /// <summary>
        /// Schedule log is changed.
        /// </summary>
        public const string ScheduleLog = "schedule-log";
        /// <summary>
        /// Workflow log is changed.
        /// </summary>
        public const string WorkflowLog = "workflow-log";
        /// <summary>
        /// Block is changed.
        /// </summary>
        public const string Block = "block";
        /// <summary>
        /// Block group is changed.
        /// </summary>
        public const string BlockGroup = "block-group";
        /// <summary>
        /// User error is changed.
        /// </summary>
        public const string UserError = "user-error";
        /// <summary>
        /// Trigger is changed.
        /// </summary>
        public const string Trigger = "trigger";
        /// <summary>
        /// Trigger group is changed.
        /// </summary>
        public const string TriggerGroup = "trigger-group";
        /// <summary>
        /// User group is changed.
        /// </summary>
        public const string DeviceGroup = "device-group";
        /// <summary>
        /// Schedule group is changed.
        /// </summary>
        public const string ScheduleGroup = "schedule-group";
        /// <summary>
        /// Agent group is changed.
        /// </summary>
        public const string AgentGroup = "agent-group";
        /// <summary>
        /// Script group is changed.
        /// </summary>
        public const string ScriptGroup = "script-group";
        /// <summary>
        /// Device template group is changed.
        /// </summary>
        public const string DeviceTemplateGroup = "device-template-group";
        /// <summary>
        /// Component view is changed.
        /// </summary>
        public const string ComponentView = "component-view";
        /// <summary>
        /// Component view group is changed.
        /// </summary>
        public const string ComponentViewGroup = "component-view-group";
        /// <summary>
        /// Device trace is changed.
        /// </summary>
        public const string DeviceTrace = "device-trace";
        /// <summary>
        /// Cron is changed.
        /// </summary>
        public const string Cron = "cron";
        /// <summary>
        /// Role is changed.
        /// </summary>
        public const string Role = "role";
        /// <summary>
        /// Manufacturer is changed.
        /// </summary>
        public const string Manufacturer = "manufacturer";
        /// <summary>
        /// Manufacturer group is changed.
        /// </summary>
        public const string ManufacturerGroup = "manufacturer-group";
        /// <summary>
        /// Favorite is changed.
        /// </summary>
        public const string Favorite = "favorite";
        /// <summary>
        /// Manufacturer model is changed.
        /// </summary>
        public const string Model = "model";
        /// <summary>
        /// Manufacturer version is changed.
        /// </summary>
        public const string Version = "version";
        /// <summary>
        /// Key management is changed.
        /// </summary>
        public const string KeyManagement = "key-management";
        /// <summary>
        /// Key management group is changed.
        /// </summary>
        public const string KeyManagementGroup = "key-management-group";
        /// <summary>
        /// Key management key is changed.
        /// </summary>
        public const string KeyManagementKey = "key-management-key";
        /// <summary>
        /// Gateway is changed.
        /// </summary>
        public const string Gateway = "gateway";
        /// <summary>
        /// Gateway group is changed.
        /// </summary>
        public const string GatewayGroup = "gateway-group";
        /// <summary>
        /// Langage is changed.
        /// </summary>
        public const string Language = "language";
        /// <summary>
        /// Performance is changed.
        /// </summary>
        public const string Performance = "performance";
        /// <summary>
        /// Subtotal group is changed.
        /// </summary>
        public const string SubtotalGroup = "subtotal-group";
        /// <summary>
        /// Subtotal is changed.
        /// </summary>
        public const string Subtotal = "subtotal";
        /// <summary>
        /// Subtotal log is changed.
        /// </summary>
        public const string SubtotalLog = "subtotal-log";
        /// <summary>
        /// Subtotal value is changed.
        /// </summary>
        public const string SubtotalValue = "subtotal-value";
        /// <summary>
        /// Report group is changed.
        /// </summary>
        public const string ReportGroup = "report-group";
        /// <summary>
        /// Report is changed.
        /// </summary>
        public const string Report = "report";
        /// <summary>
        /// Report log is changed.
        /// </summary>
        public const string ReportLog = "report-log";
        /// <summary>
        /// Report value is changed.
        /// </summary>
        public const string ReportValue = "report-value";
        /// <summary>
        /// User stamp is changed.
        /// </summary>
        public const string UserStamp = "user-stamp";
        /// <summary>
        /// Gateway log is changed.
        /// </summary>
        public const string GatewayLog = "gateway-log";
        /// <summary>
        /// Key management log is changed.
        /// </summary>
        public const string KeyManagementLog = "key-management-log";
        /// <summary>
        /// Content is changed.
        /// </summary>
        public const string Content = "content";
        /// <summary>
        /// Content group is changed.
        /// </summary>
        public const string ContentGroup = "content-group";
        /// <summary>
        /// Content type is changed.
        /// </summary>
        public const string ContentType = "content-type";
        /// <summary>
        /// Content type group is changed.
        /// </summary>
        public const string ContentTypeGroup = "content-type-group";
        /// <summary>
        /// Menu group is changed.
        /// </summary>
        public const string MenuGroup = "menu-group";
        /// <summary>
        /// Menu is changed.
        /// </summary>
        public const string Menu = "menu";
        /// <summary>
        /// Appearance is changed.
        /// </summary>
        public const string Appearance = "appearance";
        /// <summary>
        /// Prune is changed.
        /// </summary>
        public const string Prune = "prune";
        /// <summary>
        /// LocalizedResource is changed.
        /// </summary>
        public const string LocalizedResource = "localized-resource";
        /// <summary>
        /// Announcement is changed.
        /// </summary>
        public const string Announcement = "announcement";
        /// <summary>
        /// IP address is changed.
        /// </summary>
        public const string IpAddress = "ip-address";
        /// <summary>
        /// Logs is changed.
        /// </summary>
        public const string Log = "log";
        /// <summary>
        /// History is changed.
        /// </summary>
        public const string ChangeHistory = "change-history";
    }
}
