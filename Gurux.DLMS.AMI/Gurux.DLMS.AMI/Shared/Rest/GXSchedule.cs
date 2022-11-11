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

namespace Gurux.DLMS.AMI.Shared.Rest
{
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
        public GXSchedule[] Schedules
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
    public class DeleteSchedule : IGXRequest<DeleteScheduleResponse>
    {
        /// <summary>
        /// Removed schedule identifiers.
        /// </summary>
        [DataMember]
        public Guid[] ScheduleIds
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Reply from Delete schedule.
    /// </summary>
    [DataContract]
    public class DeleteScheduleResponse
    {
    }   
}
