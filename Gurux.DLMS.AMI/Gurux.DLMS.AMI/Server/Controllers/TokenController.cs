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
using Gurux.DLMS.AMI.Server.Models;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the tokens.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly IGXHost host;
        private readonly IPatRepository _patRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public TokenController(IGXHost value,
            IPatRepository patRepository)
        {
            host = value;
            _patRepository = patRepository;
        }

        /// <summary>
        /// Get available tokens.
        /// </summary>
        /// <returns>Get available tokens.</returns>
        [HttpGet]
        [Authorize(Policy = GXTokenPolicies.View)]
        public async Task<ActionResult<ListTokensResponse>> Get()
        {
            return await Post(new ListTokens());
        }

        /// <summary>
        /// List Tokens.
        /// </summary>
        /// <remarks>
        /// User can see only personal tokes.
        /// </remarks>
        [HttpPost("List")]
        [Authorize(Policy = GXTokenPolicies.View)]
        public async Task<ActionResult<ListTokensResponse>> Post(ListTokens request)
        {
            ListTokensResponse ret = new ListTokensResponse();
            ret.Tokens = await _patRepository.GetPersonalTokensAsync(User, request);
            ret.Count = ret.Tokens.Length;
            return ret;
        }

        /// <summary>
        /// Update Token.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXTokenPolicies.Add)]
        public async Task<ActionResult<UpdateTokenResponse>> Post(UpdateToken request)
        {
            if (request.Tokens == null || !request.Tokens.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateTokenResponse ret = new UpdateTokenResponse();
            foreach (GXPersonalToken it in request.Tokens)
            {
                if (string.IsNullOrEmpty(it.Name))
                {
                    return BadRequest(Properties.Resources.InvalidName);
                }
                if (string.IsNullOrEmpty(it.Id))
                {
                    ret.Token = await _patRepository.AddPersonalTokenAsync(User, it);
                }
            }
            return ret;
        }

        /// <summary>
        /// Delete token.
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXTokenPolicies.Delete)]
        public async Task<ActionResult<DeleteTokenResponse>> Post(DeleteToken request)
        {
            if (request.Ids == null || !request.Ids.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            foreach (var id in request.Ids)
            {
                await _patRepository.RemovePersonalTokenAsync(User, id);
            }
            return new DeleteTokenResponse();
        }
    }
}
