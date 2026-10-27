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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.Service.Orm.Common;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Requests the specified device trace information.</summary>
    [Description("Requests the specified device trace information.")]
    public class GetDeviceTrace
    {
        /// <summary>
        /// Device trace.
        /// </summary>
        [Description("Device trace.")]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id)
            , nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id)
            , nameof(GXDevice.Name))]
        public GXDeviceTrace? Item { get; set; }
    }

    /// <summary>Requests the addition of device traces.</summary>
    [Description("Requests the addition of device traces.")]
    [DataContract]
    public class AddDeviceTrace : IGXRequest<AddDeviceTraceResponse>
    {
        /// <summary>
        /// Added Device traces.
        /// </summary>
        [Description("Added Device traces.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id))]
        [ExcludeOpenApi(typeof(GXDeviceTrace), nameof(GXDeviceTrace.CreationTime))]
        public IEnumerable<GXDeviceTrace> Traces { get; set; } = default!;

        /// <summary>
        /// Trace type.
        /// </summary>
        [DataMember]
        [Description("Trace type.")]
        public string Type { get; set; } = default!;
    }

    /// <summary>Response to adding device traces.</summary>
    [Description("Response to adding device traces.")]
    [DataContract]
    public class AddDeviceTraceResponse
    {

    }

    /// <summary>Requests a list of device traces.</summary>
    [Description("Requests a list of device traces.")]
    [DataContract]
    public class ListDeviceTrace : IGXRequest<ListDeviceTraceResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DataMember]
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>
        /// Maximum device trace count to return.
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        [Description("Maximum device trace count to return.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter device traces.
        /// </summary>
        [Description("Optional criteria used to filter device traces.")]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id))]
        [ExcludeOpenApi(typeof(GXDeviceTrace), nameof(GXDeviceTrace.Frame))]
        public GXDeviceTrace? Filter { get; set; }


        /// <summary>
        /// Admin user can access traces from all users.
        /// </summary>
        /// <remarks>
        /// If true, traces from all users are retrieved, not just current user.
        /// </remarks>
        [Description("Admin user can access traces from all users.")]
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

    /// <summary>Response containing the requested device traces.</summary>
    [Description("Response containing the requested device traces.")]
    [DataContract]
    public class ListDeviceTraceResponse
    {
        /// <summary>
        /// List of device traces.
        /// </summary>
        [Description("List of device traces.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id), nameof(GXDevice.Name))]
        public IEnumerable<GXDeviceTrace> Traces { get; set; } = default!;

        /// <summary>
        /// Total number of matching trace items before paging.
        /// </summary>
        [Description("Total number of matching trace items before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>
    /// Clear device trace. All traces are removed from the given device.
    /// </summary>
    [Description("Clear device trace. All traces are removed from the given device.")]
    [DataContract]
    public class ClearDeviceTrace : IGXRequest<ClearDeviceTraceResponse>
    {
        /// <summary>
        /// Device identifiers where device traces are removed.
        /// </summary>
        [Description("Device identifiers where device traces are removed.")]
        [DataMember]
        public IEnumerable<Guid>? Ids { get; set; }
    }

    /// <summary>
    /// Clear device trace response.
    /// </summary>
    [Description("Clear device trace response.")]
    [DataContract]
    public class ClearDeviceTraceResponse
    {
    }
}
