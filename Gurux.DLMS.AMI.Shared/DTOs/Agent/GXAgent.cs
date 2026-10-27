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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Agent
{
    /// <summary>An agent that communicates with devices and reports its connection and execution state.</summary>
    [Description("An agent that communicates with devices and reports its connection and execution state.")]
    public class GXAgent : GXTableBase, IUnique<Guid>
    {
        /// <summary>Initializes a new instance of GXAgent.</summary>
        [Description("Initializes a new instance of GXAgent.")]
        public GXAgent()
        {
        }


        /// <summary>Initializes a new instance of GXAgent.</summary>
        /// <remarks>
        /// This constructor is called when a new agent is created. It will create all required collections.
        /// </remarks>
        /// <param name="name">Agent name.</param>
        [Description("Initializes a new instance of GXAgent.")]
        public GXAgent(string? name)
        {
            Name = name;
            Tasks = [];
            AgentGroups = [];
            Logs = [];
            Versions = [];
            ScriptMethods = [];
            Concurrently = true;
            TraceLevel = System.Diagnostics.TraceLevel.Error;
        }

        /// <summary>
        /// Agent identifier.
        /// </summary>
        [Description("Agent identifier.")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>Catalog product identity for an agent installer template.</summary>
        [Description("Catalog product identity for an agent installer template.")]
        [DataMember, StringLength(128), Index(Unique = false)]
        public string? CatalogId { get; set; }

        /// <summary>
        /// The name of the agent.
        /// </summary>
        [Description("The name of the agent.")]
        [StringLength(128, ErrorMessage = "Name is too long.")]
        [Index(false)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Name { get; set; }

        /// <summary>
        /// Agent type.
        /// </summary>
        [Description("Agent type.")]
        [Filter(FilterType.Exact)]
        [DefaultValue(0)]
        [IsRequired]
        public byte? Type { get; set; }

        /// <summary>Whether the agent is enabled.</summary>
        [Description("Whether the agent is enabled.")]
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Enabled { get; set; }

        /// <summary>Description of the agent.</summary>
        [DataMember]
        [StringLength(256)]
        [Description("Description of the agent.")]
        //Filter uses default value.
        [DefaultValue(null)]
        public string? Description { get; set; }

        /// <summary>
        /// The creator of the agent.
        /// </summary>
        [Description("The creator of the agent.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GXUser? Creator { get; set; }

        /// <summary>
        /// Agent template settings are used to shown download information.
        /// </summary>
        [Description("Agent template settings are used to shown download information.")]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Template { get; set; }

        /// <summary>
        /// Executed tasks.
        /// </summary>
        [Description("Executed tasks.")]
        [DataMember]
        [ForeignKey(typeof(GXTask))]
        [Filter(FilterType.Contains)]
        public List<GXTask>? Tasks { get; set; }

        /// <summary>
        /// List of agent groups where this agent belongs.
        /// </summary>
        [Description("List of agent groups where this agent belongs.")]
        [DataMember,
            ForeignKey(typeof(GXAgentGroup), typeof(GXAgentGroupAgent))]
        [Filter(FilterType.Contains)]
        public List<GXAgentGroup>? AgentGroups { get; set; }

        /// <summary>
        /// Agent logs.
        /// </summary>
        [Description("Agent logs.")]
        [DataMember, Ignore(IgnoreType.Db)]
        [Filter(FilterType.Contains)]
        public List<GXLog>? Logs { get; set; }

        /// <summary>
        /// Reader settings.
        /// </summary>
        [Description("Reader settings.")]
        [DefaultValue(null)]
        public string? ReaderSettings { get; set; }

        /// <summary>
        /// Listener settings. Agent waits server to connect for the listener.
        /// </summary>
        [Description("Listener settings. Agent waits server to connect for the listener.")]
        [DefaultValue(null)]
        public string? ListenerSettings { get; set; }

        /// <summary>
        /// Notification settings. Agent waits notification, event or push messages to this port.
        /// </summary>
        [Description("Notification settings. Agent waits notification, event or push messages to this port.")]
        [DefaultValue(null)]
        public string? NotifySettings { get; set; }

        /// <summary>JSON settings for parsing raw notifications retrieved from the server.</summary>
        /// <remarks>
        /// Serialized <see cref="Gurux.DLMS.AMI.Shared.RawNotificationSettings"/> for the processing agent.
        /// Independent of NotifySettings, which configures the transport receiving meter notifications.
        /// </remarks>
        [Description("JSON settings for parsing raw notifications retrieved from the server.")]
        [DefaultValue(null)]
        public string? RawNotificationSettings { get; set; }

        /// <summary>
        /// Gateway settings.
        /// </summary>
        [Description("Gateway settings.")]
        [DefaultValue(null)]
        public string? GatewaySettings { get; set; }

        /// <summary>
        /// When agent is detected last time.
        /// </summary>
        [Description("When agent is detected last time.")]
        [Filter(FilterType.GreaterOrEqual)]
        [DefaultValue(null)]
        public DateTimeOffset? Detected { get; set; }

        /// <summary>
        /// Holds metadata about the client connection, including the remote IP address and other optional
        /// network-related details associated with the current request.
        /// </summary>
        [DataMember]
        [Description("Holds metadata about the client connection, including the remote IP address and other optional network-related details associated with the current request.")]
        [Filter(FilterType.Contains)]
        [DefaultValue(null)]
        [StringLength(64)]
        public string? ConnectionInfo { get; set; }


        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>Time when the agent was last updated.</summary>
        [Description("Time when the agent was last updated.")]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }


        /// <summary>Trace level used for communication diagnostics.</summary>
        [DataMember]
        [DefaultValue(System.Diagnostics.TraceLevel.Error)]
        [Description("Trace level used for communication diagnostics.")]
        [IsRequired]
        public TraceLevel? TraceLevel { get; set; }

        /// <summary>Version token used by the database to detect conflicting updates.</summary>
        /// <remarks>
        /// A stale token causes a conflicting update to be rejected instead of overwriting newer changes.
        /// </remarks>
        [Description("Version token used by the database to detect conflicting updates.")]
        [DataMember]
        [StringLength(36)]
        [ConcurrencyCheck]
        public string? ConcurrencyStamp { get; set; }

        /// <summary>Last run time.</summary>
        [Description("Last run time.")]
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? LastRun { get; set; }

        /// <summary>Time when the record was marked as removed.</summary>
        [Description("Time when the record was marked as removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>
        /// Agent Status.
        /// </summary>
        [Description("Agent Status.")]
        [DataMember]
        [DefaultValue(AgentStatus.Offline)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public AgentStatus? Status { get; set; }

        /// <summary>
        /// List of script methods that agent is using.
        /// </summary>
        [Description("List of script methods that agent is using.")]
        [DataMember]
        [ForeignKey(typeof(GXScriptMethod), typeof(GXAgentScriptMethod))]
        [Filter(FilterType.Contains)]
        public List<GXScriptMethod>? ScriptMethods { get; set; }

        /// <summary>
        /// Available agent versions.
        /// </summary>
        [Description("Available agent versions.")]
        [DataMember, ForeignKey(typeof(GXAgentVersion))]
        [Filter(FilterType.Contains)]
        public List<GXAgentVersion>? Versions { get; set; }

        /// <summary>
        /// Active agent version number.
        /// </summary>
        [Description("Active agent version number.")]
        [DefaultValue(null)]
        [StringLength(128)]
        [Filter(FilterType.Contains)]
        public string? Version { get; set; }

        /// <summary>
        /// Latest available agent version.
        /// </summary>
        [Description("Latest available agent version.")]
        [DefaultValue(null)]
        [StringLength(128)]
        [Filter(FilterType.Contains)]
        public string? AvailableVersion { get; set; }

        /// <summary>
        /// Updated version.
        /// </summary>
        /// <remarks>
        /// Client asks agent to update for this version.
        /// </remarks>
        [Description("Updated version.")]
        [DefaultValue(null)]
        [StringLength(128)]
        [Filter(FilterType.Contains)]
        public string? UpdateVersion { get; set; }

        /// <summary>
        /// An array of available serial port names for the agent.
        /// </summary>
        [Description("An array of available serial port names for the agent.")]
        [DefaultValue(null)]
        public string? SerialPorts { get; set; }

        /// <summary>
        /// The serial port for the agent.
        /// </summary>
        [Description("The serial port for the agent.")]
        [DefaultValue(null)]
        public string? SerialPort { get; set; }

        /// <summary>
        /// Cache expiration time in seconds.
        /// </summary>
        [Description("Cache expiration time in seconds.")]
        [DefaultValue(0)]
        public int CacheExpiration { get; set; }

        /// <summary>
        /// Is concurrently reading used.
        /// </summary>
        [Description("Is concurrently reading used.")]
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Concurrently { get; set; }

        /// <summary>
        /// Latitude and Longitude.
        /// </summary>
        [Description("Latitude and Longitude.")]
        [DataMember]
        [StringLength(26)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? Coordinates { get; set; }


        /// <summary>
        /// Saved properties belonging to this agent.
        /// </summary>
        [Description("Saved properties belonging to this agent.")]
        [DataMember]
        [ForeignKey(typeof(GXAgentProperty))]
        [Filter(FilterType.Contains)]
        public List<GXAgentProperty>? Properties { get; set; }

        /// <summary>
        /// Initializes the creation timestamp before insertion if it has not been set.
        /// </summary>
        [Description("Initializes the creation timestamp before insertion if it has not been set.")]
        public override void BeforeAdd()
        {
            if (CreationTime == null)
            {
                CreationTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Updates the last-modified timestamp before persisting changes.
        /// </summary>
        [Description("Updates the last-modified timestamp before persisting changes.")]
        public override void BeforeUpdate()
        {
            Updated = DateTime.Now;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string str;
            if (!string.IsNullOrEmpty(Name))
            {
                str = Name;
                str += " " + Status;
                if (Detected != null)
                {
                    str += Environment.NewLine + " Detected: " + Detected;
                }
                if (!string.IsNullOrEmpty(Version))
                {
                    str += Environment.NewLine + " Version: " + Version;
                }
            }
            else
            {
                str = nameof(GXAgent);
            }
            return str;
        }
    }
}
