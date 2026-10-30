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
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm.Common;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Module;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested user information.</summary>
    [Description("Response containing the requested user information.")]
    public class GetUserResponse
    {
        /// <summary>
        /// User account information.
        /// </summary>
        [Description("User account information.")]
        [ExcludeOpenApi(typeof(GXUser),
            nameof(GXUser.Password),
            nameof(GXUser.PasswordHash),
            nameof(GXUser.SecurityStamp),
            nameof(GXUser.Actions),
            nameof(GXUser.BlockSettings),
            nameof(GXUser.Errors),
            nameof(GXUser.Favorites),
            nameof(GXUser.Tasks),
            nameof(GXUser.Stamps))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id), nameof(GXUserGroup.Name),
            nameof(GXUserGroup.Description), nameof(GXUserGroup.CreationTime), nameof(GXUserGroup.Removed),
            nameof(GXUserGroup.Updated), nameof(GXUserGroup.ConcurrencyStamp), nameof(GXUserGroup.Default))]
        [IncludeOpenApi(typeof(GXUserStamp), nameof(GXUserStamp.Id))]
        [IncludeOpenApi(typeof(GXWorkflow), nameof(GXWorkflow.Id), nameof(GXWorkflow.Name))]
        [IncludeOpenApi(typeof(GXWorkflowGroup), nameof(GXWorkflowGroup.Id), nameof(GXWorkflowGroup.Name))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id), nameof(GXComponentView.Name))]
        [IncludeOpenApi(typeof(GXComponentViewGroup), nameof(GXComponentViewGroup.Id), nameof(GXComponentViewGroup.Name))]
        [IncludeOpenApi(typeof(GXSchedule), nameof(GXSchedule.Id), nameof(GXSchedule.Name))]
        [IncludeOpenApi(typeof(GXScheduleGroup), nameof(GXScheduleGroup.Id), nameof(GXScheduleGroup.Name))]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id), nameof(GXAgent.Name))]
        [IncludeOpenApi(typeof(GXAgentGroup), nameof(GXAgentGroup.Id), nameof(GXAgentGroup.Name))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id), nameof(GXDevice.Name))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id), nameof(GXDeviceGroup.Name))]
        [IncludeOpenApi(typeof(GXAppearance), nameof(GXAppearance.Id))]
        [ExcludeOpenApi(typeof(GXUserProperty), nameof(GXUserProperty.User))]
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id))]
        [ExcludeOpenApi(typeof(GXUser), nameof(GXUser.Workflows), nameof(GXUser.WorkflowGroups),
            nameof(GXUser.ComponentViews), nameof(GXUser.ComponentViewGroups), nameof(GXUser.Schedules),
            nameof(GXUser.ScheduleGroups), nameof(GXUser.Agents), nameof(GXUser.AgentGroups),
            nameof(GXUser.Devices), nameof(GXUser.DeviceGroups))]
        public GXUser? Item { get; set; }
    }

    /// <summary>Requests a list of users.</summary>
    [Description("Requests a list of users.")]
    [DataContract]
    public class ListUsers : IGXRequest<ListUsersResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of users to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of users to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter users.
        /// </summary>
        [Description("Optional criteria used to filter users.")]
        [ExcludeOpenApi(typeof(GXUser),
            nameof(GXUser.Password),
            nameof(GXUser.PasswordHash),
            nameof(GXUser.SecurityStamp),
            nameof(GXUser.Actions),
            nameof(GXUser.UserGroups),
            nameof(GXUser.BlockSettings),
            nameof(GXUser.Errors),
            nameof(GXUser.Favorites),
            nameof(GXUser.Tasks),
            nameof(GXUser.Stamps),
            nameof(GXUser.Properties))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id), nameof(GXAgentGroup.Name),
            nameof(GXAgentGroup.Description))]
        [IncludeOpenApi(typeof(GXUserStamp), nameof(GXUserStamp.Id))]
        [IncludeOpenApi(typeof(GXWorkflow), nameof(GXWorkflow.Id), nameof(GXWorkflow.Name))]
        [IncludeOpenApi(typeof(GXWorkflowGroup), nameof(GXWorkflowGroup.Id), nameof(GXWorkflowGroup.Name))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id), nameof(GXComponentView.Name))]
        [IncludeOpenApi(typeof(GXComponentViewGroup), nameof(GXComponentViewGroup.Id), nameof(GXComponentViewGroup.Name))]
        [IncludeOpenApi(typeof(GXSchedule), nameof(GXSchedule.Id), nameof(GXSchedule.Name))]
        [IncludeOpenApi(typeof(GXScheduleGroup), nameof(GXScheduleGroup.Id), nameof(GXScheduleGroup.Name))]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id), nameof(GXAgent.Name))]
        [IncludeOpenApi(typeof(GXAgentGroup), nameof(GXAgentGroup.Id), nameof(GXAgentGroup.Name))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id), nameof(GXDevice.Name))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id), nameof(GXDeviceGroup.Name))]
        [IncludeOpenApi(typeof(GXAppearance), nameof(GXAppearance.Id))]
        [ExcludeOpenApi(typeof(GXUser),
            nameof(GXUser.ConcurrencyStamp), nameof(GXUser.AccessFailedCount), nameof(GXUser.Schedules),
            nameof(GXUser.ScheduleGroups), nameof(GXUser.ProfilePicture), nameof(GXUser.TimeZone))]
        public GXUser? Filter { get; set; }

        /// <summary>Gets or sets inclusive date and time ranges for properties of the filter type.</summary>
        /// <remarks>
        /// Ranges are combined with each other and with <see cref="Filter"/> using AND.
        /// Specify a public date and time property name. Null or an empty collection
        /// applies no additional date restrictions. Filtering precedes counting and paging.
        /// </remarks>
        [DataMember]
        [Description("Inclusive date and time ranges combined with the other filters using AND.")]
        public IEnumerable<GXDateTimeFilter>? DateFilters { get; set; }


        /// <summary>
        /// Admin user can access all users.
        /// </summary>
        /// <remarks>
        /// If true, all users are retrieved, not just current user.
        /// </remarks>
        [Description("Admin user can access all users.")]
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
        public IEnumerable<string>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        [Description("Identifiers to exclude from the results.")]
        public IEnumerable<string>? Exclude { get; set; }
    }

    /// <summary>Response containing the requested users.</summary>
    [DataContract]
    [Description("Response containing the requested users.")]
    public class ListUsersResponse
    {
        /// <summary>
        /// List of users.
        /// </summary>
        [DataMember]
        [Description("List of users.")]
        [ExcludeOpenApi(typeof(GXUser),
                nameof(GXUser.Password),
                nameof(GXUser.PasswordHash),
                nameof(GXUser.SecurityStamp),
                nameof(GXUser.Actions),
                nameof(GXUser.UserGroups),
                nameof(GXUser.BlockSettings),
                nameof(GXUser.Tasks),
                nameof(GXUser.Stamps),
                nameof(GXUser.Errors),
                nameof(GXUser.Favorites),
                nameof(GXUser.Properties))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id), nameof(GXAgentGroup.Name),
            nameof(GXAgentGroup.Description))]
        [IncludeOpenApi(typeof(GXUserStamp), nameof(GXUserStamp.Id))]
        [IncludeOpenApi(typeof(GXWorkflow), nameof(GXWorkflow.Id), nameof(GXWorkflow.Name))]
        [IncludeOpenApi(typeof(GXWorkflowGroup), nameof(GXWorkflowGroup.Id), nameof(GXWorkflowGroup.Name))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id), nameof(GXComponentView.Name))]
        [IncludeOpenApi(typeof(GXComponentViewGroup), nameof(GXComponentViewGroup.Id), nameof(GXComponentViewGroup.Name))]
        [IncludeOpenApi(typeof(GXSchedule), nameof(GXSchedule.Id), nameof(GXSchedule.Name))]
        [IncludeOpenApi(typeof(GXScheduleGroup), nameof(GXScheduleGroup.Id), nameof(GXScheduleGroup.Name))]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id), nameof(GXAgent.Name))]
        [IncludeOpenApi(typeof(GXAgentGroup), nameof(GXAgentGroup.Id), nameof(GXAgentGroup.Name))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id), nameof(GXDevice.Name))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id), nameof(GXDeviceGroup.Name))]
        [IncludeOpenApi(typeof(GXAppearance), nameof(GXAppearance.Id))]
        [ExcludeOpenApi(typeof(GXUser), nameof(GXUser.Workflows), nameof(GXUser.WorkflowGroups),
            nameof(GXUser.ComponentViews), nameof(GXUser.ComponentViewGroups), nameof(GXUser.Schedules),
            nameof(GXUser.ScheduleGroups), nameof(GXUser.Agents), nameof(GXUser.AgentGroups),
            nameof(GXUser.Devices), nameof(GXUser.DeviceGroups))]
        [ExcludeOpenApi(typeof(GXUser), nameof(GXUser.NormalizedEmail), nameof(GXUser.NormalizedUserName),
            nameof(GXUser.PhoneNumber), nameof(GXUser.PhoneNumberConfirmed), nameof(GXUser.TwoFactorEnabled),
            nameof(GXUser.LockoutEnabled), nameof(GXUser.ConcurrencyStamp), nameof(GXUser.AccessFailedCount))]
        public IEnumerable<GXUser>? Users { get; set; }

        /// <summary>
        /// Total number of matching users before paging.
        /// </summary>
        [DataMember]
        [Description("Total number of matching users before paging.")]
        public long Count { get; set; }
    }

    /// <summary>Requests the addition or update of user accounts.</summary>
    [Description("Requests the addition or update of user accounts.")]
    [DataContract]
    public class AddUser : IGXRequest<AddUserResponse>
    {
        /// <summary>
        /// Users to add or update.
        /// </summary>
        [Description("Users to add or update.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXUser),
                nameof(GXUser.PasswordHash),
                nameof(GXUser.Actions),
                nameof(GXUser.UserGroups),
                nameof(GXUser.BlockSettings),
                nameof(GXUser.Errors),
                nameof(GXUser.Favorites),
                nameof(GXUser.Tasks),
                nameof(GXUser.Stamps))]
        [ExcludeOpenApi(typeof(GXUserProperty),
                nameof(GXUserProperty.User))]

        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id), nameof(GXAgentGroup.Name))]
        [IncludeOpenApi(typeof(GXUserStamp), nameof(GXUserStamp.Id))]
        [IncludeOpenApi(typeof(GXWorkflow), nameof(GXWorkflow.Id))]
        [IncludeOpenApi(typeof(GXWorkflowGroup), nameof(GXWorkflowGroup.Id))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXComponentViewGroup), nameof(GXComponentViewGroup.Id))]
        [IncludeOpenApi(typeof(GXSchedule), nameof(GXSchedule.Id))]
        [IncludeOpenApi(typeof(GXScheduleGroup), nameof(GXScheduleGroup.Id))]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id))]
        [IncludeOpenApi(typeof(GXAgentGroup), nameof(GXAgentGroup.Id))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [ExcludeOpenApi(typeof(GXUserProperty), nameof(GXUserProperty.User),
            nameof(GXUserProperty.Module))]
        [IncludeOpenApi(typeof(GXAppearance), nameof(GXAppearance.Id))]
        [ExcludeOpenApi(typeof(GXUser), nameof(GXUser.CreationTime), nameof(GXUser.Updated))]
        [ExcludeOpenApi(typeof(GXUserProperty), nameof(GXUserProperty.CreationTime), nameof(GXUserProperty.Updated))]
        public IEnumerable<GXUser>? Users { get; set; }
    }

    /// <summary>Response to adding or updating user accounts.</summary>
    [Description("Response to adding or updating user accounts.")]
    [DataContract]
    public class AddUserResponse
    {
        /// <summary>
        /// New users.
        /// </summary>
        [Description("New users.")]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public IEnumerable<GXUser>? Users { get; set; }
    }

    /// <summary>Requests deletion of the selected users.</summary>
    [Description("Requests deletion of the selected users.")]
    [DataContract]
    public class RemoveUser : IGXRequest<RemoveUserResponse>
    {
        /// <summary>Identifiers of the user records to delete.</summary>
        [Description("Identifiers of the user records to delete.")]
        [DataMember]
        public IEnumerable<string> Ids { get; set; } = default!;

        /// <summary>Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.</summary>
        /// <remarks>
        /// If false, marks the items as removed and retains their database records.
        /// </remarks>
        [Description("Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.")]
        [DataMember]
        [Required]
        public bool Delete { get; set; }
    }

    /// <summary>Response to deleting the selected users.</summary>
    [Description("Response to deleting the selected users.")]
    [DataContract]
    public class RemoveUserResponse
    {
    }
}
