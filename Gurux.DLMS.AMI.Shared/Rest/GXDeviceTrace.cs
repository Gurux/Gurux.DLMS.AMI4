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
    /// <summary>
    /// Get device trace.
    /// </summary>
    public class GetDeviceTrace
    {
        /// <summary>
        /// Device trace.
        /// </summary>
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id)
            , nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id)
            , nameof(GXDevice.Name))]
        public GXDeviceTrace? Item { get; set; }
    }

    /// <summary>
    /// Adds a new device trace.
    /// </summary>
    [DataContract]
    public class AddDeviceTrace : IGXRequest<AddDeviceTraceResponse>
    {
        /// <summary>
        /// Added Device traces.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id))]
        public IEnumerable<GXDeviceTrace> Traces { get; set; } = default!;

        /// <summary>
        /// Trace type.
        /// </summary>
        [DataMember]
        [Description("Trace type.")]
        public string Type { get; set; } = default!;
    }

    /// <summary>
    /// Add device trace response.
    /// </summary>
    [DataContract]
    public class AddDeviceTraceResponse
    {

    }

    /// <summary>
    /// Get list from the device traces.
    /// </summary>
    [DataContract]
    public class ListDeviceTrace : IGXRequest<ListDeviceTraceResponse>
    {
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        [DataMember]
        public int Index { get; set; }

        /// <summary>
        /// Maximum device trace count to return.
        /// </summary>
        [DataMember]
        public int Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter device traces.
        /// </summary>
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id))]
        public GXDeviceTrace? Filter { get; set; }


        /// <summary>
        /// Admin user can access traces from all users.
        /// </summary>
        /// <remarks>
        /// If true, traces from all users are retrieved, not just current user.
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
    /// List device trace response.
    /// </summary>
    [DataContract]
    public class ListDeviceTraceResponse
    {
        /// <summary>
        /// List of device traces.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id), nameof(GXDevice.Name))]
        public IEnumerable<GXDeviceTrace> Traces { get; set; } = default!;

        /// <summary>
        /// Total number of matching trace items before paging.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>
    /// Clear device trace. All traces are removed from the given device.
    /// </summary>
    [DataContract]
    public class ClearDeviceTrace : IGXRequest<ClearDeviceTraceResponse>
    {
        /// <summary>
        /// Device identifiers where device traces are removed.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid>? Ids { get; set; }
    }

    /// <summary>
    /// Clear device trace response.
    /// </summary>
    [DataContract]
    public class ClearDeviceTraceResponse
    {
    }
}
