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
    /// This controller is used to handle user errors.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UserErrorController : ControllerBase
    {
        private readonly IUserErrorRepository _userErrorRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public UserErrorController(IUserErrorRepository userErrorRepository)
        {
            _userErrorRepository = userErrorRepository;
        }

        /// <summary>
        /// Get user error information.
        /// </summary>
        /// <param name="id">User error id.</param>
        /// <returns>User error.</returns>
        [HttpGet]
        [Authorize(Policy = GXUserErrorPolicies.View)]
        public async Task<ActionResult<GXUserError>> Get(Guid id)
        {
            return await _userErrorRepository.ReadAsync(User, id);
        }

        /// <summary>
        /// Add user Error.
        /// </summary>
        [HttpPost("Add")]
        [Authorize(Policy = GXUserErrorPolicies.Add)]
        public async Task<ActionResult<AddUserErrorResponse>> Post(AddUserError request)
        {
            await _userErrorRepository.AddAsync(User, request.Errors);
            AddUserErrorResponse response = new AddUserErrorResponse();
            return response;
        }

        /// <summary>
        /// List user Errors
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXUserErrorPolicies.View)]
        public async Task<ActionResult<ListUserErrorsResponse>> Post(
            ListUserErrors request, 
            CancellationToken cancellationToken)
        {
            ListUserErrorsResponse response = new ListUserErrorsResponse();
            await _userErrorRepository.ListAsync(User, request, response, cancellationToken);
            return response;
        }

        /// <summary>
        /// Clear user errors.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Clear")]
        [Authorize(Policy = GXUserErrorPolicies.Clear)]
        public async Task<ActionResult<ClearUserErrorsResponse>> Post(ClearUserErrors request)
        {
            await _userErrorRepository.ClearAsync(User, request.Users);
            return new ClearUserErrorsResponse();
        }

        /// <summary>
        /// Close user error(s).
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Close")]
        [Authorize(Policy = GXUserErrorPolicies.Close)]
        public async Task<ActionResult<CloseUserErrorResponse>> Post(CloseUserError request)
        {
            await _userErrorRepository.CloseAsync(User, request.Errors);
            return new CloseUserErrorResponse();
        }
    }
}
