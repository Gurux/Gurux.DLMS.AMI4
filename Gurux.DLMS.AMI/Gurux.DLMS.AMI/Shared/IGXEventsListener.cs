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
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;

namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// Event listener is used to listen Gurux.DLMS.AMI server events.
    /// </summary>
    public interface IGXEventsListener
    {
        /// <summary>
        /// Configuration is saved.
        /// </summary>
        event Action<IEnumerable<GXConfiguration>> OnConfigurationSave;

        /// <summary>
        /// Module settings are saved.
        /// </summary>
        event Action<GXModule> OnModuleSettingsSave;

        /// <summary>
        /// System errors are cleared.
        /// </summary>      
        event Action OnClearSystemLogs;

        /// <summary>
        /// New system error is added.
        /// </summary>
        event Action<IEnumerable<GXSystemLog>> OnAddSystemLogs;

        /// <summary>
        /// System errors are closed.
        /// </summary>
        event Action<IEnumerable<GXSystemLog>> OnCloseSystemLogs;

        /// <summary>
        /// Device errors are cleared.
        /// </summary>
        event Action<IEnumerable<GXDevice>?> OnClearDeviceErrors;

        /// <summary>
        /// New device error is added.
        /// </summary>
        event Action<IEnumerable<GXDeviceError>> OnAddDeviceErrors;

        /// <summary>
        /// Device errors are closed.
        /// </summary>
        event Action<IEnumerable<GXDeviceError>> OnCloseDeviceErrors;

        /// <summary>
        /// Workflow logs are cleared.
        /// </summary>
        event Action<IEnumerable<GXWorkflow>?> OnClearWorkflowLogs;

        /// <summary>
        /// New workflow log is added.
        /// </summary>
        event Action<IEnumerable<GXWorkflowLog>> OnAddWorkflowLogs;

        /// <summary>
        /// Workflow log are closed.
        /// </summary>
        event Action<IEnumerable<GXWorkflowLog>> OnCloseWorkflowLogs;

        /// <summary>
        /// Schedule logs are cleared.
        /// </summary>
        event Action<IEnumerable<GXSchedule>?> OnClearScheduleLog;

        /// <summary>
        /// New schedule logs are added.
        /// </summary>
        event Action<IEnumerable<GXScheduleLog>> OnAddScheduleLog;

        /// <summary>
        /// Schedule logs are closed.
        /// </summary>
        event Action<IEnumerable<GXScheduleLog>> OnCloseScheduleLog;

        /// <summary>
        /// Script logs are cleared.
        /// </summary>
        event Action<IEnumerable<GXScript>?> OnClearScriptLogs;

        /// <summary>
        /// New Script log is added.
        /// </summary>
        event Action<IEnumerable<GXScriptLog>> OnAddScriptLogs;

        /// <summary>
        /// Script logs are closed.
        /// </summary>
        event Action<IEnumerable<GXScriptLog>> OnCloseScriptLogs;

        /// <summary>
        /// User errors are cleared.
        /// </summary>
        event Action<IEnumerable<GXUser>?> OnClearUserErrors;

        /// <summary>
        /// New User error is added.
        /// </summary>
        event Action<IEnumerable<GXUserError>> OnAddUserErrors;

        /// <summary>
        /// User errors are closed.
        /// </summary>
        event Action<IEnumerable<GXUserError>> OnCloseUserErrors;

        /// <summary>
        /// Module logs are cleared.
        /// </summary>
        event Action<IEnumerable<GXModule>?> OnClearModuleLogs;

        /// <summary>
        /// New module log is added.
        /// </summary>
        event Action<IEnumerable<GXModuleLog>> OnAddModuleLogs;

        /// <summary>
        /// Module logs are closed.
        /// </summary>
        event Action<IEnumerable<GXModuleLog>> OnCloseModuleLogs;


        /// <summary>
        /// Agent errors are cleared.
        /// </summary>
        event Action<IEnumerable<GXAgent>?> OnClearAgentErrors;

        /// <summary>
        /// New agent error is added.
        /// </summary>
        event Action<IEnumerable<GXAgentLog>> OnAddAgentErrors;

        /// <summary>
        /// Agent errors are closed.
        /// </summary>
        event Action<IEnumerable<GXAgentLog>> OnCloseAgentErrors;


        /// <summary>
        /// New Schedule is added.
        /// </summary>
        event Action<IEnumerable<GXSchedule>> OnScheduleUpdate;

        /// <summary>
        /// Schedule is deleted.
        /// </summary>
        event Action<IEnumerable<GXSchedule>> OnScheduleDelete;

        /// <summary>
        /// Schedule is started.
        /// </summary>
        event Action<IEnumerable<GXSchedule>> OnScheduleStart;

        /// <summary>
        /// Schedule is compleated.
        /// </summary>
        event Action<IEnumerable<GXSchedule>> OnScheduleCompleate;

        /// <summary>
        /// New Schedule group is added.
        /// </summary>
        event Action<IEnumerable<GXScheduleGroup>> OnScheduleGroupUpdate;

        /// <summary>
        /// Schedule group is deleted.
        /// </summary>
        event Action<IEnumerable<GXScheduleGroup>> OnScheduleGroupDelete;

        /// <summary>
        /// New user is added or modified.
        /// </summary>
        event Action<IEnumerable<GXUser>> OnUserUpdate;

        /// <summary>
        /// User is deleted.
        /// </summary>
        event Action<IEnumerable<GXUser>> OnUserDelete;

        /// <summary>
        /// New user group is added or modified.
        /// </summary>
        event Action<IEnumerable<GXUserGroup>> OnUserGroupUpdate;

        /// <summary>
        /// User group is deleted.
        /// </summary>
        event Action<IEnumerable<GXUserGroup>> OnUserGroupDelete;

        /// <summary>
        /// New device is added or modified.
        /// </summary>
        event Action<IEnumerable<GXDevice>> OnDeviceUpdate;

        /// <summary>
        /// Device is deleted.
        /// </summary>
        event Action<IEnumerable<GXDevice>> OnDeviceDelete;

        /// <summary>
        /// New device group is added or modified.
        /// </summary>
        event Action<IEnumerable<GXDeviceGroup>> OnDeviceGroupUpdate;

        /// <summary>
        /// Device group is deleted.
        /// </summary>
        event Action<IEnumerable<GXDeviceGroup>> OnDeviceGroupDelete;

        /// <summary>
        /// New device template group is added or modified.
        /// </summary>
        event Action<IEnumerable<GXDeviceTemplateGroup>> OnDeviceTemplateGroupUpdate;

        /// <summary>
        /// Device template group is deleted.
        /// </summary>
        event Action<IEnumerable<GXDeviceTemplateGroup>> OnDeviceTemplateGroupDelete;

        /// <summary>
        /// New device template is added or modified.
        /// </summary>
        event Action<IEnumerable<GXDeviceTemplate>> OnDeviceTemplateUpdate;

        /// <summary>
        /// Device template is deleted.
        /// </summary>
        event Action<IEnumerable<GXDeviceTemplate>> OnDeviceTemplateDelete;

        /// <summary>
        /// New object template is added or modified.
        /// </summary>
        event Action<IEnumerable<GXObjectTemplate>> OnObjectTemplateUpdate;

        /// <summary>
        /// Object template is deleted.
        /// </summary>
        event Action<IEnumerable<GXObjectTemplate>> OnObjectTemplateDelete;

        /// <summary>
        /// New agent is added or modified.
        /// </summary>
        event Action<IEnumerable<GXAgent>> OnAgentUpdate;

        /// <summary>
        /// Agent is deleted.
        /// </summary>
        event Action<IEnumerable<GXAgent>> OnAgentDelete;

        /// <summary>
        /// Agent status has changed.
        /// </summary>
        event Action<IEnumerable<GXAgent>> OnAgentStatusChange;

        /// <summary>
        /// New agent group is added or modified.
        /// </summary>
        event Action<IEnumerable<GXAgentGroup>> OnAgentGroupUpdate;

        /// <summary>
        /// Agent group is deleted.
        /// </summary>
        event Action<IEnumerable<GXAgentGroup>> OnAgentGroupDelete;

        /// <summary>
        /// New workflow is added or modified.
        /// </summary>
        event Action<IEnumerable<GXWorkflow>> OnWorkflowUpdate;

        /// <summary>
        /// Workflow is deleted.
        /// </summary>
        event Action<IEnumerable<GXWorkflow>> OnWorkflowDelete;

        /// <summary>
        /// New workflow group is added or modified.
        /// </summary>
        event Action<IEnumerable<GXWorkflowGroup>> OnWorkflowGroupUpdate;

        /// <summary>
        /// Workflow group is deleted.
        /// </summary>
        event Action<IEnumerable<GXWorkflowGroup>> OnWorkflowGroupDelete;

        /// <summary>
        /// New trigger is added or modified.
        /// </summary>
        event Action<IEnumerable<GXTrigger>> OnTriggerUpdate;

        /// <summary>
        /// Trigger is deleted.
        /// </summary>
        event Action<IEnumerable<GXTrigger>> OnTriggerDelete;

        /// <summary>
        /// New trigger group is added or modified.
        /// </summary>
        event Action<IEnumerable<GXTriggerGroup>> OnTriggerGroupUpdate;

        /// <summary>
        /// Trigger group is deleted.
        /// </summary>
        event Action<IEnumerable<GXTriggerGroup>> OnTriggerGroupDelete;

        /// <summary>
        /// New module is added or modified.
        /// </summary>
        event Action<IEnumerable<GXModule>> OnModuleUpdate;

        /// <summary>
        /// Module is deleted.
        /// </summary>
        event Action<IEnumerable<GXModule>> OnModuleDelete;

        /// <summary>
        /// New module group is added or modified.
        /// </summary>
        event Action<IEnumerable<GXModuleGroup>> OnModuleGroupUpdate;

        /// <summary>
        /// Module group is deleted.
        /// </summary>
        event Action<IEnumerable<GXModuleGroup>> OnModuleGroupDelete;


        /// <summary>
        /// New object is added or modified.
        /// </summary>
        event Action<IEnumerable<GXObject>> OnObjectUpdate;

        /// <summary>
        /// Object is deleted.
        /// </summary>
        event Action<IEnumerable<GXObject>> OnObjectDelete;

        /// <summary>
        /// New attribute is added or modified.
        /// </summary>
        event Action<IEnumerable<GXAttribute>> OnAttributeUpdate;

        /// <summary>
        /// Attribute is deleted.
        /// </summary>
        event Action<IEnumerable<GXAttribute>> OnAttributeDelete;

        /// <summary>
        /// Attribute value is updated.
        /// </summary>
        event Action<IEnumerable<GXAttribute>> OnValueUpdate;

        /// <summary>
        /// New task is added.
        /// </summary>
        event Action<IEnumerable<GXTask>> OnTaskAdd;

        /// <summary>
        /// Task(s) are updated.
        /// </summary>
        event Action<IEnumerable<GXTask>> OnTaskUpdate;

        /// <summary>
        /// Task(s) are deleted.
        /// </summary>
        event Action<IEnumerable<GXTask>> OnTaskDelete;

        /// <summary>
        /// Task(s) are cleared.
        /// </summary>
        event Action<IEnumerable<GXUser>?> OnTaskClear;

        /// <summary>
        /// Blocks are updated or modified.
        /// </summary>
        event Action<IEnumerable<GXBlock>> OnBlockUpdate;

        /// <summary>
        /// Block is deleted.
        /// </summary>
        event Action<IEnumerable<GXBlock>> OnBlockDelete;

        /// <summary>
        /// Blocks are closed.
        /// </summary>
        event Action<IEnumerable<GXBlock>> OnBlockClose;

        /// <summary>
        /// New block group is added or modified.
        /// </summary>
        event Action<IEnumerable<GXBlockGroup>> OnBlockGroupUpdate;

        /// <summary>
        /// Block group is deleted.
        /// </summary>
        event Action<IEnumerable<GXBlockGroup>> OnBlockGroupDelete;

        /// <summary>
        /// New user action is added.
        /// </summary>
        event Action<IEnumerable<GXUserAction>> OnUserActionAdd;

        /// <summary>
        /// User action is deleted.
        /// </summary>
        event Action<IEnumerable<GXUserAction>> OnUserActionDelete;

        /// <summary>
        /// User actions are clear.
        /// </summary>
        event Action<IEnumerable<GXUser>?> OnUserActionsClear;

        /// <summary>
        /// New component view is added or modified.
        /// </summary>
        event Action<IEnumerable<GXComponentView>> OnComponentViewUpdate;

        /// <summary>
        /// Component view is deleted.
        /// </summary>
        event Action<IEnumerable<GXComponentView>> OnComponentViewDelete;

        /// <summary>
        /// New component view group is added or modified.
        /// </summary>
        event Action<IEnumerable<GXComponentViewGroup>> OnComponentViewGroupUpdate;

        /// <summary>
        /// Component view group is deleted.
        /// </summary>
        event Action<IEnumerable<GXComponentViewGroup>> OnComponentViewGroupDelete;

        /// <summary>
        /// New script is added or modified.
        /// </summary>
        event Action<IEnumerable<GXScript>> OnScriptUpdate;

        /// <summary>
        /// Component view is deleted.
        /// </summary>
        event Action<IEnumerable<GXScript>> OnScriptDelete;

        /// <summary>
        /// New script group is added or modified.
        /// </summary>
        event Action<IEnumerable<GXScriptGroup>> OnScriptGroupUpdate;

        /// <summary>
        /// Component view group is deleted.
        /// </summary>
        event Action<IEnumerable<GXScriptGroup>> OnScriptGroupDelete;

        /// <summary>
        /// New device trace is added.
        /// </summary>
        event Action<IEnumerable<GXDeviceTrace>> OnDeviceTraceAdd;

        /// <summary>
        /// Device traces are clear.
        /// </summary>
        event Action<IEnumerable<GXDevice>> OnDeviceTraceClear;

        /// <summary>
        /// New device action is added.
        /// </summary>
        event Action<IEnumerable<GXDeviceAction>> OnDeviceActionAdd;

        /// <summary>
        /// Device actions are clear.
        /// </summary>
        event Action<IEnumerable<GXDevice>?> OnDeviceActionsClear;

        /// <summary>
        /// New REST statistic is added or modified.
        /// </summary>
        event Action<IEnumerable<GXRestStatistic>> OnRestStatisticAdd;

        /// <summary>
        /// REST statistics are clear for the users.
        /// </summary>
        event Action<IEnumerable<GXUser>?> OnRestStatisticClear;

        /// <summary>
        /// New language is added or modified.
        /// </summary>
        event Action<IEnumerable<GXLanguage>> OnLanguageUpdate;

        /// <summary>
        /// New role is added or modified.
        /// </summary>
        event Action<IEnumerable<GXRole>> OnRoleUpdate;

        /// <summary>
        /// Role is deleted.
        /// </summary>
        event Action<IEnumerable<string>> OnRoleDelete;

        /// <summary>
        /// Cron is started.
        /// </summary>
        event Action OnCronStart;

        /// <summary>
        /// Cron is compleated.
        /// </summary>
        event Action OnCronCompleate;

        /// <summary>
        /// New manufacturer is added or modified.
        /// </summary>
        event Action<IEnumerable<GXManufacturer>>? OnManufacturerUpdate;
        /// <summary>
        /// Manufacturer is deleted.
        /// </summary>
        event Action<IEnumerable<GXManufacturer>>? OnManufacturerDelete;
        /// <summary>
        /// New manufacturer group is added or modified.
        /// </summary>
        event Action<IEnumerable<GXManufacturerGroup>>? OnManufacturerGroupUpdate;
        /// <summary>
        /// Manufacturer group is deleted.
        /// </summary>
        event Action<IEnumerable<GXManufacturerGroup>>? OnManufacturerGroupDelete;

    }
}
