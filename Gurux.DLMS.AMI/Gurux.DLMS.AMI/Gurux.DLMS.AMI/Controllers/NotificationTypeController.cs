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
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the notification types.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationTypeController(INotificationTypeRepository NotificationTypeRepository) : ControllerBase
    {
        private readonly INotificationTypeRepository _notificationTypeRepository = NotificationTypeRepository;

        /// <summary>
        /// Get notification type information.
        /// </summary>
        /// <param name="id">Notification type id.</param>
        /// <returns>Notification type information.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXNotificationTypePolicies.View)]
        public async Task<ActionResult<GetNotificationTypeResponse>> Get(Guid id = default,
            CancellationToken cancellationToken = default)
        {
            return new GetNotificationTypeResponse()
            {
                Item = await _notificationTypeRepository.ReadAsync(id, null, cancellationToken)
            };
        }
        /// <summary>
        /// List notification types.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXNotificationTypePolicies.View)]
        public async Task<ActionResult<ListNotificationTypeResponse>> Post(
            ListNotificationType request,
            CancellationToken cancellationToken)
        {
            ListNotificationTypeResponse ret = new();
            await _notificationTypeRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }
        /// <summary>
        /// Adds notification types through the shared persistence rules.
        /// </summary>
        /// <param name="request">Notification types to add.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Add")]
        [Authorize(Policy = GXNotificationTypePolicies.Add)]
        public Task<ActionResult<UpdateNotificationTypeResponse>> Add(UpdateNotificationType request, CancellationToken cancellationToken = default)
        {
            return Post(request, cancellationToken);
        }

        /// <summary>Updates notification type metadata and policy assignments.</summary>
        [HttpPost("Update")]
        [Authorize(Policy = GXNotificationTypePolicies.Edit)]
        public async Task<ActionResult<UpdateNotificationTypeResponse>> Post(UpdateNotificationType request, CancellationToken cancellationToken = default)
        {
            if (request.NotificationTypes?.Any() != true)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            UpdateNotificationTypeResponse ret = new()
            {
                Ids = await _notificationTypeRepository.UpsertAsync(request.NotificationTypes, null, cancellationToken)
            };
            return ret;
        }

        /// <summary>
        /// Delete notification types
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Delete")]
        [Authorize(Policy = GXNotificationTypePolicies.Delete)]
        public async Task<ActionResult<RemoveNotificationTypeResponse>> Post(RemoveNotificationType request, CancellationToken cancellationToken = default)
        {
            await _notificationTypeRepository.DeleteAsync(request.Ids, request.Delete, true, cancellationToken);
            return new RemoveNotificationTypeResponse();
        }
    }
}
