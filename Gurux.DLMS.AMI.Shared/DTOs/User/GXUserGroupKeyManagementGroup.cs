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
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.User
{
    /// <summary>Associates a user group with a key management group.</summary>
    [Description("Associates a user group with a key management group.")]
    [DataContract(Name = nameof(GXUserGroupKeyManagementGroup)), Serializable]
    [IndexCollection(true, nameof(UserGroupId), nameof(KeyManagementGroupId), Clustered = true)]
    public class GXUserGroupKeyManagementGroup
    {
        /// <summary>The database identifier of the user group.</summary>
        [Description("The database identifier of the user group.")]
        [DataMember(Name = "UserGroupID")]
        [ForeignKey(typeof(GXUserGroup), OnDelete = ForeignKeyDelete.None)]
        //ForeignKeyDelete is None because creator of the key management group is causing multiple cascade paths error in MSSQL.
        public Guid UserGroupId { get; set; }

        /// <summary>The database identifier of the user.</summary>
        [Description("The database identifier of the user.")]
        [DataMember(Name = "KeyManagementGroupID")]
        [ForeignKey(typeof(GXKeyManagementGroup), OnDelete = ForeignKeyDelete.Cascade)]
        public Guid KeyManagementGroupId { get; set; }

        /// <summary>
		/// Creation time.
		/// The time when the key management group was added to the user group.
		/// </summary>
		[DataMember]
        [Description("Creation time. The time when the key management group was added to the user group.")]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when key management group was removed from user group.
        /// </summary>
        [Description("Time when key management group was removed from user group.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }
    }
}
