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

namespace Gurux.DLMS.AMI.Shared.DTOs.Menu
{
    /// <summary>Associates a menu with a menu group.</summary>
    [Description("Associates a menu with a menu group.")]
    [DataContract(Name = "GXMenuGroupMenu"), Serializable]
    [IndexCollection(true, nameof(MenuGroupId), nameof(MenuId), Clustered = true)]
    public class GXMenuGroupMenu
    {
        /// <summary>The database identifier of the menu group.</summary>
        [Description("The database identifier of the menu group.")]
        [DataMember(Name = "MenuGroupId")]
        [ForeignKey(typeof(GXMenuGroup), OnDelete = ForeignKeyDelete.None)]
        [IsRequired]
        //ForeignKeyDelete is None because creator of the menu is causing multiple cascade paths error in MSSQL.
        public Guid MenuGroupId { get; set; }

        /// <summary>The menu identifier.</summary>
        [Description("The menu identifier.")]
        [DataMember(Name = "MenuId")]
        [ForeignKey(typeof(GXMenu), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public Guid MenuId { get; set; }

        /// <summary>
        /// Creation time.
        /// The time when the menu was added to the menu group.
        /// </summary>
        [Description("Creation time. The time when the menu was added to the menu group.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when block was removed from menu group.
        /// </summary>
        [Description("Time when block was removed from menu group.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }
    }
}
