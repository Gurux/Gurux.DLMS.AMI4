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
    /// This controller is used to handle the subtotals.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class SubtotalController : ControllerBase
    {
        private readonly ISubtotalRepository _subtotalRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public SubtotalController(ISubtotalRepository subtotalRepository)
        {
            _subtotalRepository = subtotalRepository;
        }
        /// <summary>
        /// Get subtotal information.
        /// </summary>
        /// <param name="id">Subtotal id.</param>
        /// <returns>Subtotal information.</returns>
        [HttpGet]
        [Authorize(Policy = GXSubtotalPolicies.View)]
        public async Task<ActionResult<GetSubtotalResponse>> Get(Guid id)
        {
            return new GetSubtotalResponse()
            {
                Item = await _subtotalRepository.ReadAsync(User, id)
            };
        }
        /// <summary>
        /// List Subtotals.
        /// </summary>
        [HttpPost("List")]
        [AllowAnonymous]
        [Authorize(Policy = GXSubtotalPolicies.View)]
        public async Task<ActionResult<ListSubtotalsResponse>> Post(
            ListSubtotals request,
            CancellationToken cancellationToken)
        {
            ListSubtotalsResponse ret = new ListSubtotalsResponse();
            await _subtotalRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }
        /// <summary>
        /// Update Subtotal.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXSubtotalPolicies.Add)]
        public async Task<ActionResult<UpdateSubtotalResponse>> Post(UpdateSubtotal request)
        {
            if (request.Subtotals == null || request.Subtotals.Count == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateSubtotalResponse ret = new UpdateSubtotalResponse();
            ret.Ids = await _subtotalRepository.UpdateAsync(User, request.Subtotals);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXSubtotalPolicies.Delete)]
        public async Task<ActionResult<RemoveSubtotalResponse>> Post(RemoveSubtotal request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _subtotalRepository.DeleteAsync(User, request.Ids, request.Delete);
            return new RemoveSubtotalResponse();
        }

        /// <summary>
        /// Count the subtotals values.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Calculate")]
        [Authorize(Policy = GXSubtotalPolicies.Calculate)]
        public async Task<ActionResult<CalculateSubtotalResponse>> Post(CalculateSubtotal request)
        {
            await _subtotalRepository.CalculateAsync(User, request.Ids);
            return new CalculateSubtotalResponse();
        }

        /// <summary>
        /// Clear the subtotals values.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Clear")]
        [Authorize(Policy = GXSubtotalPolicies.Clear)]
        public async Task<ActionResult<ClearSubtotalResponse>> Post(ClearSubtotal request)
        {
            await _subtotalRepository.ClearAsync(User, request.Ids);
            return new ClearSubtotalResponse();
        }
    }
}
