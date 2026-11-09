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

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the performances.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [Authorize(Roles = "admin")]
    [ApiController]
    public class PerformanceController(IPerformanceRepository performanceRepository) : ControllerBase
    {
        private readonly IPerformanceRepository _performanceRepository = performanceRepository;

        /// <summary>
        /// List Performances.
        /// </summary>
        [HttpPost("List")]
        public async Task<ActionResult<ListPerformancesResponse>> Post(
            ListPerformances request,
            CancellationToken cancellationToken)
        {
            ListPerformancesResponse ret = new();
            await _performanceRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>Handles the add performance request.</summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Add")]
        public async Task<ActionResult<AddPerformanceResponse>> Post(AddPerformance request, CancellationToken cancellationToken)
        {
            if (request.Performances == null || request.Performances.Count == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            AddPerformanceResponse ret = new()
            {
                Ids = await _performanceRepository.AddAsync(User, request.Performances, cancellationToken)
            };
            return ret;
        }

        /// <summary>
        /// Deletes performance records with the specified IDs.
        /// </summary>
        /// <param name="request">The request containing the IDs of the performance records to delete.</param>
        /// <returns>An action result containing the response for the delete operation.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Delete")]
        public async Task<ActionResult<RemovePerformanceResponse>> Post(RemovePerformance request, CancellationToken cancellationToken)
        {
            await _performanceRepository.DeleteAsync(User, request.Ids, cancellationToken);
            return new RemovePerformanceResponse();
        }

        /// <summary>Handles the clear performance request.</summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Clear")]
        public async Task<ActionResult<ClearPerformanceResponse>> Post(ClearPerformance request, CancellationToken cancellationToken)
        {
            _ = request;
            await _performanceRepository.ClearAsync(User, cancellationToken);
            return new ClearPerformanceResponse();
        }

    }
}
