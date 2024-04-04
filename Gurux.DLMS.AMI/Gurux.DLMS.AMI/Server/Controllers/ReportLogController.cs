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
    /// This controller is used to handle the report errors.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ReportLogController : ControllerBase
    {
        private readonly IReportLogRepository _reportLogRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ReportLogController(IReportLogRepository reportErrorRepository)
        {
            _reportLogRepository = reportErrorRepository;
        }

        /// <summary>
        /// Get report error information.
        /// </summary>
        /// <param name="id">Report error id.</param>
        /// <returns>Report error.</returns>
        [HttpGet]
        [Authorize(Policy = GXReportLogPolicies.View)]
        public async Task<ActionResult<GetReportLogResponse>> Get(Guid id)
        {
            return new GetReportLogResponse()
            {
                Item = await _reportLogRepository.ReadAsync(User, id)
            };
        }

        /// <summary>
        /// Add report Error(s).
        /// </summary>
        [HttpPost("Add")]
        [Authorize(Policy = GXReportLogPolicies.Add)]
        public async Task<ActionResult<AddReportLogResponse>> Post(AddReportLog request)
        {
            await _reportLogRepository.AddAsync(User, request.Logs);
            AddReportLogResponse response = new AddReportLogResponse();
            return response;
        }

        /// <summary>
        /// List report Errors
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXReportLogPolicies.View)]
        public async Task<ActionResult<ListReportLogsResponse>> Post(
            ListReportLogs request,
            CancellationToken cancellationToken)
        {
            ListReportLogsResponse response = new ListReportLogsResponse();
            await _reportLogRepository.ListAsync(User, request, response, cancellationToken);
            return response;
        }

        /// <summary>
        /// Clear report error(s).
        /// </summary>
        [HttpPost("Clear")]
        [Authorize(Policy = GXReportLogPolicies.Clear)]
        public async Task<ActionResult<ClearReportLogsResponse>> Post(ClearReportLogs request)
        {
            await _reportLogRepository.ClearAsync(User, request.Reports);
            return new ClearReportLogsResponse();
        }

        /// <summary>
        /// Close report error(s).
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Close")]
        [Authorize(Policy = GXReportLogPolicies.Close)]
        public async Task<ActionResult<CloseReportLogResponse>> Post(CloseReportLog request)
        {
            await _reportLogRepository.CloseAsync(User, request.Logs);
            return new CloseReportLogResponse();
        }
    }
}
