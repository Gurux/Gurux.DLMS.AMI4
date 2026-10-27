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

using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;

namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// Defines callbacks for Gurux.DLMS.AMI domain events.
    /// </summary>
    [System.ComponentModel.Description("Defines callbacks for Gurux.DLMS.AMI domain events.")]
    public interface IGXEventsListener
    {
        /// <summary>Occurs when module targets are updated.</summary>
        [System.ComponentModel.Description("Occurs when module targets are updated.")]
        event Action<IEnumerable<GXTarget>> OnModuleTargetUpdate;
        /// <summary>
        /// New item is added or updated.
        /// </summary>
        [System.ComponentModel.Description("New item is added or updated.")]
        event Action<IEnumerable<GXTarget>> OnUpdate;

        /// <summary>
        /// New item is deleted.
        /// </summary>
        [System.ComponentModel.Description("New item is deleted.")]
        event Action<IEnumerable<GXTarget>> OnDelete;

        /// <summary>
        /// Items are cleared.
        /// </summary>      
        [System.ComponentModel.Description("Items are cleared.")]
        event Action<IEnumerable<GXTarget>> OnClear;

        /// <summary>
        /// Configuration is saved.
        /// </summary>
        [System.ComponentModel.Description("Configuration is saved.")]
        event Action<IEnumerable<GXConfiguration>> OnConfigurationSave;

        /// <summary>
        /// Module settings are saved.
        /// </summary>
        [System.ComponentModel.Description("Module settings are saved.")]
        event Action<GXModule> OnModuleSettingsSave;

        /// <summary>
        /// System errors are cleared.
        /// </summary>      
        [System.ComponentModel.Description("System errors are cleared.")]
        event Action OnClearSystemLogs;

        /// <summary>
        /// New system error is added.
        /// </summary>
        [System.ComponentModel.Description("New system error is added.")]
        event Action<IEnumerable<GXLog>> OnAddSystemLogs;

        /// <summary>
        /// System errors are closed.
        /// </summary>
        [System.ComponentModel.Description("System errors are closed.")]
        event Action<IEnumerable<GXLog>> OnCloseSystemLogs;

        /// <summary>
        /// Device errors are cleared.
        /// </summary>
        [System.ComponentModel.Description("Device errors are cleared.")]
        event Action<IEnumerable<GXDevice>?> OnClearDeviceErrors;

        /// <summary>
        /// New device error is added.
        /// </summary>
        [System.ComponentModel.Description("New device error is added.")]
        event Action<IEnumerable<GXLog>> OnAddDeviceErrors;

        /// <summary>
        /// Device errors are closed.
        /// </summary>
        [System.ComponentModel.Description("Device errors are closed.")]
        event Action<IEnumerable<GXLog>> OnCloseDeviceErrors;

        /// <summary>
        /// Workflow logs are cleared.
        /// </summary>
        [System.ComponentModel.Description("Workflow logs are cleared.")]
        event Action<IEnumerable<GXWorkflow>?> OnClearWorkflowLogs;

        /// <summary>
        /// New workflow log is added.
        /// </summary>
        [System.ComponentModel.Description("New workflow log is added.")]
        event Action<IEnumerable<GXLog>> OnAddWorkflowLogs;

        /// <summary>
        /// Workflow logs are closed.
        /// </summary>
        [System.ComponentModel.Description("Workflow logs are closed.")]
        event Action<IEnumerable<GXLog>> OnCloseWorkflowLogs;

        /// <summary>
        /// Schedule logs are cleared.
        /// </summary>
        [System.ComponentModel.Description("Schedule logs are cleared.")]
        event Action<IEnumerable<GXSchedule>?> OnClearScheduleLog;

        /// <summary>
        /// New schedule logs are added.
        /// </summary>
        [System.ComponentModel.Description("New schedule logs are added.")]
        event Action<IEnumerable<GXLog>> OnAddScheduleLog;

        /// <summary>
        /// Schedule logs are closed.
        /// </summary>
        [System.ComponentModel.Description("Schedule logs are closed.")]
        event Action<IEnumerable<GXLog>> OnCloseScheduleLog;

        /// <summary>
        /// Script logs are cleared.
        /// </summary>
        [System.ComponentModel.Description("Script logs are cleared.")]
        event Action<IEnumerable<GXScript>?> OnClearScriptLogs;

        /// <summary>
        /// New script log is added.
        /// </summary>
        [System.ComponentModel.Description("New script log is added.")]
        event Action<IEnumerable<GXLog>> OnAddScriptLogs;

        /// <summary>
        /// Script logs are closed.
        /// </summary>
        [System.ComponentModel.Description("Script logs are closed.")]
        event Action<IEnumerable<GXLog>> OnCloseScriptLogs;

        /// <summary>
        /// User errors are cleared.
        /// </summary>
        [System.ComponentModel.Description("User errors are cleared.")]
        event Action<IEnumerable<GXUser>?> OnClearUserErrors;

        /// <summary>
        /// New user error is added.
        /// </summary>
        [System.ComponentModel.Description("New user error is added.")]
        event Action<IEnumerable<GXLog>> OnAddUserErrors;

        /// <summary>
        /// User errors are closed.
        /// </summary>
        [System.ComponentModel.Description("User errors are closed.")]
        event Action<IEnumerable<GXLog>> OnCloseUserErrors;

        /// <summary>
        /// Module logs are cleared.
        /// </summary>
        [System.ComponentModel.Description("Module logs are cleared.")]
        event Action<IEnumerable<GXModule>?> OnClearModuleLogs;

        /// <summary>
        /// New module log is added.
        /// </summary>
        [System.ComponentModel.Description("New module log is added.")]
        event Action<IEnumerable<GXLog>> OnAddModuleLogs;

        /// <summary>
        /// Module logs are closed.
        /// </summary>
        [System.ComponentModel.Description("Module logs are closed.")]
        event Action<IEnumerable<GXLog>> OnCloseModuleLogs;


        /// <summary>
        /// Agent errors are cleared.
        /// </summary>
        [System.ComponentModel.Description("Agent errors are cleared.")]
        event Action<IEnumerable<GXAgent>?> OnClearAgentErrors;

        /// <summary>
        /// New agent error is added.
        /// </summary>
        [System.ComponentModel.Description("New agent error is added.")]
        event Action<IEnumerable<GXLog>> OnAddAgentErrors;

        /// <summary>
        /// Agent errors are closed.
        /// </summary>
        [System.ComponentModel.Description("Agent errors are closed.")]
        event Action<IEnumerable<GXLog>> OnCloseAgentErrors;


        /// <summary>
        /// New schedule is added.
        /// </summary>
        [System.ComponentModel.Description("New schedule is added.")]
        event Action<IEnumerable<GXSchedule>> OnScheduleUpdate;

        /// <summary>
        /// Schedule is deleted.
        /// </summary>
        [System.ComponentModel.Description("Schedule is deleted.")]
        event Action<IEnumerable<GXSchedule>> OnScheduleDelete;

        /// <summary>
        /// Schedule is started.
        /// </summary>
        [System.ComponentModel.Description("Schedule is started.")]
        event Action<IEnumerable<GXSchedule>> OnScheduleStart;

        /// <summary>
        /// Schedule is completed.
        /// </summary>
        [System.ComponentModel.Description("Schedule is completed.")]
        event Action<IEnumerable<GXSchedule>> OnScheduleCompleate;

        /// <summary>
        /// New Schedule group is added.
        /// </summary>
        [System.ComponentModel.Description("New Schedule group is added.")]
        event Action<IEnumerable<GXScheduleGroup>> OnScheduleGroupUpdate;

        /// <summary>
        /// Schedule group is deleted.
        /// </summary>
        [System.ComponentModel.Description("Schedule group is deleted.")]
        event Action<IEnumerable<GXScheduleGroup>> OnScheduleGroupDelete;

        /// <summary>
        /// New user is added or modified.
        /// </summary>
        [System.ComponentModel.Description("New user is added or modified.")]
        event Action<IEnumerable<GXUser>> OnUserUpdate;

        /// <summary>
        /// User is deleted.
        /// </summary>
        [System.ComponentModel.Description("User is deleted.")]
        event Action<IEnumerable<GXUser>> OnUserDelete;

        /// <summary>
        /// New user group is added or modified.
        /// </summary>
        [System.ComponentModel.Description("New user group is added or modified.")]
        event Action<IEnumerable<GXUserGroup>> OnUserGroupUpdate;

        /// <summary>
        /// User group is deleted.
        /// </summary>
        [System.ComponentModel.Description("User group is deleted.")]
        event Action<IEnumerable<GXUserGroup>> OnUserGroupDelete;

        /// <summary>
        /// New device is added or modified.
        /// </summary>
        [System.ComponentModel.Description("New device is added or modified.")]
        event Action<IEnumerable<GXDevice>> OnDeviceUpdate;

        /// <summary>
        /// Device is deleted.
        /// </summary>
        [System.ComponentModel.Description("Device is deleted.")]
        event Action<IEnumerable<GXDevice>> OnDeviceDelete;

        /// <summary>
        /// New device group is added or modified.
        /// </summary>
        [System.ComponentModel.Description("New device group is added or modified.")]
        event Action<IEnumerable<GXDeviceGroup>> OnDeviceGroupUpdate;

        /// <summary>
        /// Device group is deleted.
        /// </summary>
        [System.ComponentModel.Description("Device group is deleted.")]
        event Action<IEnumerable<GXDeviceGroup>> OnDeviceGroupDelete;

        /// <summary>
        /// New device template group is added or modified.
        /// </summary>
        [System.ComponentModel.Description("New device template group is added or modified.")]
        event Action<IEnumerable<GXDeviceTemplateGroup>> OnDeviceTemplateGroupUpdate;

        /// <summary>
        /// Device template group is deleted.
        /// </summary>
        [System.ComponentModel.Description("Device template group is deleted.")]
        event Action<IEnumerable<GXDeviceTemplateGroup>> OnDeviceTemplateGroupDelete;

        /// <summary>
        /// New device template is added or modified.
        /// </summary>
        [System.ComponentModel.Description("New device template is added or modified.")]
        event Action<IEnumerable<GXDeviceTemplate>> OnDeviceTemplateUpdate;

        /// <summary>
        /// Device template is deleted.
        /// </summary>
        [System.ComponentModel.Description("Device template is deleted.")]
        event Action<IEnumerable<GXDeviceTemplate>> OnDeviceTemplateDelete;

        /// <summary>
        /// New object template is added or modified.
        /// </summary>
        [System.ComponentModel.Description("New object template is added or modified.")]
        event Action<IEnumerable<GXObjectTemplate>> OnObjectTemplateUpdate;

        /// <summary>
        /// Object template is deleted.
        /// </summary>
        [System.ComponentModel.Description("Object template is deleted.")]
        event Action<IEnumerable<GXObjectTemplate>> OnObjectTemplateDelete;

        /// <summary>
        /// New agent is added or modified.
        /// </summary>
        [System.ComponentModel.Description("New agent is added or modified.")]
        event Action<IEnumerable<GXAgent>> OnAgentUpdate;

        /// <summary>
        /// Agent is deleted.
        /// </summary>
        [System.ComponentModel.Description("Agent is deleted.")]
        event Action<IEnumerable<GXAgent>> OnAgentDelete;

        /// <summary>
        /// Agent status has changed.
        /// </summary>
        [System.ComponentModel.Description("Agent status has changed.")]
        event Action<IEnumerable<GXAgent>> OnAgentStatusChange;

        /// <summary>
        /// New agent group is added or modified.
        /// </summary>
        [System.ComponentModel.Description("New agent group is added or modified.")]
        event Action<IEnumerable<GXAgentGroup>> OnAgentGroupUpdate;

        /// <summary>
        /// Agent group is deleted.
        /// </summary>
        [System.ComponentModel.Description("Agent group is deleted.")]
        event Action<IEnumerable<GXAgentGroup>> OnAgentGroupDelete;

        /// <summary>
        /// New workflow is added or modified.
        /// </summary>
        [System.ComponentModel.Description("New workflow is added or modified.")]
        event Action<IEnumerable<GXWorkflow>> OnWorkflowUpdate;

        /// <summary>
        /// Workflow is deleted.
        /// </summary>
        [System.ComponentModel.Description("Workflow is deleted.")]
        event Action<IEnumerable<GXWorkflow>> OnWorkflowDelete;

        /// <summary>
        /// New workflow group is added or modified.
        /// </summary>
        [System.ComponentModel.Description("New workflow group is added or modified.")]
        event Action<IEnumerable<GXWorkflowGroup>> OnWorkflowGroupUpdate;

        /// <summary>
        /// Workflow group is deleted.
        /// </summary>
        [System.ComponentModel.Description("Workflow group is deleted.")]
        event Action<IEnumerable<GXWorkflowGroup>> OnWorkflowGroupDelete;

        /// <summary>
        /// New trigger is added or modified.
        /// </summary>
        [System.ComponentModel.Description("New trigger is added or modified.")]
        event Action<IEnumerable<GXTrigger>> OnTriggerUpdate;

        /// <summary>
        /// Trigger is deleted.
        /// </summary>
        [System.ComponentModel.Description("Trigger is deleted.")]
        event Action<IEnumerable<GXTrigger>> OnTriggerDelete;

        /// <summary>
        /// New trigger group is added or modified.
        /// </summary>
        [System.ComponentModel.Description("New trigger group is added or modified.")]
        event Action<IEnumerable<GXTriggerGroup>> OnTriggerGroupUpdate;

        /// <summary>
        /// Trigger group is deleted.
        /// </summary>
        [System.ComponentModel.Description("Trigger group is deleted.")]
        event Action<IEnumerable<GXTriggerGroup>> OnTriggerGroupDelete;

        /// <summary>
        /// New module is added or modified.
        /// </summary>
        [System.ComponentModel.Description("New module is added or modified.")]
        event Action<IEnumerable<GXModule>> OnModuleUpdate;

        /// <summary>
        /// Module is deleted.
        /// </summary>
        [System.ComponentModel.Description("Module is deleted.")]
        event Action<IEnumerable<GXModule>> OnModuleDelete;

        /// <summary>
        /// New module group is added or modified.
        /// </summary>
        [System.ComponentModel.Description("New module group is added or modified.")]
        event Action<IEnumerable<GXModuleGroup>> OnModuleGroupUpdate;

        /// <summary>
        /// Module group is deleted.
        /// </summary>
        [System.ComponentModel.Description("Module group is deleted.")]
        event Action<IEnumerable<GXModuleGroup>> OnModuleGroupDelete;


        /// <summary>
        /// New object is added or modified.
        /// </summary>
        [System.ComponentModel.Description("New object is added or modified.")]
        event Action<IEnumerable<GXObject>> OnObjectUpdate;

        /// <summary>
        /// Object is deleted.
        /// </summary>
        [System.ComponentModel.Description("Object is deleted.")]
        event Action<IEnumerable<GXObject>> OnObjectDelete;

        /// <summary>
        /// New attribute is added or modified.
        /// </summary>
        [System.ComponentModel.Description("New attribute is added or modified.")]
        event Action<IEnumerable<GXAttribute>> OnAttributeUpdate;

        /// <summary>
        /// Attribute is deleted.
        /// </summary>
        [System.ComponentModel.Description("Attribute is deleted.")]
        event Action<IEnumerable<GXAttribute>> OnAttributeDelete;

        /// <summary>
        /// Attribute value is updated.
        /// </summary>
        [System.ComponentModel.Description("Attribute value is updated.")]
        event Action<IEnumerable<GXAttribute>> OnValueUpdate;

        /// <summary>
        /// New task is added.
        /// </summary>
        [System.ComponentModel.Description("New task is added.")]
        event Action<IEnumerable<GXTask>> OnTaskAdd;

        /// <summary>Tasks are updated.</summary>
        [System.ComponentModel.Description("Tasks are updated.")]
        event Action<IEnumerable<GXTask>> OnTaskUpdate;

        /// <summary>Tasks are deleted.</summary>
        [System.ComponentModel.Description("Tasks are deleted.")]
        event Action<IEnumerable<GXTask>> OnTaskDelete;

        /// <summary>
        /// Tasks are cleared.
        /// </summary>
        [System.ComponentModel.Description("Tasks are cleared.")]
        event Action<IEnumerable<GXUser>?> OnTaskClear;

        /// <summary>
        /// Contents are updated or modified.
        /// </summary>

        /// <summary>
        /// Content is deleted.
        /// </summary>

        /// <summary>
        /// Contents are closed.
        /// </summary>

        /// <summary>
        /// New content group is added or modified.
        /// </summary>

        /// <summary>
        /// Content group is deleted.
        /// </summary>

        /// <summary>
        /// New user action is added.
        /// </summary>
        [System.ComponentModel.Description("New user action is added.")]
        event Action<IEnumerable<GXLog>> OnUserActionAdd;

        /// <summary>
        /// User action is deleted.
        /// </summary>
        [System.ComponentModel.Description("User action is deleted.")]
        event Action<IEnumerable<GXLog>> OnUserActionDelete;

        /// <summary>
        /// User actions are cleared.
        /// </summary>
        [System.ComponentModel.Description("User actions are cleared.")]
        event Action<IEnumerable<GXUser>?> OnUserActionsClear;

        /// <summary>
        /// New component view is added or modified.
        /// </summary>
        [System.ComponentModel.Description("New component view is added or modified.")]
        event Action<IEnumerable<GXComponentView>> OnComponentViewUpdate;

        /// <summary>
        /// Component view is deleted.
        /// </summary>
        [System.ComponentModel.Description("Component view is deleted.")]
        event Action<IEnumerable<GXComponentView>> OnComponentViewDelete;

        /// <summary>
        /// New component view group is added or modified.
        /// </summary>
        [System.ComponentModel.Description("New component view group is added or modified.")]
        event Action<IEnumerable<GXComponentViewGroup>> OnComponentViewGroupUpdate;

        /// <summary>
        /// Component view group is deleted.
        /// </summary>
        [System.ComponentModel.Description("Component view group is deleted.")]
        event Action<IEnumerable<GXComponentViewGroup>> OnComponentViewGroupDelete;

        /// <summary>
        /// New script is added or modified.
        /// </summary>
        [System.ComponentModel.Description("New script is added or modified.")]
        event Action<IEnumerable<GXScript>> OnScriptUpdate;

        /// <summary>
        /// Script is deleted.
        /// </summary>
        [System.ComponentModel.Description("Script is deleted.")]
        event Action<IEnumerable<GXScript>> OnScriptDelete;

        /// <summary>
        /// New script group is added or modified.
        /// </summary>
        [System.ComponentModel.Description("New script group is added or modified.")]
        event Action<IEnumerable<GXScriptGroup>> OnScriptGroupUpdate;

        /// <summary>
        /// Script group is deleted.
        /// </summary>
        [System.ComponentModel.Description("Script group is deleted.")]
        event Action<IEnumerable<GXScriptGroup>> OnScriptGroupDelete;

        /// <summary>
        /// New device trace is added.
        /// </summary>
        [System.ComponentModel.Description("New device trace is added.")]
        event Action<IEnumerable<GXDeviceTrace>> OnDeviceTraceAdd;

        /// <summary>
        /// Device traces are cleared.
        /// </summary>
        [System.ComponentModel.Description("Device traces are cleared.")]
        event Action<IEnumerable<GXDevice>> OnDeviceTraceClear;

        /// <summary>
        /// New device action is added.
        /// </summary>
        [System.ComponentModel.Description("New device action is added.")]
        event Action<IEnumerable<GXLog>> OnDeviceActionAdd;

        /// <summary>
        /// Device actions are cleared.
        /// </summary>
        [System.ComponentModel.Description("Device actions are cleared.")]
        event Action<IEnumerable<GXDevice>?> OnDeviceActionsClear;

        /// <summary>
        /// New language is added or modified.
        /// </summary>
        [System.ComponentModel.Description("New language is added or modified.")]
        event Action<IEnumerable<GXLanguage>> OnLanguageUpdate;

        /// <summary>
        /// New role is added or modified.
        /// </summary>
        [System.ComponentModel.Description("New role is added or modified.")]
        event Action<IEnumerable<GXRole>> OnRoleUpdate;

        /// <summary>
        /// Role is deleted.
        /// </summary>
        [System.ComponentModel.Description("Role is deleted.")]
        event Action<IEnumerable<string>> OnRoleDelete;

        /// <summary>
        /// Cron is started.
        /// </summary>
        [System.ComponentModel.Description("Cron is started.")]
        event Action OnCronStart;

        /// <summary>
        /// Cron is completed.
        /// </summary>
        [System.ComponentModel.Description("Cron is completed.")]
        event Action OnCronCompleate;

        /// <summary>
        /// New manufacturer is added or modified.
        /// </summary>
        [System.ComponentModel.Description("New manufacturer is added or modified.")]
        event Action<IEnumerable<GXManufacturer>>? OnManufacturerUpdate;
        /// <summary>
        /// Manufacturer is deleted.
        /// </summary>
        [System.ComponentModel.Description("Manufacturer is deleted.")]
        event Action<IEnumerable<GXManufacturer>>? OnManufacturerDelete;
        /// <summary>
        /// New manufacturer group is added or modified.
        /// </summary>
        [System.ComponentModel.Description("New manufacturer group is added or modified.")]
        event Action<IEnumerable<GXManufacturerGroup>>? OnManufacturerGroupUpdate;
        /// <summary>
        /// Manufacturer group is deleted.
        /// </summary>
        [System.ComponentModel.Description("Manufacturer group is deleted.")]
        event Action<IEnumerable<GXManufacturerGroup>>? OnManufacturerGroupDelete;
        /// <summary>
        /// New localized resource is added or modified.
        /// </summary>
        [System.ComponentModel.Description("New localized resource is added or modified.")]
        event Action<IEnumerable<GXLocalizedResource>>? OnLocalizedResourceUpdate;
        /// <summary>
        /// Localized resource is deleted.
        /// </summary>
        [System.ComponentModel.Description("Localized resource is deleted.")]
        event Action<IEnumerable<GXLocalizedResource>>? OnLocalizedResourceDelete;
    }
}
