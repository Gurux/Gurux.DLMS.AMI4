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
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

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
        [ExcludeSwagger(typeof(GXDeviceGroupParameter), nameof(GXDeviceGroupParameter.DeviceGroup),
                nameof(GXDeviceGroupParameter.Module))]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public GXDeviceGroup Item
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
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
        nameof(GXDeviceGroup.UserGroups), nameof(GXDeviceGroup.AgentGroups)
            , nameof(GXDeviceGroup.Parameters))]
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
        nameof(GXDeviceGroup.UserGroups), nameof(GXDeviceGroup.AgentGroups)
            , nameof(GXDeviceGroup.Parameters))]
        public GXDeviceGroup[] DeviceGroups
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
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeSwagger(typeof(GXDevice), nameof(GXDevice.Id))]
        [ExcludeSwagger(typeof(GXDeviceGroupParameter),
            nameof(GXDeviceGroupParameter.Removed),
            nameof(GXDeviceGroupParameter.Updated),
            nameof(GXDeviceGroupParameter.CreationTime),
            nameof(GXDeviceGroupParameter.DeviceGroup), 
            nameof(GXDeviceGroupParameter.Module))]
        public GXDeviceGroup[] DeviceGroups
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
        public Guid[] Ids
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
    /// Remove device group response.
    /// </summary>
    [DataContract]
    public class RemoveDeviceGroupResponse
    {
    }
}
