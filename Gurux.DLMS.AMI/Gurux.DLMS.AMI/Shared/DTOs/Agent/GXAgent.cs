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
using Gurux.Common.Db;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Agent
{
    /// <summary>
    /// Agent.
    /// </summary>
    public class GXAgent : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXAgent()
        {
        }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// This constuctor is called when a new agent is created. It will create all needed lists.
        /// </remarks>
        /// <param name="name">Agent name.</param>
        public GXAgent(string? name)
        {
            Name = name;
            Tasks = new List<GXTask>();
            AgentGroups = new List<GXAgentGroup>();
            Logs = new List<GXAgentLog>();
            Versions = new List<GXAgentVersion>();
            ScriptMethods = new List<GXScriptMethod>();
            Concurrently = true;
            TraceLevel = System.Diagnostics.TraceLevel.Error;
        }

        /// <summary>
        /// Agent identifier.
        /// </summary>
        [Description("Agent identifier.")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// The name of the agent.
        /// </summary>
        [Description("The name of the agent.")]
        [StringLength(32, ErrorMessage = "Name is too long.")]
        [Index(false)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// Agent type.
        /// </summary>
        [Description("Agent type.")]
        [Filter(FilterType.Exact)]
        [DefaultValue(0)]
        [IsRequired]
        public byte? Type
        {
            get;
            set;
        }

        /// <summary>
        /// Is agent active.
        /// </summary>
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Active { get; set; }

        /// <summary>
        /// Description.
        /// </summary>
        [DataMember]
        [StringLength(256)]
        [Description("Description.")]
        //Filter uses default value.
        [DefaultValue(null)]
        public string? Description
        {
            get;
            set;
        }

        /// <summary>
        /// The creator of the agent.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GXUser? Creator
        {
            get;
            set;
        }

        /// <summary>
        /// Agent template settings are used to shown download information.
        /// </summary>
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Template
        {
            get;
            set;
        }

        /// <summary>
        /// Executed tasks.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXTask))]
        [Filter(FilterType.Contains)]
        public List<GXTask>? Tasks
        {
            get;
            set;
        }

        /// <summary>
        /// List of agent groups where this agent belongs.
        /// </summary>
        [DataMember,
            ForeignKey(typeof(GXAgentGroup), typeof(GXAgentGroupAgent))]
        [Filter(FilterType.Contains)]
        public List<GXAgentGroup>? AgentGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Agent logs.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXAgentLog))]
        [Filter(FilterType.Contains)]
        public List<GXAgentLog>? Logs
        {
            get;
            set;
        }

        /// <summary>
        /// Reader settings.
        /// </summary>
        [DefaultValue(null)]
        public string? ReaderSettings
        {
            get;
            set;
        }

        /// <summary>
        /// Listener settings. Agent waits server to connect for the listener.
        /// </summary>
        [DefaultValue(null)]
        public string? ListenerSettings
        {
            get;
            set;
        }

        /// <summary>
        /// Notify settings. Agent waits notify, event or push messages to this port.
        /// </summary>
        [DefaultValue(null)]
        public string? NotifySettings
        {
            get;
            set;
        }

        /// <summary>
        /// Gateway settings.
        /// </summary>
        [DefaultValue(null)]
        public string? GatewaySettings
        {
            get;
            set;
        }

        /// <summary>
        /// When agent is detected last time.
        /// </summary>
        [Description("When agent is detected last time.")]
        [Filter(FilterType.GreaterOrEqual)]
        [DefaultValue(null)]
        public DateTimeOffset? Detected
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// </summary>
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTime? CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// When was the agent last updated.
        /// </summary>
        [Description("When was the agent last updated.")]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated
        {
            get;
            set;
        }

        /// <summary>
        /// User has modified the agent.
        /// </summary>
        [IgnoreDataMember]
        [Ignore]
        [JsonIgnore]
        public bool Modified
        {
            get;
            set;
        }

        /// <summary>
        /// Used trace level.
        /// </summary>
        [DataMember]
        [DefaultValue(System.Diagnostics.TraceLevel.Error)]
        [Description("Used trace level.")]
        [IsRequired]
        public TraceLevel? TraceLevel
        {
            get;
            set;
        }

        /// <summary>
        /// Concurrency stamp.
        /// </summary>
        /// <remarks>
        /// Concurrency stamp is used to verify that several user's can't 
        /// modify the target at the same time.
        /// </remarks>
        [DataMember]
        [StringLength(36)]
        public string? ConcurrencyStamp
        {
            get;
            set;
        }

        /// <summary>
        /// Last run time
        /// </summary>
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? LastRun
        {
            get;
            set;
        }

        /// <summary>
        /// Remove time.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed
        {
            get;
            set;
        }

        /// <summary>
        /// Agent Status.
        /// </summary>
        [DataMember]
        [DefaultValue(AgentStatus.Offline)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public AgentStatus? Status
        {
            get;
            set;
        }

        /// <summary>
        /// List of script methods that agent is using.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXScriptMethod), typeof(GXAgentScriptMethod))]
        [Filter(FilterType.Contains)]
        public List<GXScriptMethod>? ScriptMethods
        {
            get;
            set;
        }

        /// <summary>
        /// Available agent versions.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXAgentVersion))]
        [Filter(FilterType.Contains)]
        public List<GXAgentVersion>? Versions
        {
            get;
            set;
        }

        /// <summary>
        /// Active agent version number.
        /// </summary>
        [DefaultValue(null)]
        [StringLength(20)]
        [Filter(FilterType.Contains)]
        public string? Version
        {
            get;
            set;
        }

        /// <summary>
        /// Latest available agent version.
        /// </summary>
        [DefaultValue(null)]
        [StringLength(20)]
        [Filter(FilterType.Contains)]
        public string? AvailableVersion
        {
            get;
            set;
        }

        /// <summary>
        /// Updated version.
        /// </summary>
        /// <remarks>
        /// Client asks agent to update for this version.
        /// </remarks>
        [DefaultValue(null)]
        [StringLength(20)]
        [Filter(FilterType.Contains)]
        public string? UpdateVersion
        {
            get;
            set;
        }

        /// <summary>
        /// An array of available serial port names for the agent.
        /// </summary>
        [DefaultValue(null)]
        public string? SerialPorts
        {
            get;
            set;
        }

        /// <summary>
        /// The serial port for the agent.
        /// </summary>
        [DefaultValue(null)]
        public string? SerialPort
        {
            get;
            set;
        }

        /// <summary>
        /// Cache expiration time in seconds.
        /// </summary>
        [DefaultValue(0)]
        public int CacheExpiration
        {
            get;
            set;
        }

        /// <summary>
        /// Is concurrently reading used.
        /// </summary>
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Concurrently { get; set; }

        /// <summary>
        /// Update creation time before update.
        /// </summary>
        public override void BeforeAdd()
        {
            if (CreationTime == DateTime.MinValue)
            {
                CreationTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Update concurrency stamp.
        /// </summary>
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
