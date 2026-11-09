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
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the tokens.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class TokenController(IPatRepository patRepository) : ControllerBase
    {
        private readonly IPatRepository _patRepository = patRepository;

        /// <summary>
        /// Get available tokens.
        /// </summary>
        /// <returns>Get available tokens.</returns>
        [HttpGet]
        [Authorize(Policy = GXTokenPolicies.View)]
        public async Task<ActionResult<ListTokensResponse>> Get(CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            return await Post(new ListTokens(), cancellationToken);
        }

        /// <summary>
        /// List Tokens.
        /// </summary>
        /// <remarks>
        /// User can see only personal tokes.
        /// </remarks>
        [HttpPost("List")]
        [Authorize(Policy = GXTokenPolicies.View)]
        public async Task<ActionResult<ListTokensResponse>> Post(ListTokens request, CancellationToken cancellationToken = default)
        {
            ListTokensResponse ret = new()
            {
                Tokens = await _patRepository.GetPersonalTokensAsync(request, cancellationToken)
            };
            ret.Count = ret.Tokens.Count();
            return ret;
        }

        /// <summary>
        /// Update Token.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXTokenPolicies.Add)]
        public async Task<ActionResult<UpdateTokenResponse>> Post(UpdateToken request, CancellationToken cancellationToken = default)
        {
            if (request.Tokens == null || !request.Tokens.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateTokenResponse ret = new();
            foreach (GXPersonalToken it in request.Tokens)
            {
                if (string.IsNullOrEmpty(it.Name))
                {
                    return BadRequest(Properties.Resources.InvalidName);
                }
                if (string.IsNullOrEmpty(it.Id))
                {
                    ret.Token = await _patRepository.AddPersonalTokenAsync(it, cancellationToken);
                }
            }
            return ret;
        }

        /// <summary>Extends an owned token's expiration while keeping its value unchanged.</summary>
        [HttpPost("Extend")]
        [Authorize(Policy = GXTokenPolicies.Edit)]
        public async Task<ActionResult<ExtendTokenResponse>> Extend(ExtendToken request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Id) || request.Expiration == null)
                {
                return BadRequest("A token identifier and expiration are required.");
            }
            try
            {
                return new ExtendTokenResponse { Item = await _patRepository.ExtendPersonalTokenAsync(request.Id, request.Expiration.Value, cancellationToken) };
            }
            catch (ArgumentException error) { return BadRequest(error.Message); }
            catch (InvalidOperationException error) { return Conflict(error.Message); }
        }

        /// <summary>
        /// Delete token.
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXTokenPolicies.Delete)]
        public async Task<ActionResult<DeleteTokenResponse>> Post(DeleteToken request, CancellationToken cancellationToken = default)
        {
            if (request.Ids == null || !request.Ids.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            foreach (var id in request.Ids)
            {
                await _patRepository.RemovePersonalTokenAsync(id, cancellationToken);
            }
            return new DeleteTokenResponse();
        }
    }
}
