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

namespace Gurux.DLMS.AMI.Client.Shared
{
    /// <summary>
    /// Events notifier interface.
    /// </summary>
    public interface IGXEventsNotifier
    {
        /// <summary>
        /// Configuration is saved.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="configurations">List of saved configurations.</param>
        Task ConfigurationSave(IReadOnlyList<string> users, IEnumerable<GXConfiguration> configurations);

        /// <summary>
        /// Module settings are saved.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="module">Updated module.</param>
        Task ModuleSettingsSave(IReadOnlyList<string> users, GXModule module);

        /// <summary>
        /// System errors are cleared.
        /// </summary>      
        /// <param name="users">Notified users.</param>
        Task ClearSystemLogs(IReadOnlyList<string> users);

        /// <summary>
        /// New system error is added.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="errors">New system errors.</param>
        Task AddSystemLogs(IReadOnlyList<string> users, IEnumerable<GXSystemLog> errors);

        /// <summary>
        /// System errors are closed.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="errors">Closed system errors.</param>
        Task CloseSystemLogs(IReadOnlyList<string> users, IEnumerable<GXSystemLog> errors);

        /// <summary>
        /// Device errors are cleared.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="devices">List of cleared devices.</param>
        Task ClearDeviceErrors(IReadOnlyList<string> users, IEnumerable<GXDevice>? devices);

        /// <summary>
        /// New device error is added.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="errors">New device errors.</param>
        Task AddDeviceErrors(IReadOnlyList<string> users, IEnumerable<GXDeviceError> errors);

        /// <summary>
        /// Device errors are closed.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="item">Closed errors.</param>
        Task CloseDeviceErrors(IReadOnlyList<string> users, IEnumerable<GXDeviceError> item);

        /// <summary>
        /// Workflow logs are cleared.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="workflows">List of cleared workflows.</param>
        Task ClearWorkflowLogs(IReadOnlyList<string> users, IEnumerable<GXWorkflow>? workflows);

        /// <summary>
        /// New workflow log is added.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="errors">New workflow log.</param>
        Task AddWorkflowLogs(IReadOnlyList<string> users, IEnumerable<GXWorkflowLog> errors);

        /// <summary>
        /// Workflow logs are closed.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="errors">Closed workflow logs.</param>
        Task CloseWorkflowLogs(IReadOnlyList<string> users, IEnumerable<GXWorkflowLog> errors);

        /// <summary>
        /// Schedule errors are cleared.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="schedules">List of cleared schedules.</param>
        Task ClearScheduleLog(IReadOnlyList<string> users, IEnumerable<GXSchedule>? schedules);

        /// <summary>
        /// New schedule logs are added.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="logs">New schedule logs.</param>
        Task AddScheduleLog(IReadOnlyList<string> users, IEnumerable<GXScheduleLog> logs);

        /// <summary>
        /// Schedule logs are closed.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="logs">Closed schedule logs.</param>
        Task CloseScheduleLog(IReadOnlyList<string> users, IEnumerable<GXScheduleLog> logs);

        /// <summary>
        /// Schedule is started.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="schedules">Started schedules.</param>
        Task ScheduleStart(IReadOnlyList<string> users, IEnumerable<GXSchedule> schedules);

        /// <summary>
        /// Schedules are compleated.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="schedules">Compleated schedules.</param>
        Task ScheduleCompleate(IReadOnlyList<string> users, IEnumerable<GXSchedule> schedules);

        /// <summary>
        /// Script log are cleared.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="scripts">List of cleared scripts.</param>
        Task ClearScriptLogs(IReadOnlyList<string> users, IEnumerable<GXScript>? scripts);

        /// <summary>
        /// New Script log is added.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="logs">New script logs.</param>
        Task AddScriptLogs(IReadOnlyList<string> users, IEnumerable<GXScriptLog> logs);

        /// <summary>
        /// Script logs are closed.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="logs">Closed script logs.</param>
        Task CloseScriptLogs(IReadOnlyList<string> users, IEnumerable<GXScriptLog> logs);

        /// <summary>
        /// User errors are cleared.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="Users">List of cleared users.</param>
        Task ClearUserErrors(IReadOnlyList<string> users, IEnumerable<GXUser>? Users);

        /// <summary>
        /// New User error is added.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="item">New user error.</param>
        Task AddUserErrors(IReadOnlyList<string> users, IEnumerable<GXUserError> item);

        /// <summary>
        /// User errors are closed.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="errors">Closed user errors.</param>
        Task CloseUserErrors(IReadOnlyList<string> users, IEnumerable<GXUserError> errors);


        /// <summary>
        /// Module logs are cleared.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="modules">List of cleared modules.</param>
        Task ClearModuleLogs(IReadOnlyList<string> users, IEnumerable<GXModule>? modules);

        /// <summary>
        /// New module log is added.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="item">New module log.</param>
        Task AddModuleLogs(IReadOnlyList<string> users, IEnumerable<GXModuleLog> item);

        /// <summary>
        /// Module logs are closed.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="item">Closed logs.</param>
        Task CloseModuleLogs(IReadOnlyList<string> users, IEnumerable<GXModuleLog> item);


        /// <summary>
        /// Agent logs are cleared.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="agents">List of cleared agents.</param>
        Task ClearAgentLogs(IReadOnlyList<string> users, IEnumerable<GXAgent>? agents);

        /// <summary>
        /// New agent log item is added.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="agents">New agent errors.</param>
        Task AddAgentLogs(IReadOnlyList<string> users, IEnumerable<GXAgentLog> agents);

        /// <summary>
        /// Agent logs are closed.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="agents">Closed errors.</param>
        Task CloseAgentLogs(IReadOnlyList<string> users, IEnumerable<GXAgentLog> agents);


        /// <summary>
        /// New Schedule is added.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="schedules">New schedules.</param>
        Task ScheduleUpdate(IReadOnlyList<string> users, IEnumerable<GXSchedule> schedules);

        /// <summary>
        /// Schedule is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="schedules">Deleted schedules.</param>
        Task ScheduleDelete(IReadOnlyList<string> users, IEnumerable<GXSchedule> schedules);

        /// <summary>
        /// New Schedule group is added.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="groups">New schedule groups.</param>
        Task ScheduleGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXScheduleGroup> groups);

        /// <summary>
        /// Schedule group is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="groups">Deleted schedule groups.</param>
        Task ScheduleGroupDelete(IReadOnlyList<string> users, IEnumerable<GXScheduleGroup> groups);

        /// <summary>
        /// New user is added or modified.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="Users">Updated users.</param>
        Task UserUpdate(IReadOnlyList<string> users, IEnumerable<GXUser> Users);

        /// <summary>
        /// User is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="Utem">Deleted user.</param>
        Task UserDelete(IReadOnlyList<string> users, IEnumerable<GXUser> Users);

        /// <summary>
        /// New user group is added or modified.
        /// </summary>
        /// <param name="groups">Updated user groups.</param>
        Task UserGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXUserGroup> groups);

        /// <summary>
        /// User group is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="groups">Deleted user groups.</param>
        Task UserGroupDelete(IReadOnlyList<string> users, IEnumerable<GXUserGroup> groups);

        /// <summary>
        /// New device is added or modified.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="devices">Updated devices.</param>
        Task DeviceUpdate(IReadOnlyList<string> users, IEnumerable<GXDevice> devices);

        /// <summary>
        /// Device is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="devices">Deleted devices.</param>
        Task DeviceDelete(IReadOnlyList<string> users, IEnumerable<GXDevice> devices);

        /// <summary>
        /// New device group is added or modified.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="groups">Updated device groups.</param>
        Task DeviceGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXDeviceGroup> groups);

        /// <summary>
        /// Device group is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="groups">Deleted device groups.</param>
        Task DeviceGroupDelete(IReadOnlyList<string> users, IEnumerable<GXDeviceGroup> groups);

        /// <summary>
        /// New device template group is added or modified.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="groups">Updated device template groups.</param>
        Task DeviceTemplateGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXDeviceTemplateGroup> groups);

        /// <summary>
        /// Device template group is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="groups">Deleted device template groups.</param>
        Task DeviceTemplateGroupDelete(IReadOnlyList<string> users, IEnumerable<GXDeviceTemplateGroup> groups);

        /// <summary>
        /// New device template is added or modified.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="templates">Updated device template group.</param>
        Task DeviceTemplateUpdate(IReadOnlyList<string> users, IEnumerable<GXDeviceTemplate> templates);

        /// <summary>
        /// Device template is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="templates">Deleted device templates.</param>
        Task DeviceTemplateDelete(IReadOnlyList<string> users, IEnumerable<GXDeviceTemplate> templates);

        /// <summary>
        /// New object template is added or modified.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="objects">Updated object templates.</param>
        Task ObjectTemplateUpdate(IReadOnlyList<string> users, IEnumerable<GXObjectTemplate> templates);

        /// <summary>
        /// Object template is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="objects">Deleted objects templates.</param>
        Task ObjectTemplateDelete(IReadOnlyList<string> users, IEnumerable<GXObjectTemplate> templates);

        /// <summary>
        /// New agent is added or modified.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="agents">Updated agents.</param>
        Task AgentUpdate(IReadOnlyList<string> users, IEnumerable<GXAgent> agents);

        /// <summary>
        /// Agent is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="agents">Deleted agents.</param>
        Task AgentDelete(IReadOnlyList<string> users, IEnumerable<GXAgent> agents);

        /// <summary>
        /// Agent status has changed.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="agents">Agents.</param>
        Task AgentStatusChange(IReadOnlyList<string> users, IEnumerable<GXAgent> agents);

        /// <summary>
        /// New agent group is added or modified.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="groups">Updated agent groups.</param>
        Task AgentGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXAgentGroup> groups);

        /// <summary>
        /// Agent group is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="groups">Deleted agent group.</param>
        Task AgentGroupDelete(IReadOnlyList<string> users, IEnumerable<GXAgentGroup> groups);

        /// <summary>
        /// New workflow is added or modified.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="workflows">Updated workflow.</param>
        Task WorkflowUpdate(IReadOnlyList<string> users, IEnumerable<GXWorkflow> workflows);

        /// <summary>
        /// Workflow is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="workflows">Deleted workflow.</param>
        Task WorkflowDelete(IReadOnlyList<string> users, IEnumerable<GXWorkflow> workflows);

        /// <summary>
        /// New workflow group is added or modified.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="groups">Updated workflow groups.</param>
        Task WorkflowGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXWorkflowGroup> groups);

        /// <summary>
        /// Workflow group is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="groups">Deleted workflow groups.</param>
        Task WorkflowGroupDelete(IReadOnlyList<string> users, IEnumerable<GXWorkflowGroup> groups);

        /// <summary>
        /// New trigger is added or modified.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="triggers">Updated triggers.</param>
        Task TriggerUpdate(IReadOnlyList<string> users, IEnumerable<GXTrigger> triggers);

        /// <summary>
        /// Trigger is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="triggers">Deleted triggers.</param>
        Task TriggerDelete(IReadOnlyList<string> users, IEnumerable<GXTrigger> triggers);

        /// <summary>
        /// New trigger group is added or modified.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="groups">Updated trigger groups.</param>
        Task TriggerGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXTriggerGroup> groups);

        /// <summary>
        /// Trigger group is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="groups">Deleted trigger groups.</param>
        Task TriggerGroupDelete(IReadOnlyList<string> users, IEnumerable<GXTriggerGroup> groups);

        /// <summary>
        /// New module is added or modified.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="modules">Updated modules.</param>
        Task ModuleUpdate(IReadOnlyList<string> users, IEnumerable<GXModule> modules);

        /// <summary>
        /// Module is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="modules">Deleted modules.</param>
        Task ModuleDelete(IReadOnlyList<string> users, IEnumerable<GXModule> modules);

        /// <summary>
        /// New module group is added or modified.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="groups">Updated module groups.</param>
        Task ModuleGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXModuleGroup> groups);

        /// <summary>
        /// Module group is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="groups">Deleted module groups.</param>
        Task ModuleGroupDelete(IReadOnlyList<string> users, IEnumerable<GXModuleGroup> groups);


        /// <summary>
        /// New object is added or modified.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="objects">Updated objects.</param>
        Task ObjectUpdate(IReadOnlyList<string> users, IEnumerable<GXObject> objects);

        /// <summary>
        /// Object is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="objects">Deleted objects.</param>
        Task ObjectDelete(IReadOnlyList<string> users, IEnumerable<GXObject> objects);

        /// <summary>
        /// New attribute is added or modified.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="attributes">Updated attributes.</param>
        Task AttributeUpdate(IReadOnlyList<string> users, IEnumerable<GXAttribute> attributes);

        /// <summary>
        /// Attribute is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="attributes">Deleted attribute.</param>
        Task AttributeDelete(IReadOnlyList<string> users, IEnumerable<GXAttribute> attributes);

        /// <summary>
        /// Attribute value is updated.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="attributes">Updated attribute values.</param>
        Task ValueUpdate(IReadOnlyList<string> users, IEnumerable<GXAttribute> attributes);

        /// <summary>
        /// New task is added.
        /// </summary>
        /// <param name="tasks">Added tasks.</param>
        Task TaskAdd(IReadOnlyList<string> users, IEnumerable<GXTask> tasks);

        /// <summary>
        /// Task is update.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="tasks">Updated tasks.</param>
        Task TaskUpdate(IReadOnlyList<string> users, IEnumerable<GXTask> tasks);

        /// <summary>
        /// Task is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="tasks">Deleted tasks.</param>
        Task TaskDelete(IReadOnlyList<string> users, IEnumerable<GXTask> tasks);

        /// <summary>
        /// Tasks are cleared.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="items">List of cleared users.</param>
        /// <remarks>
        /// Items is null if admin clears all the tasks.
        /// </remarks>
        Task TaskClear(IReadOnlyList<string> users, IEnumerable<GXUser>? items);

        /// <summary>
        /// Blocks are updated or modified.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="blocks">Updated blocks.</param>
        Task BlockUpdate(IReadOnlyList<string> users, IEnumerable<GXBlock> blocks);

        /// <summary>
        /// Block is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="blocks">Deleted block.</param>
        Task BlockDelete(IReadOnlyList<string> users, IEnumerable<GXBlock> blocks);

        /// <summary>
        /// Blocks are closed.
        /// </summary>
        /// <param name="blocks">Closed blocks.</param>
        Task BlockClose(IReadOnlyList<string> users, IEnumerable<GXBlock> blocks);

        /// <summary>
        /// New block group is added or modified.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="groups">Updated block groups.</param>
        Task BlockGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXBlockGroup> groups);

        /// <summary>
        /// Block group is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="groups">Deleted block groups.</param>
        Task BlockGroupDelete(IReadOnlyList<string> users, IEnumerable<GXBlockGroup> groups);

        /// <summary>
        /// New user action is added.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="userActions">Updated user actions.</param>
        Task UserActionAdd(IReadOnlyList<string> users, IEnumerable<GXUserAction> userActions);

        /// <summary>
        /// User action is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="userActions">Deleted user actions.</param>
        Task UserActionDelete(IReadOnlyList<string> users, IEnumerable<GXUserAction> userActions);

        /// <summary>
        /// User actions are clear.
        /// </summary>
        /// <param name="users">Notified users.</param>
        Task UserActionsClear(IReadOnlyList<string> users, IEnumerable<GXUser>? Users);

        /// <summary>
        /// New component view is added or modified.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="componentViews">Updated component views.</param>
        Task ComponentViewUpdate(IReadOnlyList<string> users, IEnumerable<GXComponentView> componentViews);

        /// <summary>
        /// Component view is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="componentViews">Deleted component views.</param>
        Task ComponentViewDelete(IReadOnlyList<string> users, IEnumerable<GXComponentView> componentViews);

        /// <summary>
        /// New component view group is added or modified.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="groups">Updated component view groups.</param>
        Task ComponentViewGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXComponentViewGroup> groups);

        /// <summary>
        /// Component view group is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="groups">Deleted component view groups.</param>
        Task ComponentViewGroupDelete(IReadOnlyList<string> users, IEnumerable<GXComponentViewGroup> groups);

        /// <summary>
        /// New script is added or modified.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="scripts">Updated scripts.</param>
        Task ScriptUpdate(IReadOnlyList<string> users, IEnumerable<GXScript> scripts);

        /// <summary>
        /// Component view is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="scripts">Deleted scripts.</param>
        Task ScriptDelete(IReadOnlyList<string> users, IEnumerable<GXScript> scripts);

        /// <summary>
        /// New script group is added or modified.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="groups">Updated script groups.</param>
        Task ScriptGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXScriptGroup> groups);

        /// <summary>
        /// Component view group is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="groups">Deleted script groups.</param>
        Task ScriptGroupDelete(IReadOnlyList<string> users, IEnumerable<GXScriptGroup> groups);

        /// <summary>
        /// New device trace is added.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="deviceTraces">Updated device trace.</param>
        Task DeviceTraceAdd(IReadOnlyList<string> users, IEnumerable<GXDeviceTrace> deviceTraces);

        /// <summary>
        /// Device traces are clear.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="devices">Devices</param>
        Task DeviceTraceClear(IReadOnlyList<string> users, IEnumerable<GXDevice>? devices);

        /// <summary>
        /// New device action is added.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="deviceActions">Updated user action.</param>
        Task DeviceActionAdd(IReadOnlyList<string> users, IEnumerable<GXDeviceAction> deviceActions);

        /// <summary>
        /// Device actions are clear.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="devices">Devices</param>
        Task DeviceActionsClear(IReadOnlyList<string> users, IEnumerable<GXDevice>? devices);

        /// <summary>
        /// New REST statistic is added or modified.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="statistics">Updated statistics.</param>
        Task RestStatisticAdd(IReadOnlyList<string> users, IEnumerable<GXRestStatistic> statistics);

        /// <summary>
        /// REST statistics are clear for the users.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="users">Users whose statistics are cleared</param>
        Task RestStatisticClear(IReadOnlyList<string> users, IEnumerable<GXUser>? Users);

        /// <summary>
        /// New language is added or modified.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="scripts">Updated languages.</param>
        Task LanguageUpdate(IReadOnlyList<string> users, IEnumerable<GXLanguage> language);

        /// <summary>
        /// New role is added or modified.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="roles">Updated roles.</param>
        Task RoleUpdate(IReadOnlyList<string> users, IEnumerable<GXRole> roles);

        /// <summary>
        /// Component view is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="roles">Deleted roles.</param>
        Task RoleDelete(IReadOnlyList<string> users, IEnumerable<string> roles);

        /// <summary>
        /// Cron is started.
        /// </summary>
        /// <param name="users">Notified users.</param>
        Task CronStart(IReadOnlyList<string> users);

        /// <summary>
        /// Cron is compleated.
        /// </summary>
        /// <param name="users">Notified users.</param>
        Task CronCompleate(IReadOnlyList<string> users);

        /// <summary>
        /// New user settings is added or modified.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="settings">Updated user settings.</param>
        Task UserSettingUpdate(IReadOnlyList<string> users, IEnumerable<GXUserSetting> settings);

        /// <summary>
        /// User setting is deleted.
        /// </summary>
        /// <param name="users">Notified users.</param>
        /// <param name="settings">Deleted user settings.</param>
        Task UserSettingDelete(IReadOnlyList<string> users, IEnumerable<GXUserSetting> settings);
    }
}
