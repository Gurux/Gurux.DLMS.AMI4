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
    /// Policy requirement lists what policy is required.
    /// </summary>
    [DataContract(Name = "GXPolicyRequirement"), Serializable]
    [IndexCollection(false, nameof(Type), nameof(Value))]
    public partial class GXPolicyRequirement : IUnique<Guid>, IValidatableObject
    {
        /// <summary>
        /// Policy identifier.
        /// </summary>
        [Key]
        [DataMember(Name = "ID"), Index(Unique = true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public Guid Id { get; set; }

        /// <summary>
        /// Parent policy.
        /// </summary>
        [DataMember(Name = "PolicyID"), Index(Unique = false)]
        [ForeignKey(typeof(GXPolicy), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public GXPolicy? Policy { get; set; }


        /// <summary>
        /// Policy requirement type.
        /// </summary>
        [DataMember(Name = "Type"), Index(Unique = false)]
        [IsRequired]
        public PolicyRequirementType Type { get; set; }

        /// <summary>
        /// Claim type to match when Type is Claim. Independent of the display name.
        /// </summary>
        [DataMember]
        [StringLength(256)]
        public string? ClaimType { get; set; }

        /// <summary>
        /// Policy requirement value.
        /// </summary>
        [DataMember(Name = "Value"), Index(Unique = false)]
        [MaxLength(256)]
        public string? Value { get; set; }

        /// <summary>
        /// Optional display name of the requirement.
        /// </summary>
        [DataMember]
        [StringLength(128)]
        [Filter(FilterType.Equals)]
        public string? Name { get; set; }

        /// <summary>
        /// Policy description.
        /// </summary>
        [DataMember]
        public string? Description { get; set; }

        /// <summary>
        /// When was the policy last updated.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXPolicyRequirement()
        {

        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Name of the policy.</param>
        public GXPolicyRequirement(string? name)
        {
            Name = name;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name))
            {
                return Name;
            }
            return nameof(GXPolicyRequirement);
        }

        /// <inheritdoc />
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            switch (Type)
            {
                case PolicyRequirementType.Permission:
                case PolicyRequirementType.Role:
                    if (string.IsNullOrWhiteSpace(Value))
                    {
                        yield return new ValidationResult("Permission and Role requirements need a nonempty Value.", [nameof(Value)]);
                    }

                    break;
                case PolicyRequirementType.Claim:
                    if (string.IsNullOrWhiteSpace(ClaimType))
                    {
                        yield return new ValidationResult("Claim requirements need a ClaimType.", [nameof(ClaimType)]);
                    }

                    break;
                case PolicyRequirementType.AuthenticatedUser:
                case PolicyRequirementType.AnyUser:
                    break;
                default:
                    yield return new ValidationResult("This requirement type is not supported.", [nameof(Type)]);
                    break;
            }
        }
    }
}
