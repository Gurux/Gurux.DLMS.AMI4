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
using Gurux.DLMS.AMI.Client.Pages.Objects;
using Org.BouncyCastle.Asn1.Cms;
using Google.Protobuf.WellKnownTypes;
using Gurux.DLMS.AMI.Client.Pages.User;
using System.Collections.Generic;
using System.Security.Cryptography.Xml;

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
            string userId = ServerHelpers.GetUserId(User);
            GXSelectArgs arg = GXQuery.GetAttributesByUser(userId, null);
            arg.Columns.Add<GXAttributeTemplate>();
            arg.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(j => j.Template, j => j.Id);
            if (request != null && request.Filter != null)
            {
                arg.Where.FilterBy(request.Filter);
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
            arg.Descending = true;
            arg.OrderBy.Add<GXAttribute>(q => q.Id);
            GXAttribute[] attributes = (await _host.Connection.SelectAsync<GXAttribute>(arg)).ToArray();
            if (response != null)
            {
                response.Attributes = attributes;
                if (response.Count == 0)
                {
                    response.Count = attributes.Length;
                }
            }
            return attributes;
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
                throw new ArgumentNullException(Properties.Resources.UnknownTarget);
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
                    Object = new GXObject()
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
            DateTime now = DateTime.Now;
            foreach (GXAttributeParameter it in parameters)
            {
                it.Removed = now;
                it.Attribute = obj;
            }
            _host.Connection.Delete(GXDeleteArgs.DeleteRange(parameters));
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
