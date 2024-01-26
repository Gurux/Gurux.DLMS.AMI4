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

using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.Enums;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get Gateway log.
    /// </summary>
    public class GetGatewayLogResponse
    {
        /// <summary>
        /// Gateway log information.
        /// </summary>
        [IncludeSwagger(typeof(GXGateway),
                nameof(GXGateway.Id),
                nameof(GXGateway.Name))]
        public GXGatewayLog? Item
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get list from Gateway logs.
    /// </summary>
    [DataContract]
    public class ListGatewayLogs
    {
        /// <summary>
        /// Filter can be used to filter log example by date.
        /// </summary>
        [IncludeSwagger(typeof(GXGateway),
                nameof(GXGateway.Id),
                nameof(GXGateway.Name))]
        public GXGatewayLog? Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Admin user can access errors from all users.
        /// </summary>
        /// <remarks>
        /// If true, errors from all users are retreaved, not just current user. 
        /// </remarks>
        public bool AllUsers
        {
            get;
            set;
        }

        /// <summary>
        /// Start index.
        /// </summary>
        public int Index
        {
            get;
            set;
        }

        /// <summary>
        /// Amount of the logs to retreave.
        /// </summary>
        public int Count
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
        public string[]? Select
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
        /// Included Ids.
        /// </summary>
        /// <remarks>
        /// Included Ids can be used to get only part of large data.
        /// </remarks>
        public Guid[]? Included
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
    /// Get Gateway logs response.
    /// </summary>
    [DataContract]
    public class ListGatewayLogsResponse
    {
        /// <summary>
        /// List of Gateway logs.
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXGateway),
                nameof(GXGateway.Id),
                nameof(GXGateway.Name))]
        public GXGatewayLog[]? Logs
        {
            get;
            set;
        }
        /// <summary>
        /// Total amount of the logs.
        /// </summary>
        /// <remarks>
        /// With large databases reading the amount of the data can take a very long time.
        /// In those cases the count is set to -1.
        /// </remarks>
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new Gateway log.
    /// </summary>
    [DataContract]
    public class AddGatewayLog
    {
        /// <summary>
        /// New Gateway log.
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXGateway),
                nameof(GXGateway.Id))]
        public GXGatewayLog[] Logs
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new Gateway log response.
    /// </summary>
    [DataContract]
    public class AddGatewayLogResponse
    {
    }

    /// <summary>
    /// Clear logs. All logs are removed from the given user.
    /// </summary>
    [DataContract]
    public class ClearGatewayLogs
    {
        /// <summary>
        /// Gateway identifiers where logs are removed.
        /// </summary>
        [DataMember]
        public Guid[]? Gateways
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Clear logs response.
    /// </summary>
    [DataContract]
    public class ClearGatewayLogsResponse
    {
    }

    /// <summary>
    /// Close logs.
    /// </summary>
    [DataContract]
    public class CloseGatewayLog
    {
        /// <summary>
        /// Closed logs.
        /// </summary>
        [DataMember]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Guid[] Logs
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Close logs response.
    /// </summary>
    [DataContract]
    public class CloseGatewayLogResponse
    {
    }
}
