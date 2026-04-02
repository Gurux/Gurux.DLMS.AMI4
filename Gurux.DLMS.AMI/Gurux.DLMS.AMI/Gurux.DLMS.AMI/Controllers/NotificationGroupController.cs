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
using Gurux.DLMS.AMI.Client.Shared;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the notification groups.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationGroupController : ControllerBase
    {
        private readonly INotificationGroupRepository _NotificationGroupRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public NotificationGroupController(
            INotificationGroupRepository NotificationGroupRepository)
        {
            _NotificationGroupRepository = NotificationGroupRepository;
        }

        /// <summary>
        /// Get notification group information.
        /// </summary>
        /// <param name="id">Notification group id.</param>
        /// <returns>Notification group.</returns>
        [HttpGet]
        [Authorize(Policy = GXNotificationGroupPolicies.View)]
        public async Task<ActionResult<GetNotificationGroupResponse>> Get(Guid id)
        {
            return new GetNotificationGroupResponse()
            {
                Item = await _NotificationGroupRepository.ReadAsync(id)
            };
        }

        /// <summary>
        /// Update notification group.
        /// </summary>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXNotificationGroupPolicies.Add)]
        public async Task<ActionResult<AddNotificationGroupResponse>> Post(AddNotificationGroup request)
        {
            await _NotificationGroupRepository.UpdateAsync(request.NotificationGroups);
            return new AddNotificationGroupResponse() { Ids = request.NotificationGroups.Select(s => s.Id).ToArray() };
        }

        /// <summary>
        /// List notification groups.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXNotificationGroupPolicies.View)]
        public async Task<ActionResult<ListNotificationGroupsResponse>> Post(
            ListNotificationGroups request, CancellationToken cancellationToken)
        {
            ListNotificationGroupsResponse ret = new ListNotificationGroupsResponse();
            await _NotificationGroupRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXNotificationGroupPolicies.Delete)]
        public async Task<ActionResult<RemoveNotificationGroupResponse>> Post(RemoveNotificationGroup request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _NotificationGroupRepository.DeleteAsync(request.Ids, request.Delete);
            return new RemoveNotificationGroupResponse();
        }
    }
}
