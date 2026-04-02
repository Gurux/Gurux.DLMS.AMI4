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
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using System.Diagnostics;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    class DeviceErrorRepository : IDeviceErrorRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IDeviceRepository _deviceRepository;
        private readonly IUserRepository _userRepository;
        private readonly GXPerformanceSettings _performanceSettings;
        private readonly IEnumTypeRepository _enumTypeRepository;
        private readonly ISystemLogRepository _systemLogRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DeviceErrorRepository(IGXHost host,
            IGXAmiContextAccessor contextAccessor,
            IGXEventsNotifier eventsNotifier,
            IDeviceRepository deviceRepository,
            IUserRepository userRepository,
            IEnumTypeRepository enumTypeRepository,
            GXPerformanceSettings performanceSettings,
            ISystemLogRepository systemLogRepository)
        {
            var user = contextAccessor?.User;
            if (user == null ||
                (!user.IsInRole(GXRoles.Admin) &&
                !user.IsInRole(GXRoles.DeviceLogManager) &&
                //If agent
                !user.Claims.Where(w => w.Type.Equals("scope") && w.Value.Equals(GXAgentPolicies.View)).Any()))
            {
                throw new UnauthorizedAccessException();
            }
            User = user;
            _host = host;
            _eventsNotifier = eventsNotifier;
            _deviceRepository = deviceRepository;
            _userRepository = userRepository;
            _performanceSettings = performanceSettings;
            _enumTypeRepository = enumTypeRepository;
            _systemLogRepository = systemLogRepository;
        }

        /// <inheritdoc />
        public async Task AddAsync(string type, IEnumerable<GXDeviceError> errors)
        {
            DateTime now = DateTime.Now;
            Dictionary<GXDeviceError, List<string>> updates = new Dictionary<GXDeviceError, List<string>>();
            foreach (GXDeviceError it in errors)
            {
                it.CreationTime = now;
                if (it.Device != null)
                {
                    updates[it] = await _deviceRepository.GetUsersAsync(it.Device.Id);
                }
                it.Type = await _enumTypeRepository.GetLogTypeAsync(TargetType.DeviceLog, type);
            }
            await _host.Connection.InsertAsync(GXInsertArgs.InsertRange(errors));
            if (_performanceSettings.Notification(TargetType.DeviceLog))
            {
                foreach (var it in updates)
                {
                    GXDeviceError tmp = new GXDeviceError(it.Key.Level.GetValueOrDefault(1))
                    {
                        CreationTime = now,
                        Message = it.Key.Message,
                        Device = new GXDevice()
                        {
                            Id = it.Key.Device.Id,
                            Name = it.Key.Device.Name,
                        },
                    };
                    var users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                        it.Value, TargetType.DeviceLog, NotificationAction.Add);
                    if (users != null && users.Any())
                    {
                        await _eventsNotifier.AddDeviceErrors(users, [tmp]);
                    }
                }
            }
        }

        /// <inheritdoc />
        public async Task<GXDeviceError> AddAsync(string type, GXDevice device, Exception ex)
        {
            Dictionary<GXDeviceError, List<string>> updates = new Dictionary<GXDeviceError, List<string>>();
            GXDeviceError error = new GXDeviceError(TraceLevel.Error)
            {
                CreationTime = DateTime.Now,
                Message = ex.Message,
                Device = new GXDevice()
                {
                    Id = device.Id,
                    Name = device.Name,
                },
                Type = await _enumTypeRepository.GetLogTypeAsync(TargetType.DeviceLog, type)
            };
            await _host.Connection.InsertAsync(GXInsertArgs.Insert(error));
            if (_performanceSettings.Notification(TargetType.DeviceLog))
            {
                updates[error] = await _deviceRepository.GetUsersAsync(device.Id);
                foreach (var it in updates)
                {
                    await _eventsNotifier.AddDeviceErrors(it.Value, new GXDeviceError[] { it.Key });
                }
            }
            return error;
        }

        /// <inheritdoc />
        public async Task ClearAsync(IEnumerable<Guid>? devices)
        {
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
            arg.Joins.AddInnerJoin<GXDevice, GXDeviceError>(y => y.Id, x => x.Device);
            if (devices != null && devices.Any())
            {
                arg.Where.And<GXDevice>(w => devices.Contains(w.Id));
            }
            List<GXDevice>? notifiedDevices = await _host.Connection.SelectAsync<GXDevice>(arg);
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
                _host.Connection.Truncate<GXDeviceError>();
                notifiedDevices = null;
            }
            else if (notifiedDevices.Any())
            {
                GXDeleteArgs args = GXDeleteArgs.Delete<GXDeviceError>(w => notifiedDevices.Contains(w.Device));
                await _host.Connection.DeleteAsync(args);
            }
            //Clear enum types.
            await _enumTypeRepository.DeleteAsync(TargetType.DeviceLog);
            users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                users, TargetType.DeviceLog, NotificationAction.Clear);
            if (users != null && users.Any())
            {
                await _eventsNotifier.ClearDeviceErrors(users, notifiedDevices);
            }
            await _systemLogRepository.AddAsync(TargetType.DeviceLog,
    [new GXSystemLog(TraceLevel.Info)
 {
     Message = Properties.Resources.Clear
 }]);
        }

        /// <inheritdoc />
        public async Task CloseAsync(IEnumerable<Guid> errors)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXDeviceError>(where => errors.Contains(where.Id));
            arg.Columns.Add<GXDevice>();
            arg.Joins.AddInnerJoin<GXDeviceError, GXDevice>(j => j.Device, j => j.Id);
            List<GXDeviceError> list = (await _host.Connection.SelectAsync<GXDeviceError>(arg));
            Dictionary<GXDeviceError, List<string>> updates = new Dictionary<GXDeviceError, List<string>>();
            foreach (var it in list)
            {
                updates[it] = await _deviceRepository.GetUsersAsync(it.Device.Id);
            }
            DateTime now = DateTime.Now;
            foreach (GXDeviceError it in list)
            {
                it.Closed = now;
            }
            if (list.Any())
            {
                await _host.Connection.UpdateAsync(GXUpdateArgs.UpdateRange(list, q => q.Closed));
                foreach (var it in updates)
                {
                    GXDeviceError tmp = new GXDeviceError(TraceLevel.Error) { Id = it.Key.Id };
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
                    users.AddDistinct(it.Value);
                    users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                        users, TargetType.DeviceLog, NotificationAction.Close);
                    if (users != null && users.Any())
                    {
                        await _eventsNotifier.CloseDeviceErrors(users, [tmp]);
                    }
                }
            }
        }

        /// <inheritdoc />
        public async Task<GXDeviceError[]> ListAsync(

            ListDeviceErrors? request,
            ListDeviceErrorsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers &&
                User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the errors.
                arg = GXSelectArgs.SelectAll<GXDeviceError>();
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXDevice, GXDeviceError>(j => j.Id, j => j.Device);
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetDeviceErrorsByUser(s => "*", userId);
            }
            arg.Columns.Add<GXDevice>(c => new { c.Id, c.Name });
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXDeviceError>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXDeviceError>(w => request.Included.Contains(w.Id));
                }
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXDeviceError>(q => GXSql.DistinctCount(q.Id));
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
                arg.OrderBy.Add<GXDeviceError>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXDeviceError>(q => q.CreationTime);
            }
            //Errors are ignored from the device 
            //so there is no reference relation that is causing problems with JSON parser.
            arg.Columns.Exclude<GXDevice>(e => e.Errors);
            GXDeviceError[] errors = (await _host.Connection.SelectAsync<GXDeviceError>(arg, cancellationToken)).ToArray();
            if (response != null)
            {
                response.Errors = errors;
                if (response.Count == 0)
                {
                    response.Count = errors.Length;
                }
            }
            return errors;
        }

        /// <inheritdoc />
        public async Task<GXDeviceError> ReadAsync(Guid id)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXDeviceError>(where => where.Id == id);
            //Get user.
            arg.Columns.Add<GXWorkflow>();
            arg.Joins.AddInnerJoin<GXDeviceError, GXWorkflow>(x => x.Device, y => y.Id);
            //Errors are ignored from the device 
            //so there is no reference relation that is causing problems with JSON parser.
            arg.Columns.Exclude<GXWorkflow>(e => e.Logs);
            GXDeviceError error = (await _host.Connection.SingleOrDefaultAsync<GXDeviceError>(arg));
            if (error == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            return error;
        }
    }
}
