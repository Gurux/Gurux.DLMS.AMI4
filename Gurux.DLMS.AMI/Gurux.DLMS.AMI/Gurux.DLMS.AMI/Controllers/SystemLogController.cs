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
using Gurux.DLMS.AMI.Client.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Gurux.DLMS.AMI.Shared.DIs;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the system logs.
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
        /// Get available system logs.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>System logs.</returns>
        [HttpGet]
        [Authorize(Policy = GXSystemLogPolicies.View)]
        public async Task<ActionResult<ListSystemLogsResponse>> Get(CancellationToken cancellationToken)
        {
            return (await Post(new ListSystemLogs(), cancellationToken));
        }

        /// <summary>
        /// Add system log.
        /// </summary>
        /// <param name="request">Add system log request parameters.</param>
        /// <returns>Add system log response.</returns>
        [HttpPost("Add")]
        [Authorize(Policy = GXSystemLogPolicies.Add)]
        public async Task<ActionResult<AddSystemLogResponse>> Post(AddSystemLog request)
        {
            if (request.Item == null)
            {
                throw new ArgumentNullException();
            }
            await _repository.AddAsync(request.Type, [request.Item]);
            return new AddSystemLogResponse();
        }

        /// <summary>
        /// Get system logs.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXSystemLogPolicies.View)]
        public async Task<ActionResult<ListSystemLogsResponse>> Post(
            ListSystemLogs request,
            CancellationToken cancellationToken)
        {
            ListSystemLogsResponse ret = new ListSystemLogsResponse();
            await _repository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Clear system log(s).
        /// </summary>
        [HttpPost("Clear")]
        [Authorize(Policy = GXSystemLogPolicies.Clear)]
        public async Task<ActionResult<ClearSystemLogResponse>> Post(
            ClearSystemLog request)
        {
            await _repository.ClearAsync();
            return new ClearSystemLogResponse();
        }

        /// <summary>
        /// Close system log(s).
        /// </summary>
        [HttpPost("Close")]
        [Authorize(Policy = GXSystemLogPolicies.Close)]
        public async Task<ActionResult<CloseSystemLogResponse>> Post(CloseSystemLog request)
        {
            await _repository.CloseAsync(request.Logs);
            return new CloseSystemLogResponse();
        }
    }
}
