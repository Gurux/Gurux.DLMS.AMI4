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
using Gurux.DLMS.AMI.Client.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Gurux.DLMS.AMI.Shared.DIs;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the enum types.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class EnumTypeController : ControllerBase
    {
        private readonly IEnumTypeRepository _repository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public EnumTypeController(IEnumTypeRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Add enum type.
        /// </summary>
        /// <param name="request">Add enum type request parameters.</param>
        /// <returns>Add enum type response.</returns>
        [HttpPost("Add")]
        [Authorize(Policy = GXUserPolicies.Add)]
        public async Task<ActionResult<AddEnumTypeResponse>> Post(AddEnumType request)
        {
            if (string.IsNullOrEmpty(request?.Item?.Type) || string.IsNullOrEmpty(request.Item.Name))
            {
                throw new ArgumentNullException();
            }
            await _repository.GetLogTypeAsync(request.Item.Type, request.Item.Name);
            return new AddEnumTypeResponse();
        }

        /// <summary>
        /// Get enum types.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXUserPolicies.View)]
        public async Task<ActionResult<ListEnumTypesResponse>> Post(
            ListEnumTypes request,
            CancellationToken cancellationToken)
        {
            ListEnumTypesResponse ret = new ListEnumTypesResponse();
            await _repository.ListAsync(request, ret, cancellationToken);
            return ret;
        }
    }
}
