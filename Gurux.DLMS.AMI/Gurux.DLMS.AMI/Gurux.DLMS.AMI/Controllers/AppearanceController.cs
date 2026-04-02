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
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the appearances.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AppearanceController : ControllerBase
    {
        private readonly IAppearanceRepository _appearanceRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public AppearanceController(IAppearanceRepository appearanceRepository)
        {
            _appearanceRepository = appearanceRepository;
        }

        /// <summary>
        /// Get appearance information.
        /// </summary>
        /// <param name="type">Appearance type.</param>
        /// <param name="id">Appearance id.</param>
        /// <returns>Appearance information.</returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<GetAppearanceResponse>> Get(int type, string id)
        {
            return new GetAppearanceResponse()
            {
                Item = await _appearanceRepository.ReadAsync(type, id)
            };
        }

        /// <summary>
        /// List Appearances.
        /// </summary>
        [HttpPost("List")]
        [AllowAnonymous]
        public async Task<ActionResult<ListAppearancesResponse>> Post(
            ListAppearances request,
            CancellationToken cancellationToken)
        {
            ListAppearancesResponse ret = new ListAppearancesResponse();
            await _appearanceRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update appearance.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXAppearancePolicies.Add)]
        public async Task<ActionResult<UpdateAppearanceResponse>> Post(UpdateAppearance request)
        {
            if (request.Appearances == null || request.Appearances.Count == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateAppearanceResponse ret = new UpdateAppearanceResponse();
            ret.Ids = await _appearanceRepository.UpdateAsync(request.Appearances);
            return ret;
        }

        /// <summary>
        /// Delete appearance.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Delete")]
        [Authorize(Policy = GXAppearancePolicies.Delete)]
        public async Task<ActionResult<RemoveAppearanceResponse>> Post(RemoveAppearance request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _appearanceRepository.DeleteAsync(request.Ids, request.Delete);
            return new RemoveAppearanceResponse();
        }


        /// <summary>
        /// Refresh appearances.
        /// </summary>
        [HttpPost("Refresh")]
        [Authorize(Policy = GXAppearancePolicies.Edit)]
        public async Task<ActionResult<RefreshAppearanceResponse>> Post(RefreshAppearance request)
        {
            return new RefreshAppearanceResponse()
            {
                NewItems = await _appearanceRepository.RefreshAsync(request.Force, request.Filter)
            };
        }

        /// <summary>
        /// When the appearance was last changed.
        /// </summary>
        /// <returns></returns>
        [HttpPost("LastChanged")]
        [AllowAnonymous]
        public async Task<ActionResult<DateTimeOffset?>> Post(byte type)
        {
            var ret = await _appearanceRepository.LastChanged(type);
            if (ret == null)
            {
                ret = DateTimeOffset.MinValue;
            }
            return Ok(ret);
        }
    }
}
