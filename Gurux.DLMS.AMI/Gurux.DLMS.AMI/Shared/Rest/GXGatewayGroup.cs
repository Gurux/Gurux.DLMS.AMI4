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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.Common;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;

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
        [IncludeSwagger(typeof(GXGateway),nameof(GXGateway.Id),
                nameof(GXGateway.Name))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id),
                nameof(GXUserGroup.Name))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id),
                nameof(GXDeviceGroup.Name))]
        public GXGatewayGroup? Item
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get gateway group list.
    /// </summary>
    [DataContract]
    public class ListGatewayGroups : IGXRequest<ListGatewayGroupsResponse>
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
        /// Amount of the gateway groups to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter gateway groups.
        /// </summary>
        [ExcludeSwagger(typeof(GXGatewayGroup), nameof(GXGatewayGroup.Gateways),
            nameof(GXGatewayGroup.UserGroups))]
        public GXGatewayGroup? Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Admin user can access groups from all users.
        /// </summary>
        /// <remarks>
        /// If true, groups from all users are retreaved, not just current user. 
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
    /// Get gateway groups response.
    /// </summary>
    [DataContract]
    public class ListGatewayGroupsResponse
    {
        /// <summary>
        /// List of gateway groups.
        /// </summary>
        [DataMember]
        [ExcludeSwagger(typeof(GXGatewayGroup), nameof(GXGatewayGroup.Gateways),
            nameof(GXGatewayGroup.UserGroups))]
        public GXGatewayGroup[]? GatewayGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the gateway groups.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
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
        [IncludeSwagger(typeof(GXGateway), nameof(GXGateway.Id))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        public GXGatewayGroup[] GatewayGroups
        {
            get;
            set;
        }
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
        public Guid[]? Ids
        {
            get;
            set;
        }
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
        public Guid[] Ids
        {
            get;
            set;
        }

        /// <summary>
        /// Items are removed from the database.
        /// </summary>
        /// <remarks>
        /// If false, the Removed date is set for the items, but items are kept on the database.
        /// </remarks>
        [DataMember]
        [Required]
        public bool Delete
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove gateway group response.
    /// </summary>
    [DataContract]
    public class RemoveGatewayGroupResponse
    {
    }
}
