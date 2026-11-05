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

namespace Gurux.DLMS.AMI.Shared.DTOs.Module
{
    /// <summary>Associates a module with a module group.</summary>
    [Description("Associates a module with a module group.")]
    [DataContract(Name = "GXModuleGroupModule"), Serializable]
    [IndexCollection(true, nameof(ModuleGroupId), nameof(ModuleId), Clustered = true)]
    public class GXModuleGroupModule
    {
        /// <summary>The database identifier of the Module group.</summary>
        [Description("The database identifier of the Module group.")]
        [DataMember(Name = "ModuleGroupId")]
        [ForeignKey(typeof(GXModuleGroup), OnDelete = ForeignKeyDelete.None)]
        //ForeignKeyDelete is None because creator of the module is causing multiple cascade paths error in MSSQL.
        public Guid ModuleGroupId { get; set; }

        /// <summary>The database identifier of the schedule.</summary>
        [Description("The database identifier of the schedule.")]
        [DataMember(Name = "ModuleID")]
        [ForeignKey(typeof(GXModule), OnDelete = ForeignKeyDelete.Cascade)]
        [StringLength(64)]
        public string ModuleId { get; set; } = string.Empty;

        /// <summary>
        /// Creation time.
        /// The time when the module was added to the module group.
        /// </summary>
        [Description("Creation time. The time when the module was added to the module group.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset CreationTime { get; set; }

        /// <summary>
        /// Time when module was removed from module group.
        /// </summary>
        [Description("Time when module was removed from module group.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }
    }
}
