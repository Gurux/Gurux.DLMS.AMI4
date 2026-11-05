// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

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

using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested device group information.</summary>
    [Description("Response containing the requested device group information.")]
    public class GetDeviceGroupResponse
    {
        /// <summary>
        /// Device group information.
        /// </summary>
        [Description("Device group information.")]
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


    /// <summary>Requests a list of device groups.</summary>
    [Description("Requests a list of device groups.")]
    [DataContract]
    public class ListDeviceGroups : IGXRequest<ListDeviceGroupsResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of device groups to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of device groups to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter device groups.
        /// </summary>
        [Description("Optional criteria used to filter device groups.")]
        [ExcludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Devices),
        nameof(GXDeviceGroup.UserGroups), nameof(GXDeviceGroup.AgentGroups),
            nameof(GXDeviceGroup.Gateways), nameof(GXDeviceGroup.Keys),
            nameof(GXDeviceGroup.Properties))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [ExcludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Description), nameof(GXDeviceGroup.ConcurrencyStamp))]
        public GXDeviceGroup? Filter { get; set; }

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

    /// <summary>Response containing the requested device groups.</summary>
    [Description("Response containing the requested device groups.")]
    [DataContract]
    public class ListDeviceGroupsResponse
    {
        /// <summary>
        /// List of device groups.
        /// </summary>
        [Description("List of device groups.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Devices),
        nameof(GXDeviceGroup.UserGroups), nameof(GXDeviceGroup.AgentGroups),
            nameof(GXDeviceGroup.Gateways), nameof(GXDeviceGroup.Keys),
            nameof(GXDeviceGroup.Properties))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public IEnumerable<GXDeviceGroup>? DeviceGroups { get; set; }

        /// <summary>
        /// Total number of matching device groups before paging.
        /// </summary>
        [Description("Total number of matching device groups before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests the addition of device groups.</summary>
    [Description("Requests the addition of device groups.")]
    [DataContract]
    public class AddDeviceGroup : IGXRequest<AddDeviceGroupResponse>
    {
        /// <summary>New device groups.</summary>
        [Description("New device groups.")]
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
        [ExcludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.CreationTime), nameof(GXDeviceGroup.Updated))]
        public IEnumerable<GXDeviceGroup> DeviceGroups { get; set; } = default!;
    }

    /// <summary>Response to adding device groups.</summary>
    [Description("Response to adding device groups.")]
    [DataContract]
    public class AddDeviceGroupResponse
    {
        /// <summary>Identifiers of the newly added device group records.</summary>
        [Description("Identifiers of the newly added device group records.")]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Requests deletion of the selected device groups.</summary>
    [Description("Requests deletion of the selected device groups.")]
    [DataContract]
    public class RemoveDeviceGroup : IGXRequest<RemoveDeviceGroupResponse>
    {
        /// <summary>Identifiers of the device group records to delete.</summary>
        [Description("Identifiers of the device group records to delete.")]
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

    /// <summary>Response to deleting the selected device groups.</summary>
    [Description("Response to deleting the selected device groups.")]
    [DataContract]
    public class RemoveDeviceGroupResponse
    {
    }
}
