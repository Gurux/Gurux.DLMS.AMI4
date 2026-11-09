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
using System.Linq.Expressions;
using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

using System.Text.Json;
using Gurux.DLMS.AMI.Hubs;
using System.Security.Claims;
using Gurux.Service.Orm.Enums;

namespace Gurux.DLMS.AMI.Server.Repository
{

    /// <inheritdoc />
    /// <summary>
    /// Constructor.
    /// </summary>
    public class LocalizedResourceRepository(
        IAuthorizationService authorizationService,
        IDbConnectionFactory dbFactory,
        IHttpContextAccessor httpContextAccessor,
        IUserRepository userRepository,
        IEnumTypeRepository enumTypeRepository,
        ILogRepository systemLogRepository,
        IConfigurationRepository configurationRepository,
        NotificationCache notificationCache) : ILocalizedResourceRepository
    {
        private readonly ClaimsPrincipal? User = httpContextAccessor.HttpContext?.User;
        private readonly IDbConnectionFactory _dbFactory = dbFactory;
        private readonly IUserRepository _userRepository = userRepository;
        private readonly NotificationCache _notificationCache = notificationCache;
        private readonly IEnumTypeRepository _enumTypeRepository = enumTypeRepository;
        private readonly ILogRepository _systemLogRepository = systemLogRepository.ForSource("system");
        private readonly IConfigurationRepository _configurationRepository = configurationRepository;
        private readonly IAuthorizationService _authorizationService = authorizationService;

        private async Task<List<string>> GetNotifiedUsers(Guid id, NotificationAction action)
        {
            _ = id;
            GXSelectArgs args = GXQuery.GetUsersByPermission(action, GXLocalizedResourcePolicies.View);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            List<GXUser> users = connection.Select<GXUser>(args);
            return [.. users.Select(s => s.Id!)];
        }

        /// <inheritdoc />
        public async Task DeleteAsync(IEnumerable<Guid> resources, CancellationToken cancellationToken = default)
        {
            resources = resources.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXLocalizedResource, Guid>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, resources,
        async () =>
        {
            try
            {

                if (User?.IsInRole(GXRoles.Admin) != true)
                {
                    throw new UnauthorizedAccessException();
                }
                GXSelectArgs arg = GXSelectArgs.Select<GXLocalizedResource>(a => a.Id, q => resources.Contains(q.Id));
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                List<GXLocalizedResource> list = connection.Select<GXLocalizedResource>(arg);
                foreach (var resource in list)
                {
                    await connection.DeleteAsync(GXDeleteArgs.DeleteById<GXLocalizedResource>(resource.Id), cancellationToken);
                }

                if (list.Count != 0)
                {
                    await Update(cancellationToken);
                }

                var users = await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]);
                users = await GetNotifiedUsers(Guid.Empty, NotificationAction.Remove);
                if (users != null)
                {
                    _notificationCache.Add(users, new GXTarget(GXLocalizedResourcePolicies.Delete));
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
                        await DeleteAsync(resources, linkedCts.Token);
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
        public async Task<IEnumerable<GXLocalizedResource>> ListAsync(
            ListLocalizedResources? request,
            ListLocalizedResourcesResponse? response,
            CancellationToken cancellationToken = default)
        {
            bool isAdmin = User?.IsInRole(GXRoles.Admin) == true;
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXLocalizedResource>();
            arg.Columns.Add<GXLanguage>(s => new { s.Id });
            arg.Columns.Exclude<GXLanguage>(s => new { s.Resources });
            arg.Joins.AddInnerJoin<GXLocalizedResource, GXLanguage>(j => j.Language!, j => j.Id);
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
            }
            arg.OrderBy.Add<GXLanguage>(q => q.Id);
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXLocalizedResource>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXLocalizedResource>(q => q.Value!);
                arg.Descending = true;
            }
            arg.Distinct = true;
            GXDateTimeFilters.Apply<GXLocalizedResource>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXLocalizedResource>(q => GXSql.Count(1));
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
            List<GXLocalizedResource> resources = [.. (await connection.SelectAsync<GXLocalizedResource>(arg))];
            if (request?.Select?.Contains(TargetType.Language) == true && resources.Count != 0)
            {
                //Get localized texts.
                var ids = resources.Select(x => x.Id).ToList();
                var hashs = resources.Select(x => x.Hash).ToList();
                arg = GXSelectArgs.SelectAll<GXLocalizedResource>();
                arg.Columns.Add<GXLanguage>(s => new { s.Id });
                arg.Columns.Exclude<GXLanguage>(s => new { s.Resources });
                arg.Joins.AddInnerJoin<GXLocalizedResource, GXLanguage>(j => j.Language!, j => j.Id);
                arg.Where.And<GXLanguage>(w => w.Enabled == true);
                arg.Where.And<GXLocalizedResource>(w => hashs.Contains(w.Hash) && !ids.Contains(w.Id));
                resources.AddRange((await connection.SelectAsync<GXLocalizedResource>(arg)));
            }
            if (response != null)
            {
                response.LocalizedResources = [.. resources];
                if (response.Count == 0)
                {
                    response.Count = resources.Count;
                }
            }
            return [.. resources];
        }

        /// <inheritdoc />
        public async Task<GXLocalizedResource> ReadAsync(Guid id,
            Expression<Func<GXLocalizedResource, object>>? columns = null,
            CancellationToken cancellationToken = default)
        {
            try
            {

                GXSelectArgs arg = GXSelectArgs.SelectAll<GXLocalizedResource>(w => w.Id == id);
                arg.Columns.Add<GXLanguage>(s => new { s.Id, s.NativeName, s.EnglishName });
                arg.Joins.AddLeftJoin<GXLocalizedResource, GXLanguage>(j => j.Language!, j => j.Id);
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                GXLocalizedResource? resource = await connection.SingleOrDefaultAsync<GXLocalizedResource>(arg) ?? throw new GXAmiForbiddenException(id.ToString());
                return resource;

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
        public async Task<GXLocalizedResource?> ReadAsync(string lang, string hash, string? text,
            CancellationToken cancellationToken = default)
        {
            try
            {

                if (string.IsNullOrEmpty(lang))
                {
                    throw new ArgumentNullException(nameof(lang), "Language information is missing.");
                }
                GXSelectArgs arg = GXSelectArgs.SelectAll<GXLocalizedResource>(w => w.Hash == hash);
                arg.Where.And<GXLanguage>(w => w.Id == lang);
                arg.Columns.Add<GXLanguage>(s => new { s.Id, s.NativeName, s.EnglishName });
                arg.Columns.Exclude<GXLanguage>(e => e.Resources!);
                arg.Joins.AddLeftJoin<GXLocalizedResource, GXLanguage>(j => j.Language!, j => j.Id);
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                GXLocalizedResource? resource = await connection.SingleOrDefaultAsync<GXLocalizedResource>(arg);
                // English is the built-in source language. New or differently cased source
                // text needs no translation row; keep explicit database overrides above.
                if (resource == null && !string.IsNullOrEmpty(text) &&
                    (lang.Equals("en", StringComparison.OrdinalIgnoreCase) || lang.StartsWith("en-", StringComparison.OrdinalIgnoreCase)))
                {
                    return new GXLocalizedResource { Hash = hash, Value = text };
                }
                if (resource == null)
                {
                    arg = GXSelectArgs.SelectAll<GXLanguage>(w => w.Id == lang);
                    var l = await connection.SingleOrDefaultAsync<GXLanguage>(arg);
                    if (l?.Enabled != true)
                    {
                        //Return value for the default default if language is not installed.
                        arg = GXSelectArgs.SelectAll<GXLanguage>(w => w.Default == true);
                        l = await connection.SingleOrDefaultAsync<GXLanguage>(arg);
                        if (l == null)
                        {
                            return new GXLocalizedResource()
                            {
                                Value = text,
                                Hash = hash,
                            };
                        }
                        return await ReadAsync(l.Id, hash, text, cancellationToken);
                    }
                    if (!string.IsNullOrEmpty(text))
                    {
                        //If the resource is Unknown.
                        if (User?.Identity?.IsAuthenticated != true)
                        {
                            //Return value for unauthenticated users.
                            return new GXLocalizedResource()
                            {
                                Value = text,
                                Hash = hash,
                            };
                        }
                        GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
                        GXLog log = new(TraceLevel.Error)
                        {
                            Type = await _enumTypeRepository.GetLogTypeAsync(TargetType.SystemLog, TargetType.LocalizedResource, cancellationToken),
                            Message = string.Format("Localized resource '{0}' is missing for {1}.", text, lang)
                        };
                        await _systemLogRepository.AddAsync(TargetType.LocalizedResource, [log], cancellationToken);
                        //Create Unknown resource.
                        resource = new GXLocalizedResource()
                        {
                            Status = await _enumTypeRepository.GetLogTypeAsync(TargetType.LocalizedResource, "Unknown", cancellationToken),
                            Language = l,
                            Hash = hash,
                            CreationTime = DateTime.Now,
                            Creator = creator,
                            Value = text,
                        };
                        await UpsertAsync([resource], cancellationToken);
                    }
                }
                return resource;

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
                        return await ReadAsync(lang, hash, text, linkedCts.Token);
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

        /// <summary>
        /// Set the new update time.
        /// </summary>
        private Task Update(CancellationToken cancellationToken) => new GXResourceVersions(_dbFactory).TouchTextsAsync(cancellationToken);

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> UpsertAsync(IEnumerable<GXLocalizedResource> resources, CancellationToken cancellationToken = default)
        {
            resources = resources.ToArray();
            return await GXHistoryMutation.RunAsync<GXLocalizedResource, IEnumerable<Guid>>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, resources,
        async () =>
        {
            try
            {

                if (User?.IsInRole(GXRoles.Admin) != true)
                {
                    throw new UnauthorizedAccessException();
                }
                DateTime now = DateTime.Now;
                List<GXLocalizedResource> updates = [];
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                foreach (GXLocalizedResource resource in resources)
                {
                    if (string.IsNullOrEmpty(resource.Value))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    if (string.IsNullOrEmpty(resource.Language?.Id))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    //Get resource ID.
                    var arg = GXSelectArgs.SelectAll<GXLocalizedResource>(w => w.Id == resource.Id);
                    GXLocalizedResource? tmp = await connection.SingleOrDefaultAsync<GXLocalizedResource>(arg);
                    if (tmp?.Id != null)
                    {
                        resource.Id = tmp.Id;
                    }
                    if (resource.Id == Guid.Empty)
                    {
                        GXInsertArgs args = GXInsertArgs.Insert(resource);
                        resource.Hash = ClientHelpers.GetHashCode(resource.Value);
                        //Check that hash doesn't alredy exists.
                        GXSelectArgs m = GXSelectArgs.Select<GXLocalizedResource>(q => q.Id, where => where.Hash == resource.Hash);
                        m.Joins.AddRightJoin<GXLocalizedResource, GXLanguage>(j => j.Language!, j => j.Id);
                        m.Where.And<GXLanguage>(w => w.Id == resource.Language.Id);
                        GXLocalizedResource? exists = connection.SingleOrDefault<GXLocalizedResource>(m);
                        if (exists != null)
                        {
                            throw new ArgumentException("Localized string already exists.");
                        }
                        connection.Insert(args);
                    }
                    else
                    {
                        GXSelectArgs m = GXSelectArgs.Select<GXLocalizedResource>(q => new { q.ConcurrencyStamp }, where => where.Id == resource.Id);
                        GXLocalizedResource? old = connection.SingleOrDefault<GXLocalizedResource>(m);
                        if (old == null || !string.IsNullOrEmpty(old.ConcurrencyStamp) && old.ConcurrencyStamp != resource.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }
                        resource.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs args = GXUpdateArgs.Update(resource);
                        //Hash is not updated after the creation.
                        if (User.IsInRole(GXRoles.Admin) && resource.Creator?.Id != null)
                        {
                            args.Exclude<GXLocalizedResource>(e => new { e.CreationTime, e.Hash });
                        }
                        else
                        {
                            args.Exclude<GXLocalizedResource>(e => new { e.CreationTime, e.Creator, e.Hash });
                        }
                        connection.Update(args);
                        updates.Add(new GXLocalizedResource()
                        {
                            Id = resource.Id,
                            Hash = resource.Hash,
                            Value = resource.Value,
                        });
                    }
                }
                if (resources.Any())
                {
                    await Update(cancellationToken);
                    var users = await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]);
                    _notificationCache.Add(users, new GXTarget(GXLocalizedResourcePolicies.Edit));
                }
                return [.. resources.Select(s => s.Id)];

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
                        return await UpsertAsync(resources, linkedCts.Token);
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
        public async Task<DateTimeOffset?> LastChanged()
        {
            return await new GXResourceVersions(_dbFactory).TextsAsync();
        }
    }
}
