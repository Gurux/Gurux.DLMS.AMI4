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
using Gurux.DLMS.AMI.Shared.DIs;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Server.Models;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the subtotal errors.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class SubtotalLogController : ControllerBase
    {
        private readonly ISubtotalLogRepository _subtotalLogRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public SubtotalLogController(ISubtotalLogRepository subtotalErrorRepository)
        {
            _subtotalLogRepository = subtotalErrorRepository;
        }

        /// <summary>
        /// Get subtotal error information.
        /// </summary>
        /// <param name="id">Subtotal error id.</param>
        /// <returns>Subtotal error.</returns>
        [HttpGet]
        [Authorize(Policy = GXSubtotalLogPolicies.View)]
        public async Task<ActionResult<GetSubtotalLogResponse>> Get(Guid id)
        {
            return new GetSubtotalLogResponse()
            {
                Item = await _subtotalLogRepository.ReadAsync(User, id)
            };
        }

        /// <summary>
        /// Add subtotal Error(s).
        /// </summary>
        [HttpPost("Add")]
        [Authorize(Policy = GXSubtotalLogPolicies.Add)]
        public async Task<ActionResult<AddSubtotalLogResponse>> Post(AddSubtotalLog request)
        {
            await _subtotalLogRepository.AddAsync(User, request.Logs);
            AddSubtotalLogResponse response = new AddSubtotalLogResponse();
            return response;
        }

        /// <summary>
        /// List subtotal Errors
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXSubtotalLogPolicies.View)]
        public async Task<ActionResult<ListSubtotalLogsResponse>> Post(
            ListSubtotalLogs request,
            CancellationToken cancellationToken)
        {
            ListSubtotalLogsResponse response = new ListSubtotalLogsResponse();
            await _subtotalLogRepository.ListAsync(User, request, response, cancellationToken);
            return response;
        }

        /// <summary>
        /// Clear subtotal error(s).
        /// </summary>
        [HttpPost("Clear")]
        [Authorize(Policy = GXSubtotalLogPolicies.Clear)]
        public async Task<ActionResult<ClearSubtotalLogsResponse>> Post(ClearSubtotalLogs request)
        {
            await _subtotalLogRepository.ClearAsync(User, request.Subtotals);
            return new ClearSubtotalLogsResponse();
        }

        /// <summary>
        /// Close subtotal error(s).
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Close")]
        [Authorize(Policy = GXSubtotalLogPolicies.Close)]
        public async Task<ActionResult<CloseSubtotalLogResponse>> Post(CloseSubtotalLog request)
        {
            await _subtotalLogRepository.CloseAsync(User, request.Logs);
            return new CloseSubtotalLogResponse();
        }
    }
}
