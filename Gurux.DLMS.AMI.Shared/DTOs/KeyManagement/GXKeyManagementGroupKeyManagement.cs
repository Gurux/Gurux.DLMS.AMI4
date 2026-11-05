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
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.KeyManagement
{
    /// <summary>Associates a key management configuration with a key management group.</summary>
    [Description("Associates a key management configuration with a key management group.")]
    [DataContract(Name = "GXKeyManagementGroupKeyManagement"), Serializable]
    [IndexCollection(true, nameof(KeyManagementGroupId), nameof(KeyManagementId), Clustered = true)]
    public class GXKeyManagementGroupKeyManagement : GXTableBase
    {
        /// <summary>The database identifier of the KeyManagement group.</summary>
        [Description("The database identifier of the KeyManagement group.")]
        [DataMember(Name = "KeyManagementGroupId")]
        [ForeignKey(typeof(GXKeyManagementGroup), OnDelete = ForeignKeyDelete.None)]
        //ForeignKeyDelete is None because creator of the key management is causing multiple cascade paths error in MSSQL.
        public Guid KeyManagementGroupId { get; set; }

        /// <summary>The database identifier of the key management.</summary>
        [Description("The database identifier of the key management.")]
        [DataMember(Name = "KeyManagementID")]
        [ForeignKey(typeof(GXKeyManagement), OnDelete = ForeignKeyDelete.Cascade)]
        [StringLength(36)]
        public Guid KeyManagementId { get; set; }

        /// <summary>
        /// Creation time.
        /// The time when the key management was added to the key management group.
        /// </summary>
        [Description("Creation time. The time when the key management was added to the key management group.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when key management was removed from key management group.
        /// </summary>
        [Description("Time when key management was removed from key management group.")]
        [DataMember]
        [Index(false, Descend = true)]
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
