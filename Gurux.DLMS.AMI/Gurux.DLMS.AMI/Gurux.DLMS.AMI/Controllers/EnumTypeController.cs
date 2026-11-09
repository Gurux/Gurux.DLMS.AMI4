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
using Gurux.DLMS.AMI.Client.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Gurux.DLMS.AMI.Shared.DIs;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the enum types.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class EnumTypeController(IEnumTypeRepository repository) : ControllerBase
    {
        private readonly IEnumTypeRepository _repository = repository;

        /// <summary>
        /// Add enum type.
        /// </summary>
        /// <param name="request">Add enum type request parameters.</param>
        /// <returns>Add enum type response.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Add")]
        [Authorize(Policy = GXUserPolicies.Add)]
        public async Task<ActionResult<AddEnumTypeResponse>> Post(AddEnumType request, CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            if (string.IsNullOrEmpty(request?.Item?.Type) || string.IsNullOrEmpty(request.Item.Name))
            {
                throw new ArgumentException("Enum type and name are required.", nameof(request));
            }
            await _repository.GetLogTypeAsync(request.Item.Type, request.Item.Name, cancellationToken);
            return new AddEnumTypeResponse();
        }

        /// <summary>
        /// Get enum types.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXUserPolicies.View)]
        public async Task<ActionResult<ListEnumTypesResponse>> Post(
            ListEnumTypes request,
            CancellationToken cancellationToken)
        {
            ListEnumTypesResponse ret = new();
            await _repository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Regenerates enum types items based on the specified identifiers.
        /// </summary>
        /// <param name="request">The request containing the identifiers of the enum types items to regenerate.</param>
        /// <returns>A response indicating the result of the regeneration operation.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Regenerate")]
        [Authorize(Policy = GXUserPolicies.Edit)]
        public async Task<ActionResult<RegenerateEnumTypesResponse>> Post(RegenerateEnumTypes request, CancellationToken cancellationToken = default)
        {
            if (request.Ids?.Any() == true)
            {
                await _repository.ResetToDefaultsAsync(request.Ids, cancellationToken);
            }
            else
            {
                await _repository.EnsureDefaultsAsync(cancellationToken);
            }
            return new RegenerateEnumTypesResponse();
        }
    }
}
