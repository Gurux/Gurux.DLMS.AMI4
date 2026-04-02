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
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using System.Diagnostics;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class DeviceActionRepository : IDeviceActionRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IDeviceRepository _deviceRepository;
        private readonly GXPerformanceSettings _performanceSettings;
        private readonly IEnumTypeRepository _enumTypeRepository;
        private readonly ISystemLogRepository _systemLogRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DeviceActionRepository(
            IGXHost host,
            IGXAmiContextAccessor contextAccessor,
            IUserRepository userRepository,
            IGXEventsNotifier eventsNotifier,
            IDeviceRepository deviceRepository,
            IEnumTypeRepository enumTypeRepository,
            ISystemLogRepository systemLogRepository,
            GXPerformanceSettings performanceSettings)
        {
            var user = contextAccessor?.User;
            if (user == null ||
                (!user.HasScope(GXDeviceActionPolicies.Add) &&
                !user.IsInRole(GXRoles.Admin) &&
                !user.IsInRole(GXRoles.DeviceManager)))
            {
                throw new UnauthorizedAccessException();
            }
            User = user;
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
            _deviceRepository = deviceRepository;
            _performanceSettings = performanceSettings;
            _enumTypeRepository = enumTypeRepository;
            _systemLogRepository = systemLogRepository;
        }

        /// <inheritdoc />
        public async Task<GXDeviceAction[]> ListAsync(
            ListDeviceAction? request,
            ListDeviceActionResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the actions.
                arg = GXSelectArgs.SelectAll<GXDeviceAction>();
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXDeviceAction, GXDevice>(j => j.Device, j => j.Id);
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetDeviceActionsByUser(s => "*", userId, null);
            }
            arg.Columns.Add<GXDevice>(c => new { c.Id, c.Name });
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXDeviceAction>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXDeviceAction>(w => request.Included.Contains(w.Id));
                }
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXDeviceAction>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = await _host.Connection.SingleOrDefaultAsync<int>(total, cancellationToken);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXDeviceAction>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXDeviceAction>(q => q.CreationTime);
            }
            //Actions are ignored from the device 
            //so there is no reference relation that is causing problems with JSON parser.
            arg.Columns.Exclude<GXDevice>(e => e.Actions);
            GXDeviceAction[] actions = (await _host.Connection.SelectAsync<GXDeviceAction>(arg, cancellationToken)).ToArray();
            if (response != null)
            {
                response.Actions = actions;
                if (response.Count == 0)
                {
                    response.Count = actions.Length;
                }
            }
            return actions;
        }

        /// <inheritdoc />
        public async Task<GXDeviceAction> ReadAsync(Guid id)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXDeviceAction>(where => where.Id == id);
            //Get device.
            arg.Columns.Add<GXDevice>();
            arg.Joins.AddInnerJoin<GXDeviceAction, GXDevice>(x => x.Device, y => y.Id);
            //Actions are ignored from the device 
            //so there is no reference relation that is causing problems with JSON parser.
            arg.Columns.Exclude<GXDevice>(e => e.Actions);
            GXDeviceAction deviceAction = (await _host.Connection.SingleOrDefaultAsync<GXDeviceAction>(arg));
            if (deviceAction == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            return deviceAction;
        }

        /// <inheritdoc />
        public async Task AddAsync(string type, IEnumerable<GXDeviceAction> deviceActions)
        {
            DateTime now = DateTime.Now;
            foreach (var it in deviceActions)
            {
                if (it.Device == null || it.Device.Id == Guid.Empty)
                {
                    throw new ArgumentException("Invalid device.");
                }
                it.CreationTime = now;
                it.Type = await _enumTypeRepository.GetLogTypeAsync(TargetType.DeviceAction, type);
            }
            GXInsertArgs args = GXInsertArgs.InsertRange(deviceActions);
            _host.Connection.Insert(args);
            List<string>? users = await _deviceRepository.GetUsersAsync(
                deviceActions.Select(s => s.Device.Id).ToArray());
            users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                users, TargetType.DeviceAction, NotificationAction.Add);
            if (users != null && users.Any())
            {
                await _eventsNotifier.DeviceActionAdd(users, deviceActions);
            }
        }

        /// <inheritdoc />
        public async Task ClearAsync(IEnumerable<Guid>? devices)
        {
            if (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.DeviceActionManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg;
            bool admin = User.IsInRole(GXRoles.Admin);
            if (!admin)
            {
                string id = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetDevicesByUser(s => "*", id);
            }
            else
            {
                arg = GXSelectArgs.SelectAll<GXDevice>(w => w.Removed == null);
            }
            arg.Distinct = true;
            arg.Columns.Clear();
            arg.Columns.Add<GXDevice>(s => new { s.Id, s.Name });
            arg.Joins.AddInnerJoin<GXDevice, GXDeviceAction>(y => y.Id, x => x.Device);
            if (devices != null && devices.Any())
            {
                arg.Where.And<GXDevice>(w => devices.Contains(w.Id));
            }
            List<GXDevice>? errors = await _host.Connection.SelectAsync<GXDevice>(arg);
            List<string>? users;
            if (User.IsInRole(GXRoles.Admin))
            {
                users = await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]);
            }
            else
            {
                users = new List<string>();
            }
            //Notification users if their actions are cleared.
            users.AddDistinct(await _deviceRepository.GetUsersAsync(devices));
            if (admin && (devices == null || !devices.Any()))
            {
                //Admin clears all device errors.
                _host.Connection.Truncate<GXDeviceAction>();
                errors = null;
            }
            else if (errors.Any())
            {
                GXDeleteArgs args = GXDeleteArgs.Delete<GXDeviceAction>(w => errors.Contains(w.Device));
                await _host.Connection.DeleteAsync(args);
            }
            //Clear enum types.
            await _enumTypeRepository.DeleteAsync(TargetType.DeviceAction);
            users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings, users,
                TargetType.DeviceAction, NotificationAction.Clear);
            if (users != null && users.Any())
            {
                await _eventsNotifier.DeviceActionsClear(users, errors);
            }
            await _systemLogRepository.AddAsync(TargetType.DeviceAction,
    [new GXSystemLog(TraceLevel.Info)
 {
     Message = Properties.Resources.Clear
 }]);
        }
    }
}