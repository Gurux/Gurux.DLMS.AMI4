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

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get gateway.
    /// </summary>
    public class GetGatewayResponse
    {
        /// <summary>
        /// Gateway information.
        /// </summary>
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

    /// <summary>
    /// Update gateway.
    /// </summary>
    [DataContract]
    public class UpdateGateway : IGXRequest<UpdateGatewayResponse>
    {
        /// <summary>
        /// Gateway to add.
        /// </summary>
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

    /// <summary>
    /// Update reader gateway.
    /// </summary>
    [DataContract]
    public class UpdateGatewayResponse
    {
        /// <summary>
        /// Gateway identifiers.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid>? Ids { get; set; }
    }

    /// <summary>
    /// Get list from gateways.
    /// </summary>
    [DataContract]
    public class ListGateways : IGXRequest<ListGatewaysResponse>
    {
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Maximum number of gateways to retrieve; zero disables paging.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter gateways.
        /// </summary>
        [ExcludeOpenApi(typeof(GXGateway), nameof(GXGateway.Creator),
            nameof(GXGateway.ScriptMethod),
            nameof(GXGateway.GatewayGroups), nameof(GXGateway.Logs))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id))]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id))]
        public GXGateway? Filter { get; set; }

        /// <summary>
        /// Admin user can access gateways from all users.
        /// </summary>
        /// <remarks>
        /// If true, gateways from all users are retrieved, not just current user.
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
    /// List gateways response.
    /// </summary>
    [DataContract]
    public class ListGatewaysResponse
    {
        /// <summary>
        /// List of gateways.
        /// </summary>
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
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>
    /// Remove gateways.
    /// </summary>
    [DataContract]
    public class RemoveGateway : IGXRequest<RemoveGatewayResponse>
    {
        /// <summary>
        /// Gateway identifiers to remove.
        /// </summary>
        [DataMember]
        [Required]
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
    /// Remove gateway response.
    /// </summary>
    [DataContract]
    public class RemoveGatewayResponse
    {
    }

    /// <summary>
    /// Update gateway state.
    /// </summary>
    [DataContract]
    public class UpdateGatewayStatus
    {
        /// <summary>
        /// Gateway ID.
        /// </summary>
        [DataMember]
        public Guid Id { get; set; }

        /// <summary>
        /// Gateway Status.
        /// </summary>
        [DataMember]
        public DTOs.Enums.GatewayStatus Status { get; set; }
    }
}
