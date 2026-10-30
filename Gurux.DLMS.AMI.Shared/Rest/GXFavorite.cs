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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;

using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested favorite information.</summary>
    [Description("Response containing the requested favorite information.")]
    public class GetFavoriteResponse
    {
        /// <summary>
        /// Favorite information.
        /// </summary>        
        [Description("Favorite information.")]
        [ExcludeOpenApi(typeof(GXFavorite), nameof(GXFavorite.User))]
        public GXFavorite? Item { get; set; }
    }

    /// <summary>Requests a list of favorites.</summary>
    [Description("Requests a list of favorites.")]
    [DataContract]
    public class ListFavorites : IGXRequest<ListFavoritesResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of favorites to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of favorites to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter favorites.
        /// </summary>
        [Description("Optional criteria used to filter favorites.")]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [ExcludeOpenApi(typeof(GXFavorite), nameof(GXFavorite.Type), nameof(GXFavorite.Path))]
        public GXFavorite? Filter { get; set; }

        /// <summary>Gets or sets inclusive date and time ranges for properties of the filter type.</summary>
        /// <remarks>
        /// Ranges are combined with each other and with <see cref="Filter"/> using AND.
        /// Specify a public stored date and time property name. Null or an empty collection
        /// applies no additional date restrictions. Filtering precedes counting and paging.
        /// </remarks>
        [DataMember]
        [Description("Inclusive date and time ranges combined with the other filters using AND.")]
        public IEnumerable<GXDateTimeFilter>? DateFilters { get; set; }

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
    }

    /// <summary>Response containing the requested favorites.</summary>
    [Description("Response containing the requested favorites.")]
    [DataContract]
    public class ListFavoritesResponse
    {
        /// <summary>
        /// List of favorite items.
        /// </summary>
        [Description("List of favorite items.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXFavorite), nameof(GXFavorite.User))]
        public IEnumerable<GXFavorite>? Favorites { get; set; }

        /// <summary>
        /// Total number of matching favoriters before paging.
        /// </summary>
        [Description("Total number of matching favoriters before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests changes to the specified favorites.</summary>
    [Description("Requests changes to the specified favorites.")]
    [DataContract]
    public class UpdateFavorite : IGXRequest<UpdateFavoriteResponse>
    {
        /// <summary>
        /// Favorites to update.
        /// </summary>
        [Description("Favorites to update.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXFavorite),
            nameof(GXFavorite.CreationTime), nameof(GXFavorite.User))]
        [ExcludeOpenApi(typeof(GXFavorite), nameof(GXFavorite.Updated))]
        public IEnumerable<GXFavorite> Favorites { get; set; } = default!;
    }

    /// <summary>Response to updating the specified favorites.</summary>
    [Description("Response to updating the specified favorites.")]
    [DataContract]
    public class UpdateFavoriteResponse
    {
        /// <summary>Identifiers of the added or updated favorite records.</summary>
        [Description("Identifiers of the added or updated favorite records.")]
        [DataMember]
        public IEnumerable<Guid>? Ids { get; set; }
    }

    /// <summary>Requests deletion of the selected favorites.</summary>
    [Description("Requests deletion of the selected favorites.")]
    [DataContract]
    public class RemoveFavorite : IGXRequest<RemoveFavoriteResponse>
    {
        /// <summary>Identifiers of the favorite records to delete.</summary>
        [Description("Identifiers of the favorite records to delete.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Response to deleting the selected favorites.</summary>
    [Description("Response to deleting the selected favorites.")]
    [DataContract]
    public class RemoveFavoriteResponse
    {
    }
}
