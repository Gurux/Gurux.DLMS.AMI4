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

using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Server.Data;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Oracle.ManagedDataAccess.Client;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Security.Claims;
using Gurux.DLMS.AMI.Server.Triggers;
using Gurux.DLMS.AMI.Server.Repository;
using System.Data;
using Gurux.DLMS.AMI.Client.Shared;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Gurux.DLMS.AMI.Shared;
using static Gurux.DLMS.AMI.Server.Internal.ServerHelpers;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using System.Text;

namespace Gurux.DLMS.AMI.Server
{
    internal static class ServerSettings
    {
        /// <summary>
        /// Is 1st user added.
        /// </summary>
        public static bool AdminAdded = false;
        /// <summary>
        /// Server address.
        /// </summary>
        internal static string? ServerAddress = null;

        /// <summary>
        /// Add default configuration settings for the database.
        /// </summary>
        /// <param name="host"></param>
        internal static void AddDefaultConfigurationSettings(IGXHost host)
        {
            List<GXRole> list = new List<GXRole>();
            foreach (var it in typeof(GXRoles).GetFields())
            {
                list.Add(new GXRole()
                {
                    Id = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    Name = it.Name,
                    NormalizedName = it.Name.ToUpper(),
                    Default = it.Name != GXRoles.Admin
                });
            }
            host.Connection.Insert(GXInsertArgs.InsertRange(list));
            //All values are saved using invariant culture.
            CultureInfo? culture = CultureInfo.DefaultThreadCurrentUICulture;
            List<GXConfiguration> configurations = new List<GXConfiguration>();
            try
            {
                CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
                AddSystemConfiguration(configurations);
                AddStatusConfiguration(configurations);
                AddSecurityConfiguration(configurations);
                AddLanguagesConfiguration(configurations);
                AddModulesConfiguration(configurations);
                AddAgentConfiguration(configurations);
                AddComponentViewsConfiguration(configurations);
                AddBlocksConfiguration(configurations);
                AddTriggersConfiguration(configurations);
                AddWorkflowConfiguration(configurations);
                AddScriptConfiguration(configurations);
                AddCronConfiguration(configurations);
                AddManufacturerConfiguration(configurations);
                AddPerformanceConfiguration(configurations);
                AddStatisticsConfiguration(configurations);
                AddMaintenanceConfiguration(configurations);
                AddRolesConfiguration(configurations);
                AddExternalAuthenticationServicesConfiguration(configurations);
                AddKeyManagementConfiguration(configurations);
            }
            finally
            {
                CultureInfo.DefaultThreadCurrentUICulture = culture;
            }
            host.Connection.Insert(GXInsertArgs.InsertRange(configurations));
            //Update Localized strings for the invariant configurations.
            //Only language name is added.
            CultureInfo invariantCulture = new CultureInfo("en");
            GXLanguage language = new GXLanguage()
            {
                Id = invariantCulture.Name,
                EnglishName = invariantCulture.EnglishName,
                NativeName = invariantCulture.NativeName,
                Active = true,
                Default = true
            };
            GXLanguageResourceArgs args = new GXLanguageResourceArgs(host, new[] { language }, typeof(Properties.Resources), null, null, null, null);
            UpdateLanguageResourcesAsync(args).Wait();
        }

        internal static void AddSystemConfiguration(List<GXConfiguration> configurations)
        {
            //Add System configuration. 
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.System,
                Icon = "oi oi-cog",
                Description = Properties.Resources.SystemConfigurationDescription,
                Path = "config/system",
                Order = 1,
                Settings = JsonSerializer.Serialize(new SystemSettings())
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add Status configuration. 
        /// </summary>
        /// <param name="configurations"></param>
        internal static void AddStatusConfiguration(List<GXConfiguration> configurations)
        {
            //Add site version information
            Assembly asm = Assembly.GetExecutingAssembly();
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(asm.Location);
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.Status,
                Icon = "oi oi-dashboard",
                Description = Properties.Resources.SystemStatusDescription,
                Path = "config/status",
                Order = 2,
                Settings = JsonSerializer.Serialize(new StatusSettings() { SiteVersion = info.FileVersion })
            };
            configurations.Add(conf);
        }

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
        internal static void AddSecurityConfiguration(List<GXConfiguration> configurations)
        {
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.Security,
                Icon = "oi oi-shield",
                Description = Properties.Resources.SystemSecurityDescription,
                //Properties.Resources.SystemSecuritySettings
                Path = "config/security",
                Order = 3,
                Settings = JsonSerializer.Serialize(new IdentityOptions())
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add cultures configuration.
        /// </summary>
        internal static void AddLanguagesConfiguration(List<GXConfiguration> configurations)
        {
            List<LanguageSettings> list = new List<LanguageSettings>();
            ResourceManager rm = new ResourceManager(typeof(Properties.Resources));
            foreach (string file in
                Directory.EnumerateFiles(Path.GetDirectoryName(typeof(Properties.Resources).Assembly.Location),
                "Gurux.DLMS.AMI.Server.resources.dll", SearchOption.AllDirectories))
            {
                try
                {
                    Assembly assembly = Assembly.LoadFrom(file);
                    string name = Path.GetDirectoryName(file);
                    name = name.Substring(name.Length - 2);
                    CultureInfo ci = new CultureInfo(name);
                    LanguageSettings it = new LanguageSettings()
                    {
                        Id = ci.Name,
                        EnglishName = ci.EnglishName,
                        NativeName = ci.NativeName
                    };
                    list.Add(it);
                }
                catch (Exception ex)
                {

                }
            }
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.Language,
                Icon = "oi oi-globe",
                Description = Properties.Resources.LanguageDescription,
                Path = "config/language",
                Order = 4,
                Settings = JsonSerializer.Serialize(list.ToArray())
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add modules configuration.
        /// </summary>
        internal static void AddModulesConfiguration(List<GXConfiguration> configurations)
        {
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.Modules,
                Icon = "oi oi-puzzle-piece",
                Description = Properties.Resources.ModuleDescription,
                Path = "config/Module",
                Order = 4,
                Settings = JsonSerializer.Serialize(new ModuleSettings())
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add installable agent configuration.
        /// </summary>
        internal static void AddAgentConfiguration(List<GXConfiguration> configurations)
        {
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.Agents,
                Icon = "oi oi-wifi",
                Description = Properties.Resources.AgentDescription,
                Path = "config/agent",
                Order = 4,
                Settings = JsonSerializer.Serialize(new AgentSettings())
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add component views configuration.
        /// </summary>
        internal static void AddComponentViewsConfiguration(List<GXConfiguration> configurations)
        {
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.Components,
                Icon = "oi oi-layers",
                Description = Gurux.DLMS.AMI.Server.Properties.Resources.ComponentViewDescription,
                Path = "config/Component",
                Order = 5
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add blocks configuration.
        /// </summary>
        internal static void AddBlocksConfiguration(List<GXConfiguration> configurations)
        {
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.Blocks,
                Icon = "oi oi-browser",
                Description = Properties.Resources.BlockDescription,
                Path = "config/Block",
                Order = 6
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add triggers configuration.
        /// </summary>
        internal static void AddTriggersConfiguration(List<GXConfiguration> configurations)
        {
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.Triggers,
                Icon = "oi oi-flash",
                Description = Properties.Resources.TriggerDescription,
                Path = "config/Trigger",
                Order = 6
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add workflow configuration.
        /// </summary>
        internal static void AddWorkflowConfiguration(List<GXConfiguration> configurations)
        {
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.Workflows,
                Icon = "oi oi-fork",
                Description = Properties.Resources.WorkflowDescription,
                Path = "config/Workflow",
                Order = 7
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add manufacturer configuration.
        /// </summary>
        internal static void AddManufacturerConfiguration(List<GXConfiguration> configurations)
        {
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.Manufacturers,
                Icon = "oi oi-share-boxed",
                Description = Properties.Resources.ManufacturerDescription,
                Path = "config/Manufacturer",
                Order = 8,
                Settings = JsonSerializer.Serialize(new Client.Shared.ManufacturerSettings())
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add cron configuration.
        /// </summary>
        internal static void AddCronConfiguration(List<GXConfiguration> configurations)
        {
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.Cron,
                Icon = "oi oi-clock",
                Description = Properties.Resources.CronDescription,
                Path = "config/cron",
                Order = 8,
                Settings = JsonSerializer.Serialize(new CronSettings())
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add key management configuration.
        /// </summary>
        internal static void AddKeyManagementConfiguration(List<GXConfiguration> configurations)
        {
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.KeyManagement,
                Icon = "oi oi-key",
                Description = Properties.Resources.KeyManagementsDescription,
                Path = "config/KeyManagement",
                Order = 8
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add external authentication services configurations.
        /// </summary>
        internal static void AddExternalAuthenticationServicesConfiguration(List<GXConfiguration> configurations)
        {
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.Authentication,
                Icon = "oi oi-lock-unlocked",
                Description = Properties.Resources.ExternalAuthenticationDescription,
                Path = "config/authentications",
                Order = 9,
                Settings = JsonSerializer.Serialize(new AuthenticationSettings[0])
            };
            configurations.Add(conf);
        }


        /// <summary>
        /// Add performance configuration.
        /// </summary>
        internal static void AddPerformanceConfiguration(List<GXConfiguration> configurations)
        {
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.Performance,
                Icon = "oi oi-dial",
                Description = Properties.Resources.PerformanceDescription,
                Path = "config/performance",
                Order = 8,
                Settings = JsonSerializer.Serialize(new PerformanceSettings())
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add role configuration.
        /// </summary>
        internal static void AddRolesConfiguration(List<GXConfiguration> configurations)
        {
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.Roles,
                Icon = "oi oi-badge",
                Description = Properties.Resources.RolesDescription,
                Path = "config/roles",
                Order = 9,
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add statistics configuration.
        /// </summary>
        internal static void AddStatisticsConfiguration(List<GXConfiguration> configurations)
        {
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.Statistic,
                Icon = "oi oi-graph",
                Description = Properties.Resources.StatisticDescription,
                Path = "config/statistic",
                Order = 10,
                Settings = JsonSerializer.Serialize(new StatisticSettings())
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add maintenance configuration.
        /// </summary>
        internal static void AddMaintenanceConfiguration(List<GXConfiguration> configurations)
        {
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.Maintenance,
                Icon = "oi oi-wrench",
                Description = Properties.Resources.MaintenanceDescription,
                Path = "config/maintenance",
                Order = 11
            };
            configurations.Add(conf);
        }


        /// <summary>
        /// Add script configuration.
        /// </summary>
        internal static void AddScriptConfiguration(List<GXConfiguration> configurations)
        {
            Assembly asm = typeof(GXAmiException).Assembly;
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(asm.Location);
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.Scripts,
                Icon = "oi oi-script",
                Description = Properties.Resources.ScriptDescription,
                Path = "config/script",
                Order = 7,
                Settings = JsonSerializer.Serialize(new ScriptSettings()
                {
                    Versions = info.FileVersion
                })
            };
            configurations.Add(conf);
        }

        private static void UpdateScriptVersions(GXDbConnection connection,
            GXConfiguration conf, ScriptSettings settings, Type[] references)
        {
            StringBuilder sb = new StringBuilder();
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
        private static void UpdateScriptCommonSharedVersion(GXDbConnection connection)
        {
            var args = GXSelectArgs.Select<GXConfiguration>(
                s => new { s.Id, s.Settings },
                w => w.Name == GXConfigurations.Scripts);
            try
            {
                GXConfiguration conf = connection.SingleOrDefault<GXConfiguration>(args);
                ScriptSettings? settings = null;
                if (!string.IsNullOrEmpty(conf.Settings))
                {
                    settings = JsonSerializer.Deserialize<ScriptSettings>(conf.Settings);
                }
                if (settings == null)
                {
                    settings = new ScriptSettings();
                }
                Type[] references = new Type[] {
                    typeof(GXAmiException),
                    typeof(GXDateTime)
                };
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
                if (change)
                {
                    UpdateScriptVersions(connection, conf, settings, references);
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Save new component views to the database.
        /// </summary>
        /// <param name="host"></param>
        internal static void UpdateComponentViews(IGXHost host)
        {
            ServerHelpers.UpdateComponentViews(host, typeof(Client.Pages.Admin.ActiveDeviceErrors).Assembly);
            _ = ServerHelpers.UpdateTriggersAsync(host, typeof(ServiceTrigger).Assembly, true).Result;
        }

        static internal void AddRepositories(IServiceCollection services)
        {
            services.AddTransient<IPatRepository, PatRepository>();
            services.AddTransient<ILocalizationRepository, LocalizationRepository>();
            services.AddTransient<IScheduleRepository, ScheduleRepository>();
            services.AddTransient<IScheduleLogRepository, ScheduleLogRepository>();
            services.AddTransient<ISystemLogRepository, SystemLogRepository>();
            services.AddTransient<IUserRepository, UserRepository>();
            services.AddTransient<IUserErrorRepository, UserErrorRepository>();
            services.AddTransient<IUserActionRepository, UserActionRepository>();
            services.AddTransient<IUserGroupRepository, UserGroupRepository>();
            services.AddTransient<IDeviceRepository, DeviceRepository>();
            services.AddTransient<IDeviceErrorRepository, DeviceErrorRepository>();
            services.AddTransient<IObjectRepository, ObjectRepository>();
            services.AddTransient<IObjectTemplateRepository, ObjectTemplateRepository>();
            services.AddTransient<IAttributeRepository, AttributeRepository>();
            services.AddTransient<IAttributeTemplateRepository, AttributeTemplateRepository>();
            services.AddTransient<IValueRepository, ValueRepository>();
            services.AddTransient<IDeviceGroupRepository, DeviceGroupRepository>();
            services.AddTransient<IScheduleGroupRepository, ScheduleGroupRepository>();
            services.AddTransient<IAgentGroupRepository, AgentGroupRepository>();
            services.AddTransient<IAgentRepository, AgentRepository>();
            services.AddTransient<IAgentLogRepository, AgentLogRepository>();
            services.AddTransient<IModuleGroupRepository, ModuleGroupRepository>();
            services.AddTransient<ITriggerGroupRepository, TriggerGroupRepository>();
            services.AddTransient<ITriggerRepository, TriggerRepository>();
            services.AddTransient<IWorkflowGroupRepository, WorkflowGroupRepository>();
            services.AddTransient<IWorkflowRepository, WorkflowRepository>();
            services.AddTransient<IWorkflowLogRepository, WorkflowLogRepository>();
            services.AddTransient<IDeviceTemplateRepository, DeviceTemplateRepository>();
            services.AddTransient<IDeviceTemplateGroupRepository, DeviceTemplateGroupRepository>();
            services.AddSingleton<IBlackListRepository, BlackListRepository>();
            services.AddTransient<IModuleRepository, ModuleRepository>();
            services.AddTransient<IModuleLogRepository, ModuleLogRepository>();
            services.AddTransient<ITaskRepository, TaskRepository>();
            services.AddTransient<IBlockRepository, BlockRepository>();
            services.AddTransient<IBlockGroupRepository, BlockGroupRepository>();
            services.AddSingleton<IConfigurationRepository, ConfigurationRepository>();
            services.AddTransient<IComponentViewRepository, ComponentViewRepository>();
            services.AddTransient<IComponentViewGroupRepository, ComponentViewGroupRepository>();
            services.AddTransient<IScriptRepository, ScriptRepository>();
            services.AddTransient<IScriptLogRepository, ScriptLogRepository>();
            services.AddTransient<IScriptGroupRepository, ScriptGroupRepository>();
            services.AddTransient<IDeviceActionRepository, DeviceActionRepository>();
            services.AddTransient<IRestStatisticRepository, RestStatisticRepository>();
            services.AddTransient<IDeviceTraceRepository, DeviceTraceRepository>();
            services.AddTransient<IRoleRepository, RoleRepository>();
            services.AddTransient<IUserSettingRepository, UserSettingRepository>();
            services.AddTransient<IManufacturerRepository, ManufacturerRepository>();
            services.AddTransient<IManufacturerGroupRepository, ManufacturerGroupRepository>();
            services.AddTransient<IFavoriteRepository, FavoriteRepository>();
            services.AddTransient<IKeyManagementRepository, KeyManagementRepository>();
            services.AddTransient<IKeyManagementGroupRepository, KeyManagementGroupRepository>();
            services.AddTransient<IKeyManagementLogRepository, KeyManagementLogRepository>();
        }

        /// <summary>
        /// Add GitHub claims.
        /// </summary>
        internal static void AddGitHubClaims(OAuthCreatingTicketContext context, JObject user)
        {
            var identifier = user.Value<string>("id");
            if (!string.IsNullOrEmpty(identifier))
            {
                context.Identity.AddClaim(new Claim(
                    ClaimTypes.NameIdentifier, identifier,
                    ClaimValueTypes.String, context.Options.ClaimsIssuer));
            }

            var userName = user.Value<string>("login");
            if (!string.IsNullOrEmpty(userName))
            {
                context.Identity.AddClaim(new Claim(
                    ClaimsIdentity.DefaultNameClaimType, userName,
                    ClaimValueTypes.String, context.Options.ClaimsIssuer));
            }

            var name = user.Value<string>("name");
            if (!string.IsNullOrEmpty(name))
            {
                context.Identity.AddClaim(new Claim(
                    "urn:github:name", name,
                    ClaimValueTypes.String, context.Options.ClaimsIssuer));
            }

            var link = user.Value<string>("url");
            if (!string.IsNullOrEmpty(link))
            {
                context.Identity.AddClaim(new Claim(
                    "urn:github:url", link,
                    ClaimValueTypes.String, context.Options.ClaimsIssuer));
            }
        }

        /// <summary>
        /// Get connection pool size from the database.
        /// </summary>
        /// <param name="connection">Connection</param>
        /// <returns>Connection pool size.</returns>
        private static int GetConnectionPoolSize(GXDbConnection connection)
        {
            if (!connection.TableExist<GXConfiguration>())
            {
                return 10;
            }
            var args = GXSelectArgs.Select<GXConfiguration>(s => s.Settings, w => w.Name == GXConfigurations.System);
            try
            {
                GXConfiguration conf = connection.SingleOrDefault<GXConfiguration>(args);
                if (conf == null || string.IsNullOrEmpty(conf.Settings))
                {
                    return 10;
                }
                SystemSettings? settings = JsonSerializer.Deserialize<SystemSettings>(conf.Settings);
                if (settings == null)
                {
                    return 10;
                }
                return settings.PoolSize;
            }
            catch (Exception ex)
            {
                return 10;
            }
        }

        /// <summary>
        /// Connect to the database.
        /// </summary>
        /// <param name="builder"></param>
        internal static IGXHost ConnectToDb(WebApplicationBuilder builder, ILogger<GXConfiguration> logger)
        {
            // Add services to the container.
            DatabaseOptions db = builder.Configuration.GetSection("Database").Get<DatabaseOptions>();
            string settings = db.Settings;
            string type = db.Type;
            bool disabled = db.Disabled;
            if (disabled)
            {
                logger.LogInformation(Properties.Resources.DatabaseServiceIsDisabled);
            }
            else
            {
                logger.LogDebug(Properties.Resources.DatabaseType, type);
                logger.LogDebug(Properties.Resources.Connecting, settings);
            }
            int connectionCount;
            List<DbConnection> connections = new List<DbConnection>();
            DbConnection? connection = null;
            if (disabled || string.IsNullOrEmpty(type))
            {
                //Gurux.DLMS.AMI DB is defined elsewhere.
            }
            else if (string.Compare(type, "Oracle", true) == 0)
            {
                connection = new OracleConnection(settings);
                connectionCount = GetConnectionPoolSize(new GXDbConnection(connection, null));
                for (int pos = 0; pos != connectionCount; ++pos)
                {
                    connections.Add(new OracleConnection(settings));
                }
            }
            else if (string.Compare(type, "MSSQL", true) == 0)
            {
                connection = new SqlConnection(settings);

                connectionCount = GetConnectionPoolSize(new GXDbConnection(connection, null));
                for (int pos = 0; pos != connectionCount; ++pos)
                {
                    connections.Add(new SqlConnection(settings));
                }
                builder.Services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(settings));

            }
            else if (string.Compare(type, "MySQL", true) == 0)
            {
                connection = new MySql.Data.MySqlClient.MySqlConnection(settings);
                connectionCount = GetConnectionPoolSize(new GXDbConnection(connection, null));

                for (int pos = 0; pos != connectionCount; ++pos)
                {
                    connections.Add(new MySql.Data.MySqlClient.MySqlConnection(settings));
                }
                //Add My SQL for EF.
                builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySQL(settings));
            }
            else if (string.Compare(type, "SQLite", true) == 0)
            {
                connection = new SQLiteConnection(settings);
                connectionCount = GetConnectionPoolSize(new GXDbConnection(connection, null));
                for (int pos = 0; pos != connectionCount; ++pos)
                {
                    connections.Add(new SQLiteConnection(settings));
                }
                //Add SQL lite for EF.
                builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(settings));
            }
            else
            {
                throw new Exception("Invalid connection type. " + type);
            }
            GXHost? host = null;
            if (connections.Count != 0)
            {
                host = new GXHost()
                {
                    Connection = new GXDbConnection(connections.ToArray(), null)
                };
                if (!host.Connection.TableExist<GXDevice>())
                {
                    Console.WriteLine("Creating tables.");
                    IDbTransaction t = host.Connection.BeginTransaction();
                    try
                    {
                        host.Connection.CreateTable<GXSystemLog>(false, false);
                        //Add ASP.Net user management.
                        host.Connection.CreateTable<GXUserGroup>(true, false);
                        host.Connection.CreateTable<GXPersistedGrants>(false, false);
                        host.Connection.CreateTable<GXDeviceCodes>(false, false);
                        host.Connection.CreateTable<GXRoleClaim>(false, false);
                        host.Connection.CreateTable<GXUserClaim>(false, false);
                        host.Connection.CreateTable<GXUserRole>(false, false);
                        host.Connection.CreateTable<GXUserLogin>(false, false);
                        host.Connection.CreateTable<GXUserToken>(false, false);
                        host.Connection.CreateTable<GXKey>(false, false);
                        AddDefaultConfigurationSettings(host);
                        UpdateComponentViews(host);
                        t.Commit();
                    }
                    catch (Exception)
                    {
                        if (t != null)
                        {
                            t.Rollback();
                        }
                        throw;
                    }
                }
                else
                {
                    //Is admin added.
                    AdminAdded = host.Connection.SingleOrDefault<int>(GXSelectArgs.Select<GXUser>(q => GXSql.Count(q))) != 0;
                    //LastRead, LastWrite and LastAction added for GXAttribute, GXObject and GXDevice.
                    host.Connection.UpdateTable<GXAttribute>();
                    host.Connection.UpdateTable<GXObject>();
                    host.Connection.UpdateTable<GXDevice>();
                    host.Connection.UpdateTable<GXValue>();
                    host.Connection.UpdateTable<GXAttributeTemplate>();
                    //Available serial ports for the agent.
                    host.Connection.UpdateTable<GXAgent>();
                    //Manufacturers added.
                    if (!host.Connection.TableExist<GXManufacturerGroup>())
                    {
                        host.Connection.CreateTable<GXManufacturerGroup>(false, false);
                        host.Connection.CreateTable<GXManufacturer>(false, false);
                        host.Connection.CreateTable<GXManufacturerGroupManufacturer>(false, false);
                        host.Connection.CreateTable<GXUserGroupManufacturerGroup>(false, false);
                        host.Connection.CreateTable<GXDeviceModel>(false, false);
                        host.Connection.CreateTable<GXDeviceVersion>(false, false);
                        host.Connection.CreateTable<GXDeviceSettings>(false, false);
                        List<GXConfiguration> configurations = new List<GXConfiguration>();
                        //All values are saved using invariant culture.
                        CultureInfo? culture = CultureInfo.DefaultThreadCurrentUICulture;
                        try
                        {
                            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
                            AddManufacturerConfiguration(configurations);
                        }
                        finally
                        {
                            CultureInfo.DefaultThreadCurrentUICulture = culture;
                        }
                        host.Connection.Insert(GXInsertArgs.InsertRange(configurations));
                        host.Connection.CreateTable<GXFavorite>(false, false);
                    }
                    if (!host.Connection.TableExist<GXKeyManagementGroup>())
                    {
                        host.Connection.CreateTable<GXKeyManagementGroup>(false, false);
                        host.Connection.CreateTable<GXKeyManagement>(false, false);
                        host.Connection.CreateTable<GXKeyManagementLog>(false, false);
                        host.Connection.CreateTable<GXKeyManagementGroupKeyManagement>(false, false);
                        host.Connection.CreateTable<GXUserGroupKeyManagementGroup>(false, false);
                        host.Connection.CreateTable<GXKeyManagementKey>(false, false);
                        List<GXConfiguration> configurations = new List<GXConfiguration>();
                        //All values are saved using invariant culture.
                        CultureInfo? culture = CultureInfo.DefaultThreadCurrentUICulture;
                        try
                        {
                            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
                            AddKeyManagementConfiguration(configurations);
                        }
                        finally
                        {
                            CultureInfo.DefaultThreadCurrentUICulture = culture;
                        }
                        host.Connection.Insert(GXInsertArgs.InsertRange(configurations));
                        //System title moved from the GXDLMSSettings to device property.
                        host.Connection.UpdateTable<GXDevice>();
                        //Agent cache expiration time added.
                        host.Connection.UpdateTable<GXAgent>();
                    }
                    //https://www.gurux.fi/issue/guruxdlmsami4/21165
                    host.Connection.UpdateTable<GXDevice>();
                }
            }
            else
            {
                host = new GXHost();
            }
            //Update shared version.
            if (host.Connection != null)
            {
                UpdateScriptCommonSharedVersion(host.Connection);
            }
            builder.Services.AddSingleton<IGXHost>(q => host);
            builder.Services.AddSingleton<IGXDatabase>(q => host);
            return host;
        }

        /// <summary>
        /// Get the default admin user.
        /// </summary>
        /// <param name="host">DB Connection.</param>
        /// <returns>Default admin user.</returns>
        public static ClaimsPrincipal? GetDefaultAdminUser(IGXHost host)
        {
            GXSelectArgs arg = GXSelectArgs.Select<GXUser>(s => s.Id, where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUser, GXUserRole>(j => j.Id, j => j.UserId);
            arg.Joins.AddInnerJoin<GXUserRole, GXRole>(j => j.RoleId, j => j.Id);
            arg.Where.And<GXRole>(where => where.Name == "Admin");
            var admins = host.Connection.SelectAsync<GXUser>(arg).Result;
            if (admins.Any())
            {
                var admin = admins.First();
                arg = GXSelectArgs.SelectAll<GXRole>();
                arg.Joins.AddInnerJoin<GXRole, GXUserRole>(a => a.Id, b => b.RoleId);
                arg.Where.And<GXUserRole>(where => where.UserId == admin.Id);
                if (admin.Roles == null)
                {
                    admin.Roles = new List<string>();
                }
                admin.Roles.AddRange(host.Connection.Select<GXRole>(arg).Select(s => s.Name).ToList());
                return CreateClaimsPrincipalFromUser(admin);
            }
            return null;
        }

        public static T? GetServerSettings<T>(IGXHost host, string configuration)
        {
            return (T?)GetServerSettingsAsync(host, configuration, typeof(T)).Result;
        }

        public static async Task<object?> GetServerSettingsAsync(IGXHost host, string configuration, Type type)
        {
            GXSelectArgs args = GXSelectArgs.SelectAll<GXConfiguration>(w => w.Name == configuration);
            GXConfiguration conf = await host.Connection.SingleOrDefaultAsync<GXConfiguration>(args);
            if (conf == null)
            {
                throw new Exception(string.Format("Unknown configuration '{0}'.", configuration));
            }
            if (string.IsNullOrEmpty(conf.Settings))
            {
                return Activator.CreateInstance(type);
            }
            return JsonSerializer.Deserialize(conf.Settings, type);
        }
    }
}