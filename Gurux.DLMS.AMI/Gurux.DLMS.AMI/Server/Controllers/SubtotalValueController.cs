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
    /// This controller is used to handle attribute subtotalValues.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class SubtotalValueController : ControllerBase
    {
        private readonly ISubtotalValueRepository _subtotalValueRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public SubtotalValueController(ISubtotalValueRepository subtotalValueRepository)
        {
            _subtotalValueRepository = subtotalValueRepository;
        }

        /// <summary>
        /// List subtotalValues.
        /// </summary>
        /// <param name="request">Request parameters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns></returns>
        [HttpPost("List")]
        [Authorize(Policy = GXSubtotalValuePolicies.View)]
        public async Task<ActionResult<ListSubtotalValuesResponse>> Post(
            ListSubtotalValues request,
            CancellationToken cancellationToken)
        {
            ListSubtotalValuesResponse ret = new ListSubtotalValuesResponse();
            await _subtotalValueRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }
    }
}
