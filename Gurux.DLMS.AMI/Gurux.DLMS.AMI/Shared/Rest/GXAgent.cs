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
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get agent.
    /// </summary>
    public class GetAgentResponse
    {
        /// <summary>
        /// Agent information.
        /// </summary>
        [ExcludeSwagger(typeof(GXAgent),
                nameof(GXAgent.Template),
                nameof(GXAgent.ScriptMethods),
                nameof(GXAgent.Versions), nameof(GXAgent.Tasks), 
                nameof(GXAgent.Logs))]
        [IncludeSwagger(typeof(GXAgentGroup), nameof(GXAgentGroup.Id)
            , nameof(GXAgentGroup.Name)
            , nameof(GXAgentGroup.Description))]
        [IncludeSwagger(typeof(GXUser), nameof(GXUser.Id)
            , nameof(GXUser.UserName))]
        public GXAgent? Item
        {
            get;
            set;
        }
    }

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
        [ExcludeSwagger(typeof(GXAgent), nameof(GXAgent.Creator), nameof(GXAgent.Tasks),
            nameof(GXAgent.Logs), nameof(GXAgent.ScriptMethods)
            , nameof(GXAgent.LastRun), nameof(GXAgent.Detected)
            , nameof(GXAgent.CreationTime), nameof(GXAgent.Updated))]
        [IncludeSwagger(typeof(GXAgentVersion), nameof(GXAgentVersion.Id))]
        [IncludeSwagger(typeof(GXAgentGroup), nameof(GXAgentGroup.Id))]
        [Required]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public GXAgent[] Agents
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
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
        public Guid[]? AgentIds
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
        [ExcludeSwagger(typeof(GXAgent), nameof(GXAgent.Creator), nameof(GXAgent.Tasks),
            nameof(GXAgent.AgentGroups), nameof(GXAgent.Logs), nameof(GXAgent.ScriptMethods),
            nameof(GXAgent.Versions), nameof(GXAgent.Template), nameof(GXAgent.ReaderSettings),
            nameof(GXAgent.ListenerSettings), nameof(GXAgent.NotifySettings))]
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
    /// Get agents response.
    /// </summary>
    [DataContract]
    public class ListAgentsResponse
    {
        /// <summary>
        /// List of agents.
        /// </summary>
        [DataMember]
        [ExcludeSwagger(typeof(GXAgent), nameof(GXAgent.Creator), nameof(GXAgent.Tasks),
            nameof(GXAgent.AgentGroups), nameof(GXAgent.Logs), nameof(GXAgent.ScriptMethods),
            nameof(GXAgent.Versions))]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public GXAgent[] Agents
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
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
        [ExcludeSwagger(typeof(GXAgent), nameof(GXAgent.Creator), nameof(GXAgent.Tasks),
      nameof(GXAgent.AgentGroups), nameof(GXAgent.Logs), nameof(GXAgent.ScriptMethods),
      nameof(GXAgent.Versions))]
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
        [ExcludeSwagger(typeof(GXAgent), nameof(GXAgent.Creator), nameof(GXAgent.Tasks),
            nameof(GXAgent.AgentGroups), nameof(GXAgent.Logs), nameof(GXAgent.ScriptMethods),
            nameof(GXAgent.Versions))]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public GXAgent[] Agents
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
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
    public class RemoveAgent : IGXRequest<RemoveAgentResponse>
    {
        /// <summary>
        /// Agent identifiers to remove.
        /// </summary>
        [DataMember]
        [Required]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Guid[] Ids
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
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
    /// Remove agent response.
    /// </summary>
    [DataContract]
    public class RemoveAgentResponse
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
        [ExcludeSwagger(typeof(GXAgent), nameof(GXAgent.Creator), nameof(GXAgent.Tasks),
            nameof(GXAgent.AgentGroups), nameof(GXAgent.Logs), nameof(GXAgent.ScriptMethods),
            nameof(GXAgent.Versions))]
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
