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

using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested appearance information.</summary>
    [Description("Response containing the requested appearance information.")]
    public class GetAppearanceResponse
    {
        /// <summary>
        /// Appearance information.
        /// </summary>        
        [Description("Appearance information.")]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                      nameof(GXUser.UserName))]
        public GXAppearance? Item { get; set; }
    }

    /// <summary>Requests a list of appearances.</summary>
    [Description("Requests a list of appearances.")]
    [DataContract]
    public class ListAppearances : IGXRequest<ListAppearancesResponse>
    {
        /// <summary>Administrative listing including appearances unavailable to the current user.</summary>
        [Description("Administrative listing including appearances unavailable to the current user.")]
        [DataMember]
        public bool AllUsers { get; set; }
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of appearances to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of appearances to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter appearances.
        /// </summary>
        [Description("Optional criteria used to filter appearances.")]
        [ExcludeOpenApi(typeof(GXAppearance),
            nameof(GXAppearance.Description),
            nameof(GXAppearance.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        [ExcludeOpenApi(typeof(GXAppearance),
            nameof(GXAppearance.Value), nameof(GXAppearance.StorageKey), nameof(GXAppearance.OriginalFileName),
            nameof(GXAppearance.ContentType), nameof(GXAppearance.ConcurrencyStamp), nameof(GXAppearance.Policies))]
        public GXAppearance? Filter { get; set; }

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

    /// <summary>Response containing the requested appearances.</summary>
    [Description("Response containing the requested appearances.")]
    [DataContract]
    public class ListAppearancesResponse
    {
        /// <summary>
        /// List of appearance items.
        /// </summary>
        [Description("List of appearance items.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public IEnumerable<GXAppearance>? Appearances { get; set; }

        /// <summary>
        /// Total number of matching resourcers before paging.
        /// </summary>
        [Description("Total number of matching resourcers before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests changes to the specified appearances.</summary>
    [Description("Requests changes to the specified appearances.")]
    [DataContract]
    public class UpdateAppearance : IGXRequest<UpdateAppearanceResponse>
    {
        /// <summary>Initializes a new instance of UpdateAppearance.</summary>
        [Description("Initializes a new instance of UpdateAppearance.")]
        public UpdateAppearance()
        {
            Appearances = [];
        }

        /// <summary>
        /// Appearances to update.
        /// </summary>
        [Description("Appearances to update.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [ExcludeOpenApi(typeof(GXAppearance), nameof(GXAppearance.CreationTime), nameof(GXAppearance.Updated))]
        public List<GXAppearance> Appearances { get; set; }
    }

    /// <summary>Response to updating the specified appearances.</summary>
    [Description("Response to updating the specified appearances.")]
    [DataContract]
    public class UpdateAppearanceResponse
    {
        /// <summary>Identifiers of the added or updated appearance records.</summary>
        [Description("Identifiers of the added or updated appearance records.")]
        [DataMember]
        public IEnumerable<Guid>? Ids { get; set; }
    }

    /// <summary>Requests deletion of the selected appearances.</summary>
    [Description("Requests deletion of the selected appearances.")]
    [DataContract]
    public class RemoveAppearance : IGXRequest<RemoveAppearanceResponse>
    {
        /// <summary>Identifiers of the appearance records to delete.</summary>
        [Description("Identifiers of the appearance records to delete.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;

        /// <summary>Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.</summary>
        /// <remarks>
        /// If false, marks the items as removed and retains their database records.
        /// </remarks>
        [Description("Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.")]
        [DataMember]
        public bool Delete { get; set; }
    }

    /// <summary>Response to deleting the selected appearances.</summary>
    [Description("Response to deleting the selected appearances.")]
    [DataContract]
    public class RemoveAppearanceResponse
    {
    }

    /// <summary>
    /// Regenerate appearance.
    /// </summary>
    [Description("Regenerate appearance.")]
    [DataContract]
    public class RegenerateAppearance : IGXRequest<RegenerateAppearanceResponse>
    {
        /// <summary>Legacy option retained for compatibility. Selected identifiers are reset; without identifiers only missing defaults are added.</summary>
        [Description("Legacy option retained for compatibility. Selected identifiers are reset; without identifiers only missing defaults are added.")]
        [DataMember]
        public bool OverwriteExisting { get; set; }

        /// <summary>
        /// Optional resource type filter. Null includes all resource types.
        /// </summary>
        [Description("Optional resource type filter. Null includes all resource types.")]
        [DataMember]
        public int? Type { get; set; }

        /// <summary>Existing identifiers to reset to built-in defaults. Null or empty adds only missing defaults and preserves existing records.</summary>
        [Description("Existing identifiers to reset to built-in defaults. Null or empty adds only missing defaults and preserves existing records.")]
        [DataMember]
        public IEnumerable<Guid>? Ids { get; set; }
    }

    /// <summary>
    /// Regenerate appearance response.
    /// </summary>
    [Description("Regenerate appearance response.")]
    [DataContract]
    public class RegenerateAppearanceResponse
    {
        /// <summary>
        /// True, if there are new appearances available.
        /// </summary>
        [Description("True, if there are new appearances available.")]
        public bool NewItems { get; set; }
    }
}
