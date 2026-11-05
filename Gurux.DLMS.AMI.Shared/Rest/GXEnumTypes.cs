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

using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.Service.Orm.Common;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

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

    /// <summary>Requests deletion of the selected enum types.</summary>
    [Description("Requests deletion of the selected enum types.")]
    [DataContract]
    public class RemoveEnumType : IGXRequest<RemoveEnumTypeResponse>
    {
        /// <summary>Identifiers of the enum type records to delete.</summary>
        [Description("Identifiers of the enum type records to delete.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;

        /// <summary>Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.</summary>
        /// <remarks>
        /// If false, marks the items as removed and retains their database records.
        /// </remarks>
        [Description("Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.")]
        [DataMember]
        [Required]
        public bool Delete { get; set; }
    }

    /// <summary>Response to deleting the selected enum types.</summary>
    [Description("Response to deleting the selected enum types.")]
    [DataContract]
    public class RemoveEnumTypeResponse
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

    /// <summary>
    /// Regenerate enum type.
    /// </summary>
    [Description("Regenerate enum type.")]
    [DataContract]
    public class RegenerateEnumTypes : IGXRequest<RegenerateEnumTypesResponse>
    {
        /// <summary>Existing identifiers to reset to built-in defaults. Null or empty adds only missing defaults and preserves existing records.</summary>
        [Description("Existing identifiers to reset to built-in defaults. Null or empty adds only missing defaults and preserves existing records.")]
        [DataMember]
        public IEnumerable<int>? Ids { get; set; }
    }

    /// <summary>
    /// Regenerate enum types response.
    /// </summary>
    [Description("Regenerate enum types response.")]
    [DataContract]
    public class RegenerateEnumTypesResponse
    {
    }
}
