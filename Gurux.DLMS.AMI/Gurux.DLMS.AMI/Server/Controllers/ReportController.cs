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
    /// This controller is used to handle the reports.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IReportRepository _reportRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ReportController(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }
        /// <summary>
        /// Get report information.
        /// </summary>
        /// <param name="id">Report id.</param>
        /// <returns>Report information.</returns>
        [HttpGet]
        [Authorize(Policy = GXReportPolicies.View)]
        public async Task<ActionResult<GetReportResponse>> Get(Guid id)
        {
            return new GetReportResponse()
            {
                Item = await _reportRepository.ReadAsync(User, id)
            };
        }
        /// <summary>
        /// List Reports.
        /// </summary>
        [HttpPost("List")]
        [AllowAnonymous]
        [Authorize(Policy = GXReportPolicies.View)]
        public async Task<ActionResult<ListReportsResponse>> Post(
            ListReports request,
            CancellationToken cancellationToken)
        {
            ListReportsResponse ret = new ListReportsResponse();
            await _reportRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }
        /// <summary>
        /// Update Report.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXReportPolicies.Add)]
        public async Task<ActionResult<UpdateReportResponse>> Post(UpdateReport request)
        {
            if (request.Reports == null || request.Reports.Count == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateReportResponse ret = new UpdateReportResponse();
            ret.Ids = await _reportRepository.UpdateAsync(User, request.Reports);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXReportPolicies.Delete)]
        public async Task<ActionResult<RemoveReportResponse>> Post(RemoveReport request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _reportRepository.DeleteAsync(User, request.Ids, request.Delete);
            return new RemoveReportResponse();
        }

        /// <summary>
        /// Count the reports values.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Send")]
        [Authorize(Policy = GXReportPolicies.Send)]
        public async Task<ActionResult<SendReportResponse>> Post(SendReport request)
        {
            if (request.Reports == null || !request.Reports.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            return new SendReportResponse()
            {
                Reports = (await _reportRepository.SendAsync(User, request.Reports)).ToArray()
            };
        }
    }
}