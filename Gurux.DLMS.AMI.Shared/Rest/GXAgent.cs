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

using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested agent information.</summary>
    [Description("Response containing the requested agent information.")]
    public class GetAgentResponse
    {
        /// <summary>
        /// Agent information.
        /// </summary>
        [Description("Agent information.")]
        [ExcludeOpenApi(typeof(GXAgent),
                nameof(GXAgent.ScriptMethods),
                nameof(GXAgent.Properties), nameof(GXAgent.Tasks),
                nameof(GXAgent.Logs))]
        [ExcludeOpenApi(typeof(GXAgentGroup), nameof(GXAgentGroup.Agents),
            nameof(GXAgentGroup.UserGroups), nameof(GXAgentGroup.DeviceGroups))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id)
            , nameof(GXUser.UserName))]
        public GXAgent? Item { get; set; }
    }

    /// <summary>Requests changes to the specified agents.</summary>
    [Description("Requests changes to the specified agents.")]
    [DataContract]
    public class UpdateAgent : IGXRequest<UpdateAgentResponse>
    {
        /// <summary>
        /// Agents to add or update.
        /// </summary>
        [Description("Agents to add or update.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXAgent), nameof(GXAgent.Creator), nameof(GXAgent.Tasks),
            nameof(GXAgent.Logs), nameof(GXAgent.ScriptMethods)
            , nameof(GXAgent.LastRun), nameof(GXAgent.Detected)
            , nameof(GXAgent.CreationTime), nameof(GXAgent.Updated))]
        [IncludeOpenApi(typeof(GXAgentVersion), nameof(GXAgentVersion.Id))]
        [IncludeOpenApi(typeof(GXAgentGroup), nameof(GXAgentGroup.Id))]
        [ExcludeOpenApi(typeof(GXAgent), nameof(GXAgent.Properties))]
        [Required]
        public IEnumerable<GXAgent> Agents { get; set; } = default!;
    }

    /// <summary>Response to updating the specified agents.</summary>
    [Description("Response to updating the specified agents.")]
    [DataContract]
    public class UpdateAgentResponse
    {
        /// <summary>
        /// Agent identifiers.
        /// </summary>
        [Description("Agent identifiers.")]
        [DataMember]
        public IEnumerable<Guid>? AgentIds { get; set; }
    }

    /// <summary>Requests a list of agents.</summary>
    [Description("Requests a list of agents.")]
    [DataContract]
    public class ListAgents : IGXRequest<ListAgentsResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of agents to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of agents to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter agents.
        /// </summary>
        [Description("Optional criteria used to filter agents.")]
        [ExcludeOpenApi(typeof(GXAgent), nameof(GXAgent.Creator), nameof(GXAgent.Tasks),
            nameof(GXAgent.AgentGroups), nameof(GXAgent.Logs), nameof(GXAgent.ScriptMethods),
            nameof(GXAgent.Versions), nameof(GXAgent.Template), nameof(GXAgent.ReaderSettings),
            nameof(GXAgent.ListenerSettings), nameof(GXAgent.NotifySettings), nameof(GXAgent.RawNotificationSettings))]
        [ExcludeOpenApi(typeof(GXAgent), nameof(GXAgent.Properties))]
        [ExcludeOpenApi(typeof(GXAgent),
            nameof(GXAgent.CatalogId), nameof(GXAgent.Description), nameof(GXAgent.GatewaySettings),
            nameof(GXAgent.TraceLevel), nameof(GXAgent.ConcurrencyStamp), nameof(GXAgent.SerialPorts),
            nameof(GXAgent.SerialPort), nameof(GXAgent.CacheExpiration))]
        public GXAgent? Filter { get; set; }

        /// <summary>Gets or sets inclusive date and time ranges for properties of the filter type.</summary>
        /// <remarks>
        /// Ranges are combined with each other and with <see cref="Filter"/> using AND.
        /// Specify a public date and time property name. Null or an empty collection
        /// applies no additional date restrictions. Filtering precedes counting and paging.
        /// </remarks>
        [DataMember]
        [Description("Inclusive date and time ranges combined with the other filters using AND.")]
        public IEnumerable<GXDateTimeFilter>? DateFilters { get; set; }

        /// <summary>
        /// Admin user can access agents from all users.
        /// </summary>
        /// <remarks>
        /// If true, agents from all users are retrieved, not just current user.
        /// </remarks>
        [Description("Admin user can access agents from all users.")]
        public bool AllUsers { get; set; }

        /// <summary>Additional related information to include in the response. Reserved for future use.</summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        [Description("Additional related information to include in the response. Reserved for future use.")]
        public IEnumerable<string>? Select { get; set; }

        /// <summary>Name of the property to sort by. If omitted, the endpoint uses its default ordering.</summary>
        /// <remarks>
        /// Uses the endpoint's default ordering when not specified.
        /// </remarks>
        /// <seealso cref="Descending"/>
        [Description("Name of the property to sort by. If omitted, the endpoint uses its default ordering.")]
        public string? OrderBy { get; set; }

        /// <summary>
        /// Whether to sort results in descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        [Description("Whether to sort results in descending order.")]
        public bool Descending { get; set; }

        /// <summary>Identifiers to include in the results. A nonempty collection restricts results to these identifiers.</summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        [Description("Identifiers to include in the results. A nonempty collection restricts results to these identifiers.")]
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        [Description("Identifiers to exclude from the results.")]
        public IEnumerable<Guid>? Exclude { get; set; }
    }

    /// <summary>Response containing the requested agents.</summary>
    [Description("Response containing the requested agents.")]
    [DataContract]
    public class ListAgentsResponse
    {
        /// <summary>
        /// List of agents.
        /// </summary>
        [Description("List of agents.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXAgent), nameof(GXAgent.Creator), nameof(GXAgent.Tasks),
            nameof(GXAgent.AgentGroups), nameof(GXAgent.Logs), nameof(GXAgent.ScriptMethods),
            nameof(GXAgent.Versions), nameof(GXAgent.Properties), nameof(GXAgent.Template),
            nameof(GXAgent.ReaderSettings), nameof(GXAgent.ListenerSettings),
            nameof(GXAgent.NotifySettings), nameof(GXAgent.RawNotificationSettings))]
        public IEnumerable<GXAgent>? Agents { get; set; }

        /// <summary>
        /// Total number of matching agents before paging.
        /// </summary>
        [Description("Total number of matching agents before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests a list of agent installers.</summary>
    [Description("Requests a list of agent installers.")]
    [DataContract]
    public class ListAgentInstallers : IGXRequest<ListAgentInstallersResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of agent installers to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of agent installers to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter agent installers.
        /// </summary>
        [Description("Optional criteria used to filter agent installers.")]
        [ExcludeOpenApi(typeof(GXAgent), nameof(GXAgent.Creator), nameof(GXAgent.Tasks),
      nameof(GXAgent.AgentGroups), nameof(GXAgent.Logs), nameof(GXAgent.ScriptMethods),
      nameof(GXAgent.Properties))]
        [IncludeOpenApi(typeof(GXAgentVersion), nameof(GXAgentVersion.Number),
            nameof(GXAgentVersion.Removed))]
        [ExcludeOpenApi(typeof(GXAgent),
            nameof(GXAgent.CatalogId), nameof(GXAgent.Description), nameof(GXAgent.ReaderSettings),
            nameof(GXAgent.ListenerSettings), nameof(GXAgent.NotifySettings), nameof(GXAgent.RawNotificationSettings),
            nameof(GXAgent.GatewaySettings), nameof(GXAgent.TraceLevel), nameof(GXAgent.ConcurrencyStamp),
            nameof(GXAgent.SerialPorts), nameof(GXAgent.SerialPort), nameof(GXAgent.CacheExpiration))]
        public GXAgent? Filter { get; set; }

        /// <summary>Gets or sets inclusive date and time ranges for properties of the filter type.</summary>
        /// <remarks>
        /// Ranges are combined with each other and with <see cref="Filter"/> using AND.
        /// Specify a public date and time property name. Null or an empty collection
        /// applies no additional date restrictions. Filtering precedes counting and paging.
        /// </remarks>
        [DataMember]
        [Description("Inclusive date and time ranges combined with the other filters using AND.")]
        public IEnumerable<GXDateTimeFilter>? DateFilters { get; set; }
    }

    /// <summary>Response containing the requested agent installers.</summary>
    [Description("Response containing the requested agent installers.")]
    [DataContract]
    public class ListAgentInstallersResponse
    {
        /// <summary>
        /// List of agent installers.
        /// </summary>
        [Description("List of agent installers.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXAgent), nameof(GXAgent.Creator), nameof(GXAgent.Tasks),
            nameof(GXAgent.AgentGroups), nameof(GXAgent.Logs), nameof(GXAgent.ScriptMethods),
            nameof(GXAgent.Properties))]
        public IEnumerable<GXAgent>? Agents { get; set; }
        /// <summary>
        /// Total number of matching agent installers before paging.
        /// </summary>
        [Description("Total number of matching agent installers before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests deletion of the selected agents.</summary>
    [Description("Requests deletion of the selected agents.")]
    [DataContract]
    public class RemoveAgent : IGXRequest<RemoveAgentResponse>
    {
        /// <summary>Identifiers of the agent records to delete.</summary>
        [Description("Identifiers of the agent records to delete.")]
        [DataMember]
        [Required]
        public IEnumerable<Guid> Ids { get; set; } = default!;

        /// <summary>Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.</summary>
        /// <remarks>
        /// If false, marks the items as removed and retains their database records.
        /// </remarks>
        [Description("Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.")]
        [DataMember]
        public bool Delete { get; set; }
    }

    /// <summary>Response to deleting the selected agents.</summary>
    [Description("Response to deleting the selected agents.")]
    [DataContract]
    public class RemoveAgentResponse
    {
    }

    /// <summary>Requests changes to the agent status.</summary>
    [Description("Requests changes to the agent status.")]
    [DataContract]
    public class UpdateAgentStatus
    {
        /// <summary>Agent identifier.</summary>
        [Description("Agent identifier.")]
        [DataMember]
        public Guid Id { get; set; }

        /// <summary>
        /// Agent Status.
        /// </summary>
        [Description("Agent Status.")]
        [DataMember]
        public DTOs.Enums.AgentStatus Status { get; set; }

        /// <summary>
        /// Optional data.
        /// </summary>
        /// <remarks>
        /// Serial ports are added to data field when ports are added or removed from the agent.
        /// </remarks>
        [Description("Optional data.")]
        [DataMember]
        public string? Data { get; set; }
    }

    /// <summary>
    /// Install new agent version.
    /// </summary>
    [Description("Install new agent version.")]
    [DataContract]
    public class InstallAgent
    {
        /// <summary>Agent identifiers and updated version.</summary>
        [Description("Agent identifiers and updated version.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id), nameof(GXAgent.UpdateVersion))]
        public IEnumerable<GXAgent> Agents { get; set; } = default!;
    }

    /// <summary>
    /// Agent install reply.
    /// </summary>
    [Description("Agent install reply.")]
    [DataContract]
    public class InstallAgentResponse
    {

    }

    /// <summary>
    /// Download new agent version.
    /// </summary>
    [Description("Download new agent version.")]
    [DataContract]
    public class DownloadAgent : IGXRequest<DownloadAgentResponse>
    {
        /// <summary>
        /// Agent and version for which to obtain download URLs.
        /// </summary>
        [Description("Agent and version for which to obtain download URLs.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id), nameof(GXAgent.UpdateVersion))]
        public GXAgent? Agent { get; set; }
    }

    /// <summary>Response containing the requested agent download information.</summary>
    [Description("Response containing the requested agent download information.")]
    [DataContract]
    public class DownloadAgentResponse
    {
        /// <summary>
        /// URLs from which the requested agent version can be downloaded.
        /// </summary>
        [Description("URLs from which the requested agent version can be downloaded.")]
        [DataMember]
        public IEnumerable<string>? Urls { get; set; }
    }

    /// <summary>
    /// Check if there are new agent versions available.
    /// </summary>
    [Description("Check if there are new agent versions available.")]
    [DataContract]
    public class CheckAgent : IGXRequest<CheckAgentResponse>
    {
    }

    /// <summary>
    /// Check agent versions reply.
    /// </summary>
    [Description("Check agent versions reply.")]
    [DataContract]
    public class CheckAgentResponse
    {
        /// <summary>The completed background execution identifier.</summary>
        [Description("The completed background execution identifier.")]
        [DataMember]
        public Guid RunId { get; set; }
    }

    /// <summary>
    /// Clear agent cache.
    /// </summary>
    [Description("Clear agent cache.")]
    [DataContract]
    public class AgentCacheClear : IGXRequest<AgentCacheClearResponse>
    {
        /// <summary>
        /// Agent identifiers to clear.
        /// </summary>
        [Description("Agent identifiers to clear.")]
        [DataMember]
        [Required]
        public IEnumerable<Guid> Ids { get; set; } = default!;

        /// <summary>
        /// Names of the caches to clear.
        /// </summary>
        [Description("Names of the caches to clear.")]
        [DataMember]
        [Required]
        public IEnumerable<string> Names { get; set; } = default!;
    }

    /// <summary>
    /// Clear agent cache reply.
    /// </summary>
    [Description("Clear agent cache reply.")]
    [DataContract]
    public class AgentCacheClearResponse
    {
    }
}
