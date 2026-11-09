// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Client.Pages.Agent;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Data;
using Gurux.DLMS.AMI.Server.Services;
using System.Net.Http.Headers;
using System.Security.Claims;


namespace Gurux.DLMS.AMI.Server.Repository
{

    /// <inheritdoc />
    public class ManufacturerRepository : IManufacturerRepository
    {

        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IUserRepository _userRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IManufacturerGroupRepository _manufacturerGroupRepository;
        private readonly IDeviceTemplateRepository _deviceTemplateRepository;
        private readonly NotificationCache _notificationCache;
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ManufacturerRepository(IAuthorizationService authorizationService,
            IDbConnectionFactory dbFactory,
            IUserRepository userRepository,
            IHttpContextAccessor httpContextAccessor,
            IServiceProvider serviceProvider,
            IManufacturerGroupRepository manufacturerGroupRepository,
            IDeviceTemplateRepository deviceTemplateRepository,
            NotificationCache notificationCache)
        {
            _dbFactory = dbFactory;
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            User = httpContextAccessor.HttpContext.User;
            _userRepository = userRepository;
            _serviceProvider = serviceProvider;
            _manufacturerGroupRepository = manufacturerGroupRepository;
            _deviceTemplateRepository = deviceTemplateRepository;
            _notificationCache = notificationCache;
            _authorizationService = authorizationService;
        }

        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXManufacturer), id, action, GXManufacturerPolicies.View, connection, transaction, notifications: _notificationCache);

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? manufacturerId, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXManufacturerPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByManufacturer(s => s.Id!,
                ServerHelpers.GetUserId(User), manufacturerId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (manufacturerId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXManufacturer), manufacturerId, token: cancellationToken);
                args.Where.And<GXUser>(u => members.Contains(u.Id!));
            }
            List<GXUser> users = await connection.SelectAsync<GXUser>(args);
            List<string> ret = [.. users.Select(s => s.Id!)];
            if (User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? manufacturerIds, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXManufacturerPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByManufacturers(s => s.Id!,
                ServerHelpers.GetUserId(User), manufacturerIds);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            List<GXUser> users = await connection.SelectAsync<GXUser>(args);
            List<string> ret = [.. users.Select(s => s.Id!)];
            if (User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(IEnumerable<Guid> manufacturers,
            bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            manufacturers = manufacturers.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXManufacturer, Guid>(_dbFactory, User, _serviceProvider, manufacturers,
        async () =>
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXManufacturerPolicies.Delete);
                GXSelectArgs arg = GXSelectArgs.Select<GXManufacturer>(a => a.Id, q => manufacturers.Contains(q.Id));
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                List<GXManufacturer> list = connection.Select<GXManufacturer>(arg);
                DateTime now = DateTime.Now;
                Dictionary<GXManufacturer, List<string>> updates = [];
                foreach (GXManufacturer it in list)
                {
                    it.Removed = now;
                    List<string> users = await GetNotifiedUsers(it.Id, NotificationAction.Remove, connection);
                    if (delete)
                    {
                        await connection.DeleteAsync(GXDeleteArgs.DeleteById<GXManufacturer>(it.Id), cancellationToken);
                    }
                    else
                    {
                        connection.Update(GXUpdateArgs.Update(it, q => q.Removed!));
                    }
                    updates[it] = users;
                }
                foreach (var it in updates)
                {
                    var users = it.Value;
                    if (users == null)
                    {
                        break;
                    }
                    GXManufacturer tmp = new() { Id = it.Key.Id };
                    _notificationCache.Add(users, new GXTarget(GXManufacturerPolicies.Delete, tmp.Id));
                }

            }
            catch (GXDatabaseException ex)
            {
                if (ex.ErrorType == DatabaseErrorType.Deadlock)
                {
                    //Max time is 5 seconds to avoid infinite retry when there is some other issue that cause deadlock and not just temporary deadlock.
                    CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(5));
                    try
                    {
                        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                        // If deadlock, wait max 1 second and retry.
                        await Task.Delay(1000, linkedCts.Token);
                        await DeleteAsync(manufacturers, delete, notify, linkedCts.Token);
                    }
                    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                    {
                        throw ex;
                    }
                }
                else
                {
                    throw;
                }
            }
        }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXManufacturer>> ListAsync(
            ListManufacturers? request,
            ListManufacturersResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXManufacturerPolicies.View);
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
                            arg.Joins.AddLeftJoin<GXManufacturer, GXDeviceModel>(x => x.Id, y => y.Manufacturer!);
                            arg.Joins.AddLeftJoin<GXDeviceModel, GXDeviceVersion>(j => j.Id, j => j.Model!);
                            arg.Joins.AddLeftJoin<GXDeviceVersion, GXDeviceSettings>(j => j.Id, j => j.Version!);
                            arg.Joins.AddLeftJoin<GXDeviceSettings, GXDeviceTemplate>(j => j.Template!, j => j.Id);
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
                if (request.Exclude?.Any() == true)
                {
                    arg.Where.And<GXManufacturer>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included?.Any() == true)
                {
                    arg.Where.And<GXManufacturer>(w => request.Included.Contains(w.Id));
                }
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXManufacturer>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXManufacturer>(q => q.CreationTime!);
                arg.Descending = true;
            }
            arg.Distinct = true;
            GXDateTimeFilters.Apply<GXManufacturer>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXManufacturer>(q => GXSql.Count(1));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = connection.SingleOrDefault<long>(total);
                    if (response.Count == 0)
                    {
                        //No need to retrieve data when none is available.
                        return [];
                    }
                }
                arg.Index = request.Index;
                arg.Count = request.Count;
            }
            if (request != null)
            {
                if (request?.Select != null && request.Select.Contains("Version"))
                {
                    //Get model versions
                    if (!joinAdded)
                    {
                        arg.Joins.AddLeftJoin<GXManufacturer, GXDeviceModel>(x => x.Id, y => y.Manufacturer!);
                        arg.Joins.AddLeftJoin<GXDeviceModel, GXDeviceVersion>(j => j.Id, j => j.Model!);
                        arg.Joins.AddLeftJoin<GXDeviceVersion, GXDeviceSettings>(j => j.Id, j => j.Version!);
                        arg.Joins.AddLeftJoin<GXDeviceSettings, GXDeviceTemplate>(j => j.Template!, j => j.Id);
                    }
                    arg.Columns.Add<GXDeviceModel>();
                    arg.Columns.Exclude<GXDeviceModel>(e => e.Manufacturer!);
                    arg.Columns.Add<GXDeviceVersion>();
                    arg.Columns.Exclude<GXDeviceVersion>(e => e.Model!);
                    arg.Columns.Add<GXDeviceSettings>();
                    arg.Columns.Exclude<GXDeviceSettings>(e => e.Version!);
                    //Select device template ID and name.
                    arg.Columns.Add<GXDeviceTemplate>(s => new { s.Id, s.Name });
                }
                if (request?.Select != null && request.Select.Contains(TargetType.Model))
                {
                    //Get models
                    if (!joinAdded)
                    {
                        arg.Joins.AddLeftJoin<GXManufacturer, GXDeviceModel>(x => x.Id, y => y.Manufacturer!);
                    }
                    arg.Columns.Add<GXDeviceModel>();
                    arg.Columns.Exclude<GXDeviceModel>(e => e.Manufacturer!);
                }
            }
            GXManufacturer[] manufacturers = [.. (await connection.SelectAsync<GXManufacturer>(arg))];
            if (response != null)
            {
                response.Manufacturers = manufacturers;
                response.Count = manufacturers.Length;
            }
            return manufacturers;
        }

        /// <inheritdoc />
        public async Task<GXManufacturer> ReadAsync(Guid id, Expression<Func<GXManufacturer, object>>? columns = null,
            CancellationToken cancellationToken = default)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXManufacturerPolicies.View);
                GXSelectArgs arg;
                if (User.IsInRole(GXRoles.Admin))
                {
                    //Admin can see all the manufacturers.
                    arg = GXSelectArgs.SelectAll<GXManufacturer>();
                    arg.Joins.AddLeftJoin<GXManufacturer, GXDeviceModel>(x => x.Id, y => y.Manufacturer!);
                    arg.Joins.AddLeftJoin<GXDeviceModel, GXDeviceVersion>(j => j.Id, j => j.Model!);
                    arg.Joins.AddLeftJoin<GXDeviceVersion, GXDeviceSettings>(j => j.Id, j => j.Version!);
                    arg.Joins.AddLeftJoin<GXDeviceSettings, GXDeviceTemplate>(j => j.Template!, j => j.Id);
                }
                else
                {
                    string userId = ServerHelpers.GetUserId(User);
                    arg = GXQuery.GetManufacturersByUser(s => "*", userId, id);
                }
                arg.Where.And<GXManufacturer>(w => w.Id == id);
                arg.Columns.Add<GXDeviceModel>();
                arg.Columns.Exclude<GXDeviceModel>(e => e.Manufacturer!);
                arg.Columns.Add<GXDeviceVersion>();
                arg.Columns.Exclude<GXDeviceVersion>(e => e.Model!);
                arg.Columns.Add<GXDeviceSettings>();
                arg.Columns.Exclude<GXDeviceSettings>(e => e.Version!);
                arg.Columns.Add<GXDeviceTemplate>(s => new { s.Id, s.Name });
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                GXManufacturer? manufacturer = await connection.SingleOrDefaultAsync<GXManufacturer>(arg) ?? throw new GXAmiForbiddenException(id.ToString());
                arg = GXSelectArgs.SelectAll<GXManufacturerGroup>();
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXManufacturerGroupManufacturer, GXManufacturerGroup>(x => x.ManufacturerGroupId, y => y.Id);
                arg.Where.And<GXManufacturerGroupManufacturer>(w => w.ManufacturerId == id);
                manufacturer.ManufacturerGroups = await connection.SelectAsync<GXManufacturerGroup>(arg);
                manufacturer.ProfilePicture = await ReadManufacturerPictureAsync(connection, id, cancellationToken);
                manufacturer.Creator = await ReadManufacturerCreatorAsync(connection, id, cancellationToken);
                foreach (var model in manufacturer.Models ?? [])
                {
                    model.ProfilePicture = await ReadModelPictureAsync(connection, model.Id, cancellationToken);
                    model.Creator = await ReadModelCreatorAsync(connection, model.Id, cancellationToken);
                }
                return manufacturer;

            }
            catch (GXDatabaseException ex)
            {
                if (ex.ErrorType == DatabaseErrorType.Deadlock)
                {
                    //Max time is 5 seconds to avoid infinite retry when there is some other issue that cause deadlock and not just temporary deadlock.
                    CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(5));
                    try
                    {
                        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                        // If deadlock, wait max 1 second and retry.
                        await Task.Delay(1000, linkedCts.Token);
                        return await ReadAsync(id, columns, linkedCts.Token);
                    }
                    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                    {
                        throw ex;
                    }
                }
                else
                {
                    throw;
                }
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXManufacturer> manufacturers,
            Expression<Func<GXManufacturer, object>>? columns, CancellationToken cancellationToken = default)
        {
            manufacturers = manufacturers.ToArray();
            return await GXHistoryMutation.RunAsync<GXManufacturer, IEnumerable<Guid>>(_dbFactory, User, _serviceProvider, manufacturers,
        async () =>
        {
            manufacturers = manufacturers.ToArray();
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXManufacturerPolicies.Add, GXManufacturerPolicies.Edit);
                DateTime now = DateTime.Now;
                List<Guid> list = [];
                List<(List<string> Users, GXTarget Target)> notifications = [];
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                // Validate every image before writing any catalog references. Policy readers use their own commands.
                var originals = new Dictionary<Guid, GXManufacturer>();
                foreach (var manufacturer in manufacturers)
                {
                    await _authorizationService.EnsureAnyPolicyAsync(User, manufacturer.Id == Guid.Empty ? GXManufacturerPolicies.Add : GXManufacturerPolicies.Edit);
                    if (string.IsNullOrEmpty(manufacturer.Name))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    if (!manufacturer.Template.GetValueOrDefault(false) &&
                        (manufacturer.ManufacturerGroups == null || manufacturer.ManufacturerGroups.Count == 0))
                    {
                        ListManufacturerGroups request = new()
                        {
                            Filter = new GXManufacturerGroup() { Default = true }
                        };
                        manufacturer.ManufacturerGroups =
                        [
                            .. await _manufacturerGroupRepository.ListAsync(request,
                            null, CancellationToken.None),
                    ];
                        if (manufacturer.ManufacturerGroups.Count == 0)
                        {
                            throw new ArgumentException(Properties.Resources.TargetMustBelongToOneGroup);
                        }
                    }

                    if (manufacturer.Id == Guid.Empty || columns == null || ServerHelpers.Contains(columns, nameof(GXManufacturer.ProfilePicture)))
                        {
                        await GXProfilePictures.ValidateAsync(connection, _authorizationService, User, manufacturer.ProfilePicture?.Id, cancellationToken);
                    }
                    foreach (var model in manufacturer.Models ?? [])
                        {
                        await GXProfilePictures.ValidateAsync(connection, _authorizationService, User, model.ProfilePicture?.Id, cancellationToken);
                    }
                    if (manufacturer.Id != Guid.Empty)
                        {
                        originals[manufacturer.Id] = await ReadAsync(manufacturer.Id, cancellationToken: cancellationToken);
                    }
                }
                await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
                foreach (GXManufacturer uManufacturer in manufacturers)
                {

                    if (uManufacturer.Id == Guid.Empty)
                    {
                        uManufacturer.CreationTime = now;
                        GXInsertArgs args = GXInsertArgs.Insert(uManufacturer);
                        args.Exclude<GXManufacturer>(q => new
                        {
                            q.ManufacturerGroups,
                            q.Models
                        });
                        connection.Insert(transaction, args);
                        list.Add(uManufacturer.Id);
                        AddModelsToManufacturer(connection, transaction, uManufacturer, uManufacturer.Models);
                        AddManufacturerToManufacturerGroups(connection, transaction, uManufacturer.Id, uManufacturer.ManufacturerGroups);
                        notifications.Add((await GetNotifiedUsers(uManufacturer.Id, NotificationAction.Add, connection, transaction),
                            new GXTarget(GXManufacturerPolicies.Add, uManufacturer.Id, uManufacturer.Name)));

                    }
                    else
                    {
                        GXSelectArgs m = GXSelectArgs.Select<GXManufacturer>(q => q.ConcurrencyStamp!, where => where.Id == uManufacturer.Id);
                        string? updated = connection.SingleOrDefault<string>(transaction, m);
                        if (!string.IsNullOrEmpty(updated) && updated != uManufacturer.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }
                        uManufacturer.Updated = now;
                        uManufacturer.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs args = GXUpdateArgs.Update(uManufacturer, columns);
                        args.Exclude<GXManufacturer>(q => new
                        {
                            q.CreationTime,
                            q.ManufacturerGroups,
                            q.Models
                        });
                        connection.Update(transaction, args);
                        //Map manufacturer groups to manufacturer.
                        List<GXManufacturerGroup> manufacturerGroups;
                        manufacturerGroups = originals[uManufacturer.Id].ManufacturerGroups ?? [];
                        if (uManufacturer.ManufacturerGroups != null)
                        {
                            var comparer = new UniqueComparer<GXManufacturerGroup, Guid>();
                            List<GXManufacturerGroup> removedManufacturerGroups = [.. manufacturerGroups.Except(uManufacturer.ManufacturerGroups, comparer)];
                            List<GXManufacturerGroup> addedManufacturerGroups = [.. uManufacturer.ManufacturerGroups.Except(manufacturerGroups, comparer)];
                            if (removedManufacturerGroups.Count != 0)
                            {
                                RemoveManufacturersFromManufacturerGroup(connection, transaction, uManufacturer.Id, removedManufacturerGroups);
                            }
                            if (addedManufacturerGroups.Count != 0)
                            {
                                AddManufacturerToManufacturerGroups(connection, transaction, uManufacturer.Id, addedManufacturerGroups);
                            }
                        }
                        GXManufacturer iManufacturer = originals[uManufacturer.Id];
                        if (uManufacturer.Models != null)
                        {
                            foreach (var model in uManufacturer.Models.Where(m => iManufacturer.Models?.Any(existing => existing.Id == m.Id) == true))
                                {
                                connection.Update(transaction, GXUpdateArgs.Update(model, m => m.ProfilePicture!));
                            }
                            var comparer = new UniqueComparer<GXDeviceModel, Guid>();
                            List<GXDeviceModel>? addedModels;
                            if (iManufacturer.Models == null)
                            {
                                //If new model is added.
                                AddModelsToManufacturer(connection, transaction, uManufacturer, uManufacturer.Models);
                            }
                            else
                            {
                                List<GXDeviceModel> removedModels = [.. iManufacturer.Models.Except(uManufacturer.Models, comparer)];
                                addedModels = [.. uManufacturer.Models.Except(iManufacturer.Models, comparer)];
                                if (removedModels != null && removedModels.Count != 0)
                                {
                                    RemoveModelsFromManufacturer(connection, transaction, removedModels);
                                }
                                if (addedModels.Count != 0)
                                {
                                    AddModelsToManufacturer(connection, transaction, uManufacturer, addedModels);
                                }
                                else
                                {
                                    //Add new versions.
                                    var comparer2 = new UniqueComparer<GXDeviceVersion, Guid>();
                                    foreach (GXDeviceModel uModel in uManufacturer.Models)
                                    {
                                        GXDeviceModel? iModel = iManufacturer.Models.Where(w => w.Id == uModel.Id).SingleOrDefault() ?? throw new Exception("Unknown device model.");
                                        List<GXDeviceVersion>? removedVersions = null;
                                        if (iModel.Versions != null && uModel.Versions != null)
                                        {
                                            removedVersions = [.. iModel.Versions.Except(uModel.Versions, comparer2)];
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
                                                addedVersions = [.. uModel.Versions.Except(iModel.Versions, comparer2)];
                                            }
                                        }
                                        if (removedVersions != null && removedVersions.Count != 0)
                                        {
                                            RemoveVersionsFromModel(connection, transaction, removedVersions);
                                        }
                                        if (addedVersions != null && addedVersions.Count != 0)
                                        {
                                            AddVersionsToModel(connection, transaction, iModel, addedVersions);
                                        }
                                        //Add new settings.
                                        if (uModel.Versions != null)
                                        {
                                            List<GXDeviceSettings> addedSettings = [];
                                            foreach (var uVersion in uModel.Versions)
                                            {
                                                if (uVersion.Id == Guid.Empty)
                                                {
                                                    //If new version.
                                                    AddVersionsToModel(connection, transaction, iModel, [uVersion]);
                                                }
                                                if (uVersion.Settings != null)
                                                {
                                                    foreach (var it in uVersion.Settings)
                                                    {
                                                        if (it.Id == Guid.Empty)
                                                        {
                                                            addedSettings.Add(it);
                                                        }
                                                        else if (it.ProfileId != null)
                                                        {
                                                            // Refresh catalog metadata without replacing an installed template.
                                                            connection.Update(transaction, GXUpdateArgs.Update(it, s => new { s.ProfileId, s.Name, s.Location, s.Settings }));
                                                        }
                                                    }
                                                    AddSettingsToVersion(connection, transaction, uVersion, addedSettings);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        notifications.Add((await GetNotifiedUsers(uManufacturer.Id, NotificationAction.Edit, connection, transaction),
                            new GXTarget(GXManufacturerPolicies.Edit, uManufacturer.Id, uManufacturer.Name)));
                    }
                }
                await transaction.CommitAsync(cancellationToken);
                foreach (var notification in notifications)
                    {
                    _notificationCache.Add(notification.Users, notification.Target);
                }
                return [.. list];

            }
            catch (GXDatabaseException ex)
            {
                if (ex.ErrorType == DatabaseErrorType.Deadlock)
                {
                    //Max time is 5 seconds to avoid infinite retry when there is some other issue that cause deadlock and not just temporary deadlock.
                    CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(5));
                    try
                    {
                        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                        // If deadlock, wait max 1 second and retry.
                        await Task.Delay(1000, linkedCts.Token);
                        return await UpsertAsync(manufacturers, columns, linkedCts.Token);
                    }
                    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                    {
                        throw ex;
                    }
                }
                else
                {
                    throw;
                }
            }
        }, cancellationToken);
        }


        /// <summary>
        /// Download and install device template from the GitHub.
        /// </summary>
        private async Task DownloadAndInstallDeviceTemplate(
            GXDeviceSettings settings,
            string templateName)
        {
            HttpClient client = new();
            client.DefaultRequestHeaders.Add("User-Agent", "C# App");
            //Define request data format  
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = await client.GetAsync(settings.Location);
            response.EnsureSuccessStatusCode();
            string xml = await response.Content.ReadAsStringAsync();
            List<GXDeviceTemplate> templates = settings.ProfileId != null
                ? Gurux.DLMS.AMI.Update.GXManufacturerIndex.ParseProfiles(xml, templateName)
                : ClientHelpers.ConvertToTemplates(null, xml);
            foreach (var template in templates)
            {
                template.Name = templateName;
            }
            await _deviceTemplateRepository.UpsertAsync(templates);
            foreach (var template in templates)
            {
                settings.Template = template;
            }
        }


        /// <summary>
        /// Map manufacturer group to user groups.
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Catalog write transaction.</param>
        /// </summary>
        /// <param name="manufacturerId">Manufacturer ID.</param>
        /// <param name="groups">Group IDs of the manufacturer groups where the manufacturer is added.</param>
        private static void AddManufacturerToManufacturerGroups(GXDbConnection connection, IDbTransaction transaction,
            Guid manufacturerId, IEnumerable<GXManufacturerGroup>? groups)
        {
            if (groups != null && groups.Any())
            {
                DateTime now = DateTime.Now;
                List<GXManufacturerGroupManufacturer> list = [];
                foreach (GXManufacturerGroup it in groups)
                {
                    list.Add(new GXManufacturerGroupManufacturer()
                    {
                        ManufacturerId = manufacturerId,
                        ManufacturerGroupId = it.Id,
                        CreationTime = now
                    });
                }
                connection.Insert(transaction, GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between manufacturer group and manufacturer.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Catalog write transaction.</param>
        /// <param name="manufacturerId">Manufacturer ID.</param>
        /// <param name="groups">Group IDs of the manufacturer groups where the manufacturer is removed.</param>
        private static void RemoveManufacturersFromManufacturerGroup(GXDbConnection connection, IDbTransaction transaction,
            Guid manufacturerId, IEnumerable<GXManufacturerGroup> groups)
        {
            foreach (GXManufacturerGroup it in groups)
            {
                connection.Delete(transaction, GXDeleteArgs.Delete<GXManufacturerGroupManufacturer>(w => w.ManufacturerId == manufacturerId && w.ManufacturerGroupId == it.Id));
            }
        }

        /// <summary>
        /// Map models to manufacturer.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Catalog write transaction.</param>
        /// <param name="manufacturer">Manufacturer.</param>
        /// <param name="models">Added models.</param>
        private static void AddModelsToManufacturer(GXDbConnection connection, IDbTransaction transaction,
            GXManufacturer manufacturer, IEnumerable<GXDeviceModel>? models)
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
                connection.Insert(transaction, args);
                foreach (GXDeviceModel model in models)
                {
                    AddVersionsToModel(connection, transaction, model, model.Versions);
                }
            }
        }

        /// <summary>
        /// Remove map between manufacturer group and manufacturer.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Catalog write transaction.</param>
        /// <param name="models">Removed models</param>
        private static void RemoveModelsFromManufacturer(GXDbConnection connection, IDbTransaction transaction,
            IEnumerable<GXDeviceModel> models)
        {
            DateTime now = DateTime.Now;
            foreach (GXDeviceModel it in models)
            {
                it.Removed = now;
            }
            GXUpdateArgs args = GXUpdateArgs.UpdateRange(models, c => c.Removed!);
            connection.Update(transaction, args);
        }

        /// <summary>
        /// Map versions to models.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Catalog write transaction.</param>
        /// <param name="model">Model.</param>
        /// <param name="versions">Added versions.</param>
        private static void AddVersionsToModel(GXDbConnection connection, IDbTransaction transaction,
            GXDeviceModel model, IEnumerable<GXDeviceVersion>? versions)
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
                connection.Insert(transaction, args);
                foreach (GXDeviceVersion it in versions)
                {
                    AddSettingsToVersion(connection, transaction, it, it.Settings);
                }
            }
        }


        /// <summary>
        /// Map settings to versions.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Catalog write transaction.</param>
        /// <param name="version">Device version.</param>
        /// <param name="settings">Added settings.</param>
        private static void AddSettingsToVersion(GXDbConnection connection, IDbTransaction transaction,
            GXDeviceVersion version, IEnumerable<GXDeviceSettings>? settings)
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
                    connection.Insert(transaction, args);
                }
            }
        }

        /// <summary>
        /// Remove map between manufacturer group and manufacturer.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Catalog write transaction.</param>
        /// <param name="versions">Removed versions</param>
        private static void RemoveVersionsFromModel(GXDbConnection connection, IDbTransaction transaction,
            IEnumerable<GXDeviceVersion> versions)
        {
            DateTime now = DateTime.Now;
            foreach (GXDeviceVersion it in versions)
            {
                it.Removed = now;
            }
            GXUpdateArgs args = GXUpdateArgs.UpdateRange(versions, c => c.Removed!);
            connection.Update(transaction, args);
        }

        private static async Task<GXAppearance?> ReadManufacturerPictureAsync(GXDbConnection connection, Guid id, CancellationToken token)
        {
            var query = GXSelectArgs.Select<GXAppearance>(a => a.Id);
            query.Joins.AddInnerJoin<GXManufacturer, GXAppearance>(m => m.ProfilePicture!, a => a.Id);
            query.Where.And<GXManufacturer>(m => m.Id == id);
            return await connection.SingleOrDefaultAsync<GXAppearance>(query, token);
        }

        private static async Task<GXUser?> ReadManufacturerCreatorAsync(GXDbConnection connection, Guid id, CancellationToken token)
        {
            var query = GXSelectArgs.Select<GXUser>(u => u.Id!);
            query.Joins.AddInnerJoin<GXManufacturer, GXUser>(m => m.Creator!, u => u.Id!);
            query.Where.And<GXManufacturer>(m => m.Id == id);
            return await connection.SingleOrDefaultAsync<GXUser>(query, token);
        }

        private static async Task<GXUser?> ReadModelCreatorAsync(GXDbConnection connection, Guid id, CancellationToken token)
        {
            var query = GXSelectArgs.Select<GXUser>(u => u.Id!);
            query.Joins.AddInnerJoin<GXDeviceModel, GXUser>(m => m.Creator!, u => u.Id!);
            query.Where.And<GXDeviceModel>(m => m.Id == id);
            return await connection.SingleOrDefaultAsync<GXUser>(query, token);
        }

        private static async Task<GXAppearance?> ReadModelPictureAsync(GXDbConnection connection, Guid id, CancellationToken token)
        {
            var query = GXSelectArgs.Select<GXAppearance>(a => a.Id);
            query.Joins.AddInnerJoin<GXDeviceModel, GXAppearance>(m => m.ProfilePicture!, a => a.Id);
            query.Where.And<GXDeviceModel>(m => m.Id == id);
            return await connection.SingleOrDefaultAsync<GXAppearance>(query, token);
        }

        /// <inheritdoc />
        public async Task<GXDeviceModel> ReadModelAsync(Guid id, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXManufacturerPolicies.View);
            bool isAdmin = User.IsInRole(GXRoles.Admin);
            GXSelectArgs arg;
            if (isAdmin)
            {
                //Admin can see all the manufacturers.
                arg = GXSelectArgs.SelectAll<GXDeviceModel>();
                arg.Joins.AddLeftJoin<GXDeviceModel, GXDeviceVersion>(j => j.Id, j => j.Model!);
                arg.Joins.AddInnerJoin<GXDeviceModel, GXManufacturer>(y => y.Manufacturer!, x => x.Id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetManufacturersByUser(s => "*", userId, id);
            }
            arg.Where.And<GXDeviceModel>(w => w.Id == id);
            arg.Columns.Add<GXManufacturer>(c => c.Id);
            arg.Columns.Add<GXDeviceVersion>();
            arg.Columns.Exclude<GXManufacturer>(e => e.Models!);
            arg.Columns.Exclude<GXDeviceVersion>(e => e.Model!);
            arg.Distinct = true;
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            GXDeviceModel? model = await connection.SingleOrDefaultAsync<GXDeviceModel>(arg) ?? throw new GXAmiForbiddenException(id.ToString());
            model.ProfilePicture = await ReadModelPictureAsync(connection, id, cancellationToken);
            model.Creator = await ReadModelCreatorAsync(connection, id, cancellationToken);
            return model;
        }

        /// <inheritdoc />
        public async Task<GXDeviceVersion> ReadVersionAsync(Guid id, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXManufacturerPolicies.View);
            GXSelectArgs arg;
            if (User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the manufacturers.
                arg = GXSelectArgs.SelectAll<GXDeviceVersion>(w => w.Id == id);
                arg.Joins.AddLeftJoin<GXDeviceVersion, GXDeviceModel>(j => j.Model!, j => j.Id);
                arg.Joins.AddLeftJoin<GXDeviceModel, GXManufacturer>(y => y.Manufacturer!, x => x.Id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetManufacturersByUser(s => "*", userId, id);
            }
            arg.Columns.Add<GXDeviceModel>(c => c.Id);
            arg.Where.And<GXDeviceVersion>(w => w.Id == id);
            arg.Distinct = true;
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            GXDeviceVersion? version = await connection.SingleOrDefaultAsync<GXDeviceVersion>(arg) ?? throw new GXAmiForbiddenException(id.ToString());
            return version;
        }

        /// <inheritdoc />
        public async Task InstallAsync(
            IEnumerable<GXManufacturer>? manufacturers,
            IEnumerable<GXDeviceModel>? models,
            IEnumerable<GXDeviceVersion>? versions,
            IEnumerable<GXDeviceSettings>? settings, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXManufacturerPolicies.Add);
            DateTime now = DateTime.UtcNow;
            GXSelectArgs arg;
            if (User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the manufacturers.
                arg = GXSelectArgs.SelectAll<GXManufacturer>();
                arg.Joins.AddLeftJoin<GXManufacturer, GXDeviceModel>(x => x.Id, y => y.Manufacturer!);
                arg.Joins.AddLeftJoin<GXDeviceModel, GXDeviceVersion>(j => j.Id, j => j.Model!);
                arg.Joins.AddLeftJoin<GXDeviceVersion, GXDeviceSettings>(j => j.Id, j => j.Version!);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetManufacturersByUser(s => "*", userId);
            }
            if (manufacturers != null && manufacturers.Any())
            {
                List<Guid> list = [.. manufacturers.Select(s => s.Id)];
                arg.Where.And<GXManufacturer>(w => list.Contains(w.Id));
            }
            if (models != null && models.Any())
            {
                List<Guid> list = [.. models.Select(s => s.Id)];
                arg.Where.And<GXDeviceModel>(w => list.Contains(w.Id));
            }
            if (versions != null && versions.Any())
            {
                List<Guid> list = [.. versions.Select(s => s.Id)];
                arg.Where.And<GXDeviceVersion>(w => list.Contains(w.Id));
            }
            if (settings != null && settings.Any())
            {
                List<Guid> list = [.. settings.Select(s => s.Id)];
                arg.Where.And<GXDeviceSettings>(w => list.Contains(w.Id));
            }
            arg.Where.And<GXDeviceSettings>(w => w.Template == null);
            arg.Columns.Add<GXDeviceModel>();
            arg.Columns.Exclude<GXDeviceModel>(e => e.Manufacturer!);
            arg.Columns.Add<GXDeviceVersion>();
            arg.Columns.Exclude<GXDeviceVersion>(e => e.Model!);
            arg.Columns.Add<GXDeviceSettings>();
            arg.Columns.Exclude<GXDeviceSettings>(e => e.Version!);
            arg.Distinct = true;
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            List<GXManufacturer> iManufacturers = await connection.SelectAsync<GXManufacturer>(arg);
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
                                        await DownloadAndInstallDeviceTemplate(it, it.Name ?? throw new ArgumentException("Device template name is missing."));
                                        it.InstallationTime = now;
                                        //Save device template ID to the database.
                                        GXUpdateArgs args = GXUpdateArgs.Update(it, c => new { c.Template, c.InstallationTime });
                                        await connection.UpdateAsync(args, cancellationToken);
                                    }
                                }
                            }
                        }
                    }
                }
                if ((manufacturer.Models ?? []).SelectMany(model => model.Versions ?? [])
                    .SelectMany(version => version.Settings ?? []).Any(settings => settings.Template != null && settings.InstallationTime != null))
                {
                    manufacturer.ProfilesInstalled = true;
                    manufacturer.Version ??= manufacturer.AvailableVersion;
                    await connection.UpdateAsync(GXUpdateArgs.Update(manufacturer,
                        m => new { m.ProfilesInstalled, m.Version }), cancellationToken);
                }
            }
        }
    }
}
