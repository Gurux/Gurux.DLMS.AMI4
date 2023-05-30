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
    /// This controller is used to handle the key management keys.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class KeyManagementController : ControllerBase
    {
        private readonly IKeyManagementRepository _keyManagementRepository;
        /// <summary>
        /// Constructor.
        /// </summary>
        public KeyManagementController(IKeyManagementRepository keyManagementRepository)
        {
            _keyManagementRepository = keyManagementRepository;
        }

        /// <summary>
        /// Get key management information.
        /// </summary>
        /// <param name="id">KeyManagement id.</param>
        /// <returns>KeyManagement information.</returns>
        [HttpGet]
        [Authorize(Policy = GXKeyManagementPolicies.View)]
        public async Task<ActionResult<GetKeyManagementResponse>> Get(Guid id)
        {
            return new GetKeyManagementResponse()
            {
                Item = await _keyManagementRepository.ReadAsync(User, id)
            };
        }

        /// <summary>
        /// List KeyManagements.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXKeyManagementPolicies.View)]
        public async Task<ActionResult<ListKeyManagementsResponse>> Post(
            ListKeyManagements request,
            CancellationToken cancellationToken)
        {
            ListKeyManagementsResponse ret = new ListKeyManagementsResponse();
            await _keyManagementRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update KeyManagement.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXKeyManagementPolicies.Add)]
        public async Task<ActionResult<UpdateKeyManagementResponse>> Post(UpdateKeyManagement request)
        {
            if (request.KeyManagements == null || request.KeyManagements.Count == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateKeyManagementResponse ret = new UpdateKeyManagementResponse();
            ret.Ids = await _keyManagementRepository.UpdateAsync(User, request.KeyManagements);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXKeyManagementPolicies.Delete)]
        public async Task<ActionResult<RemoveKeyManagementResponse>> Post(RemoveKeyManagement request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _keyManagementRepository.DeleteAsync(User, request.Ids, request.Delete);
            return new RemoveKeyManagementResponse();
        }
    }
}
