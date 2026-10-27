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
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using System.ComponentModel;
using System.Runtime.Serialization;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;

namespace Gurux.DLMS.AMI.Shared.DTOs.Agent
{
    /// <summary>Associates an agent with a script method.</summary>
    [Description("Associates an agent with a script method.")]
    [DataContract(Name = "GXAgentScriptMethod"), Serializable]
    [IndexCollection(true, nameof(AgentId), nameof(MethodId), Clustered = true)]
    public class GXAgentScriptMethod : GXTableBase
    {
        /// <summary>The database identifier of the Agent.</summary>
        [Description("The database identifier of the Agent.")]
        [DataMember(Name = "AgentID")]
        [ForeignKey(typeof(GXAgent), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public Guid AgentId { get; set; }

        /// <summary>The database identifier of the script method.</summary>
        [Description("The database identifier of the script method.")]
        [DataMember(Name = "MethodId")]
        [ForeignKey(typeof(GXScriptMethod), OnDelete = ForeignKeyDelete.None)]
        [IsRequired]
        //ForeignKeyDelete is None because Agent will handle the deletion.
        public Guid MethodId { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when script method was removed from Agent.
        /// </summary>
        [Description("Time when script method was removed from Agent.")]
        [DataMember]
        [Index(false)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

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
    }
}
