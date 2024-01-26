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
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Schedule
{
    /// <summary>
    /// This class is used to map schedule to the module.
    /// </summary>
    [IndexCollection(true, nameof(ScheduleId), nameof(ModuleId), Clustered = true)]
    public class GXScheduleModule
    {
        /// <summary>
        /// Schedule ID.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXSchedule), OnDelete = ForeignKeyDelete.Cascade)]
        public Guid ScheduleId
        {
            get;
            set;
        }

        /// <summary>
        /// Module ID.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXModule), OnDelete = ForeignKeyDelete.Cascade)]
        [StringLength(64)]
        public string ModuleId
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// The time when the module was added to the schedule.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTime CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// Time when module was removed from schedule.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed
        {
            get;
            set;
        }

        /// <summary>
        /// Time when module was updated to the schedule.
        /// </summary>
        [DataMember]
        public DateTimeOffset? Updated
        {
            get;
            set;
        }

        /// <summary>
        /// Concurrency stamp.
        /// </summary>
        /// <remarks>
        /// Concurrency stamp is used to verify that several user's can't 
        /// modify the target at the same time.
        /// </remarks>
        [DataMember]
        [StringLength(36)]
        public string? ConcurrencyStamp
        {
            get;
            set;
        }

        /// <summary>
        /// Schedule-specific module settings.
        /// </summary>
        [DataMember]
        public string? Settings
        {
            get;
            set;
        }
    }
}
