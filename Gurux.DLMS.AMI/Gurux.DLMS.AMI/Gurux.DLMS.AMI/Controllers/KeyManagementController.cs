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
using Gurux.DLMS.AMI.Client.Shared.KeyManagement;
using Gurux.DLMS.AMI.Server.Services.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Client.Shared;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the key management keys.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class KeyManagementController(IKeyManagementRepository keyManagementRepository) : ControllerBase
    {
        private readonly IKeyManagementRepository _keyManagementRepository = keyManagementRepository;

        /// <summary>
        /// Get key management information.
        /// </summary>
        /// <param name="id">KeyManagement id.</param>
        /// <returns>KeyManagement information.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXKeyManagementPolicies.View)]
        public async Task<ActionResult<GetKeyManagementResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetKeyManagementResponse()
            {
                Item = await _keyManagementRepository.ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>Acquires material for one explicitly authorized key operation.</summary>
        [HttpPost("Acquire")]
        [Authorize(Policy = GXKeyPermissions.Use)]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<GXKeyMaterial>> Acquire(
            GXAcquireKeyRequest request,
            [FromServices] GXKeyAcquisitionService service,
            CancellationToken cancellationToken = default)
        {
            return await service.AcquireAsync(User, request, cancellationToken);
        }

        /// <summary>
        /// List KeyManagements.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXKeyManagementPolicies.View)]
        public async Task<ActionResult<ListKeyManagementsResponse>> Post(
            ListKeyManagements request,
            CancellationToken cancellationToken)
        {
            ListKeyManagementsResponse ret = new();
            await _keyManagementRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update KeyManagement.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXKeyManagementPolicies.Add)]
        public async Task<ActionResult<UpdateKeyManagementResponse>> Post(UpdateKeyManagement request,
            CancellationToken cancellationToken = default)
        {
            UpdateKeyManagementResponse ret = new()
            {
                Ids = await _keyManagementRepository.UpsertAsync(request.KeyManagements, null, cancellationToken)
            };
            return ret;
        }

        /// <summary>
        /// Deletes specified keys from the key management system.
        /// </summary>
        /// <param name="request">The request containing the identifiers of the keys to be deleted and a flag indicating whether to perform
        /// the deletion.</param>
        /// <returns>A response indicating the result of the key deletion operation.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Delete")]
        [Authorize(Policy = GXKeyManagementPolicies.Delete)]
        public async Task<ActionResult<RemoveKeyManagementResponse>> Post(RemoveKeyManagement request, CancellationToken cancellationToken = default)
        {
            await _keyManagementRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveKeyManagementResponse();
        }
    }
}
