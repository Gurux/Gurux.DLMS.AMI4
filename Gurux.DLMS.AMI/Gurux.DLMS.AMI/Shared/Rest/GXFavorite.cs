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
using Gurux.Common;
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
        [ExcludeSwagger(typeof(GXFavorite), nameof(GXFavorite.User))]
        public GXFavorite? Item
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get list from favorites.
    /// </summary>
    [DataContract]
    public class ListFavorites : IGXRequest<ListFavoritesResponse>
    {
        /// <summary>
        /// Start index.
        /// </summary>
        public int Index
        {
            get;
            set;

        }

        /// <summary>
        /// Amount of the favorites to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter favorites.
        /// </summary>
        [IncludeSwagger(typeof(GXUser), nameof(GXUser.Id))]
        public GXFavorite? Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Order by name.
        /// </summary>
        /// <remarks>
        /// Default order by is used if this is not set.
        /// </remarks>
        /// <seealso cref="Descending"/>
        public string? OrderBy
        {
            get;
            set;
        }

        /// <summary>
        /// Are values shown as descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        public bool Descending
        {
            get;
            set;
        }
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
        [ExcludeSwagger(typeof(GXFavorite), nameof(GXFavorite.User))]
        public GXFavorite[]? Favorites
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the favoriters.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
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
        [ExcludeSwagger(typeof(GXFavorite),
            nameof(GXFavorite.CreationTime), nameof(GXFavorite.User))]
        public GXFavorite[] Favorites
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update favorites reply.
    /// </summary>
    [DataContract]
    public class UpdateFavoriteResponse
    {
        /// <summary>
        /// New favorite identifiers.
        /// </summary>
        [DataMember]
        public Guid[]? Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Delete favorites.
    /// </summary>
    [DataContract]
    public class RemoveFavorite : IGXRequest<RemoveFavoriteResponse>
    {
        /// <summary>
        /// Removed favorite identifiers.
        /// </summary>
        [DataMember]
        public Guid[] Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Reply from Delete favorite.
    /// </summary>
    [DataContract]
    public class RemoveFavoriteResponse
    {
    }
}
