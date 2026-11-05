// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

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
