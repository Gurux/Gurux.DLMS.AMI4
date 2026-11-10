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
using Gurux.DLMS.AMI.Client.Shared.KeyManagement;
using Gurux.DLMS.AMI.Data;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Server.Controllers;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Repository;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Server.Services.KeyManagement;
using Gurux.DLMS.AMI.Server.Triggers;

using Gurux.DLMS.AMI.Services.Database;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.Update;
using Gurux.DLMS.AMI.Shared.DTOs.UrlAlias;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.View;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.DB;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using Gurux.Service.Orm.Model;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using static Gurux.DLMS.AMI.Server.Internal.ServerHelpers;

namespace Gurux.DLMS.AMI.Server
{
    internal static partial class ServerSettings
    {
        /// <summary>
        /// Is 1st user added.
        /// </summary>
        public static bool AdminAdded = false;
        /// <summary>
        /// Server address.
        /// </summary>
        internal static string ServerAddress = default!;

        private static void UpdateScriptVersions(GXDbConnection connection,
            GXConfiguration conf, ScriptSettings settings, Type[] references)
        {
            StringBuilder sb = new();
            foreach (var item in references)
            {
                Assembly asm = typeof(GXAmiException).Assembly;
                sb.Append(item.Name);
                sb.Append(':');
                sb.Append(FileVersionInfo.GetVersionInfo(asm.Location).FileVersion);
                sb.Append(';');
            }
            settings.Versions = sb.ToString();
            conf.Settings = JsonSerializer.Serialize(settings);
            var u = GXUpdateArgs.Update(conf, c => new { c.Updated, c.Settings });
            connection.Update(u);
        }

        /// <summary>
        /// Update script shared version.
        /// </summary>
        /// <param name="connection">Connnection.</param>
        internal static void UpdateScriptCommonSharedVersion(GXDbConnection connection)
        {
            var args = GXSelectArgs.Select<GXConfiguration>(
                s => new { s.Id, s.Settings },
                w => w.Name == GXConfigurations.Scripts);
            try
            {
                GXConfiguration? conf = connection.SingleOrDefault<GXConfiguration>(args);
                ScriptSettings? settings = null;
                if (!string.IsNullOrEmpty(conf?.Settings))
                {
                    settings = JsonSerializer.Deserialize<ScriptSettings>(conf.Settings);
                }
                settings ??= new ScriptSettings();
                Type[] references = [
                    typeof(GXAmiException),
                    typeof(GXDateTime)
                ];
                string[]? list = settings.CurrentVersions?.Split(';');
                bool change = list == null;
                if (list != null)
                {
                    foreach (var item in references)
                    {
                        Assembly asm = typeof(GXAmiException).Assembly;
                        if (!list.Contains(item.Name + ":" + FileVersionInfo.GetVersionInfo(asm.Location)))
                        {
                            change = true;
                            break;
                        }
                    }
                }
                if (change && conf != null)
                {
                    UpdateScriptVersions(connection, conf, settings, references);
                }
            }
            catch (Exception)
            {
            }
        }



        internal static async Task UpdateLanguagesAsync(GXDbConnection connection)
        {
            var languages = connection.SelectAll<GXLanguage>();
            CultureInfo invariantCulture = new("en");
            GXLanguage language;
            if (!languages.Where(w => w.Id == invariantCulture.TwoLetterISOLanguageName).Any())
            {
                language = new GXLanguage()
                {
                    Id = invariantCulture.Name,
                    EnglishName = invariantCulture.EnglishName,
                    NativeName = invariantCulture.NativeName,
                    Enabled = true,
                    Default = true
                };
                languages.Add(language);
                await connection.InsertAsync(GXInsertArgs.Insert(language));
            }

        }

        /// <summary>
        /// Save new component views to the database.
        /// </summary>
        /// <param name="connection">Database connection.</param>
        /// <param name="transaction">Database transaction.</param>
        internal static void UpdateComponentViews(GXDbConnection connection, IDbTransaction transaction)
        {
            ServerHelpers.UpdateComponentViews(connection, transaction, typeof(Client.Pages.Admin.ActiveDeviceErrors).Assembly);
            _ = GXTriggerDefaults.EnsureAsync(connection, transaction, typeof(ServiceTrigger).Assembly).GetAwaiter().GetResult();
        }

        static internal void AddRepositories(IServiceCollection services)
        {
            //Add workflow service
            services.AddSingleton<GXWorkflowService>();
            services.AddSingleton<IWorkflowHandler>(provider => provider.GetRequiredService<GXWorkflowService>());
            services.AddHostedService(provider => provider.GetRequiredService<GXWorkflowService>());
            services.AddHostedService<GXHistoryRuntimeEffectWorker>();
            services.AddTransient<IPatRepository, PatRepository>();
            services.AddTransient<ILocalizationRepository, LocalizationRepository>();
            services.AddTransient<IScheduleRepository, ScheduleRepository>();
            services.AddTransient<ILogRepository, LogRepository>();
            services.AddTransient<ILogTypeRepository, LogTypeRepository>();
            services.AddTransient<IUserRepository, UserRepository>();
            services.AddTransient<IUserGroupRepository, UserGroupRepository>();
            services.AddTransient<IDeviceRepository, DeviceRepository>();
            services.AddTransient<IObjectRepository, ObjectRepository>();
            services.AddTransient<IObjectTemplateRepository, ObjectTemplateRepository>();
            services.AddTransient<IAttributeRepository, AttributeRepository>();
            services.AddTransient<IAttributeTemplateRepository, AttributeTemplateRepository>();
            services.AddTransient<IValueRepository, ValueRepository>();
            services.AddTransient<ProfileHistoryRepository>();
            services.AddTransient<IRawNotificationRepository, RawNotificationRepository>();
            services.AddTransient<IDeviceGroupRepository, DeviceGroupRepository>();
            services.AddTransient<IScheduleGroupRepository, ScheduleGroupRepository>();
            services.AddTransient<IAgentGroupRepository, AgentGroupRepository>();
            services.AddTransient<IAgentRepository, AgentRepository>();
            services.AddTransient<IModuleGroupRepository, ModuleGroupRepository>();
            services.AddTransient<ITriggerGroupRepository, TriggerGroupRepository>();
            services.AddTransient<ITriggerRepository, TriggerRepository>();
            services.AddTransient<IWorkflowGroupRepository, WorkflowGroupRepository>();
            services.AddTransient<IWorkflowRepository, WorkflowRepository>();
            services.AddTransient<IWorkflowOperationRepository, WorkflowOperationRepository>();
            services.AddTransient<IDeviceTemplateRepository, DeviceTemplateRepository>();
            services.AddTransient<IDeviceTemplateGroupRepository, DeviceTemplateGroupRepository>();
            services.AddScoped<IGXPolicyService, GXPolicyService>();
            services.AddTransient<IModuleRepository, ModuleRepository>();
            services.AddTransient<ITaskRepository, TaskRepository>();
            services.AddTransient<IBlockRepository, BlockRepository>();
            services.AddTransient<IBlockGroupRepository, BlockGroupRepository>();
            services.AddTransient<IMenuRepository, MenuRepository>();
            services.AddTransient<IViewRepository, ViewRepository>();
            services.AddTransient<IViewGroupRepository, ViewGroupRepository>();
            services.AddTransient<IMenuGroupRepository, MenuGroupRepository>();
            services.AddSingleton<IConfigurationRepository, ConfigurationRepository>();
            services.AddTransient<IComponentViewRepository, ComponentViewRepository>();
            services.AddTransient<IComponentViewGroupRepository, ComponentViewGroupRepository>();
            services.AddTransient<IScriptRepository, ScriptRepository>();
            services.AddTransient<IScriptGroupRepository, ScriptGroupRepository>();
            services.AddTransient<IDeviceTraceRepository, DeviceTraceRepository>();
            services.AddTransient<IRoleRepository, RoleRepository>();
            services.AddTransient<IUserPropertiesRepository, UserPropertiesRepository>();
            services.AddTransient<IAgentPropertiesRepository, AgentPropertiesRepository>();
            services.AddTransient<IManufacturerRepository, ManufacturerRepository>();
            services.AddTransient<IManufacturerGroupRepository, ManufacturerGroupRepository>();
            services.AddTransient<IFavoriteRepository, FavoriteRepository>();
            services.AddTransient<IKeyManagementRepository, KeyManagementRepository>();
            services.AddTransient<IKeyManagementGroupRepository, KeyManagementGroupRepository>();
            services.AddTransient<IGatewayGroupRepository, GatewayGroupRepository>();
            services.AddTransient<IGatewayRepository, GatewayRepository>();
            services.AddSingleton<IPerformanceRepository, PerformanceRepository>();
            services.AddTransient<IUserStampRepository, UserStampRepository>();
            services.AddTransient<IChangeHistoryRepository, ChangeHistoryRepository>();
            services.AddTransient<IChangeHistoryTypeRepository, ChangeHistoryTypeRepository>();
            services.AddTransient<IGXHistoryModuleUserSettings, GXHistoryModuleUserSettings>();
            services.AddTransient<INotificationRepository, NotificationRepository>();
            services.AddTransient<INotificationSourceRepository, NotificationSourceRepository>();
            services.AddTransient<IDataExchange, DataExchangeRepository>();
            services.AddTransient<IAppearanceRepository, AppearanceRepository>();
            services.AddTransient<IEnumTypeRepository, EnumTypeRepository>();
            services.AddTransient<ILocalizedResourceRepository, LocalizedResourceRepository>();
            services.AddTransient<INotificationTypeRepository, NotificationTypeRepository>();
            services.AddTransient<IPolicyRepository, PolicyRepository>();
            services.AddScoped<GXAccountRegistrationPolicy>();
        }

        /// <summary>
        /// Get connection pool size from the database.
        /// </summary>
        /// <param name="connection">Connection</param>
        /// <returns>Connection pool size.</returns>
        private static int GetConnectionPoolSize(GXDbConnection connection)
        {
            GXSchemaManager manager = new(connection);
            if (!manager.TableExist<GXConfiguration>())
            {
                return Environment.ProcessorCount * 8;
            }
            var args = GXSelectArgs.Select<GXConfiguration>(s => s.Settings!, w => w.Name == GXConfigurations.System);
            try
            {
                GXConfiguration? conf = connection.SingleOrDefault<GXConfiguration>(args);
                if (conf == null || string.IsNullOrEmpty(conf.Settings))
                {
                    return Environment.ProcessorCount * 8;
                }
                SystemSettings? settings = JsonSerializer.Deserialize<SystemSettings>(conf.Settings);
                if (settings == null)
                {
                    return Environment.ProcessorCount * 8;
                }
                return settings.PoolSize;
            }
            catch (Exception)
            {
                return Environment.ProcessorCount * 8;
            }
        }

        /// <summary>
        /// Connect to the database.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="logger">The logger.</param>
        internal static async Task<GXDbConnection?> ConnectToDatabase(WebApplicationBuilder builder, ILogger<GXConfiguration> logger)
        {
            // Add services to the container.
            DatabaseOptions db = builder.Configuration.GetSection("Database").Get<DatabaseOptions>() ?? throw new InvalidOperationException("The Database configuration section is missing.");
            string settings = db.Settings;
            string type = db.Type;
            bool disabled = db.Disabled;
            if (disabled)
            {
                if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
                {
                    logger.LogInformation("{Message}", ServerHelpers.GetInvariantString(nameof(Properties.Resources.DatabaseServiceIsDisabled)));
                }
            }
            else
            {
                if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
                {
                    logger.LogDebug("{Message}", string.Format(System.Globalization.CultureInfo.InvariantCulture, ServerHelpers.GetInvariantString(nameof(Properties.Resources.DatabaseType)), type));
                }
                if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
                {
                    logger.LogDebug("{Message}", string.Format(System.Globalization.CultureInfo.InvariantCulture, ServerHelpers.GetInvariantString(nameof(Properties.Resources.Connecting)), settings));
                }
            }
            int connectionCount;
            GXDbConnection? connection = null;
            SqliteConnection? sqliteInitializationConnection = null;
            builder.Services.AddSingleton<GXSqlLoggingInterceptor>();
            builder.Services.AddSingleton<GXSqlEvents>();
            builder.Services.AddSingleton<IGXAmiSqlEvents>(sp => sp.GetRequiredService<GXSqlEvents>());
            if (disabled || string.IsNullOrEmpty(type))
            {
                //Gurux.DLMS.AMI DB is defined elsewhere.
            }
            else if (string.Compare(type, "Oracle", true) == 0)
            {
                connection = new GXDbConnection(new OracleConnection(settings), null);
                connectionCount = GetConnectionPoolSize(connection);
                builder.Services.AddDbContextPool<ApplicationDbContext>((sp, options) =>
                {
                    var interceptor = sp.GetRequiredService<GXSqlLoggingInterceptor>();
                    options.UseOracle(settings)
                    .AddInterceptors(interceptor);
                },
                poolSize: connectionCount);
                builder.Services.AddSingleton<IDbConnectionFactory>(sp =>
                {
                    return new SqlConnectionFactory(DatabaseType.Oracle, settings)
                    {
                        MaxPoolSize = connectionCount,
                        QueryCache = new GXQueryCache(TimeSpan.FromMinutes(10), DatabaseType.Oracle),
                        Interceptor = sp.GetRequiredService<GXSqlLoggingInterceptor>()
                    };
                });

            }
            else if (string.Compare(type, "MSSQL", true) == 0)
            {
                connection = new GXDbConnection(new SqlConnection(settings), null);
                connectionCount = GetConnectionPoolSize(connection);
                builder.Services.AddDbContextPool<ApplicationDbContext>((sp, options) =>
                  {
                      var interceptor = sp.GetRequiredService<GXSqlLoggingInterceptor>();
                      options.UseSqlServer(settings)
                      .AddInterceptors(interceptor);
                  },
                poolSize: connectionCount);
                builder.Services.AddSingleton<IDbConnectionFactory>(sp =>
                {
                    return new SqlConnectionFactory(DatabaseType.MSSQL, settings)
                    {
                        MaxPoolSize = connectionCount,
                        QueryCache = new GXQueryCache(TimeSpan.FromMinutes(10), DatabaseType.MSSQL),
                        Interceptor = sp.GetRequiredService<GXSqlLoggingInterceptor>()
                    };
                });
            }
            else if (string.Compare(type, "MySQL", true) == 0)
            {
                connection = new GXDbConnection(new MySqlConnection(settings), null);
                connectionCount = GetConnectionPoolSize(connection);
                builder.Services.AddDbContextPool<ApplicationDbContext>((sp, options) =>
                {
                    var interceptor = sp.GetRequiredService<GXSqlLoggingInterceptor>();
                    options.UseMySQL(settings)
                    .AddInterceptors(interceptor);
                },
                poolSize: connectionCount);
                builder.Services.AddSingleton<IDbConnectionFactory>(sp =>
                {
                    return new SqlConnectionFactory(DatabaseType.MySQL, settings)
                    {
                        MaxPoolSize = connectionCount,
                        QueryCache = new GXQueryCache(TimeSpan.FromMinutes(10), DatabaseType.MySQL),
                        Interceptor = sp.GetRequiredService<GXSqlLoggingInterceptor>()
                    };
                });
            }
            else if (string.Compare(type, "SQLite", true) == 0)
            {
                // SQLite table rebuilds require foreign keys disabled before the schema transaction starts.
                // This connection is used only for initialization; regular connections retain enforcement.
                var sqliteSettings = new SqliteConnectionStringBuilder(settings) { ForeignKeys = false };
                sqliteInitializationConnection = new SqliteConnection(sqliteSettings.ConnectionString);
                connection = new GXDbConnection(sqliteInitializationConnection, null);
                connectionCount = GetConnectionPoolSize(connection);
                builder.Services.AddDbContextPool<ApplicationDbContext>((sp, options) =>
                {
                    options.UseSqlite(settings)
                        .AddInterceptors(sp.GetRequiredService<GXSqlLoggingInterceptor>());
                }, poolSize: connectionCount);
                builder.Services.AddSingleton<IDbConnectionFactory>(sp =>
                    new SqlConnectionFactory(DatabaseType.SqLite, settings)
                    {
                        QueryCache = new GXQueryCache(TimeSpan.FromMinutes(10), DatabaseType.SqLite),
                        Interceptor = sp.GetRequiredService<GXSqlLoggingInterceptor>()
                    });
            }
            else if (string.Compare(type, "PostgreSQL", true) == 0)
            {
                connection = new GXDbConnection(new NpgsqlConnection(settings), null);
                connectionCount = GetConnectionPoolSize(connection);
                builder.Services.AddDbContextPool<ApplicationDbContext>((sp, options) =>
                {
                    var interceptor = sp.GetRequiredService<GXSqlLoggingInterceptor>();
                    options.UseNpgsql(settings)
                    .AddInterceptors(interceptor);
                },
                poolSize: connectionCount);
                builder.Services.AddSingleton<IDbConnectionFactory>(sp =>
                {
                    return new SqlConnectionFactory(DatabaseType.PostgreSQL, settings)
                    {
                        MaxPoolSize = connectionCount,
                        QueryCache = new GXQueryCache(TimeSpan.FromMinutes(10), DatabaseType.PostgreSQL),
                        Interceptor = sp.GetRequiredService<GXSqlLoggingInterceptor>()
                    };
                });

            }
            else
            {
                throw new Exception("Invalid connection type. " + type);
            }
            builder.Services.AddTransient<IGXDatabase, GXDatabase>();
            if (connection != null)
            {
                // FilterBy builds boolean expressions before a connection executes
                // them. Use this application's provider for those query defaults.
                GXDbConnection.DefaultDatabaseType = connection.DatabaseType;
            }
            await InitializeDatabaseAsync(connection, logger);
            if (sqliteInitializationConnection != null)
            {
                using var command = sqliteInitializationConnection.CreateCommand();
                // Enable concurrent readers before serving requests: layout ORM reads must
                // not block EF Identity UPDATE...RETURNING commits. The schema transaction
                // has finished, and SQLite keeps in-memory databases in memory journal mode.
                command.CommandText = "PRAGMA journal_mode = WAL";
                await command.ExecuteScalarAsync();
                command.CommandText = "PRAGMA foreign_keys = ON";
                await command.ExecuteNonQueryAsync();
            }
            return connection;
        }

        internal static void DropTables(GXDbConnection? connection, ILogger<GXConfiguration> logger)
        {
            if (connection != null)
            {
                GXSchemaManager manager = new(connection);
                logger.LogInformation("Dropping tables.");
                IDbTransaction t = connection.BeginTransaction();
                try
                {
                    manager.ForceDropTable<GXUrlAlias>(t);
                    manager.ForceDropTable<GXConfiguration>(t);
                    manager.ForceDropTable<GXAppearance>(t);
                    manager.ForceDropTable<GXUser>(t);
                    manager.ForceDropTable<GXLanguage>(t);
                    manager.ForceDropTable<GXLogType>(t);
                    manager.ForceDropTable<GXPolicyRequirement>(t);
                    t.Commit();
                }
                catch (Exception)
                {
                    t?.Rollback();
                    throw;
                }
                foreach (var it in manager.GetTables())
                {
                    if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
                    {
                        logger.LogInformation("Table {Value0} was not dropped.", it);
                    }
                    manager.DropTable(it);
                }
            }
        }

        /// <summary>
        /// Initialize database.
        /// </summary>
        internal static async Task InitializeDatabaseAsync(GXDbConnection? connection,
            ILogger<GXConfiguration> logger)
        {
            if (connection != null)
            {
                GXSchemaManager manager = new(connection);
                if (!manager.TableExist<GXDevice>())
                {
                    logger.LogInformation("Creating tables.");
                    var t = await connection.BeginTransactionAsync();
                    try
                    {
                        //Appereance table is created first, because it is used by GXUser and there is a cross reference.
                        manager.CreateTable<GXAppearance>(t, false, false);
                        manager.CreateTable<GXNotificationType>(t, false, false);
                        manager.CreateTable<GXNotification>(t, false, false);

                        manager.CreateTable<GXUserGroup>(t, true, false);

                        //Update foreign keys for GXUserGroup and GXUser.
                        manager.UpdateTable<GXAppearance>(t);
                        manager.UpdateTable<GXNotification>(t);
                        manager.UpdateTable<GXNotificationType>(t);

                        //Add ASP.Net user management.
                        if (!manager.TableExist(t, typeof(GXDeviceCodes)))
                        {
                            manager.CreateTable<GXDeviceCodes>(t, false, false);
                        }
                        manager.CreateTable<GXRoleClaim>(t, true, false);
                        if (!manager.TableExist(t, typeof(GXUserClaim)))
                        {
                            manager.CreateTable<GXUserClaim>(t, false, false);
                            //Update foreign keys.
                            manager.UpdateTable<GXUserClaim>(t);
                        }
                        if (!manager.TableExist(t, typeof(GXUserRole)))
                        {
                            manager.CreateTable<GXUserRole>(t, false, false);
                        }
                        if (!manager.TableExist(t, typeof(GXUserLogin)))
                        {
                            manager.CreateTable<GXUserLogin>(t, false, false);
                        }
                        if (!manager.TableExist(t, typeof(GXUserToken)))
                        {
                            manager.CreateTable<GXUserToken>(t, false, false);
                            //Update foreign keys.
                            manager.UpdateTable<GXUserToken>(t);
                            manager.UpdateTable<GXUserClaim>(t);
                        }
                        if (!manager.TableExist(t, typeof(GXKey)))
                        {
                            manager.CreateTable<GXKey>(t, false, false);
                        }
                        if (!manager.TableExist(t, typeof(GXPerformance)))
                        {
                            manager.CreateTable<GXPerformance>(t, false, false);
                        }
                        manager.CreateTable<GXLog>(t, true, false);
                        manager.CreateTable<GXUrlAlias>(t, true, false);
                        if (!manager.TableExist(t, typeof(GXAppearance)))
                        {
                            manager.CreateTable<GXAppearance>(t, false, false);
                        }
                        if (!manager.TableExist(t, typeof(GXConfiguration)))
                        {
                            manager.CreateTable<GXConfiguration>(t, false, false);
                        }
                        manager.CreateTable<GXLanguage>(t, true, false);
                        manager.CreateTable<GXEnumType>(t, true, false);
                        manager.CreateTable<Server.Services.KeyManagement.GXPasswordChangeRecord>(t, false, false);
                        manager.CreateTable<Server.Services.KeyManagement.GXPasswordChangeLock>(t, false, false);
                        manager.CreateTable<GXLog>(t, true, false);
                        manager.CreateTable<GXLogType>(t, true, false);
                        manager.CreateTable<GXPersistedGrants>(t, true, false);
                        manager.CreateTable<GXPolicyRequirement>(t, true, false);
                        manager.CreateTable<GXCatalogProduct>(t, true, false);

                        manager.CreateTable<GXModulePolicy>(t, false, false);
                        manager.CreateTable<GXScriptPolicy>(t, true, false);
                        manager.CreateTable<GXTriggerPolicy>(t, true, false);
                        manager.CreateTable<GXWorkflowPolicy>(t, true, false);
                        manager.CreateTable<GXWorkflowProperty>(t, true, false);
                        manager.CreateTable<GXWorkflowOperation>(t, true, false);
                        manager.CreateTable<GXMenuLinkPolicy>(t, true, false);
                        manager.CreateTable<GXMenuPolicy>(t, true, false);
                        manager.CreateTable<GXModulePolicy>(t, true, false);
                        manager.CreateTable<GXLogTypePolicy>(t, true, false);
                        manager.CreateTable<GXNotificationTypePolicy>(t, true, false);
                        manager.CreateTable<GXNotificationSource>(t, true, false);
                        manager.CreateTable<GXNotificationSourceMapping>(t, true, false);
                        manager.CreateTable<GXBlockGroupPolicy>(t, true, false);
                        manager.CreateTable<GXAppearancePolicy>(t, true, false);

                        if (!manager.TableExist(t, typeof(GXScriptTemplateRelease)))
                        {
                            manager.CreateTable<GXScriptTemplateRelease>(t, false, false);
                        }
                        manager.CreateTable<GXScriptTemplateHistory>(t, true, false);
                        manager.CreateTable<GXScriptDependency>(t, true, false);
                        manager.CreateTable<GXUserPermission>(t, true, false);
                        if (!manager.TableExist(t, typeof(GXKeyAudit)))
                        {
                            manager.CreateTable<GXKeyAudit>(t, false, false);
                        }
                        if (!manager.TableExist(t, typeof(GXKeyDeployment)))
                        {
                            manager.CreateTable<GXKeyDeployment>(t, false, false);
                        }
                        if (!manager.TableExist(t, typeof(GXView)))
                        {
                            manager.CreateTable<GXView>(t, false, false);
                        }
                        if (!manager.TableExist(t, typeof(GXViewColumn)))
                        {
                            manager.CreateTable<GXViewColumn>(t, false, false);
                        }
                        manager.CreateTable<GXViewPolicy>(t, true, false);
                        manager.CreateTable<GXViewColumnPolicy>(t, true, false);
                        // The user identity was established before its reverse relationships were traversed.
                        // Complete standalone/current DTO tables as well, then attach the full user schema.
                        foreach (var table in TypeHelper.GetTypesFromNamespace(typeof(GXUser).Assembly, "Gurux.DLMS.AMI.Shared.DTOs"))
                        {
                            bool unique = table.GetInterfaces().Any(it => it.IsGenericType && it.GetGenericTypeDefinition() == typeof(IUnique<>));
                            if (unique && !manager.TableExist(t, table))
                            {
                                manager.CreateTable(t, table, true, false);
                            }
                        }
                        manager.UpdateTable<GXUser>(t);
                        await PolicyRepository.EnsureNotificationSourcesAsync(connection, t);
                        await ChangeHistoryTypeRepository.EnsureDefaultsAsync(connection, t);
                        await EnumTypeRepository.EnsureAgentStatusesAsync(connection, t);
                        connection.Insert(t, GXInsertArgs.InsertRange(ConfigurationRepository.CreateDefaultConfigurationSettings().Select(l => l.Configuration!).ToArray()));
                        UpdateComponentViews(connection, t);
                        t.Commit();
                    }
                    catch (Exception)
                    {
                        t?.Rollback();
                        throw;
                    }
                    //Check that all tables exists.
                    TypeHelper.GetTypesFromNamespace(typeof(GXUser).Assembly, "Gurux.DLMS.AMI.Shared.DTOs")
                        .Concat([typeof(GXKeyAudit), typeof(GXKeyDeployment), typeof(GXPasswordChangeRecord), typeof(GXPasswordChangeLock)])
                        .ToList().ForEach(type =>
                    {
                        bool isUnique = type.GetInterfaces().Any(it =>
                            it.IsGenericType &&
                            it.GetGenericTypeDefinition() == typeof(IUnique<>));
                        if (isUnique && !manager.TableExist(type))
                        {
                            throw new Exception($"Table for type {type.Name} does not exist.");
                        }
                    });
                }
                else
                {
                    //Is admin added.
                    AdminAdded = connection.SingleOrDefault<int>(GXSelectArgs.Select<GXUser>(q => GXSql.Count(q))) != 0;
                    await using var statusTransaction = await connection.BeginTransactionAsync();
                    await EnumTypeRepository.EnsureAgentStatusesAsync(connection, statusTransaction);
                    await statusTransaction.CommitAsync();
                    //TODO: remove later.
                    var args = GXSelectArgs.Select<GXKeyManagementKey>(q => GXSql.Count(q), w => w.State == KeyState.PreActive && w.Removed == null);
                    bool preaction = connection.SingleOrDefault<int>(args) != 0;
                    if (preaction)
                    {
                        var value = new GXKeyManagementKey
                        {
                            State = Shared.Enums.KeyState.Active
                        };
                        var update = GXUpdateArgs.Update(value, q => q.State!);
                        update.Where.And<GXKeyManagementKey>(q =>
                            q.State == Shared.Enums.KeyState.PreActive &&
                            q.Removed == null);
                        await connection.UpdateAsync(update);
                    }
                }
                await MenuRepository.EnsureConfigurationMenuAsync(connection);
            }
        }

        /// <summary>
        /// Update database.
        /// </summary>
        internal static void UpdateDatabase(GXDbConnection? connection,
            ILogger<GXConfiguration> logger)
        {
            if (connection != null)
            {
                IDbTransaction transaction = connection.BeginTransaction();
                try
                {
                    GXSchemaManager manager = new(connection);
                    foreach (var type in TypeHelper.GetTypesFromNamespace(typeof(GXUser).Assembly, "Gurux.DLMS.AMI.Shared.DTOs")
                        .Concat([typeof(GXKeyAudit), typeof(GXKeyDeployment), typeof(GXPasswordChangeRecord), typeof(GXPasswordChangeLock)]))
                    {
                        bool isUnique = type.GetInterfaces().Any(item => item.IsGenericType && item.GetGenericTypeDefinition() == typeof(IUnique<>));
                        if (isUnique || type == typeof(GXViewPolicy) || type == typeof(GXViewColumnPolicy) || type == typeof(GXNotificationSource))
                        {
                            if (!manager.TableExist(transaction, type))
                            {
                                logger.LogInformation("Creating table {Table}.", type.Name);
                                manager.CreateTable(transaction, type, true, false);
                            }
                            else if (manager.GetTableChanges(transaction, type).Length != 0)
                            {
                                manager.UpdateTable(transaction, type);
                            }
                        }
                    }
                    ChangeHistoryTypeRepository.EnsureDefaultsAsync(connection, transaction).GetAwaiter().GetResult();
                    EnumTypeRepository.EnsureAgentStatusesAsync(connection, transaction).GetAwaiter().GetResult();
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction?.Rollback();
                    throw;
                }
                MenuRepository.EnsureConfigurationMenuAsync(connection).GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Get the default admin user.
        /// </summary>
        /// <param name="connection">DB Connection.</param>
        /// <param name="transaction">DB Transaction.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Default admin user.</returns>
        public static async Task<ClaimsPrincipal?> GetDefaultAdminUserAsync(GXDbConnection connection,
            IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            var admin = await GetDefaultAdmin(connection, transaction, cancellationToken);
            if (admin != null)
            {
                var arg = GXSelectArgs.SelectAll<GXRole>();
                arg.Joins.AddInnerJoin<GXRole, GXUserRole>(a => a.Id, b => b.RoleId!);
                arg.Where.And<GXUserRole>(where => where.UserId == admin.Id);
                admin.Roles ??= [];
                admin.Roles.AddRange((await connection.SelectAsync<GXRole>(transaction, arg, cancellationToken)).Select(s => s.Name!));
                return CreateClaimsPrincipalFromUser(admin);
            }
            return null;
        }

        /// <summary>
        /// Get the default admin user.
        /// </summary>
        /// <param name="connection">DB Connection.</param>
        /// <param name="transaction">DB Transaction.</param>
        /// <returns>Default admin user.</returns>
        public static ClaimsPrincipal? GetDefaultAdminUser(GXDbConnection connection,
            IDbTransaction? transaction = null)
        {
            var admin = GetDefaultAdmin(connection, transaction).Result;
            if (admin != null)
            {
                var arg = GXSelectArgs.SelectAll<GXRole>();
                arg.Joins.AddInnerJoin<GXRole, GXUserRole>(a => a.Id, b => b.RoleId!);
                arg.Where.And<GXUserRole>(where => where.UserId == admin.Id);
                admin.Roles ??= [];
                admin.Roles.AddRange([.. connection.Select<GXRole>(arg).Select(s => s.Name!)]);
                return CreateClaimsPrincipalFromUser(admin);
            }
            return null;
        }

        public static T? GetServerSettings<T>(GXDbConnection connection, string configuration)
        {
            return (T?)GetServerSettingsAsync(connection, configuration, typeof(T)).Result;
        }

        public static async Task<object?> GetServerSettingsAsync(GXDbConnection connection,
            string configuration, Type type)
        {
            GXSelectArgs args = GXSelectArgs.SelectAll<GXConfiguration>(w => w.Name == configuration);
            GXConfiguration? conf = await connection.SingleOrDefaultAsync<GXConfiguration>(args) ?? throw new Exception(string.Format("Unknown configuration '{0}'.", configuration));
            if (string.IsNullOrEmpty(conf.Settings))
            {
                return Activator.CreateInstance(type);
            }
            return JsonSerializer.Deserialize(conf.Settings, type);
        }
    }
}
