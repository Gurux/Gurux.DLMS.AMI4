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

namespace Gurux.DLMS.AMI.Shared.DTOs.Block
{
    /// <summary>Associates a user interface block with a block group.</summary>
    [Description("Associates a user interface block with a block group.")]
    [DataContract(Name = "GXBlockGroupBlock"), Serializable]
    [IndexCollection(true, nameof(BlockGroupId), nameof(BlockId), Clustered = true)]
    public class GXBlockGroupBlock
    {
        /// <summary>The database identifier of the block group.</summary>
        [Description("The database identifier of the block group.")]
        [DataMember(Name = "BlockGroupId")]
        [ForeignKey(typeof(GXBlockGroup), OnDelete = ForeignKeyDelete.None)]
        [IsRequired]
        //ForeignKeyDelete is None because creator of the block is causing multiple cascade paths error in MSSQL.
        public Guid BlockGroupId { get; set; }

        /// <summary>The database identifier of the block.</summary>
        [Description("The database identifier of the block.")]
        [DataMember(Name = "BlockID")]
        [ForeignKey(typeof(GXBlock), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public Guid BlockId { get; set; }

        /// <summary>
        /// Creation time.
        /// The time when the block was added to the block group.
        /// </summary>
        [Description("Creation time. The time when the block was added to the block group.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when block was removed from block group.
        /// </summary>
        [Description("Time when block was removed from block group.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }
    }
}
