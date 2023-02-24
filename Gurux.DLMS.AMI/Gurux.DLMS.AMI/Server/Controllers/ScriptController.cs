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
    /// This controller is used to handle the scripts.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ScriptController : ControllerBase
    {
        private readonly IScriptRepository _scriptrRepository;
        /// <summary>
        /// Constructor.
        /// </summary>
        public ScriptController(IScriptRepository scriptrRepository)
        {
            _scriptrRepository = scriptrRepository;
        }

        /// <summary>
        /// Get script information.
        /// </summary>
        /// <param name="id">Script id.</param>
        /// <returns>Script information.</returns>
        [HttpGet]
        [Authorize(Policy = GXScriptPolicies.View)]
        public async Task<ActionResult<GetScriptResponse>> Get(Guid id)
        {
            return new GetScriptResponse()
            {
                Item = await _scriptrRepository.ReadAsync(User, id)
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
            ListScriptsResponse ret = new ListScriptsResponse();
            await _scriptrRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update Script.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXScriptPolicies.Add)]
        public async Task<ActionResult<UpdateScriptResponse>> Post(UpdateScript request)
        {
            if (request.Scripts == null || request.Scripts.Count == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateScriptResponse ret = new UpdateScriptResponse();
            ret.ScriptIds = await _scriptrRepository.UpdateAsync(User, request.Scripts);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXScriptPolicies.Delete)]
        public async Task<ActionResult<RemoveScriptResponse>> Post(RemoveScript request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _scriptrRepository.DeleteAsync(User, request.Ids, request.Delete);
            return new RemoveScriptResponse();
        }

        /// <summary>
        /// Validate the script.
        /// </summary>
        [HttpPost("Validate")]
        public ActionResult<ValidateScriptResponse> Validate(ValidateScript request)
        {
            if (string.IsNullOrEmpty(request.Script))
            {
                return BadRequest("No script to validate.");
            }
            string? errorJson;
            int compileTime;
            _scriptrRepository.Compile(User, Guid.NewGuid().ToString(), request.Script, request.NameSpaces, null, out errorJson, out compileTime);
            ValidateScriptResponse ret = new ValidateScriptResponse();
            ret.Errors = errorJson;
            ret.CompileTime = compileTime;
            return ret;
        }

        /// <summary>
        /// Run the script.
        /// </summary>
        [HttpPost("Run")]
        public async Task<ActionResult<RunScriptResponse>> Run(RunScript request)
        {
            if (request.MethodId == Guid.Empty)
            {
                return BadRequest("No script to Run.");
            }
            RunScriptResponse ret = new RunScriptResponse();
            ret.Result = await _scriptrRepository.RunAsync(User, request.MethodId);
            return ret;
        }
    }
}
