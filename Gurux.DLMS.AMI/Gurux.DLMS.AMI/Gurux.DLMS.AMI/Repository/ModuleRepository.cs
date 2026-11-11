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
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Module;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json.Nodes;

namespace Gurux.DLMS.AMI.Server.Repository
{

    /// <inheritdoc />
    public partial class ModuleRepository : IModuleRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IUserRepository _userRepository;
        private readonly IModuleGroupRepository _moduleGroupRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogRepository _systemLogRepository;
        private readonly NotificationCache _notificationCache;
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ModuleRepository(IAuthorizationService authorizationService,
            IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor,
            IUserRepository userRepository,
            IModuleGroupRepository moduleGroupRepository,
            IServiceProvider serviceProvider,
            ILogRepository systemLogRepository,
            NotificationCache notificationCache)
        {
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            User = httpContextAccessor.HttpContext.User;
            _dbFactory = dbFactory;
            _userRepository = userRepository;
            _moduleGroupRepository = moduleGroupRepository;
            _serviceProvider = serviceProvider;
            _systemLogRepository = systemLogRepository.ForSource("system");
            _notificationCache = notificationCache;
            _authorizationService = authorizationService;
        }

        private void SetRuntimeSnapshot(GXModule module)
        {
            var state = _serviceProvider.GetRequiredService<Server.Module.GXModuleRuntime>().GetState(module.Id);
            module.RuntimeState = state.HasValue ? (byte)state.Value : null;
        }

        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXModule), id, action, GXModulePolicies.View, connection, transaction, notifications: _notificationCache);

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(string? moduleId, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXModulePolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByModule(s => s.Id!,
                ServerHelpers.GetUserId(User), moduleId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            List<GXUser> users = await connection.SelectAsync<GXUser>(args);
            List<string> ret = [.. users.Select(s => s.Id!)];
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(IEnumerable<string>? moduleIds, CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXModulePolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByModules(s => s.Id!,
                ServerHelpers.GetUserId(User), moduleIds);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            List<GXUser> users = await connection.SelectAsync<GXUser>(args);
            List<string> ret = [.. users.Select(s => s.Id!)];
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(IEnumerable<string> modules, CancellationToken cancellationToken = default)
        {
            modules = modules.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXModule, string>(_dbFactory, User, _serviceProvider, modules,
        async () =>
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXModulePolicies.Delete);
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                var selected = new List<GXModule>();
                var recipients = new Dictionary<string, List<string>>();
                foreach (var id in modules)
                {
                    var selection = GXSelectArgs.Select<GXModule>(module => new { module.Id, module.Name, module.Status }, module => module.Id == id);
                    var module = await connection.SingleOrDefaultAsync<GXModule>(selection) ?? throw new ArgumentException($"Module {id} does not exist.");
                    var policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXModule), id, cancellationToken);
                    if (!await GXResourcePolicyAccess.AllowsAsync(connection, _authorizationService, User, policies, cancellationToken))
                    {
                        throw new UnauthorizedAccessException();
                    }
                    selected.Add(module);
                    recipients[module.Id] = await GetNotifiedUsers(module.Id, NotificationAction.Remove, connection);
                }
                await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
                foreach (GXModule module in selected)
                {
                    await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXModulePolicy>(p => p.ModuleId == module.Id), cancellationToken);
                    await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXAgentProperty>(p => p.Module == module), cancellationToken);
                    await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXUserProperty>(p => p.Module == module), cancellationToken);
                    await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXKeyManagementProperty>(p => p.Module == module), cancellationToken);
                    await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXDeviceProperty>(p => p.Module == module), cancellationToken);
                    await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXObjectProperty>(p => p.Module == module), cancellationToken);
                    await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXAttributeProperty>(p => p.Module == module), cancellationToken);
                    await Gurux.DLMS.AMI.Server.Module.GXModuleLocalizationCleanup.DeleteAsync(connection, transaction, module.Id, cancellationToken);
                    await GXLogStore.DeleteOwnedAsync(connection, transaction, "module", [module.Id], cancellationToken);
                    int catalogVersions = await connection.SingleOrDefaultAsync<int>(transaction,
                        GXSelectArgs.Select<GXModuleVersion>(v => GXSql.Count(1), v => v.Module == module), cancellationToken);
                    if (!Gurux.DLMS.AMI.Server.Module.GXModuleInstallationPolicy.RetainAfterUninstall(module.Status, catalogVersions > 0))
                    {
                        //Delete module if module is custom module.
                        await GXNotificationSourceStorage.DeleteModuleAsync(connection, transaction, module.Id, cancellationToken);
                        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXModule>(where => where.Id == module.Id), cancellationToken);
                    }
                    else
                    {
                        //If installable module is removed.
                        module.Status = ModuleStatus.Installable;
                        module.Enabled = false;
                        module.Updated = DateTime.Now;
                        module.Version = null;
                        await connection.UpdateAsync(transaction, GXUpdateArgs.Update(module, c => new
                        {
                            c.Status,
                            c.Enabled,
                            c.Updated,
                            c.Version
                        }), cancellationToken);
                        //Delete module assemblies.
                        await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXModuleAssembly>(where => where.Module == module), cancellationToken);
                    }

                }
                await transaction.CommitAsync(cancellationToken);
                foreach (var module in selected)
                {
                    _notificationCache.Add(recipients[module.Id], new GXTarget(GXModulePolicies.Delete, module.Id, module.Name));
                }

                List<GXLog> logs = [];
                foreach (string it in modules)
                {
                    logs.Add(new GXLog(TraceLevel.Info)
                    {
                        Message = string.Format("Module {0} removed.", it)
                    });
                }
                await _systemLogRepository.AddAsync("Remove", logs, cancellationToken);

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
                        await DeleteAsync(modules, linkedCts.Token);
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
        public async Task<IEnumerable<GXModule>> ListAsync(
            ListModules? request,
            ListModulesResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXModulePolicies.View);
            GXSelectArgs arg;
            if (User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the modules.
                arg = GXSelectArgs.SelectAll<GXModule>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetModulesByUser(s => s.Id, userId);
                arg.Columns.Clear();
                arg.Columns.Add<GXModule>();
            }
            ModuleStatus? status = null;
            ModuleType? type = null;
            if (request != null && request.Filter != null)
            {
                if (request.Filter.Status != null)
                {
                    status = request.Filter.Status;
                    request.Filter.Status = null;
                }
                if (request.Filter.Type != null)
                {
                    type = request.Filter.Type;
                    request.Filter.Type = null;
                }
            }

            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude?.Any() == true)
                {
                    foreach (var excludedId in request.Exclude)
                    {
                        arg.Where.And<GXModule>(w => w.Id != excludedId);
                    }
                }
                if (request.Included?.Any() == true)
                {
                    arg.Where.And<GXModule>(w => request.Included.Contains(w.Id));
                }
            }
            arg.Distinct = true;
            GXDateTimeFilters.Apply<GXModule>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            var allowed = new List<string>();
            foreach (var candidate in await connection.SelectAsync<GXModule>(GXSelectArgs.Select<GXModule>(m => m.Id), cancellationToken))
            {
                var names = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXModule), candidate.Id, cancellationToken);
                if (await GXResourcePolicyAccess.AllowsAsync(connection, _authorizationService, User, names, cancellationToken))
                {
                    allowed.Add(candidate.Id);
                }
            }
            if (allowed.Count == 0)
            {
                if (response != null)
                {
                    response.Count = 0;
                    response.Modules = [];
                }
                return [];
            }
            arg.Where.And<GXModule>(m => allowed.Contains(m.Id));
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXModule>(q => GXSql.Count(1));
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
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXModule>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXModule>(q => q.Id);
            }
            List<GXModule> modules = [.. (await connection.SelectAsync<GXModule>(arg))];
            foreach (var item in modules)
            {
                item.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXModule), item.Id, cancellationToken);
                SetRuntimeSnapshot(item);
            }
            if (status != null)
            {
                modules.RemoveAll(w => (w.Status & status) == 0);
            }
            if (type != null)
            {
                modules.RemoveAll(w => (w.Type & type) == 0);
            }
            if (response != null)
            {
                response.Modules = [.. modules];
                if (response.Count == 0)
                {
                    response.Count = modules.Count;
                }
            }
            return [.. modules];
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXModule>> ListWithVersionsAsync()
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXModulePolicies.View);
            bool isAdmin = true;
            if (User != null)
            {
                isAdmin = User.IsInRole(GXRoles.Admin);
            }
            GXSelectArgs arg;
            if (isAdmin || User == null)
            {
                //Admin can see all the modules.
                arg = GXSelectArgs.SelectAll<GXModule>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetModulesByUser(s => s.Id, userId);
            }
            arg.Columns.Add<GXModuleVersion>();
            arg.Columns.Exclude<GXModuleVersion>(e => e.Module!);
            arg.Joins.AddLeftJoin<GXModule, GXModuleVersion>(j => j.Id, j => j.Module!);
            arg.Distinct = true;
            arg.OrderBy.Add<GXModule>(q => q.Id);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            GXModule[] modules = [.. (await connection.SelectAsync<GXModule>(arg))];
            var visible = new List<GXModule>();
            foreach (var item in modules)
            {
                item.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXModule), item.Id);
                if (await GXResourcePolicyAccess.AllowsAsync(connection, _authorizationService, User, item.Policies))
                {
                    visible.Add(item);
                    SetRuntimeSnapshot(item);
                }
            }
            return visible;
        }

        /// <inheritdoc />
        public async Task<GXModule> ReadAsync(string id, Expression<Func<GXModule, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXModulePolicies.View);
                GXSelectArgs arg = GXSelectArgs.SelectAll<GXModule>(where => where.Id == id);
                arg.Columns.Add<GXModuleProperty>();
                arg.Columns.Exclude<GXModuleProperty>(p => p.Module!);
                arg.Joins.AddLeftJoin<GXModule, GXModuleProperty>(j => j.Id, j => j.Module!);
                arg.Columns.Add<GXUser>(u => new { u.Id, u.UserName });
                arg.Joins.AddLeftJoin<GXModule, GXUser>(m => m.Creator!, u => u.Id!);
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                var module = await connection.SingleOrDefaultAsync<GXModule>(arg) ?? throw new GXAmiForbiddenException(id.ToString());
                module.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXModule), module.Id, cancellationToken);
                if (!await GXResourcePolicyAccess.AllowsAsync(connection, _authorizationService, User, module.Policies, cancellationToken))
                {
                    throw new UnauthorizedAccessException();
                }
                module.Versions = await Gurux.DLMS.AMI.Server.Module.GXModuleVersionStorage.ReadAsync(connection, module, cancellationToken);
                //Get script and methods.
                arg = GXSelectArgs.Select<GXScript>(s => new { s.Id, s.Name }, w => w.Module == module);
                arg.Columns.Add<GXScriptMethod>();
                arg.Columns.Exclude<GXScriptMethod>(e => e.Script!);
                arg.Joins.AddLeftJoin<GXScript, GXScriptMethod>(j => j.Id, j => j.Script!);
                module.Scripts = await connection.SelectAsync<GXScript>(arg);
                //Get module groups.
                arg = GXSelectArgs.Select<GXModuleGroup>(s => new { s.Id, s.Name }, where => where.Removed == null);
                arg.Joins.AddInnerJoin<GXModuleGroup, GXModuleGroupModule>(j => j.Id, j => j.ModuleGroupId);
                arg.Where.And<GXModuleGroupModule>(w => w.ModuleId == id);
                module.ModuleGroups = await connection.SelectAsync<GXModuleGroup>(arg);
                // Read the additional child collection independently to avoid multiplying joined rows.
                arg = GXSelectArgs.Select<GXModuleAssembly>(a => new { a.Id, a.FileName }, a => a.Module == module);
                arg.Distinct = true;
                arg.Columns.Exclude<GXModuleAssembly>(a => a.Module!);
                module.Assemblies = await connection.SelectAsync<GXModuleAssembly>(arg);
                SetRuntimeSnapshot(module);
                return module;

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
        public async Task UpsertAsync(GXModule module,
            Expression<Func<GXModule, object>>? columns, CancellationToken cancellationToken = default)
        {
            await GXHistoryMutation.RunAsync<GXModule>(_dbFactory, User, _serviceProvider, new[] { module },
        async () =>
        {
            try
            {
                using var settingsLock = await Server.Module.AmiModuleSettingsStorage.LockAsync(module.Id, cancellationToken);
                await _authorizationService.EnsureAnyPolicyAsync(User, GXModulePolicies.Edit);
                //Verify that the module exists and compare its saved deployment choice.
                GXSelectArgs arg = GXSelectArgs.Select<GXModule>(s => new { s.Id, s.Enabled },
                    w => w.Id == module.Id);
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                arg.Columns.Add<GXModuleProperty>();
                arg.Columns.Exclude<GXModuleProperty>(p => p.Module!);
                arg.Joins.AddLeftJoin<GXModule, GXModuleProperty>(j => j.Id, j => j.Module!);
                var mod = await connection.SingleOrDefaultAsync<GXModule>(arg) ?? throw new GXAmiNotFoundException(Properties.Resources.Module + " " + Properties.Resources.Id + " " + module.Id);
                var existingPolicies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXModule), module.Id, cancellationToken);
                if (!await GXResourcePolicyAccess.AllowsAsync(connection, _authorizationService, User, existingPolicies, cancellationToken))
                {
                    throw new UnauthorizedAccessException();
                }
                if (module.Policies != null)
                {
                    await GXResourcePolicyAccess.ValidateAsync(connection, module.Policies, cancellationToken);
                }
                module.Enabled ??= mod.Enabled;
                bool enabledChange = mod.Enabled != module.Enabled;
                SetRuntimeSnapshot(module);
                bool saveProperties = module.Properties != null &&
                    (columns == null || ServerHelpers.Contains(columns, nameof(GXModule.Properties)));
                if (saveProperties)
                    {
                    module.SetSettings(AmiModuleSettingsStorage.PreserveState(
                        _serviceProvider.GetRequiredService<GXModuleRuntime>().NormalizeSettings(module.Id,
                            AmiModuleSettingsStorage.PreserveSchemaVersion(module.GetSettings(), mod.GetSettings())), mod.GetSettings()));
                }
                GXUpdateArgs args = GXUpdateArgs.Update(module, columns);
                module.Updated = DateTime.Now;
                // Lifecycle methods own Enabled. Status and RuntimeState are server-owned snapshots.
                // Excluding them also prevents a delayed metadata save from undoing a newer enable/disable request.
                args.Exclude<GXModule>(e => new { e.Enabled, e.Status, e.RuntimeState, e.Versions, e.ModuleGroups, e.Properties });
                await using (var transaction = await connection.BeginTransactionAsync(cancellationToken))
                {
                    await connection.UpdateAsync(transaction, args, cancellationToken);
                    if (saveProperties)
                        {
                        await AmiModuleSettingsStorage.SavePropertiesAsync(connection, module, cancellationToken, transaction);
                    }
                    if (module.Policies != null)
                    {
                        await GXResourcePolicyStorage.SaveAsync(connection, typeof(GXModule), module.Id, module.Policies, cancellationToken, transaction);
                    }
                    await transaction.CommitAsync(cancellationToken);
                }
                //Add new versions.
                arg = GXSelectArgs.SelectAll<GXModuleVersion>(where => where.Module == module);
                List<GXModuleVersion> versions = connection.Select<GXModuleVersion>(arg);
                var comparer = new UniqueComparer<GXModuleVersion, Guid>();
                List<GXModuleVersion>? addedVersions = null;
                if (module.Versions != null)
                {
                    addedVersions = [.. module.Versions.Except(versions, comparer)];
                }
                if (addedVersions != null && addedVersions.Count != 0)
                {
                    GXInsertArgs i = GXInsertArgs.InsertRange(addedVersions);
                    await connection.InsertAsync(i, cancellationToken);
                }
                //Update groups.
                if (module.ModuleGroups != null)
                {
                    arg = GXSelectArgs.SelectAll<GXModuleGroup>(where => where.Removed == null);
                    arg.Joins.AddInnerJoin<GXModuleGroup, GXModuleGroupModule>(j => j.Id, j => j.ModuleGroupId);
                    arg.Joins.AddInnerJoin<GXModuleGroupModule, GXModule>(j => j.ModuleId, j => j.Id);

                    List<GXModuleGroup> groups = connection.Select<GXModuleGroup>(arg);
                    var comparer2 = new UniqueComparer<GXModuleGroup, Guid>();
                    List<GXModuleGroup> removedModuleGroups = [.. groups.Except(module.ModuleGroups, comparer2)];
                    List<GXModuleGroup> addedModuleGroups = [.. module.ModuleGroups.Except(groups, comparer2)];
                    if (removedModuleGroups.Count != 0)
                    {
                        RemoveModulesFromModuleGroup(connection, module.Id, removedModuleGroups);
                    }
                    if (addedModuleGroups.Count != 0)
                    {
                        AddModuleToModuleGroups(connection, module.Id, addedModuleGroups);
                    }
                    Dictionary<GXModule, List<string>> updates = new()
                    {
                        [module] = await GetNotifiedUsers(module.Id, NotificationAction.Edit, connection)
                    };
                    foreach (var it in updates)
                    {
                        _notificationCache.Add(await GetNotifiedUsers(module.Id, NotificationAction.Edit, connection),
                            new GXTarget(GXModulePolicies.Edit, it.Key.Id, it.Key.Name));
                    }
                }
                if (enabledChange)
                {
                    string msg;
                    if (module.Enabled.GetValueOrDefault())
                    {
                        msg = Properties.Resources.ModuleEnabled;
                    }
                    else
                    {
                        msg = Properties.Resources.ModuleDisabled;
                    }
                    using IServiceScope scope = _serviceProvider.CreateScope();
                    ILogRepository moduleLogRepository = scope.ServiceProvider.GetRequiredService<ILogRepository>().ForSource("module");
                    await moduleLogRepository.AddAsync(TargetType.Module, [
                    new GXLog(TraceLevel.Info)
                    {
                        CreationTime = DateTime.Now,
                        SourceId = module.Id,
                        Message = msg
                    }], cancellationToken);
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
                        await UpsertAsync(module, columns, linkedCts.Token);
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
        public async Task AddAsync(IEnumerable<GXModule> modules)
        {
            modules = modules.ToArray();
            await GXHistoryMutation.RunAsync<GXModule>(_dbFactory, User, _serviceProvider, modules,
        async () =>
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXModulePolicies.Add);
            DateTime now = DateTime.Now;
            GXUser user = new() { Id = ServerHelpers.GetUserId(User) };
            foreach (var it in modules)
            {
                if (it.CreationTime == null)
                {
                    it.CreationTime = now;
                }
                if (it.ModuleGroups?.Count > 0)
                {
                    ListModuleGroups request = new() { Filter = new GXModuleGroup() { Default = true } };
                    IEnumerable<GXModuleGroup> groups = await _moduleGroupRepository.ListAsync(request);
                    it.ModuleGroups.AddRange(groups);
                }
                it.Creator = user;
                it.SetSettings(Server.Module.AmiModuleSettingsStorage.PreserveState(it.GetSettings(), null));
            }
            GXInsertArgs args = GXInsertArgs.InsertRange(modules);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            await connection.InsertAsync(args);
            List<GXLog> list = [];
            foreach (var it in modules)
            {
                list.Add(new GXLog(TraceLevel.Info)
                {
                    CreationTime = DateTime.Now,
                    SourceId = it.Id,
                    Message = Properties.Resources.ModuleRemoved
                });
            }
            using IServiceScope scope = _serviceProvider.CreateScope();
            ILogRepository moduleLogRepository = scope.ServiceProvider.GetRequiredService<ILogRepository>().ForSource("module");
            await moduleLogRepository.AddAsync("Add", list);
        }, CancellationToken.None);
        }

        /// <inheritdoc />
        public async Task<List<GXScript>> GetScriptsAsync(string? moduleId)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXModulePolicies.View);
            GXModule module = new();
            if (moduleId != null)
            {
                module.Id = moduleId;
            }
            GXSelectArgs args = GXSelectArgs.SelectAll<GXScript>(w => w.Removed == null && w.Module == module);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            return await connection.SelectAsync<GXScript>(args);
        }

        /// <summary>
        /// Map module group to user groups.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="moduleId">Module ID.</param>
        /// <param name="groups">Group IDs of the module groups where the module is added.</param>
        private static void AddModuleToModuleGroups(GXDbConnection connection,
            string moduleId, IEnumerable<GXModuleGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXModuleGroupModule> list = [];
            foreach (GXModuleGroup it in groups)
            {
                list.Add(new GXModuleGroupModule()
                {
                    ModuleId = moduleId,
                    ModuleGroupId = it.Id,
                    CreationTime = now
                });
            }
            connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between module group and module.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="moduleId">Module ID.</param>
        /// <param name="groups">Group IDs of the module groups where the module is removed.</param>
        private static void RemoveModulesFromModuleGroup(GXDbConnection connection,
            string moduleId, IEnumerable<GXModuleGroup> groups)
        {
            foreach (var it in groups)
            {
                connection.Delete(GXDeleteArgs.Delete<GXModuleGroupModule>(w => w.ModuleId == moduleId && w.ModuleGroupId == it.Id));
            }
        }

        internal static IModuleSettingsRepository ForModule(GXModuleRepositoryScope scope) => new ModuleOperations(scope);

        private sealed class ModuleOperations(GXModuleRepositoryScope scope) : IModuleSettingsRepository
        {
            private const string LogMenuKey = "LogMenuLinkId";

            private Task<GXModule?> ReadAsync(CancellationToken token)
            {
                var moduleId = scope.ModuleId;
                return AmiModuleSettingsStorage.ReadModuleAsync(scope.Connection, moduleId, token, scope.Transaction);
            }

            public async Task<Guid?> ReadNotificationTypeIdAsync(CancellationToken token)
            {
                var module = await ReadAsync(token) ?? throw new InvalidOperationException("The module settings record is unavailable.");
                var value = AmiModuleSettingsStorage.Parse(module.GetSettings())[AmiModuleSettingsStorage.ResourcesKey]?["NotificationTypeId"];
                if (value == null)
                {
                    return null;
                }
                if (!Guid.TryParse(value.GetValue<string>(), out var id) || id == Guid.Empty)
                {
                    throw new InvalidOperationException("The module notification channel ownership record is invalid.");
                }
                return id;
            }

            public async Task SetNotificationTypeIdAsync(Guid? id, CancellationToken token)
            {
                var module = await ReadAsync(token) ?? throw new InvalidOperationException("The module settings record is unavailable.");
                var settings = AmiModuleSettingsStorage.Parse(module.GetSettings());
                var resources = settings[AmiModuleSettingsStorage.ResourcesKey] as JsonObject ?? [];
                if (id == null)
                {
                    resources.Remove("NotificationTypeId");
                }
                else
                {
                    resources["NotificationTypeId"] = id.Value.ToString();
                }
                if (settings[AmiModuleSettingsStorage.ResourcesKey] == null)
                {
                    settings[AmiModuleSettingsStorage.ResourcesKey] = resources;
                }
                module.SetSettings(settings.ToJsonString());
                await AmiModuleSettingsStorage.SavePropertiesAsync(scope.Connection, module, token, scope.Transaction);
            }

            public async Task<Guid?> ReadLogMenuLinkIdAsync(CancellationToken token)
            {
                var module = await ReadAsync(token) ?? throw new InvalidOperationException("The module settings record is unavailable.");
                var value = AmiModuleSettingsStorage.Parse(module.GetSettings())[AmiModuleSettingsStorage.ResourcesKey]?[LogMenuKey];
                if (value == null)
                {
                    return null;
                }
                if (!Guid.TryParse(value.GetValue<string>(), out var id) || id == Guid.Empty)
                {
                    throw new InvalidOperationException("The module log menu ownership record is invalid.");
                }
                return id;
            }

            public async Task SetLogMenuLinkIdAsync(Guid? id, CancellationToken token)
            {
                var module = await ReadAsync(token) ?? throw new InvalidOperationException("The module settings record is unavailable.");
                var settings = AmiModuleSettingsStorage.Parse(module.GetSettings());
                var resources = settings[AmiModuleSettingsStorage.ResourcesKey] as JsonObject ?? [];
                if (id == null)
                {
                    resources.Remove(LogMenuKey);
                }
                else
                {
                    resources[LogMenuKey] = id.Value.ToString();
                }
                if (settings[AmiModuleSettingsStorage.ResourcesKey] == null)
                {
                    settings[AmiModuleSettingsStorage.ResourcesKey] = resources;
                }
                module.SetSettings(settings.ToJsonString());
                await AmiModuleSettingsStorage.SavePropertiesAsync(scope.Connection, module, token, scope.Transaction);
            }
        }
    }
}

