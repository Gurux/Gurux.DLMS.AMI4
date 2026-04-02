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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.Service.Orm.Common;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get IP address.
    /// </summary>
    public class GetIpAddressResponse
    {
        /// <summary>
        /// IP address information.
        /// </summary>        
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        public GXIpAddress? Item
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get list from IP addresss.
    /// </summary>
    [DataContract]
    public class ListIpAddress : IGXRequest<ListIpAddressResponse>
    {
        /// <summary>
        /// Start index.
        /// </summary>
        public int Index
        {
            get;
            set;

        }

        /// <summary>
        /// Amount of the IP addresss to retrieve.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter IP addresss.
        /// </summary>
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public GXIpAddress? Filter
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
        /// Order by.
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
    /// IpAddress items reply.
    /// </summary>
    [DataContract]
    public class ListIpAddressResponse
    {
        /// <summary>
        /// List of IP address items.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        public GXIpAddress[]? IpAddress
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the IP addresses.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update IP addresss.
    /// </summary>
    [DataContract]
    public class UpdateIpAddress : IGXRequest<UpdateIpAddressResponse>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UpdateIpAddress()
        {
            IpAddress = new List<GXIpAddress>();
        }

        /// <summary>
        /// IP Address to update.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public List<GXIpAddress> IpAddress
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update IP addresss reply.
    /// </summary>
    [DataContract]
    public class UpdateIpAddressResponse
    {
        /// <summary>
        /// New IP address identifiers.
        /// </summary>
        [DataMember]
        public Guid[]? Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Delete IP addresss.
    /// </summary>
    [DataContract]
    public class RemoveIpAddress : IGXRequest<RemoveIpAddressResponse>
    {
        /// <summary>
        /// Removed IP address identifiers.
        /// </summary>
        [DataMember]
        public Guid[]? Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Reply from Delete IP address.
    /// </summary>
    [DataContract]
    public class RemoveIpAddressResponse
    {
    }

    /// <summary>
    /// Clear IP address. All IP address are removed from the given user.
    /// </summary>
    [DataContract]
    public class ClearIpAddress
    {
        /// <summary>
        /// Agent identifiers where logs are removed.
        /// </summary>
        [DataMember]
        public Guid[]? Agents
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Clear IP address response.
    /// </summary>
    [DataContract]
    public class ClearIpAddressResponse
    {
    }
}
