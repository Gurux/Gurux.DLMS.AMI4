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
using System.Runtime.Serialization;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get device group.
    /// </summary>
    public class GetDeviceGroupResponse
    {
        /// <summary>
        /// Device group information.
        /// </summary>
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id),
                nameof(GXDevice.Name))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id),
                nameof(GXUserGroup.Name))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXAgentGroup), nameof(GXAgentGroup.Id),
                nameof(GXAgentGroup.Name))]
        [IncludeOpenApi(typeof(GXGatewayGroup), nameof(GXGatewayGroup.Id),
                nameof(GXGatewayGroup.Name))]
        [IncludeOpenApi(typeof(GXKeyManagement), nameof(GXKeyManagement.Id),
                nameof(GXKeyManagement.Name))]
        [IncludeOpenApi(typeof(GXGateway), nameof(GXGateway.Id), nameof(GXGateway.Name))]
        [ExcludeOpenApi(typeof(GXDeviceGroupProperty), nameof(GXDeviceGroupProperty.DeviceGroup),
                nameof(GXDeviceGroupProperty.Module))]
        public GXDeviceGroup? Item { get; set; }
    }


    /// <summary>
    /// Get device group list.
    /// </summary>
    [DataContract]
    public class ListDeviceGroups : IGXRequest<ListDeviceGroupsResponse>
    {
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Maximum number of device groups to retrieve; zero disables paging.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter device groups.
        /// </summary>
        [ExcludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Devices),
        nameof(GXDeviceGroup.UserGroups), nameof(GXDeviceGroup.AgentGroups),
            nameof(GXDeviceGroup.Gateways), nameof(GXDeviceGroup.Keys),
            nameof(GXDeviceGroup.Parameters))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public GXDeviceGroup? Filter { get; set; }


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
    /// Get device groups response.
    /// </summary>
    [DataContract]
    public class ListDeviceGroupsResponse
    {
        /// <summary>
        /// List of device groups.
        /// </summary>
        [DataMember]
        [ExcludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Devices),
        nameof(GXDeviceGroup.UserGroups), nameof(GXDeviceGroup.AgentGroups),
            nameof(GXDeviceGroup.Gateways), nameof(GXDeviceGroup.Keys),
            nameof(GXDeviceGroup.Parameters))]
        public IEnumerable<GXDeviceGroup>? DeviceGroups { get; set; }

        /// <summary>
        /// Total number of matching device groups before paging.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>
    /// Add new device group.
    /// </summary>
    [DataContract]
    public class AddDeviceGroup : IGXRequest<AddDeviceGroupResponse>
    {
        /// <summary>
        /// New device group(s).
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXAgentGroup), nameof(GXAgentGroup.Id))]
        [IncludeOpenApi(typeof(GXGateway), nameof(GXGateway.Id))]
        [IncludeOpenApi(typeof(GXGatewayGroup), nameof(GXGatewayGroup.Id))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id))]
        [ExcludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Keys))]
        [ExcludeOpenApi(typeof(GXDeviceGroupProperty),
            nameof(GXDeviceGroupProperty.Removed),
            nameof(GXDeviceGroupProperty.Updated),
            nameof(GXDeviceGroupProperty.CreationTime),
            nameof(GXDeviceGroupProperty.DeviceGroup),
            nameof(GXDeviceGroupProperty.Module))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public IEnumerable<GXDeviceGroup> DeviceGroups { get; set; } = default!;
    }

    /// <summary>
    /// Add new device group response.
    /// </summary>
    [DataContract]
    public class AddDeviceGroupResponse
    {
        /// <summary>
        /// New device groups.
        /// </summary>
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>
    /// Remove device group.
    /// </summary>
    [DataContract]
    public class RemoveDeviceGroup : IGXRequest<RemoveDeviceGroupResponse>
    {
        /// <summary>
        /// User group Ids to remove.
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
    /// Remove device group response.
    /// </summary>
    [DataContract]
    public class RemoveDeviceGroupResponse
    {
    }
}
