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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Mvc;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Models;
using Microsoft.AspNetCore.Authorization;

namespace Gurux.DLMS.AMI.Server
{
    /// <summary>
    /// This controller is used to handle the object settings.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ObjectController : ControllerBase
    {
        private readonly IObjectRepository _objectRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ObjectController(IObjectRepository objectRepository)
        {
            _objectRepository = objectRepository;
        }

        /// <summary>
        /// Add COSEM objects
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXObjectPolicies.Add)]

        public async Task<ActionResult<UpdateObjectResponse>> Post(UpdateObject request)
        {
            if (request.Objects == null || !request.Objects.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateObjectResponse ret = new UpdateObjectResponse();
            ret.Ids = (await _objectRepository.UpdateAsync(User, request.Objects));
            return ret;
        }

        /// <summary>
        /// Get object information.
        /// </summary>
        /// <param name="id">Object id.</param>
        /// <returns>Object information.</returns>
        [HttpGet]
        [Authorize(Policy = GXObjectPolicies.View)]
        public async Task<ActionResult<GetObjectResponse>> Get(Guid id)
        {
            return new GetObjectResponse()
            {
                Item = await _objectRepository.ReadAsync(User, id)
            };
        }

        /// <summary>
        /// List COSEM objects
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXObjectPolicies.View)]
        public async Task<ActionResult<ListObjectsResponse>> Post(
            ListObjects request,
            CancellationToken cancellationToken)
        {
            ListObjectsResponse ret = new ListObjectsResponse();
            await _objectRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Remove selected objects
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXObjectPolicies.Delete)]
        public async Task<ActionResult<RemoveObjectResponse>> Post(RemoveObject request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _objectRepository.DeleteAsync(User, request.Ids, request.Delete);
            return new RemoveObjectResponse();
        }
    }
}
