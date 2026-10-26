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

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get localized resource.
    /// </summary>
    public class GetLocalizedResourceResponse
    {
        /// <summary>
        /// localized resource information.
        /// </summary>
        [IncludeOpenApi(typeof(GXLanguage),
                nameof(GXLanguage.Id))]
        [IncludeOpenApi(typeof(GXUser),
                nameof(GXUser.Id))]
        public GXLocalizedResource? Item { get; set; }
    }

    /// <summary>
    /// Add or Update localized resource information.
    /// </summary>
    [DataContract]
    public class UpdateLocalizedResource : IGXRequest<UpdateLocalizedResourceResponse>
    {
        /// <summary>
        /// Inserted or updated localized resources.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXLanguage),
                nameof(GXLanguage.Id))]
        [IncludeOpenApi(typeof(GXUser),
                nameof(GXUser.Id))]
        public IEnumerable<GXLocalizedResource> LocalizedResources { get; set; } = default!;
    }

    /// <summary>
    /// Update localized resource response.
    /// </summary>
    [DataContract]
    public class UpdateLocalizedResourceResponse
    {
        /// <summary>
        /// New localized resource identifier(s).
        /// </summary>
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>
    /// Get available localized resources.
    /// </summary>
    [DataContract]
    public class ListLocalizedResources : IGXRequest<ListLocalizedResourcesResponse>
    {
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Maximum number of localized resources to retrieve; zero disables paging.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter localized resources.
        /// </summary>
        [IncludeOpenApi(typeof(GXLanguage),
                nameof(GXLanguage.Id))]
        [IncludeOpenApi(typeof(GXUser),
                nameof(GXUser.Id))]
        public GXLocalizedResource? Filter { get; set; }

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
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        public IEnumerable<Guid>? Exclude { get; set; }
    }

    /// <summary>
    /// Returns localized resources.
    /// </summary>
    [DataContract]
    public class ListLocalizedResourcesResponse
    {
        /// <summary>
        /// List of localized resources.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXLanguage),
                nameof(GXLanguage.Id))]
        public IEnumerable<GXLocalizedResource>? LocalizedResources { get; set; }

        /// <summary>
        /// Total number of matching localized resources before paging.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>
    /// Delete localized resource.
    /// </summary>
    [DataContract]
    public class LocalizedResourceDelete : IGXRequest<LocalizedResourceDeleteResponse>
    {
        /// <summary>
        /// Removed localized resources.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>
    /// Delete localized resources response.
    /// </summary>
    [DataContract]
    public class LocalizedResourceDeleteResponse
    {
    }
}
