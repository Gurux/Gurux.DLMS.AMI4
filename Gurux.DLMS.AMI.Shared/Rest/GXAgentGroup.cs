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
using System.Runtime.Serialization;
using Gurux.Service.Orm.Common;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get agent group.
    /// </summary>
    public class GetAgentGroupResponse
    {
        /// <summary>
        /// Agent group information.
        /// </summary>
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id),
                nameof(GXAgent.Name))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id),
                nameof(GXUserGroup.Name))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id),
                nameof(GXDeviceGroup.Name))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        public GXAgentGroup? Item { get; set; }
    }

    /// <summary>
    /// Get agent group list.
    /// </summary>
    [DataContract]
    public class ListAgentGroups : IGXRequest<ListAgentGroupsResponse>
    {
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Maximum number of agent groups to retrieve; zero disables paging.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter agent groups.
        /// </summary>
        [ExcludeOpenApi(typeof(GXAgentGroup), nameof(GXAgentGroup.Agents),
            nameof(GXAgentGroup.UserGroups), nameof(GXAgentGroup.DeviceGroups))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        public GXAgentGroup? Filter { get; set; }

        /// <summary>
        /// Admin user can access groups from all users.
        /// </summary>
        /// <remarks>
        /// If true, groups from all users are retrieved, not just current user.
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
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        public IEnumerable<Guid>? Exclude { get; set; }
    }

    /// <summary>
    /// Get agent groups response.
    /// </summary>
    [DataContract]
    public class ListAgentGroupsResponse
    {
        /// <summary>
        /// List of agent groups.
        /// </summary>
        [DataMember]
        [ExcludeOpenApi(typeof(GXAgentGroup), nameof(GXAgentGroup.Agents),
            nameof(GXAgentGroup.UserGroups), nameof(GXAgentGroup.DeviceGroups))]
        public IEnumerable<GXAgentGroup>? AgentGroups { get; set; }

        /// <summary>
        /// Total number of matching agent groups before paging.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>
    /// Add new agent group.
    /// </summary>
    [DataContract]
    public class AddAgentGroup : IGXRequest<AddAgentGroupResponse>
    {
        /// <summary>
        /// New agent group(s).
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public IEnumerable<GXAgentGroup>? AgentGroups { get; set; }
    }

    /// <summary>
    /// Add new agent group response.
    /// </summary>
    [DataContract]
    public class AddAgentGroupResponse
    {
        /// <summary>
        /// New agent group IDs.
        /// </summary>
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>
    /// Remove agent group.
    /// </summary>
    [DataContract]
    public class RemoveAgentGroup : IGXRequest<RemoveAgentGroupResponse>
    {
        /// <summary>
        /// Agent group Ids to remove.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;

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
    /// Remove agent group response.
    /// </summary>
    [DataContract]
    public class RemoveAgentGroupResponse
    {
    }
}
