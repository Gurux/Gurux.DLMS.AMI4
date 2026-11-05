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

using Gurux.DLMS.AMI.Shared.DTOs.Module;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;

namespace Gurux.DLMS.AMI.Shared.DTOs.Device
{
    /// <summary>
    /// A module property associated with a device.
    /// </summary>
    [Description("A module property associated with a device.")]
    [DataContract(Name = nameof(GXDeviceProperty)), Serializable]
    [IndexCollection(true, nameof(Device), nameof(Module), nameof(Name))]
    public class GXDeviceProperty : IUnique<Guid>, IGXProperty
    {
        /// <summary>
        /// Property identifier.
        /// </summary>
        [Description("Property identifier.")]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// Parent device.
        /// </summary>
        [Description("Parent device.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        [JsonIgnore]
        [IsRequired]
        public GXDevice? Device { get; set; }

        /// <summary>
        /// The module whose settings these are.
        /// </summary>
        [Description("The module whose settings these are.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        //ForeignKeyDelete is None because creator of the device is causing multiple cascade paths error in MSSQL.
        public GXModule? Module { get; set; }

        /// <summary>
        /// Module settings unique name.
        /// </summary>
        [Description("Module settings unique name.")]
        [DataMember]
        [StringLength(64)]
        [IsRequired]
        public string? Name { get; set; }

        /// <summary>
        /// Module settings.
        /// </summary>
        [Description("Module settings.")]
        [DataMember]
        public string? Value { get; set; }

        /// <summary>Version token maintained by Gurux Service.</summary>
        [Description("Version token maintained by Gurux Service.")]
        [DataMember]
        [StringLength(36)]
        [ConcurrencyCheck]
        public string? ConcurrencyStamp { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset CreationTime { get; set; }
        /// <summary>Time when the parameter was last updated.</summary>
        [Description("Time when the parameter was last updated.")]
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }


        /// <summary>Time when the record was marked as removed.</summary>
        [Description("Time when the record was marked as removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }
    }
}
