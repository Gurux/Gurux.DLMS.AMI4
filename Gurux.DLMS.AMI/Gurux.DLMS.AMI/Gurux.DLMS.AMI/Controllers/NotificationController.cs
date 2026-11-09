// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

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
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController(INotificationRepository notificationRepository) : ControllerBase
    {
        private readonly INotificationRepository _notificationRepository = notificationRepository;

        /// <summary>
        /// Get notification information.
        /// </summary>
        /// <param name="id">Notification id.</param>
        /// <returns>Notification information.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXNotificationPolicies.View)]
        public async Task<ActionResult<GetNotificationResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetNotificationResponse()
            {
                Item = await _notificationRepository.ReadAsync(id, null, cancellationToken)
            };
        }
        /// <summary>
        /// List Notifications.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXNotificationPolicies.View)]
        public async Task<ActionResult<ListNotificationResponse>> Post(
            ListNotifications request,
            CancellationToken cancellationToken)
        {
            ListNotificationResponse ret = new();
            await _notificationRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update Notification.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXNotificationPolicies.Add)]
        public async Task<ActionResult<UpdateNotificationResponse>> Post(UpdateNotification request,
            CancellationToken cancellationToken = default)
        {
            if (request.Notifications == null || request.Notifications.Count == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateNotificationResponse ret = new()
            {
                Ids = await _notificationRepository.UpsertAsync(request.Notifications, null, cancellationToken)
            };
            return ret;
        }

        /// <summary>
        /// Delete notifications.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Delete")]
        [Authorize(Policy = GXNotificationPolicies.Delete)]
        public async Task<ActionResult<RemoveNotificationResponse>> Post(RemoveNotification request, CancellationToken cancellationToken = default)
        {
            await _notificationRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveNotificationResponse();
        }

        /// <summary>
        /// Adds missing defaults, or restores the selected automatic notification defaults.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Regenerate")]
        [Authorize(Policy = GXNotificationPolicies.Edit)]
        public async Task<ActionResult<RegenerateNotificationResponse>> Post(RegenerateNotification request, CancellationToken cancellationToken = default)
        {
            IEnumerable<Guid> ids;
            if (request.Ids?.Any() == true)
            {
                ids = await _notificationRepository.ResetToDefaultsAsync(request.Ids, cancellationToken);
            }
            else
            {
                ids = await _notificationRepository.EnsureDefaultsAsync(cancellationToken);
            }
            return new RegenerateNotificationResponse { Ids = ids };
        }
    }
}
