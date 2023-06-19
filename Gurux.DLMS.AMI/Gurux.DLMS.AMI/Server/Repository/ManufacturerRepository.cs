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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Client.Shared;
using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.DTOs;
using System.Net.Http.Headers;
using Gurux.DLMS.AMI.Client.Helpers;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class ManufacturerRepository : IManufacturerRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IManufacturerGroupRepository _manufacturerGroupRepository;
        private readonly IDeviceTemplateRepository _deviceTemplateRepository;


        /// <summary>
        /// Constructor.
        /// </summary>
        public ManufacturerRepository(IGXHost host,
            IUserRepository userRepository,
            IServiceProvider serviceProvider,
            IManufacturerGroupRepository manufacturerGroupRepository,
            IGXEventsNotifier eventsNotifier,
            IDeviceTemplateRepository deviceTemplateRepository)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
            _serviceProvider = serviceProvider;
            _manufacturerGroupRepository = manufacturerGroupRepository;
            _deviceTemplateRepository = deviceTemplateRepository;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal user, Guid? manufacturerId)
        {
            GXSelectArgs args = GXQuery.GetUsersByManufacturer(ServerHelpers.GetUserId(user), manufacturerId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal user, IEnumerable<Guid>? manufacturerIds)
        {
            GXSelectArgs args = GXQuery.GetUsersByManufacturers(ServerHelpers.GetUserId(user), manufacturerIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal User,
            IEnumerable<Guid> manufacturers,
            bool delete)
        {
            if (User == null || (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.ManufacturerManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXManufacturer>(a => a.Id, q => manufacturers.Contains(q.Id));
            List<GXManufacturer> list = _host.Connection.Select<GXManufacturer>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXManufacturer, List<string>> updates = new();
            foreach (GXManufacturer it in list)
            {
                it.Removed = now;
                List<string> users = await GetUsersAsync(User, it.Id);
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXManufacturer>(it.Id));
                }
                else
                {
                    _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
                }
                updates[it] = users;
            }
            foreach (var it in updates)
            {
                GXManufacturer tmp = new GXManufacturer() { Id = it.Key.Id };
                await _eventsNotifier.ManufacturerDelete(it.Value, new GXManufacturer[] { tmp });
            }
        }

        /// <inheritdoc />
        public async Task<GXManufacturer[]> ListAsync(
            ClaimsPrincipal user,
            ListManufacturers? request,
            ListManufacturersResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXManufacturer>();
            bool joinAdded = false;
            if (request?.Filter != null)
            {
                //Handle device settings installation time.
                GXDeviceModel? model = request.Filter.Models?.FirstOrDefault();
                if (model != null)
                {
                    GXDeviceVersion? version = model.Versions?.FirstOrDefault();
                    if (version != null)
                    {
                        GXDeviceSettings? settings = version.Settings?.FirstOrDefault();
                        if (settings?.InstallationTime != null)
                        {
                            DateTimeOffset dt = settings.InstallationTime.Value;
                            settings.InstallationTime = null;
                            joinAdded = true;
                            arg.Joins.AddLeftJoin<GXManufacturer, GXDeviceModel>(x => x.Id, y => y.Manufacturer);
                            arg.Joins.AddLeftJoin<GXDeviceModel, GXDeviceVersion>(j => j.Id, j => j.Model);
                            arg.Joins.AddLeftJoin<GXDeviceVersion, GXDeviceSettings>(j => j.Id, j => j.Version);
                            arg.Joins.AddLeftJoin<GXDeviceSettings, GXDeviceTemplate>(j => j.Template, j => j.Id);
                            arg.Where.And<GXDeviceSettings>(w => w.InstallationTime >= dt);
                        }
                        if (settings?.Template != null && settings.Template.Id != Guid.Empty)
                        {
                            var template = settings.Template;
                            arg.Where.And<GXDeviceSettings>(w => w.Template == template);
                            settings.Template = null;
                        }
                    }
                }
                request.Filter.Models = null;
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXManufacturer>(w => !request.Exclude.Contains(w.Id));
                }
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXManufacturer>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXManufacturer>(q => q.CreationTime);
                arg.Descending = true;
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXManufacturer>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            if (request != null)
            {
                if ((request.Select & TargetType.Version) != 0)
                {
                    //Get model versions
                    if (!joinAdded)
                    {
                        arg.Joins.AddLeftJoin<GXManufacturer, GXDeviceModel>(x => x.Id, y => y.Manufacturer);
                        arg.Joins.AddLeftJoin<GXDeviceModel, GXDeviceVersion>(j => j.Id, j => j.Model);
                        arg.Joins.AddLeftJoin<GXDeviceVersion, GXDeviceSettings>(j => j.Id, j => j.Version);
                        arg.Joins.AddLeftJoin<GXDeviceSettings, GXDeviceTemplate>(j => j.Template, j => j.Id);
                    }
                    arg.Columns.Add<GXDeviceModel>();
                    arg.Columns.Exclude<GXDeviceModel>(e => e.Manufacturer);
                    arg.Columns.Add<GXDeviceVersion>();
                    arg.Columns.Exclude<GXDeviceVersion>(e => e.Model);
                    arg.Columns.Add<GXDeviceSettings>();
                    arg.Columns.Exclude<GXDeviceSettings>(e => e.Version);
                    //Select device template ID and name.
                    arg.Columns.Add<GXDeviceTemplate>(s => new { s.Id, s.Name });
                }
                if ((request.Select & TargetType.Model) != 0)
                {
                    //Get models
                    if (!joinAdded)
                    {
                        arg.Joins.AddLeftJoin<GXManufacturer, GXDeviceModel>(x => x.Id, y => y.Manufacturer);
                    }
                    arg.Columns.Add<GXDeviceModel>();
                    arg.Columns.Exclude<GXDeviceModel>(e => e.Manufacturer);
                }
            }
            GXManufacturer[] manufacturers = (await _host.Connection.SelectAsync<GXManufacturer>(arg)).ToArray();
            if (response != null)
            {
                response.Manufacturers = manufacturers;
                response.Count = manufacturers.Length;
            }
            return manufacturers;
        }

        /// <inheritdoc />
        public async Task<GXManufacturer> ReadAsync(
            ClaimsPrincipal user,
            Guid id)
        {
            bool isAdmin = false;
            if (user != null)
            {
                isAdmin = user.IsInRole(GXRoles.Admin);
            }
            GXSelectArgs arg;
            if (user == null || isAdmin)
            {
                //Admin can see all the manufacturers.
                arg = GXSelectArgs.SelectAll<GXManufacturer>();
                arg.Joins.AddLeftJoin<GXManufacturer, GXDeviceModel>(x => x.Id, y => y.Manufacturer);
                arg.Joins.AddLeftJoin<GXDeviceModel, GXDeviceVersion>(j => j.Id, j => j.Model);
                arg.Joins.AddLeftJoin<GXDeviceVersion, GXDeviceSettings>(j => j.Id, j => j.Version);
                arg.Joins.AddLeftJoin<GXDeviceSettings, GXDeviceTemplate>(j => j.Template, j => j.Id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetManufacturersByUser(userId, id);
            }
            arg.Where.And<GXManufacturer>(w => w.Id == id);
            arg.Columns.Add<GXDeviceModel>();
            arg.Columns.Exclude<GXDeviceModel>(e => e.Manufacturer);
            arg.Columns.Add<GXDeviceVersion>();
            arg.Columns.Exclude<GXDeviceVersion>(e => e.Model);
            arg.Columns.Add<GXDeviceSettings>();
            arg.Columns.Exclude<GXDeviceSettings>(e => e.Version);
            arg.Columns.Add<GXDeviceTemplate>(s => new { s.Id, s.Name });
            arg.Distinct = true;
            GXManufacturer manufacturer = await _host.Connection.SingleOrDefaultAsync<GXManufacturer>(arg);
            if (manufacturer == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            arg = GXSelectArgs.SelectAll<GXManufacturerGroup>();
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXManufacturerGroupManufacturer, GXManufacturerGroup>(x => x.ManufacturerGroupId, y => y.Id);
            arg.Where.And<GXManufacturerGroupManufacturer>(w => w.ManufacturerId == id);
            manufacturer.ManufacturerGroups = await _host.Connection.SelectAsync<GXManufacturerGroup>(arg);
            return manufacturer;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal user,
            IEnumerable<GXManufacturer> manufacturers,
            Expression<Func<GXManufacturer, object?>>? columns)
        {
            DateTime now = DateTime.Now;
            List<Guid> list = new();
            Dictionary<GXManufacturer, List<string>> updates = new();
            foreach (GXManufacturer uManufacturer in manufacturers)
            {
                bool update = false;
                if (string.IsNullOrEmpty(uManufacturer.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                if (!uManufacturer.Template.GetValueOrDefault(false) &&
                    (uManufacturer.ManufacturerGroups == null || !uManufacturer.ManufacturerGroups.Any()))
                {
                    ListManufacturerGroups request = new ListManufacturerGroups()
                    {
                        Filter = new GXManufacturerGroup() { Default = true }
                    };
                    uManufacturer.ManufacturerGroups = new List<GXManufacturerGroup>();
                    uManufacturer.ManufacturerGroups.AddRange(await _manufacturerGroupRepository.ListAsync(user, request, null, CancellationToken.None));
                    if (!uManufacturer.ManufacturerGroups.Any())
                    {
                        throw new ArgumentException(Properties.Resources.TargetMustBelongToOneGroup);
                    }
                }
                if (uManufacturer.Id == Guid.Empty)
                {
                    uManufacturer.CreationTime = now;
                    GXInsertArgs args = GXInsertArgs.Insert(uManufacturer);
                    args.Exclude<GXManufacturer>(q => new { q.ManufacturerGroups, uManufacturer.Models });
                    _host.Connection.Insert(args);
                    list.Add(uManufacturer.Id);
                    AddModelsToManufacturer(uManufacturer, uManufacturer.Models);
                    AddManufacturerToManufacturerGroups(uManufacturer.Id, uManufacturer.ManufacturerGroups);
                    updates[uManufacturer] = await GetUsersAsync(user, uManufacturer.Id);
                    update = true;
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXManufacturer>(q => q.ConcurrencyStamp, where => where.Id == uManufacturer.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != uManufacturer.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    uManufacturer.Updated = now;
                    uManufacturer.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(uManufacturer, columns);
                    args.Exclude<GXManufacturer>(q => new { q.CreationTime, q.ManufacturerGroups, q.Models });
                    _host.Connection.Update(args);
                    //Map manufacturer groups to manufacturer.
                    List<GXManufacturerGroup> manufacturerGroups;
                    using (IServiceScope scope = _serviceProvider.CreateScope())
                    {
                        IManufacturerGroupRepository manufacturerGroupRepository = scope.ServiceProvider.GetRequiredService<IManufacturerGroupRepository>();
                        manufacturerGroups = await manufacturerGroupRepository.GetJoinedManufacturerGroups(user, uManufacturer.Id);
                    }
                    if (uManufacturer.ManufacturerGroups != null)
                    {
                        var comparer = new UniqueComparer<GXManufacturerGroup, Guid>();
                        List<GXManufacturerGroup> removedManufacturerGroups = manufacturerGroups.Except(uManufacturer.ManufacturerGroups, comparer).ToList();
                        List<GXManufacturerGroup> addedManufacturerGroups = uManufacturer.ManufacturerGroups.Except(manufacturerGroups, comparer).ToList();
                        if (removedManufacturerGroups.Any())
                        {
                            RemoveManufacturersFromManufacturerGroup(uManufacturer.Id, removedManufacturerGroups);
                            update = true;
                        }
                        if (addedManufacturerGroups.Any())
                        {
                            AddManufacturerToManufacturerGroups(uManufacturer.Id, addedManufacturerGroups);
                            update = true;
                        }
                    }
                    GXManufacturer iManufacturer = await ReadAsync(user, uManufacturer.Id);
                    if (uManufacturer.Models != null)
                    {
                        var comparer = new UniqueComparer<GXDeviceModel, Guid>();
                        List<GXDeviceModel>? addedModels;
                        if (iManufacturer.Models == null)
                        {
                            //If new model is added.
                            AddModelsToManufacturer(uManufacturer, uManufacturer.Models);
                            update = true;
                        }
                        else
                        {
                            List<GXDeviceModel> removedModels = iManufacturer.Models.Except(uManufacturer.Models, comparer).ToList();
                            addedModels = uManufacturer.Models.Except(iManufacturer.Models, comparer).ToList();
                            if (removedModels != null && removedModels.Any())
                            {
                                RemoveModelsFromManufacturer(removedModels);
                                update = true;
                            }
                            if (addedModels.Any())
                            {
                                AddModelsToManufacturer(uManufacturer, addedModels);
                                update = true;
                            }
                            else
                            {
                                //Add new versions.
                                var comparer2 = new UniqueComparer<GXDeviceVersion, Guid>();
                                foreach (GXDeviceModel uModel in uManufacturer.Models)
                                {
                                    GXDeviceModel? iModel = iManufacturer.Models.Where(w => w.Id == uModel.Id).SingleOrDefault();
                                    if (iModel == null)
                                    {
                                        throw new Exception("Unknown device model.");
                                    }
                                    List<GXDeviceVersion>? removedVersions = null;
                                    if (iModel.Versions != null && uModel.Versions != null)
                                    {
                                        removedVersions = iModel.Versions.Except(uModel.Versions, comparer2).ToList();
                                    }
                                    List<GXDeviceVersion>? addedVersions = null;
                                    if (uModel.Versions != null)
                                    {
                                        if (iModel.Versions == null)
                                        {
                                            addedVersions = uModel.Versions;
                                        }
                                        else
                                        {
                                            addedVersions = uModel.Versions.Except(iModel.Versions, comparer2).ToList();
                                        }
                                    }
                                    if (removedVersions != null && removedVersions.Any())
                                    {
                                        RemoveVersionsFromModel(removedVersions);
                                        update = true;
                                    }
                                    if (addedVersions != null && addedVersions.Any())
                                    {
                                        AddVersionsToModel(iModel, addedVersions);
                                        update = true;
                                    }
                                    //Add new settings.
                                    if (uModel.Versions != null)
                                    {
                                        List<GXDeviceSettings> addedSettings = new List<GXDeviceSettings>();
                                        foreach (var uVersion in uModel.Versions)
                                        {
                                            if (uVersion.Id == Guid.Empty)
                                            {
                                                //If new version.
                                                AddVersionsToModel(iModel, new[] { uVersion });
                                            }
                                            if (uVersion.Settings != null)
                                            {
                                                foreach (var it in uVersion.Settings)
                                                {
                                                    if (it.Id == Guid.Empty)
                                                    {
                                                        addedSettings.Add(it);
                                                        update = true;
                                                    }
                                                }
                                                AddSettingsToVersion(uVersion, addedSettings);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (update)
                    {
                        updates[uManufacturer] = await GetUsersAsync(user, uManufacturer.Id);
                    }
                }
            }
            List<GXManufacturer> tmp = new List<GXManufacturer>();
            //Add updated manufacturers to same list.
            foreach (var it in updates)
            {
                tmp.Add(new GXManufacturer()
                {
                    Id = it.Key.Id,
                    Name = it.Key.Name
                });
            }
            //Users that are already notified.
            List<string> notified = new List<string>();
            foreach (var it in updates)
            {
                //Notify user only once.
                it.Value.RemoveAll(w => notified.Contains(w));
                if (it.Value.Any())
                {
                    await _eventsNotifier.ManufacturerUpdate(it.Value, tmp.ToArray());
                    notified.AddRange(it.Value);
                }
            }
            return list.ToArray();
        }


        /// <summary>
        /// Download and install device template from the GitHub.
        /// </summary>
        private async Task DownloadAndInstallDeviceTemplate(
            ClaimsPrincipal user,
            GXDeviceSettings settings,
            string templateName)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "C# App");
            //Define request data format  
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = await client.GetAsync(settings.Location);
            response.EnsureSuccessStatusCode();
            string xml = await response.Content.ReadAsStringAsync();
            List<GXDeviceTemplate> templates = ClientHelpers.ConvertToTemplates(null, xml);
            foreach (var template in templates)
            {
                template.Name = templateName;
            }
            await _deviceTemplateRepository.UpdateAsync(user, templates);
            foreach (var template in templates)
            {
                settings.Template = template;
            }
        }


        /// <summary>
        /// Map manufacturer group to user groups.
        /// </summary>
        /// <param name="manufacturerId">Manufacturer ID.</param>
        /// <param name="groups">Group IDs of the manufacturer groups where the manufacturer is added.</param>
        public void AddManufacturerToManufacturerGroups(Guid manufacturerId, IEnumerable<GXManufacturerGroup>? groups)
        {
            if (groups != null && groups.Any())
            {
                DateTime now = DateTime.Now;
                List<GXManufacturerGroupManufacturer> list = new();
                foreach (GXManufacturerGroup it in groups)
                {
                    list.Add(new GXManufacturerGroupManufacturer()
                    {
                        ManufacturerId = manufacturerId,
                        ManufacturerGroupId = it.Id,
                        CreationTime = now
                    });
                }
                _host.Connection.Insert(GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between manufacturer group and manufacturer.
        /// </summary>
        /// <param name="manufacturerId">Manufacturer ID.</param>
        /// <param name="groups">Group IDs of the manufacturer groups where the manufacturer is removed.</param>
        public void RemoveManufacturersFromManufacturerGroup(Guid manufacturerId, IEnumerable<GXManufacturerGroup> groups)
        {
            foreach (GXManufacturerGroup it in groups)
            {
                _host.Connection.Delete(GXDeleteArgs.Delete<GXManufacturerGroupManufacturer>(w => w.ManufacturerId == manufacturerId && w.ManufacturerGroupId == it.Id));
            }
        }

        /// <summary>
        /// Map models to manufacturer.
        /// </summary>
        /// <param name="manufacturer">Manufacturer.</param>
        /// <param name="models">Added models.</param>
        public void AddModelsToManufacturer(GXManufacturer manufacturer, IEnumerable<GXDeviceModel>? models)
        {
            if (models != null && models.Any())
            {
                DateTime now = DateTime.Now;
                foreach (GXDeviceModel it in models)
                {
                    it.CreationTime = now;
                    it.Manufacturer = manufacturer;
                }
                GXInsertArgs args = GXInsertArgs.InsertRange(models);
                args.Exclude<GXDeviceModel>(e => new { e.Updated, e.Removed, e.Versions });
                _host.Connection.Insert(args);
                foreach (GXDeviceModel model in models)
                {
                    AddVersionsToModel(model, model.Versions);
                }
            }
        }

        /// <summary>
        /// Remove map between manufacturer group and manufacturer.
        /// </summary>
        /// <param name="models">Removed models</param>
        public void RemoveModelsFromManufacturer(IEnumerable<GXDeviceModel> models)
        {
            DateTime now = DateTime.Now;
            foreach (GXDeviceModel it in models)
            {
                it.Removed = now;
            }
            GXUpdateArgs args = GXUpdateArgs.UpdateRange(models, c => c.Removed);
            _host.Connection.UpdateAsync(args);
        }

        /// <summary>
        /// Map versions to models.
        /// </summary>
        /// <param name="model">Model.</param>
        /// <param name="versions">Added versions.</param>
        public void AddVersionsToModel(GXDeviceModel model, IEnumerable<GXDeviceVersion>? versions)
        {
            if (versions != null && versions.Any())
            {
                DateTime now = DateTime.Now;
                foreach (GXDeviceVersion it in versions)
                {
                    it.CreationTime = now;
                    it.Model = model;
                }
                GXInsertArgs args = GXInsertArgs.InsertRange(versions);
                args.Exclude<GXDeviceVersion>(e => new { e.Updated, e.Removed, e.Settings });
                _host.Connection.Insert(args);
                foreach (GXDeviceVersion it in versions)
                {
                    AddSettingsToVersion(it, it.Settings);
                }
            }
        }


        /// <summary>
        /// Map settings to versions.
        /// </summary>
        /// <param name="version">Device version.</param>
        /// <param name="settings">Added settings.</param>
        public void AddSettingsToVersion(GXDeviceVersion version, IEnumerable<GXDeviceSettings>? settings)
        {
            if (settings != null && settings.Any())
            {
                DateTime now = DateTime.Now;
                foreach (GXDeviceSettings it in settings)
                {
                    it.CreationTime = now;
                    it.Version = version;
                    GXInsertArgs args = GXInsertArgs.Insert(it);
                    args.Exclude<GXDeviceSettings>(e => new { e.Updated, e.Removed });
                    _host.Connection.Insert(args);
                }
            }
        }

        /// <summary>
        /// Remove map between manufacturer group and manufacturer.
        /// </summary>
        /// <param name="versions">Removed versions</param>
        public void RemoveVersionsFromModel(IEnumerable<GXDeviceVersion> versions)
        {
            DateTime now = DateTime.Now;
            foreach (GXDeviceVersion it in versions)
            {
                it.Removed = now;
            }
            GXUpdateArgs args = GXUpdateArgs.UpdateRange(versions, c => c.Removed);
            _host.Connection.UpdateAsync(args);
        }

        /// <inheritdoc />
        public async Task<GXDeviceModel> ReadModelAsync(ClaimsPrincipal user, Guid id)
        {
            bool isAdmin = false;
            if (user != null)
            {
                isAdmin = user.IsInRole(GXRoles.Admin);
            }
            GXSelectArgs arg;
            if (user == null || isAdmin)
            {
                //Admin can see all the manufacturers.
                arg = GXSelectArgs.SelectAll<GXDeviceModel>();
                arg.Joins.AddLeftJoin<GXDeviceModel, GXDeviceVersion>(j => j.Id, j => j.Model);
                arg.Joins.AddInnerJoin<GXDeviceModel, GXManufacturer>(y => y.Manufacturer, x => x.Id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetManufacturersByUser(userId, id);
            }
            arg.Where.And<GXDeviceModel>(w => w.Id == id);
            arg.Columns.Add<GXManufacturer>(c => c.Id);
            arg.Columns.Add<GXDeviceVersion>();
            arg.Columns.Exclude<GXManufacturer>(e => e.Models);
            arg.Columns.Exclude<GXDeviceVersion>(e => e.Model);
            arg.Distinct = true;
            GXDeviceModel model = await _host.Connection.SingleOrDefaultAsync<GXDeviceModel>(arg);
            if (model == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            return model;
        }

        /// <inheritdoc />
        public async Task<GXDeviceVersion> ReadVersionAsync(ClaimsPrincipal user, Guid id)
        {
            bool isAdmin = false;
            if (user != null)
            {
                isAdmin = user.IsInRole(GXRoles.Admin);
            }
            GXSelectArgs arg;
            if (user == null || isAdmin)
            {
                //Admin can see all the manufacturers.
                arg = GXSelectArgs.SelectAll<GXDeviceVersion>(w => w.Id == id);
                arg.Joins.AddLeftJoin<GXDeviceVersion, GXDeviceModel>(j => j.Model, j => j.Id);
                arg.Joins.AddLeftJoin<GXDeviceModel, GXManufacturer>(y => y.Manufacturer, x => x.Id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetManufacturersByUser(userId, id);
            }
            arg.Columns.Add<GXDeviceModel>(c => c.Id);
            arg.Where.And<GXDeviceVersion>(w => w.Id == id);
            arg.Distinct = true;
            GXDeviceVersion version = await _host.Connection.SingleOrDefaultAsync<GXDeviceVersion>(arg);
            if (version == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            return version;
        }

        /// <inheritdoc />
        public async Task InstallAsync(ClaimsPrincipal user,
            IEnumerable<GXManufacturer>? manufacturers,
            IEnumerable<GXDeviceModel>? models,
            IEnumerable<GXDeviceVersion>? versions,
            IEnumerable<GXDeviceSettings>? settings)
        {
            DateTime now = DateTime.UtcNow;
            GXSelectArgs arg;
            if (user.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the manufacturers.
                arg = GXSelectArgs.SelectAll<GXManufacturer>();
                arg.Joins.AddLeftJoin<GXManufacturer, GXDeviceModel>(x => x.Id, y => y.Manufacturer);
                arg.Joins.AddLeftJoin<GXDeviceModel, GXDeviceVersion>(j => j.Id, j => j.Model);
                arg.Joins.AddLeftJoin<GXDeviceVersion, GXDeviceSettings>(j => j.Id, j => j.Version);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetManufacturersByUser(userId);
            }
            if (manufacturers != null && manufacturers.Any())
            {
                List<Guid> list = new List<Guid>();
                list.AddRange(manufacturers.Select(s => s.Id));
                arg.Where.And<GXManufacturer>(w => list.Contains(w.Id));
            }
            if (models != null && models.Any())
            {
                List<Guid> list = new List<Guid>();
                list.AddRange(models.Select(s => s.Id));
                arg.Where.And<GXDeviceModel>(w => list.Contains(w.Id));
            }
            if (versions != null && versions.Any())
            {
                List<Guid> list = new List<Guid>();
                list.AddRange(versions.Select(s => s.Id));
                arg.Where.And<GXDeviceVersion>(w => list.Contains(w.Id));
            }
            if (settings != null && settings.Any())
            {
                List<Guid> list = new List<Guid>();
                list.AddRange(settings.Select(s => s.Id));
                arg.Where.And<GXDeviceSettings>(w => list.Contains(w.Id));
            }
            arg.Where.And<GXDeviceSettings>(w => w.Template == null);
            arg.Columns.Add<GXDeviceModel>();
            arg.Columns.Exclude<GXDeviceModel>(e => e.Manufacturer);
            arg.Columns.Add<GXDeviceVersion>();
            arg.Columns.Exclude<GXDeviceVersion>(e => e.Model);
            arg.Columns.Add<GXDeviceSettings>();
            arg.Columns.Exclude<GXDeviceSettings>(e => e.Version);
            arg.Distinct = true;
            List<GXManufacturer> iManufacturers = await _host.Connection.SelectAsync<GXManufacturer>(arg);
            foreach (var manufacturer in iManufacturers)
            {
                if (manufacturer.Models != null)
                {
                    foreach (var model in manufacturer.Models)
                    {
                        if (model.Versions != null)
                        {
                            //Bind version to device template.
                            foreach (var version in model.Versions)
                            {
                                if (version.Settings != null)
                                {
                                    foreach (var it in version.Settings)
                                    {
                                        //Install device templates.
                                        await DownloadAndInstallDeviceTemplate(user, it, it.Name);
                                        it.InstallationTime = now;
                                        //Save device template ID to the database.
                                        GXUpdateArgs args = GXUpdateArgs.Update(it, c => new { c.Template, c.InstallationTime });
                                        await _host.Connection.UpdateAsync(args);
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
