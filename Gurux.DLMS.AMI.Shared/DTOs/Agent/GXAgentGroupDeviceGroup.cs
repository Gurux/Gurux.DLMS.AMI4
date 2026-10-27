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
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Agent
{
    /// <summary>Associates an agent group with a device group.</summary>
    [Description("Associates an agent group with a device group.")]
    [IndexCollection(true, nameof(DeviceGroupId), nameof(AgentGroupId), Clustered = true)]
    public class GXAgentGroupDeviceGroup
    {
        /// <summary>Agent group identifier.</summary>
        [Description("Agent group identifier.")]
        [DataMember]
        [ForeignKey(typeof(GXAgentGroup), OnDelete = ForeignKeyDelete.None)]
        [IsRequired]
        //ForeignKeyDelete is None because creator of the device group is causing multiple cascade paths error in MSSQL.
        public Guid AgentGroupId { get; set; }

        /// <summary>Device group identifier.</summary>
        [Description("Device group identifier.")]
        [DataMember]
        [ForeignKey(typeof(GXDeviceGroup), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public Guid DeviceGroupId { get; set; }

        /// <summary>
        /// Creation time.
        /// The time when the device group was added to the agent agent.
        /// </summary>
        [Description("Creation time. The time when the device group was added to the agent agent.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when device group was removed from agent group.
        /// </summary>
        [Description("Time when device group was removed from agent group.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }
    }
}
