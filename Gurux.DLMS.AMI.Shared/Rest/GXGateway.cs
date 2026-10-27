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
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;

using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested gateway information.</summary>
    [Description("Response containing the requested gateway information.")]
    public class GetGatewayResponse
    {
        /// <summary>
        /// Gateway information.
        /// </summary>
        [Description("Gateway information.")]
        [ExcludeOpenApi(typeof(GXGateway),
                nameof(GXGateway.Logs), nameof(GXGateway.ScriptMethod))]
        [IncludeOpenApi(typeof(GXGatewayGroup), nameof(GXGatewayGroup.Id)
            , nameof(GXGatewayGroup.Name)
            , nameof(GXGatewayGroup.Description))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id), nameof(GXDeviceGroup.Name))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id), nameof(GXDevice.Name))]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id), nameof(GXAgent.Name))]
        public GXGateway? Item { get; set; }
    }

    /// <summary>Requests changes to the specified gateways.</summary>
    [Description("Requests changes to the specified gateways.")]
    [DataContract]
    public class UpdateGateway : IGXRequest<UpdateGatewayResponse>
    {
        /// <summary>
        /// Gateway to add.
        /// </summary>
        [Description("Gateway to add.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXGateway), nameof(GXGateway.Creator),
            nameof(GXGateway.ScriptMethod),
            nameof(GXGateway.Logs), nameof(GXGateway.Detected),
            nameof(GXGateway.CreationTime), nameof(GXGateway.Updated))]
        [IncludeOpenApi(typeof(GXGatewayGroup), nameof(GXGatewayGroup.Id))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id))]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id))]
        [Required]
        public IEnumerable<GXGateway> Gateways { get; set; } = default!;
    }

    /// <summary>Response to updating the specified gateways.</summary>
    [Description("Response to updating the specified gateways.")]
    [DataContract]
    public class UpdateGatewayResponse
    {
        /// <summary>Identifiers of the added or updated gateway records.</summary>
        [Description("Identifiers of the added or updated gateway records.")]
        [DataMember]
        public IEnumerable<Guid>? Ids { get; set; }
    }

    /// <summary>Requests a list of gateways.</summary>
    [Description("Requests a list of gateways.")]
    [DataContract]
    public class ListGateways : IGXRequest<ListGatewaysResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of gateways to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of gateways to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter gateways.
        /// </summary>
        [Description("Optional criteria used to filter gateways.")]
        [ExcludeOpenApi(typeof(GXGateway), nameof(GXGateway.Creator),
            nameof(GXGateway.ScriptMethod),
            nameof(GXGateway.GatewayGroups), nameof(GXGateway.Logs))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id))]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id))]
        [ExcludeOpenApi(typeof(GXGateway),
            nameof(GXGateway.Description), nameof(GXGateway.DeviceGroups), nameof(GXGateway.Devices),
            nameof(GXGateway.TraceLevel), nameof(GXGateway.ConcurrencyStamp))]
        public GXGateway? Filter { get; set; }

        /// <summary>
        /// Admin user can access gateways from all users.
        /// </summary>
        /// <remarks>
        /// If true, gateways from all users are retrieved, not just current user.
        /// </remarks>
        [Description("Admin user can access gateways from all users.")]
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

    /// <summary>Response containing the requested gateways.</summary>
    [Description("Response containing the requested gateways.")]
    [DataContract]
    public class ListGatewaysResponse
    {
        /// <summary>
        /// List of gateways.
        /// </summary>
        [Description("List of gateways.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXGateway), nameof(GXGateway.Creator),
            nameof(GXGateway.ScriptMethod),
            nameof(GXGateway.GatewayGroups), nameof(GXGateway.Logs)
            , nameof(GXGateway.DeviceGroups), nameof(GXGateway.Devices))]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id))]
        public IEnumerable<GXGateway>? Gateways { get; set; }

        /// <summary>
        /// Total number of matching gateways before paging.
        /// </summary>
        [Description("Total number of matching gateways before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests deletion of the selected gateways.</summary>
    [Description("Requests deletion of the selected gateways.")]
    [DataContract]
    public class RemoveGateway : IGXRequest<RemoveGatewayResponse>
    {
        /// <summary>Identifiers of the gateway records to delete.</summary>
        [Description("Identifiers of the gateway records to delete.")]
        [DataMember]
        [Required]
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

    /// <summary>Response to deleting the selected gateways.</summary>
    [Description("Response to deleting the selected gateways.")]
    [DataContract]
    public class RemoveGatewayResponse
    {
    }

    /// <summary>Requests changes to the gateway status.</summary>
    [Description("Requests changes to the gateway status.")]
    [DataContract]
    public class UpdateGatewayStatus
    {
        /// <summary>Gateway identifier.</summary>
        [Description("Gateway identifier.")]
        [DataMember]
        public Guid Id { get; set; }

        /// <summary>
        /// Gateway Status.
        /// </summary>
        [Description("Gateway Status.")]
        [DataMember]
        public DTOs.Enums.GatewayStatus Status { get; set; }
    }
}
