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
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Models;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the component views.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ComponentViewController : ControllerBase
    {
        private readonly IComponentViewRepository _componentviewRepository;
        public ComponentViewController(IComponentViewRepository componentViewrRepository)
        {
            _componentviewRepository = componentViewrRepository;
        }

        /// <summary>
        /// List component views.
        /// </summary>
        [HttpPost("List")]
        [AllowAnonymous]
        [Authorize(Policy = GXComponentViewPolicies.View)]
        public async Task<ActionResult<ListComponentViewsResponse>> Post(
            ListComponentViews request, 
            CancellationToken cancellationToken)
        {
            ListComponentViewsResponse ret = new ListComponentViewsResponse();
            await _componentviewRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update component view.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXComponentViewPolicies.Add)]
        public async Task<ActionResult<UpdateComponentViewResponse>> Post(UpdateComponentView request)
        {
            if (request.ComponentViews == null || request.ComponentViews.Count == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateComponentViewResponse ret = new UpdateComponentViewResponse();
            ret.ComponentViewIds = await _componentviewRepository.UpdateAsync(User, request.ComponentViews);
            return ret;
        }

        /// <summary>
        /// Delete the component view.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Delete")]
        [Authorize(Policy = GXComponentViewPolicies.Delete)]
        public async Task<ActionResult<DeleteComponentViewResponse>> Post(DeleteComponentView request)
        {
            if (request.ComponentViewIds == null || request.ComponentViewIds.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _componentviewRepository.DeleteAsync(User, request.ComponentViewIds);
            return new DeleteComponentViewResponse();
        }

        /// <summary>
        /// Refresh component views.
        /// </summary>
        [HttpPost("Refresh")]
        [Authorize(Policy = GXComponentViewPolicies.Refresh)]
        public async Task<ActionResult<RefreshComponentViewResponse>> Post(RefreshComponentView request)
        {
            return new RefreshComponentViewResponse() { NewItems = await _componentviewRepository.RefrestAsync(User) };
        }
    }
}
