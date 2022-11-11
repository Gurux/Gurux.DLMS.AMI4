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
using System.ComponentModel;
using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Shared.Rest
{

    /// <summary>
    /// Update agent.
    /// </summary>
    [DataContract]
    public class UpdateAgent : IGXRequest<UpdateAgentResponse>
    {
        /// <summary>
        /// Agent to add.
        /// </summary>
        [DataMember]
        public GXAgent[] Agents
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update reader agent.
    /// </summary>
    [DataContract]
    public class UpdateAgentResponse
    {
        /// <summary>
        /// New agent identifiers.
        /// </summary>
        [DataMember]
        public Guid[] AgentIds
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get list from agents.
    /// </summary>
    [DataContract]
    public class ListAgents : IGXRequest<ListAgentsResponse>
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
        /// Amount of the agents to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter agents.
        /// </summary>
        public GXAgent? Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Admin user can access agents from all users.
        /// </summary>
        /// <remarks>
        /// If true, agents from all users are retreaved, not just current user. 
        /// </remarks>
        public bool AllUsers
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get agents response.
    /// </summary>
    [DataContract]
    public class ListAgentsResponse
    {
        /// <summary>
        /// List of agents.
        /// </summary>
        [DataMember]
        public GXAgent[] Agents
        {
            get;
            set;
        }
        /// <summary>
        /// Total count of the agents.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get list from agent installers.
    /// </summary>
    [DataContract]
    public class ListAgentInstallers : IGXRequest<ListAgentInstallersResponse>
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
        /// Amount of the agent installers to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter agent installers.
        /// </summary>
        public GXAgent? Filter
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get agent installers response.
    /// </summary>
    [DataContract]
    public class ListAgentInstallersResponse
    {
        /// <summary>
        /// List of agent installers.
        /// </summary>
        [DataMember]
        public GXAgent[] Agents
        {
            get;
            set;
        }
        /// <summary>
        /// Total count of the agent installers.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove agents.
    /// </summary>
    [DataContract]
    public class AgentDelete : IGXRequest<AgentDeleteResponse>
    {
        /// <summary>
        /// Agent identifiers to remove.
        /// </summary>
        [DataMember]
        [Description("Agent identifiers to remove.")]
        public Guid[] Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove reased response.
    /// </summary>
    [DataContract]
    [Description("Remove reased response.")]
    public class AgentDeleteResponse
    {
    }

    /// <summary>
    /// Update agent state.
    /// </summary>
    [DataContract]
    public class UpdateAgentStatus
    {
        /// <summary>
        /// Agent ID.
        /// </summary>
        [DataMember]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Agent Status.
        /// </summary>
        [DataMember]
        public DTOs.Enums.AgentStatus Status
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Download new agent version.
    /// </summary>
    [DataContract]
    public class DownloadAgent : IGXRequest<DownloadAgentResponse>
    {
        /// <summary>
        /// Agents to update and the version number.
        /// </summary>
        [DataMember]
        public GXAgent? Agent
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update agent reply.
    /// </summary>
    [DataContract]
    public class DownloadAgentResponse
    {
        /// <summary>
        /// List of urls where agent can be loaded.
        /// </summary>
        [DataMember]
        public string[] Urls
        {
            get;
            set;
        }
    }
}
