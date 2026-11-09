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

using Gurux.DLMS.AMI.Client.Pages.Config;
using Gurux.DLMS.AMI.Client.Pages.Manufacturer;
using Gurux.DLMS.AMI.Client.Pages.Module;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Enums;
using Gurux.Service.Orm.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Security.Claims;
using System.Text.Json;
using Gurux.DLMS.AMI.Client.Pages;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    /// <summary>
    /// Constructor.
    /// </summary>
    public class ConfigurationRepository(
        IDbConnectionFactory dbFactory,
        IServiceProvider serviceProvider,
        IOptions<IdentityOptions> identityOptions,
        NotificationCache notificationCache) : IConfigurationRepository
    {
        private readonly IDbConnectionFactory _dbFactory = dbFactory;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly IOptions<IdentityOptions> _identityOptions = identityOptions;
        private readonly NotificationCache _notificationCache = notificationCache;

        /// <summary>
        /// Configuration has been modified.
        /// </summary>
        private ConfigurationModifiedEventHandler? _modified;

        /// <summary>
        /// Configuration has been modified.
        /// </summary>
        public event ConfigurationModifiedEventHandler? Updated
        {
            add
            {
                _modified += value;
            }
            remove
            {
                _modified -= value;
            }
        }

        private async Task EnsurePolicyAsync(ClaimsPrincipal user, string policy)
        {
            // Authorization receives its user explicitly; preserve the caller's HttpContext.
            using var scope = _serviceProvider.CreateScope();
            var authorizationService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
            await authorizationService.EnsureAnyPolicyAsync(user, policy);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal user, IEnumerable<Guid> tasks, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXConfiguration>> ListAsync(
            ClaimsPrincipal user,
            ListConfiguration? request,
            ListConfigurationResponse? response,
            CancellationToken cancellationToken = default)
        {
            // Maintenance is read before authentication by the maintenance middleware
            // and its public status endpoint. All other configuration reads require access.
            bool publicMaintenance = request?.Filter?.Name == GXConfigurations.Maintenance;
            if (!publicMaintenance)
            {
                await EnsurePolicyAsync(user, GXConfigurationPolicies.View);
            }
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXConfiguration>();
            arg.Where.And<GXConfiguration>(configuration => configuration.Enabled);
            if (publicMaintenance)
            {
                // Name's normal UI filter uses Contains; public access must use equality.
                arg.Where.And<GXConfiguration>(configuration => configuration.Name == GXConfigurations.Maintenance);
            }
            if (request?.Exclude != null && request.Exclude.Any())
            {
                arg.Where.And<GXConfiguration>(w => !request.Exclude.Contains(w.Id));
            }
            if (request?.Included?.Any() == true)
            {
                arg.Where.And<GXConfiguration>(w => request.Included.Contains(w.Id));
            }
            if (request != null && request.Filter != null)
            {
                arg.Where.FilterBy(request.Filter);
            }
            arg.Distinct = true;
            if (string.IsNullOrEmpty(request?.OrderBy))
            {
                arg.Columns.Add<GXMenuLink>(c => c.Order);
                arg.Joins.AddInnerJoin<GXMenuLink, GXConfiguration>(j => j.Configuration!, j => j.Id);
                arg.Joins.AddInnerJoin<GXMenuLink, GXMenu>(j => j.Menu!, j => j.Id);
                arg.Where.And<GXMenuLink>(l => l.Removed == null);
                arg.Where.And<GXMenu>(m => m.Kind == MenuKind.Configuration && m.Removed == null);
            }
            GXDateTimeFilters.Apply<GXConfiguration>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetConnectionAsync();
            if (request != null && request.Count != 0 && response != null)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXConfiguration>(q => GXSql.Count(1));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                response.Count = connection.SingleOrDefault<long>(total);
                if (response.Count == 0)
                {
                    //No need to retrieve data when none is available.
                    return [];
                }
                arg.Index = request.Index;
                arg.Count = request.Count;
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXConfiguration>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXMenuLink>(q => q.Order);
            }
            GXConfiguration[] configurations = [.. (await connection.SelectAsync<GXConfiguration>(arg))];
            foreach (var configuration in configurations)
            {
                RefreshStatusSettings(configuration);
            }
            if (response != null)
            {
                response.Configurations = configurations;
                if (response.Count == 0)
                {
                    response.Count = configurations.Length;
                }
            }
            return configurations;
        }

        /// <inheritdoc />
        public async Task<GXConfiguration> ReadAsync(
            ClaimsPrincipal user,
            Guid id,
            string? culture,
            CancellationToken cancellationToken = default)
        {
            await EnsurePolicyAsync(user, GXConfigurationPolicies.View);
            try
            {
                GXSelectArgs arg = GXSelectArgs.SelectAll<GXConfiguration>(where => where.Id == id);
                await using var connection = await _dbFactory.GetConnectionAsync();
                GXConfiguration? conf = await connection.SingleOrDefaultAsync<GXConfiguration>(arg) ?? throw new GXAmiForbiddenException(id.ToString());
                RefreshStatusSettings(conf);
                return conf;

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
                        return await ReadAsync(user, id, culture, linkedCts.Token);
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
        public async Task UpsertAsync(
            ClaimsPrincipal user,
            IEnumerable<GXConfiguration> configurations,
            bool notification,
            CancellationToken cancellationToken = default)
        {
            await EnsurePolicyAsync(user, GXConfigurationPolicies.Edit);
            try
            {
                configurations = [.. configurations];
                await using var connection = await _dbFactory.GetConnectionAsync(cancellationToken);
                var identifiers = configurations.Select(c => c.Id).ToArray();
                // Use persisted names: callers can omit or spoof the Name in an update payload.
                var restrictedConfigurations = await connection.SelectAsync<GXConfiguration>(
                    GXSelectArgs.Select<GXConfiguration>(c => new { c.Id, c.Name }, c => identifiers.Contains(c.Id)), cancellationToken);
                ConfigurationSettingsAccess.EnsureCanEdit(user, restrictedConfigurations.Select(c => c.Name));
                var notificationConfigurations = restrictedConfigurations.Where(c => GXServerNotificationConfiguration.IsConfiguration(c.Name)).ToArray();
                var notificationIds = notificationConfigurations.Select(c => c.Id).ToHashSet();
                var proxyIds = restrictedConfigurations.Where(c => c.Name == GXConfigurations.HttpProxy).Select(c => c.Id).ToHashSet();
                var maintenanceIds = restrictedConfigurations.Where(c => c.Name == GXConfigurations.Maintenance).Select(c => c.Id).ToHashSet();
                GXUpdateArgs update = GXUpdateArgs.UpdateRange(configurations);
                foreach (var configuration in configurations)
                {
                    if (proxyIds.Contains(configuration.Id))
                    {
                        if (string.IsNullOrWhiteSpace(configuration.Settings))
                        {
                            throw new ArgumentException("HTTP proxy settings are required.");
                        }
                        var proxySettings = JsonSerializer.Deserialize<HttpProxySettings>(configuration.Settings)
                            ?? throw new ArgumentException("HTTP proxy settings are required.");
                        proxySettings.Validate();
                    }
                    if (notificationIds.Contains(configuration.Id) && !string.IsNullOrWhiteSpace(configuration.Settings))
                    {
                        JsonSerializer.Deserialize<PerformanceSettings>(configuration.Settings);
                    }
                    if (maintenanceIds.Contains(configuration.Id) && !string.IsNullOrEmpty(configuration.Settings))
                    {
                        var maintenance = JsonSerializer.Deserialize<MaintenanceSettings>(configuration.Settings);
                        maintenance?.Validate();
                    }
                }
                update.Exclude<GXConfiguration>(q => new { q.Name, q.CreationTime });
                await connection.UpdateAsync(update, cancellationToken);
                if (notificationConfigurations.Length != 0)
                {
                    await GXServerNotificationConfiguration.ReloadAsync(connection, _notificationCache, cancellationToken);
                }

                foreach (var conf in configurations)
                {
                    //Update security settings.
                    if (conf.Name == GXConfigurations.Security && conf.Settings != null)
                    {
                        SecuritySettings? o = JsonSerializer.Deserialize<SecuritySettings>(conf.Settings);
                        if (o != null)
                        {
                            UpdateSecuritySettings(_identityOptions.Value, o);
                        }
                        continue;
                    }
                }

                if (notification)
                {
                    if (notification && _modified != null)
                    {
                        _modified(configurations);
                    }
                    List<string> users;
                    using (IServiceScope scope = _serviceProvider.CreateScope())
                    {
                        IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                        users = await userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]);
                    }
                    foreach (var it in configurations)
                    {
                        //Only IDs and names are notified for security reasons.
                        _notificationCache.Add(users, new GXTarget(GXConfigurationPolicies.Edit, it.Id, it.Name));
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
                        await UpsertAsync(user, configurations, notification, linkedCts.Token);
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
        public async Task<IEnumerable<string>?> GetTablesWithSchemaChangesAsync(CancellationToken cancellationToken = default)
            => [.. (await GetTableChangesAsync(cancellationToken)).Select(change => change.TableName)];

        /// <inheritdoc />
        public async Task<IEnumerable<GXConfigurationTableChange>> GetTableChangesAsync(CancellationToken cancellationToken = default)
        {
            await using var connection = await _dbFactory.GetConnectionAsync(cancellationToken);
            using IDbTransaction transaction = connection.BeginTransaction();
            GXSchemaManager manager = new(connection);
            List<GXConfigurationTableChange> changed = [];
            try
            {
                //Check that all tables exists.
                TypeHelper.GetTypesFromNamespace(typeof(GXUser).Assembly, "Gurux.DLMS.AMI.Shared.DTOs").ToList().ForEach(type =>
                {
                    bool isUnique = type.GetInterfaces().Any(it =>
                        it.IsGenericType &&
                        it.GetGenericTypeDefinition() == typeof(IUnique<>));
                    if (isUnique || type == typeof(Gurux.DLMS.AMI.Shared.DTOs.View.GXViewPolicy) ||
                        type == typeof(Gurux.DLMS.AMI.Shared.DTOs.View.GXViewColumnPolicy))
                    {
                        if (!manager.TableExist(transaction, type))
                        {
                            changed.Add(new GXConfigurationTableChange { TableName = type.Name, Changes = ["Table is missing"] });
                        }
                        else
                        {
                            var pending = manager.GetTableChanges(transaction, type);
                            if (pending.Length > 0)
                            {
                                changed.Add(new GXConfigurationTableChange { TableName = type.Name, Changes = pending });
                            }
                        }
                    }
                });
                transaction.Commit();
            }
            catch (Exception)
            {
                transaction?.Rollback();
                throw;
            }
            return changed;
        }

        /// <summary>
        /// Creates configuration navigation links with their initial setting values.
        /// </summary>
        internal static List<GXMenuLink> CreateDefaultConfigurationSettings()
        {
            //All values are saved using invariant culture.
            List<GXMenuLink> configurations = [];
            AddSystemConfiguration(configurations);
            AddStatusConfiguration(configurations);
            AddSecurityConfiguration(configurations);
            AddLanguagesConfiguration(configurations);
            AddModulesConfiguration(configurations);
            AddAgentConfiguration(configurations);
            AddComponentViewsConfiguration(configurations);
            AddBlocksConfiguration(configurations);
            AddMenuConfiguration(configurations);
            AddTabConfiguration(configurations);
            AddLogConfiguration(configurations);
            AddTriggersConfiguration(configurations);
            AddWorkflowConfiguration(configurations);
            AddScriptConfiguration(configurations);
            AddCronConfiguration(configurations);
            AddManufacturerConfiguration(configurations);
            AddMaintenanceConfiguration(configurations);
            AddAccessControlConfiguration(configurations);
            AddKeyManagementConfiguration(configurations);
            AddNotificationConfiguration(configurations);
            AddPruneConfiguration(configurations);
            AddAppearanceConfiguration(configurations);
            AddAccountConfiguration(configurations);
            AddViewConfiguration(configurations);
            AddHttpProxyConfiguration(configurations);
            AddHistoryConfiguration(configurations);
            return configurations;
        }

        internal static void AddHttpProxyConfiguration(List<GXMenuLink> configurations)
        {
            configurations.Add(new GXMenuLink
            {
                Name = "HTTP proxy",
                Icon = "http-proxy",
                Description = "Allowed HTTP servers, caching and request limits.",
                Url = "config/http-proxy",
                Order = 30,
                TargetType = typeof(HttpProxy).FullName,
                Configuration = new GXConfiguration { Name = GXConfigurations.HttpProxy, Settings = JsonSerializer.Serialize(new HttpProxySettings()) }
            });
        }

        internal static void AddHistoryConfiguration(List<GXMenuLink> configurations)
        {
            configurations.Add(new GXMenuLink
            {
                Name = "History",
                Icon = GXConfigurations.History,
                Description = "History configuration.",
                Url = "config/history",
                Order = 30,
                TargetType = typeof(ChangeHistory).FullName,
                Configuration = new GXConfiguration { Name = GXConfigurations.History, Settings = JsonSerializer.Serialize(new HistorySettings()) }
            });
        }


        /// <summary>Adds proxy settings to existing databases without replacing edited configurations.</summary>
        internal static async Task<GXConfiguration> EnsureHttpProxyConfigurationAsync(GXDbConnection connection)
        {
            var configuration = await connection.SingleOrDefaultAsync<GXConfiguration>(
                GXSelectArgs.SelectAll<GXConfiguration>(c => c.Name == GXConfigurations.HttpProxy));
            if (configuration is not null)
            {
                return configuration;
            }
            var defaults = new List<GXMenuLink>();
            AddHttpProxyConfiguration(defaults);
            configuration = defaults[0].Configuration!;
            await connection.InsertAsync(GXInsertArgs.Insert(configuration));
            return configuration;
        }

        internal static void AddSystemConfiguration(List<GXMenuLink> configurations)
        {
            //Add System configuration.
            GXMenuLink conf = new()
            {
                Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.System)),
                Icon = GXConfigurations.System,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.SystemConfigurationDescription)),
                Url = "config/system",
                Order = 1,
                TargetType = typeof(SystemConfig).FullName,
                Configuration = new GXConfiguration
                {
                    Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.System)),
                    Settings = JsonSerializer.Serialize(new SystemSettings())
                }
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add Status configuration.
        /// </summary>
        /// <param name="configurations"></param>
        internal static void AddStatusConfiguration(List<GXMenuLink> configurations)
        {
            GXMenuLink conf = new()
            {
                Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Status)),
                Icon = GXConfigurations.Status,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.SystemStatusDescription)),
                Url = "config/status",
                Order = 2,
                TargetType = typeof(Status).FullName,
                Configuration = new GXConfiguration
                {
                    Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Status)),
                    Settings = CreateRuntimeStatusSettings()
                }
            };
            configurations.Add(conf);
        }

        // Runtime status must never come from a persisted startup timestamp.
        internal static void RefreshStatusSettings(GXConfiguration configuration)
        {
            if (string.Equals(configuration.Name, GXConfigurations.Status, StringComparison.OrdinalIgnoreCase))
            {
                configuration.Settings = CreateRuntimeStatusSettings();
            }
        }

        private static string CreateRuntimeStatusSettings()
        {
            using var process = Process.GetCurrentProcess();
            var info = FileVersionInfo.GetVersionInfo(typeof(ConfigurationRepository).Assembly.Location);
            return JsonSerializer.Serialize(new StatusSettings
            {
                SiteVersion = info.FileVersion,
                StartTime = new DateTimeOffset(process.StartTime.ToUniversalTime())
            });
        }

        /// <summary>Applies configured identity and password security settings.</summary>
        public static void UpdateSecuritySettings(IdentityOptions options, SecuritySettings o)
        {
            options.ClaimsIdentity.UserIdClaimType = ClaimTypes.NameIdentifier;
            options.Password.RequiredLength = o.RequiredLength;
            options.Password.RequiredUniqueChars = o.RequiredUniqueChars;
            options.Password.RequireNonAlphanumeric = o.RequireNonAlphanumeric;
            options.Password.RequireDigit = o.RequireDigit;
            options.Password.RequireUppercase = o.RequireUppercase;
            options.Password.RequireLowercase = o.RequireLowercase;
            options.User.AllowedUserNameCharacters = o.AllowedUserNameCharacters;
            options.User.RequireUniqueEmail = o.RequireUniqueEmail;
            options.SignIn.RequireConfirmedPhoneNumber = o.RequireConfirmedPhoneNumber;
            options.SignIn.RequireConfirmedAccount = o.RequireConfirmedAccount;
            options.SignIn.RequireConfirmedEmail = o.RequireConfirmedEmail;
            options.Lockout.AllowedForNewUsers = o.AllowedForNewUsers;
            options.Lockout.DefaultLockoutTimeSpan = o.DefaultLockoutTimeSpan;
            options.Lockout.MaxFailedAccessAttempts = o.MaxFailedAccessAttempts;
        }

        /// <summary>
        /// Add Security configuration.
        /// </summary>
        internal static void AddSecurityConfiguration(List<GXMenuLink> configurations)
        {
            GXMenuLink conf = new()
            {
                Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Security)),
                Icon = GXConfigurations.Security,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.SystemSecurityDescription)),
                Url = "config/security",
                Order = 3,
                TargetType = typeof(Security).FullName,
                Configuration = new GXConfiguration
                {
                    Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Security)),
                    Settings = JsonSerializer.Serialize(new IdentityOptions())
                }
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add cultures configuration.
        /// </summary>
        internal static void AddLanguagesConfiguration(List<GXMenuLink> configurations)
        {
            List<LanguageSettings> list = [];

            _ = new ResourceManager(typeof(Properties.Resources));
            foreach (string file in
                Directory.EnumerateFiles(Path.GetDirectoryName(typeof(Properties.Resources).Assembly.Location) ?? throw new InvalidOperationException("The resources assembly has no directory."),
                "Gurux.DLMS.AMI.Server.resources.dll", SearchOption.AllDirectories))
            {
                try
                {
                    Assembly assembly = Assembly.LoadFrom(file);
                    string name = Path.GetDirectoryName(file) ?? throw new InvalidOperationException("The resource file has no directory.");
                    name = name[^2..];
                    CultureInfo ci = new(name);
                    LanguageSettings it = new()
                    {
                        Id = ci.Name,
                        EnglishName = ci.EnglishName,
                        NativeName = ci.NativeName,
                        Updated = DateTime.Now
                    };
                    list.Add(it);
                }
                catch (Exception)
                {

                }
            }
            GXMenuLink conf = new()
            {
                Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Language)),
                Icon = GXConfigurations.Language,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.LanguageDescription)),
                Url = "config/languages",
                Order = 4,
                TargetType = typeof(ConfigurationLanguages).FullName,
                Configuration = new GXConfiguration
                {
                    Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Language)),
                    Settings = JsonSerializer.Serialize(list.ToArray())
                }
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add modules configuration.
        /// </summary>
        internal static void AddModulesConfiguration(List<GXMenuLink> configurations)
        {
            GXMenuLink conf = new()
            {
                Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Modules)),
                Icon = "modules",
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.ModuleDescription)),
                Url = "config/modules",
                Order = 4,
                TargetType = typeof(ModuleManage).FullName,
                Configuration = new GXConfiguration
                {
                    Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Modules)),
                    Settings = JsonSerializer.Serialize(new ModuleSettings())
                }
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add installable agent configuration.
        /// </summary>
        internal static void AddAgentConfiguration(List<GXMenuLink> configurations)
        {
            GXMenuLink conf = new()
            {
                Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Agents)),
                Icon = "agents",
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.AgentDescription)),
                Url = "config/agents",
                Order = 4,
                TargetType = typeof(AgentInstallers).FullName,
                Configuration = new GXConfiguration
                {
                    Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Agent)),
                    Settings = JsonSerializer.Serialize(new AgentSettings())
                }
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add component views configuration.
        /// </summary>
        internal static void AddComponentViewsConfiguration(List<GXMenuLink> configurations)
        {
            GXMenuLink conf = new()
            {
                Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Components)),
                Icon = GXConfigurations.Components,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.ComponentViewDescription)),
                TargetType = null,
                Url = "config/components",
                Order = 5,
                Configuration = new GXConfiguration
                {
                    Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Components)),
                }
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add blocks configuration.
        /// </summary>
        internal static void AddBlocksConfiguration(List<GXMenuLink> configurations)
        {
            GXMenuLink conf = new()
            {
                Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Blocks)),
                Icon = GXConfigurations.Block,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.BlockDescription)),
                Url = "config/blocks",
                TargetType = null,
                Order = 6,
                Configuration = new GXConfiguration
                {
                    Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Blocks)),
                }
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add menu configuration.
        /// </summary>
        internal static void AddMenuConfiguration(List<GXMenuLink> configurations)
        {
            GXMenuLink conf = new()
            {
                Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Menus)),
                Icon = GXConfigurations.Menu,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.MenuDescription)),
                Url = "config/menus",
                TargetType = null,
                Order = 6,
                Configuration = new GXConfiguration
                {
                    Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Menus)),
                }
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add tab configuration.
        /// </summary>
        internal static void AddTabConfiguration(List<GXMenuLink> configurations)
        {
            GXMenuLink conf = new()
            {
                Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Tabs)),
                Icon = GXConfigurations.Tab,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.TabDescription)),
                Url = "config/tabs",
                TargetType = null,
                Order = 6,
                Configuration = new GXConfiguration
                {
                    Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Tabs)),
                }
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add Log configuration.
        /// </summary>
        internal static void AddLogConfiguration(List<GXMenuLink> configurations)
        {
            GXMenuLink conf = new()
            {
                Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Logs)),
                Icon = GXConfigurations.Log,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.LogDescription)),
                Url = "config/logs",
                TargetType = typeof(Gurux.DLMS.AMI.Client.Pages.Config.LogTypes).FullName,
                Order = 6,
                Configuration = new GXConfiguration
                {
                    Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Logs)),
                }
            };
            configurations.Add(conf);
        }


        /// <summary>
        /// Add view configuration.
        /// </summary>
        internal static void AddViewConfiguration(List<GXMenuLink> configurations)
        {
            GXMenuLink conf = new()
            {
                Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Views)),
                Icon = GXConfigurations.View,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.ViewDescription)),
                Url = "config/views",
                TargetType = typeof(Gurux.DLMS.AMI.Client.Pages.View.ViewManage).FullName,
                Order = 6,
                Configuration = new GXConfiguration
                {
                    Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Views)),
                }
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add triggers configuration.
        /// </summary>
        internal static void AddTriggersConfiguration(List<GXMenuLink> configurations)
        {
            GXMenuLink conf = new()
            {
                Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Triggers)),
                Icon = GXConfigurations.Trigger,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.TriggerDescription)),
                Url = "config/triggers",
                Order = 6,
                TargetType = null,
                Configuration = new GXConfiguration
                {
                    Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Trigger)),
                }
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add workflow configuration.
        /// </summary>
        internal static void AddWorkflowConfiguration(List<GXMenuLink> configurations)
        {
            GXMenuLink conf = new()
            {
                Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Workflows)),
                Icon = GXConfigurations.Workflow,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.WorkflowDescription)),
                Url = "config/workflows",
                Order = 7,
                TargetType = null,
                Configuration = new GXConfiguration
                {
                    Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Workflow)),
                }
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add notification configuration.
        /// </summary>
        internal static void AddNotificationConfiguration(List<GXMenuLink> configurations)
        {
            GXMenuLink conf = new()
            {
                Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Notification)),
                Icon = GXConfigurations.Notification,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.NotificationDescription)),
                Url = "Notification",
                Order = 10,
                TargetType = null,
                Configuration = new GXConfiguration
                {
                    Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Notification)),
                    Settings = JsonSerializer.Serialize(new PerformanceSettings())
                }
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add prune configuration.
        /// </summary>
        internal static void AddPruneConfiguration(List<GXMenuLink> configurations)
        {
            GXMenuLink conf = new()
            {
                Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Prune)),
                Icon = GXConfigurations.Prune,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.PruneDescription)),
                Url = "config/prune",
                Order = 11,
                TargetType = null,
                Configuration = new GXConfiguration
                {
                    Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Prune)),
                    Settings = JsonSerializer.Serialize(new CronSettings())
                }
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add appearance configuration.
        /// </summary>
        internal static void AddAppearanceConfiguration(List<GXMenuLink> configurations)
        {
            var settings = new AppearanceSettings();
            GXMenuLink conf = new()
            {
                Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Appearance)),
                Icon = GXConfigurations.Appearance,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.AppearanceDescription)),
                Url = "config/appearance",
                Order = 13,
                TargetType = null,
                Configuration = new GXConfiguration
                {
                    Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Appearance)),
                    Settings = JsonSerializer.Serialize(settings)
                }
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add account configuration.
        /// </summary>
        internal static void AddAccountConfiguration(List<GXMenuLink> configurations)
        {
            var settings = new AccountSettings();
            GXMenuLink conf = new()
            {
                Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Account)),
                Icon = GXConfigurations.Account,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.AccountDescription)),
                Url = "config/account",
                Order = 16,
                TargetType = typeof(Account).FullName,
                Configuration = new GXConfiguration
                {
                    Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Account)),
                    Settings = JsonSerializer.Serialize(settings)
                }
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add manufacturer configuration.
        /// </summary>
        internal static void AddManufacturerConfiguration(List<GXMenuLink> configurations)
        {
            GXMenuLink conf = new()
            {
                Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Manufacturers)),
                Icon = "manufacturer",
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.ManufacturerDescription)),
                Url = "config/manufacturers",
                Order = 8,
                TargetType = typeof(ManufacturerManage).FullName,
                Configuration = new GXConfiguration
                {
                    Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Manufacturer)),
                    Settings = JsonSerializer.Serialize(new Client.Shared.ManufacturerSettings())
                }
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add cron configuration.
        /// </summary>
        internal static void AddCronConfiguration(List<GXMenuLink> configurations)
        {
            GXMenuLink conf = new()
            {
                Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Cron)),
                Icon = GXConfigurations.Cron,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.CronDescription)),
                Url = "config/cron",
                Order = 8,
                TargetType = null,
                Configuration = new GXConfiguration
                {
                    Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Cron)),
                    Settings = JsonSerializer.Serialize(new CronSettings())
                }
            };
            configurations.Add(conf);
        }


        /// <summary>
        /// Add key management configuration.
        /// </summary>
        internal static void AddKeyManagementConfiguration(List<GXMenuLink> configurations)
        {
            GXMenuLink conf = new()
            {
                Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.KeyManagement)),
                Icon = GXConfigurations.KeyManagement,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.KeyManagementsDescription)),
                Url = "config/key-management",
                Order = 8,
                TargetType = null,
                Configuration = new GXConfiguration
                {
                    Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.KeyManagement)),
                }
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add access control configuration.
        /// </summary>
        internal static void AddAccessControlConfiguration(List<GXMenuLink> configurations)
        {
            GXMenuLink conf = new()
            {
                Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.AccessControl)),
                Icon = GXConfigurations.AccessControl,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.AccessControlDescription)),
                Url = "config/access-control",
                Order = 9,
                Configuration = new GXConfiguration
                {
                    Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.AccessControl)),
                }
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add maintenance configuration.
        /// </summary>
        internal static void AddMaintenanceConfiguration(List<GXMenuLink> configurations)
        {
            GXMenuLink conf = new()
            {
                Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Maintenance)),
                Icon = GXConfigurations.Maintenance,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.MaintenanceDescription)),
                Url = "config/maintenance",
                Order = 11,
                Configuration = new GXConfiguration
                {
                    Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Maintenance)),
                }
            };
            configurations.Add(conf);
        }


        /// <summary>
        /// Add script configuration.
        /// </summary>
        internal static void AddScriptConfiguration(List<GXMenuLink> configurations)
        {
            Assembly asm = typeof(GXAmiException).Assembly;
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(asm.Location);
            GXMenuLink conf = new()
            {
                Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Scripts)),
                Icon = GXConfigurations.Scripts,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.ScriptDescription)),
                Url = "config/scripts",
                Order = 7,
                Configuration = new GXConfiguration
                {
                    Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Scripts)),
                    Settings = JsonSerializer.Serialize(new ScriptSettings()
                    {
                        Versions = info.FileVersion
                    })
                }
            };
            configurations.Add(conf);
        }

        /// <inheritdoc />
        public Task EnsureDefaultsAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
            => ApplyDefaultsAsync(user, [], false, cancellationToken);

        /// <inheritdoc />
        public Task ResetToDefaultsAsync(ClaimsPrincipal user, IEnumerable<string> configurations, CancellationToken cancellationToken = default)
        {
            var keys = configurations?.Distinct().ToArray() ?? [];
            if (keys.Length == 0)
            {
                throw new ArgumentException("Select at least one configuration to reset.", nameof(configurations));
            }

            return ApplyDefaultsAsync(user, keys, true, cancellationToken);
        }

        private async Task ApplyDefaultsAsync(ClaimsPrincipal user, string[] keys, bool reset, CancellationToken cancellationToken)
        {
            await EnsurePolicyAsync(user, GXConfigurationPolicies.Edit);
            await using var connection = await _dbFactory.GetConnectionAsync(cancellationToken);
            var changed = await ApplyDefaultConfigurationsAsync(connection, keys, reset, cancellationToken);
            if (changed.Any(c => GXServerNotificationConfiguration.IsConfiguration(c.Name)))
            {
                await GXServerNotificationConfiguration.ReloadAsync(connection, _notificationCache, cancellationToken);
            }
            _modified?.Invoke(changed);
            using var scope = _serviceProvider.CreateScope();
            var users = await scope.ServiceProvider.GetRequiredService<IUserRepository>().GetUserIdsInRoleAsync([GXRoles.Admin]);
            foreach (var item in changed)
            {
                _notificationCache.Add(users, new GXTarget(GXConfigurationPolicies.Edit, item.Id, item.Name));
            }
        }

        /// <summary>Inserts missing settings or resets selected values while preserving their IDs.</summary>
        internal static async Task<GXConfiguration[]> ApplyDefaultConfigurationsAsync(GXDbConnection connection,
            string[] keys, bool reset, CancellationToken cancellationToken = default)
        {
            if (reset && keys.Length == 0)
            {
                throw new ArgumentException("Select at least one configuration to reset.", nameof(keys));
            }

            var defaults = CreateDefaultConfigurationSettings().Select(l => l.Configuration!).ToArray();
            var existing = await connection.SelectAsync<GXConfiguration>(GXSelectArgs.SelectAll<GXConfiguration>(), cancellationToken);
            var selected = existing.Where(c => keys.Contains(c.Name!) || keys.Contains(c.Id.ToString())).ToArray();
            if (keys.Length != 0 && (keys.Any(key => !selected.Any(c => c.Name == key || c.Id.ToString() == key)) || selected.Any(c => !defaults.Any(d => d.Name == c.Name))))
            {
                throw new ArgumentException("Every selected configuration must exist and have a built-in default.");
            }
            using var transaction = connection.BeginTransaction();
            if (!reset)
            {
                var missing = defaults.Where(d => !existing.Any(c => c.Name == d.Name)).ToArray();
                if (missing.Length != 0)
                {
                    await connection.InsertAsync(transaction, GXInsertArgs.InsertRange(missing), cancellationToken);
                }
                transaction.Commit();
                return missing;
            }
            foreach (var item in selected)
            {
                item.Settings = defaults.Single(d => d.Name == item.Name).Settings;
                item.Enabled = true;
                item.Updated = DateTimeOffset.UtcNow;
                item.ConcurrencyStamp = Guid.NewGuid().ToString();
                await connection.UpdateAsync(transaction, GXUpdateArgs.Update(item,
                    c => new { c.Settings, c.Enabled, c.Updated, c.ConcurrencyStamp }), cancellationToken);
            }
            transaction.Commit();
            return selected;
        }
    }
}
