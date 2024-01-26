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
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.User;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class AttributeRepository : IAttributeRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public AttributeRepository(IGXHost host,
            IUserRepository userRepository,
            IGXEventsNotifier eventsNotifier)
        {
            _host = host;
            _userRepository = userRepository;
            _eventsNotifier = eventsNotifier;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal User, Guid? attributeId)
        {
            GXSelectArgs args = GXQuery.GetUsersByAttribute(attributeId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal User, IEnumerable<Guid>? attributeIds)
        {
            string userId = ServerHelpers.GetUserId(User);
            GXSelectArgs args = GXQuery.GetUsersByAttributes(userId, attributeIds);
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
            IEnumerable<Guid> attributers,
            bool delete)
        {
            if (User == null || (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.DeviceTemplateManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXAttribute>(a => a.Id, q => attributers.Contains(q.Id));
            List<GXAttribute> list = _host.Connection.Select<GXAttribute>(arg);
            DateTime now = DateTime.Now;
            foreach (GXAttribute it in list)
            {
                it.Removed = now;
                List<string> users2 = await GetUsersAsync(User, it.Id);
                if (!users2.Any())
                {
                    //User doesn't have access rights for this attribute.
                    throw new UnauthorizedAccessException();
                }
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXAttribute>(it.Id));
                }
                else
                {
                    _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
                }
            }
            List<string> users = await GetUsersAsync(User, attributers);
            List<GXAttribute> list2 = new List<GXAttribute>();
            foreach (var it in list)
            {
                list.Add(new GXAttribute() { Id = it.Id });
            }
            await _eventsNotifier.AttributeDelete(users, list2);
        }

        /// <inheritdoc />
        public async Task<GXAttribute[]> ListAsync(
            ClaimsPrincipal User,
            ListAttributes? request,
            ListAttributesResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            GXAttributeTemplate? attributeTemplate = null;
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
            arg.Joins.AddLeftJoin<GXObject, GXAttribute>(j => j.Id, j => j.Object);
            arg.Joins.AddLeftJoin<GXAttribute, GXAttributeTemplate>(j => j.Template, j => j.Id);
            arg.Joins.AddInnerJoin<GXDevice, GXDeviceTemplate>(j => j.Template, j => j.Id);
            arg.Columns.Clear();
            arg.Columns.Add<GXDevice>();
            arg.Columns.Exclude<GXDevice>(e => new { e.Creator, e.CreationTime });
            arg.Columns.Add<GXObject>();
            arg.Columns.Add<GXObjectTemplate>();
            arg.Columns.Add<GXAttribute>();
            arg.Columns.Add<GXAttributeTemplate>();
            arg.Columns.Add<GXDeviceTemplate>(s => s.Id);
            arg.Columns.Exclude<GXDeviceTemplate>(e => new { e.Objects });
            arg.Where.And<GXDeviceTemplate>(q => q.Removed == null);
            if (request != null)
            {
                if (request.Devices != null && request.Devices.Any())
                {
                    arg.Where.And<GXDevice>(w => request.Devices.Contains(w.Id));
                }
                if (request.Filter?.Object != null)
                {
                    var dg = request.Filter.Object.Device?.DeviceGroups?.FirstOrDefault();
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
                    else if (request.Filter.Object.Device?.Id is Guid id && id != Guid.Empty)
                    {
                        arg.Where.And<GXDevice>(w => w.Id == id);
                    }
                    if (request.Filter.Object.Device?.Name is string dn)
                    {
                        arg.Where.And<GXDevice>(w => w.Name.Contains(dn));
                    }
                    if (request.Filter.Object.Device?.Template?.Name is string tn)
                    {
                        arg.Where.And<GXDeviceTemplate>(w => w.Name.Contains(tn));
                    }
                    request.Filter.Object.Device = null;
                }
                if (request.Filter?.Template != null)
                {
                    attributeTemplate = request.Filter.Template;
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
            arg.OrderBy.Add<GXDevice>(q => q.Id);
            arg.Count = 1;
            //Get devices before template filter or device is not retured if all objects are late binded.
            GXDevice[] devs = (await _host.Connection.SelectAsync<GXDevice>(arg)).ToArray();
            if (attributeTemplate != null)
            {
                arg.Where.FilterBy(attributeTemplate);
            }
            if (request != null)
            {
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXAttribute>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXAttribute>(w => request.Included.Contains(w.Id));
                }
            }

            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXAttribute>(q => GXSql.DistinctCount(q.Id));
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
                arg.OrderBy.Add<GXAttribute>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXAttribute>(q => q.Id);
            }
            List<GXAttribute> attributes = (await _host.Connection.SelectAsync<GXAttribute>(arg)).ToList();
            var deviceTemplateIds = devs.Select(s => s.Template.Id).ToArray();

            arg = GXSelectArgs.SelectAll<GXDeviceTemplate>();
            arg.Columns.Add<GXObjectTemplate>();
            arg.Columns.Add<GXAttributeTemplate>();
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXDeviceTemplate, GXObjectTemplate>(j => j.Id, j => j.DeviceTemplate);
            arg.Joins.AddInnerJoin<GXObjectTemplate, GXAttributeTemplate>(j => j.Id, j => j.ObjectTemplate);
            arg.Where.And<GXDeviceTemplate>(w => deviceTemplateIds.Contains(w.Id));
            if (attributeTemplate != null)
            {
                arg.Where.FilterBy(attributeTemplate);
            }
            if (request?.ObjectTypes != null && request.ObjectTypes.Any())
            {
                int?[] tmp = request.ObjectTypes.Cast<int?>().ToArray();
                arg.Where.And<GXObjectTemplate>(w => tmp.Contains(w.ObjectType));
            }
            if (request?.IgnoredObjectTypes != null && request.IgnoredObjectTypes.Any())
            {
                int?[] tmp = request.IgnoredObjectTypes.Cast<int?>().ToArray();
                arg.Where.And<GXObjectTemplate>(w => !tmp.Contains(w.ObjectType));
            }
            GXDeviceTemplate[] deviceTemplates = (await _host.Connection.SelectAsync<GXDeviceTemplate>(arg)).ToArray();
            //Remove all attributes that are saved, but not found from the template.
            //Filter might cause this.
            foreach (var dev in devs)
            {
                if (dev.Objects != null)
                {
                    var ids = deviceTemplates.Where(w => w.Id == dev.Template.Id).Single().Objects?.Select(s => s.Id);
                    dev.Objects.RemoveAll(w => !ids.Contains(w.Template.Id));
                }
            }
            int count = 0;
            if (request != null && (int)request.Count != 0)
            {
                count = (int)request.Count;
                if (request?.Exclude != null && request.Exclude.Any())
                {
                    count += request.Exclude.Length;
                }
            }
            int attributeCount = 0;
            bool lateBinded = false;
            if (deviceTemplates.Any())
            {
                foreach (var dev in devs)
                {
                    GXDeviceTemplate devTemplate = deviceTemplates.Where(w => w.Id == dev.Template.Id).Single();
                    attributeCount += devTemplate.Objects.SelectMany(s => s.Attributes).Count();
                    if (dev.Objects == null || dev.Objects.Count != devTemplate.Objects.Count)
                    {
                        //Get late bind objects.
                        foreach (var obj in devTemplate.Objects)
                        {
                            obj.DeviceTemplate = null;
                            if (count != 0 && count == attributes.Count)
                            {
                                if (!attributes.Where(w => w.Template != null && w.Template.Id == obj.Id &&
                                    w.Object?.Device != null && w.Object.Device.Id == dev.Id).Any())
                                {
                                    lateBinded = true;
                                }
                                break;
                            }
                            if (!attributes.Where(w => w.Template != null && w.Template.Id == obj.Id &&
                            w.Object?.Device != null && w.Object.Device.Id == dev.Id).Any())
                            {
                                lateBinded = true;
                                foreach (var it in obj.Attributes)
                                {
                                    if (!attributes.Where(w => w.Template.Id == it.Id).Any())
                                    {
                                        attributes.Add(new GXAttribute(it)
                                        {
                                            //Template ID is used as a ID for late bind objects.
                                            Id = it.Id,
                                            Object = new GXObject(obj)
                                            {
                                                Id = obj.Id,
                                                Latebind = true,
                                                Device = new GXDevice() { Id = dev.Id },
                                            }
                                        });
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        int removed = dev.Objects.SelectMany(s => s.Attributes).Count();
                        if (request?.Exclude != null && request.Exclude.Any())
                        {
                            attributes.RemoveAll(w => request.Exclude.Contains(w.Id));
                            removed = dev.Objects.RemoveAll(w => request.Exclude.Contains(w.Id));
                            attributes.AddRange(dev.Objects.SelectMany(s => s.Attributes).ToList());
                        }
                        count -= removed;
                    }
                }
            }
            if (lateBinded)
            {
                if (response != null)
                {
                    response.Count = attributeCount;
                    if (request?.Exclude != null && request.Exclude.Any())
                    {
                        response.Count -= request.Exclude.Count();
                    }
                }
            }
            if (request?.Exclude != null && request.Exclude.Any())
            {
                //Remove excluded objects.
                attributes.RemoveAll(w => request.Exclude.Contains(w.Id));
            }
            if (request != null && request.Count != 0 && (int)request.Count < attributes.Count)
            {
                //Remove extra objects.
                attributes = attributes.Skip((int)request.Index).Take((int)request.Count).ToList();
            }
            //Only device id is send.
            foreach (var att in attributes)
            {
                if (att.Object != null)
                {
                    att.Object = new GXObject()
                    {
                        Id = att.Object.Id,
                        Device = new GXDevice()
                        {
                            Id = att.Object.Device.Id,
                            Name = att.Object.Device.Name
                        },
                        Template = new GXObjectTemplate()
                        {
                            Id = att.Object.Template.Id,
                            LogicalName = att.Object.Template.LogicalName,
                            Name = att.Object.Template.Name
                        }
                    };
                }
                att.Template.ObjectTemplate = null;
            }
            if (response != null)
            {
                response.Attributes = attributes.ToArray();
                if (response.Count == 0 || response.Count < attributes.Count)
                {
                    response.Count = attributes.Count;
                }
            }
            return attributes.ToArray();
        }

        /// <inheritdoc />
        public async Task<GXAttribute> ReadAsync(
          ClaimsPrincipal User,
          Guid id)
        {
            string userId = ServerHelpers.GetUserId(User);
            GXSelectArgs arg = GXQuery.GetAttributesByUser(userId, id);
            arg.Columns.Add<GXAttributeTemplate>();
            arg.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(x => x.Template, y => y.Id);
            arg.Distinct = true;
            GXAttribute obj = await _host.Connection.SingleOrDefaultAsync<GXAttribute>(arg);
            if (obj == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            //Get attribute parameters.
            arg = GXSelectArgs.SelectAll<GXAttributeParameter>(w => w.Attribute == obj && w.Removed == null);
            arg.Columns.Add<GXModule>(s => s.Id);
            arg.Columns.Exclude<GXModule>(e => e.AttributeParameters);
            arg.Joins.AddInnerJoin<GXAttributeParameter, GXModule>(j => j.Module, j => j.Id);
            obj.Parameters = await _host.Connection.SelectAsync<GXAttributeParameter>(arg);
            return obj;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal User,
            IEnumerable<GXAttribute> attributes,
            Expression<Func<GXAttribute, object?>>? columns)
        {
            DateTime now = DateTime.Now;
            foreach (var att in attributes)
            {
                if (att.Id == Guid.Empty)
                {
                    att.CreationTime = now;
                    await _host.Connection.InsertAsync(GXInsertArgs.Insert(att));
                }
                await _host.Connection.UpdateAsync(GXUpdateArgs.Update(att, columns));
                if (att.Parameters != null)
                {
                    //Update attribute parameters.
                    GXSelectArgs arg = GXSelectArgs.SelectAll<GXAttributeParameter>(w => w.Attribute == att && w.Removed == null);
                    var attributeParameters = await _host.Connection.SelectAsync<GXAttributeParameter>(arg);
                    var comparer = new UniqueComparer<GXAttributeParameter, Guid>();
                    List<GXAttributeParameter> removedAttributeParameters = attributeParameters.Except(att.Parameters, comparer).ToList();
                    List<GXAttributeParameter> addedAttributeGroupParameters = att.Parameters.Except(attributeParameters, comparer).ToList();
                    List<GXAttributeParameter> updatedAttributeGroupParameters = att.Parameters.Union(attributeParameters, comparer).ToList();
                    if (removedAttributeParameters.Any())
                    {
                        RemoveAttributeParameters(att, removedAttributeParameters);
                    }
                    if (addedAttributeGroupParameters.Any())
                    {
                        AddAttributeParameters(att, addedAttributeGroupParameters);
                    }
                    if (updatedAttributeGroupParameters.Any())
                    {
                        foreach (var it in updatedAttributeGroupParameters)
                        {
                            GXUpdateArgs u = GXUpdateArgs.Update(it, c => new { c.Settings, c.Updated });
                            await _host.Connection.UpdateAsync(u);
                        }
                    }
                }
            }
            //TODO:
            var ret = attributes.Select(s => s.Id).ToArray();
            List<string> users = await GetUsersAsync(User, ret);
            List<GXAttribute> list = new List<GXAttribute>();
            foreach (var it in attributes)
            {
                list.Add(new GXAttribute()
                {
                    Id = it.Id,
                    Template = it.Template,
                    Exception = it.Exception,
                    Value = it.Value,
                    Read = it.Read,
                    Object = it.Object == null ? null : new GXObject()
                    {
                        Id = it.Object.Id,
                    }
                });
            }
            await _eventsNotifier.AttributeUpdate(users, list);
            return ret.ToArray();
        }

        /// <summary>
        /// Add attribute parameters.
        /// </summary>
        /// <param name="obj">Attribute where parameters are added.</param>
        /// <param name="parameters">Added attribute parameters.</param>
        public void AddAttributeParameters(GXAttribute obj, IEnumerable<GXAttributeParameter> parameters)
        {
            DateTime now = DateTime.Now;
            foreach (GXAttributeParameter it in parameters)
            {
                it.CreationTime = now;
                it.Attribute = obj;
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(parameters));
        }

        /// <summary>
        /// Remove attribute parameters from the attribute.
        /// </summary>
        /// <param name="obj">Attribute where parameters are removed.</param>
        /// <param name="parameters">Removed attribute parameters.</param>
        public void RemoveAttributeParameters(GXAttribute obj, IEnumerable<GXAttributeParameter> parameters)
        {
            foreach (GXAttributeParameter it in parameters)
            {
                _host.Connection.Delete(GXDeleteArgs.DeleteById<GXAttributeParameter>(it.Id));
            }
            //            _host.Connection.Delete(GXDeleteArgs.DeleteRange(parameters));
        }

        /// <inheritdoc />
        public async Task UpdateDatatypeAsync(
            ClaimsPrincipal User,
            IEnumerable<GXAttribute> attributes)
        {
            var ret = attributes.Select(s => s.Id).ToArray();
            List<string> users = await GetUsersAsync(User, ret);

            DateTime now = DateTime.Now;
            List<GXAttributeTemplate> templates = new List<GXAttributeTemplate>();
            foreach (GXAttribute it in attributes)
            {
                if (it.Template == null)
                {
                    throw new ArgumentException("Invalid template.");
                }
                templates.Add(it.Template);
                it.Updated = now;
            }
            List<GXAttribute> list = new List<GXAttribute>();
            foreach (var it in attributes)
            {
                var att = new GXAttribute()
                {
                    Id = it.Id,
                    Template = it.Template,
                    Exception = it.Exception,
                    Value = it.Value,
                    Read = it.Read
                };
                list.Add(att);
                if (it.Object != null)
                {
                    att.Object = new GXObject()
                    {
                        Id = it.Object.Id,
                    };
                }
            }
            await _host.Connection.UpdateAsync(GXUpdateArgs.UpdateRange(templates, c => new
            {
                c.DataType,
                c.Updated
            }));
            await _eventsNotifier.AttributeUpdate(users, list);
        }
    }
}
