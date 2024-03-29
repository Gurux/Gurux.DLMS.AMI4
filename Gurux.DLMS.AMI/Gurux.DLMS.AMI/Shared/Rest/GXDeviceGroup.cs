﻿//
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
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.User;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get device group.
    /// </summary>
    public class GetDeviceGroupResponse
    {
        /// <summary>
        /// Agent group information.
        /// </summary>
        [IncludeSwagger(typeof(GXDevice), nameof(GXDevice.Id),
                nameof(GXDevice.Name))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id),
                nameof(GXUserGroup.Name))]
        [IncludeSwagger(typeof(GXAgentGroup), nameof(GXAgentGroup.Id),
                nameof(GXAgentGroup.Name))]
        [IncludeSwagger(typeof(GXGatewayGroup), nameof(GXGatewayGroup.Id),
                nameof(GXGatewayGroup.Name))]
        [IncludeSwagger(typeof(GXKeyManagement), nameof(GXKeyManagement.Id),
                nameof(GXKeyManagement.Name))]
        [IncludeSwagger(typeof(GXGateway), nameof(GXGateway.Id), nameof(GXGateway.Name))]
        [ExcludeSwagger(typeof(GXDeviceGroupParameter), nameof(GXDeviceGroupParameter.DeviceGroup),
                nameof(GXDeviceGroupParameter.Module))]
        public GXDeviceGroup? Item
        {
            get;
            set;
        }
    }


    /// <summary>
    /// Get device group list.
    /// </summary>
    [DataContract]
    public class ListDeviceGroups : IGXRequest<ListDeviceGroupsResponse>
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
        /// Amount of the device groups to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter device groups.
        /// </summary>
        [ExcludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Devices),
        nameof(GXDeviceGroup.UserGroups), nameof(GXDeviceGroup.AgentGroups), 
            nameof(GXDeviceGroup.Gateways), nameof(GXDeviceGroup.Keys), 
            nameof(GXDeviceGroup.Parameters))]
        public GXDeviceGroup? Filter
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
    /// Get device groups response.
    /// </summary>
    [DataContract]
    public class ListDeviceGroupsResponse
    {
        /// <summary>
        /// List of device groups.
        /// </summary>
        [DataMember]
        [ExcludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Devices),
        nameof(GXDeviceGroup.UserGroups), nameof(GXDeviceGroup.AgentGroups), 
            nameof(GXDeviceGroup.Gateways), nameof(GXDeviceGroup.Keys), 
            nameof(GXDeviceGroup.Parameters))]
        public GXDeviceGroup[]? DeviceGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the device groups.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
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
        [IncludeSwagger(typeof(GXAgentGroup), nameof(GXAgentGroup.Id))]
        [IncludeSwagger(typeof(GXGateway), nameof(GXGateway.Id))]
        [IncludeSwagger(typeof(GXGatewayGroup), nameof(GXGatewayGroup.Id))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeSwagger(typeof(GXDevice), nameof(GXDevice.Id))]
        [ExcludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Keys))]
        [ExcludeSwagger(typeof(GXDeviceGroupParameter),
            nameof(GXDeviceGroupParameter.Removed),
            nameof(GXDeviceGroupParameter.Updated),
            nameof(GXDeviceGroupParameter.CreationTime),
            nameof(GXDeviceGroupParameter.DeviceGroup),
            nameof(GXDeviceGroupParameter.Module))]
        public GXDeviceGroup[]? DeviceGroups
        {
            get;
            set;
        }
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
        public Guid[]? Ids
        {
            get;
            set;
        }
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
        public Guid[]? Ids
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
    /// Remove device group response.
    /// </summary>
    [DataContract]
    public class RemoveDeviceGroupResponse
    {
    }
}
