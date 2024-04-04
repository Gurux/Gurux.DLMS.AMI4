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
using Gurux.DLMS.AMI.Shared.DIs;
using System.Security.Claims;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Subtotal;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class SubtotalValueRepository : ISubtotalValueRepository
    {
        private readonly IGXHost _host;
        private readonly IUserRepository _userRepository;
        private readonly IGXEventsNotifier _eventsNotifier;


        /// <summary>
        /// Constructor.
        /// </summary>
        public SubtotalValueRepository(IGXHost host,
            IUserRepository userRepository,
            IGXEventsNotifier eventsNotifier)
        {
            _host = host;
            _userRepository = userRepository;
            _eventsNotifier = eventsNotifier;
        }

        /// <inheritdoc />
        private async Task<List<string>> GetUsersAsync(
            ClaimsPrincipal user,
            Guid? attributeId)
        {
            GXSelectArgs args = GXQuery.GetUsersByAttribute(attributeId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (user != null && user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<Guid[]> AddAsync(
            ClaimsPrincipal user,
            IEnumerable<GXSubtotalValue> values)
        {
            List<string> users = new List<string>();
            foreach (GXSubtotalValue it in values)
            {
                if (it.Subtotal == null)
                {
                    throw new ArgumentException("Invalid subtotal.");
                }
                if (it.StartTime == null)
                {
                    throw new ArgumentException("Invalid subtotal start time.");
                }
                if (it.EndTime == null)
                {
                    throw new ArgumentException("Invalid subtotal end time.");
                }
            }
            //Insert new values.
            var tmp = values.Where(w => w.Id == Guid.Empty).ToList();
            if (tmp.Any())
            {
                _host.Connection.Insert(GXInsertArgs.InsertRange(tmp));
            }
            //Update values.
            tmp = values.Where(w => w.Id != Guid.Empty).ToList();
            if (tmp.Any())
            {
                GXUpdateArgs u = GXUpdateArgs.UpdateRange(tmp, u => u.Value);
                _host.Connection.Update(u);
            }
            List<Guid> list = new List<Guid>();
            await _eventsNotifier.SubtotalValueUpdate(users, values);
            return list.ToArray();
        }

        /// <inheritdoc />
        public async Task<GXSubtotalValue[]> ListAsync(ClaimsPrincipal User,
            ListSubtotalValues? request,
            ListSubtotalValuesResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXSubtotalValue>();
            arg.Distinct = true;
            if (request?.Exclude != null && request.Exclude.Any())
            {
                arg.Where.And<GXSubtotalValue>(w => !request.Exclude.Contains(w.Id));
            }
            if (request?.Included != null && request.Included.Any())
            {
                arg.Where.And<GXSubtotalValue>(w => request.Included.Contains(w.Id));
            }
            if (request != null && request.Filter != null)
            {
                if (request.Filter.Subtotal != null)
                {
                    arg.Columns.Add<GXSubtotal>();
                    arg.Joins.AddInnerJoin<GXSubtotalValue, GXSubtotal>(j => j.Subtotal, j => j.Id);
                    if (request.Filter.Subtotal.DeviceGroups != null && request.Filter.Subtotal.DeviceGroups.Any())
                    {
                        arg.Columns.Add<GXDeviceGroup>(s => new { s.Id, s.Name });
                        arg.Joins.AddInnerJoin<GXSubtotal, GXSubtotalDeviceGroup>(j => j.Id, j => j.SubtotalId);
                        arg.Joins.AddInnerJoin<GXSubtotalDeviceGroup, GXDeviceGroup>(j => j.DeviceGroupId, j => j.Id);
                        var names = request.Filter.Subtotal.DeviceGroups.Where(w => !string.IsNullOrEmpty(w.Name)).Select(s => s.Name.ToLower()).ToList();
                        if (names.Any())
                        {
                            arg.Where.And<GXDeviceGroup>(w => names.Contains(w.Name));
                        }
                    }
                    if (request.Filter.Subtotal.Devices != null && request.Filter.Subtotal.Devices.Any())
                    {
                        arg.Columns.Add<GXDevice>(s => new { s.Id, s.Name });
                        arg.Joins.AddInnerJoin<GXSubtotal, GXSubtotalDevice>(j => j.Id, j => j.SubtotalId);
                        arg.Joins.AddInnerJoin<GXSubtotalDevice, GXDevice>(j => j.DeviceId, j => j.Id);
                        var names = request.Filter.Subtotal.Devices.Where(w => !string.IsNullOrEmpty(w.Name)).Select(s => s.Name.ToLower()).ToList();
                        if (names.Any())
                        {
                            arg.Where.And<GXDevice>(w => names.Contains(w.Name));
                        }
                    }
                    if (request.Filter.Subtotal.AgentGroups != null && request.Filter.Subtotal.AgentGroups.Any())
                    {
                        arg.Columns.Add<GXAgentGroup>(s => new { s.Id, s.Name });
                        arg.Joins.AddInnerJoin<GXSubtotal, GXSubtotalAgentGroup>(j => j.Id, j => j.SubtotalId);
                        arg.Joins.AddInnerJoin<GXSubtotalAgentGroup, GXAgentGroup>(j => j.AgentGroupId, j => j.Id);
                        var names = request.Filter.Subtotal.AgentGroups.Where(w => !string.IsNullOrEmpty(w.Name)).Select(s => s.Name.ToLower()).ToList();
                        if (names.Any())
                        {
                            arg.Where.And<GXAgentGroup>(w => names.Contains(w.Name));
                        }
                    }
                    if (request.Filter.Subtotal.Agents != null && request.Filter.Subtotal.Agents.Any())
                    {
                        arg.Columns.Add<GXAgent>(s => new { s.Id, s.Name });
                        arg.Joins.AddInnerJoin<GXSubtotal, GXSubtotalAgent>(j => j.Id, j => j.SubtotalId);
                        arg.Joins.AddInnerJoin<GXSubtotalAgent, GXAgent>(j => j.AgentId, j => j.Id);
                        var names = request.Filter.Subtotal.Agents.Where(w => !string.IsNullOrEmpty(w.Name)).Select(s => s.Name.ToLower()).ToList();
                        if (names.Any())
                        {
                            arg.Where.And<GXAgent>(w => names.Contains(w.Name));
                        }
                    }
                    if (request.Filter.Subtotal.GatewayGroups != null && request.Filter.Subtotal.GatewayGroups.Any())
                    {
                        arg.Columns.Add<GXGatewayGroup>(s => new { s.Id, s.Name });
                        arg.Joins.AddInnerJoin<GXSubtotal, GXSubtotalGatewayGroup>(j => j.Id, j => j.SubtotalId);
                        arg.Joins.AddInnerJoin<GXSubtotalGatewayGroup, GXGatewayGroup>(j => j.GatewayGroupId, j => j.Id);
                        var names = request.Filter.Subtotal.GatewayGroups.Where(w => !string.IsNullOrEmpty(w.Name)).Select(s => s.Name.ToLower()).ToList();
                        if (names.Any())
                        {
                            arg.Where.And<GXGatewayGroup>(w => names.Contains(w.Name));
                        }
                    }
                    if (request.Filter.Subtotal.Gateways != null && request.Filter.Subtotal.Gateways.Any())
                    {
                        arg.Columns.Add<GXGateway>(s => new { s.Id, s.Name });
                        arg.Joins.AddInnerJoin<GXSubtotal, GXSubtotalGateway>(j => j.Id, j => j.SubtotalId);
                        arg.Joins.AddInnerJoin<GXSubtotalGateway, GXGateway>(j => j.GatewayId, j => j.Id);
                        var names = request.Filter.Subtotal.Gateways.Where(w => !string.IsNullOrEmpty(w.Name)).Select(s => s.Name.ToLower()).ToList();
                        if (names.Any())
                        {
                            arg.Where.And<GXGateway>(w => names.Contains(w.Name));
                        }
                    }
                    arg.Where.FilterBy(request.Filter.Subtotal);
                    request.Filter.Subtotal = null;
                }
                arg.Where.FilterBy(request.Filter);
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXSubtotalValue>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXSubtotalValue>(o => o.StartTime);
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXSubtotalValue>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            GXSubtotalValue[] values = (await _host.Connection.SelectAsync<GXSubtotalValue>(arg)).ToArray();
            foreach (var it in values)
            {
                if (it.Subtotal != null)
                {
                    it.Subtotal.Values = null;
                }
            }
            if (response != null)
            {
                response.SubtotalValues = values;
                if (response.Count == 0)
                {
                    response.Count = values.Length;
                }

            }
            return values;
        }

        /// <inheritdoc />
        public Task<GXSubtotalValue> ReadAsync(ClaimsPrincipal User, Guid id)
        {
            //TODO:
            throw new NotImplementedException();
        }
    }
}
