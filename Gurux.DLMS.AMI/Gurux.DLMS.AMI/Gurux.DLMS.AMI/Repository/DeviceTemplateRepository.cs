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

using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json;

namespace Gurux.DLMS.AMI.Server.Repository
{

    /// <inheritdoc />
    public class DeviceTemplateRepository : IDeviceTemplateRepository
    {

        /// <summary>Principal whose identity and claims govern repository access.</summary>
        private readonly ClaimsPrincipal _user;
        /// <summary>Creates database connections for repository queries and mutations.</summary>
        private readonly IDbConnectionFactory _dbFactory;
        /// <summary>Provides persistence and access checks for user records.</summary>
        private readonly IUserRepository _userRepository;
        /// <summary>Resolves services needed by repository operations.</summary>
        private readonly IServiceProvider _serviceProvider;
        /// <summary>Provides persistence and access checks for device template group records.</summary>
        private readonly IDeviceTemplateGroupRepository _deviceTemplateGroupRepository;
        /// <summary>Performs cryptographic operations required by key management.</summary>
        private readonly IGXAmiCryptoService _cryproService;
        /// <summary>Publishes change notifications to connected users after mutations.</summary>
        private readonly NotificationCache _notificationCache;
        /// <summary>Evaluates policies before accessing protected data.</summary>
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DeviceTemplateRepository(
            IAuthorizationService authorizationService,
            IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor,
            IServiceProvider serviceProvider,
            IUserRepository userRepository,
            IDeviceTemplateGroupRepository deviceTemplateGroupRepository,
             IGXAmiCryptoService cryproService,
             NotificationCache notificationCache)
        {
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            _user = httpContextAccessor.HttpContext.User;
            _dbFactory = dbFactory;
            _serviceProvider = serviceProvider;
            _userRepository = userRepository;
            _deviceTemplateGroupRepository = deviceTemplateGroupRepository;
            _cryproService = cryproService;
            _notificationCache = notificationCache;
            _authorizationService = authorizationService;
        }

        /// <summary>Resolves users authorized to receive notifications for the affected entity and change action.</summary>
        /// <param name="id">Identifier of the affected record.</param>
        /// <param name="action">Change action used to choose notification recipients.</param>
        /// <param name="connection">Existing database connection to reuse, when supplied.</param>
        /// <param name="transaction">Transaction in which the query or mutation participates.</param>
        /// <returns>User identifiers selected for delivery of the change notification.</returns>
        internal Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXDeviceTemplate), id, action, GXDeviceTemplatePolicies.View, connection, transaction, notifications: _notificationCache);

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? templateId, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(_user, GXDeviceTemplatePolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByDeviceTemplate(s => s.Id!,
                ServerHelpers.GetUserId(_user), templateId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (templateId != null)
            {
                var members = await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXDeviceTemplate), templateId, token: cancellationToken);
                args.Where.And<GXUser>(u => members.Contains(u.Id!));
            }
            List<GXUser> users = await connection.SelectAsync<GXUser>(args);
            List<string> ret = [.. users.Select(s => s.Id!)];
            if (_user != null && _user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? templateIds, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(_user, GXDeviceTemplatePolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByDeviceTemplates(s => s.Id!,
                ServerHelpers.GetUserId(_user), templateIds);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            List<GXUser> users = await connection.SelectAsync<GXUser>(args);
            List<string> ret = [.. users.Select(s => s.Id!)];
            if (_user != null && _user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(IEnumerable<Guid> deviceTemplates,
            bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            deviceTemplates = [.. deviceTemplates];
            await GXHistoryMutation.RunDeleteAsync<GXDeviceTemplate, Guid>(_dbFactory, _user, _serviceProvider, deviceTemplates,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(_user, GXDeviceTemplatePolicies.Delete);
            GXSelectArgs arg = GXSelectArgs.Select<GXDeviceTemplate>(a => a.Id, q => deviceTemplates.Contains(q.Id));
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            List<GXDeviceTemplate> list = connection.Select<GXDeviceTemplate>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXDeviceTemplate, List<string>> updates = [];
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                if (!delete)
                {
                    foreach (GXDeviceTemplate it in list)
                    {
                        List<string> users = await GetNotifiedUsers(it.Id, NotificationAction.Remove, connection, transaction);
                        it.Removed = now;
                        connection.Update(transaction, GXUpdateArgs.Update(it, q => q.Removed!));
                        updates[it] = users;
                    }
                }
                else
                {
                    foreach (GXDeviceTemplate it in list)
                    {
                        updates[it] = await GetNotifiedUsers(it.Id, NotificationAction.Remove, connection, transaction);
                    }
                    GXDeleteArgs args1;
                    //////////////////////////////////////////////////////
                    //It's faster to remove atribute templates, object templates and device templates with own query than removing just device templates.
                    //////////////////////////////////////////////////////
                    //Delete attributes.
                    arg = GXSelectArgs.Select<GXObjectTemplate>(a => a.Id);
                    arg.Joins.AddInnerJoin<GXObjectTemplate, GXDeviceTemplate>(j => j.DeviceTemplate!, j => j.Id);
                    arg.Where.And<GXDeviceTemplate>(q => deviceTemplates.Contains(q.Id));
                    args1 = GXDeleteArgs.Delete<GXAttributeTemplate>(w => GXSql.Exists<GXAttributeTemplate, GXObjectTemplate>(j => j.ObjectTemplate!, j => j.Id, arg));
                    await connection.DeleteAsync(transaction, args1, cancellationToken);
                    //Delete objects.
                    arg = GXSelectArgs.Select<GXDeviceTemplate>(a => a.Id, q => deviceTemplates.Contains(q.Id));
                    args1 = GXDeleteArgs.Delete<GXObjectTemplate>(w => GXSql.Exists<GXObjectTemplate, GXDeviceTemplate>(j => j.DeviceTemplate!, j => j.Id, arg));
                    await connection.DeleteAsync(transaction, args1, cancellationToken);
                    //Delete device templates
                    var args = GXDeleteArgs.Delete<GXDeviceTemplate>(q => deviceTemplates.Contains(q.Id));
                    await connection.DeleteAsync(transaction, args, cancellationToken);
                }
                await transaction.CommitAsync(cancellationToken);
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
                        await DeleteAsync(deviceTemplates, delete, notify, linkedCts.Token);
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
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
            foreach (var it in updates)
            {
                _notificationCache.Add(it.Value,
                    new GXTarget(GXDeviceTemplatePolicies.Delete, it.Key.Id, it.Key.Name));
            }
        }, cancellationToken);
        }

        /// <summary>
        /// Update manufacturers.
        /// </summary>
        /// <param name="templates"></param>
        /// <returns></returns>
        private async Task UpdateManufacturers(GXDeviceTemplate[] templates)
        {
            if (templates.Length > 0)
            {
                //Client wants to know the device settings of the manufacturer.
                Guid[] ids = [.. templates.Select(s => s.Id)];
                GXSelectArgs arg = GXSelectArgs.SelectAll<GXManufacturer>();
                arg.Columns.Add<GXDeviceModel>();
                arg.Columns.Add<GXDeviceVersion>();
                arg.Columns.Add<GXDeviceSettings>();
                arg.Columns.Add<GXDeviceTemplate>(s => s.Id);
                arg.Joins.AddInnerJoin<GXManufacturer, GXDeviceModel>(j => j.Id, j => j.Manufacturer!);
                arg.Joins.AddInnerJoin<GXDeviceModel, GXDeviceVersion>(j => j.Id, j => j.Model!);
                arg.Joins.AddInnerJoin<GXDeviceVersion, GXDeviceSettings>(j => j.Id, j => j.Version!);
                arg.Joins.AddInnerJoin<GXDeviceSettings, GXDeviceTemplate>(j => j.Template!, j => j.Id);
                arg.Where.And<GXDeviceTemplate>(w => GXSql.In(w.Id, ids));
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                GXManufacturer[] manufacturers = [.. (await connection.SelectAsync<GXManufacturer>(arg))];
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
                                                    GXDeviceVersion ver = new()
                                                    {
                                                        Id = version.Id,
                                                        Name = version.Name
                                                    };
                                                    GXDeviceModel model2 = new()
                                                    {
                                                        Id = model.Id,
                                                        Name = model.Name,
                                                        Versions = []
                                                    };
                                                    model2.Versions.Add(ver);
                                                    target.Manufacturer = new GXManufacturer()
                                                    {
                                                        Id = manufacturer.Id,
                                                        Name = manufacturer.Name,
                                                        Models = []
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
        public async Task<IEnumerable<GXDeviceTemplate>> ListAsync(
            ListDeviceTemplates? request,
            ListDeviceTemplatesResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(_user, GXDeviceTemplatePolicies.View);
            GXSelectArgs arg;
            if (request != null && request.AllUsers &&
                _user.IsInRole(GXRoles.Admin))
            {
                arg = GXSelectArgs.SelectAll<GXDeviceTemplate>();
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXDeviceTemplate>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included?.Any() == true)
                {
                    arg.Where.And<GXDeviceTemplate>(w => request.Included.Contains(w.Id));
                }
            }
            else
            {
                string userId = ServerHelpers.GetUserId(_user);
                arg = GXQuery.GetDeviceTemplatesByUser(s => "*", userId, null,
                    request?.Filter?.Removed, request?.Exclude, request?.Included);
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                if (string.Compare(request.OrderBy, "Version", true) == 0)
                {
                    arg.OrderBy.Add<GXDeviceVersion>(o => o.Name!);
                }
                else if (string.Compare(request.OrderBy, "Model", true) == 0)
                {
                    arg.OrderBy.Add<GXDeviceModel>(o => o.Name!);
                }
                else
                {
                    arg.OrderBy.Add<GXDeviceTemplate>(request.OrderBy);
                }
            }
            else
            {
                arg.OrderBy.Add<GXDeviceTemplate>(q => q.CreationTime!);
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
                        arg.Where.And<GXManufacturer>(w => w.Name!.Contains(man.Name));
                    }
                    if ((man.Models?.FirstOrDefault() is GXDeviceModel mod))
                    {
                        if (!string.IsNullOrEmpty(mod.Name))
                        {
                            addJoin = true;
                            arg.Where.And<GXDeviceModel>(w => w.Name!.Contains(mod.Name));
                        }
                        if ((mod.Versions?.FirstOrDefault() is GXDeviceVersion version))
                        {
                            if (!string.IsNullOrEmpty(version.Name))
                            {
                                addJoin = true;
                                arg.Where.And<GXDeviceVersion>(w => w.Name!.Contains(version.Name));
                            }
                        }
                    }
                }
                if (addJoin)
                {
                    arg.Joins.AddInnerJoin<GXDeviceTemplate, GXDeviceSettings>(j => j.Id, j => j.Template!);
                    arg.Joins.AddInnerJoin<GXDeviceSettings, GXDeviceVersion>(j => j.Version!, j => j.Id);
                    arg.Joins.AddInnerJoin<GXDeviceVersion, GXDeviceModel>(j => j.Model!, j => j.Id);
                    arg.Joins.AddInnerJoin<GXDeviceModel, GXManufacturer>(j => j.Manufacturer!, j => j.Id);
                }

                //Reset manufacturer filter.
                request.Filter.Manufacturer = null;
                arg.Where.FilterBy(request.Filter);
            }

            GXDateTimeFilters.Apply<GXDeviceTemplate>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXDeviceTemplate>(q => GXSql.Count(1));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = await connection.SingleOrDefaultAsync<long>(total);
                    if (response.Count == 0)
                    {
                        //No need to retrieve data when none is available.
                        return [];
                    }
                }
                arg.Index = request.Index;
                arg.Count = request.Count;
            }
            GXDeviceTemplate[] templates = [.. (await connection.SelectAsync<GXDeviceTemplate>(arg))];
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
        /// <remarks>The returned object and attribute templates include their named values and bit-field coordinates.</remarks>
        public async Task<GXDeviceTemplate> ReadAsync(Guid id, Expression<Func<GXDeviceTemplate, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(_user, GXDeviceTemplatePolicies.View);
                GXSelectArgs arg;
                if (_user.IsInRole(GXRoles.Admin))
                {
                    arg = GXSelectArgs.SelectAll<GXDeviceTemplate>(w => w.Id == id);
                }
                else
                {
                    string userId = ServerHelpers.GetUserId(_user);
                    arg = GXQuery.GetDeviceTemplatesByUser(s => "*", userId, id);
                }
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                arg.Joins.AddLeftJoin<GXDeviceTemplate, GXAppearance>(t => t.ProfilePicture!, a => a.Id);
                arg.Columns.Add<GXAppearance>(a => a.Id);
                GXDeviceTemplate? ret = await connection.SingleOrDefaultAsync<GXDeviceTemplate>(arg) ?? throw new GXAmiNotFoundException(Properties.Resources.DeviceTemplate + " " + Properties.Resources.Id + " " + id.ToString());
                //Objects and attributes are faster to retrieve with own query.
                arg = GXSelectArgs.SelectAll<GXObjectTemplate>(where => where.DeviceTemplate == ret && where.Removed == null);
                arg.Columns.Add<GXAttributeTemplate>();
                arg.Distinct = true;
                arg.Joins.AddLeftJoin<GXObjectTemplate, GXAttributeTemplate>(o => o.Id, a => a.ObjectTemplate!);
                arg.Columns.Add<GXAttributeListItem>();
                arg.Joins.AddLeftJoin<GXAttributeTemplate, GXAttributeListItem>(attribute => attribute.Id, item => item.Template!);
                arg.Columns.Exclude<GXAttributeListItem>(item => item.Template!);
                arg.Where.And<GXAttributeTemplate>(q => q.Removed == null);
                arg.Columns.Exclude<GXAttributeTemplate>(e => e.ObjectTemplate!);
                ret.Objects = connection.Select<GXObjectTemplate>(arg);

                //Get device template groups.
                arg = GXSelectArgs.SelectAll<GXDeviceTemplateGroup>(where => where.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXDeviceTemplateGroup, GXDeviceTemplateGroupDeviceTemplate>(o => o.Id, a => a.DeviceTemplateGroupId);
                arg.Where.And<GXDeviceTemplateGroupDeviceTemplate>(q => q.DeviceTemplateId == id && q.Removed == null);
                ret.DeviceTemplateGroups = connection.Select<GXDeviceTemplateGroup>(arg);
                if (!string.IsNullOrEmpty(ret.Settings))
                {
                    var s = JsonSerializer.Deserialize<Shared.DTOs.Device.GXDLMSSettings>(ret.Settings);
                    if (s?.Password?.Length > 0)
                    {
                        s.Password = await _cryproService.DecryptAsync(s.Password);
                    }
                    ret.Settings = JsonSerializer.Serialize(s);
                }
                //Get creator with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName }, q => q.Removed == null);
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXDeviceTemplate, GXUser>(s => s.Creator!, o => o.Id!);
                arg.Where.And<GXDeviceTemplate>(w => w.Id == id);
                ret.Creator = await connection.SingleOrDefaultAsync<GXUser>(arg);
                return ret;

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

        /// <summary>Encrypts a device template's nonempty serialized DLMS password before persisting its settings.</summary>
        /// <param name="it">Device template whose serialized DLMS settings are rewritten.</param>
        /// <returns>A task that completes after the template settings have been rewritten.</returns>
        private async Task EncryptPassword(GXDeviceTemplate it)
        {
            if (!string.IsNullOrEmpty(it.Settings))
            {
                var s = JsonSerializer.Deserialize<Shared.DTOs.Device.GXDLMSSettings>(it.Settings);
                if (s?.Password?.Length > 0)
                {
                    s.Password = await _cryproService.EncryptAsync(s.Password);
                }
                it.Settings = JsonSerializer.Serialize(s);
            }
        }

        /// <inheritdoc />
        /// <remarks>New templates persist their objects, attributes and named values in bounded insert batches within one transaction.</remarks>
        public async Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXDeviceTemplate> DeviceTemplates,
            Expression<Func<GXDeviceTemplate, object>>? columns, CancellationToken cancellationToken = default)
        {
            DeviceTemplates = [.. DeviceTemplates];
            return await GXHistoryMutation.RunAsync<GXDeviceTemplate, IEnumerable<Guid>>(_dbFactory, _user, _serviceProvider, DeviceTemplates,
        async () =>
        {
            DeviceTemplates = [.. DeviceTemplates];
            List<(List<string> Users, GXTarget Target)> notifications = [];
            DateTime now = DateTime.Now;
            string userId = ServerHelpers.GetUserId(_user);
            GXUser creator = new() { Id = userId };
            List<GXDeviceTemplateGroup>? defaultGroups = null;
            var newTemplates = DeviceTemplates.Where(w => w.Id == Guid.Empty).ToList();
            var updatedTemplates = DeviceTemplates.Where(w => w.Id != Guid.Empty).ToList();
            foreach (GXDeviceTemplate it in DeviceTemplates)
            {
                if (string.IsNullOrEmpty(it.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                if (it.DeviceTemplateGroups == null || it.DeviceTemplateGroups.Count == 0)
                {
                    if (defaultGroups == null)
                    {
                        ListDeviceTemplateGroups request = new()
                        {
                            Filter = new GXDeviceTemplateGroup() { Default = true }
                        };
                        defaultGroups = [.. await _deviceTemplateGroupRepository.ListAsync(cancellationToken: cancellationToken)];
                    }
                    it.DeviceTemplateGroups = defaultGroups;
                    if (it.DeviceTemplateGroups == null || it.DeviceTemplateGroups.Count == 0)
                    {
                        throw new ArgumentNullException(Properties.Resources.ArrayIsEmpty);
                    }
                }
            }
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            foreach (var template in DeviceTemplates)
                {
                if (template.Id == Guid.Empty || columns == null || ServerHelpers.Contains(columns, nameof(GXDeviceTemplate.ProfilePicture)))
                    {
                    await Gurux.DLMS.AMI.Server.Services.GXProfilePictures.ValidateAsync(connection, _authorizationService, _user, template.ProfilePicture?.Id, cancellationToken);
                }
            }
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                if (newTemplates.Count != 0)
                {
                    await _authorizationService.EnsureAnyPolicyAsync(_user, GXDeviceTemplatePolicies.Add);
                    foreach (var dt in newTemplates)
                    {
                        if (dt.Objects == null || dt.Objects.Count == 0)
                        {
                            throw new ArgumentNullException(Properties.Resources.ArrayIsEmpty);
                        }
                        await EncryptPassword(dt);
                        dt.CreationTime = now;
                        dt.Creator = creator;
                        foreach (var ot in dt.Objects)
                        {
                            ot.DeviceTemplate = dt;
                            if (ot.Attributes != null)
                            {
                                foreach (var at in ot.Attributes)
                                {
                                    at.ObjectTemplate = ot;
                                    foreach (var item in at.ListItems ?? [])
                                    {
                                        item.Template = at;
                                    }
                                }
                            }
                        }
                    }

                    GXInsertBatches.Insert(connection, transaction, newTemplates, args =>
                    {
                        args.Exclude<GXDeviceTemplate>(e => new
                        {
                            e.Updated,
                            e.Removed,
                            e.Objects,
                            e.DeviceTemplateGroups
                        });
                    }, cancellationToken);
                    //Map device templates to device template groups.
                    foreach (var dt in newTemplates)
                    {
                        AddDeviceTemplateToDeviceTemplateGroups(connection, transaction, dt.Id, dt.DeviceTemplateGroups!);
                    }

                    //Add object templates.
                    var objects = newTemplates.SelectMany(s => s.Objects!).ToArray();
                    GXInsertBatches.Insert(connection, transaction, objects, args =>
                    {
                        args.Exclude<GXObjectTemplate>(e => new
                        {
                            e.Updated,
                            e.Removed,
                            e.Attributes
                        });
                    }, cancellationToken);
                    //Add attribute templates.
                    var attributes = objects.SelectMany(s => s.Attributes!).ToArray();
                    GXInsertBatches.Insert(connection, transaction, attributes, args =>
                    {
                        args.Exclude<GXAttributeTemplate>(e => new
                        {
                            e.Updated,
                            e.Removed,
                            e.ListItems
                        });
                    }, cancellationToken);
                    // Persist imported named values after their owning attributes have received identifiers.
                    var listItems = attributes.SelectMany(attribute => attribute.ListItems ?? []).ToArray();
                    if (listItems.Length != 0)
                    {
                        GXInsertBatches.Insert(connection, transaction, listItems, cancellationToken: cancellationToken);
                    }
                    foreach (var it in newTemplates)
                    {
                        notifications.Add((await GetNotifiedUsers(it.Id, NotificationAction.Add, connection, transaction),
                                   new GXTarget(GXDeviceTemplatePolicies.Add, it.Id, it.Name) { ConcurrencyStamp = it.ConcurrencyStamp }));
                    }
                }
                if (updatedTemplates.Count != 0)
                {
                    await _authorizationService.EnsureAnyPolicyAsync(_user, GXDeviceTemplatePolicies.Edit);
                    foreach (var it in updatedTemplates)
                    {
                        GXSelectArgs m = GXSelectArgs.Select<GXDeviceTemplate>(q => q.ConcurrencyStamp!, where => where.Id == it.Id);
                        string? updated = connection.SingleOrDefault<string>(transaction, m);
                        if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }
                        it.Updated = now;
                        it.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                        await EncryptPassword(it);
                        args.Exclude<GXDeviceTemplate>(q => new { q.CreationTime, q.DeviceTemplateGroups, q.Creator });
                        connection.Update(transaction, args);
                        //Map device template to device template group.
                        List<GXDeviceTemplateGroup> deviceTemplateGroups;
                        using (IServiceScope scope = _serviceProvider.CreateScope())
                        {
                            IDeviceTemplateGroupRepository deviceTemplateGroupRepository = scope.ServiceProvider.GetRequiredService<IDeviceTemplateGroupRepository>();
                            deviceTemplateGroups = await deviceTemplateGroupRepository.GetJoinedDeviceTemplateGroups(it.Id);
                        }
                        var comparer = new UniqueComparer<GXDeviceTemplateGroup, Guid>();
                        var removedDeviceGroups = deviceTemplateGroups.Except(it.DeviceTemplateGroups!, comparer);
                        var addedDeviceGroups = it.DeviceTemplateGroups!.Except(deviceTemplateGroups, comparer);
                        if (removedDeviceGroups.Any())
                        {
                            RemoveDeviceTemplateFromDeviceTemplateGroups(connection, transaction, it.Id, removedDeviceGroups);
                        }
                        if (addedDeviceGroups.Any())
                        {
                            AddDeviceTemplateToDeviceTemplateGroups(connection, transaction, it.Id, addedDeviceGroups);
                        }
                        notifications.Add((await GetNotifiedUsers(it.Id, NotificationAction.Edit, connection, transaction),
                                   new GXTarget(GXDeviceTemplatePolicies.Edit, it.Id, it.Name) { ConcurrencyStamp = it.ConcurrencyStamp }));
                    }
                }
                await transaction.CommitAsync(cancellationToken);
                foreach (var (users, target) in notifications)
                {
                    _notificationCache.Add(users, target);
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
                        return await UpsertAsync(DeviceTemplates, columns, linkedCts.Token);
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
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
            return [.. DeviceTemplates.Select(s => s.Id)];
        }, cancellationToken);
        }

        /// <summary>
        /// Adds device-template group mappings in bounded batches within the supplied transaction.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="deviceTemplateId">Device template ID.</param>
        /// <param name="groups">Group IDs of the device template groups where the device template is added.</param>
        private static void AddDeviceTemplateToDeviceTemplateGroups(GXDbConnection connection, IDbTransaction transaction, Guid deviceTemplateId, IEnumerable<GXDeviceTemplateGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXDeviceTemplateGroupDeviceTemplate> list = [];
            foreach (GXDeviceTemplateGroup it in groups)
            {
                list.Add(new GXDeviceTemplateGroupDeviceTemplate()
                {
                    DeviceTemplateId = deviceTemplateId,
                    DeviceTemplateGroupId = it.Id,
                    CreationTime = now
                });
            }
            GXInsertBatches.Insert(connection, transaction, list);
        }

        /// <summary>
        /// Remove map between device template group and device template.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="transaction">Transaction.</param>
        /// <param name="deviceTemplateId">Device template ID.</param>
        /// <param name="groups">Group IDs of the device template groups where the device template is removed.</param>
        private static void RemoveDeviceTemplateFromDeviceTemplateGroups(GXDbConnection connection, IDbTransaction transaction, Guid deviceTemplateId, IEnumerable<GXDeviceTemplateGroup> groups)
        {
            foreach (var it in groups)
            {
                var arg = GXDeleteArgs.Delete<GXDeviceTemplateGroupDeviceTemplate>(w => w.DeviceTemplateId == deviceTemplateId && w.DeviceTemplateGroupId == it.Id);
                connection.Delete(transaction, arg);
            }
        }
    }
}
