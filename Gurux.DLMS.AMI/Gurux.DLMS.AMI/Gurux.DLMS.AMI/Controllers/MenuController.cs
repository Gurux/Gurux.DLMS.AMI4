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
using Gurux.DLMS.AMI.Client.Shared;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the menus.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class MenuController : ControllerBase
    {
        private readonly IMenuRepository _menuRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MenuController(IMenuRepository menuRepository)
        {
            _menuRepository = menuRepository;
        }

        /// <summary>
        /// Get menu information.
        /// </summary>
        /// <param name="id">Menu id.</param>
        /// <returns>Menu information.</returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<GetMenuResponse>> Get(Guid id)
        {
            return new GetMenuResponse()
            {
                Item = await _menuRepository.ReadAsync(id)
            };
        }

        /// <summary>
        /// Get menu information.
        /// </summary>
        /// <param name="name">Menu name.</param>
        /// <returns>Menu information.</returns>
        [HttpGet("name")]
        [AllowAnonymous]
        public async Task<ActionResult<GetMenuResponse>> Get(string name)
        {
            return new GetMenuResponse()
            {
                Item = await _menuRepository.ReadAsync(name)
            };
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
            ListMenusResponse ret = new ListMenusResponse();
            await _menuRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update Menu.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXMenuPolicies.Add)]
        public async Task<ActionResult<UpdateMenuResponse>> Post(UpdateMenu request)
        {
            if (request.Menus == null || request.Menus.Count == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateMenuResponse ret = new UpdateMenuResponse();
            ret.Ids = await _menuRepository.UpdateAsync(request.Menus);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXMenuPolicies.Delete)]
        public async Task<ActionResult<RemoveMenuResponse>> Post(RemoveMenu request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _menuRepository.DeleteAsync(request.Ids, request.Delete);
            return new RemoveMenuResponse();
        }
    }
}
