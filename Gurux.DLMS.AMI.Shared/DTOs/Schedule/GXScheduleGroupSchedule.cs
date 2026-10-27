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
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Schedule
{
    /// <summary>Associates a schedule with a schedule group.</summary>
    [Description("Associates a schedule with a schedule group.")]
    [DataContract(Name = "GXScheduleGroupSchedule"), Serializable]
    [IndexCollection(true, nameof(ScheduleGroupId), nameof(ScheduleId), Clustered = true)]
    public class GXScheduleGroupSchedule
    {
        /// <summary>The database identifier of the Schedule group.</summary>
        [Description("The database identifier of the Schedule group.")]
        [DataMember(Name = "ScheduleGroupId")]
        [ForeignKey(typeof(GXScheduleGroup), OnDelete = ForeignKeyDelete.None)]
        //ForeignKeyDelete is None because creator of the schedule is causing multiple cascade paths error in MSSQL.
        public Guid ScheduleGroupId { get; set; }

        /// <summary>The database identifier of the schedule.</summary>
        [Description("The database identifier of the schedule.")]
        [DataMember(Name = "ScheduleID")]
        [ForeignKey(typeof(GXSchedule), OnDelete = ForeignKeyDelete.Cascade)]
        public Guid ScheduleId { get; set; }

        /// <summary>
        /// Creation time.
        /// The time when the schedule was added to the schedule group.
        /// </summary>
        [Description("Creation time. The time when the schedule was added to the schedule group.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset CreationTime { get; set; }

        /// <summary>
        /// Time when schedule was removed from schedule group.
        /// </summary>
        [Description("Time when schedule was removed from schedule group.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }
    }
}
