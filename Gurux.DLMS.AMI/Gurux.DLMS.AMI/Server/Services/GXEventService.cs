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

using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Server.Triggers;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Microsoft.AspNetCore.SignalR;

namespace Gurux.DLMS.AMI.Services
{
    /// <summary>
    /// This service is used to handle Gurux.DLMS.AMI.Server events.
    /// </summary>
    public class GXEventService : IGXEventsNotifier, IGXEventsListener
    {
        private readonly IHubContext<GuruxAMiHub, IGXHubEvents> _hubContext;
        private readonly IWorkflowHandler _workflowHandler;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="workflowHandler">Workflow handler.</param>
        /// <param name="hubContext">Hub context</param>
        public GXEventService(IWorkflowHandler workflowHandler,
            IHubContext<GuruxAMiHub, IGXHubEvents> hubContext)
        {
            _hubContext = hubContext;
            _workflowHandler = workflowHandler;
        }

        /// <inheritdoc/>
        public event Action<IEnumerable<GXConfiguration>>? OnConfigurationSave;
        /// <inheritdoc/>
        public event Action<GXModule>? OnModuleSettingsSave;
        /// <inheritdoc/>
        public event Action? OnClearSystemLogs;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXSystemLog>>? OnAddSystemLogs;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXSystemLog>>? OnCloseSystemLogs;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXDevice>?>? OnClearDeviceErrors;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXDeviceError>>? OnAddDeviceErrors;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXDeviceError>>? OnCloseDeviceErrors;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXWorkflow>?>? OnClearWorkflowLogs;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXWorkflowLog>>? OnAddWorkflowLogs;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXWorkflowLog>>? OnCloseWorkflowLogs;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXSchedule>?>? OnClearScheduleLog;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXScheduleLog>>? OnAddScheduleLog;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXScheduleLog>>? OnCloseScheduleLog;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXScript>?>? OnClearScriptLogs;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXScriptLog>>? OnAddScriptLogs;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXScriptLog>>? OnCloseScriptLogs;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXUser>?>? OnClearUserErrors;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXUserError>>? OnAddUserErrors;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXUserError>>? OnCloseUserErrors;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXModule>?>? OnClearModuleLogs;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXModuleLog>>? OnAddModuleLogs;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXModuleLog>>? OnCloseModuleLogs;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXAgent>?>? OnClearAgentErrors;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXAgentLog>>? OnAddAgentErrors;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXAgentLog>>? OnCloseAgentErrors;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXSchedule>>? OnScheduleUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXSchedule>>? OnScheduleDelete;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXSchedule>>? OnScheduleStart;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXSchedule>>? OnScheduleCompleate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXScheduleGroup>>? OnScheduleGroupUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXScheduleGroup>>? OnScheduleGroupDelete;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXUser>>? OnUserUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXUser>>? OnUserDelete;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXUserGroup>>? OnUserGroupUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXUserGroup>>? OnUserGroupDelete;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXDevice>>? OnDeviceUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXDevice>>? OnDeviceDelete;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXDeviceGroup>>? OnDeviceGroupUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXDeviceGroup>>? OnDeviceGroupDelete;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXDeviceTemplateGroup>>? OnDeviceTemplateGroupUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXDeviceTemplateGroup>>? OnDeviceTemplateGroupDelete;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXDeviceTemplate>>? OnDeviceTemplateUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXDeviceTemplate>>? OnDeviceTemplateDelete;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXObjectTemplate>>? OnObjectTemplateUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXObjectTemplate>>? OnObjectTemplateDelete;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXAttributeTemplate>>? OnAttributeTemplateUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXAttributeTemplate>>? OnAttributeTemplateDelete;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXAgent>>? OnAgentUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXAgent>>? OnAgentDelete;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXAgent>>? OnAgentStatusChange;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXAgentGroup>>? OnAgentGroupUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXAgentGroup>>? OnAgentGroupDelete;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXWorkflow>>? OnWorkflowUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXWorkflow>>? OnWorkflowDelete;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXWorkflowGroup>>? OnWorkflowGroupUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXWorkflowGroup>>? OnWorkflowGroupDelete;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXTrigger>>? OnTriggerUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXTrigger>>? OnTriggerDelete;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXTriggerGroup>>? OnTriggerGroupUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXTriggerGroup>>? OnTriggerGroupDelete;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXModule>>? OnModuleUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXModule>>? OnModuleDelete;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXModuleGroup>>? OnModuleGroupUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXModuleGroup>>? OnModuleGroupDelete;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXObject>>? OnObjectUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXObject>>? OnObjectDelete;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXAttribute>>? OnAttributeUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXAttribute>>? OnAttributeDelete;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXAttribute>>? OnValueUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXTask>>? OnTaskAdd;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXTask>>? OnTaskUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXTask>>? OnTaskDelete;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXUser>?>? OnTaskClear;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXBlock>>? OnBlockUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXBlock>>? OnBlockDelete;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXBlock>>? OnBlockClose;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXBlockGroup>>? OnBlockGroupUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXBlockGroup>>? OnBlockGroupDelete;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXUserAction>>? OnUserActionAdd;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXUserAction>>? OnUserActionDelete;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXUser>?>? OnUserActionsClear;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXComponentView>>? OnComponentViewUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXComponentView>>? OnComponentViewDelete;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXComponentViewGroup>>? OnComponentViewGroupUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXComponentViewGroup>>? OnComponentViewGroupDelete;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXScript>>? OnScriptUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXScript>>? OnScriptDelete;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXScriptGroup>>? OnScriptGroupUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXScriptGroup>>? OnScriptGroupDelete;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXDeviceTrace>>? OnDeviceTraceAdd;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXDevice>?>? OnDeviceTraceClear;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXDeviceAction>>? OnDeviceActionAdd;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXDevice>?>? OnDeviceActionsClear;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXRestStatistic>>? OnRestStatisticAdd;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXUser>?>? OnRestStatisticClear;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXLanguage>>? OnLanguageUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<GXRole>>? OnRoleUpdate;
        /// <inheritdoc/>
        public event Action<IEnumerable<string>>? OnRoleDelete;
        /// <inheritdoc/>
        public event Action OnCronStart;
        /// <inheritdoc/>
        public event Action OnCronCompleate;

        /// <inheritdoc/>
        public async Task AddAgentLogs(IReadOnlyList<string> users, IEnumerable<GXAgentLog> agents)
        {
            OnAddAgentErrors?.Invoke(agents);
            await _hubContext.Clients.Users(users).AddAgentLogs(agents);
        }

        /// <inheritdoc/>
        public async Task AddDeviceErrors(IReadOnlyList<string> users, IEnumerable<GXDeviceError> errors)
        {
            _workflowHandler.Execute(typeof(DeviceTrigger), DeviceTrigger.Error, errors);
            OnAddDeviceErrors?.Invoke(errors);
            await _hubContext.Clients.Users(users).AddDeviceErrors(errors);
        }

        /// <inheritdoc/>
        public async Task AddModuleLogs(IReadOnlyList<string> users, IEnumerable<GXModuleLog> errors)
        {
            OnAddModuleLogs?.Invoke(errors);
            await _hubContext.Clients.Users(users).AddModuleLogs(errors);
        }

        /// <inheritdoc/>
        public async Task AddScheduleLog(IReadOnlyList<string> users, IEnumerable<GXScheduleLog> logs)
        {
            OnAddScheduleLog?.Invoke(logs);
            await _hubContext.Clients.Users(users).AddScheduleLog(logs);
        }

        /// <inheritdoc/>
        public async Task AddScriptLogs(IReadOnlyList<string> users, IEnumerable<GXScriptLog> errors)
        {
            OnAddScriptLogs?.Invoke(errors);
            await _hubContext.Clients.Users(users).AddScriptLogs(errors);
        }

        /// <inheritdoc/>
        public async Task AddSystemLogs(IReadOnlyList<string> users, IEnumerable<GXSystemLog> errors)
        {
            _workflowHandler.Execute(typeof(SystemLogTrigger), SystemLogTrigger.Add, errors);
            OnAddSystemLogs?.Invoke(errors);
            await _hubContext.Clients.Users(users).AddSystemLogs(errors);
        }

        /// <inheritdoc/>
        public async Task AddUserErrors(IReadOnlyList<string> users, IEnumerable<GXUserError> errors)
        {
            OnAddUserErrors?.Invoke(errors);
            await _hubContext.Clients.Users(users).AddUserErrors(errors);
        }

        /// <inheritdoc/>
        public async Task AddWorkflowLogs(IReadOnlyList<string> users, IEnumerable<GXWorkflowLog> errors)
        {
            OnAddWorkflowLogs?.Invoke(errors);
            await _hubContext.Clients.Users(users).AddWorkflowLogs(errors);
        }

        /// <inheritdoc/>
        public async Task AgentDelete(IReadOnlyList<string> users, IEnumerable<GXAgent> agents)
        {
            OnAgentDelete?.Invoke(agents);
            await _hubContext.Clients.Users(users).AgentDelete(agents);
        }

        /// <inheritdoc/>
        public async Task AgentGroupDelete(IReadOnlyList<string> users, IEnumerable<GXAgentGroup> groups)
        {
            OnAgentGroupDelete?.Invoke(groups);
            await _hubContext.Clients.Users(users).AgentGroupDelete(groups);
        }

        /// <inheritdoc/>
        public async Task AgentGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXAgentGroup> groups)
        {
            OnAgentGroupUpdate?.Invoke(groups);
            await _hubContext.Clients.Users(users).AgentGroupUpdate(groups);
        }

        /// <inheritdoc/>
        public async Task AgentStatusChange(IReadOnlyList<string> users, IEnumerable<GXAgent> agents)
        {
            OnAgentStatusChange?.Invoke(agents);
            await _hubContext.Clients.Users(users).AgentStatusChange(agents);
        }

        /// <inheritdoc/>
        public async Task AgentUpdate(IReadOnlyList<string> users, IEnumerable<GXAgent> agents)
        {
            OnAgentUpdate?.Invoke(agents);
            await _hubContext.Clients.Users(users).AgentUpdate(agents);
        }

        /// <inheritdoc/>
        public async Task AttributeDelete(IReadOnlyList<string> users, IEnumerable<GXAttribute> attributes)
        {
            OnAttributeDelete?.Invoke(attributes);
            await _hubContext.Clients.Users(users).AttributeDelete(attributes);
        }

        /// <inheritdoc/>
        public async Task AttributeUpdate(IReadOnlyList<string> users, IEnumerable<GXAttribute> attributes)
        {
            OnAttributeUpdate?.Invoke(attributes);
            await _hubContext.Clients.Users(users).AttributeUpdate(attributes);
        }

        /// <inheritdoc/>
        public async Task ValueUpdate(IReadOnlyList<string> users, IEnumerable<GXAttribute> attributes)
        {
            OnValueUpdate?.Invoke(attributes);
            await _hubContext.Clients.Users(users).ValueUpdate(attributes);
        }

        /// <inheritdoc/>
        public async Task BlockClose(IReadOnlyList<string> users, IEnumerable<GXBlock> blocks)
        {
            OnBlockClose?.Invoke(blocks);
            await _hubContext.Clients.Users(users).BlockClose(blocks);
        }

        /// <inheritdoc/>
        public async Task BlockDelete(IReadOnlyList<string> users, IEnumerable<GXBlock> blocks)
        {
            OnBlockDelete?.Invoke(blocks);
            await _hubContext.Clients.Users(users).BlockDelete(blocks);
        }

        /// <inheritdoc/>
        public async Task BlockGroupDelete(IReadOnlyList<string> users, IEnumerable<GXBlockGroup> groups)
        {
            OnBlockGroupDelete?.Invoke(groups);
            await _hubContext.Clients.Users(users).BlockGroupDelete(groups);
        }

        public async Task BlockGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXBlockGroup> groups)
        {
            OnBlockGroupUpdate?.Invoke(groups);
            await _hubContext.Clients.Users(users).BlockGroupUpdate(groups);
        }

        /// <inheritdoc/>
        public async Task BlockUpdate(IReadOnlyList<string> users, IEnumerable<GXBlock> blocks)
        {
            OnBlockUpdate?.Invoke(blocks);
            await _hubContext.Clients.Users(users).BlockUpdate(blocks);
        }

        /// <inheritdoc/>
        public async Task ClearAgentLogs(IReadOnlyList<string> users, IEnumerable<GXAgent>? agents)
        {
            OnClearAgentErrors?.Invoke(agents);
            await _hubContext.Clients.Users(users).ClearAgentLogs(agents);
        }

        /// <inheritdoc/>
        public async Task ClearDeviceErrors(IReadOnlyList<string> users, IEnumerable<GXDevice>? devices)
        {
            OnClearDeviceErrors?.Invoke(devices);
            await _hubContext.Clients.Users(users).ClearDeviceErrors(devices);
        }

        /// <inheritdoc/>
        public async Task ClearModuleLogs(IReadOnlyList<string> users, IEnumerable<GXModule>? modules)
        {
            OnClearModuleLogs?.Invoke(modules);
            await _hubContext.Clients.Users(users).ClearModuleLogs(modules);
        }

        /// <inheritdoc/>
        public async Task ClearScheduleLog(IReadOnlyList<string> users, IEnumerable<GXSchedule>? schedules)
        {
            OnClearScheduleLog?.Invoke(schedules);
            await _hubContext.Clients.Users(users).ClearScheduleLog(schedules);
        }

        /// <inheritdoc/>
        public async Task ClearScriptLogs(IReadOnlyList<string> users, IEnumerable<GXScript>? scripts)
        {
            OnClearScriptLogs?.Invoke(scripts);
            await _hubContext.Clients.Users(users).ClearScriptLogs(scripts);
        }

        /// <inheritdoc/>
        public async Task ClearSystemLogs(IReadOnlyList<string> users)
        {
            _workflowHandler.Execute(typeof(SystemLogTrigger), SystemLogTrigger.Clear, null);
            OnClearSystemLogs?.Invoke();
            await _hubContext.Clients.Users(users).ClearSystemLogs();
        }

        /// <inheritdoc/>
        public async Task ClearUserErrors(IReadOnlyList<string> users, IEnumerable<GXUser>? Users)
        {
            OnClearUserErrors?.Invoke(Users);
            await _hubContext.Clients.Users(users).ClearUserErrors(Users);
        }

        /// <inheritdoc/>
        public async Task ClearWorkflowLogs(IReadOnlyList<string> users, IEnumerable<GXWorkflow>? workflows)
        {
            OnClearWorkflowLogs?.Invoke(workflows);
            await _hubContext.Clients.Users(users).ClearWorkflowLogs(workflows);
        }

        /// <inheritdoc/>
        public async Task CloseAgentLogs(IReadOnlyList<string> users, IEnumerable<GXAgentLog> agents)
        {
            OnCloseAgentErrors?.Invoke(agents);
            await _hubContext.Clients.Users(users).CloseAgentLogs(agents);
        }

        /// <inheritdoc/>
        public async Task CloseDeviceErrors(IReadOnlyList<string> users, IEnumerable<GXDeviceError> errors)
        {
            OnCloseDeviceErrors?.Invoke(errors);
            await _hubContext.Clients.Users(users).CloseDeviceErrors(errors);
        }

        /// <inheritdoc/>
        public async Task CloseModuleLogs(IReadOnlyList<string> users, IEnumerable<GXModuleLog> errors)
        {
            OnCloseModuleLogs?.Invoke(errors);
            await _hubContext.Clients.Users(users).CloseModuleLogs(errors);
        }

        /// <inheritdoc/>
        public async Task CloseScheduleLog(IReadOnlyList<string> users, IEnumerable<GXScheduleLog> logs)
        {
            OnCloseScheduleLog?.Invoke(logs);
            await _hubContext.Clients.Users(users).CloseScheduleLog(logs);
        }

        /// <inheritdoc/>
        public async Task CloseScriptLogs(IReadOnlyList<string> users, IEnumerable<GXScriptLog> errors)
        {
            OnCloseScriptLogs?.Invoke(errors);
            await _hubContext.Clients.Users(users).CloseScriptLogs(errors);
        }

        /// <inheritdoc/>
        public async Task CloseSystemLogs(IReadOnlyList<string> users, IEnumerable<GXSystemLog> errors)
        {
            _workflowHandler.Execute(typeof(SystemLogTrigger), SystemLogTrigger.Close, errors);
            OnCloseSystemLogs?.Invoke(errors);
            await _hubContext.Clients.Users(users).CloseSystemLogs(errors);
        }

        /// <inheritdoc/>
        public async Task CloseUserErrors(IReadOnlyList<string> users, IEnumerable<GXUserError> errors)
        {
            OnCloseUserErrors?.Invoke(errors);
            await _hubContext.Clients.Users(users).CloseUserErrors(errors);
        }

        /// <inheritdoc/>
        public async Task CloseWorkflowLogs(IReadOnlyList<string> users, IEnumerable<GXWorkflowLog> errors)
        {
            OnCloseWorkflowLogs?.Invoke(errors);
            await _hubContext.Clients.Users(users).CloseWorkflowLogs(errors);
        }

        /// <inheritdoc/>
        public async Task ComponentViewDelete(IReadOnlyList<string> users, IEnumerable<GXComponentView> componentViews)
        {
            OnComponentViewDelete?.Invoke(componentViews);
            await _hubContext.Clients.Users(users).ComponentViewDelete(componentViews);
        }

        /// <inheritdoc/>
        public async Task ComponentViewGroupDelete(IReadOnlyList<string> users, IEnumerable<GXComponentViewGroup> groups)
        {
            OnComponentViewGroupDelete?.Invoke(groups);
            await _hubContext.Clients.Users(users).ComponentViewGroupDelete(groups);
        }

        /// <inheritdoc/>
        public async Task ComponentViewGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXComponentViewGroup> groups)
        {
            OnComponentViewGroupUpdate?.Invoke(groups);
            await _hubContext.Clients.Users(users).ComponentViewGroupUpdate(groups);
        }

        /// <inheritdoc/>
        public async Task ComponentViewUpdate(IReadOnlyList<string> users, IEnumerable<GXComponentView> componentViews)
        {
            OnComponentViewUpdate?.Invoke(componentViews);
            await _hubContext.Clients.Users(users).ComponentViewUpdate(componentViews);
        }

        /// <inheritdoc/>
        public async Task ConfigurationSave(IReadOnlyList<string> users, IEnumerable<GXConfiguration> configurations)
        {
            OnConfigurationSave?.Invoke(configurations);
            await _hubContext.Clients.Users(users).ConfigurationSave(configurations);
        }

        /// <inheritdoc/>
        public async Task DeviceActionAdd(IReadOnlyList<string> users, IEnumerable<GXDeviceAction> deviceActions)
        {
            OnDeviceActionAdd?.Invoke(deviceActions);
            await _hubContext.Clients.Users(users).DeviceActionAdd(deviceActions);
        }

        /// <inheritdoc/>
        public async Task DeviceActionsClear(IReadOnlyList<string> users, IEnumerable<GXDevice>? devices)
        {
            OnDeviceActionsClear?.Invoke(devices);
            await _hubContext.Clients.Users(users).DeviceActionClear(devices);
        }

        /// <inheritdoc/>
        public async Task DeviceDelete(IReadOnlyList<string> users, IEnumerable<GXDevice> devices)
        {
            _workflowHandler.Execute(typeof(DeviceTrigger), DeviceTrigger.Removed, devices);
            OnDeviceDelete?.Invoke(devices);
            await _hubContext.Clients.Users(users).DeviceDelete(devices);
        }

        /// <inheritdoc/>
        public async Task DeviceGroupDelete(IReadOnlyList<string> users, IEnumerable<GXDeviceGroup> groups)
        {
            OnDeviceGroupDelete?.Invoke(groups);
            await _hubContext.Clients.Users(users).DeviceGroupDelete(groups);
        }

        /// <inheritdoc/>
        public async Task DeviceGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXDeviceGroup> groups)
        {
            OnDeviceGroupUpdate?.Invoke(groups);
            await _hubContext.Clients.Users(users).DeviceGroupUpdate(groups);
        }

        /// <inheritdoc/>
        public async Task DeviceTemplateDelete(IReadOnlyList<string> users, IEnumerable<GXDeviceTemplate> templates)
        {
            OnDeviceTemplateDelete?.Invoke(templates);
            await _hubContext.Clients.Users(users).DeviceTemplateDelete(templates);
        }

        /// <inheritdoc/>
        public async Task DeviceTemplateGroupDelete(IReadOnlyList<string> users, IEnumerable<GXDeviceTemplateGroup> groups)
        {
            OnDeviceTemplateGroupDelete?.Invoke(groups);
            await _hubContext.Clients.Users(users).DeviceTemplateGroupDelete(groups);
        }

        /// <inheritdoc/>
        public async Task DeviceTemplateGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXDeviceTemplateGroup> groups)
        {
            OnDeviceTemplateGroupUpdate?.Invoke(groups);
            await _hubContext.Clients.Users(users).DeviceTemplateGroupUpdate(groups);
        }

        /// <inheritdoc/>
        public async Task DeviceTemplateUpdate(IReadOnlyList<string> users, IEnumerable<GXDeviceTemplate> templates)
        {
            OnDeviceTemplateUpdate?.Invoke(templates);
            await _hubContext.Clients.Users(users).DeviceTemplateUpdate(templates);
        }

        /// <inheritdoc/>
        public async Task DeviceTraceAdd(IReadOnlyList<string> users, IEnumerable<GXDeviceTrace> deviceTraces)
        {
            OnDeviceTraceAdd?.Invoke(deviceTraces);
            await _hubContext.Clients.Users(users).DeviceTraceAdd(deviceTraces);
        }

        /// <inheritdoc/>
        public async Task DeviceTraceClear(IReadOnlyList<string> users, IEnumerable<GXDevice>? devices)
        {
            OnDeviceTraceClear?.Invoke(devices);
            await _hubContext.Clients.Users(users).DeviceTraceClear(devices);
        }

        /// <inheritdoc/>
        public async Task DeviceUpdate(IReadOnlyList<string> users, IEnumerable<GXDevice> devices)
        {
            List<GXDevice> added = new List<GXDevice>();
            List<GXDevice> updated = new List<GXDevice>();
            foreach (GXDevice device in devices)
            {
                if (device.Updated == null)
                {
                    //Device is added.
                    added.Add(device);
                }
                else
                {
                    //Device is updated.
                    updated.Add(device);
                }
            }
            if (added.Any())
            {
                _workflowHandler.Execute(typeof(DeviceTrigger), DeviceTrigger.Add, added);
            }
            if (updated.Any())
            {
                _workflowHandler.Execute(typeof(DeviceTrigger), DeviceTrigger.Updated, updated);
            }
            OnDeviceUpdate?.Invoke(devices);
            await _hubContext.Clients.Users(users).DeviceUpdate(devices);
        }

        /// <inheritdoc/>
        public async Task LanguageUpdate(IReadOnlyList<string> users, IEnumerable<GXLanguage> languages)
        {
            OnLanguageUpdate?.Invoke(languages);
            await _hubContext.Clients.Users(users).LanguageUpdate(languages);
        }

        /// <inheritdoc/>
        public async Task ModuleDelete(IReadOnlyList<string> users, IEnumerable<GXModule> modules)
        {
            OnModuleDelete?.Invoke(modules);
            await _hubContext.Clients.Users(users).ModuleDelete(modules);
        }

        /// <inheritdoc/>
        public async Task ModuleGroupDelete(IReadOnlyList<string> users, IEnumerable<GXModuleGroup> groups)
        {
            OnModuleGroupDelete?.Invoke(groups);
            await _hubContext.Clients.Users(users).ModuleGroupDelete(groups);
        }

        /// <inheritdoc/>
        public async Task ModuleGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXModuleGroup> groups)
        {
            OnModuleGroupUpdate?.Invoke(groups);
            await _hubContext.Clients.Users(users).ModuleGroupUpdate(groups);
        }

        /// <inheritdoc/>
        public async Task ModuleSettingsSave(IReadOnlyList<string> users, GXModule updated)
        {
            OnModuleSettingsSave?.Invoke(updated);
            await _hubContext.Clients.Users(users).ModuleSettingsSave(updated);
        }

        /// <inheritdoc/>
        public async Task ModuleUpdate(IReadOnlyList<string> users, IEnumerable<GXModule> modules)
        {
            OnModuleUpdate?.Invoke(modules);
            await _hubContext.Clients.Users(users).ModuleUpdate(modules);
        }

        /// <inheritdoc/>
        public async Task ObjectDelete(IReadOnlyList<string> users, IEnumerable<GXObject> objects)
        {
            OnObjectDelete?.Invoke(objects);
            await _hubContext.Clients.Users(users).ObjectDelete(objects);
        }

        /// <inheritdoc/>
        public async Task ObjectTemplateDelete(IReadOnlyList<string> users, IEnumerable<GXObjectTemplate> templates)
        {
            OnObjectTemplateDelete?.Invoke(templates);
            await _hubContext.Clients.Users(users).ObjectTemplateDelete(templates);
        }

        /// <inheritdoc/>
        public async Task ObjectTemplateUpdate(IReadOnlyList<string> users, IEnumerable<GXObjectTemplate> templates)
        {
            OnObjectTemplateUpdate?.Invoke(templates);
            await _hubContext.Clients.Users(users).ObjectTemplateUpdate(templates);
        }

        /// <inheritdoc/>
        public async Task AttributeTemplateDelete(IReadOnlyList<string> users, IEnumerable<GXAttributeTemplate> templates)
        {
            OnAttributeTemplateDelete?.Invoke(templates);
            await _hubContext.Clients.Users(users).AttributeTemplateDelete(templates);
        }

        /// <inheritdoc/>
        public async Task AttributeTemplateUpdate(IReadOnlyList<string> users, IEnumerable<GXAttributeTemplate> templates)
        {
            OnAttributeTemplateUpdate?.Invoke(templates);
            await _hubContext.Clients.Users(users).AttributeTemplateUpdate(templates);
        }


        /// <inheritdoc/>
        public async Task ObjectUpdate(IReadOnlyList<string> users, IEnumerable<GXObject> objects)
        {
            OnObjectUpdate?.Invoke(objects);
            await _hubContext.Clients.Users(users).ObjectUpdate(objects);
        }

        /// <inheritdoc/>
        public async Task RestStatisticAdd(IReadOnlyList<string> users, IEnumerable<GXRestStatistic> statistics)
        {
            OnRestStatisticAdd?.Invoke(statistics);
            await _hubContext.Clients.Users(users).RestStatisticAdd(statistics);
        }

        /// <inheritdoc/>
        public async Task RestStatisticClear(IReadOnlyList<string> users, IEnumerable<GXUser>? Users)
        {
            OnRestStatisticClear?.Invoke(Users);
            await _hubContext.Clients.Users(users).RestStatisticClear(Users);
        }

        /// <inheritdoc/>
        public async Task RoleDelete(IReadOnlyList<string> users, IEnumerable<string> roles)
        {
            OnRoleDelete?.Invoke(roles);
            await _hubContext.Clients.Users(users).RoleDelete(roles);
        }

        /// <inheritdoc/>
        public async Task RoleUpdate(IReadOnlyList<string> users, IEnumerable<GXRole> roles)
        {
            OnRoleUpdate?.Invoke(roles);
            await _hubContext.Clients.Users(users).RoleUpdate(roles);
        }

        /// <inheritdoc/>
        public async Task ScheduleDelete(IReadOnlyList<string> users, IEnumerable<GXSchedule> schedules)
        {
            OnScheduleDelete?.Invoke(schedules);
            await _hubContext.Clients.Users(users).ScheduleDelete(schedules);
        }

        /// <inheritdoc/>
        public async Task ScheduleStart(IReadOnlyList<string> users, IEnumerable<GXSchedule> schedules)
        {
            _workflowHandler.Execute(typeof(ScheduleTrigger), ScheduleTrigger.Start, schedules);
            OnScheduleStart?.Invoke(schedules);
            await _hubContext.Clients.Users(users).ScheduleStart(schedules);
        }

        /// <inheritdoc/>
        public async Task ScheduleCompleate(IReadOnlyList<string> users, IEnumerable<GXSchedule> schedules)
        {
            _workflowHandler.Execute(typeof(ScheduleTrigger), ScheduleTrigger.Completed, schedules);
            OnScheduleCompleate?.Invoke(schedules);
            await _hubContext.Clients.Users(users).ScheduleCompleate(schedules);
        }

        /// <inheritdoc/>
        public async Task ScheduleGroupDelete(IReadOnlyList<string> users, IEnumerable<GXScheduleGroup> groups)
        {
            OnScheduleGroupDelete?.Invoke(groups);
            await _hubContext.Clients.Users(users).ScheduleGroupDelete(groups);
        }

        /// <inheritdoc/>
        public async Task ScheduleGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXScheduleGroup> groups)
        {
            OnScheduleGroupUpdate?.Invoke(groups);
            await _hubContext.Clients.Users(users).ScheduleGroupUpdate(groups);
        }

        /// <inheritdoc/>
        public async Task ScheduleUpdate(IReadOnlyList<string> users, IEnumerable<GXSchedule> schedules)
        {
            OnScheduleUpdate?.Invoke(schedules);
            await _hubContext.Clients.Users(users).ScheduleUpdate(schedules);
        }

        /// <inheritdoc/>
        public async Task ScriptDelete(IReadOnlyList<string> users, IEnumerable<GXScript> scripts)
        {
            OnScriptDelete?.Invoke(scripts);
            await _hubContext.Clients.Users(users).ScriptDelete(scripts);
        }

        /// <inheritdoc/>
        public async Task ScriptGroupDelete(IReadOnlyList<string> users, IEnumerable<GXScriptGroup> groups)
        {
            OnScriptGroupDelete?.Invoke(groups);
            await _hubContext.Clients.Users(users).ScriptGroupDelete(groups);
        }

        /// <inheritdoc/>
        public async Task ScriptGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXScriptGroup> groups)
        {
            OnScriptGroupUpdate?.Invoke(groups);
            await _hubContext.Clients.Users(users).ScriptGroupUpdate(groups);
        }

        /// <inheritdoc/>
        public async Task ScriptUpdate(IReadOnlyList<string> users, IEnumerable<GXScript> scripts)
        {
            OnScriptUpdate?.Invoke(scripts);
            await _hubContext.Clients.Users(users).ScriptUpdate(scripts);
        }

        /// <inheritdoc/>
        public async Task TaskAdd(IReadOnlyList<string> users, IEnumerable<GXTask> tasks)
        {
            OnTaskAdd?.Invoke(tasks);
            await _hubContext.Clients.Users(users).TaskAdd(tasks);
        }

        /// <inheritdoc/>
        public async Task TaskDelete(IReadOnlyList<string> users, IEnumerable<GXTask> tasks)
        {
            OnTaskDelete?.Invoke(tasks);
            await _hubContext.Clients.Users(users).TaskDelete(tasks);
        }

        /// <inheritdoc/>
        public async Task TaskUpdate(IReadOnlyList<string> users, IEnumerable<GXTask> tasks)
        {
            OnTaskUpdate?.Invoke(tasks);
            await _hubContext.Clients.Users(users).TaskUpdate(tasks);
        }

        /// <inheritdoc/>
        public async Task TaskClear(IReadOnlyList<string> users, IEnumerable<GXUser>? items)
        {
            OnTaskClear?.Invoke(items);
            await _hubContext.Clients.Users(users).TaskClear(items);
        }

        /// <inheritdoc/>
        public async Task TriggerDelete(IReadOnlyList<string> users, IEnumerable<GXTrigger> triggers)
        {
            OnTriggerDelete?.Invoke(triggers);
            await _hubContext.Clients.Users(users).TriggerDelete(triggers);
        }

        /// <inheritdoc/>
        public async Task TriggerGroupDelete(IReadOnlyList<string> users, IEnumerable<GXTriggerGroup> groups)
        {
            OnTriggerGroupDelete?.Invoke(groups);
            await _hubContext.Clients.Users(users).TriggerGroupDelete(groups);
        }

        /// <inheritdoc/>
        public async Task TriggerGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXTriggerGroup> groups)
        {
            OnTriggerGroupUpdate?.Invoke(groups);
            await _hubContext.Clients.Users(users).TriggerGroupUpdate(groups);
        }

        /// <inheritdoc/>
        public async Task TriggerUpdate(IReadOnlyList<string> users, IEnumerable<GXTrigger> triggers)
        {
            OnTriggerUpdate?.Invoke(triggers);
            await _hubContext.Clients.Users(users).TriggerUpdate(triggers);
        }

        /// <inheritdoc/>
        public async Task UserActionAdd(IReadOnlyList<string> users, IEnumerable<GXUserAction> userActions)
        {
            OnUserActionAdd?.Invoke(userActions);
            await _hubContext.Clients.Users(users).UserActionAdd(userActions);
        }

        /// <inheritdoc/>
        public async Task UserActionDelete(IReadOnlyList<string> users, IEnumerable<GXUserAction> userActions)
        {
            OnUserActionDelete?.Invoke(userActions);
            await _hubContext.Clients.Users(users).UserActionDelete(userActions);
        }

        /// <inheritdoc/>
        public async Task UserActionsClear(IReadOnlyList<string> users, IEnumerable<GXUser>? Users)
        {
            OnUserActionsClear?.Invoke(Users);
            await _hubContext.Clients.Users(users).UserActionClear(Users);
        }

        /// <inheritdoc/>
        public async Task UserDelete(IReadOnlyList<string> users, IEnumerable<GXUser> Users)
        {
            OnUserDelete?.Invoke(Users);
            await _hubContext.Clients.Users(users).UserDelete(Users);
        }

        /// <inheritdoc/>
        public async Task UserGroupDelete(IReadOnlyList<string> users, IEnumerable<GXUserGroup> groups)
        {
            OnUserGroupDelete?.Invoke(groups);
            await _hubContext.Clients.Users(users).UserGroupDelete(groups);
        }

        /// <inheritdoc/>
        public async Task UserGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXUserGroup> groups)
        {
            OnUserGroupUpdate?.Invoke(groups);
            await _hubContext.Clients.Users(users).UserGroupUpdate(groups);
        }

        /// <inheritdoc/>
        public async Task UserUpdate(IReadOnlyList<string> users, IEnumerable<GXUser> Users)
        {
            OnUserUpdate?.Invoke(Users);
            await _hubContext.Clients.Users(users).UserUpdate(Users);
        }

        /// <inheritdoc/>
        public async Task WorkflowDelete(IReadOnlyList<string> users, IEnumerable<GXWorkflow> workflows)
        {
            OnWorkflowDelete?.Invoke(workflows);
            await _hubContext.Clients.Users(users).WorkflowDelete(workflows);
        }

        /// <inheritdoc/>
        public async Task WorkflowGroupDelete(IReadOnlyList<string> users, IEnumerable<GXWorkflowGroup> groups)
        {
            OnWorkflowGroupDelete?.Invoke(groups);
            await _hubContext.Clients.Users(users).WorkflowGroupDelete(groups);
        }

        /// <inheritdoc/>
        public async Task WorkflowGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXWorkflowGroup> groups)
        {
            OnWorkflowGroupUpdate?.Invoke(groups);
            await _hubContext.Clients.Users(users).WorkflowGroupUpdate(groups);
        }

        /// <inheritdoc/>
        public async Task WorkflowUpdate(IReadOnlyList<string> users, IEnumerable<GXWorkflow> workflows)
        {
            OnWorkflowUpdate?.Invoke(workflows);
            await _hubContext.Clients.Users(users).WorkflowUpdate(workflows);
        }

        /// <inheritdoc/>
        public async Task CronStart(IReadOnlyList<string> users)
        {
            _workflowHandler.Execute(typeof(CronTrigger), CronTrigger.Start, null);
            OnCronStart?.Invoke();
            await _hubContext.Clients.Users(users).CronStart();
        }

        /// <inheritdoc/>
        public async Task CronCompleate(IReadOnlyList<string> users)
        {
            _workflowHandler.Execute(typeof(CronTrigger), CronTrigger.Completed, null);
            OnCronCompleate?.Invoke();
            await _hubContext.Clients.Users(users).CronCompleate();
        }

        /// <inheritdoc/>
        public async Task UserSettingUpdate(IReadOnlyList<string> users, IEnumerable<GXUserSetting> settings)
        {
            _workflowHandler.Execute(typeof(UserTrigger), UserTrigger.Modify, null);
            List<GXUser> list = new List<GXUser>();
            foreach (var it in settings)
            {
                if (it.User != null)
                {
                    list.Add(it.User);
                }
            }
            OnUserUpdate?.Invoke(list);
            await _hubContext.Clients.Users(users).UserSettingUpdate(settings);
        }

        /// <inheritdoc/>
        public async Task UserSettingDelete(IReadOnlyList<string> users, IEnumerable<GXUserSetting> settings)
        {
            _workflowHandler.Execute(typeof(UserTrigger), UserTrigger.Modify, null);
            List<GXUser> list = new List<GXUser>();
            foreach (var it in settings)
            {
                if (it.User != null)
                {
                    list.Add(it.User);
                }
            }
            OnUserUpdate?.Invoke(list);
            await _hubContext.Clients.Users(users).UserSettingDelete(settings);
        }
    }
}
