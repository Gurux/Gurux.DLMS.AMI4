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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.Service.Orm.Common;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested policy information.</summary>
    [Description("Response containing the requested policy information.")]
    public class GetPolicyResponse
    {
        /// <summary>
        /// Policy information.
        /// </summary>
        [Description("Policy information.")]
        [IncludeOpenApi(typeof(GXModule),
                nameof(GXModule.Id), nameof(GXModule.Name))]
        [ExcludeOpenApi(typeof(GXPolicyRequirement), nameof(GXPolicyRequirement.Policy))]
        public GXPolicy? Item { get; set; }
    }

    /// <summary>Requests changes to the specified policies.</summary>
    [Description("Requests changes to the specified policies.")]
    [DataContract]
    public class UpdatePolicy : IGXRequest<UpdatePolicyResponse>
    {
        /// <summary>Inserted or updated policies.</summary>
        [Description("Inserted or updated policies.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXPolicyRequirement), nameof(GXPolicyRequirement.Policy))]
        [ExcludeOpenApi(typeof(GXPolicy), nameof(GXPolicy.Updated))]
        [ExcludeOpenApi(typeof(GXPolicyRequirement), nameof(GXPolicyRequirement.Updated))]
        public IEnumerable<GXPolicy> Policys { get; set; } = default!;
    }

    /// <summary>Response to updating the specified policies.</summary>
    [Description("Response to updating the specified policies.")]
    [DataContract]
    public class UpdatePolicyResponse
    {
        /// <summary>Identifiers of the added or updated policy records.</summary>
        [Description("Identifiers of the added or updated policy records.")]
        [DataMember]
        public IEnumerable<Guid>? Ids { get; set; }
    }

    /// <summary>Requests a list of policies.</summary>
    [Description("Requests a list of policies.")]
    [DataContract]
    public class ListPolicys : IGXRequest<ListPolicysResponse>
    {
        /// <summary>
        /// Text to find in the policy identifier or display name.
        /// </summary>
        [Description("Text to find in the policy identifier or display name.")]
        public string? Search { get; set; }

        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of policies to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of policies to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>Optional criteria used to filter policies.</summary>
        [Description("Optional criteria used to filter policies.")]
        [ExcludeOpenApi(typeof(GXPolicyRequirement), nameof(GXPolicyRequirement.Policy))]
        [ExcludeOpenApi(typeof(GXPolicy),
            nameof(GXPolicy.NormalizedName), nameof(GXPolicy.DisplayName), nameof(GXPolicy.SystemDefined),
            nameof(GXPolicy.RequireAuthenticatedUser), nameof(GXPolicy.Description), nameof(GXPolicy.ConcurrencyStamp),
            nameof(GXPolicy.RequirementOperator))]
        public GXPolicy? Filter { get; set; }

        /// <summary>Gets or sets inclusive date and time ranges for properties of the filter type.</summary>
        /// <remarks>
        /// Ranges are combined with each other and with <see cref="Filter"/> using AND.
        /// Specify a public date and time property name. Null or an empty collection
        /// applies no additional date restrictions. Filtering precedes counting and paging.
        /// </remarks>
        [DataMember]
        [Description("Inclusive date and time ranges combined with the other filters using AND.")]
        public IEnumerable<GXDateTimeFilter>? DateFilters { get; set; }

        /// <summary>Additional related information to include in the response. Reserved for future use.</summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        [Description("Additional related information to include in the response. Reserved for future use.")]
        public IEnumerable<string>? Select { get; set; }

        /// <summary>Name of the property to sort by. If omitted, the endpoint uses its default ordering.</summary>
        /// <remarks>
        /// Uses the endpoint's default ordering when not specified.
        /// </remarks>
        /// <seealso cref="Descending"/>
        [Description("Name of the property to sort by. If omitted, the endpoint uses its default ordering.")]
        public string? OrderBy { get; set; }

        /// <summary>
        /// Whether to sort results in descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        [Description("Whether to sort results in descending order.")]
        public bool Descending { get; set; }

        /// <summary>Identifiers to include in the results. A nonempty collection restricts results to these identifiers.</summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        [Description("Identifiers to include in the results. A nonempty collection restricts results to these identifiers.")]
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        [Description("Identifiers to exclude from the results.")]
        public IEnumerable<Guid>? Exclude { get; set; }
    }

    /// <summary>Response containing the requested policies.</summary>
    [Description("Response containing the requested policies.")]
    [DataContract]
    public class ListPolicysResponse
    {
        /// <summary>List of policies.</summary>
        [Description("List of policies.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXPolicyRequirement), nameof(GXPolicyRequirement.Policy))]
        public IEnumerable<GXPolicy>? Policys { get; set; }

        /// <summary>
        /// Total number of matching policies before paging.
        /// </summary>
        [Description("Total number of matching policies before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests deletion of the selected policy records.</summary>
    [Description("Requests deletion of the selected policy records.")]
    [DataContract]
    public class PolicyDelete : IGXRequest<PolicyDeleteResponse>
    {
        /// <summary>Removed policies.</summary>
        [Description("Removed policies.")]
        [DataMember]
        public IEnumerable<string> Ids { get; set; } = default!;
    }

    /// <summary>Response to deleting policy.</summary>
    [Description("Response to deleting policy.")]
    [DataContract]
    public class PolicyDeleteResponse
    {
    }

    /// <summary>
    /// Regenerate policy.
    /// </summary>
    [Description("Regenerate policy.")]
    [DataContract]
    public class RegeneratePolicy : IGXRequest<RegeneratePolicyResponse>
    {
        /// <summary>
        /// Existing identifiers to reset to built-in defaults. Null or empty adds only missing defaults and preserves existing records.
        /// </summary>
        [Description("Existing identifiers to reset to built-in defaults. Null or empty adds only missing defaults and preserves existing records.")]
        [DataMember]
        public IEnumerable<Guid>? Ids { get; set; }
    }

    /// <summary>
    /// Regenerate policies response.
    /// </summary>
    [Description("Regenerate policies response.")]
    [DataContract]
    public class RegeneratePolicyResponse
    {
    }
}
