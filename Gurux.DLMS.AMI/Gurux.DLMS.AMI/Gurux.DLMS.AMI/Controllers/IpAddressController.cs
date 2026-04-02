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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.AMI.Client.Shared;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the IP addresses.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class IpAddressController : ControllerBase
    {
        private readonly IIpAddressRepository _ipAddressRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public IpAddressController(IIpAddressRepository ipAddressRepository)
        {
            _ipAddressRepository = ipAddressRepository;
        }

        /// <summary>
        /// Get IP address information.
        /// </summary>
        /// <param name="id">IpAddress id.</param>
        /// <returns>IpAddress information.</returns>
        [HttpGet]
        [Authorize(Policy = GXIpAddressPolicies.View)]
        public async Task<ActionResult<GetIpAddressResponse>> Get(Guid id)
        {
            return new GetIpAddressResponse()
            {
                Item = await _ipAddressRepository.ReadAsync(id)
            };
        }

        /// <summary>
        /// List IP address(es).
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXIpAddressPolicies.View)]
        public async Task<ActionResult<ListIpAddressResponse>> Post(
            ListIpAddress request,
            CancellationToken cancellationToken)
        {
            ListIpAddressResponse ret = new ListIpAddressResponse();
            await _ipAddressRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update IP address.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXIpAddressPolicies.Add)]
        public async Task<ActionResult<UpdateIpAddressResponse>> Post(UpdateIpAddress request)
        {
            if (request.IpAddress == null || request.IpAddress.Count == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateIpAddressResponse ret = new UpdateIpAddressResponse();
            ret.Ids = await _ipAddressRepository.UpdateAsync(request.IpAddress);
            return ret;
        }

        /// <summary>
        /// Delete the IP addresses.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Delete")]
        [Authorize(Policy = GXIpAddressPolicies.Delete)]
        public async Task<ActionResult<RemoveIpAddressResponse>> Post(RemoveIpAddress request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _ipAddressRepository.DeleteAsync(request.Ids);
            return new RemoveIpAddressResponse();
        }
    }
}
