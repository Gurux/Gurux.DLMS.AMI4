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
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Data;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Server.Cron;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Repository;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Server.Triggers;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.Enums;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace Gurux.DLMS.AMI.Server.Midlewares
{
    internal class GXApplicationUserManager(
        IHttpContextAccessor httpContextAccessor,
        IDbConnectionFactory dbFactory,
        IWorkflowHandler workflowHandler,
      IServiceProvider serviceProvider,
        IUserStore<ApplicationUser> userStore, IOptions<IdentityOptions> optionsAccessor,
        IPasswordHasher<ApplicationUser> passwordHasher,
        IEnumerable<IUserValidator<ApplicationUser>> userValidators,
        IEnumerable<IPasswordValidator<ApplicationUser>> passwordValidators, ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors, IServiceProvider services, ILogger<GXApplicationUserManager> logger) : UserManager<ApplicationUser>(userStore, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors,
            services, logger)
    {
        private readonly IDbConnectionFactory _dbFactory = dbFactory;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly IWorkflowHandler _workflowHandler = workflowHandler;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private static readonly SemaphoreSlim RegistrationLock = new(1, 1);

        /// <summary>
        /// This task is used to check that user manager or admin are approved the new user.
        /// </summary>
        /// <param name="user">The user whose email confirmation status is being checked. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the user's email is confirmed; otherwise, false.</returns>
        public override Task<bool> IsEmailConfirmedAsync(ApplicationUser user)
        {
            //Admin must approve the new user.
            return System.Threading.Tasks.Task.FromResult(user.EmailConfirmed && user.IsApproved);
        }

        public override async Task<IdentityResult> CreateAsync(ApplicationUser user, string password)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(password);
            IdentityResult result = await UpdatePasswordHash(user, password, validatePassword: true);
            return result.Succeeded ? await CreateAsync(user) : result;
        }

        public override Task<IdentityResult> CreateAsync(ApplicationUser user)
            => RegisterUserAsync(user, null, null);

        internal async Task<IdentityResult> CreateAsync(ApplicationUser user, string? password,
            IEnumerable<string>? roles, IEnumerable<string>? permissions)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(user);
            if (!string.IsNullOrEmpty(password))
            {
                var result = await UpdatePasswordHash(user, password, validatePassword: true);
                if (!result.Succeeded)
                {
                    return result;
                }
            }
            return await RegisterUserAsync(user, roles, permissions);
        }

        private async Task<IdentityResult> RegisterUserAsync(ApplicationUser user,
            IEnumerable<string>? roles, IEnumerable<string>? permissions)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(user);
            await RegistrationLock.WaitAsync();
            try
            {
                return await CreateRegisteredUserAsync(user, roles, permissions);
            }
            finally
            {
                RegistrationLock.Release();
            }
        }

        private async Task<IdentityResult> CreateRegisteredUserAsync(ApplicationUser user,
            IEnumerable<string>? roles, IEnumerable<string>? permissions)
        {
            bool adminAdds = user.CreationTime != null;
            if (!adminAdds)
            {
                user.CreationTime = DateTime.UtcNow;
            }
            bool emailAvailable = Gurux.DLMS.AMI.Hubs.GXEmailNotifier.IsAvailable(_serviceProvider,
                _serviceProvider.GetService<Gurux.DLMS.AMI.Services.IGXAmiEmailSender>());
            await using var registrationConnection = await _dbFactory.GetConnectionAsync();
            var registration = await GXAccountRegistrationPolicy.ReadAsync(registrationConnection);
            var actor = _httpContextAccessor.HttpContext?.User;
            if (!registration.CanCreate(actor))
            {
                return IdentityResult.Failed(new IdentityError { Code = "RegistrationDisabled", Description = Properties.Resources.RegistrationDisabled });
            }
            bool firstUser = registration.FirstUser;
            user.IsApproved = registration.ApproveNewAccount(actor);
            if (firstUser)
            {
                ServerSettings.AdminAdded = false;
            }
            if (!emailAvailable)
            {
                user.EmailConfirmed = true;
                user.IsApproved = true;
            }
            var ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress;

            Logger.LogInformation("Registration: saving the Identity user.");
            var ret = await base.CreateAsync(user);
            if (ret.Succeeded)
            {
                Logger.LogInformation("Registration: assigning default roles and permissions.");
                await using var connection = await _dbFactory.GetConnectionAsync();
                try
                {
                    string groupName = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Default));
                    if (!firstUser)
                    {
                        var defaults = await connection.SelectAsync<GXRole>(
                            GXSelectArgs.Select<GXRole>(r => r.Name!, r => r.Default == true && r.Removed == null));
                        roles = (roles ?? []).Concat(defaults.Select(r => r.Name!))
                            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                    }
                    //If admin has added the user.
                    if (adminAdds && !firstUser)
                    {
                        if (roles?.Any() == true)
                        {
                            ret = await AddToRolesAsync(user, roles);
                            if (!ret.Succeeded)
                            {
                                connection.Delete(GXDeleteArgs.DeleteById<GXUser>(user.Id));
                                return ret;
                            }
                        }
                        await UserPermissions.ReplaceAsync(connection, user.Id, permissions ?? []);
                        await CreateNotificationsAsync(connection, user);
                        await _workflowHandler.ExecuteAsync(typeof(UserTrigger), UserTrigger.Create, user);
                        return ret;
                    }
                    DateTime now = DateTime.Now;
                    GXUser u = new() { CreationTime = user.CreationTime ?? now, Id = user.Id };
                    if (firstUser)
                    {
                        u.Roles = [GXRoles.Admin];
                    }
                    else
                    {
                        u.Roles = [.. roles ?? []];
                        var defaults = connection.Select<GXPermission>(GXSelectArgs.SelectAll<GXPermission>(p => p.Default == true));
                        u.Permissions = [.. defaults.Select(p => p.Name!)];
                    }
                    if (permissions != null)
                    {
                        u.Permissions = [.. permissions];
                    }
                    ret = await AddToRolesAsync(user, u.Roles);
                    if (!ret.Succeeded)
                    {
                        var error = new GXLog()
                        {
                            CreationTime = DateTime.Now,
                            Message = "Failed to add default roles" + string.Join(';', ret.Errors)
                        };
                        await GXLogStore.InsertAsync(connection, "system", [error]);
                        connection.Delete(GXDeleteArgs.DeleteById<GXUser>(user.Id));
                        return ret;
                    }
                    await UserPermissions.ReplaceAsync(connection, user.Id, u.Permissions ?? Enumerable.Empty<string>());
                    GXUserGroup ug = new(groupName) { Creator = u, CreationTime = now, Default = true };
                    ug.Users?.Add(u);
                    await using (var groupTransaction = await connection.BeginTransactionAsync())
                    {
                        Logger.LogInformation("Registration: creating the default user group.");
                        await UserGroupRepository.UpsertAsync(connection, groupTransaction, ug);
                        //Create default schedule group and add user group to it.
                        GXScheduleGroup sg = new(groupName) { Creator = u, CreationTime = now, Default = true };
                        sg.UserGroups?.Add(ug);
                        List<GXSchedule> list = [];
                        GXSchedule m = new(ServerHelpers.GetInvariantString(nameof(Properties.Resources.Minutely)));
                        GXDateTime dt = new(DateTime.Now.Date)
                        {
                            Skip = DateTimeSkips.Year | DateTimeSkips.Month | DateTimeSkips.Day | DateTimeSkips.Hour | DateTimeSkips.Minute
                        };
                        m.Start = dt.ToFormatString(CultureInfo.InvariantCulture);
                        list.Add(m);
                        GXSchedule h = new(ServerHelpers.GetInvariantString(nameof(Properties.Resources.Hourly)));
                        dt.Skip = DateTimeSkips.Year | DateTimeSkips.Month | DateTimeSkips.Day | DateTimeSkips.Hour;
                        h.Start = dt.ToFormatString(CultureInfo.InvariantCulture);
                        list.Add(h);
                        GXSchedule d = new(ServerHelpers.GetInvariantString(nameof(Properties.Resources.Daily)));
                        dt.Skip = DateTimeSkips.Year | DateTimeSkips.Month | DateTimeSkips.Day;
                        d.Start = dt.ToFormatString(CultureInfo.InvariantCulture);
                        list.Add(d);
                        sg.Schedules = list;
                        Logger.LogInformation("Registration: creating the default schedule group.");
                        await ScheduleGroupRepository.UpsertAsync(connection, groupTransaction, sg);

                        //Create default device template group and add user group to it.
                        GXDeviceTemplateGroup dtg = new(groupName) { Creator = u, CreationTime = now, Default = true };
                        dtg.UserGroups?.Add(ug);
                        Logger.LogInformation("Registration: creating the default device template group.");
                        await DeviceTemplateGroupRepository.UpsertAsync(connection, groupTransaction, dtg);

                        //Create default device group and add user group to it.
                        GXDeviceGroup dg = new(groupName) { Creator = u, CreationTime = now, Default = true };
                        dg.UserGroups?.Add(ug);
                        Logger.LogInformation("Registration: creating the default device group.");
                        await DeviceGroupRepository.UpsertAsync(connection, groupTransaction, dg);

                        //Create default agent group and add user group to it.
                        GXAgentGroup ag = new(groupName) { Creator = u, CreationTime = now, Default = true };
                        ag.UserGroups?.Add(ug);
                        Logger.LogInformation("Registration: creating the default agent group.");
                        await AgentGroupRepository.UpsertAsync(connection, groupTransaction, ag);

                        //Create default module group and add user group to it.
                        GXModuleGroup mg = new(groupName) { Creator = u, CreationTime = now, Default = true };
                        mg.UserGroups?.Add(ug);
                        Logger.LogInformation("Registration: creating the default module group.");
                        await ModuleGroupRepository.UpsertAsync(connection, groupTransaction, mg);

                        //Create default trigger group and add user group to it.
                        GXTriggerGroup tg = new(groupName) { Creator = u, CreationTime = now, Default = true };
                        tg.UserGroups?.Add(ug);
                        Logger.LogInformation("Registration: creating the default trigger group.");
                        await TriggerGroupRepository.UpsertAsync(connection, groupTransaction, tg);

                        //Create default workflow group and add user group to it.
                        GXWorkflowGroup wfg = new(groupName) { Creator = u, CreationTime = now, Default = true };
                        wfg.UserGroups?.Add(ug);
                        Logger.LogInformation("Registration: creating the default workflow group.");
                        await WorkflowGroupRepository.UpsertAsync(connection, groupTransaction, wfg);

                        //Create default block group and add user group to it.
                        GXBlockGroup bg = new(groupName) { Creator = u, CreationTime = now, Default = true };
                        bg.UserGroups?.Add(ug);
                        Logger.LogInformation("Registration: creating the default block group.");
                        await BlockGroupRepository.UpsertAsync(connection, groupTransaction, bg);

                        //Create default script group and add user group to it.
                        GXScriptGroup sgs = new(groupName) { Creator = u, CreationTime = now, Default = true };
                        sgs.UserGroups?.Add(ug);
                        Logger.LogInformation("Registration: creating the default script group.");
                        await ScriptGroupRepository.UpsertAsync(connection, groupTransaction, sgs);

                        //Create default gateway group and add user group to it.
                        GXGatewayGroup gg = new(groupName) { Creator = u, CreationTime = now };
                        gg.UserGroups?.Add(ug);
                        Logger.LogInformation("Registration: creating the default gateway group.");
                        await GatewayGroupRepository.UpsertAsync(connection, groupTransaction, gg);
                        //Create default key management group and add user group to it.
                        GXKeyManagementGroup kmg = new(groupName) { Creator = u, CreationTime = now, Default = true };
                        kmg.UserGroups?.Add(ug);
                        Logger.LogInformation("Registration: creating the default key management group.");
                        await KeyManagementGroupRepository.UpsertAsync(connection, groupTransaction, kmg);
                        await groupTransaction.CommitAsync();
                    }
                    //Run cron after the admin is added.
                    if (firstUser)
                    {
                        ServerSettings.AdminAdded = true;
                        using IServiceScope scope = _serviceProvider.CreateScope();
                        var context = _httpContextAccessor.HttpContext
                            ?? throw new InvalidOperationException("Registration requires an HTTP context.");
                        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
                        var administrator = await signInManager.CreateUserPrincipalAsync(user);
                        using var initialization = GXRegistrationContext.Begin(context, administrator);
                        IGXBackgroundService cronService = scope.ServiceProvider.GetRequiredService<IGXBackgroundService>();
                        Logger.LogInformation("Registration: initializing defaults for the first administrator.");
                        await cronService.InitAsync();
                        // Configured modules can now bind to the persisted administrator and default groups.
                        // Start mail delivery before Register sends the mandatory confirmation link.
                        await scope.ServiceProvider.GetRequiredService<IGXModuleService>().StartConfiguredModulesAsync();
                        // Refresh available agents, modules and manufacturers for the first administrator.
                        await cronService.StartAsync(GXBackgroundTaskIds.AgentUpdate, administrator);
                        await cronService.StartAsync(GXBackgroundTaskIds.ModuleUpdate, administrator);
                        await cronService.StartAsync(GXBackgroundTaskIds.ManufacturerUpdate, administrator);
                    }
                    await CreateNotificationsAsync(connection, user);
                    await _workflowHandler.ExecuteAsync(typeof(UserTrigger), UserTrigger.Create, user);
                }
                catch (Exception error)
                {
                    Logger.LogError(error, "Registration defaults could not be initialized.");
                    if (firstUser)
                    {
                        ServerSettings.AdminAdded = false;
                    }
                    try
                    {
                        connection.Delete(GXDeleteArgs.DeleteById<GXUser>(user.Id));
                    }
                    catch (Exception cleanupError)
                    {
                        Logger.LogWarning(cleanupError, "Registration user cleanup also failed; preserving the initialization error.");
                    }
                    throw;
                }
            }
            return ret;
        }

        private async System.Threading.Tasks.Task CreateNotificationsAsync(GXDbConnection connection, ApplicationUser user)
        {
            // Evaluate the persisted owner, never the temporary administrator used to provision groups.
            var claims = _serviceProvider.GetRequiredService<IUserClaimsPrincipalFactory<ApplicationUser>>();
            var owner = await claims.CreateAsync(user);
            var authorization = _serviceProvider.GetRequiredService<IAuthorizationService>();
            var allowed = await GXNotificationTypeAccess.AllowedIdsAsync(connection, authorization, owner);
            if (allowed.Length == 0)
            {
                return;
            }

            var creator = new GXUser { Id = user.Id };
            var query = GXSelectArgs.Select<GXNotification>(n => new { n.Id, n.Type }, n => n.Creator == creator);
            query.Columns.Add<GXNotificationType>(type => type.Id);
            query.Joins.AddInnerJoin<GXNotification, GXNotificationType>(n => n.Type!, type => type.Id);
            var existing = await connection.SelectAsync<GXNotification>(query);
            var existingTypes = existing.Where(n => n.Type != null).Select(n => n.Type!.Id).ToHashSet();
            var types = await connection.SelectAsync<GXNotificationType>(
                GXSelectArgs.SelectAll<GXNotificationType>(type => allowed.Contains(type.Id)));
            List<GXNotification> notifications = [];
            foreach (var type in types.Where(type => !existingTypes.Contains(type.Id)))
            {
                var notification = new GXNotification(type.Name)
                {
                    Creator = creator,
                    Type = type,
                    CreationTime = DateTimeOffset.UtcNow,
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                };
                if (string.Equals(type.Code, "ui", StringComparison.OrdinalIgnoreCase))
                {
                    var sources = await GXNotificationPermissions.ReadAsync(connection, user.Id);
                    notification.Sources = sources.Select(permission => permission.Name!).ToList();
                }
                notifications.Add(notification);
            }
            if (notifications.Count == 0)
            {
                return;
            }
            Logger.LogInformation("Registration: creating {Count} allowed notifications for the new user.", notifications.Count);
            await using var transaction = await connection.BeginTransactionAsync();
            await connection.InsertAsync(transaction, GXInsertArgs.InsertRange(notifications));
            foreach (var notification in notifications)
                {
                await GXNotificationSourceStorage.ReplaceAsync(connection, transaction, notification.Id, notification.Sources);
            }
            await transaction.CommitAsync();
        }

        public override Task<IdentityResult> UpdateAsync(ApplicationUser user)
        {
            user.Updated = DateTime.Now;
            return base.UpdateAsync(user);
        }
    }
}
