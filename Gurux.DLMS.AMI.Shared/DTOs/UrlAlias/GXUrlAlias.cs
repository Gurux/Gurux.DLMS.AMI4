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

namespace Gurux.DLMS.AMI.Shared.DTOs.UrlAlias
{
    /// <summary>A URL alias for a resource, including resource types supplied by extensions.</summary>
    [Description("A URL alias for a resource, including resource types supplied by extensions.")]
    [DataContract(Name = "GXUrlAlias")]
    [IndexCollection(false, nameof(Target), nameof(TargetId))]
    public class GXUrlAlias : GXTableBase, IUnique<Guid>, IValidatableObject
    {
        /// <summary>URL alias identifier.</summary>
        [Description("URL alias identifier.")]
        [Key, DataMember(Name = "ID"), Index(Unique = true)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>Unique URL alias name.</summary>
        [Description("Unique URL alias name.")]
        [DataMember, IsRequired, StringLength(128), Index(Unique = true)]
        [Filter(FilterType.Contains)]
        public string? Name { get; set; } = default!;

        /// <summary>
        /// Resource type identifier, for example "module" or "vendor.custom-resource".
        /// This is a string, not an enum or a CLR type that needs to be registered.
        /// </summary>
        [Description("Resource type identifier, for example \"module\" or \"vendor.custom-resource\". This is a string, not an enum or a CLR type that needs to be registered.")]
        [DataMember, IsRequired, StringLength(256)]
        [DefaultValue("")]
        [Filter(FilterType.Equals)]
        public string? Target { get; set; } = default!;

        /// <summary>
        /// Opaque resource identifier. May be a GUID, user identifier, or extension-defined string.
        /// </summary>
        [Description("Opaque resource identifier. May be a GUID, user identifier, or extension-defined string.")]
        [DataMember, IsRequired, StringLength(256)]
        [DefaultValue("")]
        [Filter(FilterType.Equals)]
        public string TargetId { get; set; } = string.Empty;

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember, Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual), IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>Time when the URL alias was last updated.</summary>
        [Description("Time when the URL alias was last updated.")]
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }

        /// <inheritdoc />
        public override void BeforeAdd()
        {
            Validator.ValidateObject(this, new ValidationContext(this), true);
            CreationTime ??= DateTimeOffset.UtcNow;
        }

        /// <inheritdoc />
        public override void BeforeUpdate()
        {
            Validator.ValidateObject(this, new ValidationContext(this), true);
            Updated = DateTimeOffset.UtcNow;
        }

        /// <inheritdoc />
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                yield return new ValidationResult("Alias Name must not be empty.", [nameof(Name)]);
            }

            if (string.IsNullOrWhiteSpace(Target))
            {
                yield return new ValidationResult("Alias Target must not be empty.", [nameof(Target)]);
            }

            if (string.IsNullOrWhiteSpace(TargetId))
            {
                yield return new ValidationResult("Alias TargetId must not be empty.", [nameof(TargetId)]);
            }
        }

        /// <inheritdoc />
        public override string ToString() => string.IsNullOrEmpty(Name) ? nameof(GXUrlAlias) : Name;
    }
}
