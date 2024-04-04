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
using Gurux.DLMS.AMI.Shared.DTOs.Subtotal;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Report
{
    /// <summary>
    /// Report to generate report from the selected values and send it to the receivers.
    /// </summary>
    public class GXReport : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXReport()
        {
        }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// This constuctor is called when a new report is created. It will create all needed lists.
        /// </remarks>
        /// <param name="name">report name.</param>
        public GXReport(string? name)
        {
            Interval = 60;
            Operation = 0;
            Type = 0;
            Active = true;
            Name = name;
            Total = false;
            Fill = false;
            Agents = new List<GXAgent>();
            AgentGroups = new List<GXAgentGroup>();
            Gateways = new List<GXGateway>();
            GatewayGroups = new List<GXGatewayGroup>();
            ReportGroups = new List<GXReportGroup>();
            Logs = new List<GXReportLog>();
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
            SubtotalGroups = new List<GXSubtotalGroup>();
            Subtotals = new List<GXSubtotal>();
            TraceLevel = System.Diagnostics.TraceLevel.Error;
        }

        /// <summary>
        /// Report identifier.
        /// </summary>
        [Description("Report identifier.")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// The creator of the report.
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
        /// The name of the report.
        /// </summary>
        [Description("The name of the report.")]
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
        /// Is report active.
        /// </summary>
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Active { get; set; }

        /// <summary>
        /// Report Status.
        /// </summary>
        /// <remarks>
        /// Counting reports might take a long time.
        /// This will help to see the current status.
        /// </remarks>
        [DataMember]
        [DefaultValue(ReportStatus.Idle)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public ReportStatus? Status
        {
            get;
            set;
        }

        /// <summary>
        /// Delivery of the report.
        /// </summary>
        [DataMember]
        [DefaultValue(ReportDelivery.Caller)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public ReportDelivery? Delivery
        {
            get;
            set;
        }

        /// <summary>
        /// Contains a list of email address, TCP/IP address, etc where report is send.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? Destination
        {
            get;
            set;
        }

        /// <summary>
        /// Result type is reserved for future use.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        public byte? ResultType
        {
            get;
            set;
        }

        /// <summary>
        /// Additional settings.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        public string? Settings
        {
            get;
            set;
        }

        /// <summary>
        /// Script method that this report uses.
        /// </summary>
        [DefaultValue(null)]
        [ForeignKey(typeof(GXScriptMethod), OnDelete = ForeignKeyDelete.Cascade)]
        public GXScriptMethod? ScriptMethod
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
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Fill { get; set; }

        /// <summary>
        /// Agents for which the report is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXAgent),
            typeof(GXReportAgent))]
        public List<GXAgent>? Agents
        {
            get;
            set;
        }

        /// <summary>
        /// Agent groups for which the report is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXAgentGroup),
            typeof(GXReportAgentGroup))]
        [Filter(FilterType.Contains)]
        public List<GXAgentGroup>? AgentGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Gateways for which the report is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXGateway),
            typeof(GXReportGateway))]
        public List<GXGateway>? Gateways
        {
            get;
            set;
        }

        /// <summary>
        /// Gateway groups for which the report is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXGatewayGroup),
            typeof(GXReportGatewayGroup))]
        [Filter(FilterType.Contains)]
        public List<GXGatewayGroup>? GatewayGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Device groups for which the report is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXDeviceGroup),
            typeof(GXReportDeviceGroup))]
        [Filter(FilterType.Contains)]
        public List<GXDeviceGroup>? DeviceGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Devices for which the report is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXDevice),
            typeof(GXReportDevice))]
        [Filter(FilterType.Contains)]
        public List<GXDevice>? Devices
        {
            get;
            set;
        }

        /// <summary>
        /// Attribute templates for which the report is calculated for the devices.
        /// </summary>
        /// <remarks>
        /// Devices uses attribute templates to count report.
        /// </remarks>
        /// <seealso cref="Devices"/>
        /// <seealso cref="DeviceGroupAttributeTemplates"/>
        [DataMember]
        [ForeignKey(typeof(GXAttributeTemplate),
            typeof(GXReportDeviceAttributeTemplate))]
        public List<GXAttributeTemplate>? DeviceAttributeTemplates
        {
            get;
            set;
        }

        /// <summary>
        /// Attribute templates for which the report is calculated for the device groups.
        /// </summary>
        /// <remarks>
        /// Device groups uses attribute templates to count report.
        /// </remarks>
        /// <seealso cref="DeviceAttributeTemplates"/>
        /// <seealso cref="DeviceGroups"/>
        [DataMember]
        [ForeignKey(typeof(GXAttributeTemplate),
            typeof(GXReportDeviceGroupAttributeTemplate))]
        public List<GXAttributeTemplate>? DeviceGroupAttributeTemplates
        {
            get;
            set;
        }


        /// <summary>
        /// List of report groups where this report belongs.
        /// </summary>
        [DataMember,
        ForeignKey(typeof(GXReportGroup),
            typeof(GXReportGroupReport))]
        [Filter(FilterType.Contains)]
        public List<GXReportGroup>? ReportGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Schedules for which the report is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXSchedule),
            typeof(GXReportSchedule))]
        public List<GXSchedule>? Schedules
        {
            get;
            set;
        }

        /// <summary>
        /// Schedule groups for which the report is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXScheduleGroup),
            typeof(GXReportScheduleGroup))]
        public List<GXScheduleGroup>? ScheduleGroups
        {
            get;
            set;
        }
        /// <summary>
        /// Scripts for which the report is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXScript),
            typeof(GXReportScript))]
        public List<GXScript>? Scripts
        {
            get;
            set;
        }

        /// <summary>
        /// Script group for which the report is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXScriptGroup),
            typeof(GXReportScriptGroup))]
        public List<GXScriptGroup>? ScriptGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Workflows for which the report is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXWorkflow),
            typeof(GXReportWorkflow))]
        public List<GXWorkflow>? Workflows
        {
            get;
            set;
        }

        /// <summary>
        /// Workflow groups for which the report is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXWorkflowGroup),
            typeof(GXReportWorkflowGroup))]
        public List<GXWorkflowGroup>? WorkflowGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Subtotals for which the report is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXSubtotal),
            typeof(GXReportSubtotal))]
        public List<GXSubtotal>? Subtotals
        {
            get;
            set;
        }

        /// <summary>
        /// Subtotal groups for which the report is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXSubtotalGroup),
            typeof(GXReportSubtotalGroup))]
        public List<GXSubtotalGroup>? SubtotalGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Users for which the report is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXUser),
            typeof(GXReportUser))]
        public List<GXUser>? Users
        {
            get;
            set;
        }

        /// <summary>
        /// User groups for which the report is calculated.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXUserGroup),
            typeof(GXReportUserGroup))]
        public List<GXUserGroup>? UserGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Report logs.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXReportLog))]
        [Filter(FilterType.Contains)]
        public List<GXReportLog>? Logs
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
        /// When was the report last updated.
        /// </summary>
        [Description("When was the report last updated.")]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated
        {
            get;
            set;
        }

        /// <summary>
        /// Last report delivery time.
        /// </summary>
        [Description("Last report delivery time.")]
        [Filter(FilterType.GreaterOrEqual)]
        [DefaultValue(null)]
        public DateTimeOffset? Last
        {
            get;
            set;
        }

        /// <summary>
        /// Next report delivery time.
        /// </summary>
        /// <remarks>
        /// The next report delivery time when interval is set.
        /// </remarks>
        [Description("Next report delivery time.")]
        [Filter(FilterType.GreaterOrEqual)]
        [DefaultValue(null)]
        public DateTimeOffset? Next
        {
            get;
            set;
        }

        /// <summary>
        /// Report type.
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
        /// Report operation.
        /// </summary>
        /// <seealso cref="Delta"/>
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
        /// Is operator delta value counted.
        /// </summary>
        /// <seealso cref="Operation"/>
        [DataMember]
        [DefaultValue(false)]
        public bool Delta
        {
            get;
            set;
        }

        /// <summary>
        /// Report develivery interval in seconds.
        /// </summary>
        [DataMember]
        public int Interval
        {
            get;
            set;
        }

        /// <summary>
        /// User has modified the report.
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
        /// For how long time is the information retrieved for in the report.
        /// </summary>
        /// <remarks>
        /// This can be used to generate report e.g. from the latest week.
        /// If the From property is set it will be used instead of the Range property.
        /// </remarks>
        /// <seealso cref="Count"/>
        /// <seealso cref="From"/>
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public TimeSpan? Range
        {
            get;
            set;
        }

        /// <summary>
        /// How many values are included to the report.
        /// </summary>
        /// <remarks>
        /// The number of report lines is not limited if the number is zero.
        /// If the value is one, only the latest value is retrieved for the report.
        /// </remarks>
        /// <seealso cref="Range"/>
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public int? Count
        {
            get;
            set;
        }

        /// <summary>
        /// Values are retrieved from the time range that From and/or To defines.
        /// </summary>
        /// <remarks>
        /// This value is not saved for the database. The client can fill it when needed.
        /// If the From property is set it will be used instead of the Range property.
        /// </remarks>
        /// <seealso cref="Range"/>
        /// <seealso cref="To"/>
        [DataMember]
        [DefaultValue(null)]
        [Ignore]
        public DateTimeOffset? From
        {
            get;
            set;
        }

        /// <summary>
        /// Values are retrieved from the time range that From and/or To defines.
        /// </summary>
        /// <remarks>
        /// This value is not saved for the database. The client can fill it when needed.
        /// </remarks>
        /// <seealso cref="From"/>
        [DataMember]
        [DefaultValue(null)]
        [Ignore]
        public DateTimeOffset? To
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
                if (Last != null)
                {
                    str += Environment.NewLine + " Delivered: " + Last;
                }
                if (Next != null)
                {
                    str += Environment.NewLine + " Deliverering: " + Next;
                }
            }
            else
            {
                str = nameof(GXReport);
            }
            return str;
        }
    }
}
