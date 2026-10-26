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

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get schedule.
    /// </summary>
    public class GetScheduleResponse
    {
        /// <summary>
        /// Schedule information.
        /// </summary>
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

    /// <summary>
    /// Get list from schedules.
    /// </summary>
    [DataContract]
    public class ListSchedules : IGXRequest<ListSchedulesResponse>
    {
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Maximum number of schedules to retrieve; zero disables paging.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter schedules.
        /// </summary>
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
        public GXSchedule? Filter { get; set; }


        /// <summary>
        /// Admin user can access schedules from all users.
        /// </summary>
        /// <remarks>
        /// If true, schedules from all users are retrieved, not just current user.
        /// </remarks>
        public bool AllUsers { get; set; }

        /// <summary>
        /// Selected extra information.
        /// </summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        public IEnumerable<string>? Select { get; set; }

        /// <summary>
        /// Name of the property to sort by.
        /// </summary>
        /// <remarks>
        /// Uses the endpoint's default ordering when not specified.
        /// </remarks>
        /// <seealso cref="Descending"/>
        public string? OrderBy { get; set; }

        /// <summary>
        /// Whether to sort results in descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        public bool Descending { get; set; }

        /// <summary>
        /// Identifiers to include in the results.
        /// </summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        public IEnumerable<Guid>? Exclude { get; set; }
    }

    /// <summary>
    /// Schedule items reply.
    /// </summary>
    [DataContract]
    public class ListSchedulesResponse
    {
        /// <summary>
        /// List of schedule items.
        /// </summary>
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
        public IEnumerable<GXSchedule>? Schedules { get; set; }

        /// <summary>
        /// Total number of matching schedulers before paging.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>
    /// Update schedules.
    /// </summary>
    [DataContract]
    public class UpdateSchedule : IGXRequest<UpdateScheduleResponse>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UpdateSchedule()
        {
            Schedules = new List<GXSchedule>();
        }

        /// <summary>
        /// Schedules to update.
        /// </summary>
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
        public IEnumerable<GXSchedule> Schedules { get; set; } = default!;
    }

    /// <summary>
    /// Update schedules reply.
    /// </summary>
    [DataContract]
    public class UpdateScheduleResponse
    {
        /// <summary>
        /// Schedule identifiers.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>
    /// Delete schedules.
    /// </summary>
    [DataContract]
    public class RemoveSchedule : IGXRequest<RemoveScheduleResponse>
    {
        /// <summary>
        /// Schedule identifiers.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;

        /// <summary>
        /// Whether to permanently delete the items from the database.
        /// </summary>
        /// <remarks>
        /// If false, marks the items as removed and retains their database records.
        /// </remarks>
        [DataMember]
        [Required]
        public bool Delete { get; set; }
    }

    /// <summary>
    /// Reply from Delete schedule.
    /// </summary>
    [DataContract]
    public class RemoveScheduleResponse
    {
    }

    /// <summary>
    /// Get module settings for the schedule.
    /// </summary>
    [DataContract]
    public class GetModuleSettings : IGXRequest<GetModuleSettingsResponse>
    {
        /// <summary>
        /// Module and schedule IDs.
        /// </summary>
        [DataMember]
        public GXScheduleModule? Item { get; set; }
    }

    /// <summary>
    /// Reply module settings for the schedule.
    /// </summary>
    [DataContract]
    public class GetModuleSettingsResponse
    {
        /// <summary>
        /// Module settings for the schedule.
        /// </summary>
        [DataMember]
        public GXScheduleModule? Item { get; set; }
    }

    /// <summary>
    /// Updates module settings for a schedule.
    /// </summary>
    [DataContract]
    public class UpdateModuleSettings : IGXRequest<UpdateModuleSettingsResponse>
    {
        /// <summary>
        /// Module and schedule settings.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        public GXScheduleModule? Item { get; set; }
    }

    /// <summary>
    /// Reply module settings for the schedule.
    /// </summary>
    [DataContract]
    public class UpdateModuleSettingsResponse
    {
        /// <summary>
        /// Updated item.
        /// </summary>
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                      nameof(GXUser.UserName))]
        public GXScheduleModule? Item { get; set; }
    }

}
