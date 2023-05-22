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
using Gurux.Common;
using System.Runtime.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;

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
        [IncludeSwagger(typeof(GXUser), nameof(GXUser.Id)
            , nameof(GXUser.UserName))]
        public GXDeviceTrace? Item
        {
            get;
            set;
        }
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
        public GXDeviceTrace[] Traces
        {
            get;
            set;
        }
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
        /// Start index.
        /// </summary>
        [DataMember]
        public int Index
        {
            get;
            set;
        }

        /// <summary>
        /// Maximum device trace count to return.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter device traces.
        /// </summary>
        public GXDeviceTrace? Filter
        {
            get;
            set;
        }


        /// <summary>
        /// Admin user can access traces from all users.
        /// </summary>
        /// <remarks>
        /// If true, traces from all users are retreaved, not just current user. 
        /// </remarks>
        public bool AllUsers
        {
            get;
            set;
        }

        /// <summary>
        /// Selected extra information.
        /// </summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        public TargetType Select
        {
            get;
            set;
        }

        /// <summary>
        /// Order by name.
        /// </summary>
        /// <remarks>
        /// Default order by is used if this is not set.
        /// </remarks>
        /// <seealso cref="Descending"/>
        public string? OrderBy
        {
            get;
            set;
        }

        /// <summary>
        /// Are values shown as descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        public bool Descending
        {
            get;
            set;
        }

        /// <summary>
        /// Excluded Ids.
        /// </summary>
        /// <remarks>
        /// Excluded Ids can be used to filter data.
        /// </remarks>
        public Guid[]? Exclude
        {
            get;
            set;
        }
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
        public GXDeviceTrace[] Traces
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the trace items.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
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
        public Guid[]? Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Clear device trace response.
    /// </summary>
    [DataContract]
    public class ClearDeviceTraceResponse
    {
    }
}
