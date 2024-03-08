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
using System.Data;
using System.Linq;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.User;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class ObjectRepository : IObjectRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private GXPerformanceSettings _performanceSettings;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ObjectRepository(IGXHost host,
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
        public async Task DeleteAsync(ClaimsPrincipal User,
            IEnumerable<Guid> objects,
            bool delete)
        {
            if (User == null || (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.DeviceTemplateManager)))
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
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXObject>(it.Id));
                }
                else
                {
                    _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
                }
            }
            List<string> users = await GetUsersAsync(User, objects);
            List<GXObject> list2 = new List<GXObject>();
            foreach (var it in list)
            {
                list2.Add(new GXObject() { Id = it.Id });
            }
            if (_performanceSettings.Notify(TargetType.Object))
            {
                await _eventsNotifier.ObjectDelete(users, list2);
            }
        }

        /// <inheritdoc />
        public async Task<GXObject[]> ListAsync(
            ClaimsPrincipal User,
            ListObjects? request,
            ListObjectsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            GXObjectTemplate? objectTemplate = null;
            bool allUsers = request != null && request.AllUsers && User.IsInRole(GXRoles.Admin);
            string userId = ServerHelpers.GetUserId(User);
            if (allUsers)
            {
                //Admin can see all the devices.
                arg = GXSelectArgs.SelectAll<GXDevice>();
            }
            else
            {
                arg = GXQuery.GetDevicesByUser(userId, true, null);
            }
            arg.Joins.AddLeftJoin<GXDevice, GXObject>(j => j.Id, j => j.Device);
            arg.Joins.AddLeftJoin<GXObject, GXObjectTemplate>(j => j.Template, j => j.Id);
            arg.Joins.AddInnerJoin<GXDevice, GXDeviceTemplate>(j => j.Template, j => j.Id);
            arg.Columns.Clear();
            arg.Columns.Add<GXDevice>();
            arg.Columns.Exclude<GXDevice>(e => new { e.Creator, e.CreationTime });
            arg.Columns.Add<GXObject>();
            arg.Columns.Add<GXObjectTemplate>();
            arg.Columns.Add<GXDeviceTemplate>(s => s.Id);
            arg.Columns.Exclude<GXDeviceTemplate>(e => new { e.Objects });
            arg.Where.And<GXDeviceTemplate>(q => q.Removed == null);
            if (request != null)
            {
                if (request.Devices != null && request.Devices.Any())
                {
                    arg.Where.And<GXDevice>(w => request.Devices.Contains(w.Id));
                }

                if (request.Select != null && request.Select.Contains("DeviceTemplate"))
                {
                }
                else if (request.Select != null && request.Select.Contains("Device"))
                {
                }
                if (request.Filter?.Device != null)
                {
                    var dg = request.Filter.Device.DeviceGroups?.FirstOrDefault();
                    if (dg != null)
                    {
                        var ug = dg.UserGroups?.FirstOrDefault();
                        if (ug != null)
                        {
                            var user = ug.Users?.FirstOrDefault();
                            if (user != null)
                            {
                                if (allUsers)
                                {
                                    arg.Joins.AddLeftJoin<GXDevice, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceId);
                                }
                                arg.Joins.AddLeftJoin<GXDeviceGroupDevice, GXDeviceGroup>(j => j.DeviceGroupId, j => j.Id);
                                arg.Joins.AddLeftJoin<GXDeviceGroup, GXUserGroupDeviceGroup>(j => j.Id, j => j.DeviceGroupId);
                                arg.Joins.AddLeftJoin<GXUserGroupDeviceGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                                arg.Joins.AddLeftJoin<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId);
                                arg.Joins.AddLeftJoin<GXUserGroupUser, GXUser>(j => j.UserId, j => j.Id);
                                user.UserGroups = null;
                                arg.Where.FilterBy(user);
                            }
                        }
                    }
                    else if (request.Filter.Device.Id is Guid id && id != Guid.Empty)
                    {
                        arg.Where.And<GXDevice>(w => w.Id == id);
                    }
                    if (request.Filter.Device.Name is string dn)
                    {
                        arg.Where.And<GXDevice>(w => w.Name.Contains(dn));
                    }
                    if (request.Filter.Device?.Template?.Name is string tn)
                    {
                        arg.Where.And<GXDeviceTemplate>(w => w.Name.Contains(tn));
                    }
                    request.Filter.Device = null;
                }
                if (request.Filter?.Template != null)
                {
                    objectTemplate = request.Filter.Template;
                    request.Filter.Template = null;
                }
                arg.Where.FilterBy(request.Filter);
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
            //Get devices before template filter or device is not retured if all objects are late binded.
            GXDevice[] devs = (await _host.Connection.SelectAsync<GXDevice>(arg)).ToArray();
            if (objectTemplate != null)
            {
                arg.Where.FilterBy(objectTemplate);
            }
            UInt32 count2 = 0;
            if (request?.Exclude != null && request.Exclude.Any())
            {
                arg.Where.And<GXObject>(w => !request.Exclude.Contains(w.Id));
            }
            if (request?.Included != null && request.Included.Any())
            {
                arg.Where.And<GXObject>(w => request.Included.Contains(w.Id));
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
                    count2 = (UInt32)response.Count;
                }
                if (count2 < (UInt32)request.Index)
                {
                    //Handle index for late bind objects.
                    arg.Index = count2;
                    request.Index -= count2;
                }
                else
                {
                    arg.Index = (UInt32)request.Index;
                }
                arg.Count = (UInt32)request.Count;
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXObject>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXObject>(q => q.Id);
            }
            List<GXObject> objects = (await _host.Connection.SelectAsync<GXObject>(arg)).ToList();
            var deviceTemplateIds = devs.Select(s => s.Template.Id).ToArray();

            arg = GXSelectArgs.SelectAll<GXDeviceTemplate>();
            arg.Columns.Add<GXObjectTemplate>();
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXDeviceTemplate, GXObjectTemplate>(j => j.Id, j => j.DeviceTemplate);
            arg.Joins.AddInnerJoin<GXObjectTemplate, GXAttributeTemplate>(j => j.Id, j => j.ObjectTemplate);
            arg.Where.And<GXDeviceTemplate>(w => deviceTemplateIds.Contains(w.Id));
            if (objectTemplate != null)
            {
                arg.Where.FilterBy(objectTemplate);
            }
            GXDeviceTemplate[] deviceTemplates = (await _host.Connection.SelectAsync<GXDeviceTemplate>(arg)).ToArray();
            int count = 0;
            int index = 0;
            if (request != null && (int)request.Count != 0)
            {
                count = (int)request.Count;
                if (request?.Exclude != null && request.Exclude.Any())
                {
                    count += request.Exclude.Length;
                }
                index = (int)request.Index;
            }
            int objectCount = 0;
            bool lateBinded = false;
            if (deviceTemplates.Any())
            {
                foreach (var dev in devs)
                {
                    GXDeviceTemplate devTemplate = deviceTemplates.Where(w => w.Id == dev.Template.Id).Single();
                    //Remove device objects that are not in device template.
                    if (dev.Objects != null)
                    {
                        var tmp = devTemplate.Objects.Select(s => s.Id);
                        var tmp2 = dev.Objects.Select(s => s.Template.Id);
                        List<GXObject> remove = new List<GXObject>();
                        foreach (var it in dev.Objects)
                        {
                            if (!tmp.Contains(it.Template.Id))
                            {
                                remove.Add(it);
                            }
                        }
                        dev.Objects.RemoveAll(w => remove.Contains(w));
                    }
                    objectCount += devTemplate.Objects.Count;
                    if (dev.Objects == null || dev.Objects.Count != devTemplate.Objects.Count)
                    {
                        //Get late bind objects.
                        foreach (var it in devTemplate.Objects)
                        {
                            it.DeviceTemplate = null;
                            if (count != 0 && index + count == objects.Count)
                            {
                                break;
                            }
                            if (!objects.Where(w => w.Template != null && w.Template.Id == it.Id &&
                            w.Device != null && w.Device.Id == dev.Id).Any())
                            {
                                lateBinded = true;
                                objects.Add(new GXObject(it)
                                {
                                    //Template ID is used as a ID for late bind objects.
                                    Id = it.Id,
                                    Latebind = true,
                                    Device = new GXDevice() { Id = dev.Id, Name = dev.Name },
                                });
                            }
                        }
                    }
                    else
                    {
                        int removed = dev.Objects.Count;
                        if (request?.Exclude != null && request.Exclude.Any())
                        {
                            objects.RemoveAll(w => request.Exclude.Contains(w.Id));
                            removed = dev.Objects.RemoveAll(w => request.Exclude.Contains(w.Id));
                            objects.AddRange(dev.Objects);
                        }
                        count -= removed;
                    }
                }
            }
            if (lateBinded)
            {
                if (response != null)
                {
                    response.Count = objectCount;
                    if (request?.Exclude != null && request.Exclude.Any())
                    {
                        response.Count -= request.Exclude.Count();
                    }
                }
            }
            if (request?.Exclude != null && request.Exclude.Any())
            {
                //Remove excluded objects.
                objects.RemoveAll(w => request.Exclude.Contains(w.Id));
            }
            if (request != null && request.Count != 0 && (int)request.Count < objects.Count)
            {
                //Remove extra objects.
                objects = objects.Skip((int)request.Index).Take((int)request.Count).ToList();
            }
            //Only device id and name are send.
            foreach (var obj in objects)
            {
                if (obj.Device != null)
                {
                    obj.Device = new GXDevice() { Id = obj.Device.Id, Name = obj.Device.Name };
                }
            }
            if (request?.Select != null && request.Select.Contains("Attribute"))
            {
                arg = GXSelectArgs.Select<GXAttribute>(s => new { s.Id, s.Template, s.Value });
                arg.Columns.Add<GXAttributeTemplate>(s => new { s.Id, s.Name });
                arg.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(j => j.Template, j => j.Id);
                foreach (var it in objects)
                {
                    arg.Where.Clear();
                    arg.Where.And<GXAttribute>(w => w.Object == it);
                    it.Attributes = (await _host.Connection.SelectAsync<GXAttribute>(arg)).ToList();
                }
            }

            if (response != null)
            {
                response.Objects = objects.ToArray();
                if (response.Count == 0 || response.Count < objects.Count)
                {
                    response.Count = objects.Count;
                }
            }
            return objects.ToArray();
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
                //If late binding.
                arg = GXQuery.GetObjectTemplatesByUser(userId, id);
                arg.Distinct = true;
                GXObjectTemplate template = await _host.Connection.SingleOrDefaultAsync<GXObjectTemplate>(arg);
                if (template == null)
                {
                    throw new ArgumentException(Properties.Resources.UnknownTarget);
                }
                obj = new GXObject(template)
                {
                    Id = template.Id,
                    Latebind = true
                };
                //Get attributes.
                arg = GXSelectArgs.SelectAll<GXAttributeTemplate>(w => w.ObjectTemplate == template && w.Removed == null);
                arg.Columns.Add<GXAttributeListItem>();
                arg.Joins.AddInnerJoin<GXObjectTemplate, GXAttributeTemplate>(x => x.Id, y => y.ObjectTemplate);
                arg.Joins.AddLeftJoin<GXAttributeTemplate, GXAttributeListItem>(x => x.Id, y => y.Template);
                arg.Columns.Exclude<GXAttributeTemplate>(e => e.ObjectTemplate);
                arg.Columns.Exclude<GXAttributeListItem>(e => e.Template);
                arg.OrderBy.Add<GXAttributeTemplate>(o => o.Index);
                var templates = await _host.Connection.SelectAsync<GXAttributeTemplate>(arg);
                foreach (var it in templates)
                {
                    obj.Attributes.Add(new GXAttribute(it)
                    {
                        Id = it.Id,
                        Value = it.DefaultValue,
                    });
                }
            }
            else
            {
                //Get attributes.
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
            }
            return obj;
        }

        /// <summary>
        /// Return device object using device ID and logical name of the object.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="userId"></param>
        /// <param name="device"></param>
        /// <param name="logicalName"></param>
        /// <returns></returns>
        internal static async Task<GXObject> GetDeviceObjectUsingLogicalName(
            IGXHost host,
            IDbTransaction? transaction,
            string userId,
            GXDevice device,
            string logicalName)
        {
            Guid deviceId = device.Id;
            GXSelectArgs args = GXQuery.GetObjectsByUser(userId);
            args.Columns.Add<GXObjectTemplate>();
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(o => o.Template, a => a.Id);
            args.Where.And<GXDevice>(q => q.Id == deviceId);
            args.Where.And<GXObject>(q => q.Removed == null);
            args.Where.And<GXObjectTemplate>(q => q.Removed == null && q.LogicalName == logicalName);
            return await host.Connection.SingleOrDefaultAsync<GXObject>(transaction, args);
        }

        /// <summary>
        /// Return device object using device ID and logical name of the object.
        /// </summary>
        /// <param name="host">Host.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="userId">User ID.</param>
        /// <param name="deviceId">Device ID.</param>
        /// <param name="logicalName">Logical name of the object.</param>
        /// <param name="index">Attribute index</param>
        /// <returns></returns>
        internal static async Task<GXAttribute?> GetDeviceAttributeUsingLogicalName(
            IGXHost host,
            IDbTransaction? transaction,
            string userId,
            Guid deviceId,
            string logicalName,
            int index)
        {
            GXSelectArgs args = GXQuery.GetAttributesByUser(userId);
            args.Columns.Add<GXObjectTemplate>();
            args.Columns.Add<GXAttributeTemplate>();
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(o => o.Template, a => a.Id);
            args.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(o => o.Template, a => a.Id);
            args.Where.And<GXDevice>(q => q.Id == deviceId);
            args.Where.And<GXObject>(q => q.Removed == null);
            args.Where.And<GXObjectTemplate>(q => q.Removed == null && q.LogicalName == logicalName);
            args.Where.And<GXAttribute>(q => q.Removed == null);
            args.Where.And<GXAttributeTemplate>(q => q.Removed == null && q.Index == index);
            return await host.Connection.SingleOrDefaultAsync<GXAttribute>(transaction, args);
        }

        internal static async Task<GXObject> CreateLateBindObject(IGXHost host,
            IDbTransaction? transaction,
            string userId, GXDevice device, Guid objectId)
        {
            GXSelectArgs args = GXQuery.GetObjectTemplatesByUser(userId, objectId);
            args.Columns.Add<GXAttributeTemplate>();
            args.Distinct = true;
            args.Joins.AddInnerJoin<GXObjectTemplate, GXAttributeTemplate>(o => o.Id, a => a.ObjectTemplate);
            args.Where.And<GXAttributeTemplate>(q => q.Removed == null);
            GXObjectTemplate template = await host.Connection.SingleOrDefaultAsync<GXObjectTemplate>(args);
            if (template.Attributes == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            GXObject obj = new GXObject(template);
            if (obj.Attributes == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            obj.CreationTime = DateTime.Now;
            obj.Device = device;
            foreach (var it in template.Attributes)
            {
                obj.Attributes.Add(new GXAttribute(it)
                {
                    Object = obj,
                    Value = it.DefaultValue
                });
            }
            GXInsertArgs arg = GXInsertArgs.Insert(obj);
            arg.Exclude<GXObject>(e => e.Attributes);
            await host.Connection.InsertAsync(transaction, arg);
            arg = GXInsertArgs.InsertRange(obj.Attributes);
            await host.Connection.InsertAsync(transaction, arg);
            return obj;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal User,
            IEnumerable<GXObject> objects,
            Expression<Func<GXObject, object?>>? columns)
        {
            string userId = ServerHelpers.GetUserId(User);
            GXDevice? device = null;
            DateTime now = DateTime.Now;
            using IDbTransaction transaction = _host.Connection.BeginTransaction();
            try
            {
                foreach (var it in objects)
                {
                    if (it.Id == Guid.Empty)
                    {
                        it.CreationTime = now;
                        await _host.Connection.InsertAsync(transaction, GXInsertArgs.Insert(it));
                    }
                    else
                    {
                        //Check object.
                        GXSelectArgs args = GXQuery.GetObjectsByUser(userId, it.Id);
                        args.Distinct = true;
                        GXObject obj = await _host.Connection.SingleOrDefaultAsync<GXObject>(transaction, args);
                        if (obj == null)
                        {
                            //Check if this is late binding.
                            if (it.Device == null)
                            {
                                throw new ArgumentException(Properties.Resources.UnknownTarget);
                            }
                            if (device?.Template == null)
                            {
                                args = GXQuery.GetDevicesByUser(userId, false, it.Device.Id);
                                args.Columns.Add<GXDeviceTemplate>(c => c.Id);
                                args.Joins.AddInnerJoin<GXDevice, GXDeviceTemplate>(j => j.Template, j => j.Id);
                                device = await _host.Connection.SingleOrDefaultAsync<GXDevice>(transaction, args);
                                if (device?.Template == null)
                                {
                                    throw new ArgumentException(Properties.Resources.UnknownTarget);
                                }
                            }
                            await CreateLateBindObject(_host, transaction, userId, device, it.Id);
                        }
                        else
                        {
                            GXUpdateArgs arg = GXUpdateArgs.Update(it, columns);
                            if (columns == null)
                            {
                                arg.Exclude<GXObject>(e => new { e.CreationTime, e.Device });
                            }
                            await _host.Connection.UpdateAsync(transaction, arg);
                        }
                    }
                    //Update object parameters.
                    if (it.Parameters == null)
                    {
                        if (ServerHelpers.Contains(columns, nameof(GXObject.Parameters)))
                        {
                            throw new ArgumentNullException(Properties.Resources.ArrayIsEmpty);
                        }
                    }
                    else
                    {
                        GXSelectArgs arg = GXSelectArgs.SelectAll<GXObjectParameter>(w => w.Object == it && w.Removed == null);
                        var objectParameters = await _host.Connection.SelectAsync<GXObjectParameter>(transaction, arg);
                        var comparer = new UniqueComparer<GXObjectParameter, Guid>();
                        List<GXObjectParameter> removedObjectParameters = objectParameters.Except(it.Parameters, comparer).ToList();
                        List<GXObjectParameter> addedObjectGroupParameters = it.Parameters.Except(objectParameters, comparer).ToList();
                        List<GXObjectParameter> updatedObjectGroupParameters = it.Parameters.Union(objectParameters, comparer).ToList();
                        if (removedObjectParameters.Any())
                        {
                            RemoveObjectParameters(transaction, removedObjectParameters);
                        }
                        if (addedObjectGroupParameters.Any())
                        {
                            AddObjectParameters(transaction, it, addedObjectGroupParameters);
                        }
                        if (updatedObjectGroupParameters.Any())
                        {
                            foreach (var it2 in updatedObjectGroupParameters)
                            {
                                GXUpdateArgs u = GXUpdateArgs.Update(it2, c => new { c.Settings, c.Updated });
                                await _host.Connection.UpdateAsync(transaction, u);
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
            var ret = objects.Select(s => s.Id).ToArray();
            if (_performanceSettings.Notify(TargetType.Object))
            {
                List<string> users = await GetUsersAsync(User, ret);
                List<GXObject> list = new List<GXObject>();
                foreach (var it in objects)
                {
                    list.Add(new GXObject()
                    {
                        Id = it.Id,
                        LastRead = it.LastRead,
                        LastWrite = it.LastWrite,
                        LastAction = it.LastAction,
                        LastError = it.LastError,
                        LastErrorMessage = it.LastErrorMessage
                    });
                }
                await _eventsNotifier.ObjectUpdate(users, list);
            }
            return ret.ToArray();
        }

        /// <summary>
        /// Add object parameters.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="obj">Object where parameters are added.</param>
        /// <param name="parameters">Added object parameters.</param>
        public void AddObjectParameters(IDbTransaction transaction, GXObject obj, IEnumerable<GXObjectParameter> parameters)
        {
            DateTime now = DateTime.Now;
            foreach (GXObjectParameter it in parameters)
            {
                it.CreationTime = now;
                it.Object = obj;
            }
            _host.Connection.Insert(transaction, GXInsertArgs.InsertRange(parameters));
        }

        /// <summary>
        /// Remove object parameters from the object.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="parameters">Removed object parameters.</param>
        public void RemoveObjectParameters(IDbTransaction transaction, IEnumerable<GXObjectParameter> parameters)
        {
            foreach (GXObjectParameter it in parameters)
            {
                _host.Connection.Delete(transaction, GXDeleteArgs.DeleteById<GXObjectParameter>(it.Id));
            }
        }
    }
}
