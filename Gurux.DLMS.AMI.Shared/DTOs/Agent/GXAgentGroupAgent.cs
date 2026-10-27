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
