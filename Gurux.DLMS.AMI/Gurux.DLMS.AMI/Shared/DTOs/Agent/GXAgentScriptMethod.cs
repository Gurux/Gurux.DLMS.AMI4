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
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Agent
{
    /// <summary>
    /// A data contract class representing agent to script binding object.
    /// </summary>
    [DataContract(Name = "GXAgentScriptMethod"), Serializable]
    [IndexCollection(true, nameof(AgentId), nameof(MethodId), Clustered = true)]
    public class GXAgentScriptMethod : GXTableBase
    {
        /// <summary>
        /// The database ID of the Agent.
        /// </summary>
        [DataMember(Name = "AgentID")]
        [ForeignKey(typeof(GXAgent), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public Guid AgentId
        {
            get;
            set;
        }

        /// <summary>
        /// The database ID of the script method.
        /// </summary>
        [DataMember(Name = "MethodId")]
        [ForeignKey(typeof(GXScriptMethod), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public Guid MethodId
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTime CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// Time when script method was removed from Agent.
        /// </summary>
        [DataMember]
        [Index(false)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed
        {
            get;
            set;
        }

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
    }
}
