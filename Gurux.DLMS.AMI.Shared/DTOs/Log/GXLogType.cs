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

using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Log
{
    /// <summary>A log category with a stable public identifier and access and presentation settings.</summary>
    [Description("A log category with a stable public identifier and access and presentation settings.")]
    [DataContract(Name = "GXLogType"), Serializable]
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class GXLogType : IUnique<long>
    {
        /// <summary>Initializes a new instance of GXLogType.</summary>
        [Description("Initializes a new instance of GXLogType.")]
        public GXLogType()
        {
        }

        /// <summary>Initializes a new instance of GXLogType.</summary>
        /// <remarks>
        /// This constructor is called when a new log group is created. It will create all required collections.
        /// </remarks>
        /// <param name="name">Script group name.</param>
        [Description("Initializes a new instance of GXLogType.")]
        public GXLogType(string? name)
        {
            Enabled = true;
            Name = name;
            Logs = [];
        }

        /// <summary>Stable external identifier of the log category.</summary>
        [Description("Stable external identifier of the log category.")]
        [DataMember(IsRequired = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        [Index(true)]
        [IsRequired]
        public Guid PublicId { get; set; }

        /// <summary>Database-generated log type storage key.</summary>
        [Description("Database-generated log type storage key.")]
        [DataMember, PrimaryKey, AutoIncrement]
        [DefaultValue(null)]
        public long Id { get; set; }

        /// <summary>Whether the log type is enabled.</summary>
        [Description("Whether the log type is enabled.")]
        [DataMember]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Enabled { get; set; }

        /// <summary>
        /// Minimum log severity level that is saved.
        /// </summary>
        [Description("Minimum log severity level that is saved.")]
        [DataMember]
        [DefaultValue(1)]
        [IsRequired]
        [Filter(FilterType.Exact)]
        public int? Level { get; set; }

        /// <summary>
        /// The creator of the log type.
        /// </summary>
        [Description("The creator of the log type.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXUser? Creator { get; set; }

        /// <summary>Display name of the log category.</summary>
        [Description("Display name of the log category.")]
        [DataMember]
        [StringLength(64)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Name { get; set; }

        /// <summary>
        /// Normalized name.
        /// </summary>
        /// <remarks>
        /// Normalized name is unique and it's used for case-insensitive comparisons and searches.
        /// </remarks>
        [Description("Normalized name.")]
        [DataMember]
        [Index]
        [StringLength(64)]
        [IsRequired]
        [Filter(FilterType.Exact)]
        public string? NormalizedName { get; set; }

        /// <summary>Human-readable description of the log category.</summary>
        [Description("Human-readable description of the log category.")]
        [DataMember]
        public string? Description { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>Time when the log category was removed; <see langword="null"/> indicates an active category.</summary>
        [Description("Time when the log category was removed; null indicates an active category.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>Time when the log category was last modified.</summary>
        [Description("Time when the log category was last modified.")]
        [DataMember]
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
        /// Authorization policies that allow viewing this resource (alternative named policies).
        /// </summary>
        [Description("Authorization policies that allow viewing this resource (alternative named policies).")]
        [DataMember]
        [Ignore(IgnoreType.Db)]
        public List<string>? Policies { get; set; }

        /// <summary>Log entries belonging to this category.</summary>
        [Description("Log entries belonging to this category.")]
        [DataMember, ForeignKey(typeof(GXLog))]
        public List<GXLog>? Logs { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name))
            {
                return Name;
            }
            return nameof(GXLogType);
        }
    }
}



