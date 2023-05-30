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
    /// This controller is used to handle the KeyManagement groups.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class KeyManagementGroupController : ControllerBase
    {
        private readonly IKeyManagementGroupRepository _KeyManagementGroupRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public KeyManagementGroupController(
            IKeyManagementGroupRepository KeyManagementGroupRepository)
        {
            _KeyManagementGroupRepository = KeyManagementGroupRepository;
        }

        /// <summary>
        /// Get key management group information.
        /// </summary>
        /// <param name="id">KeyManagement group id.</param>
        /// <returns>KeyManagement group.</returns>
        [HttpGet]
        [Authorize(Policy = GXKeyManagementGroupPolicies.View)]
        public async Task<ActionResult<GetKeyManagementGroupResponse>> Get(Guid id)
        {
            return new GetKeyManagementGroupResponse()
            {
                Item = await _KeyManagementGroupRepository.ReadAsync(User, id)
            };
        }

        /// <summary>
        /// Update key management group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXKeyManagementGroupPolicies.Add)]
        public async Task<ActionResult<AddKeyManagementGroupResponse>> Post(AddKeyManagementGroup request)
        {
            await _KeyManagementGroupRepository.UpdateAsync(User, request.KeyManagementGroups);
            return new AddKeyManagementGroupResponse()
            {
                Ids = request.KeyManagementGroups.Select(s => s.Id).ToArray()
            };
        }

        /// <summary>
        /// List key management groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXKeyManagementGroupPolicies.View)]
        public async Task<ActionResult<ListKeyManagementGroupsResponse>> Post(
            ListKeyManagementGroups request,
            CancellationToken cancellationToken)
        {
            ListKeyManagementGroupsResponse ret = new ListKeyManagementGroupsResponse();
            await _KeyManagementGroupRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Delete key management groups.
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXKeyManagementGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveKeyManagementGroupResponse>> Post(RemoveKeyManagementGroup request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _KeyManagementGroupRepository.DeleteAsync(User, request.Ids, request.Delete);
            return new RemoveKeyManagementGroupResponse();
        }
    }
}
