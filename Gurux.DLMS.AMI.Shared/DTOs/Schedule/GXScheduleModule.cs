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
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Schedule
{
    /// <summary>Associates a schedule with a module to invoke.</summary>
    [Description("Associates a schedule with a module to invoke.")]
    [IndexCollection(true, nameof(ScheduleId), nameof(ModuleId), Clustered = true)]
    public class GXScheduleModule
    {
        /// <summary>Schedule identifier.</summary>
        [Description("Schedule identifier.")]
        [DataMember]
        [ForeignKey(typeof(GXSchedule), OnDelete = ForeignKeyDelete.Cascade)]
        public Guid ScheduleId { get; set; }

        /// <summary>Module identifier.</summary>
        [Description("Module identifier.")]
        [DataMember]
        [ForeignKey(typeof(GXModule), OnDelete = ForeignKeyDelete.None)]
        [StringLength(64)]
        public string ModuleId { get; set; } = default!;
        //ForeignKeyDelete is None because creator of the schedule is causing multiple cascade paths error in MSSQL.

        /// <summary>
        /// Creation time.
        /// The time when the module was added to the schedule.
        /// </summary>
        [Description("Creation time. The time when the module was added to the schedule.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when module was removed from schedule.
        /// </summary>
        [Description("Time when module was removed from schedule.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>
        /// Time when module was updated to the schedule.
        /// </summary>
        [Description("Time when module was updated to the schedule.")]
        [DataMember]
        public DateTimeOffset? Updated { get; set; }

        /// <summary>Version token used by the database to detect conflicting updates.</summary>
        /// <remarks>
        /// A stale token causes a conflicting update to be rejected instead of overwriting newer changes.
        /// </remarks>
        [Description("Version token used by the database to detect conflicting updates.")]
        [DataMember]
        [StringLength(36)]
        [ConcurrencyCheck]
        public string? ConcurrencyStamp { get; set; }

        /// <summary>
        /// Schedule-specific module settings.
        /// </summary>
        [Description("Schedule-specific module settings.")]
        [DataMember]
        public string? Settings { get; set; }
    }
}
