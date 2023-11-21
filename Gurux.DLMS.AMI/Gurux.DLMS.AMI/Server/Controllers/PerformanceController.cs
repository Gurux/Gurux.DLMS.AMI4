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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the performances.
    /// </summary>
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    [ApiController]
    public class PerformanceController : ControllerBase
    {
        private readonly IPerformanceRepository _performanceRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public PerformanceController(IPerformanceRepository performanceRepository)
        {
            _performanceRepository = performanceRepository;
        }

        /// <summary>
        /// List Performances.
        /// </summary>
        [HttpPost("List")]
        public async Task<ActionResult<ListPerformancesResponse>> Post(
            ListPerformances request,
            CancellationToken cancellationToken)
        {
            ListPerformancesResponse ret = new ListPerformancesResponse();
            await _performanceRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <inheritdoc/>
        [HttpPost("Add")]
        public async Task<ActionResult<AddPerformanceResponse>> Post(AddPerformance request)
        {
            if (request.Performances == null || request.Performances.Count == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            AddPerformanceResponse ret = new AddPerformanceResponse();
            ret.Ids = await _performanceRepository.AddAsync(User, request.Performances);
            return ret;
        }

        /// <inheritdoc/>
        [HttpPost("Delete")]
        public async Task<ActionResult<RemovePerformanceResponse>> Post(RemovePerformance request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _performanceRepository.DeleteAsync(User, request.Ids);
            return new RemovePerformanceResponse();
        }

        /// <inheritdoc/>
        [HttpPost("Clear")]
        public async Task<ActionResult<ClearPerformanceResponse>> Post(ClearPerformance request)
        {
            await _performanceRepository.ClearAsync(User);
            return new ClearPerformanceResponse();
        }

    }
}
