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
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared.DTOs.Device;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class ObjectTemplateRepository : IObjectTemplateRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private GXPerformanceSettings _performanceSettings;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ObjectTemplateRepository(IGXHost host,
            IUserRepository userRepository,
            IGXEventsNotifier eventsNotifier,
            GXPerformanceSettings performanceSettings)
        {
            _host = host;
            _userRepository = userRepository;
            _eventsNotifier = eventsNotifier;
            _performanceSettings = performanceSettings;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal User, Guid? objectId)
        {
            GXSelectArgs args = GXQuery.GetUsersByObjectTemplate(ServerHelpers.GetUserId(User), objectId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal User, IEnumerable<Guid>? objectIds)
        {
            GXSelectArgs args = GXQuery.GetUsersByObjectTemplates(ServerHelpers.GetUserId(User), objectIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin }));
            }
            return ret;
        }


        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal User,
            IEnumerable<Guid> objects,
            bool delete)
        {
            if (User == null || (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.DeviceTemplateManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXObjectTemplate>(a => a.Id, q => objects.Contains(q.Id));
            List<GXObjectTemplate> list = _host.Connection.Select<GXObjectTemplate>(arg);
            DateTime now = DateTime.Now;
            foreach (GXObjectTemplate it in list)
            {
                it.Removed = now;
                List<string> users2 = await GetUsersAsync(User, it.Id);
                if (!users2.Any())
                {
                    //User doesn't have access rights for this object.
                    throw new UnauthorizedAccessException();
                }
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXObjectTemplate>(it.Id));
                }
                else
                {
                    _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
                }
            }
            List<string> users = await GetUsersAsync(User, objects);
            List<GXObjectTemplate> list2 = new List<GXObjectTemplate>();
            foreach (var it in list)
            {
                list2.Add(new GXObjectTemplate() { Id = it.Id });
            }
            if (_performanceSettings.Notify(TargetType.ObjectTemplate))
            {
                await _eventsNotifier.ObjectTemplateDelete(users, list2);
            }
        }

        /// <inheritdoc />
        public async Task<GXObjectTemplate[]> ListAsync(
            ClaimsPrincipal User,
            ListObjectTemplates? request,
            ListObjectTemplatesResponse? response,
            CancellationToken cancellationToken)
        {
            string userId = ServerHelpers.GetUserId(User);
            GXSelectArgs arg = GXQuery.GetObjectTemplatesByUser(userId, null);
            arg.Where.And<GXDeviceTemplate>(q => q.Removed == null);
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXObjectTemplate>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXObjectTemplate>(w => request.Included.Contains(w.Id));
                }
                if (request.DeviceTemplates != null && request.DeviceTemplates.Any())
                {
                    arg.Where.And<GXDeviceTemplate>(w => request.DeviceTemplates.Contains(w.Id));
                }
                if (request.ObjectTypes != null && request.ObjectTypes.Any())
                {
                    int?[] tmp = request.ObjectTypes.Cast<int?>().ToArray();
                    arg.Where.And<GXObjectTemplate>(w => tmp.Contains(w.ObjectType));
                }
                if (request.IgnoredObjectTypes != null && request.IgnoredObjectTypes.Any())
                {
                    int?[] tmp = request.IgnoredObjectTypes.Cast<int?>().ToArray();
                    arg.Where.And<GXObjectTemplate>(w => !tmp.Contains(w.ObjectType));
                }
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXObjectTemplate>(q => GXSql.DistinctCount(q.Id));
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
                arg.OrderBy.Add<GXObjectTemplate>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXObjectTemplate>(q => q.Id);
            }
            GXObjectTemplate[] objects = (await _host.Connection.SelectAsync<GXObjectTemplate>(arg)).ToArray();
            if (response != null)
            {
                response.ObjectTemplates = objects;
                if (response.Count == 0)
                {
                    response.Count = objects.Length;
                }
            }
            return objects;
        }

        /// <inheritdoc />
        public async Task<GXObjectTemplate> ReadAsync(
          ClaimsPrincipal User,
          Guid id)
        {
            string userId = ServerHelpers.GetUserId(User);
            GXSelectArgs arg = GXQuery.GetObjectTemplatesByUser(userId, id);
            //Device ID and name are added.
            arg.Columns.Add<GXDeviceTemplate>(c => new {c.Id, c.Name });
            arg.Columns.Exclude<GXDeviceTemplate>(e => e.Objects);
            arg.Distinct = true;
            GXObjectTemplate obj = await _host.Connection.SingleOrDefaultAsync<GXObjectTemplate>(arg);
            if (obj == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            //Get attribute templates .
            arg = GXSelectArgs.SelectAll<GXAttributeTemplate>(w => w.ObjectTemplate == obj && w.Removed == null);
            arg.Columns.Add<GXAttributeListItem>();
            arg.Joins.AddLeftJoin<GXAttributeTemplate, GXAttributeListItem>(x => x.Id, y => y.Template);
            arg.Columns.Exclude<GXAttributeTemplate>(e => e.ObjectTemplate);
            arg.Columns.Exclude<GXAttributeListItem>(e => e.Template); obj.Attributes = await _host.Connection.SelectAsync<GXAttributeTemplate>(arg);
            return obj;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal User,
            IEnumerable<GXObjectTemplate> objects,
            Expression<Func<GXObjectTemplate, object?>>? columns)
        {
            DateTime now = DateTime.Now;
            foreach (var obj in objects)
            {
                if (obj.Id == Guid.Empty)
                {
                    obj.CreationTime = now;
                    await _host.Connection.InsertAsync(GXInsertArgs.Insert(obj));
                }
                else
                {
                    if (columns != null)
                    {
                        await _host.Connection.UpdateAsync(GXUpdateArgs.Update(obj, columns));
                    }
                    //Update attribute data type, UI data type, scaler, access rights and expiration times.
                    if (obj.Attributes != null)
                    {
                        foreach (var att in obj.Attributes)
                        {
                            await _host.Connection.UpdateAsync(GXUpdateArgs.Update(att, u => new
                            {
                                u.DefaultValue,
                                u.DataType,
                                u.UIDataType,
                                u.Scaler,
                                u.Unit,
                                u.AccessLevel,
                                u.ExpirationTime
                            }));
                        }
                    }
                }
            }
            var ret = objects.Select(s => s.Id).ToArray();
            if (_performanceSettings.Notify(TargetType.ObjectTemplate))
            {
                List<string> users = await GetUsersAsync(User, ret);
                List<GXObjectTemplate> list = new List<GXObjectTemplate>();
                foreach (var it in objects)
                {
                    list.Add(new GXObjectTemplate()
                    {
                        Id = it.Id,
                    });
                }
                await _eventsNotifier.ObjectTemplateUpdate(users, list);
            }
            return ret.ToArray();
        }
    }
}
