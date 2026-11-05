// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

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
        /// <returns>A task whose result contains the requested favorite.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read Favorite.")]
        Task<GXFavorite> ReadAsync(Guid id, Expression<Func<GXFavorite, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new favorites or updates existing ones.
        /// </summary>
        /// <param name="favorites">The records to insert or update.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new favorites or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(

            IEnumerable<GXFavorite> favorites,
            Expression<Func<GXFavorite, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete Favorites.</summary>
        /// <param name="favorites">Favorite(s) to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete Favorites.</returns>
        [System.ComponentModel.Description("Delete Favorites.")]
        Task DeleteAsync(IEnumerable<Guid> favorites, CancellationToken cancellationToken = default);
    }
}
