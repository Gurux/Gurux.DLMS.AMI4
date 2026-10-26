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
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get gateway group.
    /// </summary>
    public class GetGatewayGroupResponse
    {
        /// <summary>
        /// Gateway group information.
        /// </summary>
        [IncludeOpenApi(typeof(GXGateway), nameof(GXGateway.Id),
                nameof(GXGateway.Name))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id),
                nameof(GXUserGroup.Name))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id),
                nameof(GXDeviceGroup.Name))]
        public GXGatewayGroup? Item { get; set; }
    }

    /// <summary>
    /// Get gateway group list.
    /// </summary>
    [DataContract]
    public class ListGatewayGroups : IGXRequest<ListGatewayGroupsResponse>
    {
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Maximum number of gateway groups to retrieve; zero disables paging.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter gateway groups.
        /// </summary>
        [ExcludeOpenApi(typeof(GXGatewayGroup), nameof(GXGatewayGroup.Gateways),
            nameof(GXGatewayGroup.UserGroups))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public GXGatewayGroup? Filter { get; set; }

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
    /// Get gateway groups response.
    /// </summary>
    [DataContract]
    public class ListGatewayGroupsResponse
    {
        /// <summary>
        /// List of gateway groups.
        /// </summary>
        [DataMember]
        [ExcludeOpenApi(typeof(GXGatewayGroup), nameof(GXGatewayGroup.Gateways),
            nameof(GXGatewayGroup.UserGroups))]
        public IEnumerable<GXGatewayGroup>? GatewayGroups { get; set; }

        /// <summary>
        /// Total number of matching gateway groups before paging.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>
    /// Add new gateway group.
    /// </summary>
    [DataContract]
    public class AddGatewayGroup : IGXRequest<AddGatewayGroupResponse>
    {
        /// <summary>
        /// New gateway group(s).
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXGateway), nameof(GXGateway.Id))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public IEnumerable<GXGatewayGroup> GatewayGroups { get; set; } = default!;
    }

    /// <summary>
    /// Add new gateway group response.
    /// </summary>
    [DataContract]
    public class AddGatewayGroupResponse
    {
        /// <summary>
        /// New gateway groups.
        /// </summary>
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>
    /// Remove gateway group.
    /// </summary>
    [DataContract]
    public class RemoveGatewayGroup : IGXRequest<RemoveGatewayGroupResponse>
    {
        /// <summary>
        /// Gateway group Ids to remove.
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
    /// Remove gateway group response.
    /// </summary>
    [DataContract]
    public class RemoveGatewayGroupResponse
    {
    }
}
