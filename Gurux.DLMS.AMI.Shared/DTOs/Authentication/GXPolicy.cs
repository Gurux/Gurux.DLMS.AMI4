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
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Authentication
{
    /// <summary>
    /// A named rule that controls access to a resource or operation.
    /// </summary>
    [Description("A named rule that controls access to a resource or operation.")]
    [DataContract(Name = "GXPolicy"), Serializable]
    public partial class GXPolicy : IUnique<Guid>, IValidatableObject
    {
        /// <summary>
        /// Policy identifier.
        /// </summary>
        [Description("Policy identifier.")]
        [Key]
        [DataMember(Name = "ID"), Index(Unique = true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public Guid Id { get; set; }

        private string name = string.Empty;

        /// <summary>
        /// Stable application identifier of the policy. It cannot be renamed after creation.
        /// </summary>
        [Description("Stable application identifier of the policy. It cannot be renamed after creation.")]
        [DataMember]
        [Index(Unique = true)]
        [Required]
        [StringLength(128)]
        [Filter(FilterType.Equals)]
        [IsRequired]
        public string Name
        {
            get => name;
            set
            {
                name = value;
                NormalizedName = value?.ToUpperInvariant() ?? string.Empty;
            }
        }

        /// <summary>
        /// Culture-independent lookup key that makes identifier uniqueness consistent across databases.
        /// </summary>
        [Description("Culture-independent lookup key that makes identifier uniqueness consistent across databases.")]
        [DataMember]
        [StringLength(128)]
        [Index(Unique = true)]
        [DefaultValue("")]
        public string NormalizedName { get; set; } = string.Empty;

        /// <summary>
        /// User-editable display name. Name remains the authorization identifier.
        /// </summary>
        [Description("User-editable display name. Name remains the authorization identifier.")]
        [DataMember]
        [StringLength(128)]
        public string? DisplayName { get; set; }

        /// <summary>
        /// Whether this policy is maintained by the application rather than public editing APIs.
        /// </summary>
        [Description("Whether this policy is maintained by the application rather than public editing APIs.")]
        [DataMember]
        [DefaultValue(false)]
        public bool SystemDefined { get; set; }

        /// <summary>
        /// Require authentication independently of the permission requirement operator.
        /// </summary>
        [Description("Require authentication independently of the permission requirement operator.")]
        [DataMember]
        [DefaultValue(true)]
        public bool RequireAuthenticatedUser { get; set; } = true;

        /// <summary>
        /// Policy description.
        /// </summary>
        [Description("Policy description.")]
        [DataMember]
        public string? Description { get; set; }

        /// <summary>Time when the policy was last updated.</summary>
        [Description("Time when the policy was last updated.")]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }

        /// <summary>
        /// Time when policy was removed.
        /// </summary>
        /// <remarks>
        /// In filter if the removed time is set it will return values that are not null.
        /// </remarks>
        [Description("Time when policy was removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>Version token used by the database to detect conflicting updates.</summary>
        /// <remarks>
        /// A stale token causes a conflicting update to be rejected instead of overwriting newer changes.
        /// </remarks>
        [Description("Version token used by the database to detect conflicting updates.")]
        [DataMember]
        [StringLength(36)]
        [MaxLength(36)]
        [ConcurrencyCheck]
        public string? ConcurrencyStamp { get; set; }

        /// <summary>
        /// Gets or sets the logical operator used to combine policy requirements.
        /// </summary>
        [Description("Gets or sets the logical operator used to combine policy requirements.")]
        [DataMember]
        [DefaultValue(PolicyRequirementOperator.Or)]
        public PolicyRequirementOperator RequirementOperator { get; set; } = PolicyRequirementOperator.Or;

        /// <summary>
        /// Policy requirements.
        /// </summary>
        [Description("Policy requirements.")]
        [DataMember]
        [ForeignKey(typeof(GXPolicyRequirement))]
        [Filter(FilterType.Contains)]
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public List<GXPolicyRequirement>? Requirements { get; set; }

        /// <summary>Initializes a new instance of GXPolicy.</summary>
        [Description("Initializes a new instance of GXPolicy.")]
        public GXPolicy()
        {

        }

        /// <summary>Initializes a new instance of GXPolicy.</summary>
        /// <param name="name">Name of the policy.</param>
        [Description("Initializes a new instance of GXPolicy.")]
        public GXPolicy(string name)
        {
            Name = name;
            Requirements = [];
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(DisplayName))
            {
                return DisplayName;
            }
            if (!string.IsNullOrEmpty(Name))
            {
                return Name;
            }
            return nameof(GXPolicy);
        }

        /// <inheritdoc />
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Name) || Name != Name.Trim())
            {
                yield return new ValidationResult("Policy Name must be a nonempty identifier without surrounding whitespace.", [nameof(Name)]);
            }

            if (!Enum.IsDefined(RequirementOperator))
            {
                yield return new ValidationResult("Unsupported policy requirement operator.", [nameof(RequirementOperator)]);
            }

            if (Requirements?.Count > 1 && Requirements.Any(r => r.Type == PolicyRequirementType.AuthenticatedUser))
            {
                yield return new ValidationResult("Use RequireAuthenticatedUser for authentication alongside permission requirements.", [nameof(Requirements)]);
            }
        }
    }
}
