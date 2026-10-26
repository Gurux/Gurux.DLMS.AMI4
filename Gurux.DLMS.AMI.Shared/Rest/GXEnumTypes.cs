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
    /// <summary>
    /// Add new enum type.
    /// </summary>
    [DataContract]
    public class AddEnumType : IGXRequest<AddEnumTypeResponse>
    {
        /// <summary>
        /// Enumeration type definition to add.
        /// </summary>
        [DataMember]
        [Description("Log item.")]
        [Required]
        public GXEnumType? Item { get; set; }
    }

    /// <summary>
    /// Add enum type response.
    /// </summary>
    [DataContract]
    [Description("Add enum typeresponse.")]
    public class AddEnumTypeResponse
    {
    }

    /// <summary>
    /// Get enum types.
    /// </summary>
    [DataContract]
    [Description("Get enum types.")]
    public class ListEnumTypes : IGXRequest<ListEnumTypesResponse>
    {
        /// <summary>
        /// Optional criteria used to filter enum types.
        /// </summary>
        public GXEnumType? Filter { get; set; }

        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Maximum number of enum types to retrieve; zero disables paging.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Selected extra information.
        /// </summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        public IEnumerable<string>? Select { get; set; }

        /// <summary>
        /// Name of the property to sort by.
        /// </summary>
        /// <remarks>
        /// Uses the endpoint's default ordering when not specified.
        /// </remarks>
        /// <seealso cref="Descending"/>
        public string? OrderBy { get; set; }

        /// <summary>
        /// Whether to sort results in descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        public bool Descending { get; set; }

        /// <summary>
        /// Identifiers to include in the results.
        /// </summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        public IEnumerable<int>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        public IEnumerable<int>? Exclude { get; set; }
    }

    /// <summary>
    /// Get enum types response.
    /// </summary>
    [Description("Get enum types response.")]
    [DataContract]
    public class ListEnumTypesResponse
    {
        /// <summary>
        /// enum types.
        /// </summary>
        [DataMember]
        [Description("enum types.")]
        public IEnumerable<GXEnumType>? Types { get; set; }

        /// <summary>
        /// Amount of the enum types.
        /// </summary>
        [DataMember]
        [Description("Amount of the enum types.")]
        public int Count { get; set; }
    }
}
