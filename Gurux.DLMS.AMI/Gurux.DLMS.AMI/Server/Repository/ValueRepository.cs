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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class ValueRepository : IValueRepository
    {
        private readonly IGXHost _host;
        private readonly IUserRepository _userRepository;
        private readonly IGXEventsNotifier _eventsNotifier;


        /// <summary>
        /// Constructor.
        /// </summary>
        public ValueRepository(IGXHost host,
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
            IEnumerable<GXValue> values)
        {
            List<string> users = new List<string>();
            List<GXAttribute> updates = new List<GXAttribute>();
            GXUpdateArgs arg;
            DateTime now = DateTime.Now;
            foreach (GXValue it in values)
            {
                if (it.Attribute == null)
                {
                    throw new ArgumentException("Invalid attribute.");
                }
                if (it.Read == null)
                {
                    it.Read = now;
                }
                //Update attribute read time and last value.
                GXAttribute value = new GXAttribute();
                value.Id = it.Attribute.Id;
                value.Value = it.Value;
                value.Read = it.Read;
                arg = GXUpdateArgs.Update(value, q => new { q.Value, q.Read });
                _host.Connection.Update(arg);
                if (!users.Any())
                {
                    //Get attribute users only once.
                    users.AddRange(await GetUsersAsync(user, it.Attribute.Id));
                }

                //Read attribute template index and name.
                GXSelectArgs args = GXSelectArgs.Select<GXAttributeTemplate>(s => new { s.Index, s.Name });
                args.Joins.AddInnerJoin<GXAttributeTemplate, GXAttribute>(j => j.Id, j => j.Template);
                args.Where.And<GXAttribute>(w => w.Id == value.Id);
                value.Template = await _host.Connection.SingleOrDefaultAsync<GXAttributeTemplate>(args);
                //Read attribute object ID. It's send at the notify.
                args = GXSelectArgs.Select<GXObject>(s => s.Id);
                args.Joins.AddInnerJoin<GXAttribute, GXObject>(j => j.Object, j => j.Id);
                args.Where.And<GXAttribute>(w => w.Id == value.Id);
                value.Object = await _host.Connection.SingleOrDefaultAsync<GXObject>(args);
                // value.Object.Attributes.Add(value);
                updates.Add(value);
            }
            //Insert new values.
            _host.Connection.Insert(GXInsertArgs.InsertRange(values));
            List<Guid> list = new List<Guid>();
            foreach (GXValue it in values)
            {
                list.Add(it.Id);
            }
            await _eventsNotifier.ValueUpdate(users, updates);
            return list.ToArray();
        }

        /// <inheritdoc />
        public Task ClearAttributeAsync(ClaimsPrincipal User, IEnumerable<GXAttribute> attributes)
        {
            //TODO:
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task ClearDeviceAsync(ClaimsPrincipal User, IEnumerable<GXDevice> objects)
        {
            //TODO:
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task ClearObjectAsync(ClaimsPrincipal User, IEnumerable<GXObject> objects)
        {
            //TODO:
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> objects)
        {
            //TODO:
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public async Task<GXValue[]> ListAsync(ClaimsPrincipal User,
            ListValues? request,
            ListValuesResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXValue>();
            arg.Distinct = true;
            if (request != null && request.Filter != null)
            {
                if (request.Filter.Attribute != null)
                {
                    if (request.Filter.Attribute.Object != null)
                    {
                        if (request.Filter.Attribute.Object.Device != null)
                        {
                            //Filter by device.
                            arg.Joins.AddInnerJoin<GXValue, GXAttribute>(j => j.Attribute, j => j.Id);
                            arg.Joins.AddInnerJoin<GXAttribute, GXObject>(j => j.Object, j => j.Id);
                            arg.Joins.AddInnerJoin<GXObject, GXDevice>(j => j.Device, j => j.Id);
                        }
                        else
                        {
                            //Filter by object.
                            arg.Joins.AddInnerJoin<GXValue, GXAttribute>(j => j.Attribute, j => j.Id);
                            arg.Joins.AddInnerJoin<GXAttribute, GXObject>(j => j.Object, j => j.Id);
                        }
                    }
                    else
                    {
                        //Filter by attribute.
                        arg.Joins.AddInnerJoin<GXValue, GXAttribute>(j => j.Attribute, j => j.Id);
                    }
                }
                arg.Where.FilterBy(request.Filter);
            }
            arg.OrderBy.Add<GXValue>(o => o.Read);
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXValue>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            GXValue[] values = (await _host.Connection.SelectAsync<GXValue>(arg)).ToArray();
            if (response != null)
            {
                response.Values = values;
            }
            return values;
        }

        /// <inheritdoc />
        public Task<GXValue> ReadAsync(ClaimsPrincipal User, Guid id)
        {
            //TODO:
            throw new NotImplementedException();
        }
    }
}
