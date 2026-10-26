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

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get favorite.
    /// </summary>
    public class GetFavoriteResponse
    {
        /// <summary>
        /// Favorite information.
        /// </summary>        
        [ExcludeOpenApi(typeof(GXFavorite), nameof(GXFavorite.User))]
        public GXFavorite? Item { get; set; }
    }

    /// <summary>
    /// Get list from favorites.
    /// </summary>
    [DataContract]
    public class ListFavorites : IGXRequest<ListFavoritesResponse>
    {
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Maximum number of favorites to retrieve; zero disables paging.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter favorites.
        /// </summary>
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public GXFavorite? Filter { get; set; }

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
    }

    /// <summary>
    /// Favorite items reply.
    /// </summary>
    [DataContract]
    public class ListFavoritesResponse
    {
        /// <summary>
        /// List of favorite items.
        /// </summary>
        [DataMember]
        [ExcludeOpenApi(typeof(GXFavorite), nameof(GXFavorite.User))]
        public IEnumerable<GXFavorite>? Favorites { get; set; }

        /// <summary>
        /// Total number of matching favoriters before paging.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>
    /// Update favorites.
    /// </summary>
    [DataContract]
    public class UpdateFavorite : IGXRequest<UpdateFavoriteResponse>
    {
        /// <summary>
        /// Favorites to update.
        /// </summary>
        [DataMember]
        [ExcludeOpenApi(typeof(GXFavorite),
            nameof(GXFavorite.CreationTime), nameof(GXFavorite.User))]
        public IEnumerable<GXFavorite> Favorites { get; set; } = default!;
    }

    /// <summary>
    /// Update favorites reply.
    /// </summary>
    [DataContract]
    public class UpdateFavoriteResponse
    {
        /// <summary>
        /// Favorite identifiers.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid>? Ids { get; set; }
    }

    /// <summary>
    /// Delete favorites.
    /// </summary>
    [DataContract]
    public class RemoveFavorite : IGXRequest<RemoveFavoriteResponse>
    {
        /// <summary>
        /// Favorite identifiers.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>
    /// Reply from Delete favorite.
    /// </summary>
    [DataContract]
    public class RemoveFavoriteResponse
    {
    }
}
