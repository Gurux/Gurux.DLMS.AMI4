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
using Gurux.Service.Orm.Common;
using System.Runtime.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;

using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested schedule information.</summary>
    [Description("Response containing the requested schedule information.")]
    public class GetScheduleResponse
    {
        /// <summary>
        /// Schedule information.
        /// </summary>
        [Description("Schedule information.")]
        [IncludeOpenApi(typeof(GXScheduleGroup), nameof(GXScheduleGroup.Id),
            nameof(GXScheduleGroup.Name))]
        [IncludeOpenApi(typeof(GXAttribute), nameof(GXAttribute.Id),
            nameof(GXAttribute.Template))]
        [IncludeOpenApi(typeof(GXAttributeTemplate), nameof(GXAttributeTemplate.Id),
            nameof(GXAttributeTemplate.Name))]
        [IncludeOpenApi(typeof(GXObject), nameof(GXObject.Id),
            nameof(GXObject.Template))]
        [IncludeOpenApi(typeof(GXObjectTemplate), nameof(GXObjectTemplate.Id),
            nameof(GXObjectTemplate.Name))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id),
            nameof(GXDevice.Template))]
        [IncludeOpenApi(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id),
            nameof(GXDeviceTemplate.Name))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id),
            nameof(GXDeviceGroup.Name))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id),
            nameof(GXScriptMethod.Name))]
        [IncludeOpenApi(typeof(GXTrigger), nameof(GXTrigger.Id),
            nameof(GXTrigger.Name))]
        [IncludeOpenApi(typeof(GXAttribute), nameof(GXAttribute.Id))]
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [ExcludeOpenApi(typeof(GXSchedule),
             nameof(GXSchedule.Creator),
             nameof(GXSchedule.Devices),
             nameof(GXSchedule.ScriptMethods),
             nameof(GXSchedule.DeviceGroups),
             nameof(GXSchedule.ScheduleGroups),
             nameof(GXSchedule.Triggers),
             nameof(GXSchedule.Logs),
             nameof(GXSchedule.Modules))]
        public GXSchedule? Item { get; set; }
    }

    /// <summary>Requests a list of schedules.</summary>
    [Description("Requests a list of schedules.")]
    [DataContract]
    public class ListSchedules : IGXRequest<ListSchedulesResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of schedules to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of schedules to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter schedules.
        /// </summary>
        [Description("Optional criteria used to filter schedules.")]
        [ExcludeOpenApi(typeof(GXSchedule),
             nameof(GXSchedule.Creator),
             nameof(GXSchedule.Devices),
             nameof(GXSchedule.ScriptMethods),
             nameof(GXSchedule.DeviceGroups),
             nameof(GXSchedule.ScheduleGroups),
             nameof(GXSchedule.DeviceAttributeTemplates),
             nameof(GXSchedule.DeviceGroupAttributeTemplates),
             nameof(GXSchedule.DeviceObjectTemplates),
             nameof(GXSchedule.DeviceGroupObjectTemplates),
             nameof(GXSchedule.Triggers),
             nameof(GXSchedule.Logs),
             nameof(GXSchedule.Modules))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        [ExcludeOpenApi(typeof(GXSchedule),
            nameof(GXSchedule.Description), nameof(GXSchedule.CreationTime), nameof(GXSchedule.TraceLevel),
            nameof(GXSchedule.Concurrently), nameof(GXSchedule.ConcurrencyStamp))]
        public GXSchedule? Filter { get; set; }

        /// <summary>Gets or sets inclusive date and time ranges for properties of the filter type.</summary>
        /// <remarks>
        /// Ranges are combined with each other and with <see cref="Filter"/> using AND.
        /// Specify a public stored date and time property name. Null or an empty collection
        /// applies no additional date restrictions. Filtering precedes counting and paging.
        /// </remarks>
        [DataMember]
        [Description("Inclusive date and time ranges combined with the other filters using AND.")]
        public IEnumerable<GXDateTimeFilter>? DateFilters { get; set; }


        /// <summary>
        /// Admin user can access schedules from all users.
        /// </summary>
        /// <remarks>
        /// If true, schedules from all users are retrieved, not just current user.
        /// </remarks>
        [Description("Admin user can access schedules from all users.")]
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

    /// <summary>Response containing the requested schedules.</summary>
    [Description("Response containing the requested schedules.")]
    [DataContract]
    public class ListSchedulesResponse
    {
        /// <summary>
        /// List of schedule items.
        /// </summary>
        [Description("List of schedule items.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXSchedule),
             nameof(GXSchedule.Creator),
             nameof(GXSchedule.Devices),
             nameof(GXSchedule.ScriptMethods),
             nameof(GXSchedule.DeviceGroups),
             nameof(GXSchedule.ScheduleGroups),
             nameof(GXSchedule.Triggers),
             nameof(GXSchedule.Logs),
             nameof(GXSchedule.Modules))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        [ExcludeOpenApi(typeof(GXSchedule), nameof(GXSchedule.DeviceAttributeTemplates),
            nameof(GXSchedule.DeviceObjectTemplates), nameof(GXSchedule.DeviceGroupAttributeTemplates),
            nameof(GXSchedule.DeviceGroupObjectTemplates))]
        public IEnumerable<GXSchedule>? Schedules { get; set; }

        /// <summary>
        /// Total number of matching schedulers before paging.
        /// </summary>
        [Description("Total number of matching schedulers before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests changes to the specified schedules.</summary>
    [Description("Requests changes to the specified schedules.")]
    [DataContract]
    public class UpdateSchedule : IGXRequest<UpdateScheduleResponse>
    {
        /// <summary>Initializes a new instance of UpdateSchedule.</summary>
        [Description("Initializes a new instance of UpdateSchedule.")]
        public UpdateSchedule()
        {
            Schedules = [];
        }

        /// <summary>
        /// Schedules to update.
        /// </summary>
        [Description("Schedules to update.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXScheduleGroup), nameof(GXScheduleGroup.Id))]
        [IncludeOpenApi(typeof(GXAttribute), nameof(GXAttribute.Id))]
        [IncludeOpenApi(typeof(GXObject), nameof(GXObject.Id))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeOpenApi(typeof(GXScript), nameof(GXScript.Id))]
        [ExcludeOpenApi(typeof(GXSchedule), nameof(GXSchedule.ScriptMethods),
            nameof(GXSchedule.Triggers),
            nameof(GXSchedule.Logs),
            nameof(GXSchedule.Modules))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXAttributeTemplate), nameof(GXAttributeTemplate.Id))]
        [IncludeOpenApi(typeof(GXObjectTemplate), nameof(GXObjectTemplate.Id))]
        [ExcludeOpenApi(typeof(GXSchedule), nameof(GXSchedule.CreationTime), nameof(GXSchedule.Updated))]
        public IEnumerable<GXSchedule> Schedules { get; set; } = default!;
    }

    /// <summary>Response to updating the specified schedules.</summary>
    [Description("Response to updating the specified schedules.")]
    [DataContract]
    public class UpdateScheduleResponse
    {
        /// <summary>Identifiers of the added or updated schedule records.</summary>
        [Description("Identifiers of the added or updated schedule records.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Requests deletion of the selected schedules.</summary>
    [Description("Requests deletion of the selected schedules.")]
    [DataContract]
    public class RemoveSchedule : IGXRequest<RemoveScheduleResponse>
    {
        /// <summary>Identifiers of the schedule records to delete.</summary>
        [Description("Identifiers of the schedule records to delete.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;

        /// <summary>Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.</summary>
        /// <remarks>
        /// If false, marks the items as removed and retains their database records.
        /// </remarks>
        [Description("Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.")]
        [DataMember]
        [Required]
        public bool Delete { get; set; }
    }

    /// <summary>Response to deleting the selected schedules.</summary>
    [Description("Response to deleting the selected schedules.")]
    [DataContract]
    public class RemoveScheduleResponse
    {
    }

    /// <summary>
    /// Get module settings for the schedule.
    /// </summary>
    [Description("Get module settings for the schedule.")]
    [DataContract]
    public class GetModuleSettings : IGXRequest<GetModuleSettingsResponse>
    {
        /// <summary>Module and schedule identifiers.</summary>
        [Description("Module and schedule identifiers.")]
        [DataMember]
        public GXScheduleModule? Item { get; set; }
    }

    /// <summary>
    /// Reply module settings for the schedule.
    /// </summary>
    [Description("Reply module settings for the schedule.")]
    [DataContract]
    public class GetModuleSettingsResponse
    {
        /// <summary>
        /// Module settings for the schedule.
        /// </summary>
        [Description("Module settings for the schedule.")]
        [DataMember]
        public GXScheduleModule? Item { get; set; }
    }

    /// <summary>
    /// Updates module settings for a schedule.
    /// </summary>
    [Description("Updates module settings for a schedule.")]
    [DataContract]
    public class UpdateModuleSettings : IGXRequest<UpdateModuleSettingsResponse>
    {
        /// <summary>
        /// Module and schedule settings.
        /// </summary>
        [Description("Module and schedule settings.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        [ExcludeOpenApi(typeof(GXScheduleModule), nameof(GXScheduleModule.CreationTime), nameof(GXScheduleModule.Updated))]
        public GXScheduleModule? Item { get; set; }
    }

    /// <summary>
    /// Reply module settings for the schedule.
    /// </summary>
    [Description("Reply module settings for the schedule.")]
    [DataContract]
    public class UpdateModuleSettingsResponse
    {
        /// <summary>
        /// Updated item.
        /// </summary>
        [Description("Updated item.")]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                      nameof(GXUser.UserName))]
        public GXScheduleModule? Item { get; set; }
    }

}
