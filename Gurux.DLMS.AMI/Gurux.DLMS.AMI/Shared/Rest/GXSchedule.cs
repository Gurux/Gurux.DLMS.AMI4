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
using Gurux.Common;
using System.Runtime.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

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
        [IncludeSwagger(typeof(GXScheduleGroup), nameof(GXScheduleGroup.Id),
            nameof(GXScheduleGroup.Name))]
        [IncludeSwagger(typeof(GXAttribute), nameof(GXAttribute.Id),
            nameof(GXAttribute.Template))]
        [IncludeSwagger(typeof(GXAttributeTemplate), nameof(GXAttributeTemplate.Id),
            nameof(GXAttributeTemplate.Name))]
        [IncludeSwagger(typeof(GXObject), nameof(GXObject.Id),
            nameof(GXObject.Template))]
        [IncludeSwagger(typeof(GXObjectTemplate), nameof(GXObjectTemplate.Id),
            nameof(GXObjectTemplate.Name))]
        [IncludeSwagger(typeof(GXDevice), nameof(GXDevice.Id),
            nameof(GXDevice.Template))]
        [IncludeSwagger(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id),
            nameof(GXDeviceTemplate.Name))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id),
            nameof(GXDeviceGroup.Name))]
        [IncludeSwagger(typeof(GXScriptMethod), nameof(GXScriptMethod.Id),
            nameof(GXScriptMethod.Name))]
        [IncludeSwagger(typeof(GXTrigger), nameof(GXTrigger.Id),
            nameof(GXTrigger.Name))]
        [IncludeSwagger(typeof(GXAttribute), nameof(GXAttribute.Id))]
        [IncludeSwagger(typeof(GXModule), nameof(GXModule.Id))]
        [IncludeSwagger(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [ExcludeSwagger(typeof(GXSchedule),
        nameof(GXSchedule.Attributes),
             nameof(GXSchedule.Creator),
             nameof(GXSchedule.Objects),
             nameof(GXSchedule.Devices),
             nameof(GXSchedule.ScriptMethods),
             nameof(GXSchedule.DeviceGroups),
             nameof(GXSchedule.ScheduleGroups),
             nameof(GXSchedule.Triggers),
             nameof(GXSchedule.Logs),
             nameof(GXSchedule.Modules))]
        public GXSchedule? Item
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get list from schedules.
    /// </summary>
    [DataContract]
    public class ListSchedules : IGXRequest<ListSchedulesResponse>
    {
        /// <summary>
        /// Start index.
        /// </summary>
        public int Index
        {
            get;
            set;

        }

        /// <summary>
        /// Amount of the schedules to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter schedules.
        /// </summary>
        [ExcludeSwagger(typeof(GXSchedule),
        nameof(GXSchedule.Attributes),
             nameof(GXSchedule.Creator),
             nameof(GXSchedule.Objects),
             nameof(GXSchedule.Devices),
             nameof(GXSchedule.ScriptMethods), 
             nameof(GXSchedule.DeviceGroups),
             nameof(GXSchedule.ScheduleGroups),
             nameof(GXSchedule.Triggers),
             nameof(GXSchedule.Logs),
             nameof(GXSchedule.Modules))]
        public GXSchedule? Filter
        {
            get;
            set;
        }


        /// <summary>
        /// Admin user can access schedules from all users.
        /// </summary>
        /// <remarks>
        /// If true, schedules from all users are retreaved, not just current user. 
        /// </remarks>
        public bool AllUsers
        {
            get;
            set;
        }

        /// <summary>
        /// Selected extra information.
        /// </summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        public TargetType Select
        {
            get;
            set;
        }

        /// <summary>
        /// Order by name.
        /// </summary>
        /// <remarks>
        /// Default order by is used if this is not set.
        /// </remarks>
        /// <seealso cref="Descending"/>
        public string? OrderBy
        {
            get;
            set;
        }

        /// <summary>
        /// Are values shown as descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        public bool Descending
        {
            get;
            set;
        }
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
        [ExcludeSwagger(typeof(GXSchedule),
        nameof(GXSchedule.Attributes),
             nameof(GXSchedule.Creator),
             nameof(GXSchedule.Objects),
             nameof(GXSchedule.Devices),
             nameof(GXSchedule.ScriptMethods),
             nameof(GXSchedule.DeviceGroups),
             nameof(GXSchedule.ScheduleGroups),
             nameof(GXSchedule.Triggers),
             nameof(GXSchedule.Logs),
             nameof(GXSchedule.Modules))]
        public GXSchedule[]? Schedules
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the schedulers.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
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
        [IncludeSwagger(typeof(GXScheduleGroup), nameof(GXScheduleGroup.Id))]
        [IncludeSwagger(typeof(GXAttribute), nameof(GXAttribute.Id))]
        [IncludeSwagger(typeof(GXObject), nameof(GXObject.Id))]
        [IncludeSwagger(typeof(GXDevice), nameof(GXDevice.Id))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeSwagger(typeof(GXScript), nameof(GXScript.Id))]
        [ExcludeSwagger(typeof(GXSchedule), nameof(GXSchedule.ScriptMethods),
            nameof(GXSchedule.Creator),
            nameof(GXSchedule.Triggers),
            nameof(GXSchedule.Logs),
            nameof(GXSchedule.Modules))]
        public List<GXSchedule> Schedules
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update schedules reply.
    /// </summary>
    [DataContract]
    public class UpdateScheduleResponse
    {
        /// <summary>
        /// New schedule identifiers.
        /// </summary>
        [DataMember]
        public Guid[] ScheduleIds
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Delete schedules.
    /// </summary>
    [DataContract]
    public class RemoveSchedule : IGXRequest<RemoveScheduleResponse>
    {
        /// <summary>
        /// Removed schedule identifiers.
        /// </summary>
        [DataMember]
        public Guid[] Ids
        {
            get;
            set;
        }

        /// <summary>
        /// Items are removed from the database.
        /// </summary>
        /// <remarks>
        /// If false, the Removed date is set for the items, but items are kept on the database.
        /// </remarks>
        [DataMember]
        [Required]
        public bool Delete
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Reply from Delete schedule.
    /// </summary>
    [DataContract]
    public class RemoveScheduleResponse
    {
    }   
}
