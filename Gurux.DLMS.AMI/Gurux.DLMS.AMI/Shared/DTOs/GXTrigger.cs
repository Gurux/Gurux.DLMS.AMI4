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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// Trigger.
    /// </summary>
    public class GXTrigger : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXTrigger()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXTrigger(string? name)
        {
            Workflows = new List<GXWorkflow>();
            Activities = new List<GXTriggerActivity>();
            TriggerGroups = new List<GXTriggerGroup>();
            Schedules = new List<GXSchedule>();
            Name = name;
            ClassName = "";
        }

        [DataMember(Name = "ID")]
        [Description("Trigger identifier.")]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Trigger class name.
        /// </summary>
        [StringLength(64)]
        [IsRequired]
        public string? ClassName
        {
            get;
            set;
        }

        /// <summary>
        /// Trigger name.
        /// </summary>
        [StringLength(64)]
        [DataMember]
        [Index(false)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// Icon name.
        /// </summary>
        [Description("Icon name.")]
        public string? Icon
        {
            get;
            set;
        }

        /// <summary>
        /// Name of the configuration view class.
        /// </summary>
        [StringLength(128)]
        public string? ConfigurationUI
        {
            get;
            set;
        }

        /// <summary>
        /// Trigger activities.
        /// </summary>
        [ForeignKey(typeof(GXTriggerActivity))]
        public List<GXTriggerActivity>? Activities
        {
            get;
            set;
        }

        /// <summary>
        /// Is Trigger active.
        /// </summary>
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Active { get; set; }

        /// <summary>
        /// User that triggers this event.
        /// </summary>
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        public GXUser? User
        {
            get;
            set;
        }

        /// <summary>
        /// User group that triggers this event.
        /// </summary>
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        public GXUserGroup? UserGroup
        {
            get;
            set;
        }

        /// <summary>
        /// Device that triggers this event.
        /// </summary>
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        public GXDevice? Device
        {
            get;
            set;
        }

        /// <summary>
        /// DeviceGroup that triggers this event.
        /// </summary>
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        public GXDeviceGroup? DeviceGroup
        {
            get;
            set;
        }

        /// <summary>
        /// Module that triggers this event.
        /// </summary>
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        public GXModule? Module
        {
            get;
            set;
        }

        /// <summary>
        /// Scheduled triggers.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXSchedule), typeof(GXScheduleTrigger))]
        public List<GXSchedule>? Schedules
        {
            get;
            set;
        }

        /// <summary>
        /// List of workflows that this trigger invokes.
        /// </summary>
        [Ignore(IgnoreType.Db)]
        public List<GXWorkflow>? Workflows
        {
            get;
            set;
        }

        /// <summary>
        /// List of trigger groups where this trigger belongs.
        /// </summary>
        [DataMember,
            ForeignKey(typeof(GXTriggerGroup), typeof(GXTriggerGroupTrigger))]
        public List<GXTriggerGroup>? TriggerGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Trigger settings.
        /// </summary>
        public string? Settings
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTime? CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// When the Trigger is updated for the last time.
        /// </summary>
        [DataMember]
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
        /// Remove time.
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
        /// Last execution time
        /// </summary>
        [DataMember]
        [Description("Last execution time.")]
        //Filter uses default value.
        [DefaultValue(null)]
        public DateTimeOffset? ExecutionTime
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
    }
}
