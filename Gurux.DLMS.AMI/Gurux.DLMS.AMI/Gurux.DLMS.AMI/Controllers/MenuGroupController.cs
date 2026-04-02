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
    /// This controller is used to handle the Menu groups.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class MenuGroupController : ControllerBase
    {
        private readonly IMenuGroupRepository _MenuGroupRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MenuGroupController(
            IMenuGroupRepository MenuGroupRepository)
        {
            _MenuGroupRepository = MenuGroupRepository;
        }

        /// <summary>
        /// Get Menu group information.
        /// </summary>
        /// <param name="id">Menu group id.</param>
        /// <returns>Menu group.</returns>
        [HttpGet]
        [Authorize(Policy = GXMenuGroupPolicies.View)]
        public async Task<ActionResult<GetMenuGroupResponse>> Get(Guid id)
        {
            return new GetMenuGroupResponse()
            {
                Item = await _MenuGroupRepository.ReadAsync(id)
            };
        }

        /// <summary>
        /// Update Menu group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXMenuGroupPolicies.Add)]
        public async Task<ActionResult<AddMenuGroupResponse>> Post(AddMenuGroup request)
        {
            await _MenuGroupRepository.UpdateAsync(request.MenuGroups);
            return new AddMenuGroupResponse() { Ids = request.MenuGroups.Select(s => s.Id).ToArray() };
        }

        /// <summary>
        /// List Menu groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXMenuGroupPolicies.View)]
        public async Task<ActionResult<ListMenuGroupsResponse>> Post(
            ListMenuGroups request, CancellationToken cancellationToken)
        {
            ListMenuGroupsResponse ret = new ListMenuGroupsResponse();
            await _MenuGroupRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXMenuGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveMenuGroupResponse>> Post(RemoveMenuGroup request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _MenuGroupRepository.DeleteAsync(request.Ids, request.Delete);
            return new RemoveMenuGroupResponse();
        }
    }
}
