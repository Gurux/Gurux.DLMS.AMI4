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
using Gurux.Common.Db;
using System.Runtime.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs;
using System;
using Gurux.Common;

namespace Gurux.DLMS.AMI.Shared.Rest
{
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
        public GXAgentGroup[] AgentGroups
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
    }

    /// <summary>
    /// Remove agent group response.
    /// </summary>
    [DataContract]
    public class RemoveAgentGroupResponse
    {
    }
}
