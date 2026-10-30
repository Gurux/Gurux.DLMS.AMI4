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
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.Enums;
using System.Runtime.Serialization;

using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Requests device tasks that read or write attributes or invoke actions.</summary>
    [Description("Requests device tasks that read or write attributes or invoke actions.")]
    [DataContract]
    public class AddTask
    {
        /// <summary>
        /// Tasks to execute.
        /// </summary>
        [Description("Tasks to execute.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id))]
        [IncludeOpenApi(typeof(GXScript), nameof(GXScript.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id))]
        [IncludeOpenApi(typeof(GXObject), nameof(GXObject.Id))]
        [IncludeOpenApi(typeof(GXAttribute), nameof(GXAttribute.Id))]
        [IncludeOpenApi(typeof(GXSchedule), nameof(GXSchedule.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [ExcludeOpenApi(typeof(GXTask), nameof(GXTask.TriggerUser), nameof(GXTask.Creator))]
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id))]
        [ExcludeOpenApi(typeof(GXTask), nameof(GXTask.CreationTime), nameof(GXTask.Updated))]
        public IEnumerable<GXTask>? Tasks { get; set; }
    }

    /// <summary>Response to adding tasks.</summary>
    [Description("Response to adding tasks.")]
    [DataContract]
    public class AddTaskResponse
    {
        /// <summary>Identifiers of the newly added task records.</summary>
        [Description("Identifiers of the newly added task records.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Requests a list of tasks.</summary>
    [Description("Requests a list of tasks.")]
    [DataContract]
    public class ListTasks
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of tasks to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of tasks to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter tasks.
        /// </summary>

        [Description("Optional criteria used to filter tasks.")]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id))]
        [IncludeOpenApi(typeof(GXScript), nameof(GXScript.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id))]
        [IncludeOpenApi(typeof(GXObject), nameof(GXObject.Id))]
        [IncludeOpenApi(typeof(GXAttribute), nameof(GXAttribute.Id))]
        [IncludeOpenApi(typeof(GXSchedule), nameof(GXSchedule.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id))]
        [ExcludeOpenApi(typeof(GXTask),
            nameof(GXTask.TargetAgent), nameof(GXTask.TargetGateway), nameof(GXTask.Result),
            nameof(GXTask.Error), nameof(GXTask.Index), nameof(GXTask.Data))]
        public GXTask? Filter { get; set; }

        /// <summary>Gets or sets inclusive date and time ranges for properties of the filter type.</summary>
        /// <remarks>
        /// Ranges are combined with each other and with <see cref="Filter"/> using AND.
        /// Specify a public date and time property name. Null or an empty collection
        /// applies no additional date restrictions. Filtering precedes counting and paging.
        /// </remarks>
        [DataMember]
        [Description("Inclusive date and time ranges combined with the other filters using AND.")]
        public IEnumerable<GXDateTimeFilter>? DateFilters { get; set; }

        /// <summary>
        /// Admin user can access tasks from all users.
        /// </summary>
        /// <remarks>
        /// If true, tasks from all users are retrieved, not just current user.
        /// </remarks>
        [Description("Admin user can access tasks from all users.")]
        public bool AllUsers { get; set; }


        /// <summary>Additional related information to include in the response. Reserved for future use.</summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        [Description("Additional related information to include in the response. Reserved for future use.")]
        public IEnumerable<string>? Select { get; set; }

        /// <summary>Name of the property to sort by. If omitted, the endpoint uses its default ordering.</summary>
        /// <remarks>
        /// Uses the endpoint's default ordering when not specified.
        /// </remarks>
        /// <seealso cref="Descending"/>
        [Description("Name of the property to sort by. If omitted, the endpoint uses its default ordering.")]
        public string? OrderBy { get; set; }

        /// <summary>
        /// Whether to sort results in descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        [Description("Whether to sort results in descending order.")]
        public bool Descending { get; set; }

        /// <summary>Identifiers to include in the results. A nonempty collection restricts results to these identifiers.</summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        [Description("Identifiers to include in the results. A nonempty collection restricts results to these identifiers.")]
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        [Description("Identifiers to exclude from the results.")]
        public IEnumerable<Guid>? Exclude { get; set; }
    }

    /// <summary>Response containing the requested tasks.</summary>
    [Description("Response containing the requested tasks.")]
    [DataContract]
    public class ListTasksResponse
    {
        /// <summary>
        /// List of tasks.
        /// </summary>
        [Description("List of tasks.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id))]
        [IncludeOpenApi(typeof(GXScript), nameof(GXScript.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id))]
        [IncludeOpenApi(typeof(GXObject), nameof(GXObject.Id))]
        [IncludeOpenApi(typeof(GXAttribute), nameof(GXAttribute.Id))]
        [IncludeOpenApi(typeof(GXSchedule), nameof(GXSchedule.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id))]
        public IEnumerable<GXTask>? Tasks { get; set; }

        /// <summary>
        /// Total number of matching tasks before paging.
        /// </summary>
        /// <remarks>
        /// With large databases reading the amount of the data can take a very long time.
        /// In those cases the count is set to -1.
        /// </remarks>
        [Description("Total number of matching tasks before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>
    /// Reserved for internal use. Get next executed task. Readers use this.
    /// </summary>
    [Description("Reserved for internal use. Get next executed task. Readers use this.")]
    [DataContract]
    public class GetNextTask
    {
        /// <summary>
        /// Agent identifier.
        /// </summary>
        [Description("Agent identifier.")]
        [DataMember]
        public Guid AgentId { get; set; }

        /// <summary>
        /// Gateway identifier.
        /// </summary>
        /// <remarks>
        /// If gateway identifier is given, all tasks for that gateway are retrieved.
        /// </remarks>
        [Description("Gateway identifier.")]
        [DataMember]
        public Guid? GatewayId { get; set; }

        /// <summary>
        /// Device identifier.
        /// </summary>
        /// <remarks>
        /// If device identifier is given, all tasks for that device are retrieved.
        /// </remarks>
        [Description("Device identifier.")]
        [DataMember]
        public Guid? DeviceId { get; set; }

        /// <summary>
        /// Is listener or normal read asking next task.
        /// </summary>
        [Description("Is listener or normal read asking next task.")]
        [DataMember]
        public bool Listener { get; set; }

        /// <summary>
        /// How long is the new task expected.
        /// </summary>
        [Description("How long is the new task expected.")]
        [DataMember]
        public int WaitTime { get; set; }
    }

    /// <summary>
    /// Get next task response.
    /// </summary>
    [Description("Get next task response.")]
    [DataContract]
    public class GetNextTaskResponse
    {
        /// <summary>
        /// Executed tasks. Null if there are no operations to execute.
        /// </summary>
        [Description("Executed tasks. Null if there are no operations to execute.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id))]
        [IncludeOpenApi(typeof(GXScript), nameof(GXScript.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id))]
        [IncludeOpenApi(typeof(GXObject), nameof(GXObject.Id))]
        [IncludeOpenApi(typeof(GXAttribute), nameof(GXAttribute.Id))]
        [IncludeOpenApi(typeof(GXSchedule), nameof(GXSchedule.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [ExcludeOpenApi(typeof(GXTask), nameof(GXTask.TriggerUser), nameof(GXTask.Creator))]
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id))]
        public IEnumerable<GXTask>? Tasks { get; set; }
    }


    /// <summary>
    /// Reserved for internal use. Mark task to executed. Readers use this.
    /// </summary>
    [Description("Reserved for internal use. Mark task to executed. Readers use this.")]
    [DataContract]
    public class TaskDone
    {
        /// <summary>
        /// Executed tasks with their identifiers, original Start timestamps, and optional Result errors and Data.
        /// </summary>
        [Description("Executed tasks with their identifiers, original Start timestamps, and optional Result errors and Data.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXTask), nameof(GXTask.Id), nameof(GXTask.Start), nameof(GXTask.Result), nameof(GXTask.Data))]
        public IEnumerable<GXTask>? Tasks { get; set; }
    }

    /// <summary>
    /// Reserved for internal use. Task ready reply. Readers use this.
    /// </summary>
    [Description("Reserved for internal use. Task ready reply. Readers use this.")]
    [DataContract]
    public class TaskDoneResponse
    {

    }

    /// <summary>
    /// Reserved for internal use. Restart tasks.
    /// </summary>
    [Description("Reserved for internal use. Restart tasks.")]
    [DataContract]
    public class TaskRestart
    {
        /// <summary>
        /// Executed tasks.
        /// </summary>
        [Description("Executed tasks.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXTask), nameof(GXTask.Id))]
        public IEnumerable<GXTask>? Tasks { get; set; }
    }

    /// <summary>
    /// Reserved for internal use. Restart tasks reply.
    /// </summary>
    [Description("Reserved for internal use. Restart tasks reply.")]
    [DataContract]
    public class TaskRestartResponse
    {

    }

    /// <summary>Requests deletion of the selected tasks.</summary>
    [Description("Requests deletion of the selected tasks.")]
    [DataContract]
    public class DeleteTask
    {
        /// <summary>Identifiers of the task records to delete.</summary>
        [Description("Identifiers of the task records to delete.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Response to deleting the selected tasks.</summary>
    [Description("Response to deleting the selected tasks.")]
    [DataContract]
    public class DeleteTaskResponse
    {
    }

    /// <summary>
    /// Clear tasks.
    /// </summary>
    [Description("Clear tasks.")]
    [DataContract]
    public class ClearTask
    {
    }

    /// <summary>Response to clearing tasks.</summary>
    [Description("Response to clearing tasks.")]
    [DataContract]
    public class ClearTaskResponse
    {
    }
}
