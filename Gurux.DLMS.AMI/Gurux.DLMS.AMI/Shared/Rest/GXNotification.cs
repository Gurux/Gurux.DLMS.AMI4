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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.Content;
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
using Gurux.Service.Orm.Common;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get notification.
    /// </summary>
    public class GetNotificationResponse
    {
        /// <summary>
        /// Device information.
        /// </summary>        
        [ExcludeOpenApi(typeof(GXNotification), nameof(GXNotification.Logs))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXNotificationGroup), nameof(GXNotificationGroup.Id), nameof(GXNotificationGroup.Name))]
        [IncludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Id), nameof(GXLanguage.Resources))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id), nameof(GXUserGroup.Name))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [IncludeOpenApi(typeof(GXNotificationGroup), nameof(GXNotificationGroup.Id))]
        [ExcludeOpenApi(typeof(GXLocalizedResource), nameof(GXLocalizedResource.Language))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id), nameof(GXDevice.Name))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id), nameof(GXDeviceGroup.Name))]
        [IncludeOpenApi(typeof(GXAttributeTemplate), nameof(GXAttributeTemplate.Id), nameof(GXAttributeTemplate.Name))]
        [IncludeOpenApi(typeof(GXObjectTemplate), nameof(GXObjectTemplate.Id), nameof(GXObjectTemplate.Name))]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id), nameof(GXAgent.Name))]
        [IncludeOpenApi(typeof(GXAgentGroup), nameof(GXAgentGroup.Id), nameof(GXAgentGroup.Name))]
        [IncludeOpenApi(typeof(GXBlock), nameof(GXBlock.Id), nameof(GXBlock.Name))]
        [IncludeOpenApi(typeof(GXBlockGroup), nameof(GXBlockGroup.Id), nameof(GXBlockGroup.Name))]
        [IncludeOpenApi(typeof(GXContent), nameof(GXContent.Id), nameof(GXContent.Name))]
        [IncludeOpenApi(typeof(GXContentGroup), nameof(GXContentGroup.Id), nameof(GXContentGroup.Name))]
        [IncludeOpenApi(typeof(GXGateway), nameof(GXGateway.Id), nameof(GXGateway.Name))]
        [IncludeOpenApi(typeof(GXGatewayGroup), nameof(GXGatewayGroup.Id), nameof(GXGatewayGroup.Name))]
        [IncludeOpenApi(typeof(GXKeyManagement), nameof(GXKeyManagement.Id), nameof(GXKeyManagement.Name))]
        [IncludeOpenApi(typeof(GXKeyManagementGroup), nameof(GXKeyManagementGroup.Id), nameof(GXKeyManagementGroup.Name))]
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id), nameof(GXModule.Name))]
        [IncludeOpenApi(typeof(GXModuleGroup), nameof(GXModuleGroup.Id), nameof(GXModuleGroup.Name))]
        [IncludeOpenApi(typeof(GXReport), nameof(GXReport.Id), nameof(GXReport.Name))]
        [IncludeOpenApi(typeof(GXReportGroup), nameof(GXReportGroup.Id), nameof(GXReportGroup.Name))]
        [IncludeOpenApi(typeof(GXSchedule), nameof(GXSchedule.Id), nameof(GXSchedule.Name))]
        [IncludeOpenApi(typeof(GXScheduleGroup), nameof(GXScheduleGroup.Id), nameof(GXScheduleGroup.Name))]
        [IncludeOpenApi(typeof(GXScript), nameof(GXScript.Id), nameof(GXScript.Name))]
        [IncludeOpenApi(typeof(GXScriptGroup), nameof(GXScriptGroup.Id), nameof(GXScriptGroup.Name))]
        [IncludeOpenApi(typeof(GXSubtotal), nameof(GXSubtotal.Id), nameof(GXSubtotal.Name))]
        [IncludeOpenApi(typeof(GXSubtotalGroup), nameof(GXSubtotalGroup.Id), nameof(GXSubtotalGroup.Name))]
        [IncludeOpenApi(typeof(GXTrigger), nameof(GXTrigger.Id), nameof(GXTrigger.Name))]
        [IncludeOpenApi(typeof(GXTriggerGroup), nameof(GXTriggerGroup.Id), nameof(GXTriggerGroup.Name))]
        [IncludeOpenApi(typeof(GXTrigger), nameof(GXTrigger.Id), nameof(GXTrigger.Name))]
        [IncludeOpenApi(typeof(GXTriggerGroup), nameof(GXTriggerGroup.Id), nameof(GXTriggerGroup.Name))]
        [IncludeOpenApi(typeof(GXWorkflow), nameof(GXWorkflow.Id), nameof(GXWorkflow.Name))]
        [IncludeOpenApi(typeof(GXWorkflowGroup), nameof(GXWorkflowGroup.Id), nameof(GXWorkflowGroup.Name))]
        [IncludeOpenApi(typeof(GXManufacturer), nameof(GXManufacturer.Id), nameof(GXManufacturer.Name))]
        [IncludeOpenApi(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id), nameof(GXDeviceTemplate.Name))]
        [IncludeOpenApi(typeof(GXDeviceTemplateGroup), nameof(GXDeviceTemplateGroup.Id), nameof(GXDeviceTemplateGroup.Name))]
        public GXNotification? Item
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get list from notifications.
    /// </summary>
    [DataContract]
    public class ListNotifications : IGXRequest<ListNotificationResponse>
    {
        /// <summary>
        /// Start index.
        /// </summary>
        public int Index
        {
            get;
            set;

        }

        /// <summary>
        /// Amount of the notifications to retrieve.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter notifications.
        /// </summary>
        [ExcludeOpenApi(typeof(GXNotification),
            nameof(GXNotification.NotificationGroups),
            nameof(GXNotification.ScriptMethod))]
        [ExcludeOpenApi(typeof(GXNotification), nameof(GXNotification.Logs))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXNotificationGroup), nameof(GXNotificationGroup.Id), nameof(GXNotificationGroup.Name))]
        [IncludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Id), nameof(GXLanguage.Resources))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id), nameof(GXUserGroup.Name))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [IncludeOpenApi(typeof(GXNotificationGroup), nameof(GXNotificationGroup.Id))]
        [ExcludeOpenApi(typeof(GXLocalizedResource), nameof(GXLocalizedResource.Language))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id), nameof(GXDevice.Name))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id), nameof(GXDeviceGroup.Name))]
        [IncludeOpenApi(typeof(GXAttributeTemplate), nameof(GXAttributeTemplate.Id), nameof(GXAttributeTemplate.Name))]
        [IncludeOpenApi(typeof(GXObjectTemplate), nameof(GXObjectTemplate.Id), nameof(GXObjectTemplate.Name))]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id), nameof(GXAgent.Name))]
        [IncludeOpenApi(typeof(GXAgentGroup), nameof(GXAgentGroup.Id), nameof(GXAgentGroup.Name))]
        [IncludeOpenApi(typeof(GXBlock), nameof(GXBlock.Id), nameof(GXBlock.Name))]
        [IncludeOpenApi(typeof(GXBlockGroup), nameof(GXBlockGroup.Id), nameof(GXBlockGroup.Name))]
        [IncludeOpenApi(typeof(GXContent), nameof(GXContent.Id), nameof(GXContent.Name))]
        [IncludeOpenApi(typeof(GXContentGroup), nameof(GXContentGroup.Id), nameof(GXContentGroup.Name))]
        [IncludeOpenApi(typeof(GXGateway), nameof(GXGateway.Id), nameof(GXGateway.Name))]
        [IncludeOpenApi(typeof(GXGatewayGroup), nameof(GXGatewayGroup.Id), nameof(GXGatewayGroup.Name))]
        [IncludeOpenApi(typeof(GXKeyManagement), nameof(GXKeyManagement.Id), nameof(GXKeyManagement.Name))]
        [IncludeOpenApi(typeof(GXKeyManagementGroup), nameof(GXKeyManagementGroup.Id), nameof(GXKeyManagementGroup.Name))]
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id), nameof(GXModule.Name))]
        [IncludeOpenApi(typeof(GXModuleGroup), nameof(GXModuleGroup.Id), nameof(GXModuleGroup.Name))]
        [IncludeOpenApi(typeof(GXReport), nameof(GXReport.Id), nameof(GXReport.Name))]
        [IncludeOpenApi(typeof(GXReportGroup), nameof(GXReportGroup.Id), nameof(GXReportGroup.Name))]
        [IncludeOpenApi(typeof(GXSchedule), nameof(GXSchedule.Id), nameof(GXSchedule.Name))]
        [IncludeOpenApi(typeof(GXScheduleGroup), nameof(GXScheduleGroup.Id), nameof(GXScheduleGroup.Name))]
        [IncludeOpenApi(typeof(GXScript), nameof(GXScript.Id), nameof(GXScript.Name))]
        [IncludeOpenApi(typeof(GXScriptGroup), nameof(GXScriptGroup.Id), nameof(GXScriptGroup.Name))]
        [IncludeOpenApi(typeof(GXSubtotal), nameof(GXSubtotal.Id), nameof(GXSubtotal.Name))]
        [IncludeOpenApi(typeof(GXSubtotalGroup), nameof(GXSubtotalGroup.Id), nameof(GXSubtotalGroup.Name))]
        [IncludeOpenApi(typeof(GXTrigger), nameof(GXTrigger.Id), nameof(GXTrigger.Name))]
        [IncludeOpenApi(typeof(GXTriggerGroup), nameof(GXTriggerGroup.Id), nameof(GXTriggerGroup.Name))]
        [IncludeOpenApi(typeof(GXTrigger), nameof(GXTrigger.Id), nameof(GXTrigger.Name))]
        [IncludeOpenApi(typeof(GXTriggerGroup), nameof(GXTriggerGroup.Id), nameof(GXTriggerGroup.Name))]
        [IncludeOpenApi(typeof(GXWorkflow), nameof(GXWorkflow.Id), nameof(GXWorkflow.Name))]
        [IncludeOpenApi(typeof(GXWorkflowGroup), nameof(GXWorkflowGroup.Id), nameof(GXWorkflowGroup.Name))]
        [IncludeOpenApi(typeof(GXManufacturer), nameof(GXManufacturer.Id), nameof(GXManufacturer.Name))]
        [IncludeOpenApi(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id), nameof(GXDeviceTemplate.Name))]
        [IncludeOpenApi(typeof(GXDeviceTemplateGroup), nameof(GXDeviceTemplateGroup.Id), nameof(GXDeviceTemplateGroup.Name))]
        public GXNotification? Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Admin user can access notifications from all users.
        /// </summary>
        /// <remarks>
        /// If true, notifications from all users are retreaved, not just current user. 
        /// </remarks>
        public bool AllUsers
        {
            get;
            set;
        }

        /// <summary>
        /// Selected extra information.
        /// </summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        public string[]? Select
        {
            get;
            set;
        }

        /// <summary>
        /// Order by name.
        /// </summary>
        /// <remarks>
        /// Default order by is used if this is not set.
        /// </remarks>
        /// <seealso cref="Descending"/>
        public string? OrderBy
        {
            get;
            set;
        }

        /// <summary>
        /// Are values shown as descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        public bool Descending
        {
            get;
            set;
        }

        /// <summary>
        /// Included Ids.
        /// </summary>
        /// <remarks>
        /// Included Ids can be used to get only part of large data.
        /// </remarks>
        public Guid[]? Included
        {
            get;
            set;
        }

        /// <summary>
        /// Excluded Ids.
        /// </summary>
        /// <remarks>
        /// Excluded Ids can be used to filter data.
        /// </remarks>
        public Guid[]? Exclude
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Notification items reply.
    /// </summary>
    [DataContract]
    public class ListNotificationResponse
    {
        /// <summary>
        /// List of notification items.
        /// </summary>
        [DataMember]
        [ExcludeOpenApi(typeof(GXNotification), nameof(GXNotification.Logs))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXNotificationGroup), nameof(GXNotificationGroup.Id), nameof(GXNotificationGroup.Name))]
        [IncludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Id), nameof(GXLanguage.Resources))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id), nameof(GXUserGroup.Name))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [IncludeOpenApi(typeof(GXNotificationGroup), nameof(GXNotificationGroup.Id))]
        [ExcludeOpenApi(typeof(GXLocalizedResource), nameof(GXLocalizedResource.Language))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id), nameof(GXDevice.Name))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id), nameof(GXDeviceGroup.Name))]
        [IncludeOpenApi(typeof(GXAttributeTemplate), nameof(GXAttributeTemplate.Id), nameof(GXAttributeTemplate.Name))]
        [IncludeOpenApi(typeof(GXObjectTemplate), nameof(GXObjectTemplate.Id), nameof(GXObjectTemplate.Name))]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id), nameof(GXAgent.Name))]
        [IncludeOpenApi(typeof(GXAgentGroup), nameof(GXAgentGroup.Id), nameof(GXAgentGroup.Name))]
        [IncludeOpenApi(typeof(GXBlock), nameof(GXBlock.Id), nameof(GXBlock.Name))]
        [IncludeOpenApi(typeof(GXBlockGroup), nameof(GXBlockGroup.Id), nameof(GXBlockGroup.Name))]
        [IncludeOpenApi(typeof(GXContent), nameof(GXContent.Id), nameof(GXContent.Name))]
        [IncludeOpenApi(typeof(GXContentGroup), nameof(GXContentGroup.Id), nameof(GXContentGroup.Name))]
        [IncludeOpenApi(typeof(GXGateway), nameof(GXGateway.Id), nameof(GXGateway.Name))]
        [IncludeOpenApi(typeof(GXGatewayGroup), nameof(GXGatewayGroup.Id), nameof(GXGatewayGroup.Name))]
        [IncludeOpenApi(typeof(GXKeyManagement), nameof(GXKeyManagement.Id), nameof(GXKeyManagement.Name))]
        [IncludeOpenApi(typeof(GXKeyManagementGroup), nameof(GXKeyManagementGroup.Id), nameof(GXKeyManagementGroup.Name))]
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id), nameof(GXModule.Name))]
        [IncludeOpenApi(typeof(GXModuleGroup), nameof(GXModuleGroup.Id), nameof(GXModuleGroup.Name))]
        [IncludeOpenApi(typeof(GXReport), nameof(GXReport.Id), nameof(GXReport.Name))]
        [IncludeOpenApi(typeof(GXReportGroup), nameof(GXReportGroup.Id), nameof(GXReportGroup.Name))]
        [IncludeOpenApi(typeof(GXSchedule), nameof(GXSchedule.Id), nameof(GXSchedule.Name))]
        [IncludeOpenApi(typeof(GXScheduleGroup), nameof(GXScheduleGroup.Id), nameof(GXScheduleGroup.Name))]
        [IncludeOpenApi(typeof(GXScript), nameof(GXScript.Id), nameof(GXScript.Name))]
        [IncludeOpenApi(typeof(GXScriptGroup), nameof(GXScriptGroup.Id), nameof(GXScriptGroup.Name))]
        [IncludeOpenApi(typeof(GXSubtotal), nameof(GXSubtotal.Id), nameof(GXSubtotal.Name))]
        [IncludeOpenApi(typeof(GXSubtotalGroup), nameof(GXSubtotalGroup.Id), nameof(GXSubtotalGroup.Name))]
        [IncludeOpenApi(typeof(GXTrigger), nameof(GXTrigger.Id), nameof(GXTrigger.Name))]
        [IncludeOpenApi(typeof(GXTriggerGroup), nameof(GXTriggerGroup.Id), nameof(GXTriggerGroup.Name))]
        [IncludeOpenApi(typeof(GXTrigger), nameof(GXTrigger.Id), nameof(GXTrigger.Name))]
        [IncludeOpenApi(typeof(GXTriggerGroup), nameof(GXTriggerGroup.Id), nameof(GXTriggerGroup.Name))]
        [IncludeOpenApi(typeof(GXWorkflow), nameof(GXWorkflow.Id), nameof(GXWorkflow.Name))]
        [IncludeOpenApi(typeof(GXWorkflowGroup), nameof(GXWorkflowGroup.Id), nameof(GXWorkflowGroup.Name))]
        [IncludeOpenApi(typeof(GXManufacturer), nameof(GXManufacturer.Id), nameof(GXManufacturer.Name))]
        [IncludeOpenApi(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id), nameof(GXDeviceTemplate.Name))]
        [IncludeOpenApi(typeof(GXDeviceTemplateGroup), nameof(GXDeviceTemplateGroup.Id), nameof(GXDeviceTemplateGroup.Name))]
        public GXNotification[]? Notifications
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the notificationrs.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update notifications.
    /// </summary>
    [DataContract]
    public class UpdateNotification : IGXRequest<UpdateNotificationResponse>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UpdateNotification()
        {
            Notifications = new List<GXNotification>();
        }

        /// <summary>
        /// Notifications to update.
        /// </summary>
        [DataMember]
        [ExcludeOpenApi(typeof(GXNotification), nameof(GXNotification.Logs))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Id), nameof(GXLanguage.Resources))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id), nameof(GXUserGroup.Name))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [IncludeOpenApi(typeof(GXNotificationGroup), nameof(GXNotificationGroup.Id))]
        [ExcludeOpenApi(typeof(GXLocalizedResource), nameof(GXLocalizedResource.Language))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id), nameof(GXDevice.Name))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id), nameof(GXDeviceGroup.Name))]
        [IncludeOpenApi(typeof(GXAttributeTemplate), nameof(GXAttributeTemplate.Id), nameof(GXAttributeTemplate.Name))]
        [IncludeOpenApi(typeof(GXObjectTemplate), nameof(GXObjectTemplate.Id), nameof(GXObjectTemplate.Name))]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id), nameof(GXAgent.Name))]
        [IncludeOpenApi(typeof(GXAgentGroup), nameof(GXAgentGroup.Id), nameof(GXAgentGroup.Name))]
        [IncludeOpenApi(typeof(GXBlock), nameof(GXBlock.Id), nameof(GXBlock.Name))]
        [IncludeOpenApi(typeof(GXBlockGroup), nameof(GXBlockGroup.Id), nameof(GXBlockGroup.Name))]
        [IncludeOpenApi(typeof(GXContent), nameof(GXContent.Id), nameof(GXContent.Name))]
        [IncludeOpenApi(typeof(GXContentGroup), nameof(GXContentGroup.Id), nameof(GXContentGroup.Name))]
        [IncludeOpenApi(typeof(GXGateway), nameof(GXGateway.Id), nameof(GXGateway.Name))]
        [IncludeOpenApi(typeof(GXGatewayGroup), nameof(GXGatewayGroup.Id), nameof(GXGatewayGroup.Name))]
        [IncludeOpenApi(typeof(GXKeyManagement), nameof(GXKeyManagement.Id), nameof(GXKeyManagement.Name))]
        [IncludeOpenApi(typeof(GXKeyManagementGroup), nameof(GXKeyManagementGroup.Id), nameof(GXKeyManagementGroup.Name))]
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id), nameof(GXModule.Name))]
        [IncludeOpenApi(typeof(GXModuleGroup), nameof(GXModuleGroup.Id), nameof(GXModuleGroup.Name))]
        [IncludeOpenApi(typeof(GXReport), nameof(GXReport.Id), nameof(GXReport.Name))]
        [IncludeOpenApi(typeof(GXReportGroup), nameof(GXReportGroup.Id), nameof(GXReportGroup.Name))]
        [IncludeOpenApi(typeof(GXSchedule), nameof(GXSchedule.Id), nameof(GXSchedule.Name))]
        [IncludeOpenApi(typeof(GXScheduleGroup), nameof(GXScheduleGroup.Id), nameof(GXScheduleGroup.Name))]
        [IncludeOpenApi(typeof(GXScript), nameof(GXScript.Id), nameof(GXScript.Name))]
        [IncludeOpenApi(typeof(GXScriptGroup), nameof(GXScriptGroup.Id), nameof(GXScriptGroup.Name))]
        [IncludeOpenApi(typeof(GXSubtotal), nameof(GXSubtotal.Id), nameof(GXSubtotal.Name))]
        [IncludeOpenApi(typeof(GXSubtotalGroup), nameof(GXSubtotalGroup.Id), nameof(GXSubtotalGroup.Name))]
        [IncludeOpenApi(typeof(GXTrigger), nameof(GXTrigger.Id), nameof(GXTrigger.Name))]
        [IncludeOpenApi(typeof(GXTriggerGroup), nameof(GXTriggerGroup.Id), nameof(GXTriggerGroup.Name))]
        [IncludeOpenApi(typeof(GXTrigger), nameof(GXTrigger.Id), nameof(GXTrigger.Name))]
        [IncludeOpenApi(typeof(GXTriggerGroup), nameof(GXTriggerGroup.Id), nameof(GXTriggerGroup.Name))]
        [IncludeOpenApi(typeof(GXWorkflow), nameof(GXWorkflow.Id), nameof(GXWorkflow.Name))]
        [IncludeOpenApi(typeof(GXWorkflowGroup), nameof(GXWorkflowGroup.Id), nameof(GXWorkflowGroup.Name))]
        [IncludeOpenApi(typeof(GXManufacturer), nameof(GXManufacturer.Id), nameof(GXManufacturer.Name))]
        [IncludeOpenApi(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id), nameof(GXDeviceTemplate.Name))]
        [IncludeOpenApi(typeof(GXDeviceTemplateGroup), nameof(GXDeviceTemplateGroup.Id), nameof(GXDeviceTemplateGroup.Name))]

        public List<GXNotification> Notifications
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update notifications reply.
    /// </summary>
    [DataContract]
    public class UpdateNotificationResponse
    {
        /// <summary>
        /// New notification identifiers.
        /// </summary>
        [DataMember]
        public Guid[]? Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Delete notifications.
    /// </summary>
    [DataContract]
    public class RemoveNotification : IGXRequest<RemoveNotificationResponse>
    {
        /// <summary>
        /// Removed notification identifiers.
        /// </summary>
        [DataMember]
        public Guid[]? Ids
        {
            get;
            set;
        }

        /// <summary>
        /// Items are removed from the database.
        /// </summary>
        /// <remarks>
        /// If false, the Removed date is set for the items, but items are kept on the database.
        /// </remarks>
        [DataMember]
        [Required]
        public bool Delete
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Reply from Delete notification.
    /// </summary>
    [DataContract]
    public class RemoveNotificationResponse
    {
    }

    /// <summary>
    /// Close notification.
    /// </summary>
    [DataContract]
    public class CloseNotification : IGXRequest<CloseNotificationResponse>
    {
        /// <summary>
        /// Notifications IDs to close.
        /// </summary>
        [DataMember]
        public Guid[]? Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Close notifications response.
    /// </summary>
    [DataContract]
    public class CloseNotificationResponse
    {
    }
}
