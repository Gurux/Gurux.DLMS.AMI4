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

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get user.
    /// </summary>
    public class GetUserResponse
    {
        /// <summary>
        /// User account information.
        /// </summary>
        [ExcludeOpenApi(typeof(GXUser),
            nameof(GXUser.Password),
            nameof(GXUser.PasswordHash),
            nameof(GXUser.SecurityStamp),
            nameof(GXUser.Actions),
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
        public GXUser? Item { get; set; }
    }

    /// <summary>
    /// Get user list.
    /// </summary>
    [DataContract]
    public class ListUsers : IGXRequest<ListUsersResponse>
    {
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Maximum number of users to retrieve; zero disables paging.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter users.
        /// </summary>
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
        public GXUser? Filter { get; set; }


        /// <summary>
        /// Admin user can access all users.
        /// </summary>
        /// <remarks>
        /// If true, all users are retrieved, not just current user.
        /// </remarks>
        public bool AllUsers { get; set; }

        /// <summary>
        /// Selected extra information.
        /// </summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        public IEnumerable<string>? Select { get; set; }

        /// <summary>
        /// Name of the property to sort by.
        /// </summary>
        /// <remarks>
        /// Uses the endpoint's default ordering when not specified.
        /// </remarks>
        /// <seealso cref="Descending"/>
        public string? OrderBy { get; set; }

        /// <summary>
        /// Whether to sort results in descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        public bool Descending { get; set; }

        /// <summary>
        /// Identifiers to include in the results.
        /// </summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        public IEnumerable<string>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        public IEnumerable<string>? Exclude { get; set; }
    }

    /// <summary>
    /// Get users response.
    /// </summary>
    [DataContract]
    [Description("Get users response.")]
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
        public IEnumerable<GXUser>? Users { get; set; }

        /// <summary>
        /// Total number of matching users before paging.
        /// </summary>
        [DataMember]
        [Description("Total count of the users.")]
        public int Count { get; set; }
    }

    /// <summary>
    /// Add or update users.
    /// </summary>
    [DataContract]
    public class AddUser : IGXRequest<AddUserResponse>
    {
        /// <summary>
        /// Users to add or update.
        /// </summary>
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
        public IEnumerable<GXUser>? Users { get; set; }
    }

    /// <summary>
    /// Add or update users response.
    /// </summary>
    [DataContract]
    public class AddUserResponse
    {
        /// <summary>
        /// New users.
        /// </summary>
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public IEnumerable<GXUser>? Users { get; set; }
    }

    /// <summary>
    /// Remove user.
    /// </summary>
    [DataContract]
    public class RemoveUser : IGXRequest<RemoveUserResponse>
    {
        /// <summary>
        /// User Ids to remove.
        /// </summary>
        [DataMember]
        public IEnumerable<string> Ids { get; set; } = default!;

        /// <summary>
        /// Whether to permanently delete the items from the database.
        /// </summary>
        /// <remarks>
        /// If false, marks the items as removed and retains their database records.
        /// </remarks>
        [DataMember]
        [Required]
        public bool Delete { get; set; }
    }

    /// <summary>
    /// Remove user response.
    /// </summary>
    [DataContract]
    public class RemoveUserResponse
    {
    }
}
