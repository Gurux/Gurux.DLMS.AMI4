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
using Gurux.Service.Orm.Common;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;

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
        [ExcludeOpenApi(typeof(GXAgent),
                nameof(GXAgent.Template),
                nameof(GXAgent.ScriptMethods),
                nameof(GXAgent.Versions), nameof(GXAgent.Tasks),
                nameof(GXAgent.Logs))]
        [IncludeOpenApi(typeof(GXAgentGroup), nameof(GXAgentGroup.Id)
            , nameof(GXAgentGroup.Name)
            , nameof(GXAgentGroup.Description))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id)
            , nameof(GXUser.UserName))]
        public GXAgent? Item { get; set; }
    }

    /// <summary>
    /// Update agent.
    /// </summary>
    [DataContract]
    public class UpdateAgent : IGXRequest<UpdateAgentResponse>
    {
        /// <summary>
        /// Agents to add or update.
        /// </summary>
        [DataMember]
        [ExcludeOpenApi(typeof(GXAgent), nameof(GXAgent.Creator), nameof(GXAgent.Tasks),
            nameof(GXAgent.Logs), nameof(GXAgent.ScriptMethods)
            , nameof(GXAgent.LastRun), nameof(GXAgent.Detected)
            , nameof(GXAgent.CreationTime), nameof(GXAgent.Updated))]
        [IncludeOpenApi(typeof(GXAgentVersion), nameof(GXAgentVersion.Id))]
        [IncludeOpenApi(typeof(GXAgentGroup), nameof(GXAgentGroup.Id))]
        [Required]
        public IEnumerable<GXAgent> Agents { get; set; } = default!;
    }

    /// <summary>
    /// Identifiers returned after adding or updating agents.
    /// </summary>
    [DataContract]
    public class UpdateAgentResponse
    {
        /// <summary>
        /// Agent identifiers.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid>? AgentIds { get; set; }
    }

    /// <summary>
    /// Get list from agents.
    /// </summary>
    [DataContract]
    public class ListAgents : IGXRequest<ListAgentsResponse>
    {
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Maximum number of agents to retrieve; zero disables paging.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter agents.
        /// </summary>
        [ExcludeOpenApi(typeof(GXAgent), nameof(GXAgent.Creator), nameof(GXAgent.Tasks),
            nameof(GXAgent.AgentGroups), nameof(GXAgent.Logs), nameof(GXAgent.ScriptMethods),
            nameof(GXAgent.Versions), nameof(GXAgent.Template), nameof(GXAgent.ReaderSettings),
            nameof(GXAgent.ListenerSettings), nameof(GXAgent.NotifySettings), nameof(GXAgent.RawNotificationSettings))]
        public GXAgent? Filter { get; set; }

        /// <summary>
        /// Admin user can access agents from all users.
        /// </summary>
        /// <remarks>
        /// If true, agents from all users are retrieved, not just current user.
        /// </remarks>
        public bool AllUsers { get; set; }

        /// <summary>
        /// Selected extra information.
        /// </summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        public IEnumerable<string>? Select { get; set; }

        /// <summary>
        /// Name of the property to sort by.
        /// </summary>
        /// <remarks>
        /// Uses the endpoint's default ordering when not specified.
        /// </remarks>
        /// <seealso cref="Descending"/>
        public string? OrderBy { get; set; }

        /// <summary>
        /// Whether to sort results in descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        public bool Descending { get; set; }

        /// <summary>
        /// Identifiers to include in the results.
        /// </summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        public IEnumerable<Guid>? Exclude { get; set; }
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
        [ExcludeOpenApi(typeof(GXAgent), nameof(GXAgent.Creator), nameof(GXAgent.Tasks),
            nameof(GXAgent.AgentGroups), nameof(GXAgent.Logs), nameof(GXAgent.ScriptMethods),
            nameof(GXAgent.Versions))]
        public IEnumerable<GXAgent>? Agents { get; set; }

        /// <summary>
        /// Total number of matching agents before paging.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>
    /// Get list from agent installers.
    /// </summary>
    [DataContract]
    public class ListAgentInstallers : IGXRequest<ListAgentInstallersResponse>
    {
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Maximum number of agent installers to retrieve; zero disables paging.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter agent installers.
        /// </summary>
        [ExcludeOpenApi(typeof(GXAgent), nameof(GXAgent.Creator), nameof(GXAgent.Tasks),
      nameof(GXAgent.AgentGroups), nameof(GXAgent.Logs), nameof(GXAgent.ScriptMethods),
      nameof(GXAgent.Versions))]
        public GXAgent? Filter { get; set; }
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
        [ExcludeOpenApi(typeof(GXAgent), nameof(GXAgent.Creator), nameof(GXAgent.Tasks),
            nameof(GXAgent.AgentGroups), nameof(GXAgent.Logs), nameof(GXAgent.ScriptMethods),
            nameof(GXAgent.Versions))]
        public IEnumerable<GXAgent>? Agents { get; set; }
        /// <summary>
        /// Total number of matching agent installers before paging.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
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
        public IEnumerable<Guid> Ids { get; set; } = default!;

        /// <summary>
        /// Whether to permanently delete the items from the database.
        /// </summary>
        /// <remarks>
        /// If false, marks the items as removed and retains their database records.
        /// </remarks>
        [DataMember]
        public bool Delete { get; set; } = true;
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
        public Guid Id { get; set; }

        /// <summary>
        /// Agent Status.
        /// </summary>
        [DataMember]
        public DTOs.Enums.AgentStatus Status { get; set; }

        /// <summary>
        /// Optional data.
        /// </summary>
        /// <remarks>
        /// Serial ports are added to data field when ports are added or removed from the agent.
        /// </remarks>
        [DataMember]
        public string? Data { get; set; }
    }

    /// <summary>
    /// Install new agent version.
    /// </summary>
    [DataContract]
    public class InstallAgent
    {
        /// <summary>
        /// Agent IDs and updated version.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id), nameof(GXAgent.UpdateVersion))]
        public IEnumerable<GXAgent> Agents { get; set; } = default!;
    }

    /// <summary>
    /// Agent install reply.
    /// </summary>
    [DataContract]
    public class InstallAgentResponse
    {

    }

    /// <summary>
    /// Download new agent version.
    /// </summary>
    [DataContract]
    public class DownloadAgent : IGXRequest<DownloadAgentResponse>
    {
        /// <summary>
        /// Agent and version for which to obtain download URLs.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id), nameof(GXAgent.UpdateVersion))]
        public GXAgent? Agent { get; set; }
    }

    /// <summary>
    /// Update agent reply.
    /// </summary>
    [DataContract]
    public class DownloadAgentResponse
    {
        /// <summary>
        /// URLs from which the requested agent version can be downloaded.
        /// </summary>
        [DataMember]
        public IEnumerable<string>? Urls { get; set; }
    }

    /// <summary>
    /// Check if there are new agent versions available.
    /// </summary>
    [DataContract]
    public class CheckAgent : IGXRequest<CheckAgentResponse>
    {
    }

    /// <summary>
    /// Check agent versions reply.
    /// </summary>
    [DataContract]
    public class CheckAgentResponse
    {
        /// <summary>The completed background execution ID.</summary>
        [DataMember]
        public Guid RunId { get; set; }
    }

    /// <summary>
    /// Clear agent cache.
    /// </summary>
    [DataContract]
    public class AgentCacheClear : IGXRequest<AgentCacheClearResponse>
    {
        /// <summary>
        /// Agent identifiers to clear.
        /// </summary>
        [DataMember]
        [Required]
        public IEnumerable<Guid> Ids { get; set; } = default!;

        /// <summary>
        /// Names of the caches to clear.
        /// </summary>
        [DataMember]
        [Required]
        public IEnumerable<string> Names { get; set; } = default!;
    }

    /// <summary>
    /// Clear agent cache reply.
    /// </summary>
    [DataContract]
    public class AgentCacheClearResponse
    {
    }
}
