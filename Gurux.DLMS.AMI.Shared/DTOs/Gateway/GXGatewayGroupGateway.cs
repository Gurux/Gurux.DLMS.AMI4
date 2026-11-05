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

namespace Gurux.DLMS.AMI.Shared.DTOs.Gateway
{
    /// <summary>Associates a gateway with a gateway group.</summary>
    [Description("Associates a gateway with a gateway group.")]
    [DataContract(Name = "GXGatewayGroupGateway"), Serializable]
    [IndexCollection(true, nameof(GatewayGroupId), nameof(GatewayId), Clustered = true)]
    public class GXGatewayGroupGateway : GXTableBase
    {
        /// <summary>The database identifier of the gateway group.</summary>
        [Description("The database identifier of the gateway group.")]
        [DataMember(Name = "GatewayGroupID")]
        [ForeignKey(typeof(GXGatewayGroup), OnDelete = ForeignKeyDelete.None)]
        [IsRequired]
        //ForeignKeyDelete is None because creator of the gateway is causing multiple cascade paths error in MSSQL.
        public Guid GatewayGroupId { get; set; }

        /// <summary>The database identifier of the gateway.</summary>
        [Description("The database identifier of the gateway.")]
        [DataMember(Name = "GatewayID")]
        [ForeignKey(typeof(GXGateway), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public Guid GatewayId { get; set; }

        /// <summary>
		/// Creation time.
		/// The time when the gateway was added to the gateway group.
		/// </summary>
		[DataMember]
        [Description("Creation time. The time when the gateway was added to the gateway group.")]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when gateway was removed from gateway group.
        /// </summary>
        [Description("Time when gateway was removed from gateway group.")]
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
