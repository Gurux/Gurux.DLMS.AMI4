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

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc/>
    public class DeviceTraceRepository : IDeviceTraceRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IDeviceRepository _deviceRepository;
        private readonly IUserRepository _userRepository;
        private GXPerformanceSettings _performanceSettings;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DeviceTraceRepository(
            IGXHost host,
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

        /// <inheritdoc/>
        public async Task<GXDeviceTrace[]> ListAsync(
            ClaimsPrincipal user,
            ListDeviceTrace? request,
            ListDeviceTraceResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && user.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the traces.
                arg = GXSelectArgs.SelectAll<GXDeviceTrace>();
                arg.Joins.AddInnerJoin<GXDeviceTrace, GXDevice>(j => j.Device, j => j.Id);
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetDeviceTracesByUser(userId, null);
            }
            arg.Columns.Add<GXDevice>(c => new { c.Id, c.Name });
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXDeviceTrace>(w => !request.Exclude.Contains(w.Id));
                }
            }
            arg.Distinct = true;
            //Trace is shown as ascending order;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXDeviceTrace>(q => GXSql.DistinctCount(q.Id));
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
                arg.OrderBy.Add<GXDeviceTrace>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXDeviceTrace>(q => q.CreationTime);
            }
            //Traces are ignored from the device 
            //so there is no reference relation that is causing problems with JSON parser.
            arg.Columns.Exclude<GXDevice>(e => e.Traces);
            GXDeviceTrace[] traces = (await _host.Connection.SelectAsync<GXDeviceTrace>(arg, cancellationToken)).ToArray();
            if (response != null)
            {
                response.Traces = traces;
                if (response.Count == 0)
                {
                    response.Count = traces.Length;
                }
            }
            return traces;
        }

        /// <inheritdoc/>
        public async Task<GXDeviceTrace> ReadAsync(ClaimsPrincipal user, Guid id)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXDeviceTrace>(where => where.Id == id);
            //Get device.
            arg.Columns.Add<GXDevice>();
            arg.Joins.AddInnerJoin<GXDeviceTrace, GXDevice>(x => x.Device, y => y.Id);
            //Traces are ignored from the device 
            //so there is no reference relation that is causing problems with JSON parser.
            arg.Columns.Exclude<GXDevice>(e => e.Traces);
            GXDeviceTrace deviceTrace = (await _host.Connection.SingleOrDefaultAsync<GXDeviceTrace>(arg));
            if (deviceTrace == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            return deviceTrace;
        }

        /// <inheritdoc/>
        public async Task AddAsync(ClaimsPrincipal user, IEnumerable<GXDeviceTrace> deviceTraces)
        {
            foreach (var it in deviceTraces)
            {
                it.CreationTime = DateTime.Now;
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(deviceTraces));
            //TODO: ToArray()
            List<string> users = await _deviceRepository.GetUsersAsync(user,
                deviceTraces.Select(s => s.Device.Id).ToArray());
            if (_performanceSettings.Notify(TargetType.DeviceTrace))
            {
                await _eventsNotifier.DeviceTraceAdd(users, deviceTraces);
            }
        }

        /// <inheritdoc />
        public async Task ClearAsync(ClaimsPrincipal user, IEnumerable<Guid>? devices)
        {
            if (!user.IsInRole(GXRoles.Admin) && !user.IsInRole(GXRoles.DeviceTraceManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg;
            bool admin = user.IsInRole(GXRoles.Admin);
            if (!admin)
            {
                string id = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetDevicesByUser(id, true);
            }
            else
            {
                arg = GXSelectArgs.SelectAll<GXDevice>(w => w.Removed == null);
            }
            arg.Distinct = true;
            arg.Columns.Clear();
            arg.Columns.Add<GXDevice>(s => new { s.Id, s.Name });
            arg.Joins.AddInnerJoin<GXDevice, GXDeviceTrace>(y => y.Id, x => x.Device);
            if (devices != null && devices.Any())
            {
                arg.Where.And<GXDevice>(w => devices.Contains(w.Id));
            }
            List<GXDevice>? errors = await _host.Connection.SelectAsync<GXDevice>(arg);
            List<string> list = await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin });
            //Notify users if their actions are cleared.
            list.AddDistinct(await _deviceRepository.GetUsersAsync(user, devices));
            if (admin && (devices == null || !devices.Any()))
            {
                //Admin clears all device errors.
                _host.Connection.Truncate<GXDeviceTrace>();
                errors = null;
            }
            else if (errors.Any())
            {
                GXDeleteArgs args = GXDeleteArgs.Delete<GXDeviceTrace>(w => errors.Contains(w.Device));
                await _host.Connection.DeleteAsync(args);
            }
            if (_performanceSettings.Notify(TargetType.DeviceTrace))
            {
                await _eventsNotifier.DeviceTraceClear(list, errors);
            }
        }
    }
}