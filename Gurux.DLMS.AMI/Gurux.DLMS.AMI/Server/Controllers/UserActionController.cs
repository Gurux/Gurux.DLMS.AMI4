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
// This file is a part of Gurux User Framework.
//
// Gurux User Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux User Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Models;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle user actions.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UserActionController : ControllerBase
    {
        private readonly CancellationToken _cancellationToken;
        private readonly IUserActionRepository _userActionRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public UserActionController(
            IUserActionRepository userActionRepository, 
            IHostApplicationLifetime applicationLifetime)
        {
            _userActionRepository = userActionRepository;
            _cancellationToken = applicationLifetime.ApplicationStopping;
        }

        /// <summary>
        /// Get user action information.
        /// </summary>
        /// <param name="id">User action id.</param>
        /// <returns>User action.</returns>
        [HttpGet]
        [Authorize(Policy = GXUserActionPolicies.View)]
        public async Task<ActionResult<GXUserAction>> Get(Guid id)
        {
            return await _userActionRepository.ReadAsync(User, id);
        }

        /// <summary>
        /// List User actions.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXUserActionPolicies.View)]
        public async Task<ActionResult<ListUserActionResponse>> Post(ListUserAction request, CancellationToken cancellationToken)
        {
            ListUserActionResponse ret = new ListUserActionResponse();
            await _userActionRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// New User action is added..
        /// </summary>
        /// <param name="request">User action item.</param>
        /// <returns></returns>
        [HttpPost("Add")]
        [Authorize(Policy = GXUserActionPolicies.Add)]
        public async Task<ActionResult<AddUserActionResponse>> Post(AddUserAction request)
        {
            AddUserActionResponse ret = new AddUserActionResponse();
            await _userActionRepository.AddAsync(User, request.Actions);
            return ret;
        }

        /// <summary>
        /// New User action is added..
        /// </summary>
        /// <param name="request">User action item.</param>
        /// <returns></returns>
        [HttpPost("Clear")]
        [Authorize(Policy = GXUserActionPolicies.Clear)]

        public async Task<ActionResult<ClearUserActionResponse>> Post(ClearUserAction request)
        {
            ClearUserActionResponse ret = new ClearUserActionResponse();
            await _userActionRepository.ClearAsync(User, request.Ids);
            return ret;
        }        
    }
}
