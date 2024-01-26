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
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using System.Linq.Expressions;
using System.Diagnostics;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared;
using System.Data;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class GatewayRepository : IGatewayRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IGatewayGroupRepository _gatewayGroupRepository;
        private readonly IServiceProvider _serviceProvider;
        private GXPerformanceSettings _performanceSettings;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GatewayRepository(IGXHost host,
            IUserRepository userRepository,
            IGXEventsNotifier eventsNotifier,
            IGatewayGroupRepository gatewayGroupRepository,
            IServiceProvider serviceProvider,
            GXPerformanceSettings performanceSettings)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
            _gatewayGroupRepository = gatewayGroupRepository;
            _serviceProvider = serviceProvider;
            _performanceSettings = performanceSettings;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(
            ClaimsPrincipal User,
            Guid? gatewayId)
        {
            GXSelectArgs args = GXQuery.GetUsersByGateway(ServerHelpers.GetUserId(User), gatewayId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(
            ClaimsPrincipal User,
            IEnumerable<Guid>? gatewayIds)
        {
            GXSelectArgs args = GXQuery.GetUsersByGateways(ServerHelpers.GetUserId(User), gatewayIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <summary>
        /// Returns gateway groups where gateway belongs.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="gatewayId">Gateway ID</param>
        /// <returns>List of gateway groups where gateway belongs.</returns>
        private async Task<List<GXGatewayGroup>> GetGatewayGroupsByGatewayId(
            ClaimsPrincipal User,
            Guid gatewayId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXGatewayGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXGatewayGroup, GXGatewayGroupGateway>(a => a.Id, b => b.GatewayGroupId);
            arg.Joins.AddInnerJoin<GXGatewayGroupGateway, GXGateway>(a => a.GatewayId, b => b.Id);
            arg.Where.And<GXGateway>(where => where.Removed == null && where.Id == gatewayId);
            return (await _host.Connection.SelectAsync<GXGatewayGroup>(arg));
        }

        /// <summary>
        /// Returns joined device groups.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="gatewayId">Gateway ID</param>
        /// <returns>List of joined device groups.</returns>
        private async Task<List<GXDeviceGroup>> GetDeviceGroupsByGatewayId(
            ClaimsPrincipal User,
            Guid gatewayId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXDeviceGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXDeviceGroup, GXGatewayDeviceGroup>(a => a.Id, b => b.DeviceGroupId);
            arg.Joins.AddInnerJoin<GXGatewayDeviceGroup, GXGateway>(a => a.GatewayId, b => b.Id);
            arg.Where.And<GXGateway>(where => where.Removed == null && where.Id == gatewayId);
            return await _host.Connection.SelectAsync<GXDeviceGroup>(arg);
        }

        /// <summary>
        /// Returns joined device.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="gatewayId">Gateway ID</param>
        /// <returns>List of joined devices.</returns>
        private async Task<List<GXDevice>> GetDevicesByGatewayId(
            ClaimsPrincipal User,
            Guid gatewayId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXDevice>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXDevice, GXGatewayDevice>(a => a.Id, b => b.DeviceId);
            arg.Joins.AddInnerJoin<GXGatewayDevice, GXGateway>(a => a.GatewayId, b => b.Id);
            arg.Where.And<GXGateway>(where => where.Removed == null && where.Id == gatewayId);
            return await _host.Connection.SelectAsync<GXDevice>(arg);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(
            ClaimsPrincipal User,
            IEnumerable<Guid> gateways,
            bool delete)
        {
            if (User == null || (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.GatewayManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXGateway>(a => a.Id, q => gateways.Contains(q.Id));
            List<GXGateway> list = _host.Connection.Select<GXGateway>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXGateway, List<string>> updates = new Dictionary<GXGateway, List<string>>();
            foreach (GXGateway it in list)
            {
                it.Removed = now;
                List<string> users = await GetUsersAsync(User, it.Id);
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXGateway>(it.Id));
                }
                else
                {
                    await _host.Connection.UpdateAsync(GXUpdateArgs.Update(it, q => q.Removed));
                }
                updates[it] = users;
            }
            if (_performanceSettings.Notify(TargetType.Gateway))
            {
                foreach (var it in updates)
                {
                    GXGateway tmp = new GXGateway() { Id = it.Key.Id };
                    await _eventsNotifier.GatewayDelete(it.Value, new GXGateway[] { tmp });
                }
            }
        }

        /// <inheritdoc />
        public async Task<GXGateway[]> ListAsync(
            ClaimsPrincipal User,
            ListGateways? request,
            ListGatewaysResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the gateways.
                arg = GXSelectArgs.SelectAll<GXGateway>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetGatewaysByUser(userId, null);
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXGateway>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXGateway>(w => request.Included.Contains(w.Id));
                }
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXGateway>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXGateway>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXGateway>(q => q.CreationTime);
            }
            GXGateway[] gateways = (await _host.Connection.SelectAsync<GXGateway>(arg)).ToArray();
            if (response != null)
            {
                response.Gateways = gateways;
                if (response.Count == 0)
                {
                    response.Count = gateways.Length;
                }
            }
            return gateways;
        }

        /// <inheritdoc />
        public async Task<GXGateway> ReadAsync(
            ClaimsPrincipal User,
            Guid id)
        {
            GXSelectArgs arg;
            if (User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the gateways.
                arg = GXSelectArgs.SelectAll<GXGateway>(w => w.Id == id);
                arg.Where.And<GXGatewayGroup>(w => w.Removed == null);
                arg.Where.And<GXGatewayGroupGateway>(w => w.Removed == null);
                //Gateway installers are not part of any group.
                arg.Joins.AddLeftJoin<GXGateway, GXGatewayGroupGateway>(x => x.Id, y => y.GatewayId);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetGatewaysByUser(userId, id);
                arg.Where.And<GXGatewayGroup>(w => w.Removed == null);
                arg.Where.And<GXGatewayGroupGateway>(w => w.Removed == null);
            }
            arg.Joins.AddLeftJoin<GXGatewayGroupGateway, GXGatewayGroup>(j => j.GatewayGroupId, j => j.Id);
            arg.Joins.AddLeftJoin<GXGateway, GXAgent>(j => j.Agent, j => j.Id);
            arg.Columns.Add<GXGatewayGroup>();
            arg.Columns.Add<GXAgent>(s => new { s.Id, s.Name });
            arg.Columns.Exclude<GXGatewayGroup>(e => e.Gateways);
            arg.Distinct = true;
            GXGateway gateway = await _host.Connection.SingleOrDefaultAsync<GXGateway>(arg);
            if (gateway == null)
            {
                throw new GXAmiNotFoundException(Properties.Resources.Gateway + " " + Properties.Resources.Id + " " + id.ToString());
            }
            if (gateway.GatewayGroups == null)
            {
                gateway.GatewayGroups = new List<GXGatewayGroup>();
            }
            ////////////////////////////////////////////////////
            //Get device groups.
            gateway.DeviceGroups = await GetDeviceGroupsByGatewayId(User, id);
            ////////////////////////////////////////////////////
            //Get devices.
            arg = GXSelectArgs.Select<GXDevice>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXGatewayDevice, GXDevice>(j => j.DeviceId, j => j.Id);
            arg.Where.And<GXGatewayDevice>(q => q.Removed == null && q.GatewayId == id);
            gateway.Devices = await _host.Connection.SelectAsync<GXDevice>(arg);
            return gateway;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal user,
            IEnumerable<GXGateway> gateways,
            Expression<Func<GXGateway, object?>>? columns)
        {
            GXUser creator = new GXUser() { Id = ServerHelpers.GetUserId(user) };
            DateTime now = DateTime.Now;
            List<Guid> list = new List<Guid>();
            Dictionary<GXGateway, List<string>> updates = new Dictionary<GXGateway, List<string>>();
            foreach (GXGateway gateway in gateways)
            {
                if (string.IsNullOrEmpty(gateway.Name) && (columns == null || ServerHelpers.Contains(columns, nameof(GXGateway.Name))))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                if (gateway.Id == Guid.Empty)
                {
                    if (gateway.GatewayGroups == null || !gateway.GatewayGroups.Any())
                    {
                        ListGatewayGroups request = new ListGatewayGroups() { Filter = new GXGatewayGroup() { Default = true } };
                        GXGatewayGroup[] groups = await _gatewayGroupRepository.ListAsync(user, request, null, CancellationToken.None);
                        gateway.GatewayGroups = new List<GXGatewayGroup>(groups);
                    }
                    gateway.CreationTime = now;
                    gateway.Creator = creator;
                    GXInsertArgs args = GXInsertArgs.Insert(gateway);
                    args.Exclude<GXGateway>(q => new { q.Updated, q.GatewayGroups, q.DeviceGroups, q.Devices, q.Removed });
                    _host.Connection.Insert(args);
                    list.Add(gateway.Id);
                    AddGatewayToGatewayGroups(gateway.Id, gateway.GatewayGroups);
                    AddDeviceGroupsToGateway(gateway.Id, gateway.DeviceGroups);
                    AddDevicesToGateway(gateway.Id, gateway.Devices);
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXGateway>(q => q.ConcurrencyStamp, where => where.Id == gateway.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != gateway.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    gateway.Updated = now;
                    gateway.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(gateway, columns);
                    args.Exclude<GXGateway>(q => new { q.CreationTime, q.GatewayGroups, q.DeviceGroups, q.Devices, q.Creator });
                    _host.Connection.Update(args);
                    //Map gateway groups to gateway.
                    if (gateway.GatewayGroups != null && gateway.GatewayGroups.Any())
                    {
                        List<GXGatewayGroup> gatewayGroups = await GetGatewayGroupsByGatewayId(user, gateway.Id);
                        var comparer = new UniqueComparer<GXGatewayGroup, Guid>();
                        List<GXGatewayGroup> removedGatewayGroups = gatewayGroups.Except(gateway.GatewayGroups, comparer).ToList();
                        List<GXGatewayGroup> addedGatewayGroups = gateway.GatewayGroups.Except(gatewayGroups, comparer).ToList();
                        if (removedGatewayGroups.Any())
                        {
                            RemoveGatewaysFromGatewayGroup(gateway.Id, removedGatewayGroups);
                        }
                        if (addedGatewayGroups.Any())
                        {
                            AddGatewayToGatewayGroups(gateway.Id, addedGatewayGroups);
                        }
                    }
                    //Map device groups to gateway.
                    if (gateway.DeviceGroups != null)
                    {
                        List<GXDeviceGroup> deviceGroups = await GetDeviceGroupsByGatewayId(user, gateway.Id);
                        var comparer = new UniqueComparer<GXDeviceGroup, Guid>();
                        List<GXDeviceGroup> removedDeviceGroups = deviceGroups.Except(gateway.DeviceGroups, comparer).ToList();
                        List<GXDeviceGroup> addedDeviceGroups = gateway.DeviceGroups.Except(deviceGroups, comparer).ToList();
                        if (removedDeviceGroups.Any())
                        {
                            RemoveDeviceGroupsFromGateway(gateway.Id, removedDeviceGroups);
                        }
                        if (addedDeviceGroups.Any())
                        {
                            AddDeviceGroupsToGateway(gateway.Id, addedDeviceGroups);
                        }
                    }
                    //Map devices to gateway.
                    if (gateway.Devices != null)
                    {
                        List<GXDevice> devices = await GetDevicesByGatewayId(user, gateway.Id);
                        var comparer = new UniqueComparer<GXDevice, Guid>();
                        List<GXDevice> removedDevices = devices.Except(gateway.Devices, comparer).ToList();
                        List<GXDevice> addedDevices = gateway.Devices.Except(devices, comparer).ToList();
                        if (removedDevices.Any())
                        {
                            RemoveDevicesFromGateway(gateway.Id, removedDevices);
                        }
                        if (addedDevices.Any())
                        {
                            AddDevicesToGateway(gateway.Id, addedDevices);
                        }
                    }
                }
                updates[gateway] = await GetUsersAsync(user, gateway.Id);
            }
            if (_performanceSettings.Notify(TargetType.Gateway))
            {
                foreach (var it in updates)
                {
                    await _eventsNotifier.GatewayUpdate(it.Value, new GXGateway[] { it.Key });
                }
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map gateway group to user groups.
        /// </summary>
        /// <param name="gatewayId">Gateway ID.</param>
        /// <param name="groups">Gateway groups where the gateway is added.</param>
        private void AddGatewayToGatewayGroups(Guid gatewayId, IEnumerable<GXGatewayGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXGatewayGroupGateway> list = new List<GXGatewayGroupGateway>();
            foreach (GXGatewayGroup it in groups)
            {
                list.Add(new GXGatewayGroupGateway()
                {
                    GatewayId = gatewayId,
                    GatewayGroupId = it.Id,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between gateway group and gateway.
        /// </summary>
        /// <param name="gatewayId">Gateway ID.</param>
        /// <param name="groups">Gateway groups where the gateway is removed.</param>
        private void RemoveGatewaysFromGatewayGroup(Guid gatewayId, IEnumerable<GXGatewayGroup> groups)
        {
            foreach (var it in groups)
            {
                _host.Connection.Delete(GXDeleteArgs.Delete<GXGatewayGroupGateway>(w => w.GatewayId == gatewayId && w.GatewayGroupId == it.Id));
            }
        }

        /// <inheritdoc />
        public async Task UpdateStatusAsync(ClaimsPrincipal User, Guid gatewayId, GatewayStatus status)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXGateway>(s => new { s.Id, s.Name }, where => where.Id == gatewayId && where.Removed == null);
            GXGateway gateway = await _host.Connection.SingleOrDefaultAsync<GXGateway>(args);
            if (gateway == null)
            {
                throw new GXAmiNotFoundException(Properties.Resources.Gateway + " " + Properties.Resources.Id + " " + gatewayId.ToString());
            }
            gateway.Status = status;
            gateway.Detected = DateTime.Now;
            GXUpdateArgs update;
            update = GXUpdateArgs.Update(gateway, c => new { c.Status, c.Detected });
            await _host.Connection.UpdateAsync(update);
            //Only part of the gateway properties are send.
            GXGateway tmp = new GXGateway()
            {
                Id = gateway.Id,
                Name = gateway.Name,
                Detected = gateway.Detected,
                Status = gateway.Status
            };
            if (_performanceSettings.Notify(TargetType.Gateway))
            {
                await _eventsNotifier.GatewayStatusChange(await GetUsersAsync(User, gateway.Id), new GXGateway[] { tmp });
            }
            //Update gateway state for the gateway log.
            GXGatewayLog? log = new GXGatewayLog(TraceLevel.Info);
            log.Gateway = gateway;
            switch (status)
            {
                case GatewayStatus.Connected:
                    log.Message = Properties.Resources.Connected;
                    break;
                case GatewayStatus.Offline:
                    log.Message = Properties.Resources.Offline;
                    break;
                case GatewayStatus.Error:
                    log.Message = Properties.Resources.Error;
                    break;
                default:
                    log = null;
                    break;
            }
            if (log != null)
            {
                //Add gateway log. Idle status is not logged.
                IGatewayLogRepository repository = _serviceProvider.GetRequiredService<IGatewayLogRepository>();
                await repository.AddAsync(User, new GXGatewayLog[] { log });
            }
        }

        /// <summary>
        /// Map gateway to device group.
        /// </summary>
        /// <param name="gatewayId">Gateway group ID.</param>
        /// <param name="gateways">Device groups where the gateway is added.</param>
        public void AddDeviceGroupsToGateway(Guid gatewayId, IEnumerable<GXDeviceGroup>? gateways)
        {
            if (gateways != null)
            {
                DateTime now = DateTime.Now;
                List<GXGatewayDeviceGroup> list = new List<GXGatewayDeviceGroup>();
                foreach (var it in gateways)
                {
                    list.Add(new GXGatewayDeviceGroup()
                    {
                        GatewayId = gatewayId,
                        DeviceGroupId = it.Id,
                        CreationTime = now
                    });
                }
                _host.Connection.Insert(GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between gateway and device group.
        /// </summary>
        /// <param name="gatewayId">Gateway group ID.</param>
        /// <param name="groups">Device groups where the gateway is removed.</param>
        public void RemoveDeviceGroupsFromGateway(Guid gatewayId, IEnumerable<GXDeviceGroup> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXGatewayDeviceGroup>();
            foreach (var it in groups)
            {
                args.Where.Or<GXGatewayDeviceGroup>(w => w.DeviceGroupId == it.Id &&
                    w.GatewayId == gatewayId);
            }
            _host.Connection.Delete(args);
        }

        /// <summary>
        /// Map devices to gateway.
        /// </summary>
        /// <param name="gatewayId">Gateway group ID.</param>
        /// <param name="devices">Added devices.</param>
        public void AddDevicesToGateway(Guid gatewayId, IEnumerable<GXDevice>? devices)
        {
            if (devices != null)
            {
                DateTime now = DateTime.Now;
                List<GXGatewayDevice> list = new List<GXGatewayDevice>();
                foreach (var it in devices)
                {
                    list.Add(new GXGatewayDevice()
                    {
                        GatewayId = gatewayId,
                        DeviceId = it.Id,
                        CreationTime = now
                    });
                }
                _host.Connection.Insert(GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between gateway and device.
        /// </summary>
        /// <param name="gatewayId">Gateway group ID.</param>
        /// <param name="devices">Removed devices.</param>
        public void RemoveDevicesFromGateway(Guid gatewayId, IEnumerable<GXDevice> devices)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXGatewayDevice>();
            foreach (var it in devices)
            {
                args.Where.Or<GXGatewayDevice>(w => w.DeviceId == it.Id &&
                    w.GatewayId == gatewayId);
            }
            _host.Connection.Delete(args);
        }

        public Task ResetAsync(ClaimsPrincipal user, IEnumerable<Guid> gateways)
        {
            throw new NotImplementedException();
        }
    }
}
