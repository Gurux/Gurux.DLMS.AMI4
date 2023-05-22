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

using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Client.Shared;
using System.Diagnostics;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    class DeviceErrorRepository : IDeviceErrorRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IDeviceRepository _deviceRepository;
        private readonly IUserRepository _userRepository;
        private GXPerformanceSettings _performanceSettings;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DeviceErrorRepository(IGXHost host,
            IGXEventsNotifier eventsNotifier,
            IDeviceRepository deviceRepository,
            IUserRepository userRepository,
            GXPerformanceSettings performanceSettings)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _deviceRepository = deviceRepository;
            _userRepository = userRepository;
            _performanceSettings = performanceSettings;
        }

        /// <inheritdoc />
        public async Task AddAsync(ClaimsPrincipal User, IEnumerable<GXDeviceError> errors)
        {
            Dictionary<GXDeviceError, List<string>> updates = new Dictionary<GXDeviceError, List<string>>();
            foreach (GXDeviceError it in errors)
            {
                it.CreationTime = DateTime.Now;
                updates[it] = await _deviceRepository.GetUsersAsync(User, it.Device.Id);
            }
            await _host.Connection.InsertAsync(GXInsertArgs.InsertRange(errors));
            if (_performanceSettings.Notify(TargetType.DeviceError))
            {
                foreach (var it in updates)
                {
                    await _eventsNotifier.AddDeviceErrors(it.Value, new GXDeviceError[] { it.Key });
                }
            }
        }

        /// <inheritdoc />
        public async Task<GXDeviceError> AddAsync(ClaimsPrincipal User, GXDevice device, Exception ex)
        {
            Dictionary<GXDeviceError, List<string>> updates = new Dictionary<GXDeviceError, List<string>>();
            GXDeviceError error = new GXDeviceError(TraceLevel.Error)
            {
                CreationTime = DateTime.Now,
                Message = ex.Message,
                Device = device
            };
            await _host.Connection.InsertAsync(GXInsertArgs.Insert(error));
            if (_performanceSettings.Notify(TargetType.DeviceError))
            {
                updates[error] = await _deviceRepository.GetUsersAsync(User, device.Id);
                foreach (var it in updates)
                {
                    await _eventsNotifier.AddDeviceErrors(it.Value, new GXDeviceError[] { it.Key });
                }
            }
            return error;
        }

        /// <inheritdoc />
        public async Task ClearAsync(ClaimsPrincipal user, IEnumerable<Guid>? devices)
        {
            if (!user.IsInRole(GXRoles.Admin) && !user.IsInRole(GXRoles.DeviceLogManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg;
            bool admin = user.IsInRole(GXRoles.Admin);
            if (!admin)
            {
                string id = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetDevicesByUser(id);
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
            List<string> list = await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin });
            //Notify users if their actions are cleared.
            list.AddDistinct(await _deviceRepository.GetUsersAsync(user, devices));
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
            if (_performanceSettings.Notify(TargetType.DeviceError))
            {
                await _eventsNotifier.ClearDeviceErrors(list, notifiedDevices);
            }
        }

        /// <inheritdoc />
        public async Task CloseAsync(ClaimsPrincipal User, IEnumerable<Guid> errors)
        {
            if (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.DeviceLogManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXDeviceError>(where => errors.Contains(where.Id));
            arg.Columns.Add<GXDevice>();
            arg.Joins.AddInnerJoin<GXDeviceError, GXDevice>(j => j.Device, j => j.Id);
            List<GXDeviceError> list = (await _host.Connection.SelectAsync<GXDeviceError>(arg));
            Dictionary<GXDeviceError, List<string>> updates = new Dictionary<GXDeviceError, List<string>>();
            foreach (var it in list)
            {
                updates[it] = await _deviceRepository.GetUsersAsync(User, it.Device.Id);
            }
            DateTime now = DateTime.Now;
            foreach (GXDeviceError it in list)
            {
                it.Closed = now;
            }
            if (list.Count != 0)
            {
                await _host.Connection.UpdateAsync(GXUpdateArgs.UpdateRange(list, q => q.Closed));
                if (_performanceSettings.Notify(TargetType.DeviceError))
                {
                    foreach (var it in updates)
                    {
                        GXDeviceError tmp = new GXDeviceError(TraceLevel.Error) { Id = it.Key.Id };
                        await _eventsNotifier.CloseDeviceErrors(it.Value, new GXDeviceError[] { tmp });
                    }
                }
            }
        }

        /// <inheritdoc />
        public async Task<GXDeviceError[]> ListAsync(
            ClaimsPrincipal user,
            ListDeviceErrors? request,
            ListDeviceErrorsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && user.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the errors.
                arg = GXSelectArgs.SelectAll<GXDeviceError>();
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXDevice, GXDeviceError>(j => j.Id, j => j.Device);
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetDeviceErrorsByUser(userId, null);
            }
            arg.Columns.Add<GXDevice>(c => new { c.Id, c.Name });
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXDeviceError>(w => request.Exclude.Contains(w.Id) == false);
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
        public async Task<GXDeviceError> ReadAsync(ClaimsPrincipal User, Guid id)
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
