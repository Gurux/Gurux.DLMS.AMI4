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
    /// This controller is used to handle the key management logs.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class KeyManagementLogController : ControllerBase
    {
        private readonly IKeyManagementLogRepository _deviceKeyLogRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public KeyManagementLogController(IKeyManagementLogRepository deviceKeyLogRepository)
        {
            _deviceKeyLogRepository = deviceKeyLogRepository;
        }

        /// <summary>
        /// Get key management log information.
        /// </summary>
        /// <param name="id">KeyManagement log id.</param>
        /// <returns>KeyManagement log.</returns>
        [HttpGet]
        [Authorize(Policy = GXKeyManagementLogPolicies.View)]
        public async Task<ActionResult<GetKeyManagementLog>> Get(Guid id)
        {
            return new GetKeyManagementLog()
            {
                Item = await _deviceKeyLogRepository.ReadAsync(User, id)
            };
        }

        /// <summary>
        /// Add key management logs.
        /// </summary>
        [HttpPost("Add")]
        [Authorize(Policy = GXKeyManagementLogPolicies.Add)]
        public async Task<ActionResult<AddKeyManagementLogResponse>> Post(AddKeyManagementLog request)
        {
            await _deviceKeyLogRepository.AddAsync(User, request.Logs);
            AddKeyManagementLogResponse response = new AddKeyManagementLogResponse();
            return response;
        }

        /// <summary>
        /// List key management logs
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXKeyManagementLogPolicies.View)]
        public async Task<ActionResult<ListKeyManagementLogsResponse>> Post(
            ListKeyManagementLogs request,
            CancellationToken cancellationToken)
        {
            ListKeyManagementLogsResponse response = new ListKeyManagementLogsResponse();
            await _deviceKeyLogRepository.ListAsync(User, request, response, cancellationToken);
            return response;
        }

        /// <summary>
        /// Clear system error(s).
        /// </summary>
        [HttpPost("Clear")]
        [Authorize(Policy = GXKeyManagementLogPolicies.Clear)]
        public async Task<ActionResult<ClearKeyManagementLogsResponse>> Post(ClearKeyManagementLogs request)
        {
            await _deviceKeyLogRepository.ClearAsync(User, request.KeyManagements);
            return new ClearKeyManagementLogsResponse();
        }

        /// <summary>
        /// Close system error(s).
        /// </summary>
        [HttpPost("Close")]
        [Authorize(Policy = GXKeyManagementLogPolicies.Close)]
        public async Task<ActionResult<CloseKeyManagementLogResponse>> Post(CloseKeyManagementLog request)
        {
            await _deviceKeyLogRepository.CloseAsync(User, request.Logs);
            return new CloseKeyManagementLogResponse();
        }
    }
}
