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

namespace Gurux.DLMS.AMI.Shared.DTOs.Trigger
{
    /// <summary>Associates a trigger with a trigger group.</summary>
    [Description("Associates a trigger with a trigger group.")]
    [DataContract(Name = "GXTriggerGroupTrigger"), Serializable]
    [IndexCollection(true, nameof(TriggerGroupId), nameof(TriggerId), Clustered = true)]
    public class GXTriggerGroupTrigger
    {
        /// <summary>The database identifier of the trigger group.</summary>
        [Description("The database identifier of the trigger group.")]
        [DataMember(Name = "TriggerGroupID")]
        [ForeignKey(typeof(GXTriggerGroup), OnDelete = ForeignKeyDelete.None)]
        public Guid TriggerGroupId { get; set; }

        /// <summary>The database identifier of the trigger.</summary>
        [Description("The database identifier of the trigger.")]
        [DataMember(Name = "TriggerID")]
        [ForeignKey(typeof(GXTrigger), OnDelete = ForeignKeyDelete.Cascade)]
        public Guid TriggerId { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when agent group was removed from user group.
        /// </summary>
        [Description("Time when agent group was removed from user group.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }
    }
}
