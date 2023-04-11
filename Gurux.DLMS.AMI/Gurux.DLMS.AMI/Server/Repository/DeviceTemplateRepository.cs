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

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class DeviceTemplateRepository : IDeviceTemplateRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDeviceTemplateGroupRepository _deviceTemplateGroupRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DeviceTemplateRepository(
            IGXHost host,
            IServiceProvider serviceProvider,
            IUserRepository userRepository,
            IDeviceTemplateGroupRepository deviceTemplateGroupRepository,
            IGXEventsNotifier eventsNotifier)
        {
            _host = host;
            _serviceProvider = serviceProvider;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
            _deviceTemplateGroupRepository = deviceTemplateGroupRepository;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal User, Guid? templateId)
        {
            GXSelectArgs args = GXQuery.GetUsersByDeviceTemplate(ServerHelpers.GetUserId(User), templateId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal User, IEnumerable<Guid>? templateIds)
        {
            GXSelectArgs args = GXQuery.GetUsersByDeviceTemplates(ServerHelpers.GetUserId(User), templateIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal user, IEnumerable<Guid> deviceTemplates,
            bool delete)
        {
            if (user == null || (!user.IsInRole(GXRoles.Admin) && !user.IsInRole(GXRoles.DeviceTemplateManager)))
            {
                throw new UnauthorizedAccessException();
            }

            GXSelectArgs arg = GXSelectArgs.Select<GXDeviceTemplate>(a => a.Id, q => deviceTemplates.Contains(q.Id));
            List<GXDeviceTemplate> list = _host.Connection.Select<GXDeviceTemplate>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXDeviceTemplate, List<string>> updates = new Dictionary<GXDeviceTemplate, List<string>>();
            foreach (GXDeviceTemplate it in list)
            {
                it.Removed = now;
                List<string> users = await GetUsersAsync(user, it.Id);
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXDeviceTemplate>(it.Id));
                }
                else
                {
                    _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
                }
                updates[it] = users;
            }
            foreach (var it in updates)
            {
                GXDeviceTemplate tmp = new GXDeviceTemplate() { Id = it.Key.Id };
                await _eventsNotifier.DeviceTemplateDelete(it.Value, new GXDeviceTemplate[] { tmp });
            }
        }

        /// <inheritdoc />
        public async Task<GXDeviceTemplate[]> ListAsync(
            ClaimsPrincipal user,
            ListDeviceTemplates? request,
            ListDeviceTemplatesResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && user.IsInRole(GXRoles.Admin))
            {
                arg = GXSelectArgs.SelectAll<GXDeviceTemplate>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetDeviceTemplatesByUser(userId, null);
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXDeviceTemplate>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXDeviceTemplate>(q => q.CreationTime);
                arg.Descending = true;
            }
            if (request != null && request.Filter != null)
            {
                arg.Where.FilterBy(request.Filter);
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXDeviceTemplate>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = await _host.Connection.SingleOrDefaultAsync<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            GXDeviceTemplate[] templates = (await _host.Connection.SelectAsync<GXDeviceTemplate>(arg)).ToArray();
            if (response != null)
            {
                response.Templates = templates;
                if (response.Count == 0)
                {
                    response.Count = templates.Length;
                }
            }
            return templates;
        }

        /// <inheritdoc />
        public async Task<GXDeviceTemplate> ReadAsync(
           ClaimsPrincipal user,
           Guid id)
        {
            string userId = ServerHelpers.GetUserId(user);
            GXSelectArgs arg = GXQuery.GetDeviceTemplatesByUser(userId, id);
            GXDeviceTemplate ret = await _host.Connection.SingleOrDefaultAsync<GXDeviceTemplate>(arg);
            if (ret == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            //Objects and attributes are faster to retrieve with own query.
            arg = GXSelectArgs.SelectAll<GXObjectTemplate>(where => where.DeviceTemplate == ret && where.Removed == null);
            arg.Columns.Add<GXAttributeTemplate>();
            arg.Distinct = true;
            arg.Joins.AddLeftJoin<GXObjectTemplate, GXAttributeTemplate>(o => o.Id, a => a.ObjectTemplate);
            arg.Where.And<GXAttributeTemplate>(q => q.Removed == null);
            arg.Columns.Exclude<GXAttributeTemplate>(e => e.ObjectTemplate);
            ret.Objects = _host.Connection.Select<GXObjectTemplate>(arg);

            //Get device template groups.
            arg = GXSelectArgs.SelectAll<GXDeviceTemplateGroup>(where => where.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXDeviceTemplateGroup, GXDeviceTemplateGroupDeviceTemplate>(o => o.Id, a => a.DeviceTemplateGroupId);
            arg.Where.And<GXDeviceTemplateGroupDeviceTemplate>(q => q.DeviceTemplateId == id && q.Removed == null);
            ret.DeviceTemplateGroups = _host.Connection.Select<GXDeviceTemplateGroup>(arg);
            return ret;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal user,
            IEnumerable<GXDeviceTemplate> DeviceTemplates,
            Expression<Func<GXDeviceTemplate, object?>>? columns)
        {
            DateTime now = DateTime.Now;
            bool isAdmin = true;
            string userId = ServerHelpers.GetUserId(user);
            isAdmin = user.IsInRole(GXRoles.Admin);
            List<Guid> list = new List<Guid>();
            Dictionary<GXDeviceTemplate, List<string>> updates = new Dictionary<GXDeviceTemplate, List<string>>();
            foreach (GXDeviceTemplate it in DeviceTemplates)
            {
                if (string.IsNullOrEmpty(it.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                if (it.Id == Guid.Empty)
                {
                    if (it.DeviceTemplateGroups == null || !it.DeviceTemplateGroups.Any())
                    {
                        ListDeviceTemplateGroups request = new ListDeviceTemplateGroups()
                        {
                            Filter = new GXDeviceTemplateGroup() { Default = true }
                        };
                        it.DeviceTemplateGroups = new List<GXDeviceTemplateGroup>();
                        it.DeviceTemplateGroups.AddRange(await _deviceTemplateGroupRepository.ListAsync(user, request, null, CancellationToken.None));
                    }
                    if (!it.DeviceTemplateGroups.Any())
                    {
                        throw new ArgumentNullException(Properties.Resources.ArrayIsEmpty);
                    }
                    if (it.Objects == null || !it.Objects.Any())
                    {
                        throw new ArgumentNullException(Properties.Resources.ArrayIsEmpty);
                    }
                    it.CreationTime = now;
                    GXInsertArgs args = GXInsertArgs.Insert(it);
                    args.Exclude<GXDeviceTemplate>(e => new { e.Updated, e.DeviceTemplateGroups });
                    _host.Connection.Insert(args);
                    list.Add(it.Id);
                    AddDeviceToDeviceGroups(it.Id, it.DeviceTemplateGroups);
                }
                else
                {
                    if (it.DeviceTemplateGroups == null || !it.DeviceTemplateGroups.Any())
                    {
                        throw new ArgumentNullException(Properties.Resources.ArrayIsEmpty);
                    }
                    GXSelectArgs m = GXSelectArgs.Select<GXDeviceTemplate>(q => q.ConcurrencyStamp, where => where.Id == it.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    it.Updated = now;
                    it.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                    args.Exclude<GXDeviceTemplate>(q => new { q.CreationTime, q.DeviceTemplateGroups });
                    _host.Connection.Update(args);

                    //Map device template to device template group.
                    List<GXDeviceTemplateGroup> deviceTemplateGroups;
                    using (IServiceScope scope = _serviceProvider.CreateScope())
                    {
                        IDeviceTemplateGroupRepository deviceTemplateGroupRepository = scope.ServiceProvider.GetRequiredService<IDeviceTemplateGroupRepository>();
                        deviceTemplateGroups = await deviceTemplateGroupRepository.GetJoinedDeviceTemplateGroups(user, it.Id);
                    }
                    var comparer = new UniqueComparer<GXDeviceTemplateGroup, Guid>();
                    List<GXDeviceTemplateGroup> removedDeviceGroups = deviceTemplateGroups.Except(it.DeviceTemplateGroups, comparer).ToList();
                    List<GXDeviceTemplateGroup> addedDeviceGroups = it.DeviceTemplateGroups.Except(deviceTemplateGroups, comparer).ToList();
                    if (removedDeviceGroups.Any())
                    {
                        RemoveDevicesFromDeviceGroup(it.Id, removedDeviceGroups);
                    }
                    if (addedDeviceGroups.Any())
                    {
                        AddDeviceToDeviceGroups(it.Id, addedDeviceGroups);
                    }
                }
                updates[it] = await GetUsersAsync(user, it.Id);
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.DeviceTemplateUpdate(it.Value, new GXDeviceTemplate[] { it.Key });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map device template to device template groups.
        /// </summary>
        /// <param name="deviceTemplateId">Device template ID.</param>
        /// <param name="groups">Group IDs of the device template groups where the device template is added.</param>
        public void AddDeviceToDeviceGroups(Guid deviceTemplateId, IEnumerable<GXDeviceTemplateGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXDeviceTemplateGroupDeviceTemplate> list = new List<GXDeviceTemplateGroupDeviceTemplate>();
            foreach (GXDeviceTemplateGroup it in groups)
            {
                list.Add(new GXDeviceTemplateGroupDeviceTemplate()
                {
                    DeviceTemplateId = deviceTemplateId,
                    DeviceTemplateGroupId = it.Id,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between device template group and device template.
        /// </summary>
        /// <param name="deviceTemplateId">Device template ID.</param>
        /// <param name="groups">Group IDs of the device template groups where the device template is removed.</param>
        public void RemoveDevicesFromDeviceGroup(Guid deviceTemplateId, IEnumerable<GXDeviceTemplateGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXDeviceTemplateGroupDeviceTemplate> list = new List<GXDeviceTemplateGroupDeviceTemplate>();
            foreach (var it in groups)
            {
                list.Add(new GXDeviceTemplateGroupDeviceTemplate()
                {
                    DeviceTemplateId = deviceTemplateId,
                    DeviceTemplateGroupId = it.Id,
                    Removed = now
                });
            }
            _host.Connection.Delete(GXDeleteArgs.DeleteRange(list));
        }
    }
}