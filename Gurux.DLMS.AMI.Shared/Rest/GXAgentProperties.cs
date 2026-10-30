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
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.Service.Orm.Common;
using System.Runtime.Serialization;

using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested agent property.</summary>
    [Description("Response containing the requested agent property.")]
    public class GetAgentProperties
    {
        /// <summary>
        /// Agent properties information.
        /// </summary>
        [Description("Agent properties information.")]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id)
            , nameof(GXAgent.Name))]
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id))]
        public GXAgentProperty? Item { get; set; }
    }

    /// <summary>Requests changes to the specified agent properties.</summary>
    [Description("Requests changes to the specified agent properties.")]
    [DataContract]
    public class UpdateAgentProperties : IGXRequest<UpdateAgentPropertiesResponse>
    {
        /// <summary>
        /// Agent properties to add.
        /// </summary>
        [Description("Agent properties to add.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id))]
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id))]
        [ExcludeOpenApi(typeof(GXAgentProperty), nameof(GXAgentProperty.CreationTime), nameof(GXAgentProperty.Updated))]
        public IEnumerable<GXAgentProperty> Properties { get; set; } = default!;
    }

    /// <summary>Response to updating the agent properties.</summary>
    [Description("Response to updating the agent properties.")]
    [DataContract]
    public class UpdateAgentPropertiesResponse
    {
        /// <summary>Identifiers of the added or updated agent property records.</summary>
        [Description("Identifiers of the added or updated agent property records.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Requests a list of agent properties.</summary>
    [Description("Requests a list of agent properties.")]
    [DataContract]
    public class ListAgentProperties
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of agent properties to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of agent properties to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter agent properties.
        /// </summary>
        [Description("Optional criteria used to filter agent properties.")]
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id))]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id))]
        [ExcludeOpenApi(typeof(GXAgentProperty),
            nameof(GXAgentProperty.Name), nameof(GXAgentProperty.Value), nameof(GXAgentProperty.ConcurrencyStamp))]
        public GXAgentProperty? Filter { get; set; }

        /// <summary>Gets or sets inclusive date and time ranges for properties of the filter type.</summary>
        /// <remarks>
        /// Ranges are combined with each other and with <see cref="Filter"/> using AND.
        /// Specify a public stored date and time property name. Null or an empty collection
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

    /// <summary>Response containing the requested agent properties.</summary>
    [Description("Response containing the requested agent properties.")]
    [DataContract]
    public class ListAgentPropertiesResponse
    {
        /// <summary>
        /// List of agent properties items.
        /// </summary>
        [Description("List of agent properties items.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id))]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id))]
        public IEnumerable<GXAgentProperty> Properties { get; set; } = default!;

        /// <summary>
        /// Total number of matching user properties items before paging.
        /// </summary>
        [Description("Total number of matching user properties items before paging.")]
        [DataMember]
        public long Count { get; set; }
    }
}
