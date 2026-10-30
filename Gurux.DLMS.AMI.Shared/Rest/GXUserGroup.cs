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
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.Service.Orm.Common;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs.View;

using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested user group information.</summary>
    [Description("Response containing the requested user group information.")]
    public class GetUserGroupResponse
    {
        /// <summary>
        /// User group information.
        /// </summary>
        [Description("User group information.")]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName),
            nameof(GXUser.GivenName), nameof(GXUser.Surname))]
        [IncludeOpenApi(typeof(GXScheduleGroup), nameof(GXScheduleGroup.Id),
            nameof(GXScheduleGroup.Name))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id),
            nameof(GXDeviceGroup.Name))]
        [ExcludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.DeviceTemplateGroups),
            nameof(GXUserGroup.AgentGroups),
            nameof(GXUserGroup.GatewayGroups),
            nameof(GXUserGroup.ModuleGroups),
            nameof(GXUserGroup.WorkflowGroups),
            nameof(GXUserGroup.TriggerGroups),
            nameof(GXUserGroup.BlockGroups),
            nameof(GXUserGroup.ComponentViewGroups),
            nameof(GXUserGroup.ManufacturerGroups),
            nameof(GXUserGroup.KeyManagementGroups),
            nameof(GXUserGroup.ScriptGroups),
            nameof(GXUserGroup.MenuGroups))]
        [ExcludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.ViewGroups))]
        public GXUserGroup? Item { get; set; }
    }

    /// <summary>Requests a list of user groups.</summary>
    [Description("Requests a list of user groups.")]
    [DataContract]
    public class ListUserGroups : IGXRequest<ListUserGroupsResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of user groups to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of user groups to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter user groups.
        /// </summary>
        [Description("Optional criteria used to filter user groups.")]
        [ExcludeOpenApi(typeof(GXUserGroup),
            nameof(GXUserGroup.Users),
            nameof(GXUserGroup.DeviceGroups),
            nameof(GXUserGroup.ScheduleGroups),
            nameof(GXUserGroup.DeviceTemplateGroups),
            nameof(GXUserGroup.AgentGroups),
            nameof(GXUserGroup.GatewayGroups),
            nameof(GXUserGroup.ModuleGroups),
            nameof(GXUserGroup.WorkflowGroups),
            nameof(GXUserGroup.TriggerGroups),
            nameof(GXUserGroup.BlockGroups),
            nameof(GXUserGroup.ComponentViewGroups),
            nameof(GXUserGroup.ManufacturerGroups),
            nameof(GXUserGroup.KeyManagementGroups),
            nameof(GXUserGroup.ScriptGroups),
            nameof(GXUserGroup.MenuGroups))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [ExcludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.ViewGroups))]
        [ExcludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.ConcurrencyStamp))]
        public GXUserGroup? Filter { get; set; }

        /// <summary>Gets or sets inclusive date and time ranges for properties of the filter type.</summary>
        /// <remarks>
        /// Ranges are combined with each other and with <see cref="Filter"/> using AND.
        /// Specify a public stored date and time property name. Null or an empty collection
        /// applies no additional date restrictions. Filtering precedes counting and paging.
        /// </remarks>
        [DataMember]
        [Description("Inclusive date and time ranges combined with the other filters using AND.")]
        public IEnumerable<GXDateTimeFilter>? DateFilters { get; set; }

        /// <summary>Whether administrators can access groups owned by other users.</summary>
        /// <remarks>
        /// If true, groups from all users are retrieved, not just current user.
        /// </remarks>
        [Description("Whether administrators can access groups owned by other users.")]
        public bool AllUsers { get; set; }

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

    /// <summary>Response containing the requested user groups.</summary>
    [Description("Response containing the requested user groups.")]
    [DataContract]
    public class ListUserGroupsResponse
    {
        /// <summary>
        /// List of user groups.
        /// </summary>
        [Description("List of user groups.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXUserGroup),
            nameof(GXUserGroup.Users),
            nameof(GXUserGroup.DeviceGroups),
            nameof(GXUserGroup.ScheduleGroups),
            nameof(GXUserGroup.DeviceTemplateGroups),
            nameof(GXUserGroup.AgentGroups),
            nameof(GXUserGroup.GatewayGroups),
            nameof(GXUserGroup.ModuleGroups),
            nameof(GXUserGroup.WorkflowGroups),
            nameof(GXUserGroup.TriggerGroups),
            nameof(GXUserGroup.BlockGroups),
            nameof(GXUserGroup.ComponentViewGroups),
            nameof(GXUserGroup.ManufacturerGroups),
            nameof(GXUserGroup.KeyManagementGroups),
            nameof(GXUserGroup.ScriptGroups),
            nameof(GXUserGroup.MenuGroups))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [ExcludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.ViewGroups))]
        public IEnumerable<GXUserGroup>? UserGroups { get; set; }

        /// <summary>
        /// Total number of matching user groups before paging.
        /// </summary>
        [Description("Total number of matching user groups before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests the addition of user groups.</summary>
    [Description("Requests the addition of user groups.")]
    [DataContract]
    public class AddUserGroup : IGXRequest<AddUserGroupResponse>
    {
        /// <summary>New user groups.</summary>
        [Description("New user groups.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeOpenApi(typeof(GXScheduleGroup), nameof(GXScheduleGroup.Id))]
        [IncludeOpenApi(typeof(GXDeviceTemplateGroup), nameof(GXDeviceTemplateGroup.Id))]
        [IncludeOpenApi(typeof(GXAgentGroup), nameof(GXAgentGroup.Id))]
        [IncludeOpenApi(typeof(GXGatewayGroup), nameof(GXGatewayGroup.Id))]
        [IncludeOpenApi(typeof(GXModuleGroup), nameof(GXModuleGroup.Id))]
        [IncludeOpenApi(typeof(GXWorkflowGroup), nameof(GXWorkflowGroup.Id))]
        [IncludeOpenApi(typeof(GXTriggerGroup), nameof(GXTriggerGroup.Id))]
        [IncludeOpenApi(typeof(GXBlockGroup), nameof(GXBlockGroup.Id))]
        [IncludeOpenApi(typeof(GXComponentViewGroup), nameof(GXComponentViewGroup.Id))]
        [IncludeOpenApi(typeof(GXScriptGroup), nameof(GXScriptGroup.Id))]
        [IncludeOpenApi(typeof(GXManufacturerGroup), nameof(GXManufacturerGroup.Id))]
        [IncludeOpenApi(typeof(GXKeyManagementGroup), nameof(GXKeyManagementGroup.Id))]
        [IncludeOpenApi(typeof(GXMenuGroup), nameof(GXMenuGroup.Id))]
        [IncludeOpenApi(typeof(GXViewGroup), nameof(GXViewGroup.Id))]
        [ExcludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.CreationTime), nameof(GXUserGroup.Updated))]
        public IEnumerable<GXUserGroup> UserGroups { get; set; } = default!;
    }

    /// <summary>Response to adding user groups.</summary>
    [Description("Response to adding user groups.")]
    [DataContract]
    public class AddUserGroupResponse
    {
        /// <summary>Identifiers of the newly added user group records.</summary>
        [Description("Identifiers of the newly added user group records.")]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Requests deletion of the selected user groups.</summary>
    [Description("Requests deletion of the selected user groups.")]
    [DataContract]
    public class RemoveUserGroup : IGXRequest<RemoveUserGroupResponse>
    {
        /// <summary>Identifiers of the user group records to delete.</summary>
        [Description("Identifiers of the user group records to delete.")]
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

    /// <summary>Response to deleting the selected user groups.</summary>
    [Description("Response to deleting the selected user groups.")]
    [DataContract]
    public class RemoveUserGroupResponse
    {
    }
}
