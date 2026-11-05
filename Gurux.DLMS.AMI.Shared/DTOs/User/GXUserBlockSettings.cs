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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs.Block;

namespace Gurux.DLMS.AMI.Shared.DTOs.User
{
    /// <summary>A user's saved configuration for a user interface block.</summary>
    [Description("A user's saved configuration for a user interface block.")]
    [DataContract(Name = "GXUserBlockSettings"), Serializable]
    [IndexCollection(true, nameof(UserId), nameof(BlockId), Clustered = true)]
    public class GXUserBlockSettings
    {
        /// <summary>The database identifier of the user.</summary>
        [Description("The database identifier of the user.")]
        [DataMember(Name = "UserID")]
        [ForeignKey(typeof(GXUser), OnDelete = ForeignKeyDelete.None)]
        [StringLength(36)]
        //ForeignKeyDelete is None because creator of the block is causing multiple cascade paths error in MSSQL.
        public string? UserId { get; set; }

        /// <summary>Block identifier.</summary>
        [Description("Block identifier.")]
        [DataMember(Name = "BlockID"), ForeignKey(typeof(GXBlock),
            OnDelete = ForeignKeyDelete.Cascade)]
        public Guid BlockId { get; set; }

        /// <summary>
        /// Time when user closed the block.
        /// </summary>
        /// <remarks>
        /// If block is closed it's not shown for the user.
        /// </remarks>
        [Description("Time when user closed the block.")]
        [DataMember]
        [Index(false)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Closed { get; set; }

        /// <summary>
        /// User-dependent block settings.
        /// </summary>
        [Description("User-dependent block settings.")]
        [DataMember]
        public string? Settings { get; set; }
    }
}
