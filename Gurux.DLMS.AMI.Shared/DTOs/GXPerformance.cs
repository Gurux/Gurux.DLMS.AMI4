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
using System.ComponentModel.DataAnnotations;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>Aggregated operation counts and execution times for a resource over a measurement interval.</summary>
    [Description("Aggregated operation counts and execution times for a resource over a measurement interval.")]
    public class GXPerformance : IUnique<Guid>
    {
        /// <summary>
        /// Performance identifier.
        /// </summary>
        //Filter uses default value.
        [Description("Performance identifier.")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>Resource type whose operation counts and timings are measured.</summary>
        [Description("Resource type whose operation counts and timings are measured.")]
        [StringLength(32)]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public string? Target { get; set; }

        /// <summary>
        /// The number of the list requests during the period.
        /// </summary>
        [Description("The number of the list requests during the period.")]
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? ListCount { get; set; }

        /// <summary>
        /// The total time spent on list requests in milliseconds during the period.
        /// </summary>
        [Description("The total time spent on list requests in milliseconds during the period.")]
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? ListTime { get; set; }

        /// <summary>
        /// The number of the get requests during the period.
        /// </summary>
        [Description("The number of the get requests during the period.")]
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? ReadCount { get; set; }

        /// <summary>
        /// The total time spent on get requests in milliseconds during the period.
        /// </summary>
        [Description("The total time spent on get requests in milliseconds during the period.")]
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? ReadTime { get; set; }

        /// <summary>
        /// The number of the add requests during the period.
        /// </summary>
        [Description("The number of the add requests during the period.")]
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? AddCount { get; set; }

        /// <summary>
        /// The total time spent on add requests in milliseconds during the period.
        /// </summary>
        [Description("The total time spent on add requests in milliseconds during the period.")]
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? AddTime { get; set; }

        /// <summary>
        /// The total time spent on add notifications in milliseconds during the period.
        /// </summary>
        [Description("The total time spent on add notifications in milliseconds during the period.")]
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? AddNotificationTime { get; set; }


        /// <summary>
        /// The number of the update requests during the period.
        /// </summary>
        [Description("The number of the update requests during the period.")]
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? UpdateCount { get; set; }

        /// <summary>
        /// The total time spent on update requests in milliseconds during the period.
        /// </summary>
        [Description("The total time spent on update requests in milliseconds during the period.")]
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? UpdateTime { get; set; }

        /// <summary>
        /// The total time spent on update notifications in milliseconds during the period.
        /// </summary>
        [Description("The total time spent on update notifications in milliseconds during the period.")]
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? UpdateNotificationTime { get; set; }


        /// <summary>
        /// The number of the delete requests during the period.
        /// </summary>
        [Description("The number of the delete requests during the period.")]
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? DeleteCount { get; set; }

        /// <summary>
        /// The total time spent on delete requests in milliseconds during the period.
        /// </summary>
        [Description("The total time spent on delete requests in milliseconds during the period.")]
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? DeleteTime { get; set; }

        /// <summary>
        /// The total time spent on delete notifications in milliseconds during the period.
        /// </summary>
        [Description("The total time spent on delete notifications in milliseconds during the period.")]
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? DeleteNotificationTime { get; set; }

        /// <summary>
        /// The number of the clear requests during the period.
        /// </summary>
        [Description("The number of the clear requests during the period.")]
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? ClearCount { get; set; }

        /// <summary>
        /// The total time spent on clear requests in milliseconds during the period.
        /// </summary>
        [Description("The total time spent on clear requests in milliseconds during the period.")]
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? ClearTime { get; set; }

        /// <summary>
        /// The total time spent on clear notifications in milliseconds during the period.
        /// </summary>
        [Description("The total time spent on clear notifications in milliseconds during the period.")]
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? ClearNotificationTime { get; set; }

        /// <summary>Start of the measurement interval.</summary>
        [Description("Start of the measurement interval.")]
        [DefaultValue(null)]
        [IsRequired]
        [Index(false, Descend = true)]
        public DateTimeOffset? Start { get; set; }

        /// <summary>End of the measurement interval.</summary>
        [Description("End of the measurement interval.")]
        [DefaultValue(null)]
        [IsRequired]
        public DateTimeOffset? End { get; set; }

        /// <summary>Total number of add, read, update, delete, list, and clear operations during the interval.</summary>
        [Description("Total number of add, read, update, delete, list, and clear operations during the interval.")]
        public UInt64 TotalCount
        {
            get
            {
                UInt64 count = AddCount.GetValueOrDefault();
                count += ReadCount.GetValueOrDefault();
                count += UpdateCount.GetValueOrDefault();
                count += DeleteCount.GetValueOrDefault();
                count += ListCount.GetValueOrDefault();
                count += ClearCount.GetValueOrDefault();
                return count;
            }
        }

        /// <summary>
        /// Total execution time for add, read, update, delete, list, and clear operations, in milliseconds.
        /// </summary>
        [Description("Total execution time for add, read, update, delete, list, and clear operations, in milliseconds.")]
        public UInt64 TotalTime
        {
            get
            {
                UInt64 count = AddTime.GetValueOrDefault();
                count += ReadTime.GetValueOrDefault();
                count += UpdateTime.GetValueOrDefault();
                count += DeleteTime.GetValueOrDefault();
                count += ListTime.GetValueOrDefault();
                count += ClearTime.GetValueOrDefault();
                return count;
            }
        }

        /// <summary>Total time spent delivering add, update, delete, and clear notifications, in milliseconds.</summary>
        [Description("Total time spent delivering add, update, delete, and clear notifications, in milliseconds.")]
        public UInt64 NotificationTime
        {
            get
            {
                UInt64 count = AddNotificationTime.GetValueOrDefault();
                count += UpdateNotificationTime.GetValueOrDefault();
                count += DeleteNotificationTime.GetValueOrDefault();
                count += ClearNotificationTime.GetValueOrDefault();
                return count;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string str = Target + " " + Start + "-" + End;
            str += "Count:" + TotalCount + ", Total: " + TotalTime;
            return str;
        }
    }
}
