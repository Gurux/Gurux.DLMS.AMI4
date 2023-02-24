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

namespace Gurux.DLMS.AMI.Server.Controllers
{
    /// <summary>
    /// This controller is used to handle the REST statistics.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class RestStatisticController : ControllerBase
    {
        private readonly IRestStatisticRepository _restStatisticRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="restStatisticRepository">REST statistic repository.</param>
        public RestStatisticController(IRestStatisticRepository restStatisticRepository)
        {
            _restStatisticRepository = restStatisticRepository;
        }

        /// <summary>
        /// Get module or job settings.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("List")]
        public async Task<ActionResult<ListRestStatisticsResponse>> Post(
            ListRestStatistics request, 
            CancellationToken cancellationToken)
        {
            ListRestStatisticsResponse ret = new ListRestStatisticsResponse();
            await _restStatisticRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Add new REST statistics.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Add")]
        public async Task<ActionResult<AddRestStatisticResponse>> Post(AddRestStatistic request)
        {
            if (request.Statistics == null || request.Statistics.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            AddRestStatisticResponse ret = new();
            await _restStatisticRepository.AddAsync(User, request.Statistics);
            return ret;
        }      

        /// <summary>
        /// Clear REST statistics.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Clear")]
        public async Task<ActionResult<ClearRestStatisticResponse>> Post(ClearRestStatistic request)
        {
            await _restStatisticRepository.ClearAsync(User);
            return new ClearRestStatisticResponse();
        }
    }
}
