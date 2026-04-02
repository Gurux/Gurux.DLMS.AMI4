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
using Gurux.DLMS.AMI.Shared.DTOs.ContentType;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using Gurux.DLMS.AMI.Shared.DTOs.Report;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Subtotal;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.UrlAlias
{
    /// <summary>
    /// Url alias.
    /// </summary>
    public class GXUrlAlias : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Url alias ID.
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
        /// Url alias.
        /// </summary>
        [StringLength(128)]
        [Index(true)]
        [Filter(FilterType.Contains)]
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the device group.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXDeviceGroup), typeof(GXUrlAliasDeviceGroup))]
        public GXDeviceGroup? DeviceGroup
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the device.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXDevice), typeof(GXUrlAliasDevice))]
        public GXDevice? Device
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the agent group.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXAgentGroup), typeof(GXUrlAliasAgentGroup))]
        public GXAgentGroup? AgentGroup
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the agent.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXAgent), typeof(GXUrlAliasAgent))]
        public GXAgent? Agent
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the block group.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXBlockGroup), typeof(GXUrlAliasBlockGroup))]
        public GXBlockGroup? BlockGroup
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the block.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXBlock), typeof(GXUrlAliasBlock))]
        public GXBlock? Block
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the content group type.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXContentTypeGroup), typeof(GXUrlAliasContentTypeGroup))]
        public GXContentTypeGroup? ContentTypeGroup
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the content type.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXContentType), typeof(GXUrlAliasContentType))]
        public GXContentType? ContentType
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the content group.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXContentGroup), typeof(GXUrlAliasContentGroup))]
        public GXContentGroup? ContentGroup
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the content.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXContent), typeof(GXUrlAliasContent))]
        public GXContent? Content
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the gateway group.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXGatewayGroup), typeof(GXUrlAliasGatewayGroup))]
        public GXGatewayGroup? GatewayGroup
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the gateway.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXGateway), typeof(GXUrlAliasGateway))]
        public GXGateway? Gateway
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the key management group.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXKeyManagementGroup), typeof(GXUrlAliasKeyManagementGroup))]
        public GXKeyManagementGroup? KeyManagementGroup
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the key management.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXKeyManagement), typeof(GXUrlAliasKeyManagement))]
        public GXKeyManagement? KeyManagement
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the key manufacturers group.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXManufacturerGroup), typeof(GXUrlAliasManufacturerGroup))]
        public GXManufacturerGroup? ManufacturerGroup
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the manufacturer.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXManufacturer), typeof(GXUrlAliasManufacturer))]
        public GXManufacturer? Manufacturer
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the key device template group.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXDeviceTemplateGroup), typeof(GXUrlAliasDeviceTemplateGroup))]
        public GXDeviceTemplateGroup? DeviceTemplateGroup
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the device template.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXDeviceTemplate), typeof(GXUrlAliasDeviceTemplate))]
        public GXDeviceTemplate? DeviceTemplate
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the module group.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXModuleGroup), typeof(GXUrlAliasModuleGroup))]
        public GXModuleGroup? ModuleGroup
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the module.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXModule), typeof(GXUrlAliasModule))]
        public GXModule? Module
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the report group.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXReportGroup), typeof(GXUrlAliasReportGroup))]
        public GXReportGroup? ReportGroup
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the report.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXReport), typeof(GXUrlAliasReport))]
        public GXReport? Report
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the schedule group.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXScheduleGroup), typeof(GXUrlAliasScheduleGroup))]
        public GXScheduleGroup? ScheduleGroup
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the schedule.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXSchedule), typeof(GXUrlAliasSchedule))]
        public GXSchedule? Schedule
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the script group.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXScriptGroup), typeof(GXUrlAliasScriptGroup))]
        public GXScriptGroup? ScriptGroup
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the script.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXScript), typeof(GXUrlAliasScript))]
        public GXScript? Script
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the subtotal group.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXSubtotalGroup), typeof(GXUrlAliasSubtotalGroup))]
        public GXSubtotalGroup? SubtotalGroup
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the subtotal.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXSubtotal), typeof(GXUrlAliasSubtotal))]
        public GXSubtotal? Subtotal
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the trigger group.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXTriggerGroup), typeof(GXUrlAliasTriggerGroup))]
        public GXTriggerGroup? TriggerGroup
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the trigger.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXTrigger), typeof(GXUrlAliasTrigger))]
        public GXTrigger? Trigger
        {
            get;
            set;
        }


        /// <summary>
        /// Url alias for the user group.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXUserGroup), typeof(GXUrlAliasUserGroup))]
        public GXUserGroup? UserGroup
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the user.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXUser), typeof(GXUrlAliasUser))]
        public GXUser? User
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the workflow group.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXWorkflowGroup), typeof(GXUrlAliasWorkflowGroup))]
        public GXWorkflowGroup? WorkflowGroup
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the workflow.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXWorkflow), typeof(GXUrlAliasWorkflow))]
        public GXWorkflow? Workflow
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the notification group.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXNotificationGroup), typeof(GXUrlAliasNotificationGroup))]
        [Filter(FilterType.Contains)]
        public GXNotificationGroup? NotificationGroup
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias for the notification.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXNotification), typeof(GXUrlAliasNotification))]
        [Filter(FilterType.Contains)]
        public GXNotification? Notification
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
        /// When the block is updated for the last time.
        /// </summary>
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated
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
            string? str = null;
            if (!string.IsNullOrEmpty(Name))
            {
                str = Name;
            }
            if (!string.IsNullOrEmpty(str))
            {
                return str;
            }
            return typeof(GXFavorite).Name;
        }
    }
}
