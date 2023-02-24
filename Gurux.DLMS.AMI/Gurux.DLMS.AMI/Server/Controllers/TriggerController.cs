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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Models;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle triggers.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class TriggerController : ControllerBase
    {
        private readonly ITriggerRepository _triggerRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public TriggerController(ITriggerRepository triggerrRepository)
        {
            _triggerRepository = triggerrRepository;
        }

        /// <summary>
        /// Get trigger information.
        /// </summary>
        /// <param name="id">Trigger id.</param>
        /// <returns>Trigger information.</returns>
        [HttpGet]
        [Authorize(Policy = GXTriggerPolicies.View)]
        public async Task<ActionResult<GetTriggerResponse>> Get(Guid id)
        {
            return new GetTriggerResponse()
            {
                Item = await _triggerRepository.ReadAsync(User, id)
            };
        }

        /// <summary>
        /// List Triggers.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXTriggerPolicies.View)]
        public async Task<ActionResult<ListTriggersResponse>> Post(
            ListTriggers request, 
            CancellationToken cancellationToken)
        {
            ListTriggersResponse ret = new ListTriggersResponse();
            await _triggerRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update Trigger.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXTriggerPolicies.Add)]
        public async Task<ActionResult<UpdateTriggerResponse>> Post(UpdateTrigger request)
        {
            if (request.Triggers == null || request.Triggers.Count == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateTriggerResponse ret = new UpdateTriggerResponse();
            ret.TriggerIds = await _triggerRepository.UpdateAsync(User, request.Triggers);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXTriggerPolicies.Delete)]
        public async Task<ActionResult<RemoveTriggerResponse>> Post(RemoveTrigger request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _triggerRepository.DeleteAsync(User, request.Ids, request.Delete);
            return new RemoveTriggerResponse();
        }

        /// <summary>
        /// Refresh triggers.
        /// </summary>
        [HttpPost("Refresh")]
        [Authorize(Policy = GXTriggerPolicies.Edit)]
        public async Task<ActionResult<RefreshComponentViewResponse>> Post(RefreshComponentView request)
        {
            await _triggerRepository.RefrestAsync(User);
            return new RefreshComponentViewResponse();
        }
    }
}
