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

    /// <summary>Requests changes to the localization.</summary>
    [Description("Requests changes to the localization.")]
    [DataContract]
    public class UpdateLocalization : IGXRequest<UpdateLocalizationResponse>
    {
        /// <summary>
        /// Localization to add.
        /// </summary>
        [Description("Localization to add.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXLocalizedResource), nameof(GXLocalizedResource.Language), nameof(GXLocalizedResource.Creator))]
        [ExcludeOpenApi(typeof(GXLocalizedResource), nameof(GXLocalizedResource.CreationTime), nameof(GXLocalizedResource.Updated))]
        public IEnumerable<GXLanguage> Localizations { get; set; } = default!;
    }

    /// <summary>Response to updating the localization.</summary>
    [Description("Response to updating the localization.")]
    [DataContract]
    public class UpdateLocalizationResponse
    {
        /// <summary>
        /// Localization identifiers.
        /// </summary>
        [Description("Localization identifiers.")]
        [DataMember]
        public IEnumerable<Guid> LocalizationIds { get; set; } = default!;
    }

    /// <summary>Requests a list of languages.</summary>
    [Description("Requests a list of languages.")]
    [DataContract]
    public class ListLanguages : IGXRequest<ListLanguagesResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of localizations to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of localizations to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter localizations.
        /// </summary>
        [Description("Optional criteria used to filter localizations.")]
        [ExcludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Resources))]
        [ExcludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.EnglishName), nameof(GXLanguage.NativeName))]
        public GXLanguage? Filter { get; set; }

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

    /// <summary>Response containing the requested languages.</summary>
    [Description("Response containing the requested languages.")]
    [DataContract]
    public class ListLanguagesResponse
    {
        /// <summary>
        /// List of languages.
        /// </summary>
        [Description("List of languages.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXLocalizedResource), nameof(GXLocalizedResource.Language),
            nameof(GXLocalizedResource.Creator))]
        public IEnumerable<GXLanguage>? Languages { get; set; }

        /// <summary>
        /// Total number of matching languages before paging.
        /// </summary>
        [Description("Total number of matching languages before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests deletion of the selected localizations.</summary>
    [Description("Requests deletion of the selected localizations.")]
    [DataContract]
    public class RemoveLocalization : IGXRequest<RemoveLocalizationResponse>
    {
        /// <summary>Identifiers of the localization records to delete.</summary>
        [Description("Identifiers of the localization records to delete.")]
        [DataMember]
        public IEnumerable<string>? Ids { get; set; }
    }

    /// <summary>Response to deleting the selected localizations.</summary>
    [Description("Response to deleting the selected localizations.")]
    [DataContract]
    public class RemoveLocalizationResponse
    {
    }

    /// <summary>Refresh localized strings.</summary>
    [Description("Refresh localized strings.")]
    [DataContract]
    public class RefreshLocalization
    {
        /// <summary>
        /// Refrest localizations for the selected languages.
        /// </summary>
        [Description("Refrest localizations for the selected languages.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Id))]
        public IEnumerable<GXLanguage>? Languages { get; set; }
    }
}
