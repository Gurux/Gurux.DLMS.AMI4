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
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Server.Internal;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the blocks.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class BlockController(IBlockRepository blockRepository) : ControllerBase
    {
        private readonly IBlockRepository _blockRepository = blockRepository;

        /// <summary>
        /// Get block information.
        /// </summary>
        /// <param name="id">Block id.</param>
        /// <returns>Block information.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXBlockPolicies.View)]
        public async Task<ActionResult<GetBlockResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetBlockResponse()
            {
                Item = await _blockRepository.ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>Reads the visible block's content from a published server file.</summary>
        /// <param name="id">Block identifier.</param>
        /// <param name="environment">Server file providers.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet("Content/{id:guid}")]
        [Authorize]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> GetContent(Guid id, [FromServices] IWebHostEnvironment environment,
            CancellationToken cancellationToken = default)
        {
            // Use the same group visibility checks as layout block listing.
            var blocks = await _blockRepository.ListAsync(new ListBlocks { Included = [id] }, null, cancellationToken);
            var block = blocks.SingleOrDefault();
            if (block == null)
            {
                return NotFound();
            }
            if (block.BlockType is not (BlockType.HtmlFile or BlockType.MarkdownFile))
            {
                return BadRequest("This block does not use a content file.");
            }
            try
            {
                var content = await Gurux.DLMS.AMI.Server.Internal.GXBlockFileContent.ReadAsync(environment.WebRootFileProvider, block.Body, cancellationToken);
                return Content(content, "text/plain; charset=utf-8");
            }
            catch (FileNotFoundException error)
            {
                return NotFound(error.Message);
            }
        }

        /// <summary>Reads an RSS block through the server to avoid browser CORS restrictions.</summary>
        [HttpGet("Feed/{id:guid}")]
        [Authorize]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<GXBlockFeed>> GetFeed(Guid id, [FromServices] IHttpClientFactory clients,
            CancellationToken cancellationToken = default)
        {
            var blocks = await _blockRepository.ListAsync(new ListBlocks { Included = [id], ExcludeClosed = true }, null, cancellationToken);
            var block = blocks.SingleOrDefault();
            if (block == null)
            {
                return NotFound();
            }
            if (block.BlockType != BlockType.RSS)
            {
                return BadRequest("This block is not an RSS feed.");
            }
            try
            {
                using var http = clients.CreateClient();
                return await Gurux.DLMS.AMI.Server.Internal.GXBlockRssFeed.ReadAsync(http, block.Body, cancellationToken);
            }
            catch (ArgumentException)
            {
                return BadRequest("Invalid RSS feed URL.");
            }
            catch (OperationCanceledException)when (!cancellationToken.IsCancellationRequested)
            {
                return StatusCode(StatusCodes.Status504GatewayTimeout, "RSS feed request timed out.");
            }
            catch (Exception error)when (error is HttpRequestException or System.Xml.XmlException or InvalidDataException)
            {
                return StatusCode(StatusCodes.Status502BadGateway, "Unable to read the RSS feed.");
            }
        }

        /// <summary>
        /// List Blocks.
        /// </summary>
        [HttpPost("List")]
        [AllowAnonymous]
        public async Task<ActionResult<ListBlocksResponse>> Post(
            ListBlocks request,
            CancellationToken cancellationToken)
        {
            ListBlocksResponse ret = new();
            await _blockRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>Gets the current user's active layout blocks with conditional cache validation.</summary>
        [HttpGet("Active")]
        [AllowAnonymous]
        public async Task<ActionResult<ListBlocksResponse>> GetActive(CancellationToken cancellationToken = default)
        {
            var result = new ListBlocksResponse();
            await _blockRepository.ListAsync(new ListBlocks
            {
                ExcludeClosed = true,
                Filter = new GXBlock { Enabled = true }
            }, result, cancellationToken);
            return GXBlockHttpCache.Response(this, result);
        }

        /// <summary>
        /// Update Block.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXBlockPolicies.Add)]
        public async Task<ActionResult<UpdateBlockResponse>> Post(UpdateBlock request, CancellationToken cancellationToken = default)
        {
            if (request.Blocks == null || request.Blocks.Count == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateBlockResponse ret = new()
            {
                Ids = await _blockRepository.UpsertAsync(request.Blocks, null, cancellationToken)
            };
            return ret;
        }

        /// <summary>Handles the remove block request.</summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Delete")]
        [Authorize(Policy = GXBlockPolicies.Delete)]
        public async Task<ActionResult<RemoveBlockResponse>> Post(RemoveBlock request,
            CancellationToken cancellationToken = default)
        {
            await _blockRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveBlockResponse();
        }

        /// <summary>
        /// Close the blocks(s).
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Close")]
        [Authorize(Policy = GXBlockPolicies.Close)]
        public async Task<ActionResult<CloseBlockResponse>> Post(CloseBlock request, CancellationToken cancellationToken = default)
        {
            await _blockRepository.CloseAsync(request.Ids, cancellationToken);
            return new CloseBlockResponse();
        }

        /// <summary>
        /// Regenerate blocks.
        /// </summary>
        [HttpPost("Regenerate")]
        [Authorize(Policy = GXBlockPolicies.Regenerate)]
        public async Task<ActionResult<RegenerateBlockResponse>> Post(RegenerateBlock request, CancellationToken cancellationToken = default)
        {
            if (request.Ids?.Any() == true)
            {
                await _blockRepository.ResetToDefaultsAsync(request.Ids, cancellationToken);
            }
            else
            {
                await _blockRepository.EnsureDefaultsAsync(cancellationToken);
            }
            return new RegenerateBlockResponse();
        }
    }
}
