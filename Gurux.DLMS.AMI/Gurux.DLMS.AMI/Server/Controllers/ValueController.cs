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
using Gurux.DLMS.AMI.Server.Models;
using Microsoft.AspNetCore.Authorization;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle attribute values.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ValueController : ControllerBase
    {
        private readonly IValueRepository _valueRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ValueController(IValueRepository valueRepository)
        {
            _valueRepository = valueRepository;
        }

        /// <summary>
        /// List values.
        /// </summary>
        /// <param name="request">Request parameters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns></returns>
        [HttpPost("List")]
        [Authorize(Policy = GXValuePolicies.View)]
        public async Task<ActionResult<ListValuesResponse>> Post(
            ListValues request,
            CancellationToken cancellationToken)
        {
            ListValuesResponse ret = new ListValuesResponse();
            await _valueRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update value.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Add")]
        [Authorize(Policy = GXValuePolicies.Add)]
        public async Task<ActionResult<AddValueResponse>> Post(AddValue request)
        {
            if (request.Values == null || request.Values.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            AddValueResponse ret = new AddValueResponse();
            await _valueRepository.AddAsync(User, request.Values);
            return ret;
        }

        /// <summary>
        /// Clear device, object or attribute values.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Clear")]
        [Authorize(Policy = GXValuePolicies.Clear)]
        public async Task<ActionResult<ClearValueResponse>> Post(ClearValue request)
        {
            if (request.Devices != null && request.Devices.Any())
            {
                await _valueRepository.ClearDeviceAsync(User, request.Devices);
            }
            if (request.Objects != null && request.Objects.Any())
            {
                await _valueRepository.ClearObjectAsync(User, request.Objects);
            }
            if (request.Attributes != null && request.Attributes.Any())
            {
                await _valueRepository.ClearAttributeAsync(User, request.Attributes);
            }
            ClearValueResponse ret = new ClearValueResponse();
            return ret;
        }

    }
}
