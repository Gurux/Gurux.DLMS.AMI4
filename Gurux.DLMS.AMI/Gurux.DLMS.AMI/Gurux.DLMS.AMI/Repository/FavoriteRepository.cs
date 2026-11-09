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
using Gurux.DLMS.AMI.Client.Pages.DeviceTemplate;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Claims;


namespace Gurux.DLMS.AMI.Server.Repository
{

    /// <inheritdoc />
    /// <summary>
    /// Constructor.
    /// </summary>
    public class FavoriteRepository(
        IAuthorizationService authorizationService,
        IDbConnectionFactory dbFactory,
        IHttpContextAccessor httpContextAccessor,
        NotificationCache notificationCache,
        Gurux.DLMS.AMI.Module.IGXAmiModuleServices modules) : IFavoriteRepository
    {

        private readonly ClaimsPrincipal? User = httpContextAccessor.HttpContext?.User;
        private readonly IDbConnectionFactory _dbFactory = dbFactory;
        private readonly NotificationCache _notificationCache = notificationCache;
        private readonly IAuthorizationService _authorizationService = authorizationService;
        private readonly Gurux.DLMS.AMI.Module.IGXAmiModuleServices _modules = modules;

        /// <inheritdoc />
        public async Task DeleteAsync(IEnumerable<Guid> userGrouprs, CancellationToken cancellationToken = default)
        {
            userGrouprs = userGrouprs.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXFavorite, Guid>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, userGrouprs,
        async () =>
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXUserPolicies.View);
                string userId = ServerHelpers.GetUserId(User);
                GXSelectArgs arg = GXSelectArgs.Select<GXFavorite>(a => new { a.Id, a.Name, a.Path },
                    q => userGrouprs.Contains(q.Id));
                var owner = new GXUser { Id = userId };
                arg.Where.And<GXFavorite>(f => f.User == owner);
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                List<GXFavorite> list = connection.Select<GXFavorite>(arg);
                DateTime now = DateTime.Now;
                foreach (GXFavorite it in list)
                {
                    await connection.DeleteAsync(GXDeleteArgs.DeleteById<GXFavorite>(it.Id), cancellationToken);
                }
                foreach (var favorite in list)
                    {
                    _notificationCache.Add(userId, new GXTarget(GXFavoritePolicies.Delete, favorite.Id, favorite.Name));
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
                        await DeleteAsync(userGrouprs, linkedCts.Token);
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
        public async Task<IEnumerable<GXFavorite>> ListAsync(
            ListFavorites? request,
            ListFavoritesResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXFavoritePolicies.View);
            string userId = ServerHelpers.GetUserId(User);
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXFavorite>();
            arg.Joins.AddInnerJoin<GXFavorite, GXUser>(j => j.User!, j => j.Id!);
            arg.Where.And<GXUser>(w => w.Id == userId);
            if (request != null && request.Filter != null)
            {
                arg.Where.FilterBy(request.Filter);
            }
            GXDateTimeFilters.Apply<GXFavorite>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXFavorite>(q => GXSql.Count(1));
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
                arg.OrderBy.Add<GXFavorite>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXFavorite>(q => q.CreationTime!);
            }
            GXFavorite[] favorites = [.. (await connection.SelectAsync<GXFavorite>(arg))];
            foreach (var favorite in favorites)
            {
                await RefreshModuleLabelAsync(favorite, cancellationToken);
            }

            if (response != null)
            {
                response.Favorites = favorites;
                if (response.Count == 0)
                {
                    response.Count = favorites.Length;
                }
            }
            return favorites;
        }

        /// <inheritdoc />
        public async Task<GXFavorite> ReadAsync(Guid id, Expression<Func<GXFavorite, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXFavoritePolicies.View);
                string userId = ServerHelpers.GetUserId(User);
                GXSelectArgs arg = GXSelectArgs.SelectAll<GXFavorite>();
                arg.Where.And<GXFavorite>(f => f.Id == id);
                arg.Joins.AddInnerJoin<GXFavorite, GXUser>(j => j.User!, j => j.Id!);
                arg.Where.And<GXUser>(w => w.Id == userId);
                arg.Distinct = true;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                var favorite = await connection.SingleOrDefaultAsync<GXFavorite>(arg) ?? throw new GXAmiForbiddenException(id.ToString());
                await RefreshModuleLabelAsync(favorite, cancellationToken);
                return favorite;

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
        private async Task RefreshModuleLabelAsync(GXFavorite favorite, CancellationToken token)
        {
            var path = favorite.Path?.TrimStart('/').Split('/') ?? [];
            string? target = null, targetId = null;
            if (path.Length >= 3 && path[0].Equals("addin", StringComparison.OrdinalIgnoreCase))
            {
                target = path[2];
                targetId = path.Length > 3 ? path[3] : "";
            }
            else if (path.Length >= 2 && path[0].Equals("Content", StringComparison.OrdinalIgnoreCase))
            {
                target = path[0];
                targetId = path.Last();
            }
            if (target == null)
            {
                return;
            }

            try
            {
                var resolved = await _modules.InvokeAsync<Gurux.DLMS.AMI.Module.IGXAmiTargetProvider, Gurux.DLMS.AMI.Module.GXAmiTarget?>((provider, ct) => provider.ResolveAsync(User ?? throw new UnauthorizedAccessException(), target, targetId!, ct), token);
                favorite.Name = resolved?.Name;
            }
            catch (Gurux.DLMS.AMI.Module.AmiModuleUnavailableException)
            {
                favorite.Name = null;
            }
            catch (UnauthorizedAccessException)
            {
                favorite.Name = null;
            }
        }

        /// <summary>Performs the update operation.</summary>
        /// <param name="favorites">The favorites.</param>
        /// <param name="columns">The columns to select or update.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        public async Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXFavorite> favorites,
            Expression<Func<GXFavorite, object>>? columns, CancellationToken cancellationToken = default)
        {
            favorites = favorites.ToArray();
            return await GXHistoryMutation.RunAsync<GXFavorite, IEnumerable<Guid>>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, favorites,
        async () =>
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXUserPolicies.View);
                favorites = favorites.ToList();
                GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
                DateTime now = DateTime.Now;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                foreach (GXFavorite it in favorites)
                {
                    if (string.IsNullOrEmpty(it.Path))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    GXSelectArgs? args = null;
                    string[] tmp = it.Path.Split('/');
                    string? action = null;
                    string? id = null;
                    if (tmp.Length >= 3 && tmp[0].Equals("addin", StringComparison.OrdinalIgnoreCase))
                    {
                        var resolved = await _modules.InvokeAsync<IGXAmiTargetProvider, GXAmiTarget?>((provider, ct) => provider.ResolveAsync(User ?? throw new UnauthorizedAccessException(), tmp[2], tmp.Length > 3 ? tmp[3] : "", ct), cancellationToken);
                        if (resolved == null || !resolved.Path.StartsWith("addin/" + tmp[1] + "/", StringComparison.OrdinalIgnoreCase))
                        {
                            throw new UnauthorizedAccessException();
                        }

                        it.Type = tmp[2];
                        it.Name = resolved.Name;
                    }
                    else if (tmp.Length == 1)
                    {
                        it.Type = it.Path;
                        if (string.Compare(it.Type, "Config", true) == 0)
                        {
                            it.Type = "Configuration";
                            it.Name = it.Type;
                        }
                    }
                    else if (tmp.Length == 2)
                    {
                        if (string.Compare(tmp[0], "Config", true) == 0)
                        {
                            it.Type = "Configuration";
                            it.Name = tmp[1];
                        }
                        else
                        if (string.Compare(tmp[0], "Logs", true) == 0)
                        {
                            it.Type = "Log";
                            it.Name = tmp[1];
                        }
                    }
                    else if (tmp.Length == 4)
                    {
                        if (string.Compare(tmp[0], "Config", true) == 0)
                        {
                            it.Type = tmp[1];
                            action = tmp[2];
                            id = tmp[3];
                            if (string.Compare(it.Type, "Module") != 0)
                            {
                                Guid Id = Guid.Parse(id);
                                switch (it.Type.ToLower())
                                {
                                    case "modulegroup":
                                        args = GXSelectArgs.Select<GXModuleGroup>(s => s.Name!, w => w.Id == Id);
                                        break;
                                    case "blockgroup":
                                        args = GXSelectArgs.Select<Gurux.DLMS.AMI.Shared.DTOs.Block.GXBlockGroup>(s => s.Name!, w => w.Id == Id);
                                        break;
                                    case "component":
                                        args = GXSelectArgs.Select<GXComponentView>(s => s.Name!, w => w.Id == Id);
                                        break;
                                    case "componentgroup":
                                        args = GXSelectArgs.Select<GXComponentViewGroup>(s => s.Name!, w => w.Id == Id);
                                        break;
                                    case "workflowgroup":
                                        args = GXSelectArgs.Select<GXWorkflowGroup>(s => s.Name!, w => w.Id == Id);
                                        break;
                                    case "scriptgroup":
                                        args = GXSelectArgs.Select<GXScriptGroup>(s => s.Name!, w => w.Id == Id);
                                        break;
                                    case "triggergroup":
                                        args = GXSelectArgs.Select<GXTriggerGroup>(s => s.Name!, w => w.Id == Id);
                                        break;
                                    case "manufacturergroup":
                                        args = GXSelectArgs.Select<GXManufacturerGroup>(s => s.Name!, w => w.Id == Id);
                                        break;
                                    case "keymanagement":
                                        args = GXSelectArgs.Select<GXKeyManagement>(s => s.Name!, w => w.Id == Id);
                                        break;
                                    case "gateway":
                                        args = GXSelectArgs.Select<GXGateway>(s => s.Name!, w => w.Id == Id);
                                        break;
                                }
                            }
                        }
                    }
                    else if (tmp.Length > 2)
                    {
                        it.Name = it.Type = tmp[0];
                        action = tmp[1];
                        id = tmp[2];
                    }
                    string? target = it.Name;
                    if (id != null && target != null)
                    {
                        Guid? Id = null;
                        if (target != TargetType.User &&
                            target != TargetType.Module)
                        {
                            Id = Guid.Parse(id);
                        }
                        switch (target)
                        {
                            case TargetType.Device:
                                args = GXSelectArgs.Select<GXDevice>(s => s.Name!, w => w.Id == Id);
                                break;
                            case TargetType.Object:
                                args = GXSelectArgs.Select<GXObjectTemplate>(s => s.Name!);
                                args.Joins.AddInnerJoin<GXObjectTemplate, GXObject>(j => j.Id, j => j.Template!);
                                args.Where.And<GXObject>(w => w.Id == Id);
                                break;
                            case TargetType.Attribute:
                                args = GXSelectArgs.Select<GXAttributeTemplate>(s => s.Name!);
                                args.Joins.AddInnerJoin<GXAttributeTemplate, GXAttribute>(j => j.Id, j => j.Template!);
                                args.Where.And<GXAttribute>(w => w.Id == Id);
                                break;
                            case TargetType.Value:
                                break;
                            case TargetType.Task:
                                break;
                            case TargetType.DeviceLog:
                                break;
                            case TargetType.SystemLog:
                                break;
                            case TargetType.UserError:
                            case TargetType.ScheduleLog:
                                break;
                            case TargetType.WorkflowLog:
                                break;
                            case TargetType.AgentLog:
                                break;
                            case TargetType.ScriptLog:
                                break;
                            case TargetType.ModuleLog:
                                break;
                            case TargetType.DeviceAction:
                                break;
                            case TargetType.Token:
                                break;
                            case TargetType.UserAction:
                                break;
                            case TargetType.Schedule:
                                args = GXSelectArgs.Select<GXSchedule>(s => s.Name!, w => w.Id == Id);
                                break;
                            case TargetType.Agent:
                                args = GXSelectArgs.Select<GXAgent>(s => s.Name!, w => w.Id == Id);
                                break;
                            case TargetType.DeviceTemplate:
                                args = GXSelectArgs.Select<GXDeviceTemplate>(s => s.Name!, w => w.Id == Id);
                                break;
                            case TargetType.ObjectTemplate:
                                args = GXSelectArgs.Select<GXObjectTemplate>(s => s.Name!, w => w.Id == Id);
                                break;
                            case TargetType.AttributeTemplate:
                                args = GXSelectArgs.Select<GXAttributeTemplate>(s => s.Name!, w => w.Id == Id);
                                break;
                            case TargetType.UserGroup:
                                args = GXSelectArgs.Select<GXUserGroup>(s => s.Name!, w => w.Id == Id);
                                break;
                            case TargetType.User:
                                args = GXSelectArgs.Select<GXUser>(s => s.UserName!, w => w.Id == id);
                                break;
                            case TargetType.Configuration:
                                args = GXSelectArgs.Select<GXConfiguration>(s => s.Name!, w => w.Id == Id);
                                break;
                            case TargetType.Module:
                                it.Name = id;
                                break;
                            case TargetType.Workflow:
                                args = GXSelectArgs.Select<GXWorkflow>(s => s.Name!, w => w.Id == Id);
                                break;
                            case TargetType.Script:
                                args = GXSelectArgs.Select<GXScript>(s => s.Name!, w => w.Id == Id);
                                break;
                            case TargetType.Content:
                                try
                                {
                                    var moduleTarget = await _modules.InvokeAsync<Gurux.DLMS.AMI.Module.IGXAmiTargetProvider, Gurux.DLMS.AMI.Module.GXAmiTarget?>((provider, ct) => provider.ResolveAsync(User ?? throw new UnauthorizedAccessException(), it.Type!, id, ct), cancellationToken);
                                    it.Name = moduleTarget?.Name;
                                }
                                catch (Gurux.DLMS.AMI.Module.AmiModuleUnavailableException)
                                {
                                    it.Name = null;
                                }
                                break;
                            case TargetType.Trigger:
                                args = GXSelectArgs.Select<GXTrigger>(s => s.Name!, w => w.Id == Id);
                                break;
                            case TargetType.TriggerGroup:
                                args = GXSelectArgs.Select<GXTriggerGroup>(s => s.Name!, w => w.Id == Id);
                                break;
                            case TargetType.DeviceGroup:
                                args = GXSelectArgs.Select<GXDeviceGroup>(s => s.Name!, w => w.Id == Id);
                                break;
                            case TargetType.ScheduleGroup:
                                args = GXSelectArgs.Select<GXScheduleGroup>(s => s.Name!, w => w.Id == Id);
                                break;
                            case TargetType.AgentGroup:
                                args = GXSelectArgs.Select<GXAgentGroup>(s => s.Name!, w => w.Id == Id);
                                break;
                            case TargetType.ScriptGroup:
                                args = GXSelectArgs.Select<GXScriptGroup>(s => s.Name!, w => w.Id == Id);
                                break;
                            case TargetType.DeviceTemplateGroup:
                                args = GXSelectArgs.Select<GXDeviceTemplateGroup>(s => s.Name!, w => w.Id == Id);
                                break;
                            case TargetType.ComponentView:
                                args = GXSelectArgs.Select<GXComponentView>(s => s.Name!, w => w.Id == Id);
                                break;
                            case TargetType.ComponentViewGroup:
                                args = GXSelectArgs.Select<GXComponentViewGroup>(s => s.Name!, w => w.Id == Id);
                                break;
                            case TargetType.Role:
                                args = GXSelectArgs.Select<GXRole>(s => s.Name!, w => w.Id == id);
                                break;
                            case TargetType.Manufacturer:
                                args = GXSelectArgs.Select<GXManufacturer>(s => s.Name!, w => w.Id == Id);
                                break;
                            case TargetType.ManufacturerGroup:
                                args = GXSelectArgs.Select<GXManufacturerGroup>(s => s.Name!, w => w.Id == Id);
                                break;
                            case TargetType.Gateway:
                                args = GXSelectArgs.Select<GXGateway>(s => s.Name!, w => w.Id == Id);
                                break;
                            case TargetType.GatewayGroup:
                                args = GXSelectArgs.Select<GXGatewayGroup>(s => s.Name!, w => w.Id == Id);
                                break;
                            case TargetType.Performance:
                                args = GXSelectArgs.Select<GXPerformance>(s => new { s.Start, s.Target }, w => w.Id == Id);
                                break;
                            case TargetType.Appearance:
                                args = GXSelectArgs.Select<GXAppearance>(s => s.Name, w => w.Id == Id);
                                break;
                        }
                    }
                    if (args != null)
                    {
                        it.Name = await connection.SingleOrDefaultAsync<string>(args);
                        if (it.Name == null)
                        {
                            if (target == TargetType.Object)
                            {
                                //Object is not created yet.
                                return [];
                            }
                            throw new Exception(Properties.Resources.InvalidName);
                        }
                    }
                    if (it.Id == Guid.Empty)
                    {
                        it.CreationTime = now;
                        it.User = creator;
                        GXInsertArgs i = GXInsertArgs.Insert(it);
                        i.Exclude<GXFavorite>(q => q.Updated!);
                        connection.Insert(i);
                        //Remove user so it's not sent as part of the notification.
                        it.User = null;
                    }
                    else
                    {
                        it.Updated = now;
                        GXUpdateArgs u = GXUpdateArgs.Update(it, columns);
                        u.Exclude<GXFavorite>(q => new { q.CreationTime, q.User });
                        connection.Update(u);
                    }
                }
                foreach (var it in favorites)
                {
                    _notificationCache.Add(creator.Id, new GXTarget(GXFavoritePolicies.Add, it.Id, it.Name));
                }
                return [.. favorites.Select(s => s.Id)];

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
                        return await UpsertAsync(favorites, columns, linkedCts.Token);
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
    }
}
