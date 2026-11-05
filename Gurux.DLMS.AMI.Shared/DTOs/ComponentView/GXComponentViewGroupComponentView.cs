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

namespace Gurux.DLMS.AMI.Shared.DTOs.ComponentView
{
    /// <summary>Associates a component view with a component view group.</summary>
    [Description("Associates a component view with a component view group.")]
    [DataContract(Name = "GXComponentViewGroupComponentView"), Serializable]
    [IndexCollection(true, nameof(ComponentViewGroupId), nameof(ComponentViewId), Clustered = true)]
    public class GXComponentViewGroupComponentView
    {
        /// <summary>The database identifier of the component view group.</summary>
        [Description("The database identifier of the component view group.")]
        [DataMember(Name = "ComponentViewGroupId")]
        [ForeignKey(typeof(GXComponentViewGroup), OnDelete = ForeignKeyDelete.None)]
        [IsRequired]
        //ForeignKeyDelete is None because creator of the component view is causing multiple cascade paths error in MSSQL.
        public Guid ComponentViewGroupId { get; set; }

        /// <summary>The database identifier of the component view.</summary>
        [Description("The database identifier of the component view.")]
        [DataMember(Name = "ComponentViewID")]
        [ForeignKey(typeof(GXComponentView), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public Guid ComponentViewId { get; set; }

        /// <summary>
        /// Creation time.
        /// The time when the component view was added to the component view group.
        /// </summary>
        [Description("Creation time. The time when the component view was added to the component view group.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when component view was removed from component view group.
        /// </summary>
        [Description("Time when component view was removed from component view group.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }
    }
}
