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

using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Server.Repository;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gurux.DLMS.AMI.Server.Controllers
{
    /// <summary>
    /// This controller is used to handle the localized resources.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class LocalizedResourceController : ControllerBase
    {
        private readonly ILocalizedResourceRepository _localizedResourceRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public LocalizedResourceController(ILocalizedResourceRepository localizedResourceRepository)
        {
            _localizedResourceRepository = localizedResourceRepository;
        }

        /// <summary>
        /// Update localized resource.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXLocalizedResourcePolicies.Add)]
        public async Task<ActionResult<UpdateLocalizedResourceResponse>> Post(UpdateLocalizedResource request)
        {
            if (request.LocalizedResources == null || request.LocalizedResources.Length == 0)
            {
                return BadRequest("Invalid localized resources");
            }
            UpdateLocalizedResourceResponse ret = new UpdateLocalizedResourceResponse();
            ret.Ids = await _localizedResourceRepository.UpdateAsync(request.LocalizedResources);
            return ret;
        }

        /// <summary>
        /// Get localized resources.
        /// </summary>
        /// <returns>Available localized resources.</returns>
        [HttpPost("List")]
        [Authorize(Policy = GXLocalizedResourcePolicies.View)]
        [Authorize(Policy = GXUserPolicies.View)]
        public async Task<ActionResult<ListLocalizedResourcesResponse>> Post(
            ListLocalizedResources request,
            CancellationToken cancellationToken)
        {
            ListLocalizedResourcesResponse ret = new ListLocalizedResourcesResponse();
            await _localizedResourceRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Get localized resource information.
        /// </summary>
        /// <param name="hash">Hash of localized resource.</param>
        /// <param name="lang">Used language identifier.</param>
        /// <param name="text">Resource text.</param>
        /// <returns>LocalizedResource information.</returns>
        [HttpGet]
        [Authorize(Policy = GXLocalizedResourcePolicies.View)]
        public async Task<ActionResult<GetLocalizedResourceResponse>> Get(string lang, string hash, string? text)
        {
            var ret = new GetLocalizedResourceResponse();
            ret.Item = await _localizedResourceRepository.ReadAsync(lang, hash, text);
            return ret;
        }

        /// <summary>
        /// Remove selected localized resources.
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXLocalizedResourcePolicies.Delete)]
        public async Task<ActionResult<LocalizedResourceDeleteResponse>> Post(LocalizedResourceDelete request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _localizedResourceRepository.DeleteAsync(request.Ids);
            return new LocalizedResourceDeleteResponse();
        }

        /// <summary>
        /// When the localized resources was last changed.
        /// </summary>
        /// <returns></returns>
        [HttpPost("LastChanged")]
        [AllowAnonymous]
        public async Task<ActionResult<DateTimeOffset?>> Post()
        {
            var ret = await _localizedResourceRepository.LastChanged();
            if (ret == null)
            {
                ret = DateTimeOffset.MinValue;
            }
            return Ok(ret);
        }
    }
}
