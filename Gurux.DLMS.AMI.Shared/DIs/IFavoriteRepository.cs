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

using System.Linq.Expressions;
using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle Favorites.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle Favorites.")]
    public interface IFavoriteRepository
    {
        /// <summary>
        /// List Favorites.
        /// </summary>
        /// <returns>Favorites.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List Favorites.")]
        Task<IEnumerable<GXFavorite>> ListAsync(
            ListFavorites? request = null,
            ListFavoritesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read Favorite.
        /// </summary>
        /// <param name="id">Favorite id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read Favorite.")]
        Task<GXFavorite> ReadAsync(Guid id, Expression<Func<GXFavorite, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new favorites or updates existing ones.
        /// </summary>
        /// <param name="favorites">The records to insert or update.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Inserts new favorites or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(

            IEnumerable<GXFavorite> favorites,
            Expression<Func<GXFavorite, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete Favorites.</summary>
        /// <param name="favorites">Favorite(s) to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Delete Favorites.")]
        Task DeleteAsync(IEnumerable<Guid> favorites, CancellationToken cancellationToken = default);
    }
}
