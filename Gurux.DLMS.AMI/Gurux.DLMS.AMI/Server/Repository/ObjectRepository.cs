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

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class ObjectRepository : IObjectRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ObjectRepository(IGXHost host,
            IUserRepository userRepository,
            IGXEventsNotifier eventsNotifier)
        {
            _host = host;
            _userRepository = userRepository;
            _eventsNotifier = eventsNotifier;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal User, Guid? objectId)
        {
            GXSelectArgs args = GXQuery.GetUsersByObject(ServerHelpers.GetUserId(User), objectId);
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
            GXSelectArgs args = GXQuery.GetUsersByObjects(ServerHelpers.GetUserId(User), objectIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin }));
            }
            return ret;
        }


        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> objects)
        {
            if (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.DeviceTemplateManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXObject>(a => a.Id, q => objects.Contains(q.Id));
            List<GXObject> list = _host.Connection.Select<GXObject>(arg);
            DateTime now = DateTime.Now;
            foreach (GXObject it in list)
            {
                it.Removed = now;
                List<string> users2 = await GetUsersAsync(User, it.Id);
                if (!users2.Any())
                {
                    //User doesn't have access rights for this object.
                    throw new UnauthorizedAccessException();
                }
                _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
            }
            List<string> users = await GetUsersAsync(User, objects);
            List<GXObject> list2 = new List<GXObject>();
            foreach (var it in list)
            {
                list2.Add(new GXObject() { Id = it.Id });
            }
            await _eventsNotifier.ObjectDelete(users, list2);
        }

        /// <inheritdoc />
        public async Task<GXObject[]> ListAsync(
            ClaimsPrincipal User,
            ListObjects? request,
            ListObjectsResponse? response,
            CancellationToken cancellationToken)
        {
            string userId = ServerHelpers.GetUserId(User);
            GXSelectArgs arg = GXQuery.GetObjectsByUser(userId, null);
            arg.Columns.Add<GXObjectTemplate>();
            arg.Where.And<GXDeviceTemplate>(q => q.Removed == null);
            arg.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(j => j.Template, j => j.Id);
            arg.Joins.AddInnerJoin<GXDevice, GXDeviceTemplate>(j => j.Template, j => j.Id);
            if (request != null && request.Filter != null)
            {
                arg.Where.FilterBy(request.Filter);
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXObject>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            arg.Descending = true;
            arg.OrderBy.Add<GXObject>(q => q.Id);
            GXObject[] objects = (await _host.Connection.SelectAsync<GXObject>(arg)).ToArray();
            if (response != null)
            {
                response.Objects = objects;
                if (response.Count == 0)
                {
                    response.Count = objects.Length;
                }
            }
            return objects;
        }

        /// <inheritdoc />
        public async Task<GXObject> ReadAsync(
          ClaimsPrincipal User,
          Guid id)
        {
            string userId = ServerHelpers.GetUserId(User);
            GXSelectArgs arg = GXQuery.GetObjectsByUser(userId, id);
            arg.Columns.Add<GXObjectTemplate>();
            arg.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(x => x.Template, y => y.Id);
            arg.Distinct = true;
            GXObject obj = await _host.Connection.SingleOrDefaultAsync<GXObject>(arg);
            if (obj == null)
            {
                throw new ArgumentNullException(Properties.Resources.UnknownTarget);
            }
            //Get attributes .
            arg = GXSelectArgs.SelectAll<GXAttribute>(w => w.Object == obj && w.Removed == null);
            arg.Columns.Add<GXAttributeTemplate>();
            arg.Columns.Add<GXAttributeListItem>();
            arg.Joins.AddInnerJoin<GXObject, GXAttribute>(x => x.Id, y => y.Object);
            arg.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(x => x.Template, y => y.Id);
            arg.Joins.AddLeftJoin<GXAttributeTemplate, GXAttributeListItem>(x => x.Id, y => y.Template);
            arg.Columns.Exclude<GXAttributeTemplate>(e => e.ObjectTemplate);
            arg.Columns.Exclude<GXAttributeListItem>(e => e.Template);
            arg.OrderBy.Add<GXAttributeTemplate>(o => o.Index);
            obj.Attributes = await _host.Connection.SelectAsync<GXAttribute>(arg);
            //Get object parameters.
            arg = GXSelectArgs.SelectAll<GXObjectParameter>(w => w.Object == obj && w.Removed == null);
            arg.Columns.Add<GXModule>(s => s.Id);
            arg.Columns.Exclude<GXModule>(e => e.ObjectParameters);
            arg.Joins.AddInnerJoin<GXObjectParameter, GXModule>(j => j.Module, j => j.Id);
            obj.Parameters = await _host.Connection.SelectAsync<GXObjectParameter>(arg);
            return obj;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal User,
            IEnumerable<GXObject> objects)
        {
            DateTime now = DateTime.Now;
            foreach (var obj in objects)
            {
                if (obj.Id == Guid.Empty)
                {
                    obj.CreationTime = now;
                    await _host.Connection.InsertAsync(GXInsertArgs.Insert(obj));
                }
                {
                    //Update object parameters.
                    GXSelectArgs arg = GXSelectArgs.SelectAll<GXObjectParameter>(w => w.Object == obj && w.Removed == null);
                    var objectParameters = await _host.Connection.SelectAsync<GXObjectParameter>(arg);
                    var comparer = new UniqueComparer<GXObjectParameter, Guid>();
                    List<GXObjectParameter> removedObjectParameters = objectParameters.Except(obj.Parameters, comparer).ToList();
                    List<GXObjectParameter> addedObjectGroupParameters = obj.Parameters.Except(objectParameters, comparer).ToList();
                    List<GXObjectParameter> updatedObjectGroupParameters = obj.Parameters.Union(objectParameters, comparer).ToList();
                    if (removedObjectParameters.Any())
                    {
                        RemoveObjectParameters(obj, removedObjectParameters);
                    }
                    if (addedObjectGroupParameters.Any())
                    {
                        AddObjectParameters(obj, addedObjectGroupParameters);
                    }
                    if (updatedObjectGroupParameters.Any())
                    {
                        foreach (var it2 in updatedObjectGroupParameters)
                        {
                            GXUpdateArgs u = GXUpdateArgs.Update(it2, c => new { c.Settings, c.Updated });
                            await _host.Connection.UpdateAsync(u);
                        }
                    }
                }
            }
            var ret = objects.Select(s => s.Id);
            List<string> users = await GetUsersAsync(User, ret);
            List<GXObject> list = new List<GXObject>();
            foreach (var it in objects)
            {
                list.Add(new GXObject() { Id = it.Id });
            }
            await _eventsNotifier.ObjectUpdate(users, list);
            return ret.ToArray();
        }

        /// <summary>
        /// Add object parameters.
        /// </summary>
        /// <param name="obj">Object where parameters are added.</param>
        /// <param name="parameters">Added object parameters.</param>
        public void AddObjectParameters(GXObject obj, IEnumerable<GXObjectParameter> parameters)
        {
            DateTime now = DateTime.Now;
            foreach (GXObjectParameter it in parameters)
            {
                it.CreationTime = now;
                it.Object = obj;
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(parameters));
        }

        /// <summary>
        /// Remove object parameters from the object.
        /// </summary>
        /// <param name="obj">Object where parameters are removed.</param>
        /// <param name="parameters">Removed object parameters.</param>
        public void RemoveObjectParameters(GXObject obj, IEnumerable<GXObjectParameter> parameters)
        {
            DateTime now = DateTime.Now;
            foreach (GXObjectParameter it in parameters)
            {
                it.Removed = now;
                it.Object = obj;
            }
            _host.Connection.Delete(GXDeleteArgs.DeleteRange(parameters));
        }
    }
}
