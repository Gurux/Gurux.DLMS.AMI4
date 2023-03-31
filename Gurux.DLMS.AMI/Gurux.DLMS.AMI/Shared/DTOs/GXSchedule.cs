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
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// Schedule controller.
    /// </summary>
    public class GXSchedule : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXSchedule()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// This constuctor is called when a new schedule is created. It will create all needed lists.
        /// </remarks>
        /// <param name="name">Schedule name.</param>
        public GXSchedule(string? name)
        {
            Name = name;
            Attributes = new List<GXAttribute>();
            Objects = new List<GXObject>();
            Devices = new List<GXDevice>();
            ScriptMethods = new List<GXScriptMethod>();
            DeviceGroups = new List<GXDeviceGroup>();
            ScheduleGroups = new List<GXScheduleGroup>();
            Triggers = new List<GXTrigger>();
            Logs = new List<GXScheduleLog>();
            Modules = new List<GXModule>();
        }

        /// <summary>
        /// Reader identifier.
        /// </summary>
        [Description("Schedule identifier.")]
        [DataMember]
        //Filter uses default value.
        [DefaultValue(null)]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Scheduled attributes.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXAttribute), typeof(GXScheduleToAttribute))]
        public List<GXAttribute>? Attributes
        {
            get;
            set;
        }

        /// <summary>
        /// Scheduled objects.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXObject), typeof(GXScheduleToObject))]
        public List<GXObject>? Objects
        {
            get;
            set;
        }

        /// <summary>
        /// Scheduled devices.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXDevice), typeof(GXScheduleToDevice))]
        public List<GXDevice>? Devices
        {
            get;
            set;
        }

        /// <summary>
        /// Scheduled device groups.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXDeviceGroup), typeof(GXScheduleToDeviceGroup))]
        public List<GXDeviceGroup>? DeviceGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Scheduled script methods.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXScriptMethod), typeof(GXScheduleScript))]
        public List<GXScriptMethod>? ScriptMethods
        {
            get;
            set;
        }

        /// <summary>
        /// Scheduled modules.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXModule), typeof(GXScheduleModule))]
        public List<GXModule>? Modules
        {
            get;
            set;
        }

        /// <summary>
        /// Scheduled triggers.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXTrigger), typeof(GXScheduleTrigger))]
        public List<GXTrigger>? Triggers
        {
            get;
            set;
        }

        /// <summary>
        /// List of schedule groups where this schedule belongs.
        /// </summary>
        [DataMember,
            ForeignKey(typeof(GXScheduleGroup), typeof(GXScheduleGroupSchedule))]
        public List<GXScheduleGroup>? ScheduleGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Schedule logs.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXScheduleLog))]
        public List<GXScheduleLog>? Logs
        {
            get;
            set;
        }

        /// <summary>
        /// The creator of the schedule.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GXUser? Creator
        {
            get;
            set;
        }


        /// <summary>
        /// Schedule name.
        /// </summary>
        [DataMember]
        [StringLength(64)]
        //Filter uses default value.
        [DefaultValue(null)]
        [Index(Unique = false)]
        [Filter(FilterType.Contains)]
        public string? Name
        {
            get;
            set;
        }
      
        /// <summary>
        /// Creation time.
        /// </summary>
        [DataMember]
        //Filter uses default value.
        [DefaultValue(null)]
        [Index(false, Descend = true)]
        [IsRequired]
        public DateTime? CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// When schedule is updated last time.
        /// </summary>
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated
        {
            get;
            set;
        }

        /// <summary>
        /// User has modified the item.
        /// </summary>
        [IgnoreDataMember]
        [Ignore]
        [JsonIgnore]
        public bool Modified
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
        /// Schedule start time.
        /// </summary>
        [DataMember]
        [StringLength(36)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Start
        {
            get;
            set;
        }

        /// <summary>
        /// Last execution time
        /// </summary>
        [DataMember]
        //Filter uses default value.
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? ExecutionTime
        {
            get;
            set;
        }

        /// <summary>
        /// Remove time.
        /// </summary>
        //Filter uses default value.
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
        /// Update creation time before update.
        /// </summary>
        public override void BeforeAdd()
        {
            if (CreationTime == DateTime.MinValue)
            {
                CreationTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Update concurrency stamp.
        /// </summary>
        public override void BeforeUpdate()
        {
            Updated = DateTime.Now;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name))
            {
                return Name;
            }
            return nameof(GXSchedule);
        }
    }
}
