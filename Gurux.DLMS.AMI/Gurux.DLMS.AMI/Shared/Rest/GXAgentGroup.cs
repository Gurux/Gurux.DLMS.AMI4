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
using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get agent group.
    /// </summary>
    public class GetAgentGroupResponse
    {
        /// <summary>
        /// Agent group information.
        /// </summary>
        [IncludeSwagger(typeof(GXAgent),nameof(GXAgent.Id),
                nameof(GXAgent.Name))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id),
                nameof(GXUserGroup.Name))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id),
                nameof(GXDeviceGroup.Name))]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public GXAgentGroup Item
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get agent group list.
    /// </summary>
    [DataContract]
    public class ListAgentGroups : IGXRequest<ListAgentGroupsResponse>
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
        /// Amount of the agent groups to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter agent groups.
        /// </summary>
        [ExcludeSwagger(typeof(GXAgentGroup), nameof(GXAgentGroup.Agents),
            nameof(GXAgentGroup.UserGroups), nameof(GXAgentGroup.DeviceGroups))]
        public GXAgentGroup? Filter
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
    }

    /// <summary>
    /// Get agent groups response.
    /// </summary>
    [DataContract]
    public class ListAgentGroupsResponse
    {
        /// <summary>
        /// List of agent groups.
        /// </summary>
        [DataMember]
        [ExcludeSwagger(typeof(GXAgentGroup), nameof(GXAgentGroup.Agents),
            nameof(GXAgentGroup.UserGroups), nameof(GXAgentGroup.DeviceGroups))]
        public GXAgentGroup[] AgentGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the agent groups.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new agent group.
    /// </summary>
    [DataContract]
    public class AddAgentGroup : IGXRequest<AddAgentGroupResponse>
    {
        /// <summary>
        /// New agent group(s).
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXAgent), nameof(GXAgent.Id))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        public GXAgentGroup[] AgentGroups
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new agent group response.
    /// </summary>
    [DataContract]
    public class AddAgentGroupResponse
    {
        /// <summary>
        /// New agent groups.
        /// </summary>
        public Guid[]? Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove agent group.
    /// </summary>
    [DataContract]
    public class RemoveAgentGroup : IGXRequest<RemoveAgentGroupResponse>
    {
        /// <summary>
        /// Agent group Ids to remove.
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
    /// Remove agent group response.
    /// </summary>
    [DataContract]
    public class RemoveAgentGroupResponse
    {
    }
}
