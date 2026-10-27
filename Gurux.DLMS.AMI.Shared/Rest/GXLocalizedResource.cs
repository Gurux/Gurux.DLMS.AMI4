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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.Service.Orm.Common;
using System.Runtime.Serialization;

using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested localized resource information.</summary>
    [Description("Response containing the requested localized resource information.")]
    public class GetLocalizedResourceResponse
    {
        /// <summary>Localized resource information.</summary>
        [Description("Localized resource information.")]
        [IncludeOpenApi(typeof(GXLanguage),
                nameof(GXLanguage.Id))]
        [IncludeOpenApi(typeof(GXUser),
                nameof(GXUser.Id))]
        public GXLocalizedResource? Item { get; set; }
    }

    /// <summary>Requests changes to the specified localized resources.</summary>
    [Description("Requests changes to the specified localized resources.")]
    [DataContract]
    public class UpdateLocalizedResource : IGXRequest<UpdateLocalizedResourceResponse>
    {
        /// <summary>
        /// Inserted or updated localized resources.
        /// </summary>
        [Description("Inserted or updated localized resources.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXLanguage),
                nameof(GXLanguage.Id))]
        [IncludeOpenApi(typeof(GXUser),
                nameof(GXUser.Id))]
        [ExcludeOpenApi(typeof(GXLocalizedResource), nameof(GXLocalizedResource.CreationTime), nameof(GXLocalizedResource.Updated))]
        public IEnumerable<GXLocalizedResource> LocalizedResources { get; set; } = default!;
    }

    /// <summary>Response to updating the specified localized resources.</summary>
    [Description("Response to updating the specified localized resources.")]
    [DataContract]
    public class UpdateLocalizedResourceResponse
    {
        /// <summary>Identifiers of the added or updated localized resource records.</summary>
        [Description("Identifiers of the added or updated localized resource records.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Requests a list of localized resources.</summary>
    [Description("Requests a list of localized resources.")]
    [DataContract]
    public class ListLocalizedResources : IGXRequest<ListLocalizedResourcesResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of localized resources to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of localized resources to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter localized resources.
        /// </summary>
        [Description("Optional criteria used to filter localized resources.")]
        [IncludeOpenApi(typeof(GXLanguage),
                nameof(GXLanguage.Id))]
        [IncludeOpenApi(typeof(GXUser),
                nameof(GXUser.Id))]
        [ExcludeOpenApi(typeof(GXLocalizedResource), nameof(GXLocalizedResource.ConcurrencyStamp))]
        public GXLocalizedResource? Filter { get; set; }

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

    /// <summary>Response containing the requested localized resources.</summary>
    [Description("Response containing the requested localized resources.")]
    [DataContract]
    public class ListLocalizedResourcesResponse
    {
        /// <summary>
        /// List of localized resources.
        /// </summary>
        [Description("List of localized resources.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXLanguage),
                nameof(GXLanguage.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public IEnumerable<GXLocalizedResource>? LocalizedResources { get; set; }

        /// <summary>
        /// Total number of matching localized resources before paging.
        /// </summary>
        [Description("Total number of matching localized resources before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests deletion of the selected localized resource records.</summary>
    [Description("Requests deletion of the selected localized resource records.")]
    [DataContract]
    public class LocalizedResourceDelete : IGXRequest<LocalizedResourceDeleteResponse>
    {
        /// <summary>
        /// Removed localized resources.
        /// </summary>
        [Description("Removed localized resources.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Response to deleting localized resource.</summary>
    [Description("Response to deleting localized resource.")]
    [DataContract]
    public class LocalizedResourceDeleteResponse
    {
    }
}
