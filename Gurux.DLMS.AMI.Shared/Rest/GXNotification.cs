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
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.Service.Orm.Common;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested notification information.</summary>
    [Description("Response containing the requested notification information.")]
    public class GetNotificationResponse
    {
        /// <summary>
        /// Device information.
        /// </summary>        
        [Description("Device information.")]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Id), nameof(GXLanguage.Resources))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id), nameof(GXUserGroup.Name))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [ExcludeOpenApi(typeof(GXLocalizedResource), nameof(GXLocalizedResource.Language))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id), nameof(GXDevice.Name))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id), nameof(GXDeviceGroup.Name))]
        [IncludeOpenApi(typeof(GXAttributeTemplate), nameof(GXAttributeTemplate.Id), nameof(GXAttributeTemplate.Name))]
        [IncludeOpenApi(typeof(GXObjectTemplate), nameof(GXObjectTemplate.Id), nameof(GXObjectTemplate.Name))]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id), nameof(GXAgent.Name))]
        [IncludeOpenApi(typeof(GXAgentGroup), nameof(GXAgentGroup.Id), nameof(GXAgentGroup.Name))]
        [IncludeOpenApi(typeof(GXBlock), nameof(GXBlock.Id), nameof(GXBlock.Name))]
        [IncludeOpenApi(typeof(GXBlockGroup), nameof(GXBlockGroup.Id), nameof(GXBlockGroup.Name))]
        [IncludeOpenApi(typeof(GXGateway), nameof(GXGateway.Id), nameof(GXGateway.Name))]
        [IncludeOpenApi(typeof(GXGatewayGroup), nameof(GXGatewayGroup.Id), nameof(GXGatewayGroup.Name))]
        [IncludeOpenApi(typeof(GXKeyManagement), nameof(GXKeyManagement.Id), nameof(GXKeyManagement.Name))]
        [IncludeOpenApi(typeof(GXKeyManagementGroup), nameof(GXKeyManagementGroup.Id), nameof(GXKeyManagementGroup.Name))]
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id), nameof(GXModule.Name))]
        [IncludeOpenApi(typeof(GXModuleGroup), nameof(GXModuleGroup.Id), nameof(GXModuleGroup.Name))]
        [IncludeOpenApi(typeof(GXSchedule), nameof(GXSchedule.Id), nameof(GXSchedule.Name))]
        [IncludeOpenApi(typeof(GXScheduleGroup), nameof(GXScheduleGroup.Id), nameof(GXScheduleGroup.Name))]
        [IncludeOpenApi(typeof(GXScript), nameof(GXScript.Id), nameof(GXScript.Name))]
        [IncludeOpenApi(typeof(GXScriptGroup), nameof(GXScriptGroup.Id), nameof(GXScriptGroup.Name))]
        [IncludeOpenApi(typeof(GXTrigger), nameof(GXTrigger.Id), nameof(GXTrigger.Name))]
        [IncludeOpenApi(typeof(GXTriggerGroup), nameof(GXTriggerGroup.Id), nameof(GXTriggerGroup.Name))]
        [IncludeOpenApi(typeof(GXTrigger), nameof(GXTrigger.Id), nameof(GXTrigger.Name))]
        [IncludeOpenApi(typeof(GXTriggerGroup), nameof(GXTriggerGroup.Id), nameof(GXTriggerGroup.Name))]
        [IncludeOpenApi(typeof(GXWorkflow), nameof(GXWorkflow.Id), nameof(GXWorkflow.Name))]
        [IncludeOpenApi(typeof(GXWorkflowGroup), nameof(GXWorkflowGroup.Id), nameof(GXWorkflowGroup.Name))]
        [IncludeOpenApi(typeof(GXManufacturer), nameof(GXManufacturer.Id), nameof(GXManufacturer.Name))]
        [IncludeOpenApi(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id), nameof(GXDeviceTemplate.Name))]
        [IncludeOpenApi(typeof(GXDeviceTemplateGroup), nameof(GXDeviceTemplateGroup.Id), nameof(GXDeviceTemplateGroup.Name))]
        public GXNotification? Item { get; set; }
    }

    /// <summary>Requests a list of notifications.</summary>
    [Description("Requests a list of notifications.")]
    [DataContract]
    public class ListNotifications : IGXRequest<ListNotificationResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of notifications to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of notifications to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter notifications.
        /// </summary>
        [Description("Optional criteria used to filter notifications.")]
        [ExcludeOpenApi(typeof(GXNotification),
            nameof(GXNotification.ScriptMethod))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Id), nameof(GXLanguage.Resources))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id), nameof(GXUserGroup.Name))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [ExcludeOpenApi(typeof(GXLocalizedResource), nameof(GXLocalizedResource.Language))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id), nameof(GXDevice.Name))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id), nameof(GXDeviceGroup.Name))]
        [IncludeOpenApi(typeof(GXAttributeTemplate), nameof(GXAttributeTemplate.Id), nameof(GXAttributeTemplate.Name))]
        [IncludeOpenApi(typeof(GXObjectTemplate), nameof(GXObjectTemplate.Id), nameof(GXObjectTemplate.Name))]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id), nameof(GXAgent.Name))]
        [IncludeOpenApi(typeof(GXAgentGroup), nameof(GXAgentGroup.Id), nameof(GXAgentGroup.Name))]
        [IncludeOpenApi(typeof(GXBlock), nameof(GXBlock.Id), nameof(GXBlock.Name))]
        [IncludeOpenApi(typeof(GXBlockGroup), nameof(GXBlockGroup.Id), nameof(GXBlockGroup.Name))]
        [IncludeOpenApi(typeof(GXGateway), nameof(GXGateway.Id), nameof(GXGateway.Name))]
        [IncludeOpenApi(typeof(GXGatewayGroup), nameof(GXGatewayGroup.Id), nameof(GXGatewayGroup.Name))]
        [IncludeOpenApi(typeof(GXKeyManagement), nameof(GXKeyManagement.Id), nameof(GXKeyManagement.Name))]
        [IncludeOpenApi(typeof(GXKeyManagementGroup), nameof(GXKeyManagementGroup.Id), nameof(GXKeyManagementGroup.Name))]
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id), nameof(GXModule.Name))]
        [IncludeOpenApi(typeof(GXModuleGroup), nameof(GXModuleGroup.Id), nameof(GXModuleGroup.Name))]
        [IncludeOpenApi(typeof(GXSchedule), nameof(GXSchedule.Id), nameof(GXSchedule.Name))]
        [IncludeOpenApi(typeof(GXScheduleGroup), nameof(GXScheduleGroup.Id), nameof(GXScheduleGroup.Name))]
        [IncludeOpenApi(typeof(GXScript), nameof(GXScript.Id), nameof(GXScript.Name))]
        [IncludeOpenApi(typeof(GXScriptGroup), nameof(GXScriptGroup.Id), nameof(GXScriptGroup.Name))]
        [IncludeOpenApi(typeof(GXTrigger), nameof(GXTrigger.Id), nameof(GXTrigger.Name))]
        [IncludeOpenApi(typeof(GXTriggerGroup), nameof(GXTriggerGroup.Id), nameof(GXTriggerGroup.Name))]
        [IncludeOpenApi(typeof(GXTrigger), nameof(GXTrigger.Id), nameof(GXTrigger.Name))]
        [IncludeOpenApi(typeof(GXTriggerGroup), nameof(GXTriggerGroup.Id), nameof(GXTriggerGroup.Name))]
        [IncludeOpenApi(typeof(GXWorkflow), nameof(GXWorkflow.Id), nameof(GXWorkflow.Name))]
        [IncludeOpenApi(typeof(GXWorkflowGroup), nameof(GXWorkflowGroup.Id), nameof(GXWorkflowGroup.Name))]
        [IncludeOpenApi(typeof(GXManufacturer), nameof(GXManufacturer.Id), nameof(GXManufacturer.Name))]
        [IncludeOpenApi(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id), nameof(GXDeviceTemplate.Name))]
        [IncludeOpenApi(typeof(GXDeviceTemplateGroup), nameof(GXDeviceTemplateGroup.Id), nameof(GXDeviceTemplateGroup.Name))]
        [ExcludeOpenApi(typeof(GXNotification), nameof(GXNotification.MinimumDeliveryTime), nameof(GXNotification.TraceLevel), nameof(GXNotification.ConcurrencyStamp))]
        public GXNotification? Filter { get; set; }

        /// <summary>Additional related information to include in the response. Reserved for future use.</summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        [Description("Additional related information to include in the response. Reserved for future use.")]
        public IEnumerable<string>? Select { get; set; }

        /// <summary>Name of the property to sort by. If omitted, the endpoint uses its default ordering.</summary>
        /// <remarks>
        /// Uses the endpoint's default ordering when not specified.
        /// </remarks>
        /// <seealso cref="Descending"/>
        [Description("Name of the property to sort by. If omitted, the endpoint uses its default ordering.")]
        public string? OrderBy { get; set; }

        /// <summary>
        /// Whether to sort results in descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        [Description("Whether to sort results in descending order.")]
        public bool Descending { get; set; }

        /// <summary>Identifiers to include in the results. A nonempty collection restricts results to these identifiers.</summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        [Description("Identifiers to include in the results. A nonempty collection restricts results to these identifiers.")]
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        [Description("Identifiers to exclude from the results.")]
        public IEnumerable<Guid>? Exclude { get; set; }
    }

    /// <summary>Response containing the requested notifications.</summary>
    [Description("Response containing the requested notifications.")]
    [DataContract]
    public class ListNotificationResponse
    {
        /// <summary>
        /// List of notification items.
        /// </summary>
        [Description("List of notification items.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Id), nameof(GXLanguage.Resources))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id), nameof(GXUserGroup.Name))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [ExcludeOpenApi(typeof(GXLocalizedResource), nameof(GXLocalizedResource.Language))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id), nameof(GXDevice.Name))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id), nameof(GXDeviceGroup.Name))]
        [IncludeOpenApi(typeof(GXAttributeTemplate), nameof(GXAttributeTemplate.Id), nameof(GXAttributeTemplate.Name))]
        [IncludeOpenApi(typeof(GXObjectTemplate), nameof(GXObjectTemplate.Id), nameof(GXObjectTemplate.Name))]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id), nameof(GXAgent.Name))]
        [IncludeOpenApi(typeof(GXAgentGroup), nameof(GXAgentGroup.Id), nameof(GXAgentGroup.Name))]
        [IncludeOpenApi(typeof(GXBlock), nameof(GXBlock.Id), nameof(GXBlock.Name))]
        [IncludeOpenApi(typeof(GXBlockGroup), nameof(GXBlockGroup.Id), nameof(GXBlockGroup.Name))]
        [IncludeOpenApi(typeof(GXGateway), nameof(GXGateway.Id), nameof(GXGateway.Name))]
        [IncludeOpenApi(typeof(GXGatewayGroup), nameof(GXGatewayGroup.Id), nameof(GXGatewayGroup.Name))]
        [IncludeOpenApi(typeof(GXKeyManagement), nameof(GXKeyManagement.Id), nameof(GXKeyManagement.Name))]
        [IncludeOpenApi(typeof(GXKeyManagementGroup), nameof(GXKeyManagementGroup.Id), nameof(GXKeyManagementGroup.Name))]
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id), nameof(GXModule.Name))]
        [IncludeOpenApi(typeof(GXModuleGroup), nameof(GXModuleGroup.Id), nameof(GXModuleGroup.Name))]
        [IncludeOpenApi(typeof(GXSchedule), nameof(GXSchedule.Id), nameof(GXSchedule.Name))]
        [IncludeOpenApi(typeof(GXScheduleGroup), nameof(GXScheduleGroup.Id), nameof(GXScheduleGroup.Name))]
        [IncludeOpenApi(typeof(GXScript), nameof(GXScript.Id), nameof(GXScript.Name))]
        [IncludeOpenApi(typeof(GXScriptGroup), nameof(GXScriptGroup.Id), nameof(GXScriptGroup.Name))]
        [IncludeOpenApi(typeof(GXTrigger), nameof(GXTrigger.Id), nameof(GXTrigger.Name))]
        [IncludeOpenApi(typeof(GXTriggerGroup), nameof(GXTriggerGroup.Id), nameof(GXTriggerGroup.Name))]
        [IncludeOpenApi(typeof(GXTrigger), nameof(GXTrigger.Id), nameof(GXTrigger.Name))]
        [IncludeOpenApi(typeof(GXTriggerGroup), nameof(GXTriggerGroup.Id), nameof(GXTriggerGroup.Name))]
        [IncludeOpenApi(typeof(GXWorkflow), nameof(GXWorkflow.Id), nameof(GXWorkflow.Name))]
        [IncludeOpenApi(typeof(GXWorkflowGroup), nameof(GXWorkflowGroup.Id), nameof(GXWorkflowGroup.Name))]
        [IncludeOpenApi(typeof(GXManufacturer), nameof(GXManufacturer.Id), nameof(GXManufacturer.Name))]
        [IncludeOpenApi(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id), nameof(GXDeviceTemplate.Name))]
        [IncludeOpenApi(typeof(GXDeviceTemplateGroup), nameof(GXDeviceTemplateGroup.Id), nameof(GXDeviceTemplateGroup.Name))]
        public IEnumerable<GXNotification>? Notifications { get; set; }

        /// <summary>
        /// Total number of matching notificationrs before paging.
        /// </summary>
        [Description("Total number of matching notificationrs before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests changes to the specified notifications.</summary>
    [Description("Requests changes to the specified notifications.")]
    [DataContract]
    public class UpdateNotification : IGXRequest<UpdateNotificationResponse>
    {
        /// <summary>Initializes a new instance of UpdateNotification.</summary>
        [Description("Initializes a new instance of UpdateNotification.")]
        public UpdateNotification()
        {
            Notifications = [];
        }

        /// <summary>
        /// Notifications to update.
        /// </summary>
        [Description("Notifications to update.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Id), nameof(GXLanguage.Resources))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id), nameof(GXUserGroup.Name))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [ExcludeOpenApi(typeof(GXLocalizedResource), nameof(GXLocalizedResource.Language))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id), nameof(GXDevice.Name))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id), nameof(GXDeviceGroup.Name))]
        [IncludeOpenApi(typeof(GXAttributeTemplate), nameof(GXAttributeTemplate.Id), nameof(GXAttributeTemplate.Name))]
        [IncludeOpenApi(typeof(GXObjectTemplate), nameof(GXObjectTemplate.Id), nameof(GXObjectTemplate.Name))]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id), nameof(GXAgent.Name))]
        [IncludeOpenApi(typeof(GXAgentGroup), nameof(GXAgentGroup.Id), nameof(GXAgentGroup.Name))]
        [IncludeOpenApi(typeof(GXBlock), nameof(GXBlock.Id), nameof(GXBlock.Name))]
        [IncludeOpenApi(typeof(GXBlockGroup), nameof(GXBlockGroup.Id), nameof(GXBlockGroup.Name))]
        [IncludeOpenApi(typeof(GXGateway), nameof(GXGateway.Id), nameof(GXGateway.Name))]
        [IncludeOpenApi(typeof(GXGatewayGroup), nameof(GXGatewayGroup.Id), nameof(GXGatewayGroup.Name))]
        [IncludeOpenApi(typeof(GXKeyManagement), nameof(GXKeyManagement.Id), nameof(GXKeyManagement.Name))]
        [IncludeOpenApi(typeof(GXKeyManagementGroup), nameof(GXKeyManagementGroup.Id), nameof(GXKeyManagementGroup.Name))]
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id), nameof(GXModule.Name))]
        [IncludeOpenApi(typeof(GXModuleGroup), nameof(GXModuleGroup.Id), nameof(GXModuleGroup.Name))]
        [IncludeOpenApi(typeof(GXSchedule), nameof(GXSchedule.Id), nameof(GXSchedule.Name))]
        [IncludeOpenApi(typeof(GXScheduleGroup), nameof(GXScheduleGroup.Id), nameof(GXScheduleGroup.Name))]
        [IncludeOpenApi(typeof(GXScript), nameof(GXScript.Id), nameof(GXScript.Name))]
        [IncludeOpenApi(typeof(GXScriptGroup), nameof(GXScriptGroup.Id), nameof(GXScriptGroup.Name))]
        [IncludeOpenApi(typeof(GXTrigger), nameof(GXTrigger.Id), nameof(GXTrigger.Name))]
        [IncludeOpenApi(typeof(GXTriggerGroup), nameof(GXTriggerGroup.Id), nameof(GXTriggerGroup.Name))]
        [IncludeOpenApi(typeof(GXTrigger), nameof(GXTrigger.Id), nameof(GXTrigger.Name))]
        [IncludeOpenApi(typeof(GXTriggerGroup), nameof(GXTriggerGroup.Id), nameof(GXTriggerGroup.Name))]
        [IncludeOpenApi(typeof(GXWorkflow), nameof(GXWorkflow.Id), nameof(GXWorkflow.Name))]
        [IncludeOpenApi(typeof(GXWorkflowGroup), nameof(GXWorkflowGroup.Id), nameof(GXWorkflowGroup.Name))]
        [IncludeOpenApi(typeof(GXManufacturer), nameof(GXManufacturer.Id), nameof(GXManufacturer.Name))]
        [IncludeOpenApi(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id), nameof(GXDeviceTemplate.Name))]
        [IncludeOpenApi(typeof(GXDeviceTemplateGroup), nameof(GXDeviceTemplateGroup.Id), nameof(GXDeviceTemplateGroup.Name))]

        [ExcludeOpenApi(typeof(GXNotificationType), nameof(GXNotificationType.CreationTime), nameof(GXNotificationType.Updated))]
        [ExcludeOpenApi(typeof(GXNotification), nameof(GXNotification.CreationTime), nameof(GXNotification.Updated))]
        public List<GXNotification> Notifications { get; set; }
    }

    /// <summary>Response to updating the specified notifications.</summary>
    [Description("Response to updating the specified notifications.")]
    [DataContract]
    public class UpdateNotificationResponse
    {
        /// <summary>Identifiers of the added or updated notification records.</summary>
        [Description("Identifiers of the added or updated notification records.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Requests deletion of the selected notifications.</summary>
    [Description("Requests deletion of the selected notifications.")]
    [DataContract]
    public class RemoveNotification : IGXRequest<RemoveNotificationResponse>
    {
        /// <summary>Identifiers of the notification records to delete.</summary>
        [Description("Identifiers of the notification records to delete.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;

        /// <summary>Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.</summary>
        /// <remarks>
        /// If false, marks the items as removed and retains their database records.
        /// </remarks>
        [Description("Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.")]
        [DataMember]
        [Required]
        public bool Delete { get; set; }
    }

    /// <summary>Response to deleting the selected notifications.</summary>
    [Description("Response to deleting the selected notifications.")]
    [DataContract]
    public class RemoveNotificationResponse
    {
    }

    /// <summary>
    /// Refresh user notifications.
    /// </summary>
    [Description("Refresh user notifications.")]
    [DataContract]
    public class RefreshNotification : IGXRequest<RefreshNotificationResponse>
    {
        /// <summary>
        /// User for whom the notifications are being refreshed.
        /// </summary>
        [Description("User for whom the notifications are being refreshed.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public GXUser? User { get; set; }

        /// <summary>
        /// If true, existing notifications are removed.
        /// </summary>
        [Description("If true, existing notifications are removed.")]
        [DataMember]
        public bool Delete { get; set; }
    }

    /// <summary>
    /// Refresh user notifications response.
    /// </summary>
    [Description("Refresh user notifications response.")]
    [DataContract]
    public class RefreshNotificationResponse
    {
        /// <summary>
        /// Notification identifiers.
        /// </summary>
        [Description("Notification identifiers.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }
}
