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
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Agent
{
    /// <summary>A version of an agent available for installation or update.</summary>
    [Description("A version of an agent available for installation or update.")]
    [IndexCollection(true, nameof(Agent), nameof(Number))]
    public class GXAgentVersion : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Agent version identifier.
        /// </summary>
        [Description("Agent version identifier.")]
        [DefaultValue(null)]
        public Guid Id { get; set; }

        /// <summary>Whether this is the default agent version.</summary>
        [Description("Whether this is the default agent version.")]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Default { get; set; }

        /// <summary>Whether the agent version is enabled.</summary>
        [Description("Whether the agent version is enabled.")]
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Enabled { get; set; }


        /// <summary>
        /// Agent version number.
        /// </summary>
        [Description("Agent version number.")]
        [StringLength(128)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Number { get; set; }

        /// <summary>Whether this is a prerelease version.</summary>
        [Description("Whether this is a prerelease version.")]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Prerelease { get; set; }

        /// <summary>
        /// Installation Url.
        /// </summary>
        /// <remarks>
        /// Only agent template sets this.
        /// </remarks>
        [Description("Installation Url.")]
        [StringLength(2048)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? Url { get; set; }

        /// <summary>
        /// File path.
        /// </summary>
        [Description("File path.")]
        [StringLength(256)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? FileName { get; set; }

        /// <summary>Description of the agent version.</summary>
        [Description("Description of the agent version.")]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Description { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DefaultValue(null)]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>Time when the record was marked as removed.</summary>
        [Description("Time when the record was marked as removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>
        /// The agent that owns this version.
        /// </summary>
        [Description("The agent that owns this version.")]
        [DefaultValue(null)]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        [IsRequired]
        [JsonIgnore]
        public GXAgent? Agent { get; set; }

        /// <summary>Initializes a new instance of GXAgentVersion.</summary>
        [Description("Initializes a new instance of GXAgentVersion.")]
        public GXAgentVersion()
        {
            Enabled = true;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Number))
            {
                return Number;
            }
            return nameof(GXAgentVersion);
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
    }
}
