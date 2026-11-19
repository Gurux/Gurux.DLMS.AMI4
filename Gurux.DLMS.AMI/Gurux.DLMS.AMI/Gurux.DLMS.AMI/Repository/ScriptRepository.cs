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
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Client.Pages.Device;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Script;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json;

namespace Gurux.DLMS.AMI.Server.Repository
{

    /// <inheritdoc />
    public class ScriptRepository : IScriptRepository
    {
        /// <summary>Principal whose identity and claims govern repository access.</summary>
        private readonly ClaimsPrincipal _user;
        /// <summary>Creates database connections for repository queries and mutations.</summary>
        private readonly IDbConnectionFactory _dbFactory;
        /// <summary>Provides persistence and access checks for user records.</summary>
        private readonly IUserRepository _userRepository;
        /// <summary>Resolves services needed by repository operations.</summary>
        private readonly IServiceProvider _serviceProvider;
        /// <summary>Schedules or executes workflow actions after repository changes.</summary>
        private readonly IWorkflowHandler _workflowHandler;
        /// <summary>Provides persistence and access checks for script group records.</summary>
        private readonly IScriptGroupRepository _scriptGroupRepository;
        /// <summary>Publishes change notifications to connected users after mutations.</summary>
        private readonly NotificationCache _notificationCache;
        /// <summary>Evaluates policies before accessing protected data.</summary>
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ScriptRepository(
            IAuthorizationService authorizationService,
            IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor,
            IUserRepository userRepository,
            IServiceProvider serviceProvider,
            IWorkflowHandler workflowHandler,
            IScriptGroupRepository scriptGroupRepository,
            NotificationCache notificationCache)
        {
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            _user = httpContextAccessor.HttpContext.User;
            _dbFactory = dbFactory;
            _userRepository = userRepository;
            _serviceProvider = serviceProvider;
            _workflowHandler = workflowHandler;
            _scriptGroupRepository = scriptGroupRepository;
            _notificationCache = notificationCache;
            _authorizationService = authorizationService;
        }

        /// <summary>Resolves users authorized to receive notifications for the affected entity and change action.</summary>
        /// <param name="id">Identifier of the affected record.</param>
        /// <param name="action">Change action used to choose notification recipients.</param>
        /// <param name="connection">Existing database connection to reuse, when supplied.</param>
        /// <param name="transaction">Transaction in which the query or mutation participates.</param>
        /// <returns>User identifiers selected for delivery of the change notification.</returns>
        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXScript), id, action, GXScriptPolicies.View, connection, transaction, notifications: _notificationCache);

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid scriptId, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(_user, GXScriptPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByScript(s => s.Id!,
                ServerHelpers.GetUserId(_user), scriptId);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            await GXAutomationPolicyAccess.EnsureAsync(connection, _authorizationService, _user, typeof(GXScript), scriptId, cancellationToken);
            List<GXUser> users = await connection.SelectAsync<GXUser>(args);
            List<string> ret = [.. users.Select(s => s.Id!)];
            if (_user != null && _user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? Ids, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(_user, GXScriptPolicies.View);
            GXSelectArgs args = GXQuery.GetUsersByScripts(s => s.Id!,
                ServerHelpers.GetUserId(_user), Ids);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (Ids != null)
                {
                foreach (var resourceId in Ids.Distinct())
                    {
                    await GXAutomationPolicyAccess.EnsureAsync(connection, _authorizationService, _user, typeof(GXScript), resourceId, cancellationToken);
                }
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
        public async Task DeleteAsync(
            IEnumerable<Guid> scripts,
            bool delete, bool notify, CancellationToken cancellationToken)
        {
            scripts = scripts.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXScript, Guid>(_dbFactory, _user, _serviceProvider, scripts,
        async () =>
        {
            try
            {
                await _authorizationService.EnsureAnyPolicyAsync(_user, GXScriptPolicies.Delete);
                if (scripts != null && scripts.Any())
                {
                    GXSelectArgs arg = GXSelectArgs.Select<GXScript>(a => a.Id, q => scripts.Contains(q.Id));
                    await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                    List<GXScript> list = await connection.SelectAsync<GXScript>(arg);
                foreach (var resource in list)
                {
                    await GXAutomationPolicyAccess.EnsureAsync(connection, _authorizationService, _user, typeof(GXScript), resource.Id, cancellationToken);
                    await GXWorkflowIdentityAccess.ValidateScriptMutationAsync(connection, _user, resource.Id, cancellationToken);
                }
                    DateTime now = DateTime.Now;
                    Dictionary<GXScript, List<string>> updates = [];

                    foreach (var entity in list)
                    {
                        updates[entity] = await GetNotifiedUsers(entity.Id, NotificationAction.Remove, connection);
                    }

                    foreach (var script in list)
                    {
                        var uninstall = GXSelectArgs.Select<GXScriptMethod>(method => method.Id, method => method.Script == script && method.Name == "Uninstall");
                        var method = await connection.SingleOrDefaultAsync<GXScriptMethod>(uninstall);
                        if (method != null)
                        {
                            await RunAsync(method.Id, cancellationToken);
                        }
                    }
                    await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
                    foreach (GXScript script in list)
                    {
                        await GXLogStore.DeleteOwnedAsync(connection, transaction, "script", [script.Id], cancellationToken);
                        script.Removed = now;

                        if (delete)
                        {
                            await connection.DeleteAsync(transaction, GXDeleteArgs.DeleteById<GXScript>(script.Id), cancellationToken);
                        }
                        else
                        {
                            connection.Update(transaction, GXUpdateArgs.Update(script, q => q.Removed!));
                        }

                        //Unload script.
                        GXHistoryRuntimeEffects.EnqueueScript(script, _workflowHandler, deleted: true);
                    }
                    await GXLogStore.DeleteOwnedAsync(connection, transaction, "script", list.Select(item => item.Id), cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    foreach (var it in updates)
                    {
                        var users = it.Value;
                        if (users == null)
                        {
                            break;
                        }
                        GXScript tmp = new() { Id = it.Key.Id };
                        _notificationCache.Add(users, new GXTarget(GXScriptPolicies.Delete, tmp.Id));
                    }

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
                        await DeleteAsync(scripts, delete, notify, linkedCts.Token);
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
        public async Task<IEnumerable<GXScript>> ListAsync(
            ListScripts? request,
            ListScriptsResponse? response,
            CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(_user, GXScriptPolicies.View);
            GXSelectArgs arg;
            if (request != null && request.AllUsers && _user.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the scripts.
                arg = GXSelectArgs.SelectAll<GXScript>();
                if (request?.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXScript>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included?.Any() == true)
                {
                    arg.Where.And<GXScript>(w => request.Included.Contains(w.Id));
                }
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(_user);
                arg = GXQuery.GetScriptsByUser(s => "*", userId, null,
                    request?.Filter?.Removed, request?.Exclude, request?.Included);
            }
            if (!(request?.Select != null && request.Select.Contains("Assembly")))
            {
                //Byte assembly is send only when it's requested.
                arg.Columns.Exclude<GXScript>(e => e.ByteAssembly!);
            }
            if (!(request?.Select != null && request.Select.Contains("SourceCode")))
            {
                //Source code is send only when it's requested.
                arg.Columns.Exclude<GXScript>(e => e.SourceCode!);
            }

            if (request != null && request.Filter != null)
            {
                if (request.Filter.Methods != null && request.Filter.Methods.Count != 0)
                {
                    arg.Columns.Add<GXScriptMethod>();
                    arg.Joins.AddInnerJoin<GXScript, GXScriptMethod>(j => j.Id, j => j.Script!);
                }
                arg.Where.FilterBy(request.Filter);
            }
            if (request?.RebuildRequired == true)
            {
                GXScriptDependencyStorage.ApplyRebuildFilter(arg, GXScriptCompiler.GetAvailableVersions());
            }
            arg.Distinct = true;
            GXDateTimeFilters.Apply<GXScript>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            await GXAutomationPolicyAccess.ApplyAsync(connection, _authorizationService, _user, typeof(GXScript), arg, cancellationToken);
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXScript>(q => GXSql.Count(1));
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
                arg.OrderBy.Add<GXScript>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXScript>(q => q.CreationTime!);
                arg.Descending = true;
            }
            GXScript[] scripts = [.. (await connection.SelectAsync<GXScript>(arg))];
            foreach (var resource in scripts)
                {
                resource.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXScript), resource.Id, cancellationToken);
            }
            if (response != null)
            {
                response.Scripts = scripts;
                if (response.Count == 0)
                {
                    response.Count = scripts.Length;
                }
            }
            //Read methods with own query.
            foreach (GXScript script in scripts)
            {
                arg = GXSelectArgs.SelectAll<GXScriptMethod>(q => q.Script == script);
                script.Methods = (await connection.SelectAsync<GXScriptMethod>(arg));
                if (request?.Select?.Contains("Assembly") == true || request?.Select?.Contains("CompilationDependencies") == true)
                {
                    script.CompilationDependencies = GXScriptDependencyStorage.Read(connection, script.Id);
                }
            }
            return scripts;
        }

        /// <inheritdoc />
        public async Task<GXScript> ReadAsync(Guid id, Expression<Func<GXScript, object>>? columns,
            CancellationToken cancellationToken)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(_user, GXScriptPolicies.View);
                GXSelectArgs arg;
                if (_user.IsInRole(GXRoles.Admin))
                {
                    //Admin can see all the scripts.
                    arg = GXSelectArgs.SelectAll<GXScript>(w => w.Id == id);
                    arg.Joins.AddInnerJoin<GXScript, GXScriptGroupScript>(x => x.Id, y => y.ScriptId);
                    arg.Joins.AddInnerJoin<GXScriptGroupScript, GXScriptGroup>(j => j.ScriptGroupId, j => j.Id);
                }
                else
                {
                    string userId = ServerHelpers.GetUserId(_user);
                    arg = GXQuery.GetScriptsByUser(s => "*", userId, id);
                    arg.Joins.AddInnerJoin<GXScript, GXScriptGroupScript>(x => x.Id, y => y.ScriptId);
                    arg.Joins.AddInnerJoin<GXScriptGroupScript, GXScriptGroup>(j => j.ScriptGroupId, j => j.Id);
                }
                arg.Columns.Add<GXScriptGroup>();
                arg.Columns.Add<GXScriptMethod>();
                //Byte assembly is not send for the client.
                arg.Columns.Exclude<GXScript>(e => e.ByteAssembly!);
                arg.Columns.Exclude<GXScriptGroup>(e => e.Scripts!);
                arg.Columns.Exclude<GXScriptMethod>(e => e.Script!);
                arg.Joins.AddLeftJoin<GXScript, GXScriptMethod>(j => j.Id, j => j.Script!);
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                GXScript? script = await connection.SingleOrDefaultAsync<GXScript>(arg) ?? throw new GXAmiForbiddenException(id.ToString());
                await GXAutomationPolicyAccess.EnsureAsync(connection, _authorizationService, _user, typeof(GXScript), id, cancellationToken);
                script.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXScript), id, cancellationToken);
                script.CompilationDependencies = GXScriptDependencyStorage.Read(connection, id);
                //Get creator with own query. It's faster for some DBs.
                arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName });
                arg.Distinct = true;
                arg.Joins.AddInnerJoin<GXScript, GXUser>(s => s.Creator!, o => o.Id!);
                arg.Where.And<GXScript>(w => w.Id == id);
                script.Creator = await connection.SingleOrDefaultAsync<GXUser>(arg);
                return script;
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

        /// <summary>Rejects changes to executable code, imports, language, or execution location of a persisted catalog-owned script.</summary>
        /// <param name="saved">Persisted script whose catalog provenance determines whether executable content is immutable.</param>
        /// <param name="script">Proposed script update to compare against the persisted definition.</param>
        /// <exception cref="ArgumentException">Executable properties of a catalog-owned script are being changed.</exception>
        private static void ValidateCatalogScriptUpdate(GXScript saved, GXScript script)
        {
            // Trust persisted provenance, including when the request omits CatalogId.
            if (!string.IsNullOrEmpty(saved.CatalogId) &&
                (script.SourceCode != saved.SourceCode || script.Namespaces != saved.Namespaces ||
                script.Language != saved.Language || script.ServerSide != saved.ServerSide))
            {
                throw new ArgumentException("Catalog script code is read-only. Create an independent copy to edit it.", nameof(script));
            }
        }

        /// <summary>
        /// This method checks is script source code modified.
        /// </summary>
        /// <param name="script">Updated script.</param>
        /// <returns>True, if the source code has changed.</returns>
        /// <param name="connection">The database connection.</param>
        private static async Task<bool> IsChangedAsync(GXDbConnection connection, GXScript script)
        {
            if (script.Id == Guid.Empty)
            {
                script.CatalogId = script.CatalogVersion = script.CatalogSha256 = script.CatalogSourceCode = null;
                //If the new script is added.
                return true;
            }
            //Remove time is not checked because adming can revert the script.
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXScript>(q => q.Id == script.Id);
            GXScript? saved = await connection.SingleOrDefaultAsync<GXScript>(arg) ?? throw new GXAmiForbiddenException(script.Id.ToString());
            ValidateCatalogScriptUpdate(saved, script);
            // Ordinary editor saves cannot forge or reset publisher provenance.
            script.CatalogId = saved.CatalogId;
            script.CatalogVersion = saved.CatalogVersion;
            script.CatalogSha256 = saved.CatalogSha256;
            script.CatalogSourceCode = saved.CatalogSourceCode;
            bool changed = script.SourceCode != saved.SourceCode || script.Namespaces != saved.Namespaces || script.Language != saved.Language;
            if (!changed)
            {
                // Methods are server-owned metadata. Partial editor saves must not erase
                // existing method IDs or the agent references that use them.
                script.Methods = await connection.SelectAsync<GXScriptMethod>(
                    GXSelectArgs.SelectAll<GXScriptMethod>(method => method.Script == saved));
                if (script.Methods.Count == 0 || saved.ByteAssembly == null || saved.ByteAssembly.Length == 0)
                {
                    // Repair legacy scripts whose compiled method metadata is missing.
                    return true;
                }
                //Use saved byte assembly if source code hasn't changed.
                script.ByteAssembly = saved.ByteAssembly;
                script.CompilationDependencies = GXScriptDependencyStorage.Read(connection, script.Id);
            }
            return changed;
        }

        /// <summary>Compiles a C# script, records dependency versions, and reconciles its discovered method definitions.</summary>
        /// <param name="script">Script model receiving compiled bytes, dependency identities, and discovered methods.</param>
        /// <param name="compileTime">Receives the elapsed script compilation time in milliseconds.</param>
        /// <param name="cancellationToken">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
        /// <exception cref="ArgumentException">The script is empty or compilation reports errors.</exception>
        /// <exception cref="NotSupportedException">The script language is not C#.</exception>
        /// <exception cref="InvalidOperationException">Compilation produced no assembly.</exception>
        private void UpdateScript(GXScript script, out int compileTime, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(script.SourceCode))
            {
                throw new ArgumentException("Script is empty.");
            }
            if (script.Language != ScriptLanguage.CSharp)
            {
                throw new NotSupportedException("Only C# scripts are supported.");
            }
            List<GXScriptMethod> methods = [];
            script.ByteAssembly = Compile(script.Id.ToString(), script.SourceCode, script.Namespaces,
                methods, out string? errorJson, out compileTime, cancellationToken);
            if (errorJson != null)
            {
                throw new ArgumentException("Script compile failed. Validate script to get errors.", nameof(script));
            }
            script.CompilationDependencies = GXScriptCompiler.ReadDependencies(script.ByteAssembly
                ?? throw new InvalidOperationException("Script compilation did not produce an assembly."));
            var comparer = new GXScriptMethodComparer();
            List<GXScriptMethod> addedScriptMethods;
            if (script.Methods == null)
            {
                addedScriptMethods = methods;
            }
            else
            {
                addedScriptMethods = [.. methods.Except(script.Methods, comparer)];
            }
            List<GXScriptMethod>? removedScriptMethods = null;
            if (script.Methods != null)
            {
                removedScriptMethods = [.. script.Methods.Except(methods, comparer)];
            }
            if (script.Methods != null && removedScriptMethods != null && removedScriptMethods.Count != 0)
            {
                script.Methods.RemoveAll(q => removedScriptMethods.Contains(q));
            }
            if (addedScriptMethods.Count != 0)
            {
                if (script.Methods == null)
                {
                    script.Methods = addedScriptMethods;
                }
                else
                {
                    script.Methods.AddRange(addedScriptMethods);
                }
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXScript> scripts,
            Expression<Func<GXScript, object>>? columns, CancellationToken cancellationToken = default)
        {
            scripts = scripts.ToArray();
            return await GXHistoryMutation.RunAsync<GXScript, IEnumerable<Guid>>(_dbFactory, _user, _serviceProvider, scripts,
        async () =>
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(_user, GXScriptPolicies.Add, GXScriptPolicies.Edit);
                DateTime now = DateTime.Now;
                List<Guid> list = [];
                Dictionary<GXScript, List<string>> updates = [];
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                foreach (GXScript script in scripts)
                {
                    if (script.Id != Guid.Empty)
                    {
                        await GXAutomationPolicyAccess.EnsureAsync(connection, _authorizationService, _user, typeof(GXScript), script.Id, cancellationToken);
                        await GXWorkflowIdentityAccess.ValidateScriptMutationAsync(connection, _user, script.Id, cancellationToken);
                    }
                    await _authorizationService.EnsureAnyPolicyAsync(_user, script.Id == Guid.Empty ? GXScriptPolicies.Add : GXScriptPolicies.Edit);
                    await GXResourcePolicyAccess.ValidateAsync(connection, script.Policies, cancellationToken);
                    //Verify script name.
                    if (string.IsNullOrEmpty(script.Name))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    if (string.IsNullOrEmpty(script.SourceCode))
                    {
                        throw new ArgumentException("Script is empty.");
                    }
                    if (script.ScriptGroups == null || script.ScriptGroups.Count == 0)
                    {
                        //Get default script groups.
                        ListScriptGroups request = new()
                        {
                            Filter = new GXScriptGroup() { Default = true }
                        };
                        script.ScriptGroups = [.. await _scriptGroupRepository.ListAsync(request, cancellationToken: cancellationToken)];
                        if (script.ScriptGroups.Count == 0)
                        {
                            throw new ArgumentException(Properties.Resources.TargetMustBelongToOneGroup);
                        }
                    }
                    //Update compiled script if it's changed.
                    bool changed = await IsChangedAsync(connection, script);
                    bool refreshRuntime = changed && script.Id != Guid.Empty;
                    if (changed)
                    {
                        UpdateScript(script, out int compileTime, cancellationToken);
                    }
                    using (var transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable))
                    {
                        if (script.Id == Guid.Empty)
                        {
                            script.CreationTime = now;
                            script.Creator = new GXUser() { Id = ServerHelpers.GetUserId(_user) };
                            GXInsertArgs args = GXInsertArgs.Insert(script);
                            args.Exclude<GXScript>(q => new
                            {
                                q.Updated,
                                q.ScriptGroups,
                                q.Methods,
                                q.CompilationDependencies,
                                q.Creator
                            });
                            connection.Insert(transaction, args);
                            list.Add(script.Id);
                            AddScriptToScriptGroups(connection, script.Id, script.ScriptGroups, transaction);
                            AddScriptMethodsToScript(connection, script, script.Methods, transaction);
                        }
                        else
                        {
                            GXSelectArgs m = GXSelectArgs.Select<GXScript>(q => q.ConcurrencyStamp!, where => where.Id == script.Id);
                            string? updated = connection.SingleOrDefault<string>(transaction, m);
                            if (!string.IsNullOrEmpty(updated) && updated != script.ConcurrencyStamp)
                            {
                                throw new ArgumentException(Properties.Resources.ContentEdited);
                            }
                            m = GXSelectArgs.Select<GXScript>(q => q.Enabled!, where => where.Id == script.Id);
                            bool active = connection.SingleOrDefault<bool>(transaction, m);

                            script.Updated = now;
                            script.ConcurrencyStamp = Guid.NewGuid().ToString();
                            GXUpdateArgs args = GXUpdateArgs.Update(script, columns);
                            args.Exclude<GXScript>(q => new
                            {
                                q.CreationTime,
                                q.CatalogId,
                                q.CatalogVersion,
                                q.CatalogSha256,
                                q.CatalogSourceCode,
                                q.ScriptGroups,
                                q.Methods,
                                q.CompilationDependencies,
                                q.Creator
                            });
                            if (!_user.IsInRole(GXRoles.Admin) ||
                                script.Creator == null ||
                                string.IsNullOrEmpty(script.Creator.Id))
                            {
                                //Only admin can update the creator.
                                args.Exclude<GXScript>(q => q.Creator!);
                            }

                            if (updated != null)
                            {
                                args.Where.And<GXScript>(s => s.ConcurrencyStamp == updated);
                            }
                            if (connection.Update(transaction, args) != 1)
                            {
                                throw new ArgumentException(Properties.Resources.ContentEdited);
                            }
                            //Map script groups to script.
                            var joined = GXSelectArgs.SelectAll<GXScriptGroup>(g => g.Removed == null);
                            joined.Joins.AddInnerJoin<GXScriptGroup, GXScriptGroupScript>(g => g.Id, map => map.ScriptGroupId);
                            joined.Where.And<GXScriptGroupScript>(map => map.ScriptId == script.Id && map.Removed == null);
                            var scriptGroups = await connection.SelectAsync<GXScriptGroup>(transaction, joined, cancellationToken);
                            var comparer = new UniqueComparer<GXScriptGroup, Guid>();
                            List<GXScriptGroup> removedScriptGroups = [.. scriptGroups.Except(script.ScriptGroups, comparer)];
                            List<GXScriptGroup> addedScriptGroups = [.. script.ScriptGroups.Except(scriptGroups, comparer)];
                            if (removedScriptGroups.Count != 0)
                            {
                                RemoveScriptsFromScriptGroup(connection, script.Id, removedScriptGroups, transaction);
                            }
                            if (addedScriptGroups.Count != 0)
                            {
                                AddScriptToScriptGroups(connection, script.Id, addedScriptGroups, transaction);
                            }

                            //Handle script methods.
                            m = GXSelectArgs.SelectAll<GXScriptMethod>(where => where.Script == script);
                            List<GXScriptMethod> scriptMethods = await connection.SelectAsync<GXScriptMethod>(transaction, m, cancellationToken);
                            var comparer2 = new GXScriptMethodComparer();
                            List<GXScriptMethod> removedScriptMethods = [.. scriptMethods.Except(script.Methods ?? [], comparer2)];
                            List<GXScriptMethod> addedScriptMethods = [.. (script.Methods ?? []).Except(scriptMethods, comparer2)];
                            if (removedScriptMethods.Count != 0)
                            {
                                RemoveScriptMethodsFromScript(connection, removedScriptMethods, transaction);
                            }
                            if (addedScriptMethods.Count != 0)
                            {
                                AddScriptMethodsToScript(connection, script, addedScriptMethods, transaction);
                            }
                            if (changed)
                            {
                                refreshRuntime = true;
                            }
                            else if (script.Enabled.HasValue && script.Enabled.Value != active)
                            {
                                //If script active state has changed.
                                refreshRuntime = true;
                            }
                        }
                        if (changed)
                        {
                            // Store the emitted image even for a caller's partial-column update.
                            connection.Update(transaction, GXUpdateArgs.Update(script, q => q.ByteAssembly!));
                            GXScriptDependencyStorage.Replace(connection, transaction, script.Id, script.CompilationDependencies!);
                        }
                        if (script.Policies != null)
                            {
                            await GXResourcePolicyStorage.SaveAsync(connection, typeof(GXScript), script.Id, script.Policies, cancellationToken, transaction);
                        }
                        transaction.Commit();
                    }
                    if (refreshRuntime)
                    {
                        GXHistoryRuntimeEffects.EnqueueScript(script, _workflowHandler, deleted: true);
                        GXHistoryRuntimeEffects.EnqueueScript(script, _workflowHandler);
                    }
                    updates[script] = await GetNotifiedUsers(script.Id, NotificationAction.Edit, connection);
                }
                foreach (var it in updates)
                {
                    _notificationCache.Add(await GetNotifiedUsers(it.Key.Id, NotificationAction.Edit, connection),
                        new GXTarget(GXScriptPolicies.Edit, it.Key.Id, it.Key.Name) { ConcurrencyStamp = it.Key.ConcurrencyStamp });
                }
                List<GXLog> logs = [];
                foreach (var it in updates.Keys)
                {
                    logs.Add(new GXLog(TraceLevel.Info)
                    {
                        CreationTime = DateTime.Now,
                        Creator = it.Id,
                        Message = it.CreationTime == now ? Properties.Resources.ScriptInstalled :
                        Properties.Resources.ScriptUpdated
                    });
                }
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    var scriptLogRepository = scope.ServiceProvider.GetRequiredService<ILogRepository>().ForSource("script");
                    await scriptLogRepository.AddAsync("Updated", logs, cancellationToken);
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
                        return await UpsertAsync(scripts, columns, linkedCts.Token);
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
        /// Map script group to user groups.
        /// </summary>
        /// <param name="scriptId">Script ID.</param>
        /// <param name="groups">Group IDs of the script groups where the script is added.</param>
        /// <param name="connection">The database connection.</param>
        /// <param name="transaction">Database transaction used for the update.</param>
        private static void AddScriptToScriptGroups(GXDbConnection connection,
            Guid scriptId, IEnumerable<GXScriptGroup> groups, System.Data.IDbTransaction? transaction = null)
        {
            DateTime now = DateTime.Now;
            List<GXScriptGroupScript> list = [];
            foreach (GXScriptGroup it in groups)
            {
                list.Add(new GXScriptGroupScript()
                {
                    ScriptId = scriptId,
                    ScriptGroupId = it.Id,
                    CreationTime = now
                });
            }
            connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between script group and script.
        /// </summary>
        /// <param name="scriptId">Script ID.</param>
        /// <param name="groups">Group IDs of the script groups where the script is removed.</param>
        /// <param name="connection">The database connection.</param>
        /// <param name="transaction">Database transaction used for the update.</param>
        private static void RemoveScriptsFromScriptGroup(GXDbConnection connection,
            Guid scriptId, IEnumerable<GXScriptGroup> groups, System.Data.IDbTransaction? transaction = null)
        {
            foreach (var it in groups)
            {
                connection.Delete(transaction, GXDeleteArgs.Delete<GXScriptGroupScript>(w => w.ScriptId == scriptId && w.ScriptGroupId == it.Id));
            }
        }

        /// <summary>
        /// Add script methods to script.
        /// </summary>
        /// <param name="script">Script where methods are added.</param>
        /// <param name="methods">Added script methods.</param>
        /// <param name="connection">The database connection.</param>
        /// <param name="transaction">Database transaction used for the update.</param>
        private static void AddScriptMethodsToScript(GXDbConnection connection,
            GXScript script, IEnumerable<GXScriptMethod>? methods, System.Data.IDbTransaction? transaction = null)
        {
            if (methods == null)
            {
                return;
            }
            foreach (GXScriptMethod it in methods)
            {
                it.Script = script;
            }
            connection.Insert(transaction, GXInsertArgs.InsertRange(methods));
        }

        /// <summary>
        /// Remove script methods from the script.
        /// </summary>
        /// <param name="methods">Removed script methods.</param>
        /// <param name="connection">The database connection.</param>
        /// <param name="transaction">Database transaction used for the update.</param>
        private static void RemoveScriptMethodsFromScript(GXDbConnection connection,
            IEnumerable<GXScriptMethod> methods, System.Data.IDbTransaction? transaction = null)
        {
            foreach (var it in methods)
            {
                connection.Delete(transaction, GXDeleteArgs.DeleteById<GXScriptMethod>(it.Id));
            }
        }

        /// <inheritdoc />
        public byte[]? Compile(
            string fileName,
            string script,
            string? additionalNamespaces,
            List<GXScriptMethod>? methods,
            out string? errorJson,
            out int compileTime,
            CancellationToken cancellationToken)
        {
            _authorizationService.EnsureAnyPolicyAsync(_user, GXScriptPolicies.Add, GXScriptPolicies.Edit).Wait(cancellationToken);
            errorJson = null;
            if (string.IsNullOrEmpty(script))
            {
                throw new ArgumentException("No script to validate.", nameof(script));
            }
            List<GXScriptException> errors = [];
            DateTime start = DateTime.Now;
            List<MethodDeclarationSyntax> scriptMethods = [];
            byte[]? byteAssembly = GXScriptCompiler.Compile(fileName, ScriptLanguage.CSharp,
                script, additionalNamespaces, errors, scriptMethods);
            if (methods != null && scriptMethods.Count != 0)
            {
                //Add all public methods that are not static.
                foreach (MethodDeclarationSyntax it in scriptMethods)
                {
                    if (!it.Modifiers.Where(w => w.IsKind(SyntaxKind.PublicKeyword)).Any())
                    {
                        continue;
                    }
                    if (it.Modifiers.Where(w => w.IsKind(SyntaxKind.StaticKeyword)).Any())
                    {
                        continue;
                    }
                    string description = "";
                    var trivia = it.GetLeadingTrivia().SingleOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                    t.IsKind(SyntaxKind.MultiLineCommentTrivia));
                    if (trivia != default)
                    {
                        var xml = trivia.GetStructure();
                        if (xml == null)
                        {
                            continue;
                        }
                        foreach (var line in xml.GetText().Lines)
                        {
                            string str = line.ToString().Replace("///", "").TrimStart();
                            if (!str.StartsWith("<summary>") &&
                                !str.StartsWith("</summary>"))
                            {
                                description += str;
                            }
                        }
                        Console.WriteLine(xml);
                    }
                    GXScriptMethod m = new()
                    {
                        Description = description,
                        Name = it.Identifier.Text,
                        IsFunction = it.ReturnType.ToString() != "void"
                    };

                    methods.Add(m);
                }
            }
            compileTime = (int)(DateTime.Now - start).TotalMilliseconds;
            if (errors.Count != 0)
            {
                errorJson = JsonSerializer.Serialize(errors);
            }
            return byteAssembly;
        }

        /// <inheritdoc />
        public async Task<object?> RunAsync(Guid methodId, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(_user, GXScriptPolicies.Run);
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXScriptMethod>(q => q.Id == methodId);
            arg.Columns.Add<GXScript>();
            arg.Joins.AddInnerJoin<GXScriptMethod, GXScript>(j => j.Script!, j => j.Id);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            GXScriptMethod? method = connection.SingleOrDefault<GXScriptMethod>(arg);
            if (method == null || method.Script == null || method.Script.ByteAssembly == null)
            {
                throw new GXAmiForbiddenException(methodId.ToString());
            }
            await GXAutomationPolicyAccess.EnsureAsync(connection, _authorizationService, _user, typeof(GXScript), method.Script.Id, cancellationToken);
            GXAmiScript tmp = new(_serviceProvider);
            if (_user != null)
            {
                string? userId = ServerHelpers.GetUserId(_user);
                if (userId != null)
                {
                    arg = GXSelectArgs.SelectAll<GXUser>(q => q.Id == userId && q.Removed == null);
                    tmp.User = connection.SingleOrDefault<GXUser>(arg);
                    tmp.Sender = tmp.User;
                }
            }
            GXScriptRunArgs args = new()
            {
                ByteAssembly = method.Script.ByteAssembly,
                MethodName = method.Name ?? throw new ArgumentException("Script method name is missing."),
                Asyncronous = method.IsAsynchronous
            };
            try
            {
                return await tmp.RunAsync(args);
            }
            finally
            {
                //Unload script after the run.
                args.AssemblyLoadContext?.Unload();
            }
        }

        /// <summary>
        /// Update script shared version after scripts are updated.
        /// </summary>
        private static void UpdateScriptSharedVersion(GXDbConnection connection)
        {
            var args = GXSelectArgs.Select<GXConfiguration>(
                s => new { s.Id, s.Settings },
                w => w.Name == GXConfigurations.Scripts);
            try
            {
                GXConfiguration? conf = connection.SingleOrDefault<GXConfiguration>(args);
                ScriptSettings? settings = null;
                if (conf == null)
                {
                    return;
                }
                if (!string.IsNullOrEmpty(conf.Settings))
                {
                    settings = JsonSerializer.Deserialize<ScriptSettings>(conf.Settings);
                }
                settings ??= new ScriptSettings();
                settings.CurrentVersions = settings.Versions;
                conf.Settings = JsonSerializer.Serialize(settings);
                var u = GXUpdateArgs.Update(conf, c => new { c.Updated, c.Settings });
                connection.Update(u);
            }
            catch (Exception)
            {
            }
        }

        /// <inheritdoc />
        public async Task RebuildAsync(IEnumerable<Guid>? ids, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(_user, GXScriptPolicies.Edit);
            DateTime now = DateTime.Now;
            List<Guid> list = [];
            List<GXScript> scripts = [];
            if (ids == null || !ids.Any())
            {
                //All scripts are updated.
                scripts.AddRange(await ListAsync(new ListScripts { Select = ["SourceCode", "Assembly"] }, null, cancellationToken));
            }
            else
            {
                foreach (Guid id in ids)
                {
                    var script = await ReadAsync(id, null, cancellationToken);
                    scripts.Add(script);
                }
            }
            List<GXLog> logs = [];
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            foreach (var resource in scripts)
                {
                await GXWorkflowIdentityAccess.ValidateScriptMutationAsync(connection, _user, resource.Id, cancellationToken);
            }
            foreach (GXScript script in scripts)
            {
                try
                {
                    //Update compiled script if it's changed.
                    UpdateScript(script, out int compileTime, cancellationToken);
                    using (var transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable))
                    {
                        var previousStamp = script.ConcurrencyStamp;
                        script.Updated = now;
                        script.ConcurrencyStamp = Guid.NewGuid().ToString();
                        var update = GXUpdateArgs.Update(script, c => new { c.Updated, c.ConcurrencyStamp, c.ByteAssembly });
                        update.Where.And<GXScript>(s => s.ConcurrencyStamp == previousStamp);
                        if (connection.Update(transaction, update) != 1)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }
                        GXScriptDependencyStorage.Replace(connection, transaction, script.Id, script.CompilationDependencies!);
                        transaction.Commit();
                    }
                    GXHistoryRuntimeEffects.EnqueueScript(script, _workflowHandler, deleted: true);
                    //Notification the workflow handler that the scipt has been updated.
                    //This will cause that script is reload.
                    GXHistoryRuntimeEffects.EnqueueScript(script, _workflowHandler);
                    logs.Add(new GXLog(TraceLevel.Info)
                    {
                        CreationTime = DateTime.Now,
                        Creator = script.Id,
                        Message = Properties.Resources.ScriptRebuild
                    });
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    //Disable the script.
                    script.Enabled = false;
                    GXUpdateArgs args = GXUpdateArgs.Update(script, c => new { c.Enabled });
                    connection.Update(args);
                    logs.Add(new GXLog(TraceLevel.Error)
                    {
                        CreationTime = DateTime.Now,
                        Creator = script.Id,
                        Message = string.Format(Properties.Resources.ScriptRebuildFailed, ex.Message),
                        StackTrace = ex.StackTrace
                    });
                }
            }
            UpdateScriptSharedVersion(connection);
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                var scriptLogRepository = scope.ServiceProvider.GetRequiredService<ILogRepository>().ForSource("script");
                await scriptLogRepository.AddAsync("Rebuild", logs, cancellationToken);
            }
            foreach (GXScript script in scripts)
            {
                _notificationCache.Add(await GetNotifiedUsers(script.Id, NotificationAction.Edit, connection),
                    new GXTarget(GXScriptPolicies.Edit, script.Id, script.Name));
            }
        }
    }
}
