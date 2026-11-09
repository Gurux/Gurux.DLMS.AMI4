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

using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Shared.DIs;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the favorites.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class FavoriteController(
        IFavoriteRepository FavoriteRepository) : ControllerBase
    {
        private readonly IFavoriteRepository _favoriteRepository = FavoriteRepository;

        /// <summary>
        /// Get favorite information.
        /// </summary>
        /// <param name="id">Agent group id.</param>
        /// <returns>Agent group.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<GetFavoriteResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetFavoriteResponse()
            {
                Item = (await _favoriteRepository.ReadAsync(id, null, cancellationToken))
            };
        }

        /// <summary>
        /// Update favorite.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize]
        public async Task<ActionResult<UpdateFavoriteResponse>> Post(UpdateFavorite request, CancellationToken cancellationToken = default)
        {
            return new UpdateFavoriteResponse()
            {
                Ids = await _favoriteRepository.UpsertAsync(request.Favorites, null, cancellationToken)
            };
        }

        /// <summary>
        /// List favorites.
        /// </summary>
        [HttpPost("List")]
        [Authorize]
        public async Task<ActionResult<ListFavoritesResponse>> Post(
            ListFavorites request,
            CancellationToken cancellationToken)
        {
            ListFavoritesResponse ret = new();
            await _favoriteRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Delete favorites.
        /// </summary>
        [HttpPost("Delete")]
        [Authorize]
        public async Task<ActionResult<RemoveFavoriteResponse>> Post(RemoveFavorite request, CancellationToken cancellationToken = default)
        {
            await _favoriteRepository.DeleteAsync(request.Ids, cancellationToken);
            return new RemoveFavoriteResponse();
        }
    }
}
