//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.UrlAlias
{
    /// <summary>
    /// An URL alias for one resource, including resource types supplied by extensions.
    /// </summary>
    [DataContract(Name = "GXUrlAlias")]
    [IndexCollection(false, nameof(Target), nameof(TargetId))]
    public class GXUrlAlias : GXTableBase, IUnique<Guid>, IValidatableObject
    {
        /// <summary>URL alias identifier.</summary>
        [Key, DataMember(Name = "ID"), Index(Unique = true)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>Unique URL alias name.</summary>
        [DataMember, IsRequired, StringLength(128), Index(Unique = true)]
        [Filter(FilterType.Contains)]
        public string? Name { get; set; } = default!;

        /// <summary>
        /// Resource type identifier, for example "module" or "vendor.custom-resource".
        /// This is a string, not an enum or a CLR type that needs to be registered.
        /// </summary>
        [DataMember, IsRequired, StringLength(256)]
        [DefaultValue("")]
        [Filter(FilterType.Equals)]
        public string? Target { get; set; } = default!;

        /// <summary>
        /// Opaque resource identifier. May be a GUID, user identifier, or extension-defined string.
        /// </summary>
        [DataMember, Required, IsRequired, StringLength(256)]
        [DefaultValue("")]
        [Filter(FilterType.Equals)]
        public string TargetId { get; set; } = string.Empty;

        /// <summary>Creation time.</summary>
        [DataMember, Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual), IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>Last update time.</summary>
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
