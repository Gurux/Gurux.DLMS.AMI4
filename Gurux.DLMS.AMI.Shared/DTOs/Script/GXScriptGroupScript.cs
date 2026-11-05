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

namespace Gurux.DLMS.AMI.Shared.DTOs.Script
{
    /// <summary>Associates a script with a script group.</summary>
    [Description("Associates a script with a script group.")]
    [DataContract(Name = "GXScriptGroupScript"), Serializable]
    [IndexCollection(true, nameof(ScriptGroupId), nameof(ScriptId), Clustered = true)]
    public class GXScriptGroupScript : GXTableBase
    {
        /// <summary>The database identifier of the Script group.</summary>
        [Description("The database identifier of the Script group.")]
        [DataMember(Name = "ScriptGroupId")]
        [ForeignKey(typeof(GXScriptGroup), OnDelete = ForeignKeyDelete.None)]
        public Guid ScriptGroupId { get; set; }

        /// <summary>The database identifier of the script.</summary>
        [Description("The database identifier of the script.")]
        [DataMember(Name = "ScriptID")]
        [ForeignKey(typeof(GXScript), OnDelete = ForeignKeyDelete.Cascade)]
        public Guid ScriptId { get; set; }

        /// <summary>
        /// Creation time.
        /// The time when the script was added to the script group.
        /// </summary>
        [Description("Creation time. The time when the script was added to the script group.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when script was removed from script group.
        /// </summary>
        [Description("Time when script was removed from script group.")]
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
