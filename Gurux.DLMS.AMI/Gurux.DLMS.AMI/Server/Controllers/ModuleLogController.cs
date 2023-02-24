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
    /// This controller is used to handle the module log.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ModuleLogController : ControllerBase
    {
        private readonly IModuleLogRepository _moduleLogRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ModuleLogController(IModuleLogRepository moduleLogRepository)
        {
            _moduleLogRepository = moduleLogRepository;
        }

        /// <summary>
        /// Get module log information.
        /// </summary>
        /// <param name="id">Module log id.</param>
        /// <returns>Module log.</returns>
        [HttpGet]
        [Authorize(Policy = GXModuleLogPolicies.View)]
        public async Task<ActionResult<GetModuleLog>> Get(Guid id)
        {
            return new GetModuleLog()
            {
                Item = await _moduleLogRepository.ReadAsync(User, id)
            };
        }

        /// <summary>
        /// Add module log.
        /// </summary>
        [HttpPost("Add")]
        [Authorize(Policy = GXModuleLogPolicies.Add)]
        public async Task<ActionResult<AddModuleLogResponse>> Post(AddModuleLog request)
        {
            await _moduleLogRepository.AddAsync(User, request.Logs);
            AddModuleLogResponse response = new AddModuleLogResponse();
            return response;
        }

        /// <summary>
        /// List module logs
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXModuleLogPolicies.View)]
        public async Task<ActionResult<ListModuleLogsResponse>> Post(
            ListModuleLogs request,
            CancellationToken cancellationToken)
        {
            ListModuleLogsResponse response = new ListModuleLogsResponse();
            await _moduleLogRepository.ListAsync(User, request, response, cancellationToken);
            return response;
        }

        /// <summary>
        /// Clear module log(s).
        /// </summary>
        [HttpPost("Clear")]
        [Authorize(Policy = GXModuleLogPolicies.Clear)]
        public async Task<ActionResult<ClearModuleLogsResponse>> Post(ClearModuleLogs request)
        {
            await _moduleLogRepository.ClearAsync(User, request.Modules);
            return new ClearModuleLogsResponse();
        }

        /// <summary>
        /// Close module log(s).
        /// </summary>
        [HttpPost("Close")]
        [Authorize(Policy = GXModuleLogPolicies.Close)]
        public async Task<ActionResult<CloseModuleLogResponse>> Post(CloseModuleLog request)
        {
            await _moduleLogRepository.CloseAsync(User, request.Logs);
            return new CloseModuleLogResponse();
        }
    }
}
