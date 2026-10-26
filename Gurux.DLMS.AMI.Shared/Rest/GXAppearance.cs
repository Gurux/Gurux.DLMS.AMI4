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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get appearance.
    /// </summary>
    public class GetAppearanceResponse
    {
        /// <summary>
        /// Appearance information.
        /// </summary>        
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                      nameof(GXUser.UserName))]
        public GXAppearance? Item { get; set; }
    }

    /// <summary>
    /// Get list from appearances.
    /// </summary>
    [DataContract]
    public class ListAppearances : IGXRequest<ListAppearancesResponse>
    {
        /// <summary>Administrative listing including appearances unavailable to the current user.</summary>
        [DataMember]
        public bool AllUsers { get; set; }
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Maximum number of appearances to retrieve; zero disables paging.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter appearances.
        /// </summary>
        [ExcludeOpenApi(typeof(GXAppearance),
            nameof(GXAppearance.Description),
            nameof(GXAppearance.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        public GXAppearance? Filter { get; set; }

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
        public IEnumerable<string>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        public IEnumerable<string>? Exclude { get; set; }
    }

    /// <summary>
    /// Appearance items reply.
    /// </summary>
    [DataContract]
    public class ListAppearancesResponse
    {
        /// <summary>
        /// List of appearance items.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public IEnumerable<GXAppearance>? Appearances { get; set; }

        /// <summary>
        /// Total number of matching resourcers before paging.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>
    /// Update appearances.
    /// </summary>
    [DataContract]
    public class UpdateAppearance : IGXRequest<UpdateAppearanceResponse>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UpdateAppearance()
        {
            Appearances = new List<GXAppearance>();
        }

        /// <summary>
        /// Appearances to update.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public List<GXAppearance> Appearances { get; set; }
    }

    /// <summary>
    /// Update appearances reply.
    /// </summary>
    [DataContract]
    public class UpdateAppearanceResponse
    {
        /// <summary>
        /// Appearance identifiers.
        /// </summary>
        [DataMember]
        public IEnumerable<string>? Ids { get; set; }
    }

    /// <summary>
    /// Delete appearances.
    /// </summary>
    [DataContract]
    public class RemoveAppearance : IGXRequest<RemoveAppearanceResponse>
    {
        /// <summary>
        /// Appearance identifiers.
        /// </summary>
        [DataMember]
        public IEnumerable<string> Ids { get; set; } = default!;

        /// <summary>
        /// Whether to permanently delete the items from the database.
        /// </summary>
        /// <remarks>
        /// If false, marks the items as removed and retains their database records.
        /// </remarks>
        [DataMember]
        [Required]
        public bool Delete { get; set; }
    }

    /// <summary>
    /// Reply from Delete appearance.
    /// </summary>
    [DataContract]
    public class RemoveAppearanceResponse
    {
    }

    /// <summary>
    /// Regenerate appearance.
    /// </summary>
    [DataContract]
    public class RegenerateAppearance : IGXRequest<RegenerateAppearanceResponse>
    {
        /// <summary>
        /// If true, existing appearances will be overwritten.
        /// </summary>
        [DataMember]
        public bool OverwriteExisting { get; set; }

        /// <summary>
        /// Resource type. Image, Iconpack, Theme, Image. If null or empty, all types are regenerated.
        /// </summary>
        [DataMember]
        public int? Type { get; set; }

        /// <summary>
        /// Existing appearance IDs to restore; null or empty replaces all appearances with built-in defaults.
        /// </summary>
        [DataMember]
        public IEnumerable<string>? Ids { get; set; }
    }

    /// <summary>
    /// Regenerate appearance response.
    /// </summary>
    [DataContract]
    public class RegenerateAppearanceResponse
    {
        /// <summary>
        /// True, if there are new appearances available.
        /// </summary>
        public bool NewItems { get; set; }
    }
}
