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
    /// <summary>Selects an attribute template for a scheduled device operation.</summary>
    [Description("Selects an attribute template for a scheduled device operation.")]
    [IndexCollection(true, nameof(ScheduleId), nameof(AttributeTemplateId), Clustered = true)]
    public class GXScheduleToDeviceAttributeTemplate
    {
        /// <summary>Schedule identifier.</summary>
        [Description("Schedule identifier.")]
        [DataMember]
        [ForeignKey(typeof(GXSchedule), OnDelete = ForeignKeyDelete.None)]
        //ForeignKeyDelete is None because creator of the device template is causing multiple cascade paths error in MSSQL.
        public Guid ScheduleId { get; set; }

        /// <summary>Attribute template identifier.</summary>
        [Description("Attribute template identifier.")]
        [DataMember]
        [ForeignKey(typeof(GXAttributeTemplate), OnDelete = ForeignKeyDelete.Cascade)]
        public Guid AttributeTemplateId { get; set; }

        /// <summary>
        /// Creation time.
        /// The time when the attribute template was added to the schedule.
        /// </summary>
        [Description("Creation time. The time when the attribute template was added to the schedule.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when attribute template was removed from user schedule.
        /// </summary>
        [Description("Time when attribute template was removed from user schedule.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }
    }
}
