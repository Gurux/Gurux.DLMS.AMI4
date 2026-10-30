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
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Schedule
{
    /// <summary>A schedule defining when operations run and which resources they target.</summary>
    [Description("A schedule defining when operations run and which resources they target.")]
    public class GXSchedule : GXTableBase, IUnique<Guid>
    {
        /// <summary>Initializes a new instance of GXSchedule.</summary>
        [Description("Initializes a new instance of GXSchedule.")]
        public GXSchedule()
        {
        }

        /// <summary>Initializes a new instance of GXSchedule.</summary>
        /// <remarks>
        /// This constructor is called when a new schedule is created. It will create all required collections.
        /// </remarks>
        /// <param name="name">Schedule name.</param>
        [Description("Initializes a new instance of GXSchedule.")]
        public GXSchedule(string? name)
        {
            Name = name;
            Enabled = true;
            Devices = [];
            ScriptMethods = [];
            DeviceGroups = [];
            ScheduleGroups = [];
            Triggers = [];
            Logs = [];
            Modules = [];
            DeviceObjectTemplates = [];
            DeviceAttributeTemplates = [];
            DeviceGroupObjectTemplates = [];
            DeviceGroupAttributeTemplates = [];
            TraceLevel = System.Diagnostics.TraceLevel.Error;
        }

        /// <summary>Unique identifier of the schedule.</summary>
        [Description("Unique identifier of the schedule.")]
        [DataMember]
        //Filter uses default value.
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// The creator of the schedule.
        /// </summary>
        [Description("The creator of the schedule.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXUser? Creator { get; set; }


        /// <summary>
        /// Gets or sets who can view the schedule.
        /// </summary>
        /// <remarks>
        /// Modification, deletion and operation permissions are evaluated separately.
        /// </remarks>
        [Description("Gets or sets who can view the schedule.")]
        [DataMember]
        [DefaultValue(ResourceVisibility.Inherit)]
        [IsRequired]
        public ResourceVisibility? AccessVisibility { get; set; }

        /// <summary>Whether the schedule is enabled.</summary>
        [Description("Whether the schedule is enabled.")]
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Enabled { get; set; }

        /// <summary>
        /// Scheduled devices.
        /// </summary>
        [Description("Scheduled devices.")]
        [DataMember]
        [ForeignKey(typeof(GXDevice), typeof(GXScheduleToDevice))]
        public List<GXDevice>? Devices { get; set; }

        /// <summary>
        /// Scheduled device groups.
        /// </summary>
        [Description("Scheduled device groups.")]
        [DataMember]
        [ForeignKey(typeof(GXDeviceGroup), typeof(GXScheduleToDeviceGroup))]
        public List<GXDeviceGroup>? DeviceGroups { get; set; }

        /// <summary>
        /// Scheduled device attribute templates.
        /// </summary>
        /// <remarks>
        /// Devices use attribute templates to schedule attribute reads.
        /// </remarks>
        /// <seealso cref="Devices"/>
        /// <seealso cref="DeviceGroupAttributeTemplates"/>
        [Description("Scheduled device attribute templates.")]
        [DataMember]
        [ForeignKey(typeof(GXAttributeTemplate), typeof(GXScheduleToDeviceAttributeTemplate))]
        public List<GXAttributeTemplate>? DeviceAttributeTemplates { get; set; }

        /// <summary>
        /// Scheduled device objects.
        /// </summary>
        /// <remarks>
        /// Devices use object templates to schedule object reads.
        /// </remarks>
        /// <seealso cref="Devices"/>
        /// <seealso cref="DeviceGroupAttributeTemplates"/>
        [Description("Scheduled device objects.")]
        [DataMember]
        [ForeignKey(typeof(GXObjectTemplate), typeof(GXScheduleToDeviceObjectTemplate))]
        public List<GXObjectTemplate>? DeviceObjectTemplates { get; set; }

        /// <summary>
        /// Scheduled device group attribute templates.
        /// </summary>
        /// <remarks>
        /// Device groups use attribute templates to schedule attribute reads.
        /// </remarks>
        /// <seealso cref="DeviceAttributeTemplates"/>
        /// <seealso cref="DeviceGroups"/>
        [Description("Scheduled device group attribute templates.")]
        [DataMember]
        [ForeignKey(typeof(GXAttributeTemplate), typeof(GXScheduleToDeviceGroupAttributeTemplate))]
        public List<GXAttributeTemplate>? DeviceGroupAttributeTemplates { get; set; }

        /// <summary>
        /// Scheduled device group objects.
        /// </summary>
        /// <remarks>
        /// Device groups use object templates to schedule object reads.
        /// </remarks>
        /// <seealso cref="DeviceAttributeTemplates"/>
        /// <seealso cref="DeviceGroups"/>
        [Description("Scheduled device group objects.")]
        [DataMember]
        [ForeignKey(typeof(GXObjectTemplate), typeof(GXScheduleToDeviceGroupObjectTemplate))]
        public List<GXObjectTemplate>? DeviceGroupObjectTemplates { get; set; }

        /// <summary>
        /// Scheduled script methods.
        /// </summary>
        [Description("Scheduled script methods.")]
        [DataMember]
        [ForeignKey(typeof(GXScriptMethod), typeof(GXScheduleScript))]
        public List<GXScriptMethod>? ScriptMethods { get; set; }

        /// <summary>
        /// Scheduled modules.
        /// </summary>
        [Description("Scheduled modules.")]
        [DataMember]
        [ForeignKey(typeof(GXModule), typeof(GXScheduleModule))]
        public List<GXModule>? Modules { get; set; }

        /// <summary>
        /// Scheduled triggers.
        /// </summary>
        [Description("Scheduled triggers.")]
        [DataMember]
        [ForeignKey(typeof(GXTrigger), typeof(GXScheduleTrigger))]
        public List<GXTrigger>? Triggers { get; set; }

        /// <summary>Schedule groups containing this schedule.</summary>
        [Description("Schedule groups containing this schedule.")]
        [DataMember,
            ForeignKey(typeof(GXScheduleGroup), typeof(GXScheduleGroupSchedule))]
        public List<GXScheduleGroup>? ScheduleGroups { get; set; }

        /// <summary>
        /// Schedule logs.
        /// </summary>
        [Description("Schedule logs.")]
        [DataMember, Ignore(IgnoreType.Db)]
        public List<GXLog>? Logs { get; set; }

        /// <summary>
        /// Schedule name.
        /// </summary>
        [Description("Schedule name.")]
        [DataMember]
        [StringLength(64)]
        //Filter uses default value.
        [DefaultValue(null)]
        [Index(Unique = false)]
        [Filter(FilterType.Contains)]
        public string? Name { get; set; }

        /// <summary>Description of the schedule.</summary>
        [DataMember]
        [StringLength(256)]
        [Description("Description of the schedule.")]
        //Filter uses default value.
        [DefaultValue(null)]
        public string? Description { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        //Filter uses default value.
        [DefaultValue(null)]
        [Index(false, Descend = true)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>Time when the schedule was last updated.</summary>
        [Description("Time when the schedule was last updated.")]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }


        /// <summary>Trace level used for communication diagnostics.</summary>
        [DataMember]
        [DefaultValue(System.Diagnostics.TraceLevel.Error)]
        [Description("Trace level used for communication diagnostics.")]
        [IsRequired]
        public TraceLevel? TraceLevel { get; set; }

        /// <summary>
        /// Schedule targets are handled concurrently.
        /// </summary>
        [Description("Schedule targets are handled concurrently.")]
        [DefaultValue(false)]
        [IsRequired]
        public bool? Concurrently { get; set; }

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
        /// Schedule start time.
        /// </summary>
        /// <seealso cref="ExecutionTime"/>
        /// <seealso cref="Next"/>
        [Description("Schedule start time.")]
        [DataMember]
        [StringLength(36)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Start { get; set; }

        /// <summary>Time of the most recent execution.</summary>
        /// <seealso cref="Start"/>
        /// <seealso cref="Next"/>
        [Description("Time of the most recent execution.")]
        [DataMember]
        //Filter uses default value.
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? ExecutionTime { get; set; }

        /// <summary>
        /// Next schedule execution time.
        /// </summary>
        /// <seealso cref="Start"/>
        /// <seealso cref="ExecutionTime"/>
        [Description("Next schedule execution time.")]
        [Filter(FilterType.GreaterOrEqual)]
        [DefaultValue(null)]
        public DateTimeOffset? Next { get; set; }

        /// <summary>Time when the record was marked as removed.</summary>
        //Filter uses default value.
        [Description("Time when the record was marked as removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

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
