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

using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Components;
using Gurux.DLMS.AMI.Components.Enums;
using Gurux.DLMS.AMI.Data;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Repository;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Server.Triggers;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.Content;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using Gurux.DLMS.AMI.Shared.DTOs.Report;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Subtotal;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.UrlAlias;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using static Gurux.DLMS.AMI.Server.Internal.ServerHelpers;

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
        internal static string ServerAddress = default!;

        /// <summary>
        /// Add role scopes to the database.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="services"></param>
        /// <returns></returns>
        internal static async Task UpdateRoleScopes(IGXHost host, IServiceCollection services)
        {
            Dictionary<string, List<string>> policies = new Dictionary<string, List<string>>();
            var authorizationPolicyCollectionProvider = services
                .BuildServiceProvider()
                .GetRequiredService<IAuthorizationPolicyProvider>();

            var actionDescriptorCollectionProvider = services
                .BuildServiceProvider()
                .GetRequiredService<IActionDescriptorCollectionProvider>();

            var actionDescriptors = actionDescriptorCollectionProvider.ActionDescriptors.Items;

            foreach (var actionDescriptor in actionDescriptors)
            {
                if (actionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
                {
                    string controllerName = controllerActionDescriptor.ControllerName;
                    AuthorizeAttribute? authoriseAttribute = null;
                    var authorisation = (controllerActionDescriptor?.EndpointMetadata
                        .Where(p => p.GetType() == typeof(AuthorizeAttribute)));

                    if (authorisation != null && authorisation.Any())
                        authoriseAttribute = (AuthorizeAttribute)(authorisation).First();
                    if (authoriseAttribute != null)
                    {
                        var policy = authoriseAttribute.Policy;
                        if (policy != null)
                        {
                            var authorizationPolicy = await authorizationPolicyCollectionProvider.GetPolicyAsync(authoriseAttribute.Policy);
                            if (authorizationPolicy == null)
                            {
                                continue;
                            }
                            foreach (IAuthorizationRequirement it in authorizationPolicy.Requirements)
                            {
                                if (it is ScopeRequirement sr)
                                {
                                    foreach (var role in sr.Scopes)
                                    {
                                        string name;
                                        int pos = role.IndexOf(".");
                                        if (pos != -1)
                                        {
                                            name = role.Substring(0, pos).Replace("-", "");
                                        }
                                        else
                                        {
                                            name = role;
                                        }
                                        string scope = role.Substring(1 + pos);
                                        List<string>? list2;
                                        if (!policies.TryGetValue(name, out list2))
                                        {
                                            list2 = new List<string>();
                                            policies.Add(name, list2);
                                        }
                                        //Add scope.
                                        if (!list2.Contains(scope))
                                        {
                                            list2.Add(scope);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            GXSelectArgs args = GXSelectArgs.SelectAll<GXRole>(w => w.Removed == null);
            args.Columns.Add<GXScope>();
            args.Joins.AddLeftJoin<GXRole, GXScope>(j => j.Id, j => j.Role);
            //All managers roles are removed.
            GXDeleteArgs del = GXDeleteArgs.Delete<GXRole>(w => w.Name.Contains("Manager"));
            await host.Connection.DeleteAsync(del);
            List<GXRole> list5 = host.Connection.Select<GXRole>(args);
            List<GXRole> roles = new List<GXRole>();
            List<GXScope> scopes = new List<GXScope>();
            foreach (var it in typeof(GXRoles).GetFields())
            {
                //Add scopes if not manager or admin.
                if (it.Name != GXRoles.Admin &&
                    !it.Name.EndsWith("Manager"))
                {
                    if (it.Name.ToLower() == "useraction")
                    {
                        continue;
                    }
                    GXRole? role = list5.Where(w => w.Name == it.Name).SingleOrDefault();
                    if (role == null)
                    {
                        role = new GXRole(it.Name)
                        {
                            ConcurrencyStamp = Guid.NewGuid().ToString(),
                            NormalizedName = it.Name.ToUpper(),
                            Default = it.Name != GXRoles.Admin
                        };
                        roles.Add(role);
                    }
                    if (!policies.TryGetValue(it.Name.ToLower(), out List<string>? scopes2))
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                    foreach (var scope in scopes2)
                    {
                        if (role.Scopes?.Where(w => w.Name == scope).Any() != true)
                        {
                            scopes.Add(new GXScope()
                            {
                                Role = role,
                                Name = scope
                            });
                        }
                    }
                }
            }
            host.Connection.Insert(GXInsertArgs.InsertRange(roles));
            host.Connection.Insert(GXInsertArgs.InsertRange(scopes));
        }

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
                AddMenuConfiguration(configurations);
                AddContentTypeConfiguration(configurations);
                AddContentConfiguration(configurations);
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
                AddSubtotalConfiguration(configurations);
                AddReportConfiguration(configurations);
                AddNotificationConfiguration(configurations);
                AddPruneConfiguration(configurations);
                AddDataExchangeConfiguration(configurations);
                AddAppearanceConfiguration(configurations);
                AddProxyConfiguration(configurations);
                AddIpAccessControlConfiguration(configurations);
                AddAccountConfiguration(configurations);
            }
            finally
            {
                CultureInfo.DefaultThreadCurrentUICulture = culture;
            }
            host.Connection.Insert(GXInsertArgs.InsertRange(configurations));
        }

        internal static void AddSystemConfiguration(List<GXConfiguration> configurations)
        {
            //Add System configuration. 
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.System,
                Icon = GXConfigurations.System,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.SystemConfigurationDescription)),
                Path = "config/System",
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
                Icon = GXConfigurations.Status,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.SystemStatusDescription)),
                Path = "config/Status",
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
                Icon = GXConfigurations.Security,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.SystemSecurityDescription)),
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
                        NativeName = ci.NativeName,
                        Updated = DateTime.Now
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
                Icon = GXConfigurations.Language,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.LanguageDescription)),
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
                Icon = GXConfigurations.Modules,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.ModuleDescription)),
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
                Name = GXConfigurations.Agent,
                Icon = GXConfigurations.Agent,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.AgentDescription)),
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
                Icon = GXConfigurations.Components,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.ComponentViewDescription)),
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
                Name = GXConfigurations.Block,
                Icon = GXConfigurations.Block,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.BlockDescription)),
                Path = "config/Block",
                Order = 6
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add menu configuration.
        /// </summary>
        internal static void AddMenuConfiguration(List<GXConfiguration> configurations)
        {
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.Menu,
                Icon = GXConfigurations.Menu,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.MenuDescription)),
                Path = "config/Menu",
                Order = 6
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add content configuration.
        /// </summary>
        internal static void AddContentConfiguration(List<GXConfiguration> configurations)
        {
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.Content,
                Icon = GXConfigurations.Content,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.ContentDescription)),
                Path = "config/Content",
                Order = 6
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add content type configuration.
        /// </summary>
        internal static void AddContentTypeConfiguration(List<GXConfiguration> configurations)
        {
            GXConfiguration conf = new GXConfiguration()
            {
                Name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.ContentTypes)),
                Icon = GXConfigurations.ContentType,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.ContentTypeDescription)),
                Path = "config/ContentType",
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
                Name = GXConfigurations.Trigger,
                Icon = GXConfigurations.Trigger,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.TriggerDescription)),
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
                Name = GXConfigurations.Workflow,
                Icon = GXConfigurations.Workflow,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.WorkflowDescription)),
                Path = "config/Workflow",
                Order = 7
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add subtotal configuration.
        /// </summary>
        internal static void AddSubtotalConfiguration(List<GXConfiguration> configurations)
        {
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.Subtotal,
                Icon = GXConfigurations.Subtotal,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.SubtotalDescription)),
                Path = "config/Subtotal",
                Order = 8
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add report configuration.
        /// </summary>
        internal static void AddReportConfiguration(List<GXConfiguration> configurations)
        {
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.Report,
                Icon = GXConfigurations.Report,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.ReportDescription)),
                Path = "config/Report",
                Order = 9
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add notification configuration.
        /// </summary>
        internal static void AddNotificationConfiguration(List<GXConfiguration> configurations)
        {
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.Notification,
                Icon = GXConfigurations.Notification,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.NotificationDescription)),
                Path = "config/Notification",
                Order = 10
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add prune configuration.
        /// </summary>
        internal static void AddPruneConfiguration(List<GXConfiguration> configurations)
        {
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.Prune,
                Icon = GXConfigurations.Prune,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.PruneDescription)),
                Path = "config/prune",
                Order = 11,
                Settings = JsonSerializer.Serialize(new CronSettings())
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add data exchange configuration.
        /// </summary>
        internal static void AddDataExchangeConfiguration(List<GXConfiguration> configurations)
        {
            GXConfiguration conf = new GXConfiguration()
            {
                Name = "Data Exchange",
                Icon = GXConfigurations.DataExchange,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.DataExchangeDescription)),
                Path = "config/DataExchange",
                Order = 12
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add appearance configuration.
        /// </summary>
        internal static void AddAppearanceConfiguration(List<GXConfiguration> configurations)
        {
            var settings = new AppearanceSettings();
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.Appearance,
                Icon = GXConfigurations.Appearance,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.AppearanceDescription)),
                Path = "config/appearance",
                Order = 13,
                Settings = JsonSerializer.Serialize(settings)
            };
            configurations.Add(conf);
        }


        /// <summary>
        /// Add proxy configuration.
        /// </summary>
        internal static void AddProxyConfiguration(List<GXConfiguration> configurations)
        {
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.Proxy,
                Icon = GXConfigurations.Proxy,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.ProxyDescription)),
                Path = "config/proxy",
                Order = 14,
            };
            configurations.Add(conf);
        }


        /// <summary>
        /// Add block list configuration.
        /// </summary>
        internal static void AddIpAccessControlConfiguration(List<GXConfiguration> configurations)
        {
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.IpAccessControl,
                Icon = GXConfigurations.IpAccessControl,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.IpAccessDescription)),
                Path = "config/IpAccessControl",
                Order = 15,
            };
            configurations.Add(conf);
        }

        /// <summary>
        /// Add account configuration.
        /// </summary>
        internal static void AddAccountConfiguration(List<GXConfiguration> configurations)
        {
            var settings = new AccountSettings();
            GXConfiguration conf = new GXConfiguration()
            {
                Name = GXConfigurations.Account,
                Icon = GXConfigurations.Account,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.AccountDescription)),
                Path = "config/Account",
                Order = 16,
                Settings = JsonSerializer.Serialize(settings)
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
                Name = GXConfigurations.Manufacturer,
                Icon = GXConfigurations.Manufacturer,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.ManufacturerDescription)),
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
                Icon = GXConfigurations.Cron,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.CronDescription)),
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
                Icon = GXConfigurations.KeyManagement,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.KeyManagementsDescription)),
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
                Icon = GXConfigurations.Authentication,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.ExternalAuthenticationDescription)),
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
                Icon = GXConfigurations.Performance,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.PerformanceDescription)),
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
                Icon = GXConfigurations.Roles,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.RolesDescription)),
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
                Icon = GXConfigurations.Statistic,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.StatisticDescription)),
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
                Icon = GXConfigurations.Maintenance,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.MaintenanceDescription)),
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
                Icon = GXConfigurations.Scripts,
                Description = ServerHelpers.GetInvariantString(nameof(Properties.Resources.ScriptDescription)),
                Path = "config/Script",
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
                if (!string.IsNullOrEmpty(conf?.Settings))
                {
                    settings = JsonSerializer.Deserialize<ScriptSettings>(conf.Settings);
                }
                if (settings == null)
                {
                    settings = new ScriptSettings();
                }
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
                if (change)
                {
                    UpdateScriptVersions(connection, conf, settings, references);
                }
            }
            catch (Exception)
            {
            }
        }

        private static GXAppearance CreateTheme(string name,
            string description, string path, bool active = false)
        {
            string value = JsonSerializer.Serialize(new GXThemeInfo()
            {
                Name = name,
                CssFile = path,
            });
            return new GXAppearance((byte)ResourceType.Theme, name, description, path)
            {
                Active = active,
                Value = value
            };
        }

        private static GXAppearance CreateIconpack(string name,
           string description, string path, bool active = false)
        {
            return new GXAppearance((byte)ResourceType.Iconpack, name, description, path)
            {
                Active = active,
                Value = path
            };
        }


        private static List<GXAppearance> GetIconpacks()
        {
            return [
            CreateIconpack("Bootstrap", "Bootstrap",
            "https://cdn.jsdelivr.net/npm/bootstrap-icons@1.13.1/font/bootstrap-icons.min.css"),
            CreateIconpack("FontAwesome", "FontAwesome", "https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.2/css/all.min.css"),
            CreateIconpack("MaterialIcons", "Material Icons", "https://fonts.googleapis.com/icon?family=Material+Icons"),
            ];
        }

        private static List<GXAppearance> GetThemes()
        {
            return [
            CreateTheme("Default", "", "", true),
        CreateTheme("Cerulean", "","https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/cerulean/bootstrap.min.css" ),
        CreateTheme("Cosmo",   "", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/cosmo/bootstrap.min.css" ),
        CreateTheme("Cyborg", "",  "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/cyborg/bootstrap.min.css" ),
        CreateTheme("Darkly", "",  "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/darkly/bootstrap.min.css" ),
        CreateTheme("Flatly", "",  "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/flatly/bootstrap.min.css" ) ,
        CreateTheme("Journal","",  "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/journal/bootstrap.min.css" ) ,
        CreateTheme("Litera",  "", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/litera/bootstrap.min.css" ) ,
        CreateTheme("Lumen",  "",  "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/lumen/bootstrap.min.css" ) ,
        CreateTheme("Lux",   "",   "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/lux/bootstrap.min.css" ) ,
        CreateTheme("Materia", "", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/materia/bootstrap.min.css" ),
        CreateTheme("Minty", "",   "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/minty/bootstrap.min.css" ),
        CreateTheme("Pulse",  "",  "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/pulse/bootstrap.min.css" ) ,
        CreateTheme("Sandstone","", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/sandstone/bootstrap.min.css" ) ,
        CreateTheme("Simplex", "", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/simplex/bootstrap.min.css" ) ,
        CreateTheme("Sketchy", "", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/sketchy/bootstrap.min.css" ) ,
        CreateTheme("Slate",  "",  "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/slate/bootstrap.min.css" ) ,
        CreateTheme("Solar",  "",  "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/solar/bootstrap.min.css" ) ,
        CreateTheme("Spacelab","", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/spacelab/bootstrap.min.css" ) ,
        CreateTheme("Superhero", "","https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/superhero/bootstrap.min.css" ) ,
        CreateTheme("United",  "", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/united/bootstrap.min.css" ) ,
        CreateTheme("Yeti",   "",  "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/yeti/bootstrap.min.css" ) ];
        }

        /// <summary>
        /// Adds a new menu link to the specified menu or returns the existing link with the given name.
        /// </summary>
        /// <remarks>If a link with the specified name already exists in the menu, the existing link is
        /// returned and no changes are made. If roles are provided, they are associated with the new menu link. The
        /// method updates the menu's link collection accordingly.</remarks>
        /// <param name="host">The host context used to access the data connection and perform database operations. Cannot be null.</param>
        /// <param name="now">The timestamp to assign as the creation time for the menu link.</param>
        /// <param name="menu">The menu to which the link will be added. Cannot be null.</param>
        /// <param name="visibility">The visibility level to assign to the menu link.</param>
        /// <param name="targetType">Menu link target type.</param>
        /// <param name="link">The URL or target address for the menu link. Cannot be null or empty.</param>
        /// <param name="name">The unique name of the menu link. Used to identify existing links. Cannot be null or empty.</param>
        /// <param name="description">An optional description for the menu link. May be null.</param>
        /// <param name="icon">An optional icon identifier or path for the menu link. May be null.</param>
        /// <param name="padges">An optional array of badge strings to associate with the menu link. May be null.</param>
        /// <param name="padgeTitle">An optional title for the badges associated with the menu link. May be null.</param>
        /// <param name="roles">An optional collection of role identifiers that are allowed to access the menu link. May be null.</param>
        /// <param name="type">Blazor target type.</param>
        /// <returns>The menu link that was added to the menu, or the existing link if one with the specified name already
        /// exists.</returns>
        private static GXMenuLink AddLink(IGXHost host, DateTime now,
            GXMenu menu,
            Visibility visibility,
            string link, string name,
            string? description = null,
            string? icon = null,
            string[]? padges = null,
            string? padgeTitle = null,
            IEnumerable<string>? roles = null,
            Type? type = null)
        {
            GXSelectArgs arg = GXSelectArgs.Select<GXMenuLink>(s => s.Id, w => w.Name == name);
            GXMenuLink item = host.Connection.SingleOrDefault<GXMenuLink>(arg);
            if (item == null)
            {
                MenuLinkType targetType = type == null ? MenuLinkType.Link : MenuLinkType.Content;
                item = new GXMenuLink()
                {
                    MenuLinkType = targetType,
                    Visibility = (byte)visibility,
                    Menu = menu,
                    Order = menu.Links?.Count + 1 ?? 1,
                    Icon = icon,
                    CreationTime = now,
                    Name = name,
                    Description = description,
                    Url = link,
                    Padges = padges != null ? string.Join(';', padges) : null,
                    PadgeTitle = padgeTitle,
                    TargetType = type != null ? type.FullName : null,
                };

                host.Connection.Insert(GXInsertArgs.Insert(item));
                if (roles?.Any() == true)
                {
                    List<GXMenuLinkRole> tmp = new List<GXMenuLinkRole>();
                    foreach (string role in roles)
                    {
                        GXRole r = host.Connection.SelectById<GXRole>(role);
                        if (r != null)
                        {
                            item.Roles?.Add(r);
                            GXMenuLinkRole mlr = new GXMenuLinkRole()
                            {
                                MenuLinkId = item.Id,
                                RoleId = r.Id,
                            };
                            tmp.Add(mlr);
                        }
                    }
                    if (tmp.Any())
                    {
                        host.Connection.Insert(GXInsertArgs.InsertRange(tmp));
                    }
                }
                menu.Links?.Add(item);
            }
            return item;
        }

        private static void AddNavMenu(IGXHost host, GXMenuGroup def, GXUser creator, DateTime now)
        {
            GXSelectArgs arg = GXSelectArgs.Select<GXMenu>(s => s.Id, w => w.Name == GXMenus.NavMenu);
            GXMenu menu = host.Connection.SingleOrDefault<GXMenu>(arg);
            if (menu == null)
            {
                menu = new GXMenu(GXMenus.NavMenu)
                {
                    Title = "Navication Menu",
                    Description = "The Navigation menu contains links intended for site visitors.",
                    CreationTime = now,
                    Creator = creator,
                };
                //NavMenu is shown only for login users.
                var user = host.Connection.SelectById<GXRole>(GXRoles.User);
                if (user != null)
                {
                    menu.Roles = [user];
                }
                host.Connection.Insert(GXInsertArgs.Insert(menu));
                GXMenuGroupMenu mg = new GXMenuGroupMenu()
                {
                    MenuId = menu.Id,
                    MenuGroupId = def.Id,
                    CreationTime = now,
                };
                host.Connection.Insert(GXInsertArgs.Insert(mg));
            }
            AddLink(host, now, menu, Visibility.All, "/", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Home)), null, "Home");
            AddLink(host, now, menu, Visibility.All, "/device", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Devices)), null, "Device", [TargetType.DeviceLog, TargetType.DeviceTrace], "New Logs", [GXRoles.Admin, GXRoles.DeviceManager]);
            AddLink(host, now, menu, Visibility.All, "/Schedule", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Schedules)), null, "Schedule", [TargetType.ScheduleLog], "New Logs", [GXRoles.Admin, GXRoles.ScheduleManager]);
            AddLink(host, now, menu, Visibility.All, "/DeviceTemplate", ServerHelpers.GetInvariantString(nameof(Properties.Resources.DeviceTemplates)), null, "DeviceTemplate", null, "New Logs", [GXRoles.Admin, GXRoles.DeviceTemplateManager]);
            AddLink(host, now, menu, Visibility.All, "/User", ServerHelpers.GetInvariantString(nameof(Properties.Resources.UserManagement)), null, "User", [TargetType.UserError, TargetType.UserAction], "New Logs", [GXRoles.Admin, GXRoles.DeviceTemplateManager]);
            AddLink(host, now, menu, Visibility.All, "/Agent", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Agents)), null, "Agents", [TargetType.AgentLog], "New Logs", [GXRoles.Admin, GXRoles.DeviceTemplateManager]);
            AddLink(host, now, menu, Visibility.All, "/Gateway", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Gateway)), null, "Gateway", [TargetType.GatewayLog], "New Logs", [GXRoles.Admin, GXRoles.DeviceTemplateManager]);
            AddLink(host, now, menu, Visibility.All, "/Logs", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Logs)), null, "Log", null, null, [GXRoles.Admin, GXRoles.DeviceTemplateManager]);
            AddLink(host, now, menu, Visibility.All, "/Config", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Configuration)), null, "Configuration", null, null, [GXRoles.Admin, GXRoles.DeviceTemplateManager]);
            AddLink(host, now, menu, Visibility.All, "/Favorites", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Favorites)), null, "Favorite", null, null, [GXRoles.Admin, GXRoles.User]);
        }

        private static void AddHamburgerMenu(IGXHost host, GXMenuGroup def, GXUser creator, DateTime now)
        {
            GXSelectArgs arg = GXSelectArgs.Select<GXMenu>(s => s.Id, w => w.Name == GXMenus.HamburgerMenu);
            GXMenu menu = host.Connection.SingleOrDefault<GXMenu>(arg);
            if (menu == null)
            {
                menu = new GXMenu(GXMenus.HamburgerMenu)
                {
                    Title = "Hamburger Menu",
                    Description = "The hamburger menu provides access to navigation links.",
                    CreationTime = now,
                    Creator = creator,
                };
                //NavMenu is shown for all users.
                menu.Roles = null;
                host.Connection.Insert(GXInsertArgs.Insert(menu));
                GXMenuGroupMenu mg = new GXMenuGroupMenu()
                {
                    MenuId = menu.Id,
                    MenuGroupId = def.Id,
                    CreationTime = now,
                };
                host.Connection.Insert(GXInsertArgs.Insert(mg));
            }
            AddLink(host, now, menu, Visibility.All, "/Login", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Login)), null, "Login");
            AddLink(host, now, menu, Visibility.All, "/Logout", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Logout)), null, "Logout");
        }

        private static void AddLogTabs(IGXHost host, GXMenuGroup def, GXUser creator, DateTime now)
        {
            GXSelectArgs arg = GXSelectArgs.Select<GXMenu>(s => s.Id, w => w.Name == GXMenus.LogTab);
            GXMenu menu = host.Connection.SingleOrDefault<GXMenu>(arg);
            if (menu == null)
            {
                menu = new GXMenu(GXMenus.LogTab)
                {
                    Title = "Log tab links.",
                    Description = "The Log tab contains links to log-related views and actions.",
                    CreationTime = now,
                    Creator = creator,
                };
                //LogTab is shown only for admin users.
                var admin = host.Connection.SelectById<GXRole>(GXRoles.Admin);
                if (admin != null)
                {
                    menu.Roles = [admin];
                }
                host.Connection.Insert(GXInsertArgs.Insert(menu));
                GXMenuGroupMenu mg = new GXMenuGroupMenu()
                {
                    MenuId = menu.Id,
                    MenuGroupId = def.Id,
                    CreationTime = now,
                };
                host.Connection.Insert(GXInsertArgs.Insert(mg));
            }
            AddLink(host, now, menu, Visibility.All, "Logs/SystemLog", ServerHelpers.GetInvariantString(nameof(Properties.Resources.SystemLogs)), null, TargetType.SystemLog, [TargetType.SystemLog], "New logs", [GXRoles.Admin, GXRoles.SystemLogManager], typeof(Client.Pages.Admin.SystemLog));
            AddLink(host, now, menu, Visibility.All, "Logs/Task", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Tasks)), null, TargetType.Task, [TargetType.Task], "New Tasks", [GXRoles.Admin, GXRoles.TaskManager], typeof(Client.Pages.Admin.Tasks));
            AddLink(host, now, menu, Visibility.All, "Logs/ScheduleLog", ServerHelpers.GetInvariantString(nameof(Properties.Resources.ScheduleLogs)), null, TargetType.ScheduleLog, [TargetType.ScheduleLog], "New logs", [GXRoles.Admin, GXRoles.ScheduleLog, GXRoles.ScheduleLogManager], typeof(Client.Pages.Schedule.ScheduleLogs));
            AddLink(host, now, menu, Visibility.All, "Logs/Performances", ServerHelpers.GetInvariantString(nameof(Properties.Resources.Performance)), null, TargetType.Performance, [TargetType.Performance], "New performance", [GXRoles.Admin], typeof(Client.Pages.Admin.Performances));
            AddLink(host, now, menu, Visibility.All, "Logs/ModuleLog", ServerHelpers.GetInvariantString(nameof(Properties.Resources.ModuleLogs)), null, TargetType.ModuleLog, [TargetType.ModuleLog], "New logs", [GXRoles.Admin, GXRoles.ModuleLog], typeof(Client.Pages.Module.ModuleLogs));
            AddLink(host, now, menu, Visibility.All, "Logs/ScriptLog", ServerHelpers.GetInvariantString(nameof(Properties.Resources.ScriptLog)), null, TargetType.ScriptLog, [TargetType.ScriptLog], "New logs", [GXRoles.Admin, GXRoles.ScriptLogManager], typeof(Client.Pages.Script.ScriptLogs));
            AddLink(host, now, menu, Visibility.All, "Logs/WorkflowLog", ServerHelpers.GetInvariantString(nameof(Properties.Resources.WorkflowLog)), null, TargetType.WorkflowLog, [TargetType.WorkflowLog], "New logs", [GXRoles.Admin, GXRoles.WorkflowLogManager], typeof(Client.Pages.Workflow.WorkflowLogs));
        }

        /// <summary>
        /// Adds the default set of menus to the specified host.
        /// </summary>
        /// <param name="host">The host to which the default menus will be added. Cannot be null.</param>
        internal static void AddDefaultMenus(IGXHost host)
        {
            DateTime now = DateTime.Now;
            var adminUser = GetDefaultAdminUser(host);
            var creator = new GXUser() { Id = ClientHelpers.GetUserId(adminUser) };
            string name = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Default));
            GXSelectArgs arg = GXSelectArgs.Select<GXMenuGroup>(s => s.Id, w => w.Name == name);
            GXMenuGroup def = host.Connection.SingleOrDefault<GXMenuGroup>(arg);
            if (def == null)
            {
                def = new GXMenuGroup(ServerHelpers.GetInvariantString(nameof(Properties.Resources.Default)))
                {
                    CreationTime = now,
                    Creator = creator
                };
                host.Connection.Insert(GXInsertArgs.Insert(def));
            }
            //Add log tab links first because nav menu is using it.
            AddLogTabs(host, def, creator, now);
            AddNavMenu(host, def, creator, now);
            AddHamburgerMenu(host, def, creator, now);
        }

        private static string UnknownImage = "<svg width=\"16\" height=\"16\" viewBox=\"0 0 16 16\" xmlns=\"http://www.w3.org/2000/svg\">\r\n  <!-- Box -->\r\n  <rect x=\"1\" y=\"1\" width=\"14\" height=\"14\" stroke=\"black\" stroke-width=\"1\" fill=\"none\" />\r\n  \r\n  <!-- Cross -->\r\n  <line x1=\"1\" y1=\"1\" x2=\"15\" y2=\"15\" stroke=\"black\" stroke-width=\"1\" />\r\n  <line x1=\"15\" y1=\"1\" x2=\"1\" y2=\"15\" stroke=\"black\" stroke-width=\"1\" />\r\n</svg>\r\n";

        private static GXAppearance AddImage(string id, string value)
        {
            return new GXAppearance()
            {
                Id = id,
                ResourceType = (byte)ResourceType.Image,
                Value = value,
            };
        }
        internal static void UpdateLanguages(IGXHost host)
        {
            var supportedLanguages = new string[] { "en", "fi", "de", "fr", "it", "sv", "ee" };
            var languages = host.Connection.SelectAll<GXLanguage>();
            CultureInfo invariantCulture = new CultureInfo("en");
            GXLanguage language;
            if (!languages.Where(w => w.Id == invariantCulture.TwoLetterISOLanguageName).Any())
            {
                language = new GXLanguage()
                {
                    Id = invariantCulture.Name,
                    EnglishName = invariantCulture.EnglishName,
                    NativeName = invariantCulture.NativeName,
                    Active = true,
                    Default = true
                };
                languages.Add(language);
                host.Connection.Insert(GXInsertArgs.Insert(language));
            }
            foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures | CultureTypes.NeutralCultures))
            {
                if (supportedLanguages.Contains(culture.TwoLetterISOLanguageName) &&
                    !languages.Where(w => w.Id == culture.TwoLetterISOLanguageName).Any())
                {
                    language = new GXLanguage()
                    {
                        Id = culture.TwoLetterISOLanguageName,
                        EnglishName = culture.EnglishName,
                        NativeName = culture.NativeName,
                    };
                    host.Connection.Insert(GXInsertArgs.Insert(language));
                    languages.Add(language);
                }
            }
        }

        /// <summary>
        /// Add default apperenances to the database.
        /// </summary>
        /// <param name="host"></param>
        internal static void UpdateAppereances(IGXHost host)
        {
            var adminUser = GetDefaultAdminUser(host);
            var admin = new GXUser() { Id = ClientHelpers.GetUserId(adminUser) };
            DateTime now = DateTime.Now;
            var apperenances = GetThemes();
            apperenances.AddRange(GetIconpacks());
            apperenances.Add(AddImage("Unknown", UnknownImage));
            apperenances.Add(AddImage("Search", "&#x1F50D;"));
            apperenances.Add(AddImage("Save", "&#10004;"));
            apperenances.Add(AddImage("Ok", "&#10004;"));
            apperenances.Add(AddImage("Cancel", "&#10006;"));
            apperenances.Add(AddImage("Add", "&#10133;"));
            apperenances.Add(AddImage("Remove", "&#10134;"));
            apperenances.Add(AddImage("Edit", "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"16\" height=\"16\" viewBox=\"0 0 16 16\" fill=\"currentColor\">\r\n    <path d=\"M6 0l-1 1 2 2 1-1-2-2zm-2 2l-4 4v2h2l4-4-2-2z\"/>\r\n</svg>\r\n"));
            apperenances.Add(AddImage("Update", "&#9851;"));
            apperenances.Add(AddImage("Refresh", "&#9851;"));
            apperenances.Add(AddImage("Clone", "&#128464;"));
            apperenances.Add(AddImage("Clear", "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"24\" height=\"24\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\"     stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"     aria-label=\"trash can\">  <path d=\"M3 6h18\"/>  <path d=\"M8 6l1-2h6l1 2\"/>  <path d=\"M6 6l1 16h10l1-16\"/>  <path d=\"M10 11v7\"/>  <path d=\"M14 11v7\"/></svg>"));
            apperenances.Add(AddImage("Home", "&#x1F3E0;"));
            apperenances.Add(AddImage("Favorite", "&#9734;"));
            apperenances.Add(AddImage("Configuration", "&#128736;"));
            apperenances.Add(AddImage("Log", "&#129534;"));
            apperenances.Add(AddImage("Logs", "&#129534;"));
            apperenances.Add(AddImage("User", "&#128100;"));
            apperenances.Add(AddImage("UserGroup", "&#128101;"));
            apperenances.Add(AddImage("DeviceTemplate", "&#128196;"));
            apperenances.Add(AddImage("DeviceTemplates", "&#128196;"));
            apperenances.Add(AddImage("DeviceTemplateGroups", "&#128196;"));
            apperenances.Add(AddImage("Help", "&#10067;"));
            apperenances.Add(AddImage("Gateway", "&#128225;"));
            apperenances.Add(AddImage("SystemLog", ""));
            apperenances.Add(AddImage("Agent", ""));
            apperenances.Add(AddImage("Connections", ""));
            apperenances.Add(AddImage("Favorite", "&#9734;"));
            apperenances.Add(AddImage("Task", ""));
            apperenances.Add(AddImage("Tasks", ""));
            apperenances.Add(AddImage("Device", "&#128268;"));
            apperenances.Add(AddImage("Performance", "&#128200;"));
            apperenances.Add(AddImage("ReportGroups", ""));
            apperenances.Add(AddImage("Schedule", "&#128338;"));
            apperenances.Add(AddImage("Schedules", "&#128338;"));
            apperenances.Add(AddImage("Roles", ""));
            apperenances.Add(AddImage("Menus", "&#x2630;"));
            apperenances.Add(AddImage("Prune", ""));
            apperenances.Add(AddImage("System", "&#9881;"));
            apperenances.Add(AddImage("Subtotal", "&#x1F9EE;"));
            apperenances.Add(AddImage("Statistic", "&#128480;"));
            apperenances.Add(AddImage("IpAccessControl", "&#128423;"));
            apperenances.Add(AddImage("Appearance", ""));
            apperenances.Add(AddImage("Run", ""));
            apperenances.Add(AddImage("Icon", "&#x1F5BC;"));
            apperenances.Add(AddImage("Blocks", ""));
            apperenances.Add(AddImage("Account", "&#128273;"));

            apperenances.Add(AddImage("ModuleGroups", ""));
            apperenances.Add(AddImage("ModulesManage", ""));
            apperenances.Add(AddImage("ModuleLog", ""));
            apperenances.Add(AddImage("ModuleLogs", ""));

            apperenances.Add(AddImage("Theme", "&#127912;"));
            apperenances.Add(AddImage("Images", ""));
            apperenances.Add(AddImage("IconPack", ""));
            apperenances.Add(AddImage("Text", ""));
            apperenances.Add(AddImage("Color", "&#x1F3A8;"));

            apperenances.Add(AddImage("KeyManagement", "&#128272;"));
            apperenances.Add(AddImage("KeyManagements", "&#128272;"));
            apperenances.Add(AddImage("KeyManagementGroups", ""));
            apperenances.Add(AddImage("Script", "&#128187;"));
            apperenances.Add(AddImage("Scripts", "&#128187;"));
            apperenances.Add(AddImage("ScriptLog", ""));
            apperenances.Add(AddImage("Proxy", ""));
            apperenances.Add(AddImage("DataExchange", ""));
            apperenances.Add(AddImage("Cron", ""));
            apperenances.Add(AddImage("Notification", ""));
            apperenances.Add(AddImage("Authentication", "&#128737;"));
            apperenances.Add(AddImage("Maintenance", ""));
            apperenances.Add(AddImage("Report", ""));
            apperenances.Add(AddImage("Manufacturer", ""));
            apperenances.Add(AddImage("Modules", "&#x1F9E9;"));
            apperenances.Add(AddImage("Contents", ""));
            apperenances.Add(AddImage("Triggers", ""));
            apperenances.Add(AddImage("Language", "&#127757;"));
            apperenances.Add(AddImage("Language", "&#127757;"));
            apperenances.Add(AddImage("ContentTypes", ""));
            apperenances.Add(AddImage("Workflows", ""));
            apperenances.Add(AddImage("Components", ""));
            apperenances.Add(AddImage("Agents", ""));
            apperenances.Add(AddImage("LocalizedResource", "&#128506;"));

            apperenances.Add(AddImage("ScheduleLog", "&#128338;&#129534;"));
            apperenances.Add(AddImage("ScheduleLogs", "&#128338;&#129534;"));
            apperenances.Add(AddImage("ScheduleGroups", ""));
            apperenances.Add(AddImage("Read", ""));
            apperenances.Add(AddImage("DeviceTraces", ""));
            apperenances.Add(AddImage("DeviceActions", ""));
            apperenances.Add(AddImage("DeviceLogs", ""));
            apperenances.Add(AddImage("Devices", "&#128268;"));
            apperenances.Add(AddImage("DeviceGroups", ""));
            apperenances.Add(AddImage("Status", ""));
            apperenances.Add(AddImage("Security", "&#128737;"));

            apperenances.Add(AddImage("AgentLog", ""));
            apperenances.Add(AddImage("UserActions", ""));
            apperenances.Add(AddImage("AgentGroups", ""));
            apperenances.Add(AddImage("UserGroups", ""));
            apperenances.Add(AddImage("UserErrors", ""));
            apperenances.Add(AddImage("Users", ""));
            //Add flags for countries.
            apperenances.Add(AddImage("fi", "<svg xmlns=\"http://www.w3.org/2000/svg\" id=\"fi\" viewBox=\"0 0 24 16\">\r\n  <rect width=\"24\" height=\"16\" fill=\"white\"/>\r\n  <rect x=\"6\" width=\"4\" height=\"16\" fill=\"#003580\"/>\r\n  <rect y=\"6\" width=\"24\" height=\"4\" fill=\"#003580\"/>\r\n</svg>"));
            apperenances.Add(AddImage("fr", "<svg xmlns=\"http://www.w3.org/2000/svg\" id=\"fr\" viewBox=\"0 0 24 16\">\r\n  <rect width=\"8\" height=\"16\" fill=\"#0055A4\"/>\r\n  <rect x=\"8\" width=\"8\" height=\"16\" fill=\"white\"/>\r\n  <rect x=\"16\" width=\"8\" height=\"16\" fill=\"#EF4135\"/>\r\n</svg>"));
            apperenances.Add(AddImage("de", "<svg xmlns=\"http://www.w3.org/2000/svg\" id=\"de\" viewBox=\"0 0 24 16\">\r\n  <rect width=\"24\" height=\"16\" fill=\"black\"/>\r\n  <rect y=\"5.33\" width=\"24\" height=\"5.33\" fill=\"red\"/>\r\n  <rect y=\"10.67\" width=\"24\" height=\"5.33\" fill=\"gold\"/>\r\n</svg>"));
            apperenances.Add(AddImage("it", "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 16\">\r\n    <rect width=\"8\" height=\"16\" fill=\"#008C45\"/>\r\n    <rect x=\"8\" width=\"8\" height=\"16\" fill=\"white\"/>\r\n    <rect x=\"16\" width=\"8\" height=\"16\" fill=\"#CD212A\"/>\r\n</svg>\r\n"));
            apperenances.Add(AddImage("sv", "<svg xmlns=\"http://www.w3.org/2000/svg\" id=\"sv\" viewBox=\"0 0 24 16\">\r\n  <rect width=\"24\" height=\"16\" fill=\"#0057B7\"/>\r\n  <rect x=\"6\" width=\"4\" height=\"16\" fill=\"#FFCC00\"/>\r\n  <rect y=\"6\" width=\"24\" height=\"4\" fill=\"#FFCC00\"/>\r\n</svg>"));
            apperenances.Add(AddImage("en", "<svg xmlns=\"http://www.w3.org/2000/svg\"\r\n     width=\"60\" height=\"30\" viewBox=\"0 0 60 30\">\r\n    <clipPath id=\"clip\">\r\n        <path d=\"M0,0 v30 h60 v-30 z\"/>\r\n    </clipPath>\r\n    <g clip-path=\"url(#clip)\">\r\n        <rect width=\"60\" height=\"30\" fill=\"#012169\"/>\r\n        <path d=\"M0,0 L60,30 M60,0 L0,30\" stroke=\"#fff\" stroke-width=\"6\"/>\r\n        <path d=\"M0,0 L60,30 M60,0 L0,30\" stroke=\"#C8102E\" stroke-width=\"4\"/>\r\n        <path d=\"M30,0 V30 M0,15 H60\" stroke=\"#fff\" stroke-width=\"10\"/>\r\n        <path d=\"M30,0 V30 M0,15 H60\" stroke=\"#C8102E\" stroke-width=\"6\"/>\r\n    </g>\r\n</svg>\r\n"));
            apperenances.Add(AddImage("ee", "<svg xmlns=\"http://www.w3.org/2000/svg\" id=\"ee\" viewBox=\"0 0 24 16\">\r\n    <rect width=\"24\" height=\"16\" fill=\"white\"/>\r\n    <rect width=\"24\" height=\"5.33\" fill=\"#0072CE\"/>\r\n    <rect y=\"5.33\" width=\"24\" height=\"5.33\" fill=\"black\"/>\r\n</svg>"));

            foreach (var it in apperenances)
            {
                var args = GXSelectArgs.SelectAll<GXAppearance>(w => w.Id == it.Id && w.ResourceType == it.ResourceType);
                var item = host.Connection.SingleOrDefault<GXAppearance>(args);
                if (item?.Id == null)
                {
                    it.Creator = admin;
                    it.CreationTime = now;
                    host.Connection.Insert(GXInsertArgs.Insert(it));
                }
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
            //Add workflow service
            services.AddSingleton<IWorkflowHandler, GXWorkflowService>();
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
            services.AddSingleton<BlockService>();
            services.AddTransient<IIpAddressRepository, IPAddressRepository>();
            services.AddTransient<IModuleRepository, ModuleRepository>();
            services.AddTransient<IModuleLogRepository, ModuleLogRepository>();
            services.AddTransient<ITaskRepository, TaskRepository>();
            services.AddTransient<IBlockRepository, BlockRepository>();
            services.AddTransient<IBlockGroupRepository, BlockGroupRepository>();
            services.AddTransient<IContentTypeRepository, ContentTypeRepository>();
            services.AddTransient<IContentTypeGroupRepository, ContentTypeGroupRepository>();
            services.AddTransient<IMenuRepository, MenuRepository>();
            services.AddTransient<IMenuGroupRepository, MenuGroupRepository>();
            services.AddTransient<IContentRepository, ContentRepository>();
            services.AddTransient<IContentGroupRepository, ContentGroupRepository>();
            services.AddSingleton<IConfigurationRepository, ConfigurationRepository>();
            services.AddTransient<IComponentViewRepository, ComponentViewRepository>();
            services.AddTransient<IComponentViewGroupRepository, ComponentViewGroupRepository>();
            services.AddTransient<IScriptRepository, ScriptRepository>();
            services.AddTransient<IScriptLogRepository, ScriptLogRepository>();
            services.AddTransient<IScriptGroupRepository, ScriptGroupRepository>();
            services.AddTransient<IDeviceActionRepository, DeviceActionRepository>();
            services.AddTransient<IDeviceTraceRepository, DeviceTraceRepository>();
            services.AddTransient<IRoleRepository, RoleRepository>();
            services.AddTransient<IUserSettingRepository, UserSettingRepository>();
            services.AddTransient<IManufacturerRepository, ManufacturerRepository>();
            services.AddTransient<IManufacturerGroupRepository, ManufacturerGroupRepository>();
            services.AddTransient<IFavoriteRepository, FavoriteRepository>();
            services.AddTransient<IKeyManagementRepository, KeyManagementRepository>();
            services.AddTransient<IKeyManagementGroupRepository, KeyManagementGroupRepository>();
            services.AddTransient<IKeyManagementLogRepository, KeyManagementLogRepository>();
            services.AddTransient<IGatewayGroupRepository, GatewayGroupRepository>();
            services.AddTransient<IGatewayRepository, GatewayRepository>();
            services.AddTransient<IGatewayLogRepository, GatewayLogRepository>();
            services.AddSingleton<IPerformanceRepository, PerformanceRepository>();
            services.AddTransient<ISubtotalGroupRepository, SubtotalGroupRepository>();
            services.AddTransient<ISubtotalRepository, SubtotalRepository>();
            services.AddTransient<ISubtotalValueRepository, SubtotalValueRepository>();
            services.AddTransient<ISubtotalLogRepository, SubtotalLogRepository>();
            services.AddTransient<IReportGroupRepository, ReportGroupRepository>();
            services.AddTransient<IReportRepository, ReportRepository>();
            services.AddTransient<IReportLogRepository, ReportLogRepository>();
            services.AddTransient<IUserStampRepository, UserStampRepository>();
            services.AddTransient<INotificationGroupRepository, NotificationGroupRepository>();
            services.AddTransient<INotificationRepository, NotificationRepository>();
            services.AddTransient<INotificationLogRepository, NotificationLogRepository>();
            services.AddTransient<IDataExchange, DataExchangeRepository>();
            services.AddTransient<IAppearanceRepository, AppearanceRepository>();
            services.AddTransient<IEnumTypeRepository, EnumTypeRepository>();
            services.AddTransient<ILocalizedResourceRepository, LocalizedResourceRepository>();
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
        internal static async Task<IGXHost> ConnectToDb(WebApplicationBuilder builder, ILogger<GXConfiguration> logger)
        {
            // Add services to the container.
            DatabaseOptions db = builder.Configuration.GetSection("Database").Get<DatabaseOptions>();
            string settings = db.Settings;
            string type = db.Type;
            bool disabled = db.Disabled;
            if (disabled)
            {
                logger.LogInformation(ServerHelpers.GetInvariantString(nameof(Properties.Resources.DatabaseServiceIsDisabled)));
            }
            else
            {
                logger.LogDebug(ServerHelpers.GetInvariantString(nameof(Properties.Resources.DatabaseType)), type);
                logger.LogDebug(ServerHelpers.GetInvariantString(nameof(Properties.Resources.Connecting)), settings);
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
                builder.Services.AddDbContextPool<ApplicationDbContext>(options =>
                    options.UseSqlServer(settings));
            }
            else if (string.Compare(type, "MySQL", true) == 0)
            {
                connection = new MySqlConnection(settings);
                connectionCount = GetConnectionPoolSize(new GXDbConnection(connection, null));

                for (int pos = 0; pos != connectionCount; ++pos)
                {
                    connections.Add(new MySqlConnection(settings));
                }
                builder.Services.AddDbContextPool<ApplicationDbContext>(options =>
                options.UseMySQL(settings));
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
                        host.Connection.CreateTable<GXUserGroup>(true, false);
                        //Add ASP.Net user management.
                        host.Connection.CreateTable<GXCluster>(true, false);
                        host.Connection.CreateTable<GXPersistedGrants>(false, false);
                        host.Connection.CreateTable<GXDeviceCodes>(false, false);
                        //  host.Connection.CreateTable<GXRole>(false, false);
                        host.Connection.CreateTable<GXRoleClaim>(false, false);
                        host.Connection.CreateTable<GXUserClaim>(false, false);
                        host.Connection.CreateTable<GXUserRole>(false, false);
                        host.Connection.CreateTable<GXUserScope>(false, false);
                        host.Connection.CreateTable<GXUserLogin>(false, false);
                        host.Connection.CreateTable<GXUserToken>(false, false);
                        host.Connection.CreateTable<GXKey>(false, false);
                        host.Connection.CreateTable<GXPerformance>(false, false);
                        host.Connection.CreateTable<GXSystemLog>(false, false);
                        host.Connection.CreateTable<GXUrlAlias>(true, false);
                        host.Connection.CreateTable<GXAppearance>(false, false);
                        host.Connection.CreateTable<GXConfiguration>(false, false);
                        host.Connection.CreateTable<GXLanguage>(true, false);
                        host.Connection.CreateTable<GXEnumType>(true, false);

                        AddDefaultConfigurationSettings(host);
                        UpdateComponentViews(host);
                        host.Connection.CommitTransaction(t);
                    }
                    catch (Exception)
                    {
                        if (t != null)
                        {
                            host.Connection.RollbackTransaction(t);
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
                    //Gateways added.
                    if (!host.Connection.TableExist<GXGatewayGroup>())
                    {
                        host.Connection.CreateTable<GXGatewayGroup>(false, false);
                        host.Connection.CreateTable<GXGateway>(false, false);
                        host.Connection.CreateTable<GXGatewayLog>(false, false);
                        host.Connection.CreateTable<GXGatewayDeviceGroup>(false, false);
                        host.Connection.CreateTable<GXGatewayDevice>(false, false);
                        host.Connection.CreateTable<GXUserGroupGatewayGroup>(false, false);
                        host.Connection.CreateTable<GXGatewayGroupGateway>(false, false);
                    }
                    //Concurrently and active properties added to schedule.
                    host.Connection.UpdateTable<GXSchedule>();
                    //Active property added to device.
                    host.Connection.UpdateTable<GXDevice>();
                    //Active property added to device group.
                    host.Connection.UpdateTable<GXDeviceGroup>();
                    //Scaler added to attribute template.
                    host.Connection.UpdateTable<GXAttributeTemplate>();
                    //Performance table added.
                    if (!host.Connection.TableExist<GXPerformance>())
                    {
                        host.Connection.CreateTable<GXPerformance>(false, false);
                        //TraceLevel added to gateway.
                        host.Connection.UpdateTable<GXGateway>();
                    }
                    if (!host.Connection.TableExist<GXSubtotal>())
                    {
                        host.Connection.CreateTable<GXSubtotal>(false, false);
                        host.Connection.CreateTable<GXSubtotalGroup>(false, false);
                        host.Connection.CreateTable<GXSubtotalGroupSubtotal>(false, false);
                        host.Connection.CreateTable<GXSubtotalDevice>(false, false);
                        host.Connection.CreateTable<GXSubtotalDeviceAttributeTemplate>(false, false);
                        host.Connection.CreateTable<GXSubtotalDeviceGroupAttributeTemplate>(false, false);
                        host.Connection.CreateTable<GXSubtotalGateway>(false, false);
                        host.Connection.CreateTable<GXSubtotalGatewayGroup>(false, false);
                        host.Connection.CreateTable<GXSubtotalAgent>(false, false);
                        host.Connection.CreateTable<GXSubtotalAgentGroup>(false, false);
                        host.Connection.CreateTable<GXSubtotalValue>(false, false);
                        host.Connection.CreateTable<GXUserGroupSubtotalGroup>(false, false);
                        host.Connection.CreateTable<GXSubtotalDeviceGroup>(false, false);
                        host.Connection.CreateTable<GXSubtotalValue>(false, false);
                        host.Connection.CreateTable<GXSubtotalLog>(false, false);
                        host.Connection.CreateTable<GXSubtotalSchedule>(false, false);
                        host.Connection.CreateTable<GXSubtotalScheduleGroup>(false, false);
                        host.Connection.CreateTable<GXSubtotalScript>(false, false);
                        host.Connection.CreateTable<GXSubtotalScriptGroup>(false, false);
                        host.Connection.CreateTable<GXSubtotalWorkflow>(false, false);
                        host.Connection.CreateTable<GXSubtotalWorkflowGroup>(false, false);
                        host.Connection.CreateTable<GXSubtotalUser>(false, false);
                        host.Connection.CreateTable<GXSubtotalUserGroup>(false, false);
                        {
                            List<GXConfiguration> configurations = new List<GXConfiguration>();
                            //All values are saved using invariant culture.
                            CultureInfo? culture = CultureInfo.DefaultThreadCurrentUICulture;
                            try
                            {
                                CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
                                AddSubtotalConfiguration(configurations);
                            }
                            finally
                            {
                                CultureInfo.DefaultThreadCurrentUICulture = culture;
                            }
                            host.Connection.Insert(GXInsertArgs.InsertRange(configurations));
                        }
                        if (!host.Connection.TableExist<GXScope>())
                        {
                            //Name, module type and protocol added to module.
                            host.Connection.UpdateTable<GXModule>();
                            //Schedule-specific module settings added.
                            host.Connection.UpdateTable<GXScheduleModule>();
                            //Condition and action scripts added to task.
                            host.Connection.UpdateTable<GXTask>();

                            //Module added to role.
                            host.Connection.UpdateTable<GXRole>();
                            //Module added to user settings.
                            host.Connection.UpdateTable<GXUserSetting>();
                            //Scope added.
                            host.Connection.CreateTable<GXScope>(false, false);
                            //Protocol property added.
                            host.Connection.UpdateTable<GXDeviceTemplate>();
                            //Schedule to object template added.
                            host.Connection.CreateTable<GXScheduleToDeviceObjectTemplate>(false, false);
                            //Schedule to attribute template added.
                            host.Connection.CreateTable<GXScheduleToDeviceAttributeTemplate>(false, false);
                            //Schedule to object template added.
                            host.Connection.CreateTable<GXScheduleToDeviceGroupObjectTemplate>(false, false);
                            //Schedule to attribute template added.
                            host.Connection.CreateTable<GXScheduleToDeviceGroupAttributeTemplate>(false, false);
                            //Parent property added.
                            host.Connection.UpdateTable<GXBlock>();
                            host.Connection.CreateTable<GXUserStamp>(false, false);
                            //Target changed from UInt64 to string.
                            host.Connection.UpdateTable<GXUserAction>();
                            //Target changed from UInt64 to string.
                            host.Connection.UpdateTable<GXPerformance>();
                            //Description added to agent.
                            host.Connection.UpdateTable<GXAgent>();
                            //Description added to gateway.
                            host.Connection.UpdateTable<GXGateway>();
                            //Description added to schedule.
                            host.Connection.UpdateTable<GXSchedule>();
                            //Type added.
                            host.Connection.UpdateTable<GXWorkflowLog>();
                            //Type added.
                            host.Connection.UpdateTable<GXUserError>();
                            //Type added.
                            host.Connection.UpdateTable<GXSystemLog>();
                            //Type added.
                            host.Connection.UpdateTable<GXScriptLog>();
                            //Type added.
                            host.Connection.UpdateTable<GXScheduleLog>();
                            //Type added.
                            host.Connection.UpdateTable<GXModuleLog>();
                            //Type added.
                            host.Connection.UpdateTable<GXGatewayLog>();
                            //Type added.
                            host.Connection.UpdateTable<GXDeviceError>();
                            //Type added.
                            host.Connection.UpdateTable<GXAgentLog>();
                            //Type added.
                            host.Connection.UpdateTable<GXKeyManagementLog>();

                            //TraceLevel added.
                            host.Connection.UpdateTable<GXKeyManagement>();
                            //TraceLevel added.
                            host.Connection.UpdateTable<GXWorkflow>();
                            //TraceLevel added.
                            host.Connection.UpdateTable<GXScript>();
                            //TraceLevel added.
                            host.Connection.UpdateTable<GXSchedule>();
                            //TraceLevel added.
                            host.Connection.UpdateTable<GXModule>();
                            //TraceLevel added.
                            host.Connection.UpdateTable<GXGateway>();
                            //TraceLevel added.
                            host.Connection.UpdateTable<GXAgent>();
                        }
                    }
                    if (!host.Connection.TableExist<GXReport>())
                    {
                        host.Connection.CreateTable<GXReport>(false, false);
                        host.Connection.CreateTable<GXReportGroup>(false, false);
                        host.Connection.CreateTable<GXReportGroupReport>(false, false);
                        host.Connection.CreateTable<GXReportDevice>(false, false);
                        host.Connection.CreateTable<GXReportDeviceAttributeTemplate>(false, false);
                        host.Connection.CreateTable<GXReportDeviceGroupAttributeTemplate>(false, false);
                        host.Connection.CreateTable<GXReportGateway>(false, false);
                        host.Connection.CreateTable<GXReportGatewayGroup>(false, false);
                        host.Connection.CreateTable<GXReportAgent>(false, false);
                        host.Connection.CreateTable<GXReportAgentGroup>(false, false);
                        host.Connection.CreateTable<GXUserGroupReportGroup>(false, false);
                        host.Connection.CreateTable<GXReportDeviceGroup>(false, false);
                        host.Connection.CreateTable<GXReportLog>(false, false);
                        host.Connection.CreateTable<GXReportSchedule>(false, false);
                        host.Connection.CreateTable<GXReportScheduleGroup>(false, false);
                        host.Connection.CreateTable<GXReportScript>(false, false);
                        host.Connection.CreateTable<GXReportScriptGroup>(false, false);
                        host.Connection.CreateTable<GXReportWorkflow>(false, false);
                        host.Connection.CreateTable<GXReportWorkflowGroup>(false, false);
                        host.Connection.CreateTable<GXReportUser>(false, false);
                        host.Connection.CreateTable<GXReportUserGroup>(false, false);
                        host.Connection.CreateTable<GXReportSubtotal>(false, false);
                        host.Connection.CreateTable<GXReportSubtotalGroup>(false, false);
                        List<GXConfiguration> configurations = new List<GXConfiguration>();
                        //All values are saved using invariant culture.
                        CultureInfo? culture = CultureInfo.DefaultThreadCurrentUICulture;
                        try
                        {
                            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
                            AddReportConfiguration(configurations);
                        }
                        finally
                        {
                            CultureInfo.DefaultThreadCurrentUICulture = culture;
                        }
                        host.Connection.Insert(GXInsertArgs.InsertRange(configurations));
                        //Last and Next updated for the subtotal.
                        host.Connection.UpdateTable<GXSubtotal>();
                        //Creator property added to Component view.
                        host.Connection.UpdateTable<GXComponentView>();
                        //Creator property added to block.
                        host.Connection.UpdateTable<GXBlock>();
                        //Creator property added to device group.
                        host.Connection.UpdateTable<GXDeviceGroup>();
                        //Creator property added to trigger.
                        host.Connection.UpdateTable<GXTrigger>();
                        //Creator property added to subtotal group.
                        host.Connection.UpdateTable<GXSubtotalGroup>();
                        //Creator property added to component view group.
                        host.Connection.UpdateTable<GXComponentViewGroup>();
                        //Creator property added to workflow.
                        host.Connection.UpdateTable<GXWorkflow>();
                        //Creator property added to workflow group.
                        host.Connection.UpdateTable<GXWorkflowGroup>();
                        //Creator property added to agent group.
                        host.Connection.UpdateTable<GXAgentGroup>();
                        //Creator property added to block group.
                        host.Connection.UpdateTable<GXBlockGroup>();
                        //Creator property added to device template group.
                        host.Connection.UpdateTable<GXDeviceTemplateGroup>();
                        //Creator property added to gateway group.
                        host.Connection.UpdateTable<GXGatewayGroup>();
                        //Creator property added to key management group.
                        host.Connection.UpdateTable<GXKeyManagementGroup>();
                        //Creator property added to management group.
                        host.Connection.UpdateTable<GXManufacturerGroup>();
                        //Creator property added to module group.
                        host.Connection.UpdateTable<GXModuleGroup>();
                        //Creator property added to report group.
                        host.Connection.UpdateTable<GXReportGroup>();
                        //Creator property added to schedule group.
                        host.Connection.UpdateTable<GXScheduleGroup>();
                        //Creator property added to script group.
                        host.Connection.UpdateTable<GXScriptGroup>();
                        //Creator property added to trigger group.
                        host.Connection.UpdateTable<GXTriggerGroup>();
                        //Creator property added to user group.
                        host.Connection.UpdateTable<GXUserGroup>();

                    }
                    //Creator property added to workflow. This can be removed.
                    host.Connection.UpdateTable<GXWorkflow>();
                    if (!host.Connection.TableExist<GXNotificationGroup>())
                    {
                        //Error property added to task.
                        host.Connection.UpdateTable<GXTask>();
                        //Creator property added to device template.
                        host.Connection.UpdateTable<GXDeviceTemplate>();
                        //Flag property added to language.
                        host.Connection.UpdateTable<GXLanguage>();
                        //Configuration localized name property added.
                        host.Connection.UpdateTable<GXConfiguration>();
                        //Creation time property added.
                        host.Connection.UpdateTable<GXDeviceTemplate>();

                        //Content properties added.
                        host.Connection.UpdateTable<GXLocalizedResource>();
                        host.Connection.CreateTable<GXMenu>(true, false);
                        host.Connection.CreateTable<GXContent>(true, false);

                        host.Connection.CreateTable<GXNotification>(true, false);
                        host.Connection.CreateTable<GXNotificationDeviceAttributeTemplate>(false, false);
                        host.Connection.CreateTable<GXNotificationDeviceGroupAttributeTemplate>(false, false);
                        List<GXConfiguration> configurations = new List<GXConfiguration>();
                        //All values are saved using invariant culture.
                        CultureInfo? culture = CultureInfo.DefaultThreadCurrentUICulture;
                        try
                        {
                            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
                            AddNotificationConfiguration(configurations);
                            AddPruneConfiguration(configurations);
                            AddDataExchangeConfiguration(configurations);
                            AddContentTypeConfiguration(configurations);
                            AddContentConfiguration(configurations);
                            AddAppearanceConfiguration(configurations);
                        }
                        finally
                        {
                            CultureInfo.DefaultThreadCurrentUICulture = culture;
                        }
                        host.Connection.Insert(GXInsertArgs.InsertRange(configurations));
                    }
                    if (!host.Connection.TableExist<GXUrlAlias>())
                    {
                        host.Connection.CreateTable<GXUrlAlias>(true, false);
                        host.Connection.CreateTable<GXUserScope>(false, false);
                        //ID changed to autoincrement.
                        host.Connection.DropTable<GXRoleClaim>(false);
                        host.Connection.CreateTable<GXRoleClaim>(false, false);
                        host.Connection.CreateTable<GXAppearance>(false, false);
                        host.Connection.CreateTable<GXEnumType>(false, false);
                        host.Connection.UpdateTable<GXUserAction>();
                        host.Connection.UpdateTable<GXMenuGroup>();
                        host.Connection.UpdateTable<GXMenu>();
                        host.Connection.CreateTable<GXMenuRole>(false, false);
                        host.Connection.UpdateTable<GXMenuLink>();
                        host.Connection.CreateTable<GXMenuLinkRole>(false, false);
                        host.Connection.UpdateTable<GXDeviceTrace>();
                    }
                    //TODO: await UpdateRoleScopes(host, builder.Services);
                }
            }
            else
            {
                host = new GXHost();
            }
            if (host.Connection != null && GetDefaultAdminUser(host) != null)
            {
                UpdateLanguages(host);
                UpdateAppereances(host);
                AddDefaultMenus(host);
                //Update shared version.
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
            arg.Where.And<GXRole>(where => where.Name == GXRoles.Admin);
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