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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Client.Shared;
using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared.DTOs.Subtotal;
using System.Data;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Client.Pages.Subtotal;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class SubtotalRepository : ISubtotalRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly ISubtotalGroupRepository _subtotalGroupRepository;
        private readonly ISubtotal _subTotal;

        /// <summary>
        /// Constructor.
        /// </summary>
        public SubtotalRepository(IGXHost host,
            IUserRepository userRepository,
            IServiceProvider serviceProvider,
            ISubtotalGroupRepository subtotalGroupRepository,
            IGXEventsNotifier eventsNotifier,
            ISubtotal subTotal)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
            _serviceProvider = serviceProvider;
            _subtotalGroupRepository = subtotalGroupRepository;
            _subTotal = subTotal;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal user, Guid? subtotalId)
        {
            GXSelectArgs args = GXQuery.GetUsersBySubtotal(ServerHelpers.GetUserId(user), subtotalId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal user, IEnumerable<Guid>? subtotalIds)
        {
            GXSelectArgs args = GXQuery.GetUsersBySubtotals(ServerHelpers.GetUserId(user), subtotalIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal User,
            IEnumerable<Guid> subtotals,
            bool delete)
        {
            if (User == null || (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.SubtotalManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXSubtotal>(a => a.Id, q => subtotals.Contains(q.Id));
            List<GXSubtotal> list = _host.Connection.Select<GXSubtotal>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXSubtotal, List<string>> updates = new();
            foreach (GXSubtotal it in list)
            {
                it.Removed = now;
                List<string> users = await GetUsersAsync(User, it.Id);
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXSubtotal>(it.Id));
                }
                else
                {
                    _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
                }
                updates[it] = users;
            }
            foreach (var it in updates)
            {
                GXSubtotal tmp = new GXSubtotal() { Id = it.Key.Id };
                await _eventsNotifier.SubtotalDelete(it.Value, new GXSubtotal[] { tmp });
            }
        }

        /// <inheritdoc />
        public async Task<GXSubtotal[]> ListAsync(
            ClaimsPrincipal user,
            ListSubtotals? request,
            ListSubtotalsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && user.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the subtotals.
                arg = GXSelectArgs.SelectAll<GXSubtotal>();
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetSubtotalsByUser(userId, null);
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXSubtotal>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXSubtotal>(w => request.Included.Contains(w.Id));
                }
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXSubtotal>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXSubtotal>(q => q.CreationTime);
                arg.Descending = true;
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXSubtotal>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            GXSubtotal[] subtotals = (await _host.Connection.SelectAsync<GXSubtotal>(arg)).ToArray();
            if (response != null)
            {
                response.Subtotals = subtotals;
                if (response.Count == 0)
                {
                    response.Count = subtotals.Length;
                }
            }
            return subtotals;
        }

        /// <inheritdoc />
        public async Task<GXSubtotal> ReadAsync(
            ClaimsPrincipal user,
            Guid id)
        {
            bool isAdmin = user.IsInRole(GXRoles.Admin);
            string userId = ServerHelpers.GetUserId(user);
            GXSelectArgs arg;
            if (user == null || isAdmin)
            {
                //Admin can see all the subtotals.
                arg = GXSelectArgs.SelectAll<GXSubtotal>(w => w.Id == id);
                arg.Joins.AddLeftJoin<GXSubtotal, GXSubtotalGroupSubtotal>(x => x.Id, y => y.SubtotalId);
                arg.Joins.AddLeftJoin<GXSubtotalGroupSubtotal, GXSubtotalGroup>(j => j.SubtotalGroupId, j => j.Id);
            }
            else
            {
                arg = GXQuery.GetSubtotalsByUser(userId, id);
                arg.Joins.AddInnerJoin<GXSubtotalGroupSubtotal, GXSubtotalGroup>(j => j.SubtotalGroupId, j => j.Id);
            }
            arg.Columns.Add<GXSubtotalGroup>();
            arg.Columns.Exclude<GXSubtotalGroup>(e => e.Subtotals);
            arg.Distinct = true;
            GXSubtotal subtotal = await _host.Connection.SingleOrDefaultAsync<GXSubtotal>(arg);
            if (subtotal == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            //Get agents.
            arg = GXSelectArgs.Select<GXAgent>(s => new { s.Id, s.Name });
            arg.Joins.AddInnerJoin<GXSubtotal, GXSubtotalAgent>(y => y.Id, x => x.SubtotalId);
            arg.Joins.AddInnerJoin<GXSubtotalAgent, GXAgent>(y => y.AgentId, x => x.Id);
            arg.Where.And<GXSubtotal>(w => w.Id == subtotal.Id);
            subtotal.Agents = (await _host.Connection.SelectAsync<GXAgent>(arg)).ToList();
            //Get gateways.
            arg = GXSelectArgs.Select<GXGateway>(s => new { s.Id, s.Name });
            arg.Joins.AddInnerJoin<GXSubtotal, GXSubtotalGateway>(y => y.Id, x => x.SubtotalId);
            arg.Joins.AddInnerJoin<GXSubtotalGateway, GXGateway>(y => y.GatewayId, x => x.Id);
            arg.Where.And<GXSubtotal>(w => w.Id == subtotal.Id);
            subtotal.Gateways = (await _host.Connection.SelectAsync<GXGateway>(arg)).ToList();
            //Get device attributes with own query.
            arg = GXSelectArgs.Select<GXAttributeTemplate>(s => new { s.Id, s.Name, s.ObjectTemplate, s.Index });
            arg.Joins.AddInnerJoin<GXSubtotal, GXSubtotalDeviceAttributeTemplate>(y => y.Id, x => x.SubtotalId);
            arg.Joins.AddInnerJoin<GXSubtotalDeviceAttributeTemplate, GXAttributeTemplate>(y => y.AttributeTemplateId, x => x.Id);
            arg.Joins.AddInnerJoin<GXAttributeTemplate, GXObjectTemplate>(y => y.ObjectTemplate, x => x.Id);
            arg.Columns.Add<GXObjectTemplate>(s => new { s.Id, s.Name, s.LogicalName, s.Attributes });
            arg.Columns.Exclude<GXObjectTemplate>(e => e.Attributes);
            arg.Where.And<GXSubtotal>(w => w.Id == subtotal.Id);
            subtotal.DeviceAttributeTemplates = (await _host.Connection.SelectAsync<GXAttributeTemplate>(arg)).ToList();
            //Get devices with own query.
            arg = GXSelectArgs.Select<GXDevice>(s => new { s.Id, s.Name, s.Template });
            arg.Columns.Add<GXDeviceTemplate>(s => new { s.Id });
            arg.Joins.AddInnerJoin<GXSubtotal, GXSubtotalDevice>(y => y.Id, x => x.SubtotalId);
            arg.Joins.AddInnerJoin<GXSubtotalDevice, GXDevice>(y => y.DeviceId, x => x.Id);
            arg.Joins.AddInnerJoin<GXDevice, GXDeviceTemplate>(y => y.Template, x => x.Id);
            arg.Where.And<GXSubtotal>(w => w.Id == subtotal.Id);
            subtotal.Devices = (await _host.Connection.SelectAsync<GXDevice>(arg)).ToList();
            //Get device groups with own query.
            arg = GXSelectArgs.SelectAll<GXDeviceGroup>();
            arg.Joins.AddInnerJoin<GXSubtotal, GXSubtotalDeviceGroup>(y => y.Id, x => x.SubtotalId);
            arg.Joins.AddInnerJoin<GXSubtotalDeviceGroup, GXDeviceGroup>(y => y.DeviceGroupId, x => x.Id);
            arg.Where.And<GXSubtotal>(w => w.Id == subtotal.Id);
            subtotal.DeviceGroups = (await _host.Connection.SelectAsync<GXDeviceGroup>(arg)).ToList();
            //Get device group attributes with own query.
            arg = GXSelectArgs.Select<GXAttributeTemplate>(s => new { s.Id, s.Name, s.ObjectTemplate, s.Index });
            arg.Joins.AddInnerJoin<GXSubtotal, GXSubtotalDeviceGroupAttributeTemplate>(y => y.Id, x => x.SubtotalId);
            arg.Joins.AddInnerJoin<GXSubtotalDeviceGroupAttributeTemplate, GXAttributeTemplate>(y => y.AttributeTemplateId, x => x.Id);
            arg.Joins.AddInnerJoin<GXAttributeTemplate, GXObjectTemplate>(y => y.ObjectTemplate, x => x.Id);
            arg.Columns.Add<GXObjectTemplate>(s => new { s.Id, s.Name, s.LogicalName, s.Attributes });
            arg.Columns.Exclude<GXObjectTemplate>(e => e.Attributes);
            arg.Where.And<GXSubtotal>(w => w.Id == subtotal.Id);
            subtotal.DeviceGroupAttributeTemplates = (await _host.Connection.SelectAsync<GXAttributeTemplate>(arg)).ToList();

            //Get agent groups.
            arg = GXSelectArgs.Select<GXAgentGroup>(s => new { s.Id, s.Name });
            arg.Joins.AddInnerJoin<GXSubtotal, GXSubtotalAgentGroup>(y => y.Id, x => x.SubtotalId);
            arg.Joins.AddInnerJoin<GXSubtotalAgentGroup, GXAgentGroup>(y => y.AgentGroupId, x => x.Id);
            arg.Where.And<GXSubtotal>(w => w.Id == subtotal.Id);
            subtotal.AgentGroups = (await _host.Connection.SelectAsync<GXAgentGroup>(arg)).ToList();
            //Get gateway groups.
            arg = GXSelectArgs.Select<GXGatewayGroup>(s => new { s.Id, s.Name });
            arg.Joins.AddInnerJoin<GXSubtotal, GXSubtotalGatewayGroup>(y => y.Id, x => x.SubtotalId);
            arg.Joins.AddInnerJoin<GXSubtotalGatewayGroup, GXGatewayGroup>(y => y.GatewayGroupId, x => x.Id);
            arg.Where.And<GXSubtotal>(w => w.Id == subtotal.Id);
            subtotal.GatewayGroups = (await _host.Connection.SelectAsync<GXGatewayGroup>(arg)).ToList();
            return subtotal;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal user,
            IEnumerable<GXSubtotal> subtotals,
            Expression<Func<GXSubtotal, object?>>? columns)
        {
            GXUser creator = new GXUser() { Id = ServerHelpers.GetUserId(user) };
            DateTime now = DateTime.Now;
            List<Guid> list = new();
            using IDbTransaction transaction = _host.Connection.BeginTransaction();
            try
            {
                foreach (GXSubtotal subtotal in subtotals)
                {
                    if (string.IsNullOrEmpty(subtotal.Name) &&
                        (columns == null || ServerHelpers.Contains(columns, nameof(GXSubtotal.Name))))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    if (subtotal.SubtotalGroups == null || !subtotal.SubtotalGroups.Any())
                    {
                        ListSubtotalGroups request = new ListSubtotalGroups()
                        {
                            Filter = new GXSubtotalGroup() { Default = true }
                        };
                        subtotal.SubtotalGroups = new List<GXSubtotalGroup>();
                        subtotal.SubtotalGroups.AddRange(await _subtotalGroupRepository.ListAsync(user, request, null, CancellationToken.None));
                        if (!subtotal.SubtotalGroups.Any())
                        {
                            throw new ArgumentException(Properties.Resources.TargetMustBelongToOneGroup);
                        }
                    }
                    if (subtotal.Id == Guid.Empty)
                    {
                        subtotal.CreationTime = now;
                        subtotal.Creator = creator;
                        GXInsertArgs args = GXInsertArgs.Insert(subtotal);
                        args.Exclude<GXSubtotal>(q => new
                        {
                            q.SubtotalGroups,
                            q.DeviceGroups,
                            q.Devices,
                            q.DeviceAttributeTemplates,
                            q.DeviceGroupAttributeTemplates,
                            q.Agents,
                            q.Gateways,
                        });
                        _host.Connection.Insert(args);
                        list.Add(subtotal.Id);
                        AddSubtotalToSubtotalGroups(transaction, subtotal.Id, subtotal.SubtotalGroups);
                        AddDeviceAttributesToSubtotal(transaction, subtotal.Id, subtotal.DeviceAttributeTemplates);
                        AddDevicesToSubtotal(transaction, subtotal.Id, subtotal.Devices);
                        AddDeviceGroupsToSubtotal(transaction, subtotal.Id, subtotal.DeviceGroups);
                        AddDeviceGroupAttributesToSubtotal(transaction, subtotal.Id, subtotal.DeviceGroupAttributeTemplates);
                        AddAgentsToSubtotal(transaction, subtotal.Id, subtotal.Agents);
                        AddGatewaysToSubtotal(transaction, subtotal.Id, subtotal.Gateways);
                    }
                    else
                    {
                        if (columns == null || !ServerHelpers.Contains(columns, nameof(GXSubtotal.Status)))
                        {
                            GXSelectArgs m = GXSelectArgs.Select<GXSubtotal>(q => q.ConcurrencyStamp, where => where.Id == subtotal.Id);
                            string updated = _host.Connection.SingleOrDefault<string>(m);
                            if (!string.IsNullOrEmpty(updated) && updated != subtotal.ConcurrencyStamp)
                            {
                                throw new ArgumentException(Properties.Resources.ContentEdited);
                            }
                            subtotal.ConcurrencyStamp = Guid.NewGuid().ToString();
                            subtotal.Updated = now;
                        }
                        GXUpdateArgs args = GXUpdateArgs.Update(subtotal, columns);
                        args.Exclude<GXSubtotal>(q => new
                        {
                            q.CreationTime,
                            q.SubtotalGroups,
                            q.DeviceGroups,
                            q.Devices,
                            q.DeviceAttributeTemplates,
                            q.DeviceGroupAttributeTemplates,
                            q.Agents,
                            q.Gateways,
                        });
                        _host.Connection.Update(args);
                        //Map subtotal groups to subtotal.
                        List<GXSubtotalGroup> subtotalGroups;
                        using (IServiceScope scope = _serviceProvider.CreateScope())
                        {
                            {
                                ISubtotalGroupRepository subtotalGroupRepository = scope.ServiceProvider.GetRequiredService<ISubtotalGroupRepository>();
                                subtotalGroups = await subtotalGroupRepository.GetJoinedSubtotalGroups(user, subtotal.Id);
                                var comparer = new UniqueComparer<GXSubtotalGroup, Guid>();
                                List<GXSubtotalGroup> removedSubtotalGroups = subtotalGroups.Except(subtotal.SubtotalGroups, comparer).ToList();
                                List<GXSubtotalGroup> addedSubtotalGroups = subtotal.SubtotalGroups.Except(subtotalGroups, comparer).ToList();
                                if (removedSubtotalGroups.Any())
                                {
                                    RemoveSubtotalsFromSubtotalGroup(transaction,
                                        subtotal.Id, removedSubtotalGroups);
                                }
                                if (addedSubtotalGroups.Any())
                                {
                                    AddSubtotalToSubtotalGroups(transaction,
                                        subtotal.Id, addedSubtotalGroups);
                                }
                            }
                        }
                        //Add agents.
                        if (columns == null || ServerHelpers.Contains(columns, nameof(GXSubtotal.Agents)))
                        {
                            GXSelectArgs arg = GXSelectArgs.SelectAll<GXAgent>(where => where.Removed == null);
                            arg.Joins.AddInnerJoin<GXAgent, GXSubtotalAgent>(a => a.Id, b => b.AgentId);
                            arg.Where.And<GXSubtotalAgent>(where => where.Removed == null && where.SubtotalId == subtotal.Id);
                            List<GXAgent> agents = (await _host.Connection.SelectAsync<GXAgent>(arg)).ToList();
                            var comparer = new UniqueComparer<GXAgent, Guid>();
                            List<GXAgent> removed = agents.Except(subtotal.Agents, comparer).ToList();
                            List<GXAgent> added = subtotal.Agents.Except(agents, comparer).ToList();
                            if (removed.Any())
                            {
                                RemoveAgentsFromSubtotals(transaction,
                                    subtotal.Id, removed);
                            }
                            if (added.Any())
                            {
                                AddAgentsToSubtotal(transaction,
                                    subtotal.Id, added);
                            }
                        }
                        //Add gateways.
                        if (columns == null || ServerHelpers.Contains(columns, nameof(GXSubtotal.Gateways)))
                        {
                            GXSelectArgs arg = GXSelectArgs.SelectAll<GXGateway>(where => where.Removed == null);
                            arg.Joins.AddInnerJoin<GXGateway, GXSubtotalGateway>(a => a.Id, b => b.GatewayId);
                            arg.Where.And<GXSubtotalGateway>(where => where.Removed == null && where.SubtotalId == subtotal.Id);
                            List<GXGateway> gateways = (await _host.Connection.SelectAsync<GXGateway>(arg)).ToList();
                            var comparer = new UniqueComparer<GXGateway, Guid>();
                            List<GXGateway> removed = gateways.Except(subtotal.Gateways, comparer).ToList();
                            List<GXGateway> added = subtotal.Gateways.Except(gateways, comparer).ToList();
                            if (removed.Any())
                            {
                                RemoveGatewaysFromSubtotals(transaction,
                                    subtotal.Id, removed);
                            }
                            if (added.Any())
                            {
                                AddGatewaysToSubtotal(transaction,
                                    subtotal.Id, added);
                            }
                        }
                        //Add device template attributes.
                        if (columns == null || ServerHelpers.Contains(columns, nameof(GXSubtotal.DeviceAttributeTemplates)))
                        {
                            GXSelectArgs arg = GXSelectArgs.SelectAll<GXAttributeTemplate>(where => where.Removed == null);
                            arg.Joins.AddInnerJoin<GXAttributeTemplate, GXSubtotalDeviceAttributeTemplate>(a => a.Id, b => b.AttributeTemplateId);
                            arg.Where.And<GXSubtotalDeviceAttributeTemplate>(where => where.Removed == null && where.SubtotalId == subtotal.Id);
                            List<GXAttributeTemplate> attributes = (await _host.Connection.SelectAsync<GXAttributeTemplate>(arg)).ToList();
                            var comparer = new UniqueComparer<GXAttributeTemplate, Guid>();
                            List<GXAttributeTemplate> removedSubtotalAttributes = attributes.Except(subtotal.DeviceAttributeTemplates, comparer).ToList();
                            List<GXAttributeTemplate> addedSubtotalAttributes = subtotal.DeviceAttributeTemplates.Except(attributes, comparer).ToList();
                            if (removedSubtotalAttributes.Any())
                            {
                                RemoveDeviceAttributesFromSubtotals(transaction,
                                    subtotal.Id, removedSubtotalAttributes);
                            }
                            if (addedSubtotalAttributes.Any())
                            {
                                AddDeviceAttributesToSubtotal(transaction,
                                    subtotal.Id, addedSubtotalAttributes);
                            }
                        }
                        if (columns == null || ServerHelpers.Contains(columns, nameof(GXSubtotal.DeviceGroupAttributeTemplates)))
                        {
                            GXSelectArgs arg = GXSelectArgs.SelectAll<GXAttributeTemplate>(where => where.Removed == null);
                            arg.Joins.AddInnerJoin<GXAttributeTemplate, GXSubtotalDeviceGroupAttributeTemplate>(a => a.Id, b => b.AttributeTemplateId);
                            arg.Where.And<GXSubtotalDeviceGroupAttributeTemplate>(where => where.Removed == null && where.SubtotalId == subtotal.Id);
                            List<GXAttributeTemplate> attributes = (await _host.Connection.SelectAsync<GXAttributeTemplate>(arg)).ToList();
                            var comparer = new UniqueComparer<GXAttributeTemplate, Guid>();
                            List<GXAttributeTemplate> removedSubtotalAttributes = attributes.Except(subtotal.DeviceGroupAttributeTemplates, comparer).ToList();
                            List<GXAttributeTemplate> addedSubtotalAttributes = subtotal.DeviceGroupAttributeTemplates.Except(attributes, comparer).ToList();
                            if (removedSubtotalAttributes.Any())
                            {
                                RemoveDeviceGroupAttributesFromSubtotals(transaction,
                                    subtotal.Id, removedSubtotalAttributes);
                            }
                            if (addedSubtotalAttributes.Any())
                            {
                                AddDeviceGroupAttributesToSubtotal(transaction,
                                    subtotal.Id, addedSubtotalAttributes);
                            }
                        }
                        if (columns == null || ServerHelpers.Contains(columns, nameof(GXSubtotal.Devices)))
                        {
                            GXSelectArgs arg = GXSelectArgs.SelectAll<GXDevice>(where => where.Removed == null);
                            arg.Joins.AddInnerJoin<GXDevice, GXSubtotalDevice>(a => a.Id, b => b.DeviceId);
                            arg.Where.And<GXSubtotalDevice>(where => where.Removed == null && where.SubtotalId == subtotal.Id);
                            List<GXDevice> devices = (await _host.Connection.SelectAsync<GXDevice>(arg)).ToList();
                            var comparer = new UniqueComparer<GXDevice, Guid>();
                            List<GXDevice> removedSubtotalDevices = devices.Except(subtotal.Devices, comparer).ToList();
                            List<GXDevice> addedSubtotalDevices = subtotal.Devices.Except(devices, comparer).ToList();
                            if (removedSubtotalDevices.Any())
                            {
                                RemoveDevicesFromSubtotals(transaction,
                                    subtotal.Id, removedSubtotalDevices);
                            }
                            if (addedSubtotalDevices.Any())
                            {
                                AddDevicesToSubtotal(transaction,
                                    subtotal.Id, addedSubtotalDevices);
                            }
                        }
                        if (columns == null || ServerHelpers.Contains(columns, nameof(GXSubtotal.DeviceGroups)))
                        {
                            GXSelectArgs arg = GXSelectArgs.SelectAll<GXDeviceGroup>(where => where.Removed == null);
                            arg.Joins.AddInnerJoin<GXDeviceGroup, GXSubtotalDeviceGroup>(a => a.Id, b => b.DeviceGroupId);
                            arg.Where.And<GXSubtotalDeviceGroup>(where => where.Removed == null && where.SubtotalId == subtotal.Id);
                            List<GXDeviceGroup> deviceGroups = (await _host.Connection.SelectAsync<GXDeviceGroup>(arg)).ToList();
                            var comparer = new UniqueComparer<GXDeviceGroup, Guid>();
                            List<GXDeviceGroup> removedSubtotalDeviceGroups = deviceGroups.Except(subtotal.DeviceGroups, comparer).ToList();
                            List<GXDeviceGroup> addedSubtotalDeviceGroups = subtotal.DeviceGroups.Except(deviceGroups, comparer).ToList();
                            if (removedSubtotalDeviceGroups.Any())
                            {
                                RemoveDeviceGroupsFromSubtotals(transaction,
                                    subtotal.Id, removedSubtotalDeviceGroups);
                            }
                            if (addedSubtotalDeviceGroups.Any())
                            {
                                AddDeviceGroupsToSubtotal(transaction,
                                    subtotal.Id, addedSubtotalDeviceGroups);
                            }
                        }
                    }
                }
                _host.Connection.CommitTransaction(transaction);
            }
            catch (Exception)
            {
                _host.Connection.RollbackTransaction(transaction);
                throw;
            }
            Dictionary<GXSubtotal, List<string>> updates = new();
            foreach (GXSubtotal subtotal in subtotals)
            {
                updates[subtotal] = await GetUsersAsync(user, subtotal.Id);
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.SubtotalUpdate(it.Value, new GXSubtotal[] { it.Key });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map subtotal to sub total groups.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="subtotalId">Subtotal ID.</param>
        /// <param name="groups">Group IDs of the subtotal groups where the subtotal is added.</param>
        public void AddSubtotalToSubtotalGroups(
            IDbTransaction transaction,
            Guid subtotalId, IEnumerable<GXSubtotalGroup>? groups)
        {
            if (groups != null)
            {
                DateTime now = DateTime.Now;
                List<GXSubtotalGroupSubtotal> list = new();
                foreach (GXSubtotalGroup it in groups)
                {
                    list.Add(new GXSubtotalGroupSubtotal()
                    {
                        SubtotalId = subtotalId,
                        SubtotalGroupId = it.Id,
                        CreationTime = now
                    });
                }
                _host.Connection.Insert(transaction,
                    GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between subtotal group and subtotal.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="subtotalId">Subtotal ID.</param>
        /// <param name="groups">Group IDs of the subtotal groups where the subtotal is removed.</param>
        public void RemoveSubtotalsFromSubtotalGroup(
            IDbTransaction transaction,
            Guid subtotalId, IEnumerable<GXSubtotalGroup> groups)
        {
            foreach (GXSubtotalGroup it in groups)
            {
                _host.Connection.Delete(transaction,
                GXDeleteArgs.Delete<GXSubtotalGroupSubtotal>(w => w.SubtotalId == subtotalId && w.SubtotalGroupId == it.Id));
            }
        }

        /// <summary>
        /// Map device with subtotal.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="subtotalId">Subtotal ID.</param>
        /// <param name="devices">Joined devices.</param>
        public void AddDevicesToSubtotal(
            IDbTransaction transaction,
            Guid subtotalId, IEnumerable<GXDevice>? devices)
        {
            if (devices != null)
            {
                DateTime now = DateTime.Now;
                List<GXSubtotalDevice> list = new();
                foreach (GXDevice it in devices)
                {
                    list.Add(new GXSubtotalDevice()
                    {
                        SubtotalId = subtotalId,
                        DeviceId = it.Id,
                        CreationTime = now
                    });
                }
                _host.Connection.Insert(transaction,
                    GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between device and subtotal.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="subtotalId">Subtotal ID.</param>
        /// <param name="devices">Removed devices.</param>
        public void RemoveDevicesFromSubtotals(
            IDbTransaction transaction,
            Guid subtotalId, IEnumerable<GXDevice> devices)
        {
            foreach (GXDevice it in devices)
            {
                _host.Connection.Delete(transaction,
                    GXDeleteArgs.Delete<GXSubtotalDevice>(w => w.SubtotalId == subtotalId && w.DeviceId == it.Id));
            }
        }

        /// <summary>
        /// Map device group with subtotal.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="subtotalId">Subtotal ID.</param>
        /// <param name="groups">Joined device groups.</param>
        public void AddDeviceGroupsToSubtotal(IDbTransaction transaction,
            Guid subtotalId, IEnumerable<GXDeviceGroup>? groups)
        {
            if (groups != null)
            {
                DateTime now = DateTime.Now;
                List<GXSubtotalDeviceGroup> list = new();
                foreach (GXDeviceGroup it in groups)
                {
                    list.Add(new GXSubtotalDeviceGroup()
                    {
                        SubtotalId = subtotalId,
                        DeviceGroupId = it.Id,
                        CreationTime = now
                    });
                }
                _host.Connection.Insert(transaction,
                    GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between device group and subtotal.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="subtotalId">Subtotal ID.</param>
        /// <param name="groups">Removed device groups.</param>
        public void RemoveDeviceGroupsFromSubtotals(IDbTransaction transaction,
            Guid subtotalId, IEnumerable<GXDeviceGroup> groups)
        {
            foreach (GXDeviceGroup it in groups)
            {
                _host.Connection.Delete(transaction,
                    GXDeleteArgs.Delete<GXSubtotalDeviceGroup>(w => w.SubtotalId == subtotalId && w.DeviceGroupId == it.Id));
            }
        }

        /// <summary>
        /// Map agents with subtotal.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="subtotalId">Subtotal ID.</param>
        /// <param name="agents">Joined agents.</param>
        public void AddAgentsToSubtotal(IDbTransaction transaction,
            Guid subtotalId, IEnumerable<GXAgent>? agents)
        {
            if (agents != null)
            {
                DateTime now = DateTime.Now;
                List<GXSubtotalAgent> list = new();
                foreach (GXAgent it in agents)
                {
                    list.Add(new GXSubtotalAgent()
                    {
                        SubtotalId = subtotalId,
                        AgentId = it.Id,
                        CreationTime = now
                    });
                }
                _host.Connection.Insert(transaction,
                    GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between agents and subtotal.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="subtotalId">Subtotal ID.</param>
        /// <param name="agents">Removed agents.</param>
        public void RemoveAgentsFromSubtotals(IDbTransaction transaction,
            Guid subtotalId, IEnumerable<GXAgent> agents)
        {
            foreach (GXAgent it in agents)
            {
                _host.Connection.Delete(transaction,
                    GXDeleteArgs.Delete<GXSubtotalAgent>(w =>
                    w.SubtotalId == subtotalId &&
                    w.AgentId == it.Id));
            }
        }

        /// <summary>
        /// Map gateways with subtotal.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="subtotalId">Subtotal ID.</param>
        /// <param name="gateways">Joined gateways.</param>
        public void AddGatewaysToSubtotal(IDbTransaction transaction,
            Guid subtotalId, IEnumerable<GXGateway>? gateways)
        {
            if (gateways != null)
            {
                DateTime now = DateTime.Now;
                List<GXSubtotalGateway> list = new();
                foreach (GXGateway it in gateways)
                {
                    list.Add(new GXSubtotalGateway()
                    {
                        SubtotalId = subtotalId,
                        GatewayId = it.Id,
                        CreationTime = now
                    });
                }
                _host.Connection.Insert(transaction,
                    GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between gateways and subtotal.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="subtotalId">Subtotal ID.</param>
        /// <param name="gateways">Removed gateways.</param>
        public void RemoveGatewaysFromSubtotals(IDbTransaction transaction,
            Guid subtotalId, IEnumerable<GXGateway> gateways)
        {
            foreach (GXGateway it in gateways)
            {
                _host.Connection.Delete(transaction,
                    GXDeleteArgs.Delete<GXSubtotalGateway>(w =>
                    w.SubtotalId == subtotalId &&
                    w.GatewayId == it.Id));
            }
        }

        /// <summary>
        /// Map device attributes with subtotal.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="subtotalId">Subtotal ID.</param>
        /// <param name="attributes">Joined attributes.</param>
        public void AddDeviceAttributesToSubtotal(IDbTransaction transaction,
            Guid subtotalId, IEnumerable<GXAttributeTemplate>? attributes)
        {
            if (attributes != null)
            {
                DateTime now = DateTime.Now;
                List<GXSubtotalDeviceAttributeTemplate> list = new();
                foreach (GXAttributeTemplate it in attributes)
                {
                    list.Add(new GXSubtotalDeviceAttributeTemplate()
                    {
                        SubtotalId = subtotalId,
                        AttributeTemplateId = it.Id,
                        CreationTime = now
                    });
                }
                _host.Connection.Insert(transaction,
                    GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between device attribute and subtotal.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="subtotalId">Subtotal ID.</param>
        /// <param name="attributes">Removed attributes.</param>
        public void RemoveDeviceAttributesFromSubtotals(IDbTransaction transaction,
            Guid subtotalId, IEnumerable<GXAttributeTemplate> attributes)
        {
            foreach (GXAttributeTemplate it in attributes)
            {
                _host.Connection.Delete(transaction,
                    GXDeleteArgs.Delete<GXSubtotalDeviceAttributeTemplate>(w =>
                    w.SubtotalId == subtotalId &&
                    w.AttributeTemplateId == it.Id));
            }
        }

        /// <summary>
        /// Map device group attributes with subtotal.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="subtotalId">Subtotal ID.</param>
        /// <param name="attributes">Joined attributes.</param>
        public void AddDeviceGroupAttributesToSubtotal(IDbTransaction transaction,
            Guid subtotalId, IEnumerable<GXAttributeTemplate>? attributes)
        {
            if (attributes != null)
            {
                DateTime now = DateTime.Now;
                List<GXSubtotalDeviceGroupAttributeTemplate> list = new();
                foreach (GXAttributeTemplate it in attributes)
                {
                    list.Add(new GXSubtotalDeviceGroupAttributeTemplate()
                    {
                        SubtotalId = subtotalId,
                        AttributeTemplateId = it.Id,
                        CreationTime = now
                    });
                }
                _host.Connection.Insert(transaction,
                    GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between device group attribute and subtotal.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="subtotalId">Subtotal ID.</param>
        /// <param name="attributes">Removed attributes.</param>
        public void RemoveDeviceGroupAttributesFromSubtotals(IDbTransaction transaction,
            Guid subtotalId, IEnumerable<GXAttributeTemplate> attributes)
        {
            foreach (GXAttributeTemplate it in attributes)
            {
                _host.Connection.Delete(transaction,
                    GXDeleteArgs.Delete<GXSubtotalDeviceGroupAttributeTemplate>(w =>
                    w.SubtotalId == subtotalId &&
                    w.AttributeTemplateId == it.Id));
            }
        }

        /// <inheritdoc />
        public async Task CalculateAsync(ClaimsPrincipal user, IEnumerable<Guid>? subtotals)
        {
            if (subtotals != null)
            {
                Dictionary<GXSubtotal, List<string>> updates = new();
                ListSubtotals req = new ListSubtotals()
                {
                    Included = subtotals?.ToArray()
                };
                var list = await ListAsync(user, req, null, CancellationToken.None);
                //Check that subtotal is not calculated.
                foreach (var subtotal in list)
                {
                    if (subtotal.Status != SubtotalStatus.Calculate &&
                        subtotal.Status != SubtotalStatus.Clear)
                    {
                        subtotal.Status = SubtotalStatus.Calculate;
                        await UpdateAsync(user,
                            new GXSubtotal[] { subtotal },
                            c => c.Status);
                        _subTotal.Calculate(user, subtotals);
                        List<string> users = await GetUsersAsync(user, subtotal.Id);
                        updates[subtotal] = users;
                    }
                }
                foreach (var it in updates)
                {
                    //Don't notify if already calulated.
                    if (it.Key.Status == SubtotalStatus.Calculate)
                    {
                        GXSubtotal tmp = new GXSubtotal() { Id = it.Key.Id, Status = it.Key.Status };
                        await _eventsNotifier.SubtotalUpdate(it.Value,
                            new GXSubtotal[] { tmp });
                    }
                }
            }
        }

        /// <inheritdoc />
        public async Task ClearAsync(ClaimsPrincipal user, IEnumerable<Guid>? subtotals)
        {
            if (subtotals != null)
            {
                Dictionary<GXSubtotal, List<string>> updates = new();
                ListSubtotals req = new ListSubtotals()
                {
                    Included = subtotals?.ToArray()
                };
                var list = await ListAsync(user, req, null, CancellationToken.None);
                //Notify users
                foreach (var subtotal in list)
                {
                    subtotal.Calculated = null;
                    subtotal.Status = SubtotalStatus.Clear;
                    await UpdateAsync(user,
                        new GXSubtotal[] { subtotal },
                        c => new { c.Status, c.Calculated });
                    List<string> users = await GetUsersAsync(user, subtotal.Id);
                    updates[subtotal] = users;
                }
                foreach (var it in updates)
                {
                    if (it.Key.Status == SubtotalStatus.Clear)
                    {
                        GXSubtotal tmp = new GXSubtotal() { Id = it.Key.Id, Status = it.Key.Status };
                        await _eventsNotifier.SubtotalClear(it.Value,
                            new GXSubtotal[] { tmp });
                    }
                }
                using IDbTransaction transaction = _host.Connection.BeginTransaction();
                foreach (var subtotal in list)
                {
                    subtotal.Calculated = null;
                    subtotal.Status = SubtotalStatus.Clear;
                    await UpdateAsync(user,
                        new GXSubtotal[] { subtotal },
                        c => new { c.Status, c.Calculated });

                    await _host.Connection.DeleteAsync(transaction,
                        GXDeleteArgs.Delete<GXSubtotalValue>(w =>
                        w.Subtotal == subtotal));

                    subtotal.Status = SubtotalStatus.Idle;
                    await UpdateAsync(user,
                        new GXSubtotal[] { subtotal },
                        c => c.Status);
                }
                transaction.Commit();
                foreach (var it in updates)
                {
                    //Don't notify if already calulated.
                    if (it.Key.Status == SubtotalStatus.Idle)
                    {
                        GXSubtotal tmp = new GXSubtotal() { Id = it.Key.Id, Status = it.Key.Status };
                        await _eventsNotifier.SubtotalClear(it.Value,
                            new GXSubtotal[] { tmp });
                    }
                }
            }
        }

        /// <inheritdoc />
        public async Task CancelAsync(ClaimsPrincipal user, IEnumerable<Guid>? subtotals)
        {
            if (subtotals != null)
            {
                Dictionary<GXSubtotal, List<string>> updates = new();
                ListSubtotals req = new ListSubtotals()
                {
                    Included = subtotals?.ToArray()
                };
                var list = await ListAsync(user, req, null, CancellationToken.None);
                //Notify users
                foreach (var subtotal in list)
                {
                    if (subtotal.Status == SubtotalStatus.Calculate)
                    {
                        _subTotal.Cancel(user, subtotals);
                        subtotal.Status = SubtotalStatus.Idle;
                        await UpdateAsync(user,
                            new GXSubtotal[] { subtotal },
                            c => new { c.Status });
                        List<string> users = await GetUsersAsync(user, subtotal.Id);
                        updates[subtotal] = users;
                    }
                }
                foreach (var it in updates)
                {
                    //Don't notify if already cancelled.
                    if (it.Key.Status == SubtotalStatus.Cancel)
                    {
                        GXSubtotal tmp = new GXSubtotal() { Id = it.Key.Id, Status = it.Key.Status };
                        await _eventsNotifier.SubtotalClear(it.Value,
                            new GXSubtotal[] { tmp });
                    }
                }
            }
        }
    }
}
