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
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Shared.DIs;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the favorites.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class FavoriteController : ControllerBase
    {
        private readonly IFavoriteRepository _favoriteRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public FavoriteController(
            IFavoriteRepository FavoriteRepository)
        {
            _favoriteRepository = FavoriteRepository;
        }

        /// <summary>
        /// Get favorite information.
        /// </summary>
        /// <param name="id">Agent group id.</param>
        /// <returns>Agent group.</returns>
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<GetFavoriteResponse>> Get(Guid id)
        {
            return new GetFavoriteResponse()
            {
                Item = (await _favoriteRepository.ReadAsync(User, id))
            };
        }

        /// <summary>
        /// Update favorite.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize]
        public async Task<ActionResult<UpdateFavoriteResponse>> Post(UpdateFavorite request)
        {
            return new UpdateFavoriteResponse()
            {
                Ids = await _favoriteRepository.UpdateAsync(User, request.Favorites)
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
            ListFavoritesResponse ret = new ListFavoritesResponse();
            await _favoriteRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Delete favorites.
        /// </summary>
        [HttpPost("Delete")]
        [Authorize]
        public async Task<ActionResult<RemoveFavoriteResponse>> Post(RemoveFavorite request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _favoriteRepository.DeleteAsync(User, request.Ids);
            return new RemoveFavoriteResponse();
        }
    }
}
