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
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>A translated resource value associated with a language and resource key.</summary>
    [Description("A translated resource value associated with a language and resource key.")]
    [IndexCollection(true, nameof(Language), nameof(Hash))]
    public class GXLocalizedResource : IUnique<Guid>
    {
        /// <summary>
        /// Localized string identifier.
        /// </summary>
        [Description("Localized string identifier.")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// Localized string.
        /// </summary>
        [Description("Localized string.")]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Value { get; set; }

        /// <summary>
        /// Hash value for Localized string key.
        /// </summary>
        [Description("Hash value for Localized string key.")]
        [Index(false)]
        [Filter(FilterType.Exact)]
        [StringLength(64)]
        [IsRequired]
        public string? Hash { get; set; }

        /// <summary>
        /// Status is used to tell when a resource is missing.
        /// </summary>
        [Description("Status is used to tell when a resource is missing.")]
        [Index(false)]
        [DefaultValue(0)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public int? Status { get; set; }

        /// <summary>
        /// Parent language.
        /// </summary>
        /// <remarks>
        /// Creator
        /// </remarks>
        [Description("Parent language.")]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [Index(false)]
        public GXLanguage? Language { get; set; }

        /// <summary>
        /// The creator of the localized resource.
        /// </summary>
        [Description("The creator of the localized resource.")]
        [DataMember]
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

        /// <summary>Time when the localized resource was last updated.</summary>
        [Description("Time when the localized resource was last updated.")]
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

        /// <inheritdoc/>
        public override string ToString()
        {
            return Id + ": " + Value;
        }
        /// <summary>Load this resource during client initialization when access is allowed.</summary>
        [Description("Load this resource during client initialization when access is allowed.")]
        [DataMember, IsRequired, DefaultValue(false), Filter(FilterType.Exact)]
        public bool? Preload { get; set; }
    }
}
