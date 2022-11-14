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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.AMI.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Gurux.DLMS.AMI.Shared.DIs;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the system errors.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class SystemLogController : ControllerBase
    {
        private readonly ISystemLogRepository _repository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public SystemLogController(ISystemLogRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Get available system errors.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>System errors.</returns>
        [HttpGet]
        [Authorize(Policy = GXSystemLogPolicies.View)]
        public async Task<ActionResult<ListSystemLogsResponse>> Get(CancellationToken cancellationToken)
        {
            return (await Post(new ListSystemLogs(), cancellationToken));
        }

        /// <summary>
        /// Add system Error.
        /// </summary>
        /// <param name="request">Add system error request parameters.</param>
        /// <returns>Add system error response.</returns>
        [HttpPost("Add")]
        [Authorize(Policy = GXSystemLogPolicies.Add)]
        public async Task<ActionResult<AddSystemLogResponse>> Post(AddSystemLog request)
        {
            await _repository.AddAsync(User, new GXSystemLog[] { request.Error });
            return new AddSystemLogResponse();
        }

        /// <summary>
        /// Get system errors.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXSystemLogPolicies.View)]
        public async Task<ActionResult<ListSystemLogsResponse>> Post(
            ListSystemLogs request, 
            CancellationToken cancellationToken)
        {
            ListSystemLogsResponse ret = new ListSystemLogsResponse();
            await _repository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Clear system error(s).
        /// </summary>
        [HttpPost("Clear")]
        [Authorize(Policy = GXSystemLogPolicies.Clear)]
        public async Task<ActionResult<ClearSystemLogResponse>> Post(ClearSystemLog request)
        {
            await _repository.ClearAsync(User);
            return new ClearSystemLogResponse();
        }

        /// <summary>
        /// Close system error(s).
        /// </summary>
        [HttpPost("Close")]
        [Authorize(Policy = GXSystemLogPolicies.Close)]
        public async Task<ActionResult<CloseSystemLogResponse>> Post(CloseSystemLog request)
        {
            await _repository.CloseAsync(User, request.Errors);
            return new CloseSystemLogResponse();
        }
    }
}
