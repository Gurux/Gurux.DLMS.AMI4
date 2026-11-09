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
    /// This controller is used to handle the scripts.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class ScriptController(IScriptRepository scriptrRepository) : ControllerBase
    {
        private readonly IScriptRepository _scriptrRepository = scriptrRepository;

        /// <summary>
        /// Get script information.
        /// </summary>
        /// <param name="id">Script id.</param>
        /// <returns>Script information.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXScriptPolicies.View)]
        public async Task<ActionResult<GetScriptResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetScriptResponse()
            {
                Item = await _scriptrRepository.ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>
        /// List Scripts.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXScriptPolicies.View)]
        public async Task<ActionResult<ListScriptsResponse>> Post(
            ListScripts request,
            CancellationToken cancellationToken)
        {
            ListScriptsResponse ret = new();
            await _scriptrRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update Script.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXScriptPolicies.Add)]
        public async Task<ActionResult<UpdateScriptResponse>> Post(UpdateScript request, CancellationToken cancellationToken = default)
        {
            UpdateScriptResponse ret = new()
            {
                Ids = await _scriptrRepository.UpsertAsync(request.Scripts, null, cancellationToken)
            };
            return ret;
        }

        /// <summary>
        /// Deletes scripts based on the provided IDs and delete option.
        /// </summary>
        /// <param name="request">The request containing script IDs and a flag indicating whether to perform a soft delete.</param>
        /// <returns>An action result containing the outcome of the delete operation.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Delete")]
        [Authorize(Policy = GXScriptPolicies.Delete)]
        public async Task<ActionResult<RemoveScriptResponse>> Post(RemoveScript request, CancellationToken cancellationToken = default)
        {
            await _scriptrRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveScriptResponse();
        }

        /// <summary>
        /// Validate the script.
        /// </summary>
        [HttpPost("Validate")]
        public ActionResult<ValidateScriptResponse> Validate(ValidateScript request, CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            if (string.IsNullOrEmpty(request.Script))
            {
                return BadRequest("No script to validate.");
            }
            _scriptrRepository.Compile(Guid.NewGuid().ToString(), request.Script, request.NameSpaces, [], out string? errorJson, out int compileTime, cancellationToken);
            ValidateScriptResponse ret = new()
            {
                Errors = errorJson,
                CompileTime = compileTime
            };
            return ret;
        }

        /// <summary>
        /// Run the script.
        /// </summary>
        [HttpPost("Run")]
        public async Task<ActionResult<RunScriptResponse>> Run(RunScript request, CancellationToken cancellationToken = default)
        {
            if (request.MethodId == Guid.Empty)
            {
                return BadRequest("No script to Run.");
            }
            RunScriptResponse ret = new()
            {
                Result = await _scriptrRepository.RunAsync(request.MethodId, cancellationToken)
            };
            return ret;
        }

        /// <summary>
        /// Rebuild the scripts.
        /// </summary>
        [HttpPost("Rebuild")]
        public async Task<ActionResult<RebuildScriptResponse>> Rebuild(RebuildScript request, CancellationToken cancellationToken = default)
        {
            await _scriptrRepository.RebuildAsync(request.Scripts, cancellationToken);
            return new RebuildScriptResponse();
        }
    }
}
