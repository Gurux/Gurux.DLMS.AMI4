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
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm.Common;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{

    /// <summary>
    /// Update localization.
    /// </summary>
    [DataContract]
    public class UpdateLocalization : IGXRequest<UpdateLocalizationResponse>
    {
        /// <summary>
        /// Localization to add.
        /// </summary>
        [DataMember]
        [ExcludeOpenApi(typeof(GXLocalizedResource), nameof(GXLocalizedResource.Language), nameof(GXLocalizedResource.Creator))]
        public IEnumerable<GXLanguage> Localizations { get; set; } = default!;
    }

    /// <summary>
    /// Update reader localization.
    /// </summary>
    [DataContract]
    public class UpdateLocalizationResponse
    {
        /// <summary>
        /// Localization identifiers.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid> LocalizationIds { get; set; } = default!;
    }

    /// <summary>
    /// Get list from localizations.
    /// </summary>
    [DataContract]
    public class ListLanguages : IGXRequest<ListLanguagesResponse>
    {
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Maximum number of localizations to retrieve; zero disables paging.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter localizations.
        /// </summary>
        [ExcludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Resources))]
        public GXLanguage? Filter { get; set; }

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
    /// Get localizations response.
    /// </summary>
    [DataContract]
    public class ListLanguagesResponse
    {
        /// <summary>
        /// List of languages.
        /// </summary>
        [DataMember]
        [ExcludeOpenApi(typeof(GXLocalizedResource), nameof(GXLocalizedResource.Language),
            nameof(GXLocalizedResource.Creator))]
        public IEnumerable<GXLanguage>? Languages { get; set; }

        /// <summary>
        /// Total number of matching languages before paging.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>
    /// Remove localizations.
    /// </summary>
    [DataContract]
    public class RemoveLocalization : IGXRequest<RemoveLocalizationResponse>
    {
        /// <summary>
        /// Language identifiers to remove.
        /// </summary>
        [DataMember]
        public IEnumerable<string>? Ids { get; set; }
    }

    /// <summary>
    /// Remove localizations response.
    /// </summary>
    [DataContract]
    public class RemoveLocalizationResponse
    {
    }

    /// <summary>
    /// Refresh localizated strings.
    /// </summary>
    [DataContract]
    public class RefreshLocalization
    {
        /// <summary>
        /// Refrest localizations for the selected languages.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Id))]
        public IEnumerable<GXLanguage>? Languages { get; set; }
    }
}
