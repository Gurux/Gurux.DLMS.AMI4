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

using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Trigger
{
    /// <summary>An activity configured to run when a trigger fires.</summary>
    [Description("An activity configured to run when a trigger fires.")]
    public class GXTriggerActivity : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Trigger activity identifier.
        /// </summary>
        [DataMember(Name = "ID")]
        [Description("Trigger activity identifier.")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// Trigger activity name.
        /// </summary>
        [Description("Trigger activity name.")]
        [DataMember]
        [StringLength(64)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Name { get; set; }

        /// <summary>Action permission required by the registered trigger event.</summary>
        [DataMember, StringLength(128)]
        [Description("Action permission required by the registered trigger event.")]
        public string? Permission { get; set; }

        /// <summary>Phases that this activity can publish. Existing activities publish After only.</summary>
        [DataMember, Description("Supported trigger execution phases.")]
        [DefaultValue(TriggerPhase.After), IsRequired]
        [Filter(FilterType.Exact)]
        public TriggerPhase SupportedPhases { get; set; } = TriggerPhase.After;
        /// <summary>
        /// Parent trigger.
        /// </summary>
        [Description("Parent trigger.")]
        [ForeignKey(typeof(GXTrigger))]
        [Index(false)]
        public GXTrigger? Trigger { get; set; }


        /// <summary>Time when the record was created.</summary>
        [DataMember]
        [Description("Time when the record was created.")]
        [DefaultValue(null)]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>Time when the trigger activity was last updated.</summary>
        [Description("Time when the trigger activity was last updated.")]
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }

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

        /// <summary>
        /// Updates the last-modified timestamp before persisting changes.
        /// </summary>
        [Description("Updates the last-modified timestamp before persisting changes.")]
        public override void BeforeUpdate()
        {
            Updated = DateTime.Now;
        }
    }
}
