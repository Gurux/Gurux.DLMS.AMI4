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
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared;
using System.Text.Json;
using Gurux.DLMS.AMI.Module;
using System.Data;
using Gurux.DLMS.AMI.Shared.DTOs.Device;

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
        private readonly IAmiCryproService _cryproService;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DeviceTemplateRepository(
            IGXHost host,
            IServiceProvider serviceProvider,
            IUserRepository userRepository,
            IDeviceTemplateGroupRepository deviceTemplateGroupRepository,
            IGXEventsNotifier eventsNotifier,
             IAmiCryproService cryproService)
        {
            _host = host;
            _serviceProvider = serviceProvider;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
            _deviceTemplateGroupRepository = deviceTemplateGroupRepository;
            _cryproService = cryproService;
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
            using IDbTransaction transaction = _host.Connection.BeginTransaction();
            try
            {
                if (!delete)
                {
                    foreach (GXDeviceTemplate it in list)
                    {
                        List<string> users = await GetUsersAsync(user, it.Id);
                        it.Removed = now;
                        _host.Connection.Update(transaction, GXUpdateArgs.Update(it, q => q.Removed));
                        updates[it] = users;
                    }
                }
                else
                {
                    foreach (GXDeviceTemplate it in list)
                    {
                        updates[it] = await GetUsersAsync(user, it.Id);
                    }
                    GXDeleteArgs args1;
                    //////////////////////////////////////////////////////
                    //It's faster to remove atribute templates, object templates and device templates with own query than removing just device templates.
                    //////////////////////////////////////////////////////
                    //Delete attributes.
                    arg = GXSelectArgs.Select<GXObjectTemplate>(a => a.Id);
                    arg.Joins.AddInnerJoin<GXObjectTemplate, GXDeviceTemplate>(j => j.DeviceTemplate, j => j.Id);
                    arg.Where.And<GXDeviceTemplate>(q => deviceTemplates.Contains(q.Id));
                    args1 = GXDeleteArgs.Delete<GXAttributeTemplate>(w => GXSql.Exists<GXAttributeTemplate, GXObjectTemplate>(j => j.ObjectTemplate, j => j.Id, arg));
                    await _host.Connection.DeleteAsync(transaction, args1);
                    //Delete objects.
                    arg = GXSelectArgs.Select<GXDeviceTemplate>(a => a.Id, q => deviceTemplates.Contains(q.Id));
                    args1 = GXDeleteArgs.Delete<GXObjectTemplate>(w => GXSql.Exists<GXObjectTemplate, GXDeviceTemplate>(j => j.DeviceTemplate, j => j.Id, arg));
                    await _host.Connection.DeleteAsync(transaction, args1);
                    //Delete device templates
                    var args = GXDeleteArgs.Delete<GXDeviceTemplate>(q => deviceTemplates.Contains(q.Id));
                    await _host.Connection.DeleteAsync(transaction, args);
                }
                _host.Connection.CommitTransaction(transaction);
            }
            catch (Exception)
            {
                _host.Connection.RollbackTransaction(transaction);
                throw;
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.DeviceTemplateDelete(it.Value, new GXDeviceTemplate[] { it.Key });
            }
        }

        /// <summary>
        /// Update manufacturers.
        /// </summary>
        /// <param name="templates"></param>
        /// <returns></returns>
        private async Task UpdateManufacturers(GXDeviceTemplate[] templates)
        {
            if (templates.Any())
            {
                //Client wants to know the device settings of the manufacturer.
                Guid[] ids = templates.Select(s => s.Id).ToArray();
                GXSelectArgs arg = GXSelectArgs.SelectAll<GXManufacturer>();
                arg.Columns.Add<GXDeviceModel>();
                arg.Columns.Add<GXDeviceVersion>();
                arg.Columns.Add<GXDeviceSettings>();
                arg.Columns.Add<GXDeviceTemplate>(s => s.Id);
                arg.Joins.AddInnerJoin<GXManufacturer, GXDeviceModel>(j => j.Id, j => j.Manufacturer);
                arg.Joins.AddInnerJoin<GXDeviceModel, GXDeviceVersion>(j => j.Id, j => j.Model);
                arg.Joins.AddInnerJoin<GXDeviceVersion, GXDeviceSettings>(j => j.Id, j => j.Version);
                arg.Joins.AddInnerJoin<GXDeviceSettings, GXDeviceTemplate>(j => j.Template, j => j.Id);
                arg.Where.And<GXDeviceTemplate>(w => GXSql.In(w.Id, ids));
                GXManufacturer[] manufacturers = (await _host.Connection.SelectAsync<GXManufacturer>(arg)).ToArray();
                foreach (var manufacturer in manufacturers)
                {
                    if (manufacturer.Models != null)
                    {
                        foreach (var model in manufacturer.Models)
                        {
                            if (model.Versions != null)
                            {
                                foreach (var version in model.Versions)
                                {
                                    if (version.Settings != null)
                                    {
                                        foreach (var settings in version.Settings)
                                        {
                                            if (settings.Template != null)
                                            {
                                                var target = templates.Where(w => w.Id == settings.Template.Id).SingleOrDefault();
                                                if (target != null)
                                                {
                                                    GXDeviceVersion ver = new GXDeviceVersion()
                                                    {
                                                        Id = version.Id,
                                                        Name = version.Name
                                                    };
                                                    GXDeviceModel model2 = new GXDeviceModel()
                                                    {
                                                        Id = model.Id,
                                                        Name = model.Name,
                                                        Versions = new List<GXDeviceVersion>()
                                                    };
                                                    model2.Versions.Add(ver);
                                                    target.Manufacturer = new GXManufacturer()
                                                    {
                                                        Id = manufacturer.Id,
                                                        Name = manufacturer.Name,
                                                        Models = new List<GXDeviceModel>()
                                                    };
                                                    target.Manufacturer.Models.Add(model2);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
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
                if (string.Compare(request.OrderBy, "Version", true) == 0)
                {
                    arg.OrderBy.Add<GXDeviceVersion>(o => o.Name);
                }
                else if (string.Compare(request.OrderBy, "Model", true) == 0)
                {
                    arg.OrderBy.Add<GXDeviceModel>(o => o.Name);
                }
                else
                {
                    arg.OrderBy.Add<GXDeviceTemplate>(request.OrderBy);
                }
            }
            else
            {
                arg.OrderBy.Add<GXDeviceTemplate>(q => q.CreationTime);
                arg.Descending = true;
            }
            if (request != null && request.Filter != null)
            {
                bool addJoin = false;
                if ((request.Filter.Manufacturer is GXManufacturer man))
                {
                    if (!string.IsNullOrEmpty(man.Name))
                    {
                        addJoin = true;
                        arg.Where.And<GXManufacturer>(w => w.Name.Contains(man.Name));
                    }
                    if ((man.Models?.FirstOrDefault() is GXDeviceModel mod))
                    {
                        if (!string.IsNullOrEmpty(mod.Name))
                        {
                            addJoin = true;
                            arg.Where.And<GXDeviceModel>(w => w.Name.Contains(mod.Name));
                        }
                        if ((mod.Versions?.FirstOrDefault() is GXDeviceVersion version))
                        {
                            if (!string.IsNullOrEmpty(version.Name))
                            {
                                addJoin = true;
                                arg.Where.And<GXDeviceVersion>(w => w.Name.Contains(version.Name));
                            }
                        }
                    }
                }
                if (addJoin)
                {
                    arg.Joins.AddInnerJoin<GXDeviceTemplate, GXDeviceSettings>(j => j.Id, j => j.Template);
                    arg.Joins.AddInnerJoin<GXDeviceSettings, GXDeviceVersion>(j => j.Version, j => j.Id);
                    arg.Joins.AddInnerJoin<GXDeviceVersion, GXDeviceModel>(j => j.Model, j => j.Id);
                    arg.Joins.AddInnerJoin<GXDeviceModel, GXManufacturer>(j => j.Manufacturer, j => j.Id);
                }

                //Reset manufacturer filter.
                request.Filter.Manufacturer = null;
                arg.Where.FilterBy(request.Filter);
            }
            if (request?.Exclude != null && request.Exclude.Any())
            {
                arg.Where.And<GXDeviceTemplate>(w => !request.Exclude.Contains(w.Id));
            }
            if (request?.Included != null && request.Included.Any())
            {
                arg.Where.And<GXDeviceTemplate>(w => request.Included.Contains(w.Id));
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
            if (request?.Select != null && request.Select.Contains(TargetType.Manufacturer))
            {
                await UpdateManufacturers(templates);
            }
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
            GXSelectArgs arg;
            if (user.IsInRole(GXRoles.Admin))
            {
                arg = GXSelectArgs.SelectAll<GXDeviceTemplate>(w => w.Id == id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetDeviceTemplatesByUser(userId, id);
            }
            GXDeviceTemplate ret = await _host.Connection.SingleOrDefaultAsync<GXDeviceTemplate>(arg);
            if (ret == null)
            {
                throw new GXAmiNotFoundException(Properties.Resources.DeviceTemplate + " " + Properties.Resources.Id + " " + id.ToString());
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
            if (!string.IsNullOrEmpty(ret.Settings))
            {
                var s = JsonSerializer.Deserialize<Shared.DTOs.Device.GXDLMSSettings>(ret.Settings);
                if (!string.IsNullOrEmpty(s.Password))
                {
                    try
                    {
                        s.Password = _cryproService.Decrypt(s.Password);
                    }
                    catch (Exception)
                    {
                        //Old data is not encrypted.
                        //This can be removed later.
                    }
                }
                if (s.HexPassword != null && s.HexPassword.Any())
                {
                    try
                    {
                        s.HexPassword = _cryproService.Decrypt(s.HexPassword);
                    }
                    catch (Exception)
                    {
                        //Old data is not encrypted.
                        //This can be removed later.
                    }
                }
                ret.Settings = JsonSerializer.Serialize(s);
            }
            return ret;
        }

        private void EncryptPassword(GXDeviceTemplate it)
        {
            if (!string.IsNullOrEmpty(it.Settings))
            {
                var s = JsonSerializer.Deserialize<Shared.DTOs.Device.GXDLMSSettings>(it.Settings);
                if (!string.IsNullOrEmpty(s.Password))
                {
                    s.Password = _cryproService.Encrypt(s.Password);
                }
                if (s.HexPassword != null && s.HexPassword.Any())
                {
                    s.HexPassword = _cryproService.Encrypt(s.HexPassword);
                }
                it.Settings = JsonSerializer.Serialize(s);
            }
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
            List<GXDeviceTemplateGroup>? defaultGroups = null;
            var newTemplates = DeviceTemplates.Where(w => w.Id == Guid.Empty).ToList();
            var updatedTemplates = DeviceTemplates.Where(w => w.Id != Guid.Empty).ToList();
            //Get notified users.
            if (newTemplates.Any())
            {
                var first = newTemplates.First();
                var users = await GetUsersAsync(user, first.Id);
                foreach (var it in newTemplates)
                {
                    updates[it] = users;
                }
            }
            foreach (var it in updatedTemplates)
            {
                updates[it] = await GetUsersAsync(user, it.Id);
            }
            foreach (GXDeviceTemplate it in DeviceTemplates)
            {
                if (string.IsNullOrEmpty(it.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                if (it.DeviceTemplateGroups == null || !it.DeviceTemplateGroups.Any())
                {
                    if (defaultGroups == null)
                    {
                        ListDeviceTemplateGroups request = new ListDeviceTemplateGroups()
                        {
                            Filter = new GXDeviceTemplateGroup() { Default = true }
                        };
                        defaultGroups = new List<GXDeviceTemplateGroup>(await _deviceTemplateGroupRepository.ListAsync(user, request, null, CancellationToken.None));
                    }
                    it.DeviceTemplateGroups = defaultGroups;
                    if (it.DeviceTemplateGroups == null || !it.DeviceTemplateGroups.Any())
                    {
                        throw new ArgumentNullException(Properties.Resources.ArrayIsEmpty);
                    }
                }
            }
            using IDbTransaction transaction = _host.Connection.BeginTransaction();
            try
            {
                if (newTemplates.Any())
                {
                    foreach (var dt in newTemplates)
                    {
                        if (dt.Objects == null || !dt.Objects.Any())
                        {
                            throw new ArgumentNullException(Properties.Resources.ArrayIsEmpty);
                        }
                        EncryptPassword(dt);
                        dt.CreationTime = now;
                        foreach (var ot in dt.Objects)
                        {
                            ot.DeviceTemplate = dt;
                            if (ot.Attributes != null)
                            {
                                foreach (var at in ot.Attributes)
                                {
                                    at.ObjectTemplate = ot;
                                }
                            }
                        }
                    }

                    GXInsertArgs args = GXInsertArgs.InsertRange(newTemplates);
                    args.Exclude<GXDeviceTemplate>(e => new
                    {
                        e.Updated,
                        e.Removed,
                        e.Objects
                    });
                    using IDbTransaction transaction2 = _host.Connection.BeginTransaction();
                    _host.Connection.Insert(transaction2, args);
                    //Add object templates.
                    var objects = newTemplates.SelectMany(s => s.Objects).ToArray();
                    args = GXInsertArgs.InsertRange(objects);
                    args.Exclude<GXObjectTemplate>(e => new
                    {
                        e.Updated,
                        e.Removed,
                        e.Attributes
                    });
                    _host.Connection.Insert(transaction2, args);
                    //Add attribute templates.
                    var attributes = objects.SelectMany(s => s.Attributes).ToArray();
                    args = GXInsertArgs.InsertRange(attributes);
                    args.Exclude<GXAttributeTemplate>(e => new
                    {
                        e.Updated,
                        e.Removed,
                    });
                    _host.Connection.Insert(transaction2, args);
                    _host.Connection.CommitTransaction(transaction2);
                    foreach (var it in newTemplates)
                    {
                        list.Add(it.Id);
                    }
                }
                foreach (var it in updatedTemplates)
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXDeviceTemplate>(q => q.ConcurrencyStamp, where => where.Id == it.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    it.Updated = now;
                    it.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                    EncryptPassword(it);
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
                        RemoveDeviceTemplateFromDeviceTemplateGroups(transaction, it.Id, removedDeviceGroups);
                    }
                    if (addedDeviceGroups.Any())
                    {
                        AddDeviceTemplateToDeviceTemplateGroups(transaction, it.Id, addedDeviceGroups);
                    }
                }
                _host.Connection.CommitTransaction(transaction);
            }
            catch (Exception)
            {
                _host.Connection.RollbackTransaction(transaction);
                throw;
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.DeviceTemplateUpdate(it.Value,
                    new GXDeviceTemplate[]
                    { new GXDeviceTemplate(){Id = it.Key.Id}
                    });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map device template to device template groups.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="deviceTemplateId">Device template ID.</param>
        /// <param name="groups">Group IDs of the device template groups where the device template is added.</param>
        public void AddDeviceTemplateToDeviceTemplateGroups(IDbTransaction transaction, Guid deviceTemplateId, IEnumerable<GXDeviceTemplateGroup> groups)
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
            _host.Connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between device template group and device template.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// /// <param name="deviceTemplateId">Device template ID.</param>
        /// <param name="groups">Group IDs of the device template groups where the device template is removed.</param>
        public void RemoveDeviceTemplateFromDeviceTemplateGroups(IDbTransaction transaction, Guid deviceTemplateId, IEnumerable<GXDeviceTemplateGroup> groups)
        {
            foreach (var it in groups)
            {
                var arg = GXDeleteArgs.Delete<GXDeviceTemplateGroupDeviceTemplate>(w => w.DeviceTemplateId == deviceTemplateId && w.DeviceTemplateGroupId == it.Id);
                _host.Connection.Delete(transaction, arg);
            }
        }
    }
}