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
using Gurux.Common.Db;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// Performance is used to find possible bottle-necks.
    /// </summary>
    public class GXPerformance : IUnique<Guid>
    {
        /// <summary>
        /// Performance identifier.
        /// </summary>
        //Filter uses default value.
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// The target type.
        /// </summary>
        [StringLength(32)]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public string? Target
        {
            get;
            set;
        }

        /// <summary>
        /// The number of the list requests during the period.
        /// </summary>
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? ListCount
        {
            get;
            set;
        }

        /// <summary>
        /// The total time spent on list requests in milliseconds during the period.
        /// </summary>
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? ListTime
        {
            get;
            set;
        }

        /// <summary>
        /// The number of the get requests during the period.
        /// </summary>
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? ReadCount
        {
            get;
            set;
        }

        /// <summary>
        /// The total time spent on get requests in milliseconds during the period.
        /// </summary>
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? ReadTime
        {
            get;
            set;
        }

        /// <summary>
        /// The number of the add requests during the period.
        /// </summary>
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? AddCount
        {
            get;
            set;
        }

        /// <summary>
        /// The total time spent on add requests in milliseconds during the period.
        /// </summary>
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? AddTime
        {
            get;
            set;
        }

        /// <summary>
        /// The total time spent on add notifications in milliseconds during the period.
        /// </summary>
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? AddNotificationTime
        {
            get;
            set;
        }


        /// <summary>
        /// The number of the update requests during the period.
        /// </summary>
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? UpdateCount
        {
            get;
            set;
        }

        /// <summary>
        /// The total time spent on update requests in milliseconds during the period.
        /// </summary>
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? UpdateTime
        {
            get;
            set;
        }

        /// <summary>
        /// The total time spent on update notifications in milliseconds during the period.
        /// </summary>
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? UpdateNotificationTime
        {
            get;
            set;
        }


        /// <summary>
        /// The number of the delete requests during the period.
        /// </summary>
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? DeleteCount
        {
            get;
            set;
        }

        /// <summary>
        /// The total time spent on delete requests in milliseconds during the period.
        /// </summary>
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? DeleteTime
        {
            get;
            set;
        }

        /// <summary>
        /// The total time spent on delete notifications in milliseconds during the period.
        /// </summary>
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? DeleteNotificationTime
        {
            get;
            set;
        }

        /// <summary>
        /// The number of the clear requests during the period.
        /// </summary>
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? ClearCount
        {
            get;
            set;
        }

        /// <summary>
        /// The total time spent on clear requests in milliseconds during the period.
        /// </summary>
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? ClearTime
        {
            get;
            set;
        }

        /// <summary>
        /// The total time spent on clear notifications in milliseconds during the period.
        /// </summary>
        [DefaultValue(0)]
        [IsRequired]
        public UInt64? ClearNotificationTime
        {
            get;
            set;
        }

        /// <summary>
        /// The start time of performance.
        /// </summary>
        [DefaultValue(null)]
        [IsRequired]
        [Index(false, Descend = true)]
        public DateTimeOffset? Start
        {
            get;
            set;
        }

        /// <summary>
        /// The end time of performance.
        /// </summary>
        [DefaultValue(null)]
        [IsRequired]
        public DateTimeOffset? End
        {
            get;
            set;
        }

        /// <summary>
        /// Returns total operation count.
        /// </summary>
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
        /// Returns total time.
        /// </summary>
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

        /// <summary>
        /// Returns total notification count.
        /// </summary>
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
