// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Agent
{
    /// <summary>Associates an agent with an agent group.</summary>
    [Description("Associates an agent with an agent group.")]
    [DataContract(Name = "GXAgentGroupAgent"), Serializable]
    [IndexCollection(true, nameof(AgentGroupId), nameof(AgentId), Clustered = true)]
    public class GXAgentGroupAgent : GXTableBase
    {
        /// <summary>The database identifier of the agent group.</summary>
        [Description("The database identifier of the agent group.")]
        [DataMember(Name = "AgentGroupID")]
        [ForeignKey(typeof(GXAgentGroup), OnDelete = ForeignKeyDelete.None)]
        [IsRequired]
        //ForeignKeyDelete is None because creator of the agent is causing multiple cascade paths error in MSSQL.
        public Guid AgentGroupId { get; set; }

        /// <summary>The database identifier of the agent.</summary>
        [Description("The database identifier of the agent.")]
        [DataMember(Name = "GXAgentID")]
        [ForeignKey(typeof(GXAgent), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public Guid AgentId { get; set; }

        /// <summary>
		/// Creation time.
		/// The time when the agent was added to the agent group.
		/// </summary>
		[DataMember]
        [Description("Creation time. The time when the agent was added to the agent group.")]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when agent was removed from agent group.
        /// </summary>
        [Description("Time when agent was removed from agent group.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>Initializes the creation timestamp before insertion.</summary>
        [Description("Initializes the creation timestamp before insertion.")]
        public override void BeforeAdd()
        {
            CreationTime = DateTime.Now;
        }
    }
}
