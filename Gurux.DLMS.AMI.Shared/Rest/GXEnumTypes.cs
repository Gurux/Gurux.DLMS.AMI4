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
using System.Runtime.Serialization;
using System.ComponentModel;
using Gurux.DLMS.AMI.Shared.DTOs;
using System.ComponentModel.DataAnnotations;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Requests the addition of enum types.</summary>
    [Description("Requests the addition of enum types.")]
    [DataContract]
    public class AddEnumType : IGXRequest<AddEnumTypeResponse>
    {
        /// <summary>
        /// Enumeration type definition to add.
        /// </summary>
        [DataMember]
        [Description("Enumeration type definition to add.")]
        [Required]
        public GXEnumType? Item { get; set; }
    }

    /// <summary>Response to adding enum types.</summary>
    [DataContract]
    [Description("Response to adding enum types.")]
    public class AddEnumTypeResponse
    {
    }

    /// <summary>Requests a list of enum types.</summary>
    [DataContract]
    [Description("Requests a list of enum types.")]
    public class ListEnumTypes : IGXRequest<ListEnumTypesResponse>
    {
        /// <summary>
        /// Optional criteria used to filter enum types.
        /// </summary>
        [Description("Optional criteria used to filter enum types.")]
        [ExcludeOpenApi(typeof(GXEnumType), nameof(GXEnumType.Id), nameof(GXEnumType.Value))]
        public GXEnumType? Filter { get; set; }

        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of enum types to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of enum types to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

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
        public IEnumerable<int>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        [Description("Identifiers to exclude from the results.")]
        public IEnumerable<int>? Exclude { get; set; }
    }

    /// <summary>Response containing the requested enum types.</summary>
    [Description("Response containing the requested enum types.")]
    [DataContract]
    public class ListEnumTypesResponse
    {
        /// <summary>Enum types.</summary>
        [DataMember]
        [Description("Enum types.")]
        public IEnumerable<GXEnumType>? Types { get; set; }

        /// <summary>
        /// Amount of the enum types.
        /// </summary>
        [DataMember]
        [Description("Amount of the enum types.")]
        public long Count { get; set; }
    }
}
