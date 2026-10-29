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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Trigger
{
    /// <summary>A trigger definition that evaluates conditions and runs configured activities.</summary>
    [Description("A trigger definition that evaluates conditions and runs configured activities.")]
    public class GXTrigger : GXTableBase, IUnique<Guid>
    {
        /// <summary>Initializes a new instance of GXTrigger.</summary>
        [Description("Initializes a new instance of GXTrigger.")]
        public GXTrigger()
        {
        }

        /// <summary>Initializes a new instance of GXTrigger.</summary>
        /// <param name="name">Display name of the trigger, or null when not assigned.</param>
        [Description("Initializes a new instance of GXTrigger.")]
        public GXTrigger(string? name)
        {
            Workflows = [];
            Activities = [];
            TriggerGroups = [];
            Schedules = [];
            Properties = [];
            Name = name;
        }

        /// <summary>
        /// Trigger identifier.
        /// </summary>
        [DataMember(Name = "ID")]
        [Description("Trigger identifier.")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>Additional access policies; empty retains existing permissions, otherwise any assigned policy must pass.</summary>
        [Description("Additional access policies; empty retains existing permissions, otherwise any assigned policy must pass.")]
        [DataMember, Ignore(IgnoreType.Db)]
        public List<string>? Policies { get; set; }

        /// <summary>
        /// The creator of the trigger.
        /// </summary>
        [Description("The creator of the trigger.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXUser? Creator { get; set; }

        /// <summary>
        /// Trigger class name.
        /// </summary>
        [Description("Trigger class name.")]
        [StringLength(64)]
        [IsRequired]
        public string? ClassName { get; set; }

        /// <summary>
        /// Trigger name.
        /// </summary>
        [Description("Trigger name.")]
        [StringLength(64)]
        [DataMember]
        [Index(false)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Name { get; set; }

        /// <summary>
        /// Icon name.
        /// </summary>
        [Description("Icon name.")]
        public string? Icon { get; set; }

        /// <summary>
        /// Name of the configuration view class.
        /// </summary>
        [Description("Name of the configuration view class.")]
        [StringLength(128)]
        public string? ConfigurationUI { get; set; }

        /// <summary>
        /// Trigger activities.
        /// </summary>
        [Description("Trigger activities.")]
        [ForeignKey(typeof(GXTriggerActivity))]
        public List<GXTriggerActivity>? Activities { get; set; }

        /// <summary>Whether the trigger is enabled.</summary>
        [Description("Whether the trigger is enabled.")]
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Enabled { get; set; }

        /// <summary>
        /// User that triggers this event.
        /// </summary>
        [Description("User that triggers this event.")]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        public GXUser? User { get; set; }

        /// <summary>
        /// User group that triggers this event.
        /// </summary>
        [Description("User group that triggers this event.")]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        public GXUserGroup? UserGroup { get; set; }

        /// <summary>
        /// Device that triggers this event.
        /// </summary>
        [Description("Device that triggers this event.")]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        public GXDevice? Device { get; set; }

        /// <summary>
        /// DeviceGroup that triggers this event.
        /// </summary>
        [Description("DeviceGroup that triggers this event.")]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        public GXDeviceGroup? DeviceGroup { get; set; }

        /// <summary>
        /// Module that triggers this event.
        /// </summary>
        [Description("Module that triggers this event.")]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        //ForeignKeyDelete is None because creator is causing multiple cascade paths error in MSSQL.
        public GXModule? Module { get; set; }

        /// <summary>
        /// Scheduled triggers.
        /// </summary>
        [Description("Scheduled triggers.")]
        [DataMember]
        [ForeignKey(typeof(GXSchedule), typeof(GXScheduleTrigger))]
        public List<GXSchedule>? Schedules { get; set; }

        /// <summary>
        /// List of workflows that this trigger invokes.
        /// </summary>
        [Description("List of workflows that this trigger invokes.")]
        [Ignore(IgnoreType.Db)]
        public List<GXWorkflow>? Workflows { get; set; }

        /// <summary>Trigger groups containing this trigger.</summary>
        [Description("Trigger groups containing this trigger.")]
        [DataMember,
            ForeignKey(typeof(GXTriggerGroup), typeof(GXTriggerGroupTrigger))]
        public List<GXTriggerGroup>? TriggerGroups { get; set; }

        /// <summary>
        /// Trigger properties.
        /// </summary>
        [Description("Trigger properties.")]
        [DataMember]
        [ForeignKey]
        [Filter(FilterType.Contains)]
        public List<GXTriggerProperty>? Properties { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        [DefaultValue(null)]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>Time when the trigger was last updated.</summary>
        [Description("Time when the trigger was last updated.")]
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
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
        /// <summary>Time when the record was marked as removed.</summary>
        [Description("Time when the record was marked as removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>Time of the most recent execution.</summary>
        [DataMember]
        [Description("Time of the most recent execution.")]
        //Filter uses default value.
        [DefaultValue(null)]
        public DateTimeOffset? ExecutionTime { get; set; }

        /// <summary>
        /// Initializes the creation timestamp before insertion if it has not been set.
        /// </summary>
        [Description("Initializes the creation timestamp before insertion if it has not been set.")]
        public override void BeforeAdd()
        {
            if (CreationTime == null)
            {
                CreationTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Updates the last-modified timestamp before persisting changes.
        /// </summary>
        [Description("Updates the last-modified timestamp before persisting changes.")]
        public override void BeforeUpdate()
        {
            Updated = DateTime.Now;
        }
    }
}
