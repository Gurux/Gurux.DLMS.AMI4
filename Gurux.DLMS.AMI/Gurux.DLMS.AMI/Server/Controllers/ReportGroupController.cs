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
    /// This controller is used to handle the report groups.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ReportGroupController : ControllerBase
    {
        private readonly IReportGroupRepository _ReportGroupRepository;
      
        /// <summary>
        /// Constructor.
        /// </summary>
        public ReportGroupController(
            IReportGroupRepository ReportGroupRepository)
        {
            _ReportGroupRepository = ReportGroupRepository;
        }

        /// <summary>
        /// Get report group information.
        /// </summary>
        /// <param name="id">Report group id.</param>
        /// <returns>Report group.</returns>
        [HttpGet]
        [Authorize(Policy = GXReportGroupPolicies.View)]
        public async Task<ActionResult<GetReportGroupResponse>> Get(Guid id)
        {
            return new GetReportGroupResponse()
            {
                Item = await _ReportGroupRepository.ReadAsync(User, id)
            };
        }

        /// <summary>
        /// Update report group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXReportGroupPolicies.Add)]
        public async Task<ActionResult<AddReportGroupResponse>> Post(AddReportGroup request)
        {
            await _ReportGroupRepository.UpdateAsync(User, request.ReportGroups);
            return new AddReportGroupResponse() { Ids = request.ReportGroups.Select(s => s.Id).ToArray() };
        }

        /// <summary>
        /// List report groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXReportGroupPolicies.View)]
        public async Task<ActionResult<ListReportGroupsResponse>> Post(
            ListReportGroups request, CancellationToken cancellationToken)
        {
            ListReportGroupsResponse ret = new ListReportGroupsResponse();
            await _ReportGroupRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXReportGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveReportGroupResponse>> Post(RemoveReportGroup request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _ReportGroupRepository.DeleteAsync(User, request.Ids, request.Delete);
            return new RemoveReportGroupResponse();
        }
    }
}
