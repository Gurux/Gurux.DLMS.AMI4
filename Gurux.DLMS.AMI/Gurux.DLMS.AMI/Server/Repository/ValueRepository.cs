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
using Org.BouncyCastle.Asn1.Cms;
using System.Linq;
using System;
using Gurux.DLMS.AMI.Client.Pages.Objects;

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
            GXSelectArgs args;
            List<GXObject>? objects = new List<GXObject>();

            foreach (GXValue it in values)
            {
                if (it.Attribute == null)
                {
                    throw new ArgumentException("Invalid attribute.");
                }
                //Read time is set for profle generic objects.
                if (it.Read == null)
                {
                    it.Read = now;
                }
                else
                {
                    now = it.Read.Value.LocalDateTime;
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
                args = GXSelectArgs.Select<GXAttributeTemplate>(s => new { s.Index, s.Name });
                args.Joins.AddInnerJoin<GXAttributeTemplate, GXAttribute>(j => j.Id, j => j.Template);
                args.Where.And<GXAttribute>(w => w.Id == value.Id);
                value.Template = await _host.Connection.SingleOrDefaultAsync<GXAttributeTemplate>(args);
                //Read attribute object ID. It's send at the notify.
                args = GXSelectArgs.Select<GXObject>(s => new { s.Id, s.Template });
                args.Columns.Add<GXObjectTemplate>(s => new { s.Id, s.LogicalName, s.Name, s.ObjectType });
                args.Joins.AddInnerJoin<GXObject, GXAttribute>(j => j.Id, j => j.Object);
                args.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(j => j.Template, j => j.Id);
                args.Where.And<GXAttribute>(w => w.Id == value.Id);
                value.Object = await _host.Connection.SingleOrDefaultAsync<GXObject>(args);
                objects.Add(value.Object);
                updates.Add(value);
                //Update read time for the object.
                value.Object.LastRead = it.Read;
                arg = GXUpdateArgs.Update(value.Object, q => new { q.LastRead });
                await _host.Connection.UpdateAsync(arg);
                //Don't do anything If empty row is added for the profile generic.
                //Agent sends empty row if it's not able to read new rows from the meter.
                if (it.Value == null && value.Template.Index == 2 && value.Object != null && value.Object.Template != null &&
                    value.Object.Template.ObjectType == (int)Enums.ObjectType.ProfileGeneric)
                {
                    arg = GXUpdateArgs.Update(value, q => new { q.Read });
                    await _host.Connection.UpdateAsync(arg);
                    return new Guid[0];
                }
            }
            //Insert new values.
            _host.Connection.Insert(GXInsertArgs.InsertRange(values));
            List<Guid> list = new List<Guid>();
            foreach (GXValue it in values)
            {
                list.Add(it.Id);
            }
            //Update last read time for the device.
            List<GXDevice>? devices = null;
            if (objects.Any())
            {
                var ids = objects.Select(s => s.Id).ToArray();
                if (ids.Any())
                {
                    args = GXSelectArgs.Select<GXDevice>(s => new { s.Id, s.Name });
                    args.Joins.AddInnerJoin<GXDevice, GXObject>(j => j.Id, j => j.Device);
                    args.Where.And<GXObject>(w => ids.Contains(w.Id));
                    args.Distinct = true;
                    devices = await _host.Connection.SelectAsync<GXDevice>(args);
                    foreach (var it in devices)
                    {
                        it.LastRead = now;
                        arg = GXUpdateArgs.Update(it, q => new { q.LastRead });
                        await _host.Connection.UpdateAsync(arg);
                    }
                }
            }
            await _eventsNotifier.ValueUpdate(users, updates);
            if (objects != null)
            {
                await _eventsNotifier.ObjectUpdate(users, objects);
            }
            if (devices != null)
            {
                await _eventsNotifier.DeviceUpdate(users, devices);
            }
            return list.ToArray();
        }

        /// <inheritdoc />
        public async Task ClearAttributeAsync(ClaimsPrincipal User, IEnumerable<GXAttribute> attributes)
        {
            List<string>? users = null;
            foreach (var it in attributes)
            {
                if (users == null)
                {
                    users = await GetUsersAsync(User, it.Id);
                }
                await _host.Connection.DeleteAsync(GXDeleteArgs.Delete<GXValue>(w => w.Attribute == it));
                //Reset last read time.
                it.Read = null;
                await _host.Connection.UpdateAsync(GXUpdateArgs.Update(it, c => c.Read));
            }
            if (users != null)
            {
                await _eventsNotifier.AttributeUpdate(users, attributes);
            }
        }

        /// <inheritdoc />
        public async Task ClearDeviceAsync(ClaimsPrincipal User, IEnumerable<GXDevice> devices)
        {
            List<string>? users = null;
            Guid[] guids = devices.Select(s => s.Id).ToArray();
            //Reset last read time for the attribute.
            GXSelectArgs args = GXSelectArgs.Select<GXAttribute>(s => s.Id);
            args.Joins.AddInnerJoin<GXAttribute, GXObject>(j => j.Object, j => j.Id);
            args.Joins.AddInnerJoin<GXObject, GXDevice>(j => j.Device, j => j.Id);
            args.Where.And<GXDevice>(w => GXSql.In(w.Id, guids));
            List<GXAttribute> attributes = (await _host.Connection.SelectAsync<GXAttribute>(args));
            foreach (var it in attributes)
            {
                if (users == null)
                {
                    users = await GetUsersAsync(User, it.Id);
                }
                await _host.Connection.DeleteAsync(GXDeleteArgs.Delete<GXValue>(w => w.Attribute == it));
                it.Read = null;
                await _host.Connection.UpdateAsync(GXUpdateArgs.Update(it, c => c.Read));
            }
            //Reset last read time for the object.
            args = GXSelectArgs.Select<GXObject>(s => s.Id);
            args.Joins.AddInnerJoin<GXObject, GXDevice>(j => j.Device, j => j.Id);
            args.Where.And<GXDevice>(w => GXSql.In(w.Id, guids));
            List<GXObject> objects = (await _host.Connection.SelectAsync<GXObject>(args));
            foreach (var it in objects)
            {
                it.LastRead = null;
                await _host.Connection.UpdateAsync(GXUpdateArgs.Update(it, c => c.LastRead));
            }
            //Reset last read time for the device.
            foreach (var it in devices)
            {
                it.LastRead = null;
                await _host.Connection.UpdateAsync(GXUpdateArgs.Update(it, c => c.LastRead));
            }
            if (users != null)
            {
                await _eventsNotifier.AttributeUpdate(users, attributes);
            }
        }

        /// <inheritdoc />
        public async Task ClearObjectAsync(ClaimsPrincipal User, IEnumerable<GXObject> objects)
        {
            if (objects != null && objects.Any())
            {
                List<string>? users = null;
                Guid[] guids = objects.Select(s => s.Id).ToArray();
                GXSelectArgs args = GXSelectArgs.Select<GXAttribute>(s => s.Id);
                args.Joins.AddInnerJoin<GXAttribute, GXObject>(j => j.Object, j => j.Id);
                args.Where.And<GXObject>(w => GXSql.In(w.Id, guids));
                List<GXAttribute> attributes = (await _host.Connection.SelectAsync<GXAttribute>(args));
                foreach (var it in attributes)
                {
                    if (users == null)
                    {
                        users = await GetUsersAsync(User, it.Id);
                    }
                    await _host.Connection.DeleteAsync(GXDeleteArgs.Delete<GXValue>(w => w.Attribute == it));
                    //Reset last read time for the attribute.
                    it.Read = null;
                    await _host.Connection.UpdateAsync(GXUpdateArgs.Update(it, c => c.Read));
                }
                foreach (var it in objects)
                {
                    //Reset last read time for the object.
                    it.LastRead = null;
                    await _host.Connection.UpdateAsync(GXUpdateArgs.Update(it, c => c.LastRead));
                }
                if (users != null)
                {
                    await _eventsNotifier.AttributeUpdate(users, attributes);
                }
            }
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> values)
        {
            if (values != null && values.Any())
            {
                Guid[] guids = values.ToArray();
                List<string>? users = null;
                GXSelectArgs args = GXSelectArgs.Select<GXAttribute>(s => s.Id);
                args.Joins.AddInnerJoin<GXValue, GXAttribute>(j => j.Attribute, j => j.Id);
                args.Joins.AddInnerJoin<GXAttribute, GXObject>(j => j.Object, j => j.Id);
                args.Where.And<GXValue>(w => GXSql.In(w.Id, guids));
                List<GXAttribute> attributes = (await _host.Connection.SelectAsync<GXAttribute>(args));
                foreach (var it in values)
                {
                    if (users == null)
                    {
                        users = await GetUsersAsync(User, it);
                    }
                    await _host.Connection.DeleteAsync(GXDeleteArgs.Delete<GXValue>(w => w.Id == it));
                }
                if (users != null)
                {
                    _eventsNotifier.AttributeUpdate(users, attributes);
                }
            }
        }

        /// <inheritdoc />
        public async Task<GXValue[]> ListAsync(ClaimsPrincipal User,
            ListValues? request,
            ListValuesResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXValue>();
            arg.Distinct = true;
            if (request != null)
            {
                arg.Descending = request.Descending;
            }
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
                if (response.Count == 0)
                {
                    response.Count = values.Length;
                }

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
