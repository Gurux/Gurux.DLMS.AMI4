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
    /// This controller is used to handle the notifications.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationRepository _notificationRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public NotificationController(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }
        /// <summary>
        /// Get notification information.
        /// </summary>
        /// <param name="id">Notification id.</param>
        /// <returns>Notification information.</returns>
        [HttpGet]
        [Authorize(Policy = GXNotificationPolicies.View)]
        public async Task<ActionResult<GetNotificationResponse>> Get(Guid id)
        {
            return new GetNotificationResponse()
            {
                Item = await _notificationRepository.ReadAsync(id)
            };
        }
        /// <summary>
        /// List Notifications.
        /// </summary>
        [HttpPost("List")]
        [AllowAnonymous]
        [Authorize(Policy = GXNotificationPolicies.View)]
        public async Task<ActionResult<ListNotificationResponse>> Post(
            ListNotifications request,
            CancellationToken cancellationToken)
        {
            ListNotificationResponse ret = new ListNotificationResponse();
            await _notificationRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }
        /// <summary>
        /// Update Notification.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXNotificationPolicies.Add)]
        public async Task<ActionResult<UpdateNotificationResponse>> Post(UpdateNotification request)
        {
            if (request.Notifications == null || request.Notifications.Count == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateNotificationResponse ret = new UpdateNotificationResponse();
            ret.Ids = await _notificationRepository.UpdateAsync(request.Notifications);
            return ret;
        }

        [HttpPost("Delete")]
        [Authorize(Policy = GXNotificationPolicies.Delete)]
        public async Task<ActionResult<RemoveNotificationResponse>> Post(RemoveNotification request)
        {
            if (request.Ids == null || request.Ids.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _notificationRepository.DeleteAsync(request.Ids, request.Delete);
            return new RemoveNotificationResponse();
        }
    }
}