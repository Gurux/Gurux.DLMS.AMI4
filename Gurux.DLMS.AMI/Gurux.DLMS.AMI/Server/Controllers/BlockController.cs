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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Models;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the blocks.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class BlockController : ControllerBase
    {
        private readonly IBlockRepository _blockRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public BlockController(IBlockRepository blockRepository)
        {
            _blockRepository = blockRepository;
        }

        /// <summary>
        /// Get block information.
        /// </summary>
        /// <param name="id">Block id.</param>
        /// <returns>Block information.</returns>
        [HttpGet]
        [Authorize(Policy = GXBlockPolicies.View)]
        public async Task<ActionResult<GXBlock>> Get(Guid id)
        {
            return await _blockRepository.ReadAsync(User, id);
        }

        /// <summary>
        /// List Blocks.
        /// </summary>
        [HttpPost("List")]
        [AllowAnonymous]
        [Authorize(Policy = GXBlockPolicies.View)]
        public async Task<ActionResult<ListBlocksResponse>> Post(
            ListBlocks request, 
            CancellationToken cancellationToken)
        {
            ListBlocksResponse ret = new ListBlocksResponse();
            await _blockRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update Block.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXBlockPolicies.Add)]
        public async Task<ActionResult<UpdateBlockResponse>> Post(UpdateBlock request)
        {
            if (request.Blocks == null || request.Blocks.Count == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateBlockResponse ret = new UpdateBlockResponse();
            ret.BlockIds = await _blockRepository.UpdateAsync(User, request.Blocks);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXBlockPolicies.Delete)]
        public async Task<ActionResult<DeleteBlockResponse>> Post(DeleteBlock request)
        {
            if (request.BlockIds == null || request.BlockIds.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _blockRepository.DeleteAsync(User, request.BlockIds);
            return new DeleteBlockResponse();
        }

        /// <summary>
        /// Close the blocks(s).
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Close")]
        [Authorize(Policy = GXBlockPolicies.Close)]
        public async Task<ActionResult<CloseBlockResponse>> Post(CloseBlock request)
        {
            await _blockRepository.CloseAsync(User, request.Blocks);
            return new CloseBlockResponse();
        }
    }
}
