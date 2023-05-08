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
using Gurux.DLMS.AMI.Server.Models;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the manufacturer groups.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ManufacturerGroupController : ControllerBase
    {
        private readonly IManufacturerGroupRepository _ManufacturerGroupRepository;
      
        /// <summary>
        /// Constructor.
        /// </summary>
        public ManufacturerGroupController(
            IManufacturerGroupRepository ManufacturerGroupRepository)
        {
            _ManufacturerGroupRepository = ManufacturerGroupRepository;
        }

        /// <summary>
        /// Get manufacturer group information.
        /// </summary>
        /// <param name="id">Manufacturer group id.</param>
        /// <returns>Manufacturer group.</returns>
        [HttpGet]
        [Authorize(Policy = GXManufacturerGroupPolicies.View)]
        public async Task<ActionResult<GetManufacturerGroupResponse>> Get(Guid id)
        {
            return new GetManufacturerGroupResponse()
            {
                Item = await _ManufacturerGroupRepository.ReadAsync(User, id)
            };
        }

        /// <summary>
        /// Update manufacturer group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXManufacturerGroupPolicies.Add)]
        public async Task<ActionResult<AddManufacturerGroupResponse>> Post(AddManufacturerGroup request)
        {
            await _ManufacturerGroupRepository.UpdateAsync(User, request.ManufacturerGroups);
            return new AddManufacturerGroupResponse() { Ids = request.ManufacturerGroups.Select(s => s.Id).ToArray() };
        }

        /// <summary>
        /// List manufacturer groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXManufacturerGroupPolicies.View)]
        public async Task<ActionResult<ListManufacturerGroupsResponse>> Post(
            ListManufacturerGroups request, CancellationToken cancellationToken)
        {
            ListManufacturerGroupsResponse ret = new ListManufacturerGroupsResponse();
            await _ManufacturerGroupRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXManufacturerGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveManufacturerGroupResponse>> Post(RemoveManufacturerGroup request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _ManufacturerGroupRepository.DeleteAsync(User, request.Ids, request.Delete);
            return new RemoveManufacturerGroupResponse();
        }
    }
}
