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
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Subtotal
{
    /// <summary>
    /// Sub total value is used to save sum value 
    /// for given attribute.
    /// </summary>
    /// <remarks>This can be used to store the sum, or avarage value 
    /// of a specific attribute from device or device groups.
    /// /// </remarks>
    public class GXSubtotal : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXSubtotal()
        {
        }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// This constuctor is called when a new subtotal is created. It will create all needed lists.
        /// </remarks>
        /// <param name="name">subtotal name.</param>
        public GXSubtotal(string? name)
        {
            Interval = 60;
            Operation = 0;
            Type = 0;
            Active = true;
            Name = name;
            Total = false;
            Fill = true;
            Agents = new List<GXAgent>();
            AgentGroups = new List<GXAgentGroup>();
            Gateways = new List<GXGateway>();
            GatewayGroups = new List<GXGatewayGroup>();
            SubtotalGroups = new List<GXSubtotalGroup>();
            Logs = new List<GXSubtotalLog>();
            Devices = new List<GXDevice>();
            DeviceGroups = new List<GXDeviceGroup>();
            DeviceAttributeTemplates = new List<GXAttributeTemplate>();
            DeviceGroupAttributeTemplates = new List<GXAttributeTemplate>();
            Schedules = new List<GXSchedule>();
            ScheduleGroups = new List<GXScheduleGroup>();
            Scripts = new List<GXScript>();
            ScriptGroups = new List<GXScriptGroup>();
            Workflows = new List<GXWorkflow>();
            WorkflowGroups = new List<GXWorkflowGroup>();
            Users = new List<GXUser>();
            UserGroups = new List<GXUserGroup>();
            Values = new List<GXSubtotalValue>();
            TraceLevel = System.Diagnostics.TraceLevel.Error;
        }

        /// <summary>
        /// Subtotal identifier.
        /// </summary>
        [Description("Subtotal identifier.")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// The name of the subtotal.
        /// </summary>
        [Description("The name of the subtotal.")]
        [StringLength(64, ErrorMessage = "Name is too long.")]
        [Index(false)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// Description.
        /// </summary>
        [DataMember]
        [StringLength(256)]
        [Description("Description.")]
        //Filter uses default value.
        [DefaultValue(null)]
        public string? Description
        {
            get;
            set;
        }

        /// <summary>
        /// Is subtotal active.
        /// </summary>
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Active { get; set; }

        /// <summary>
        /// Subtotal Status.
        /// </summary>
        /// <remarks>
        /// Counting subtotals might take a long time.
        /// This will help to see the current status.
        /// </remarks>
        [DataMember]
        [DefaultValue(SubtotalStatus.Idle)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public SubtotalStatus? Status
        {
            get;
            set;
        }

        /// <summary>
        /// All target values are counted to total.
        /// </summary>
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Total { get; set; }

        /// <summary>
        /// Empty values are fill with zero value.
        /// </summary>
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Fill { get; set; }

        /// <summary>
        /// The creator of the subtotal.
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
        /// Agents for which the subtotal is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXAgent),
            typeof(GXSubtotalAgent))]
        public List<GXAgent>? Agents
        {
            get;
            set;
        }

        /// <summary>
        /// Agent groups for which the subtotal is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXAgentGroup),
            typeof(GXSubtotalAgentGroup))]
        [Filter(FilterType.Contains)]
        public List<GXAgentGroup>? AgentGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Agents for which the subtotal is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXGateway),
            typeof(GXSubtotalGateway))]
        public List<GXGateway>? Gateways
        {
            get;
            set;
        }

        /// <summary>
        /// Gateway groups for which the subtotal is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXGatewayGroup),
            typeof(GXSubtotalGatewayGroup))]
        [Filter(FilterType.Contains)]
        public List<GXGatewayGroup>? GatewayGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Device groups for which the subtotal is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXDeviceGroup),
            typeof(GXSubtotalDeviceGroup))]
        [Filter(FilterType.Contains)]
        public List<GXDeviceGroup>? DeviceGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Devices for which the subtotal is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXDevice),
            typeof(GXSubtotalDevice))]
        [Filter(FilterType.Contains)]
        public List<GXDevice>? Devices
        {
            get;
            set;
        }

        /// <summary>
        /// Attribute templates for which the subtotal is calculated for the devices.
        /// </summary>
        /// <remarks>
        /// Devices uses attribute templates to count sub total.
        /// </remarks>
        /// <seealso cref="Devices"/>
        /// <seealso cref="DeviceGroupAttributeTemplates"/>
        [DataMember]
        [ForeignKey(typeof(GXAttributeTemplate),
            typeof(GXSubtotalDeviceAttributeTemplate))]
        public List<GXAttributeTemplate>? DeviceAttributeTemplates
        {
            get;
            set;
        }

        /// <summary>
        /// Attribute templates for which the subtotal is calculated for the device groups.
        /// </summary>
        /// <remarks>
        /// Device groups uses attribute templates to count sub total.
        /// </remarks>
        /// <seealso cref="DeviceAttributeTemplates"/>
        /// <seealso cref="DeviceGroups"/>
        [DataMember]
        [ForeignKey(typeof(GXAttributeTemplate),
            typeof(GXSubtotalDeviceGroupAttributeTemplate))]
        public List<GXAttributeTemplate>? DeviceGroupAttributeTemplates
        {
            get;
            set;
        }


        /// <summary>
        /// List of subtotal groups where this subtotal belongs.
        /// </summary>
        [DataMember,
        ForeignKey(typeof(GXSubtotalGroup),
            typeof(GXSubtotalGroupSubtotal))]
        [Filter(FilterType.Contains)]
        public List<GXSubtotalGroup>? SubtotalGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Schedules for which the subtotal is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXSchedule),
            typeof(GXSubtotalSchedule))]
        public List<GXSchedule>? Schedules
        {
            get;
            set;
        }

        /// <summary>
        /// Schedule groups for which the subtotal is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXScheduleGroup),
            typeof(GXSubtotalScheduleGroup))]
        public List<GXScheduleGroup>? ScheduleGroups
        {
            get;
            set;
        }
        /// <summary>
        /// Scripts for which the subtotal is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXScript),
            typeof(GXSubtotalScript))]
        public List<GXScript>? Scripts
        {
            get;
            set;
        }

        /// <summary>
        /// Script group for which the subtotal is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXScriptGroup),
            typeof(GXSubtotalScriptGroup))]
        public List<GXScriptGroup>? ScriptGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Workflows for which the subtotal is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXWorkflow),
            typeof(GXSubtotalWorkflow))]
        public List<GXWorkflow>? Workflows
        {
            get;
            set;
        }

        /// <summary>
        /// Workflow groups for which the subtotal is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXWorkflowGroup),
            typeof(GXSubtotalWorkflowGroup))]
        public List<GXWorkflowGroup>? WorkflowGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Users for which the subtotal is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXUser),
            typeof(GXSubtotalUser))]
        public List<GXUser>? Users
        {
            get;
            set;
        }

        /// <summary>
        /// User groups for which the subtotal is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXUserGroup),
            typeof(GXSubtotalUserGroup))]
        public List<GXUserGroup>? UserGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Subtotal logs.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXSubtotalLog))]
        [Filter(FilterType.Contains)]
        public List<GXSubtotalLog>? Logs
        {
            get;
            set;
        }

        /// <summary>
        /// Subtotal values.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXSubtotalValue))]
        [Filter(FilterType.Contains)]
        public List<GXSubtotalValue>? Values
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// </summary>
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTime? CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// When was the subtotal last updated.
        /// </summary>
        [Description("When was the subtotal last updated.")]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated
        {
            get;
            set;
        }

        /// <summary>
        /// When was the subtotal calculated for the last time.
        /// </summary>
        [Description("When was the subtotal calculated for the last time..")]
        [Filter(FilterType.GreaterOrEqual)]
        [DefaultValue(null)]
        public DateTimeOffset? Calculated
        {
            get;
            set;
        }

        /// <summary>
        /// Subtotal type.
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public byte? Type
        {
            get;
            set;
        }

        /// <summary>
        /// Subtotal operation.
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public byte? Operation
        {
            get;
            set;
        }

        /// <summary>
        /// Subtotal interval in seconds.
        /// </summary>
        [DataMember]
        public int Interval
        {
            get;
            set;
        }

        /// <summary>
        /// User has modified the subtotal.
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
        /// Used trace level.
        /// </summary>
        [DataMember]
        [DefaultValue(System.Diagnostics.TraceLevel.Error)]
        [Description("Used trace level.")]
        [IsRequired]
        public TraceLevel? TraceLevel
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
            string str;
            if (!string.IsNullOrEmpty(Name))
            {
                str = Name;
                if (Calculated != null)
                {
                    str += Environment.NewLine + " Calculated: " + Calculated;
                }
            }
            else
            {
                str = nameof(GXSubtotal);
            }
            return str;
        }
    }
}
