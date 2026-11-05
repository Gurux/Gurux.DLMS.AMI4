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

namespace Gurux.DLMS.AMI.Shared.DTOs.View
{
    /// <summary>Associates a table view with a view group.</summary>
    [Description("Associates a table view with a view group.")]
    [DataContract(Name = "GXViewGroupView"), Serializable]
    [IndexCollection(true, nameof(ViewGroupId), nameof(ViewId), Clustered = true)]
    public class GXViewGroupView
    {
        /// <summary>The database identifier of the content group.</summary>
        [Description("The database identifier of the content group.")]
        [DataMember(Name = "ViewGroupId")]
        [ForeignKey(typeof(GXViewGroup), OnDelete = ForeignKeyDelete.None)]
        [IsRequired]
        //ForeignKeyDelete is None because creator of the view is causing multiple cascade paths error in MSSQL.
        public Guid ViewGroupId { get; set; }

        /// <summary>Identifier of the view assigned to the group.</summary>
        [Description("Identifier of the view assigned to the group.")]
        [DataMember(Name = "ViewId")]
        [ForeignKey(typeof(GXView), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public Guid ViewId { get; set; }

        /// <summary>
        /// Creation time.
        /// The time when the view was added to the view group.
        /// </summary>
        [Description("Creation time. The time when the view was added to the view group.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when block was removed from view group.
        /// </summary>
        [Description("Time when block was removed from view group.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }
    }
}
