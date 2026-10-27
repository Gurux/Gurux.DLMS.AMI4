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
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Schedule
{
    /// <summary>Associates a schedule with a target device.</summary>
    [Description("Associates a schedule with a target device.")]
    [IndexCollection(true, nameof(ScheduleId), nameof(DeviceId), Clustered = true)]
    public class GXScheduleToDevice
    {
        /// <summary>Schedule identifier.</summary>
        [Description("Schedule identifier.")]
        [DataMember]
        [ForeignKey(typeof(GXSchedule), OnDelete = ForeignKeyDelete.None)]
        //ForeignKeyDelete is None because creator of the device is causing multiple cascade paths error in MSSQL.
        public Guid ScheduleId { get; set; }

        /// <summary>Device identifier.</summary>
        [Description("Device identifier.")]
        [DataMember]
        [ForeignKey(typeof(GXDevice), OnDelete = ForeignKeyDelete.Cascade)]
        public Guid DeviceId { get; set; }

        /// <summary>
        /// Creation time.
        /// The time when the device was added to the schedule.
        /// </summary>
        [Description("Creation time. The time when the device was added to the schedule.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when device was removed from user schedule.
        /// </summary>
        [Description("Time when device was removed from user schedule.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }
    }
}
