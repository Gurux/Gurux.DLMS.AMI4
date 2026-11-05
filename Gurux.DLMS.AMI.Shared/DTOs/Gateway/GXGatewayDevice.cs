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
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Gateway
{
    /// <summary>Associates a gateway with a device.</summary>
    [Description("Associates a gateway with a device.")]
    [IndexCollection(true, nameof(DeviceId), nameof(GatewayId), Clustered = true)]
    public class GXGatewayDevice
    {
        /// <summary>Device identifier.</summary>
        [Description("Device identifier.")]
        [DataMember]
        [ForeignKey(typeof(GXDevice), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public Guid DeviceId { get; set; }

        /// <summary>Gateway identifier.</summary>
        [Description("Gateway identifier.")]
        [DataMember]
        [ForeignKey(typeof(GXGateway), OnDelete = ForeignKeyDelete.None)]
        [IsRequired]
        //ForeignKeyDelete is None because creator of the device is causing multiple cascade paths error in MSSQL.
        public Guid GatewayId { get; set; }

        /// <summary>
        /// Creation time.
        /// The time when the device group was added to the gateway gateway.
        /// </summary>
        [Description("Creation time. The time when the device group was added to the gateway gateway.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when device group was removed from gateway group.
        /// </summary>
        [Description("Time when device group was removed from gateway group.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }
    }
}
