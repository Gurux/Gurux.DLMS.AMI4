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
    /// This controller is used to handle the notification logs.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationLogController : ControllerBase
    {
        private readonly INotificationLogRepository _notificationLogRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public NotificationLogController(INotificationLogRepository notificationErrorRepository)
        {
            _notificationLogRepository = notificationErrorRepository;
        }

        /// <summary>
        /// Get notification error information.
        /// </summary>
        /// <param name="id">Notification error id.</param>
        /// <returns>Notification error.</returns>
        [HttpGet]
        [Authorize(Policy = GXNotificationLogPolicies.View)]
        public async Task<ActionResult<GetNotificationLogResponse>> Get(Guid id)
        {
            return new GetNotificationLogResponse()
            {
                Item = await _notificationLogRepository.ReadAsync(id)
            };
        }

        /// <summary>
        /// Add notification Error(s).
        /// </summary>
        [HttpPost("Add")]
        [Authorize(Policy = GXNotificationLogPolicies.Add)]
        public async Task<ActionResult<AddNotificationLogResponse>> Post(AddNotificationLog request)
        {
            await _notificationLogRepository.AddAsync(request.Type, request.Logs);
            AddNotificationLogResponse response = new AddNotificationLogResponse();
            return response;
        }

        /// <summary>
        /// List notification Errors
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXNotificationLogPolicies.View)]
        public async Task<ActionResult<ListNotificationLogsResponse>> Post(
            ListNotificationLogs request,
            CancellationToken cancellationToken)
        {
            ListNotificationLogsResponse response = new ListNotificationLogsResponse();
            await _notificationLogRepository.ListAsync(request, response, cancellationToken);
            return response;
        }

        /// <summary>
        /// Clear notification error(s).
        /// </summary>
        [HttpPost("Clear")]
        [Authorize(Policy = GXNotificationLogPolicies.Clear)]
        public async Task<ActionResult<ClearNotificationLogsResponse>> Post(ClearNotificationLogs request)
        {
            await _notificationLogRepository.ClearAsync(request.Notifications);
            return new ClearNotificationLogsResponse();
        }
    }
}
