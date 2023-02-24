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
using Gurux.DLMS.AMI.Shared.DIs;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Server.Models;
using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the script logs.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ScriptLogController : ControllerBase
    {
        private readonly IScriptLogRepository _scriptLogRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ScriptLogController(IScriptLogRepository scriptLogRepository)
        {
            _scriptLogRepository = scriptLogRepository;
        }

        /// <summary>
        /// Get script log information.
        /// </summary>
        /// <param name="id">Script log id.</param>
        /// <returns>Script log.</returns>
        [HttpGet]
        [Authorize(Policy = GXScriptLogPolicies.View)]
        public async Task<ActionResult<GetScriptLog>> Get(Guid id)
        {
            return new GetScriptLog()
            {
                Item = await _scriptLogRepository.ReadAsync(User, id)
            };
        }

        /// <summary>
        /// Add script logs.
        /// </summary>
        [HttpPost("Add")]
        [Authorize(Policy = GXScriptLogPolicies.Add)]
        public async Task<ActionResult<AddScriptLogResponse>> Post(AddScriptLog request)
        {
            await _scriptLogRepository.AddAsync(User, request.Logs);
            AddScriptLogResponse response = new AddScriptLogResponse();
            return response;
        }

        /// <summary>
        /// List script logs
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXScriptLogPolicies.View)]
        public async Task<ActionResult<ListScriptLogsResponse>> Post(
            ListScriptLogs request, 
            CancellationToken cancellationToken)
        {
            ListScriptLogsResponse response = new ListScriptLogsResponse();
            await _scriptLogRepository.ListAsync(User, request, response, cancellationToken);
            return response;
        }

        /// <summary>
        /// Clear system error(s).
        /// </summary>
        [HttpPost("Clear")]
        [Authorize(Policy = GXScriptLogPolicies.Clear)]
        public async Task<ActionResult<ClearScriptLogsResponse>> Post(ClearScriptLogs request)
        {
            await _scriptLogRepository.ClearAsync(User, request.Scripts);
            return new ClearScriptLogsResponse();
        }

        /// <summary>
        /// Close system error(s).
        /// </summary>
        [HttpPost("Close")]
        [Authorize(Policy = GXScriptLogPolicies.Close)]
        public async Task<ActionResult<CloseScriptLogResponse>> Post(CloseScriptLog request)
        {
            await _scriptLogRepository.CloseAsync(User, request.Logs);
            return new CloseScriptLogResponse();
        }
    }
}
