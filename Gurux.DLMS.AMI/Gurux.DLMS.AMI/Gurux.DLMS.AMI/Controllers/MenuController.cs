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
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Server.Internal;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the menus.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class MenuController(IMenuRepository menuRepository) : ControllerBase
    {
        private readonly IMenuRepository _menuRepository = menuRepository;

        /// <summary>
        /// Get menu information.
        /// </summary>
        /// <param name="id">Menu id.</param>
        /// <returns>Menu information.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<GetMenuResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return GXMenuHttpCache.Response(this, await _menuRepository.ReadAsync(id, null, cancellationToken));
        }

        /// <summary>
        /// Get menu information.
        /// </summary>
        /// <param name="name">Menu name.</param>
        /// <returns>Menu information.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet("name")]
        [AllowAnonymous]
        public async Task<ActionResult<GetMenuResponse>> Get(string name, CancellationToken cancellationToken = default)
        {
            return GXMenuHttpCache.Response(this, await _menuRepository.ReadAsync(name, null, cancellationToken));
        }

        /// <summary>
        /// List Menus.
        /// </summary>
        [HttpPost("List")]
        [AllowAnonymous]
        public async Task<ActionResult<ListMenusResponse>> Post(
            ListMenus request,
            CancellationToken cancellationToken)
        {
            ListMenusResponse ret = new();
            await _menuRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update Menu.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXMenuPolicies.Add)]
        public async Task<ActionResult<UpdateMenuResponse>> Post(UpdateMenu request, CancellationToken cancellationToken = default)
        {
            if (request.Menus == null || request.Menus.Count == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateMenuResponse ret = new()
            {
                Ids = await _menuRepository.UpsertAsync(request.Menus, null, cancellationToken)
            };
            return ret;
        }

        /// <summary>
        /// Deletes menu items specified in the request.
        /// </summary>
        /// <param name="request">The request containing menu item IDs and a flag indicating whether to perform a soft delete.</param>
        /// <returns>A response indicating the outcome of the delete operation.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Delete")]
        [Authorize(Policy = GXMenuPolicies.Delete)]
        public async Task<ActionResult<RemoveMenuResponse>> Post(RemoveMenu request, CancellationToken cancellationToken = default)
        {
            await _menuRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveMenuResponse();
        }

        /// <summary>
        /// Regenerates menu items based on the specified identifiers.
        /// </summary>
        /// <param name="request">The request containing the identifiers of the menu items to regenerate.</param>
        /// <returns>A response indicating the result of the regeneration operation.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Regenerate")]
        [Authorize(Policy = GXMenuPolicies.Edit)]
        public async Task<ActionResult<RegenerateMenuResponse>> Post(RegenerateMenu request, CancellationToken cancellationToken = default)
        {
            if (request.Ids?.Any() == true)
            {
                await _menuRepository.ResetToDefaultsAsync(request.Kind, request.Ids, cancellationToken);
            }
            else
            {
                await _menuRepository.EnsureDefaultsAsync(request.Kind, cancellationToken);
            }

            return new RegenerateMenuResponse();
        }
    }
}
