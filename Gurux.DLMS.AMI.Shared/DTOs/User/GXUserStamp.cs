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
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.User
{
    /// <summary>Tracks which resources a user has seen, supporting unread-item indicators.</summary>
    [Description("Tracks which resources a user has seen, supporting unread-item indicators.")]
    public class GXUserStamp : GXTableBase, IUnique<Guid>
    {
        /// <summary>Initializes a new instance of GXUserStamp.</summary>
        [Description("Initializes a new instance of GXUserStamp.")]
        public GXUserStamp()
        {
        }

        /// <summary>
        /// Stamp identifier.
        /// </summary>
        [Description("Stamp identifier.")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// The stamp Creator.
        /// </summary>
        [Description("The stamp Creator.")]
        [DataMember]
        [Index(false)]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GXUser? Creator { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>Time when the user stamp was last updated.</summary>
        [Description("Time when the user stamp was last updated.")]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }


        /// <summary>Version token used by the database to detect conflicting updates.</summary>
        /// <remarks>
        /// A stale token causes a conflicting update to be rejected instead of overwriting newer changes.
        /// </remarks>
        [Description("Version token used by the database to detect conflicting updates.")]
        [DataMember]
        [StringLength(36)]
        [ConcurrencyCheck]
        public string? ConcurrencyStamp { get; set; }

        /// <summary>
        /// Stamped target.
        /// </summary>
        [Description("Stamped target.")]
        [DefaultValue(null)]
        [Filter(FilterType.Equals)]
        public Guid? Target { get; set; }

        /// <summary>
        /// Target type.
        /// </summary>
        /// <remarks>
        /// Target type can be e.g. user, schedule, device, etc.
        /// </remarks>
        [Description("Target type.")]
        [Filter(FilterType.Equals)]
        [StringLength(32)]
        [IsRequired]
        public string? TargetType { get; set; }

        /// <summary>
        /// Number of the errors.
        /// </summary>
        [Description("Number of the errors.")]
        [Ignore]
        public int Errors { get; set; }

        /// <summary>
        /// Number of the warnings.
        /// </summary>
        [Description("Number of the warnings.")]
        [Ignore]
        public int Warnings { get; set; }

        /// <summary>
        /// Number of the informational messages.
        /// </summary>
        [Description("Number of the informational messages.")]
        [Ignore]
        public int Informational { get; set; }

        /// <summary>
        /// Number of the verbose messages.
        /// </summary>
        [Description("Number of the verbose messages.")]
        [Ignore]
        public int Verboses { get; set; }

        /// <summary>
        /// Get user stamp count.
        /// </summary>
        /// <returns></returns>
        [Description("Get user stamp count.")]
        public int GetCount()
        {
            if (Errors != 0)
            {
                return Errors;
            }
            if (Warnings != 0)
            {
                return Warnings;
            }
            if (Informational != 0)
            {
                return Informational;
            }
            if (Verboses != 0)
            {
                return Verboses;
            }
            return 0;
        }

        /// <summary>
        /// Get user stamp count.
        /// </summary>
        /// <returns></returns>
        [Description("Get user stamp count.")]
        public int GetTotalCount()
        {
            return Errors + Warnings + Informational + Verboses;
        }

        /// <summary>
        /// Get used trace level.
        /// </summary>
        /// <returns></returns>
        [Description("Get used trace level.")]
        public TraceLevel GetTraceLevel()
        {
            if (Errors != 0)
            {
                return TraceLevel.Error;
            }
            if (Warnings != 0)
            {
                return TraceLevel.Warning;
            }
            if (Informational != 0)
            {
                return TraceLevel.Info;
            }
            if (Verboses != 0)
            {
                return TraceLevel.Verbose;
            }
            return TraceLevel.Off;
        }

        /// <summary>
        /// Add stamp counts to the target.
        /// </summary>
        /// <param name="target">Target.</param>
        [Description("Add stamp counts to the target.")]
        public void AddTo(GXUserStamp target)
        {
            if (Errors == 0 && Warnings == 0 &&
                Informational == 0 &&
                Verboses == 0)
            {
                //Clear values.
                target.Errors = 0;
                target.Warnings = 0;
                target.Informational = 0;
                target.Verboses = 0;
            }
            else
            {
                target.Errors += Errors;
                target.Warnings += Warnings;
                target.Informational += Informational;
                target.Verboses += Verboses;
            }
        }

        /// <summary>
        /// Copy stamp counts to the target.
        /// </summary>
        /// <param name="target">Target.</param>
        [Description("Copy stamp counts to the target.")]
        public void CopyTo(GXUserStamp target)
        {
            target.Errors = Errors;
            target.Warnings = Warnings;
            target.Informational = Informational;
            target.Verboses = Verboses;
        }

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

        /// <inheritdoc/>
        public override string ToString()
        {
            string str;
            if (!string.IsNullOrEmpty(Creator?.UserName))
            {
                str = Creator.UserName;
            }
            else
            {
                str = nameof(GXUserStamp);
            }
            return str;
        }
    }
}
