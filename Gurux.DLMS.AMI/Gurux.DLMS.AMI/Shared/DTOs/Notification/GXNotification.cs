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
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.Content;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Report;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Subtotal;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Notification
{
    /// <summary>
    /// Notification when content is added.
    /// </summary>
    public class GXNotification : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXNotification()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// This constuctor is called when a new notification is created. It will create all needed lists.
        /// </remarks>
        /// <param name="name">Notification name.</param>
        public GXNotification(string? name)
        {
            Active = true;
            Name = name;
            Action = (byte)NotificationAction.Add;
            Targets = null;
            //UI notifications are used as default.
            Delivery = 1;
            NotificationGroups = new List<GXNotificationGroup>();
            Logs = new List<GXNotificationLog>();
            DeviceGroups = new List<GXDeviceGroup>();
            DeviceGroupAttributeTemplates = new List<GXAttributeTemplate>();
            Devices = new List<GXDevice>();
            DeviceAttributeTemplates = new List<GXAttributeTemplate>();
            AgentGroups = new List<GXAgentGroup>();
            Agents = new List<GXAgent>();
            BlockGroups = new List<GXBlockGroup>();
            Blocks = new List<GXBlock>();
            ContentGroups = new List<GXContentGroup>();
            Contents = new List<GXContent>();
            GatewayGroups = new List<GXGatewayGroup>();
            Gateways = new List<GXGateway>();
            KeyManagementGroups = new List<GXKeyManagementGroup>();
            KeyManagements = new List<GXKeyManagement>();
            ModuleGroups = new List<GXModuleGroup>();
            Modules = new List<GXModule>();
            ReportGroups = new List<GXReportGroup>();
            Reports = new List<GXReport>();
            ScheduleGroups = new List<GXScheduleGroup>();
            Schedules = new List<GXSchedule>();
            ScriptGroups = new List<GXScriptGroup>();
            Scripts = new List<GXScript>();
            SubtotalGroups = new List<GXSubtotalGroup>();
            Subtotals = new List<GXSubtotal>();
            TriggerGroups = new List<GXTriggerGroup>();
            Triggers = new List<GXTrigger>();
            UserGroups = new List<GXUserGroup>();
            Users = new List<GXUser>();
            WorkflowGroups = new List<GXWorkflowGroup>();
            Workflows = new List<GXWorkflow>();
        }

        /// <summary>
        /// Notification ID.
        /// </summary>
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Notification Name.
        /// </summary>
        [StringLength(128)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias.
        /// </summary>
        [Ignore]
        public string? UrlAlias
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
        /// Script method that this notification uses.
        /// </summary>
        [DefaultValue(null)]
        [ForeignKey(typeof(GXScriptMethod), OnDelete = ForeignKeyDelete.None)]
        public GXScriptMethod? ScriptMethod
        {
            //ForeignKeyDelete is None because creator of the script is causing multiple cascade paths error in MSSQL.
            get;
            set;
        }

        /// <summary>
        /// Is notification active.
        /// </summary>
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Active { get; set; }

        /// <summary>
        /// Notification actions.
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public UInt16? Action
        {
            get;
            set;
        }

        /// <summary>
        /// Notification target(s).
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Targets
        {
            get;
            set;
        }

        /// <summary>
        /// Delivery of the report.
        /// </summary>
        [DataMember]
        [DefaultValue(ReportDelivery.Caller)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public UInt16? Delivery
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
        /// List of device groups that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXDeviceGroup), typeof(GXNotificationDeviceGroup))]
        public List<GXDeviceGroup>? DeviceGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of devices that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXDevice), typeof(GXNotificationDevice))]
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
            typeof(GXNotificationDeviceAttributeTemplate))]
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
            typeof(GXNotificationDeviceGroupAttributeTemplate))]
        public List<GXAttributeTemplate>? DeviceGroupAttributeTemplates
        {
            get;
            set;
        }

        /// <summary>
        /// List of agent groups that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXAgentGroup), typeof(GXNotificationAgentGroup))]
        public List<GXAgentGroup>? AgentGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of agents that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXAgent), typeof(GXNotificationAgent))]
        public List<GXAgent>? Agents
        {
            get;
            set;
        }

        /// <summary>
        /// List of block groups that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXBlockGroup), typeof(GXNotificationBlockGroup))]
        public List<GXBlockGroup>? BlockGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of blocks that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXBlock), typeof(GXNotificationBlock))]
        public List<GXBlock>? Blocks
        {
            get;
            set;
        }

        /// <summary>
        /// List of content groups that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXContentGroup), typeof(GXNotificationContentGroup))]
        public List<GXContentGroup>? ContentGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of contents that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXContent), typeof(GXNotificationContent))]
        public List<GXContent>? Contents
        {
            get;
            set;
        }

        /// <summary>
        /// List of gateway groups that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXGatewayGroup), typeof(GXNotificationGatewayGroup))]
        public List<GXGatewayGroup>? GatewayGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of gateways that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXGateway), typeof(GXNotificationGateway))]
        public List<GXGateway>? Gateways
        {
            get;
            set;
        }

        /// <summary>
        /// List of key management groups that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXKeyManagementGroup), typeof(GXNotificationKeyManagementGroup))]
        public List<GXKeyManagementGroup>? KeyManagementGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of key managements that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXKeyManagement), typeof(GXNotificationKeyManagement))]
        public List<GXKeyManagement>? KeyManagements
        {
            get;
            set;
        }

        /// <summary>
        /// List of key manufacturers groups that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXManufacturerGroup), typeof(GXNotificationManufacturerGroup))]
        public List<GXManufacturerGroup>? ManufacturerGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of manufacturers that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXManufacturer), typeof(GXNotificationManufacturer))]
        public List<GXManufacturer>? Manufacturers
        {
            get;
            set;
        }

        /// <summary>
        /// List of key device template groups that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXDeviceTemplateGroup), typeof(GXNotificationDeviceTemplateGroup))]
        public List<GXDeviceTemplateGroup>? DeviceTemplateGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of device templates that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXDeviceTemplate), typeof(GXNotificationDeviceTemplate))]
        public List<GXDeviceTemplate>? DeviceTemplates
        {
            get;
            set;
        }

        /// <summary>
        /// List of module groups that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXModuleGroup), typeof(GXNotificationModuleGroup))]
        public List<GXModuleGroup>? ModuleGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of modules that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXModule), typeof(GXNotificationModule))]
        public List<GXModule>? Modules
        {
            get;
            set;
        }

        /// <summary>
        /// List of report groups that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXReportGroup), typeof(GXNotificationReportGroup))]
        public List<GXReportGroup>? ReportGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of reports that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXReport), typeof(GXNotificationReport))]
        public List<GXReport>? Reports
        {
            get;
            set;
        }

        /// <summary>
        /// List of schedule groups that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXScheduleGroup), typeof(GXNotificationScheduleGroup))]
        public List<GXScheduleGroup>? ScheduleGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of schedules that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXSchedule), typeof(GXNotificationSchedule))]
        public List<GXSchedule>? Schedules
        {
            get;
            set;
        }

        /// <summary>
        /// List of script groups that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXScriptGroup), typeof(GXNotificationScriptGroup))]
        public List<GXScriptGroup>? ScriptGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of scripts that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXScript), typeof(GXNotificationScript))]
        public List<GXScript>? Scripts
        {
            get;
            set;
        }

        /// <summary>
        /// List of subtotal groups that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXSubtotalGroup), typeof(GXNotificationSubtotalGroup))]
        public List<GXSubtotalGroup>? SubtotalGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of subtotals that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXSubtotal), typeof(GXNotificationSubtotal))]
        public List<GXSubtotal>? Subtotals
        {
            get;
            set;
        }

        /// <summary>
        /// List of trigger groups that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXTriggerGroup), typeof(GXNotificationTriggerGroup))]
        public List<GXTriggerGroup>? TriggerGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of triggers that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXTrigger), typeof(GXNotificationTrigger))]
        public List<GXTrigger>? Triggers
        {
            get;
            set;
        }


        /// <summary>
        /// List of user groups that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXUserGroup), typeof(GXNotificationUserGroup))]
        public List<GXUserGroup>? UserGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of users that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXUser), typeof(GXNotificationUser))]
        public List<GXUser>? Users
        {
            get;
            set;
        }

        /// <summary>
        /// List of workflow groups that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXWorkflowGroup), typeof(GXNotificationWorkflowGroup))]
        public List<GXWorkflowGroup>? WorkflowGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of workflows that this notification can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXWorkflow), typeof(GXNotificationWorkflow))]
        public List<GXWorkflow>? Workflows
        {
            get;
            set;
        }

        /// <summary>
        /// List of notification groups where this notification belongs.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXNotificationGroup), typeof(GXNotificationGroupNotification))]
        [Filter(FilterType.Contains)]
        public List<GXNotificationGroup>? NotificationGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// The creator of the notification.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GXUser? Creator
        {
            get;
            set;
        }

        /// <summary>
        /// When the notification is sent for the last time.
        /// </summary>
        [Description("When the notification is detected for the last time.")]
        [Filter(FilterType.GreaterOrEqual)]
        [DefaultValue(null)]
        public DateTimeOffset? Detected
        {
            get;
            set;
        }

        /// <summary>
        /// Notification minimum develivery time in seconds.
        /// </summary>
        [DataMember]
        public int MinimumDeliveryTime
        {
            get;
            set;
        }

        /// <summary>
        /// Time when notification was removed.
        /// </summary>
        /// <remarks>
        /// In filter if the removed time is set it will return values that are not null.
        /// </remarks>
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
        /// When the notification is updated for the last time.
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
        /// Notification logs.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXNotificationLog))]
        [Filter(FilterType.Contains)]
        public List<GXNotificationLog>? Logs
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
        /// Update creation time before update.
        /// </summary>
        public override void BeforeAdd()
        {
            if (CreationTime == null)
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
            string? str = Name;
            if (Active.HasValue && Active.Value)
            {
                str += ", Active";
            }
            if (string.IsNullOrEmpty(str))
            {
                str = nameof(GXNotification);
            }
            return str;
        }
    }
}
