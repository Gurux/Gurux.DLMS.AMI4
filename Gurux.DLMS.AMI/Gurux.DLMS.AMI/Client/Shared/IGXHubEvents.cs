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
using Gurux.DLMS.AMI.Shared.DTOs.Block;
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
using Gurux.DLMS.AMI.Shared.DTOs.Subtotal;

namespace Gurux.DLMS.AMI.Client.Shared
{
    /// <summary>
    /// Hub server events.
    /// </summary>
    public interface IGXHubEvents
    {
        /// <summary>
        /// Configuration is saved.
        /// </summary>
        /// <param name="configurations">List of saved configurations.</param>
        Task ConfigurationSave(IEnumerable<GXConfiguration> configurations);

        /// <summary>
        /// Module settings are saved.
        /// </summary>
        /// <param name="updated"></param>
        Task ModuleSettingsSave(GXModule updated);

        /// <summary>
        /// System errors are cleared.
        /// </summary>      
        Task ClearSystemLogs();

        /// <summary>
        /// New system error is added.
        /// </summary>
        /// <param name="errors">New system errors.</param>
        Task AddSystemLogs(IEnumerable<GXSystemLog> errors);

        /// <summary>
        /// System errors are closed.
        /// </summary>
        /// <param name="errors">Closed system errors.</param>
        Task CloseSystemLogs(IEnumerable<GXSystemLog> errors);

        /// <summary>
        /// Device errors are cleared.
        /// </summary>
        /// <param name="devices">List of cleared devices.</param>
        Task ClearDeviceErrors(IEnumerable<GXDevice>? devices);

        /// <summary>
        /// New device error is added.
        /// </summary>
        /// <param name="item">New device error.</param>
        Task AddDeviceErrors(IEnumerable<GXDeviceError> item);

        /// <summary>
        /// Device errors are closed.
        /// </summary>
        /// <param name="item">Closed errors.</param>
        Task CloseDeviceErrors(IEnumerable<GXDeviceError> item);

        /// <summary>
        /// Workflow logs are cleared.
        /// </summary>
        /// <param name="Workflows">List of cleared workflows log items.</param>
        Task ClearWorkflowLogs(IEnumerable<GXWorkflow>? Workflows);

        /// <summary>
        /// New workflow log is added.
        /// </summary>
        /// <param name="item">New workflow log.</param>
        Task AddWorkflowLogs(IEnumerable<GXWorkflowLog> item);

        /// <summary>
        /// Workflow logs are closed.
        /// </summary>
        /// <param name="item">Closed workflow logs.</param>
        Task CloseWorkflowLogs(IEnumerable<GXWorkflowLog> item);

        /// <summary>
        /// Schedule errors are cleared.
        /// </summary>
        /// <param name="schedules">List of cleared schedules.</param>
        Task ClearScheduleLog(IEnumerable<GXSchedule>? schedules);

        /// <summary>
        /// New schedule logs are added.
        /// </summary>
        /// <param name="logs">New schedule logs.</param>
        Task AddScheduleLog(IEnumerable<GXScheduleLog> logs);

        /// <summary>
        /// Schedule logs are closed.
        /// </summary>
        /// <param name="logs">Closed schedule logs.</param>
        Task CloseScheduleLog(IEnumerable<GXScheduleLog> logs);

        /// <summary>
        /// Script logs are cleared.
        /// </summary>
        /// <param name="logs">List of cleared logs.</param>
        Task ClearScriptLogs(IEnumerable<GXScript>? logs);

        /// <summary>
        /// New Script log is added.
        /// </summary>
        /// <param name="logs">New script logs.</param>
        Task AddScriptLogs(IEnumerable<GXScriptLog> logs);

        /// <summary>
        /// Script logs are closed.
        /// </summary>
        /// <param name="errors">Closed script logs.</param>
        Task CloseScriptLogs(IEnumerable<GXScriptLog> errors);

        /// <summary>
        /// User errors are cleared.
        /// </summary>
        /// <param name="Users">List of cleared users.</param>
        Task ClearUserErrors(IEnumerable<GXUser>? Users);

        /// <summary>
        /// New User error is added.
        /// </summary>
        /// <param name="item">New user error.</param>
        Task AddUserErrors(IEnumerable<GXUserError> item);

        /// <summary>
        /// User errors are closed.
        /// </summary>
        /// <param name="item">Closed user errors.</param>
        Task CloseUserErrors(IEnumerable<GXUserError> item);


        /// <summary>
        /// Module logs are cleared.
        /// </summary>
        /// <param name="modules">List of cleared modules.</param>
        Task ClearModuleLogs(IEnumerable<GXModule>? modules);

        /// <summary>
        /// New module log is added.
        /// </summary>
        /// <param name="item">New module log.</param>
        Task AddModuleLogs(IEnumerable<GXModuleLog> item);

        /// <summary>
        /// Module log are closed.
        /// </summary>
        /// <param name="item">Closed errors.</param>
        Task CloseModuleLogs(IEnumerable<GXModuleLog> item);


        /// <summary>
        /// Agent logs are cleared.
        /// </summary>
        /// <param name="agents">List of cleared agents.</param>
        Task ClearAgentLogs(IEnumerable<GXAgent>? agents);

        /// <summary>
        /// New agent logs is added.
        /// </summary>
        /// <param name="logs">New agent logs.</param>
        Task AddAgentLogs(IEnumerable<GXAgentLog> logs);

        /// <summary>
        /// Agent log items are closed.
        /// </summary>
        /// <param name="agents">Closed log items.</param>
        Task CloseAgentLogs(IEnumerable<GXAgentLog> agents);

        /// <summary>
        /// New Schedule is added.
        /// </summary>
        /// <param name="schedules">New schedules.</param>
        Task ScheduleUpdate(IEnumerable<GXSchedule> schedules);

        /// <summary>
        /// Schedule is deleted.
        /// </summary>
        /// <param name="schedules">Deleted schedules.</param>
        Task ScheduleDelete(IEnumerable<GXSchedule> schedules);

        /// <summary>
        /// Schedules are started.
        /// </summary>
        /// <param name="schedules">Started schedules.</param>
        Task ScheduleStart(IEnumerable<GXSchedule> schedules);

        /// <summary>
        /// Schedules are compleated.
        /// </summary>
        /// <param name="schedules">Compleated schedules.</param>
        Task ScheduleCompleate(IEnumerable<GXSchedule> schedules);

        /// <summary>
        /// New Schedule group is added.
        /// </summary>
        /// <param name="groups">New schedule groups.</param>
        Task ScheduleGroupUpdate(IEnumerable<GXScheduleGroup> groups);

        /// <summary>
        /// Schedule group is deleted.
        /// </summary>
        /// <param name="groups">Deleted schedule groups.</param>
        Task ScheduleGroupDelete(IEnumerable<GXScheduleGroup> groups);

        /// <summary>
        /// New user is added or modified.
        /// </summary>
        /// <param name="users">Updated users.</param>
        Task UserUpdate(IEnumerable<GXUser> users);

        /// <summary>
        /// User is deleted.
        /// </summary>
        /// <param name="item">Deleted user.</param>
        Task UserDelete(IEnumerable<GXUser> users);

        /// <summary>
        /// New user group is added or modified.
        /// </summary>
        /// <param name="groups">Updated user groups.</param>
        Task UserGroupUpdate(IEnumerable<GXUserGroup> groups);

        /// <summary>
        /// User group is deleted.
        /// </summary>
        /// <param name="item">Deleted user groups.</param>
        Task UserGroupDelete(IEnumerable<GXUserGroup> groups);

        /// <summary>
        /// New device is added or modified.
        /// </summary>
        /// <param name="devices">Updated devices.</param>
        Task DeviceUpdate(IEnumerable<GXDevice> devices);

        /// <summary>
        /// Device is deleted.
        /// </summary>
        /// <param name="devices">Deleted devices.</param>
        Task DeviceDelete(IEnumerable<GXDevice> devices);

        /// <summary>
        /// New device group is added or modified.
        /// </summary>
        /// <param name="groups">Updated device groups.</param>
        Task DeviceGroupUpdate(IEnumerable<GXDeviceGroup> groups);

        /// <summary>
        /// Device group is deleted.
        /// </summary>
        /// <param name="groups">Deleted device groups.</param>
        Task DeviceGroupDelete(IEnumerable<GXDeviceGroup> groups);

        /// <summary>
        /// New device template group is added or modified.
        /// </summary>
        /// <param name="groups">Updated device template groups.</param>
        Task DeviceTemplateGroupUpdate(IEnumerable<GXDeviceTemplateGroup> groups);

        /// <summary>
        /// Device template group is deleted.
        /// </summary>
        /// <param name="groups">Deleted device template groups.</param>
        Task DeviceTemplateGroupDelete(IEnumerable<GXDeviceTemplateGroup> groups);

        /// <summary>
        /// New device template is added or modified.
        /// </summary>
        /// <param name="templates">Updated device template group.</param>
        Task DeviceTemplateUpdate(IEnumerable<GXDeviceTemplate> templates);

        /// <summary>
        /// Device template is deleted.
        /// </summary>
        /// <param name="templates">Deleted device templates.</param>
        Task DeviceTemplateDelete(IEnumerable<GXDeviceTemplate> templates);

        /// <summary>
        /// New object template is added or modified.
        /// </summary>
        /// <param name="objects">Updated object templates.</param>
        Task ObjectTemplateUpdate(IEnumerable<GXObjectTemplate> templates);

        /// <summary>
        /// Object template is deleted.
        /// </summary>
        /// <param name="objects">Deleted objects templates.</param>
        Task ObjectTemplateDelete(IEnumerable<GXObjectTemplate> templates);

        /// <summary>
        /// New attribute template is added or modified.
        /// </summary>
        /// <param name="objects">Updated attribute templates.</param>
        Task AttributeTemplateUpdate(IEnumerable<GXAttributeTemplate> templates);

        /// <summary>
        /// Attribute template is deleted.
        /// </summary>
        /// <param name="objects">Deleted attribute templates.</param>
        Task AttributeTemplateDelete(IEnumerable<GXAttributeTemplate> templates);

        /// <summary>
        /// New agent is added or modified.
        /// </summary>
        /// <param name="agents">Updated agents.</param>
        Task AgentUpdate(IEnumerable<GXAgent> agents);

        /// <summary>
        /// Agent is deleted.
        /// </summary>
        /// <param name="agents">Deleted agents.</param>
        Task AgentDelete(IEnumerable<GXAgent> agents);

        /// <summary>
        /// Agent status has changed.
        /// </summary>
        /// <param name="agents">Agents.</param>
        Task AgentStatusChange(IEnumerable<GXAgent> agents);

        /// <summary>
        /// New agent group is added or modified.
        /// </summary>
        /// <param name="groups">Updated agent groups.</param>
        Task AgentGroupUpdate(IEnumerable<GXAgentGroup> groups);

        /// <summary>
        /// Agent group is deleted.
        /// </summary>
        /// <param name="groups">Deleted agent group.</param>
        Task AgentGroupDelete(IEnumerable<GXAgentGroup> groups);

        /// <summary>
        /// New workflow is added or modified.
        /// </summary>
        /// <param name="workflows">Updated workflow.</param>
        Task WorkflowUpdate(IEnumerable<GXWorkflow> workflows);

        /// <summary>
        /// Workflow is deleted.
        /// </summary>
        /// <param name="workflows">Deleted workflow.</param>
        Task WorkflowDelete(IEnumerable<GXWorkflow> workflows);

        /// <summary>
        /// New workflow group is added or modified.
        /// </summary>
        /// <param name="groups">Updated workflow groups.</param>
        Task WorkflowGroupUpdate(IEnumerable<GXWorkflowGroup> groups);

        /// <summary>
        /// Workflow group is deleted.
        /// </summary>
        /// <param name="groups">Deleted workflow groups.</param>
        Task WorkflowGroupDelete(IEnumerable<GXWorkflowGroup> groups);

        /// <summary>
        /// New trigger is added or modified.
        /// </summary>
        /// <param name="triggers">Updated triggers.</param>
        Task TriggerUpdate(IEnumerable<GXTrigger> triggers);

        /// <summary>
        /// Trigger is deleted.
        /// </summary>
        /// <param name="triggers">Deleted triggers.</param>
        Task TriggerDelete(IEnumerable<GXTrigger> triggers);

        /// <summary>
        /// New trigger group is added or modified.
        /// </summary>
        /// <param name="groups">Updated trigger groups.</param>
        Task TriggerGroupUpdate(IEnumerable<GXTriggerGroup> groups);

        /// <summary>
        /// Trigger group is deleted.
        /// </summary>
        /// <param name="groups">Deleted trigger groups.</param>
        Task TriggerGroupDelete(IEnumerable<GXTriggerGroup> groups);

        /// <summary>
        /// New module is added or modified.
        /// </summary>
        /// <param name="modules">Updated modules.</param>
        Task ModuleUpdate(IEnumerable<GXModule> modules);

        /// <summary>
        /// Module is deleted.
        /// </summary>
        /// <param name="modules">Deleted modules.</param>
        Task ModuleDelete(IEnumerable<GXModule> modules);

        /// <summary>
        /// New module group is added or modified.
        /// </summary>
        /// <param name="groups">Updated module groups.</param>
        Task ModuleGroupUpdate(IEnumerable<GXModuleGroup> groups);

        /// <summary>
        /// Module group is deleted.
        /// </summary>
        /// <param name="groups">Deleted module groups.</param>
        Task ModuleGroupDelete(IEnumerable<GXModuleGroup> groups);


        /// <summary>
        /// New object is added or modified.
        /// </summary>
        /// <param name="objects">Updated objects.</param>
        Task ObjectUpdate(IEnumerable<GXObject> objects);

        /// <summary>
        /// Object is deleted.
        /// </summary>
        /// <param name="objects">Deleted objects.</param>
        Task ObjectDelete(IEnumerable<GXObject> objects);

        /// <summary>
        /// New attribute is added or modified.
        /// </summary>
        /// <param name="attributes">Updated attributes.</param>
        Task AttributeUpdate(IEnumerable<GXAttribute> attributes);

        /// <summary>
        /// Attribute is deleted.
        /// </summary>
        /// <param name="attributes">Deleted attribute.</param>
        Task AttributeDelete(IEnumerable<GXAttribute> attributes);

        /// <summary>
        /// Attribute value is updated.
        /// </summary>
        /// <param name="attributes">Updated attribute values.</param>
        Task ValueUpdate(IEnumerable<GXAttribute> attributes);

        /// <summary>
        /// New task is added.
        /// </summary>
        /// <param name="tasks">Added tasks.</param>
        Task TaskAdd(IEnumerable<GXTask> tasks);

        /// <summary>
        /// Task is update.
        /// </summary>
        /// <param name="tasks">Updated tasks.</param>
        Task TaskUpdate(IEnumerable<GXTask> tasks);

        /// <summary>
        /// Task is deleted.
        /// </summary>
        /// <param name="tasks">Deleted tasks.</param>
        Task TaskDelete(IEnumerable<GXTask> tasks);

        /// <summary>
        /// Tasks are cleared for the selected users.
        /// </summary>
        /// <param name="users">List of cleared users.</param>
        Task TaskClear(IEnumerable<GXUser>? users);

        /// <summary>
        /// Blocks are updated or modified.
        /// </summary>
        /// <param name="blocks">Updated blocks.</param>
        Task BlockUpdate(IEnumerable<GXBlock> blocks);

        /// <summary>
        /// Block is deleted.
        /// </summary>
        /// <param name="blocks">Deleted block.</param>
        Task BlockDelete(IEnumerable<GXBlock> blocks);

        /// <summary>
        /// Blocks are closed.
        /// </summary>
        /// <param name="blocks">Closed blocks.</param>
        Task BlockClose(IEnumerable<GXBlock> blocks);

        /// <summary>
        /// New block group is added or modified.
        /// </summary>
        /// <param name="groups">Updated block groups.</param>
        Task BlockGroupUpdate(IEnumerable<GXBlockGroup> groups);

        /// <summary>
        /// Block group is deleted.
        /// </summary>
        /// <param name="groups">Deleted block groups.</param>
        Task BlockGroupDelete(IEnumerable<GXBlockGroup> groups);

        /// <summary>
        /// New user action is added.
        /// </summary>
        /// <param name="userActions">Updated user actions.</param>
        Task UserActionAdd(IEnumerable<GXUserAction> userActions);

        /// <summary>
        /// User action is deleted.
        /// </summary>
        /// <param name="userActions">Deleted user actions.</param>
        Task UserActionDelete(IEnumerable<GXUserAction> userActions);

        /// <summary>
        /// User actions are clear.
        /// </summary>
        Task UserActionClear(IEnumerable<GXUser>? users);

        /// <summary>
        /// New component view is added or modified.
        /// </summary>
        /// <param name="componentViews">Updated component views.</param>
        Task ComponentViewUpdate(IEnumerable<GXComponentView> componentViews);

        /// <summary>
        /// Component view is deleted.
        /// </summary>
        /// <param name="componentViews">Deleted component views.</param>
        Task ComponentViewDelete(IEnumerable<GXComponentView> componentViews);

        /// <summary>
        /// New component view group is added or modified.
        /// </summary>
        /// <param name="groups">Updated component view groups.</param>
        Task ComponentViewGroupUpdate(IEnumerable<GXComponentViewGroup> groups);

        /// <summary>
        /// Component view group is deleted.
        /// </summary>
        /// <param name="groups">Deleted component view groups.</param>
        Task ComponentViewGroupDelete(IEnumerable<GXComponentViewGroup> groups);

        /// <summary>
        /// New script is added or modified.
        /// </summary>
        /// <param name="scripts">Updated scripts.</param>
        Task ScriptUpdate(IEnumerable<GXScript> scripts);

        /// <summary>
        /// Component view is deleted.
        /// </summary>
        /// <param name="scripts">Deleted scripts.</param>
        Task ScriptDelete(IEnumerable<GXScript> scripts);

        /// <summary>
        /// New script group is added or modified.
        /// </summary>
        /// <param name="groups">Updated script groups.</param>
        Task ScriptGroupUpdate(IEnumerable<GXScriptGroup> groups);

        /// <summary>
        /// Component view group is deleted.
        /// </summary>
        /// <param name="groups">Deleted script groups.</param>
        Task ScriptGroupDelete(IEnumerable<GXScriptGroup> groups);

        /// <summary>
        /// New device trace is added.
        /// </summary>
        /// <param name="deviceTraces">Updated device trace.</param>
        Task DeviceTraceAdd(IEnumerable<GXDeviceTrace> deviceTraces);

        /// <summary>
        /// Device traces are clear.
        /// </summary>
        Task DeviceTraceClear(IEnumerable<GXDevice>? devices);

        /// <summary>
        /// New device action is added.
        /// </summary>
        /// <param name="deviceActions">Updated user action.</param>
        Task DeviceActionAdd(IEnumerable<GXDeviceAction> deviceActions);

        /// <summary>
        /// Device actions are clear.
        /// </summary>
        Task DeviceActionClear(IEnumerable<GXDevice>? devices);

        /// <summary>
        /// New REST statistic is added or modified.
        /// </summary>
        /// <param name="statistics">Updated statistics.</param>
        Task RestStatisticAdd(IEnumerable<GXRestStatistic> statistics);

        /// <summary>
        /// REST statistics are clear for the users.
        /// </summary>
        /// <param name="users">Users whose statistics are cleared</param>
        Task RestStatisticClear(IEnumerable<GXUser>? users);

        /// <summary>
        /// New language is added or modified.
        /// </summary>
        /// <param name="scripts">Updated languages.</param>
        Task LanguageUpdate(IEnumerable<GXLanguage> language);

        /// <summary>
        /// New role is added or modified.
        /// </summary>
        /// <param name="roles">Updated roles.</param>
        Task RoleUpdate(IEnumerable<GXRole> roles);

        /// <summary>
        /// Component view is deleted.
        /// </summary>
        /// <param name="roles">Deleted roles.</param>
        Task RoleDelete(IEnumerable<string> roles);

        /// <summary>
        /// Cron started.
        /// </summary>
        Task CronStart();

        /// <summary>
        /// Cron compleated.
        /// </summary>
        Task CronCompleate();

        /// <summary>
        /// New user settings is added or modified.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="settings">Updated user settings.</param>
        Task UserSettingUpdate(IEnumerable<GXUserSetting> settings);

        /// <summary>
        /// User setting is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="settings">Deleted user settings.</param>
        Task UserSettingDelete(IEnumerable<GXUserSetting> settings);

        /// <summary>
        /// New manufacturer is added or modified.
        /// </summary>
        /// <param name="manufacturers">Updated manufacturers.</param>
        Task ManufacturerUpdate(IEnumerable<GXManufacturer> manufacturers);

        /// <summary>
        /// Manufacturer is deleted.
        /// </summary>
        /// <param name="manufacturers">Deleted manufacturers.</param>
        Task ManufacturerDelete(IEnumerable<GXManufacturer> manufacturers);

        /// <summary>
        /// New manufacturer group is added or modified.
        /// </summary>
        /// <param name="manufacturers">Updated manufacturer groups.</param>
        Task ManufacturerGroupUpdate(IEnumerable<GXManufacturerGroup> groups);

        /// <summary>
        /// Manufacturer group is deleted.
        /// </summary>
        /// <param name="manufacturers">Deleted manufacturer groups.</param>
        Task ManufacturerGroupDelete(IEnumerable<GXManufacturerGroup> groups);

        /// <summary>
        /// Favorite is added or modified.
        /// </summary>
        /// <param name="groups">Updated favorites.</param>
        Task FavoriteUpdate(IEnumerable<GXFavorite> favorites);

        /// <summary>
        /// Favorite is deleted.
        /// </summary>
        /// <param name="favorites">Deleted favorites.</param>
        Task FavoriteDelete(IEnumerable<GXFavorite> favorites);

        /// <summary>
        /// New key management is added or modified.
        /// </summary>
        /// <param name="key managements">Updated key managements.</param>
        Task KeyManagementUpdate(IEnumerable<GXKeyManagement> keys);

        /// <summary>
        /// Key management is deleted.
        /// </summary>
        /// <param name="key managements">Deleted key managements.</param>
        Task KeyManagementDelete(IEnumerable<GXKeyManagement> keys);

        /// <summary>
        /// New key management group is added or modified.
        /// </summary>
        /// <param name="key managements">Updated key management groups.</param>
        Task KeyManagementGroupUpdate(IEnumerable<GXKeyManagementGroup> groups);

        /// <summary>
        /// Key management group is deleted.
        /// </summary>
        /// <param name="key managements">Deleted key management groups.</param>
        Task KeyManagementGroupDelete(IEnumerable<GXKeyManagementGroup> groups);

        /// <summary>
        /// Key management logs are cleared.
        /// </summary>
        /// <param name="logs">List of cleared logs.</param>
        Task ClearKeyManagementLogs(IEnumerable<GXKeyManagement>? logs);

        /// <summary>
        /// New key management log is added.
        /// </summary>
        /// <param name="logs">New key management logs.</param>
        Task AddKeyManagementLogs(IEnumerable<GXKeyManagementLog> logs);

        /// <summary>
        /// Key management logs are closed.
        /// </summary>
        /// <param name="errors">Closed key management logs.</param>
        Task CloseKeyManagementLogs(IEnumerable<GXKeyManagementLog> errors);

        /// <summary>
        /// Gateway logs are cleared.
        /// </summary>
        /// <param name="gateways">List of cleared gateways.</param>
        Task ClearGatewayLogs(IEnumerable<GXGateway>? gateways);

        /// <summary>
        /// New gateway logs is added.
        /// </summary>
        /// <param name="logs">New gateway logs.</param>
        Task AddGatewayLogs(IEnumerable<GXGatewayLog> logs);

        /// <summary>
        /// Gateway log items are closed.
        /// </summary>
        /// <param name="gateways">Closed log items.</param>
        Task CloseGatewayLogs(IEnumerable<GXGatewayLog> gateways);

        /// <summary>
        /// New gateway is added or modified.
        /// </summary>
        /// <param name="gateways">Updated gateways.</param>
        Task GatewayUpdate(IEnumerable<GXGateway> gateways);

        /// <summary>
        /// Gateway is deleted.
        /// </summary>
        /// <param name="gateways">Deleted gateways.</param>
        Task GatewayDelete(IEnumerable<GXGateway> gateways);

        /// <summary>
        /// Gateway status has changed.
        /// </summary>
        /// <param name="gateways">Gateways.</param>
        Task GatewayStatusChange(IEnumerable<GXGateway> gateways);

        /// <summary>
        /// New gateway group is added or modified.
        /// </summary>
        /// <param name="groups">Updated gateway groups.</param>
        Task GatewayGroupUpdate(IEnumerable<GXGatewayGroup> groups);

        /// <summary>
        /// Gateway group is deleted.
        /// </summary>
        /// <param name="groups">Deleted gateway group.</param>
        Task GatewayGroupDelete(IEnumerable<GXGatewayGroup> groups);

        /// <summary>
        /// Device status has changed.
        /// </summary>
        /// <param name="devices">Devices.</param>
        Task DeviceStatusChange(IEnumerable<GXDevice> devices);

        /// <summary>
        /// New performance is added.
        /// </summary>
        /// <param name="statistics">Updated performances.</param>
        Task PerformanceAdd(IEnumerable<GXPerformance> performance);

        /// <summary>
        /// Performances are clear.
        /// </summary>
        Task PerformanceClear();

        /// <summary>
        /// Performances are removed.
        /// </summary>
        /// <param name="statistics">Deleted performances.</param>
        Task PerformanceDelete(IEnumerable<Guid> performance);

        /// <summary>
        /// Subtotals are updated or modified.
        /// </summary>
        /// <param name="subtotals">Updated subtotals.</param>
        Task SubtotalUpdate(IEnumerable<GXSubtotal> subtotals);

        /// <summary>
        /// Subtotal is deleted.
        /// </summary>
        /// <param name="subtotals">Deleted subtotal.</param>
        Task SubtotalDelete(IEnumerable<GXSubtotal> subtotals);

        /// <summary>
        /// Subtotals are calculated.
        /// </summary>
        /// <param name="subtotals">Calculated subtotals.</param>
        Task SubtotalCalculate(IEnumerable<GXSubtotal> subtotals);

        /// <summary>
        /// Subtotals are cleared.
        /// </summary>
        /// <param name="subtotals">Cleared subtotals.</param>
        Task SubtotalClear(IEnumerable<GXSubtotal> subtotals);

        /// <summary>
        /// New subtotal group is added or modified.
        /// </summary>
        /// <param name="groups">Updated subtotal groups.</param>
        Task SubtotalGroupUpdate(IEnumerable<GXSubtotalGroup> groups);

        /// <summary>
        /// Subtotal group is deleted.
        /// </summary>
        /// <param name="groups">Deleted subtotal groups.</param>
        Task SubtotalGroupDelete(IEnumerable<GXSubtotalGroup> groups);

        /// <summary>
        /// Subtotals values are updated.
        /// </summary>
        /// <param name="values">Updated subtotal values.</param>
        Task SubtotalValueUpdate(IEnumerable<GXSubtotalValue> values);

        /// <summary>
        /// Subtotal logs are cleared.
        /// </summary>
        /// <param name="subtotals">List of cleared subtotals.</param>
        Task ClearSubtotalLogs(IEnumerable<GXSubtotal>? subtotals);

        /// <summary>
        /// New subtotal logs is added.
        /// </summary>
        /// <param name="logs">New subtotal logs.</param>
        Task AddSubtotalLogs(IEnumerable<GXSubtotalLog> logs);

        /// <summary>
        /// Subtotal log items are closed.
        /// </summary>
        /// <param name="subtotals">Closed log items.</param>
        Task CloseSubtotalLogs(IEnumerable<GXSubtotalLog> subtotals);

        /// <summary>
        /// Reports are updated or modified.
        /// </summary>
        /// <param name="reports">Updated reports.</param>
        Task ReportUpdate(IEnumerable<GXReport> reports);

        /// <summary>
        /// Report is deleted.
        /// </summary>
        /// <param name="reports">Deleted report.</param>
        Task ReportDelete(IEnumerable<GXReport> reports);

        /// <summary>
        /// Reports are calculated.
        /// </summary>
        /// <param name="reports">Calculated reports.</param>
        Task ReportCalculate(IEnumerable<GXReport> reports);

        /// <summary>
        /// Reports are cleared.
        /// </summary>
        /// <param name="reports">Cleared reports.</param>
        Task ReportClear(IEnumerable<GXReport> reports);

        /// <summary>
        /// New report group is added or modified.
        /// </summary>
        /// <param name="groups">Updated report groups.</param>
        Task ReportGroupUpdate(IEnumerable<GXReportGroup> groups);

        /// <summary>
        /// Report group is deleted.
        /// </summary>
        /// <param name="groups">Deleted report groups.</param>
        Task ReportGroupDelete(IEnumerable<GXReportGroup> groups);

        /// <summary>
        /// Report logs are cleared.
        /// </summary>
        /// <param name="reports">List of cleared reports.</param>
        Task ClearReportLogs(IEnumerable<GXReport>? reports);

        /// <summary>
        /// New report logs is added.
        /// </summary>
        /// <param name="logs">New report logs.</param>
        Task AddReportLogs(IEnumerable<GXReportLog> logs);

        /// <summary>
        /// Report log items are closed.
        /// </summary>
        /// <param name="reports">Closed log items.</param>
        Task CloseReportLogs(IEnumerable<GXReportLog> reports);

        /// <summary>
        /// User stamp is added or updated.
        /// </summary>
        /// <param name="stamps">Updated user stamps.</param>
        Task StampUpdate(IEnumerable<GXUserStamp> stamps);
    }
}
