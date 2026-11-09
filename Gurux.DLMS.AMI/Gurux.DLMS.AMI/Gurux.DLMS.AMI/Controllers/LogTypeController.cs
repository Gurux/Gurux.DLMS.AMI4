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

using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Client.Shared;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the log groups.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class LogTypeController(
        ILogTypeRepository LogTypeRepository) : ControllerBase
    {
        private readonly ILogTypeRepository _LogTypeRepository = LogTypeRepository;

        /// <summary>
        /// Get Log group information.
        /// </summary>
        /// <param name="id">Log group id.</param>
        /// <returns>Log group.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXLogTypePolicies.View)]
        public async Task<ActionResult<GetLogTypeResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetLogTypeResponse()
            {
                Item = await _LogTypeRepository.ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>
        /// Update Log types.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize]
        public async Task<ActionResult<AddLogTypeResponse>> Post(AddLogType request, CancellationToken cancellationToken = default)
        {
            await _LogTypeRepository.UpsertAsync(request.LogTypes, null, cancellationToken);
            return new AddLogTypeResponse() { Ids = [.. request.LogTypes.Select(s => s.PublicId)] };
        }

        /// <summary>
        /// List Log types.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXLogTypePolicies.View)]
        public async Task<ActionResult<ListLogTypesResponse>> Post(
            ListLogTypes request, CancellationToken cancellationToken)
        {
            ListLogTypesResponse ret = new();
            await _LogTypeRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Deletes Log groups based on the specified request.
        /// </summary>
        /// <param name="request">The request containing the IDs of the Log groups to delete and a flag indicating whether to perform a soft
        /// delete.</param>
        /// <returns>A response indicating the result of the delete operation.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Delete")]
        [Authorize(Policy = GXLogTypePolicies.Delete)]
        public async Task<ActionResult<RemoveLogTypeResponse>> Post(RemoveLogType request, CancellationToken cancellationToken = default)
        {
            await _LogTypeRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveLogTypeResponse();
        }

        /// <summary>
        /// Regenerates log type items based on the specified identifiers.
        /// </summary>
        /// <param name="request">The request containing the identifiers of the log type items to regenerate.</param>
        /// <returns>A response indicating the result of the regeneration operation.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Regenerate")]
        [Authorize(Policy = GXLogTypePolicies.Edit)]
        public async Task<ActionResult<RegenerateLogTypeResponse>> Post(RegenerateLogType request, CancellationToken cancellationToken = default)
        {
            if (request.Ids?.Any() == true)
            {
                await _LogTypeRepository.ResetToDefaultsAsync(request.Ids, cancellationToken);
            }
            else
            {
                await _LogTypeRepository.EnsureDefaultsAsync(cancellationToken);
            }
            return new RegenerateLogTypeResponse();
        }
    }
}
