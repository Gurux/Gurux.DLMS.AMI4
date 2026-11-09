// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the logs.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LogController(
        ILogRepository LogRepository) : ControllerBase
    {
        private readonly ILogRepository _LogRepository = LogRepository;

        /// <summary>
        /// Get log information.
        /// </summary>
        /// <param name="id">log id.</param>
        /// <returns>log.</returns>
        /// <param name="source">The log source to query or modify.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        public async Task<ActionResult<GetLogResponse>> Get([FromQuery] string source, Guid id, CancellationToken cancellationToken = default)
        {
            return new GetLogResponse()
            {
                Item = await _LogRepository.ForSource(source).ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>
        /// List logs.
        /// </summary>
        [HttpPost("List")]
        public async Task<ActionResult<ListLogsResponse>> Post(
        [FromQuery] string? source, ListLogs request,
        CancellationToken cancellationToken)
        {
            ListLogsResponse ret = new();
            var repository = string.IsNullOrWhiteSpace(source) || source == "-" ? _LogRepository : _LogRepository.ForSource(source);
            await repository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Add logs.
        /// </summary>
        [HttpPost("Add")]
        public async Task<ActionResult<AddLogResponse>> Post([FromQuery] string source, AddLog request, CancellationToken cancellationToken = default)
        {
            AddLogResponse response = new()
            {
                Ids = await _LogRepository.ForSource(source).AddAsync(request.Type, request.Logs, cancellationToken)
            };
            return response;
        }

        /// <summary>
        /// Clear logs.
        /// </summary>
        /// <param name="request">Clear log request.</param>
        /// <returns></returns>
        /// <param name="source">The log source to query or modify.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Clear")]
        public async Task<ActionResult<ClearLogResponse>> Post([FromQuery] string source, ClearLog request, CancellationToken cancellationToken = default)
        {
            ClearLogResponse ret = new();
            await _LogRepository.ForSource(source).ClearAsync(request, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Close the logs(s).
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="source">The log source to query or modify.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Close")]
        public async Task<ActionResult<CloseLogResponse>> Post([FromQuery] string source, CloseLog request, CancellationToken cancellationToken = default)
        {
            await _LogRepository.ForSource(source).CloseAsync(request.Ids, cancellationToken);
            return new CloseLogResponse();
        }

        /// <summary>Handles the remove log request.</summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="source">The log source to query or modify.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Delete")]
        public async Task<ActionResult<RemoveLogResponse>> Post([FromQuery] string source, RemoveLog request, CancellationToken cancellationToken = default)
        {
            await _LogRepository.ForSource(source).DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveLogResponse();
        }
    }
}
