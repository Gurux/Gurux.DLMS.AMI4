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
    /// This controller is used to handle the script groups.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ScriptGroupController : ControllerBase
    {
        private readonly IScriptGroupRepository _ScriptGroupRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ScriptGroupController(
            IScriptGroupRepository ScriptGroupRepository)
        {
            _ScriptGroupRepository = ScriptGroupRepository;
        }

        /// <summary>
        /// Get script group information.
        /// </summary>
        /// <param name="id">Script group id.</param>
        /// <returns>Script group.</returns>
        [HttpGet]
        [Authorize(Policy = GXScriptGroupPolicies.View)]
        public async Task<ActionResult<GetScriptGroupResponse>> Get(Guid id)
        {
            return new GetScriptGroupResponse()
            {
                Item = await _ScriptGroupRepository.ReadAsync(User, id)
            };
        }

        /// <summary>
        /// Update script group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXScriptGroupPolicies.Add)]
        public async Task<ActionResult<AddScriptGroupResponse>> Post(AddScriptGroup request)
        {
            await _ScriptGroupRepository.UpdateAsync(User, request.ScriptGroups);
            return new AddScriptGroupResponse()
            {
                Ids = request.ScriptGroups.Select(s => s.Id).ToArray()
            };
        }

        /// <summary>
        /// List script groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXScriptGroupPolicies.View)]
        public async Task<ActionResult<ListScriptGroupsResponse>> Post(
            ListScriptGroups request,
            CancellationToken cancellationToken)
        {
            ListScriptGroupsResponse ret = new ListScriptGroupsResponse();
            await _ScriptGroupRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXScriptGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveScriptGroupResponse>> Post(RemoveScriptGroup request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _ScriptGroupRepository.DeleteAsync(User, request.Ids, request.Delete);
            return new RemoveScriptGroupResponse();
        }
    }
}
