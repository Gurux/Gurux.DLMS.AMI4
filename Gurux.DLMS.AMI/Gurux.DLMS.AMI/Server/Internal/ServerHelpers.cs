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
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Services;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm;
using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Resources;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Gurux.DLMS.AMI.Server.Internal
{
    /// <summary>
    /// Server helper methods.
    /// </summary>
    static class ServerHelpers
    {
        /// <summary>
        /// Get user ID from claims.
        /// </summary>
        /// <param name="user"></param>
        /// <returns>User ID.</returns>
        public static string GetUserId(ClaimsPrincipal user)
        {
            string? id = GetUserId(user, true);
            if (id == null)
            {
                throw new UnauthorizedAccessException();
            }
            return id;
        }

        /// <summary>
        /// Get user ID from claims.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="throwException">Exception is thrown if user is unknown.</param>
        /// <returns>User ID.</returns>
        public static string? GetUserId(ClaimsPrincipal user, bool throwException)
        {
            if (user == null)
            {
                return null;
            }
            var id = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (id == null)
            {
                if (throwException)
                {
                    throw new UnauthorizedAccessException();
                }
                return null;
            }
            return id.Value;
        }

        public static int GetHashCode(this string str)
        {
            unchecked
            {
                int hash1 = (5381 << 16) + 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1)
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }
                return hash1 + (hash2 * 1566083941);
            }
        }

        /// <summary>
        /// Create ClaimsPrincipal from the user information.
        /// </summary>
        /// <param name="user">User information.</param>
        /// <returns>Claims principal.</returns>
        public static ClaimsPrincipal CreateClaimsPrincipalFromUser(GXUser? user)
        {
            if (user == null || string.IsNullOrEmpty(user.Id))
            {
                throw new UnauthorizedAccessException();
            }
            List<Claim> claims = new List<Claim>
            {
                new Claim("sub", user.Id),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };
            if (user.Roles != null)
            {
                foreach (var role in user.Roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "Basic"));
        }

        /// <summary>
        /// Get component views from the assembly and add them to the database.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="assembly">Assebly where component views are searched for.</param>
        public static async Task<List<GXTrigger>> UpdateTriggersAsync(IGXHost host, Assembly assembly, bool activities)
        {
            List<GXTrigger> newTriggers = new();
            List<GXTrigger> updatedTriggers = new();
            foreach (Type it in assembly.GetTypes())
            {
                if (!it.IsAbstract && typeof(ITriggerAction).IsAssignableFrom(it))
                {
                    GXSelectArgs args = GXSelectArgs.SelectAll<GXTrigger>(where => where.ClassName == it.FullName);
                    if (activities)
                    {
                        args.Columns.Add<GXTriggerActivity>();
                        args.Joins.AddLeftJoin<GXTrigger, GXTriggerActivity>(j => j.Id, j => j.Trigger);
                    }
                    GXTrigger? item = host.Connection.SingleOrDefault<GXTrigger>(args);
                    ITriggerAction? tmp = (ITriggerAction?)Activator.CreateInstance(it);
                    if (tmp == null)
                    {
                        throw new ArgumentException("Unknown component " + it.FullName);
                    }
                    string? configurationUI = null;
                    if (tmp.ConfigurationUI != null)
                    {
                        configurationUI = tmp.ConfigurationUI.FullName;
                    }
                    if (item == null)
                    {
                        GXTrigger trigger = new GXTrigger()
                        {
                            ClassName = it.FullName,
                            Name = tmp.Name,
                            ConfigurationUI = configurationUI,
                            Icon = tmp.Icon,
                            Activities = new List<GXTriggerActivity>(),
                            CreationTime = DateTime.Now
                        };
                        newTriggers.Add(trigger);
                        //Get static activity names.
                        FieldInfo[] fields = it.GetFields(BindingFlags.Static | BindingFlags.Public);
                        foreach (FieldInfo fi in fields)
                        {
                            trigger.Activities.Add(new GXTriggerActivity() { Name = fi.Name });
                        }
                    }
                    else if (activities)
                    {
                        if (tmp.Name != item.Name || item.ConfigurationUI != configurationUI || tmp.Icon != item.Icon)
                        {
                            item.Name = tmp.Name;
                            item.ConfigurationUI = configurationUI;
                            item.Icon = tmp.Icon;
                            updatedTriggers.Add(item);
                        }
                        //Get static activity names.
                        List<GXTriggerActivity> added = new();
                        FieldInfo[] fields = it.GetFields(BindingFlags.Static | BindingFlags.Public);
                        foreach (FieldInfo fi in fields)
                        {
                            bool found = false;
                            foreach (var a in item.Activities)
                            {
                                if (a.Name == fi.Name)
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                            {
                                added.Add(new GXTriggerActivity() { Name = fi.Name, Trigger = item });
                            }
                        }
                        await host.Connection.InsertAsync(GXInsertArgs.InsertRange(added));
                    }

                }
            }
            await host.Connection.UpdateAsync(GXUpdateArgs.UpdateRange(updatedTriggers));
            await host.Connection.InsertAsync(GXInsertArgs.InsertRange(newTriggers));
            newTriggers.AddRange(updatedTriggers);
            return newTriggers;
        }

        /// <summary>
        /// Add distinct items to the list.
        /// </summary>
        /// <param name="self">List where items are added.</param>
        /// <param name="items">Added items.</param>
        public static void AddDistinct(this IList self, IEnumerable items)
        {
            foreach (object item in items)
            {
                if (!self.Contains(item))
                {
                    self.Add(item);
                }
            }
        }

        /// <summary>
        /// Get component views from the assembly and add them to the database.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="assembly">Assebly where component views are searched for.</param>
        public static void UpdateComponentViews(IGXHost host, Assembly assembly)
        {
            //All values are saved using invariant culture.
            CultureInfo? culture = CultureInfo.DefaultThreadCurrentUICulture;
            try
            {
                CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
                List<GXComponentView> list = new List<GXComponentView>();
                foreach (Type it in assembly.GetTypes())
                {
                    if (!it.IsAbstract && typeof(IGXComponentView).IsAssignableFrom(it))
                    {
                        GXSelectArgs args = GXSelectArgs.SelectAll<GXComponentView>(where => where.ClassName == it.FullName);
                        GXComponentView? item = host.Connection.SingleOrDefault<GXComponentView>(args);
                        if (item == null)
                        {
                            IGXComponentView tmp = (IGXComponentView)Activator.CreateInstance(it);
                            string? configurationUI = null;
                            if (tmp.ConfigurationUI != null)
                            {
                                configurationUI = tmp.ConfigurationUI.FullName;
                            }
                            list.Add(new GXComponentView() { ClassName = it.FullName, Name = tmp.Name, ConfigurationUI = configurationUI, Icon = tmp.Icon });
                        }
                    }
                }
                host.Connection.Insert(GXInsertArgs.InsertRange(list));
            }
            finally
            {
                CultureInfo.DefaultThreadCurrentUICulture = culture;
            }
        }

        internal class GXLanguageResourceArgs
        {
            public IGXHost Host;
            public IEnumerable<GXLanguage> Cultures;
            public Type Type;
            public GXConfiguration? Configuration { get; set; }
            public GXModule? Module { get; set; }
            public GXBlock? Block { get; set; }
            public GXScript? Script { get; set; }

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="host"></param>
            /// <param name="cultures"></param>
            /// <param name="type"></param>
            /// <param name="configuration"></param>
            /// <param name="module"></param>
            /// <param name="block"></param>
            /// <param name="script"></param>
            public GXLanguageResourceArgs(IGXHost host,
            IEnumerable<GXLanguage> cultures,
            Type type,
            GXConfiguration? configuration,
            GXModule? module,
            GXBlock? block,
            GXScript? script)
            {
                Host = host;
                Cultures = cultures;
                Type = type;
                Configuration = configuration;
                Module = module;
                Block = block;
                Script = script;
            }
        }

        /// <summary>
        /// Localized strings are updated.
        /// </summary>
        /// <param name="arg"></param>
        /// <returns>List from new languages.</returns>
        internal static async Task<List<GXLanguage>> UpdateLanguageResourcesAsync(GXLanguageResourceArgs arg)
        {
            List<GXLanguage> languages = new List<GXLanguage>();
            List<GXLocalizedResource> resources = new List<GXLocalizedResource>();
            ResourceManager rm = new ResourceManager(arg.Type);
            ResourceSet invariantResourceSet = rm.GetResourceSet(CultureInfo.InvariantCulture, true, true);
            if (invariantResourceSet != null)
            {
                //All values are saved using invariant culture.
                CultureInfo? culture = CultureInfo.DefaultThreadCurrentUICulture;
                try
                {
                    //Find culture resources from the database.
                    CultureInfo invariantCulture = new CultureInfo("en");
                    GXSelectArgs args = GXSelectArgs.SelectAll<GXLanguage>(w => w.Id == invariantCulture.Name);
                    GXLanguage language = arg.Host.Connection.SingleOrDefault<GXLanguage>(args);
                    if (language == null)
                    {
                        language = new GXLanguage()
                        {
                            Id = invariantCulture.Name,
                            EnglishName = invariantCulture.EnglishName,
                            NativeName = invariantCulture.NativeName,
                            Active = true,
                            Default = true
                        };
                        arg.Host.Connection.Insert(GXInsertArgs.Insert(language));
                        languages.Add(language);
                    }
                    CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
                    string name = arg.Type.Assembly.GetName().Name + ".resources.dll";
                    foreach (string file in Directory.EnumerateFiles(Path.GetDirectoryName(arg.Type.Assembly.Location), name, SearchOption.AllDirectories))
                    {
                        try
                        {
                            Assembly assembly = Assembly.LoadFrom(file);
                            name = Path.GetDirectoryName(file);
                            name = name.Substring(name.Length - 2);
                            if (arg.Cultures != null && !arg.Cultures.Where(q => q.Id == name).Any())
                            {
                                //If language is not updated.
                                continue;
                            }
                            CultureInfo ci = new CultureInfo(name);
                            rm = new ResourceManager(typeof(Properties.Resources));
                            ResourceSet? resourceSet = rm.GetResourceSet(ci, true, true);
                            //Find culture resources from the database.
                            args = GXSelectArgs.SelectAll<GXLanguage>(w => w.Id == name);
                            language = await arg.Host.Connection.SingleOrDefaultAsync<GXLanguage>(args);
                            if (language == null)
                            {
                                language = new GXLanguage()
                                {
                                    Id = ci.Name,
                                    EnglishName = ci.EnglishName,
                                    NativeName = ci.NativeName
                                };
                                arg.Host.Connection.Insert(GXInsertArgs.Insert(language));
                                languages.Add(language);
                            }
                            if (language.Active.HasValue && language.Active.Value)
                            {
                                //Only active languages are saved.
                                args = GXSelectArgs.SelectAll<GXLocalizedResource>(w => w.Language == language);
                                if (arg.Configuration != null)
                                {
                                    GXConfiguration c = arg.Configuration;
                                    args.Where.And<GXLocalizedResource>(w => w.Configuration == c);
                                }
                                else if (arg.Module != null)
                                {
                                    GXModule mod = arg.Module;
                                    args.Where.And<GXLocalizedResource>(w => w.Module == mod);
                                }
                                else if (arg.Block != null)
                                {
                                    GXBlock bock = arg.Block;
                                    args.Where.And<GXLocalizedResource>(w => w.Block == bock);
                                }
                                else if (arg.Script != null)
                                {
                                    GXScript script = arg.Script;
                                    args.Where.And<GXLocalizedResource>(w => w.Script == script);
                                }
                                language.Resources = (await arg.Host.Connection.SelectAsync<GXLocalizedResource>(args)).ToArray();
                                foreach (DictionaryEntry entry in resourceSet)
                                {
                                    //Invariant culture is saved as resource key.
                                    int resourceKey = ServerHelpers.GetHashCode(invariantResourceSet.GetString(Convert.ToString(entry.Key)));
                                    if (language.Resources == null || !language.Resources.Where(q => q.Hash == resourceKey).Any())
                                    {
                                        resources.Add(new GXLocalizedResource()
                                        {
                                            Hash = resourceKey,
                                            Value = Convert.ToString(entry.Value),
                                            Language = language,
                                            Module = arg.Module,
                                            Block = arg.Block,
                                            Configuration = arg.Configuration,
                                            Script = arg.Script,
                                        });
                                    }
                                }
                                if (resources.Count != 0)
                                {
                                    //Resources are added one by one because there might be duplicate values
                                    //and they are ignored.
                                    foreach (GXLocalizedResource it in resources)
                                    {
                                        try
                                        {
                                            await arg.Host.Connection.InsertAsync(GXInsertArgs.Insert(it));
                                        }
                                        catch (Exception ex)
                                        {
                                            var error = new GXSystemLog()
                                            {
                                                CreationTime = DateTime.Now,
                                                Message = Properties.Resources.LocalizedResourceDublicate +
                                                it.Id + Environment.NewLine + ex.Message,
                                            };
                                        }
                                    }
                                    resources.Clear();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            var error = new GXSystemLog()
                            {
                                CreationTime = DateTime.Now,
                                Message = Properties.Resources.SystemException + ex.Message,
                            };
                            await arg.Host.Connection.InsertAsync(GXInsertArgs.Insert(error));
                        }
                    }
                }
                finally
                {
                    CultureInfo.DefaultThreadCurrentUICulture = culture;
                }
            }
            return languages;
        }

        public static void AddHostedService<TService, TImplementation>(this IServiceCollection services)
   where TService : class
   where TImplementation : class, IHostedService, TService
        {
            services.AddSingleton<TService, TImplementation>();
            services.AddHostedService<HostedServiceWrapper<TService>>();
        }

        private class HostedServiceWrapper<TService> : IHostedService
        {
            private readonly IHostedService _hostedService;

            public HostedServiceWrapper(TService hostedService)
            {
                _hostedService = (IHostedService)hostedService;
            }

            public Task StartAsync(CancellationToken cancellationToken)
            {
                return _hostedService.StartAsync(cancellationToken);
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                return _hostedService.StopAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Add events service.
        /// </summary>
        /// <param name="services"></param>
        public static void AddEventService(this IServiceCollection services)
        {
            services.AddSingleton<IGXEventsListener, GXEventService>();
            services.AddTransient<IGXEventsNotifier, EventServiceWrapper>();
        }

        private class EventServiceWrapper : IGXEventsNotifier
        {
            private readonly GXEventService _hostedService;

            /// <summary>
            /// Constructor.
            /// </summary>
            public EventServiceWrapper(IGXEventsListener hostedService)
            {
                _hostedService = (GXEventService)hostedService;
            }


            /// <inheritdoc/>
            public Task AddAgentLogs(IReadOnlyList<string> users, IEnumerable<GXAgentLog> agents)
            {
                return _hostedService.AddAgentLogs(users, agents);
            }

            /// <inheritdoc/>
            public Task AddDeviceErrors(IReadOnlyList<string> users, IEnumerable<GXDeviceError> errors)
            {
                return _hostedService.AddDeviceErrors(users, errors);
            }

            /// <inheritdoc/>
            public Task AddModuleLogs(IReadOnlyList<string> users, IEnumerable<GXModuleLog> errors)
            {
                return _hostedService.AddModuleLogs(users, errors);
            }

            /// <inheritdoc/>
            public Task AddScheduleLog(IReadOnlyList<string> users, IEnumerable<GXScheduleLog> logs)
            {
                return _hostedService.AddScheduleLog(users, logs);
            }

            /// <inheritdoc/>
            public Task AddScriptLogs(IReadOnlyList<string> users, IEnumerable<GXScriptLog> errors)
            {
                return _hostedService.AddScriptLogs(users, errors);
            }

            /// <inheritdoc/>
            public Task AddSystemLogs(IReadOnlyList<string> users, IEnumerable<GXSystemLog> errors)
            {
                return _hostedService.AddSystemLogs(users, errors);
            }

            /// <inheritdoc/>
            public Task AddUserErrors(IReadOnlyList<string> users, IEnumerable<GXUserError> item)
            {
                return _hostedService.AddUserErrors(users, item);
            }

            /// <inheritdoc/>
            public Task AddWorkflowLogs(IReadOnlyList<string> users, IEnumerable<GXWorkflowLog> item)
            {
                return _hostedService.AddWorkflowLogs(users, item);
            }

            /// <inheritdoc/>
            public Task AgentDelete(IReadOnlyList<string> users, IEnumerable<GXAgent> agents)
            {
                return _hostedService.AgentDelete(users, agents);
            }

            /// <inheritdoc/>
            public Task AgentGroupDelete(IReadOnlyList<string> users, IEnumerable<GXAgentGroup> groups)
            {
                return _hostedService.AgentGroupDelete(users, groups);
            }

            /// <inheritdoc/>
            public Task AgentGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXAgentGroup> groups)
            {
                return _hostedService.AgentGroupUpdate(users, groups);
            }

            /// <inheritdoc/>
            public Task AgentStatusChange(IReadOnlyList<string> users, IEnumerable<GXAgent> agents)
            {
                return _hostedService.AgentStatusChange(users, agents);
            }

            /// <inheritdoc/>
            public Task AgentUpdate(IReadOnlyList<string> users, IEnumerable<GXAgent> agents)
            {
                return _hostedService.AgentUpdate(users, agents);
            }

            /// <inheritdoc/>
            public Task AttributeDelete(IReadOnlyList<string> users, IEnumerable<GXAttribute> attributes)
            {
                return _hostedService.AttributeDelete(users, attributes);
            }

            /// <inheritdoc/>
            public Task AttributeUpdate(IReadOnlyList<string> users, IEnumerable<GXAttribute> attributes)
            {
                return _hostedService.AttributeUpdate(users, attributes);
            }

            /// <inheritdoc/>
            public Task ValueUpdate(IReadOnlyList<string> users, IEnumerable<GXAttribute> attributes)
            {
                return _hostedService.ValueUpdate(users, attributes);
            }

            /// <inheritdoc/>
            public Task BlockClose(IReadOnlyList<string> users, IEnumerable<GXBlock> blocks)
            {
                return _hostedService.BlockClose(users, blocks);
            }

            /// <inheritdoc/>
            public Task BlockDelete(IReadOnlyList<string> users, IEnumerable<GXBlock> blocks)
            {
                return _hostedService.BlockDelete(users, blocks);
            }

            /// <inheritdoc/>
            public Task BlockGroupDelete(IReadOnlyList<string> users, IEnumerable<GXBlockGroup> groups)
            {
                return _hostedService.BlockGroupDelete(users, groups);
            }

            /// <inheritdoc/>
            public Task BlockGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXBlockGroup> groups)
            {
                return _hostedService.BlockGroupUpdate(users, groups);
            }

            /// <inheritdoc/>
            public Task BlockUpdate(IReadOnlyList<string> users, IEnumerable<GXBlock> blocks)
            {
                return _hostedService.BlockUpdate(users, blocks);
            }

            /// <inheritdoc/>
            public Task ClearAgentLogs(IReadOnlyList<string> users, IEnumerable<GXAgent>? agents)
            {
                return _hostedService.ClearAgentLogs(users, agents);
            }

            /// <inheritdoc/>
            public Task ClearDeviceErrors(IReadOnlyList<string> users, IEnumerable<GXDevice>? devices)
            {
                return _hostedService.ClearDeviceErrors(users, devices);
            }

            /// <inheritdoc/>
            public Task ClearModuleLogs(IReadOnlyList<string> users, IEnumerable<GXModule>? modules)
            {
                return _hostedService.ClearModuleLogs(users, modules);
            }

            /// <inheritdoc/>
            public Task ClearScheduleLog(IReadOnlyList<string> users, IEnumerable<GXSchedule>? schedules)
            {
                return _hostedService.ClearScheduleLog(users, schedules);
            }

            /// <inheritdoc/>
            public Task ClearScriptLogs(IReadOnlyList<string> users, IEnumerable<GXScript>? scripts)
            {
                return _hostedService.ClearScriptLogs(users, scripts);
            }

            /// <inheritdoc/>
            public Task ClearSystemLogs(IReadOnlyList<string> users)
            {
                return _hostedService.ClearSystemLogs(users);
            }

            /// <inheritdoc/>
            public Task ClearUserErrors(IReadOnlyList<string> users, IEnumerable<GXUser>? Users)
            {
                return _hostedService.ClearUserErrors(users, Users);
            }

            /// <inheritdoc/>
            public Task ClearWorkflowLogs(IReadOnlyList<string> users, IEnumerable<GXWorkflow>? workflows)
            {
                return _hostedService.ClearWorkflowLogs(users, workflows);
            }

            /// <inheritdoc/>
            public Task CloseAgentLogs(IReadOnlyList<string> users, IEnumerable<GXAgentLog> agents)
            {
                return _hostedService.CloseAgentLogs(users, agents);
            }

            /// <inheritdoc/>
            public Task CloseDeviceErrors(IReadOnlyList<string> users, IEnumerable<GXDeviceError> errors)
            {
                return _hostedService.CloseDeviceErrors(users, errors);
            }

            /// <inheritdoc/>
            public Task CloseModuleLogs(IReadOnlyList<string> users, IEnumerable<GXModuleLog> errors)
            {
                return _hostedService.CloseModuleLogs(users, errors);
            }

            /// <inheritdoc/>
            public Task CloseScheduleLog(IReadOnlyList<string> users, IEnumerable<GXScheduleLog> logs)
            {
                return _hostedService.CloseScheduleLog(users, logs);
            }

            /// <inheritdoc/>
            public Task CloseScriptLogs(IReadOnlyList<string> users, IEnumerable<GXScriptLog> errors)
            {
                return _hostedService.CloseScriptLogs(users, errors);
            }

            /// <inheritdoc/>
            public Task CloseSystemLogs(IReadOnlyList<string> users, IEnumerable<GXSystemLog> errors)
            {
                return _hostedService.CloseSystemLogs(users, errors);
            }

            /// <inheritdoc/>
            public Task CloseUserErrors(IReadOnlyList<string> users, IEnumerable<GXUserError> errors)
            {
                return _hostedService.CloseUserErrors(users, errors);
            }

            /// <inheritdoc/>
            public Task CloseWorkflowLogs(IReadOnlyList<string> users, IEnumerable<GXWorkflowLog> item)
            {
                return _hostedService.CloseWorkflowLogs(users, item);
            }

            /// <inheritdoc/>
            public Task ComponentViewDelete(IReadOnlyList<string> users, IEnumerable<GXComponentView> componentViews)
            {
                return _hostedService.ComponentViewDelete(users, componentViews);
            }

            /// <inheritdoc/>
            public Task ComponentViewGroupDelete(IReadOnlyList<string> users, IEnumerable<GXComponentViewGroup> groups)
            {
                return _hostedService.ComponentViewGroupDelete(users, groups);
            }

            /// <inheritdoc/>
            public Task ComponentViewGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXComponentViewGroup> groups)
            {
                return _hostedService.ComponentViewGroupUpdate(users, groups);
            }

            /// <inheritdoc/>
            public Task ComponentViewUpdate(IReadOnlyList<string> users, IEnumerable<GXComponentView> componentViews)
            {
                return _hostedService.ComponentViewUpdate(users, componentViews);
            }

            /// <inheritdoc/>
            public Task ConfigurationSave(IReadOnlyList<string> users, IEnumerable<GXConfiguration> configurations)
            {
                return _hostedService.ConfigurationSave(users, configurations);
            }

            /// <inheritdoc/>
            public Task DeviceActionAdd(IReadOnlyList<string> users, IEnumerable<GXDeviceAction> deviceActions)
            {
                return _hostedService.DeviceActionAdd(users, deviceActions);
            }

            /// <inheritdoc/>
            public Task DeviceActionsClear(IReadOnlyList<string> users, IEnumerable<GXDevice>? devices)
            {
                return _hostedService.DeviceActionsClear(users, devices);
            }

            /// <inheritdoc/>
            public Task DeviceDelete(IReadOnlyList<string> users, IEnumerable<GXDevice> devices)
            {
                return _hostedService.DeviceDelete(users, devices);
            }

            /// <inheritdoc/>
            public Task DeviceGroupDelete(IReadOnlyList<string> users, IEnumerable<GXDeviceGroup> groups)
            {
                return _hostedService.DeviceGroupDelete(users, groups);
            }

            /// <inheritdoc/>
            public Task DeviceGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXDeviceGroup> groups)
            {
                return _hostedService.DeviceGroupUpdate(users, groups);
            }

            /// <inheritdoc/>
            public Task DeviceTemplateDelete(IReadOnlyList<string> users, IEnumerable<GXDeviceTemplate> templates)
            {
                return _hostedService.DeviceTemplateDelete(users, templates);
            }

            /// <inheritdoc/>
            public Task DeviceTemplateGroupDelete(IReadOnlyList<string> users, IEnumerable<GXDeviceTemplateGroup> groups)
            {
                return _hostedService.DeviceTemplateGroupDelete(users, groups);
            }

            /// <inheritdoc/>
            public Task DeviceTemplateGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXDeviceTemplateGroup> groups)
            {
                return _hostedService.DeviceTemplateGroupUpdate(users, groups);
            }

            /// <inheritdoc/>
            public Task DeviceTemplateUpdate(IReadOnlyList<string> users, IEnumerable<GXDeviceTemplate> templates)
            {
                return _hostedService.DeviceTemplateUpdate(users, templates);
            }

            /// <inheritdoc/>
            public Task DeviceTraceAdd(IReadOnlyList<string> users, IEnumerable<GXDeviceTrace> deviceTraces)
            {
                return _hostedService.DeviceTraceAdd(users, deviceTraces);
            }

            /// <inheritdoc/>
            public Task DeviceTraceClear(IReadOnlyList<string> users, IEnumerable<GXDevice>? devices)
            {
                return _hostedService.DeviceTraceClear(users, devices);
            }

            /// <inheritdoc/>
            public Task DeviceUpdate(IReadOnlyList<string> users, IEnumerable<GXDevice> devices)
            {
                return _hostedService.DeviceUpdate(users, devices);
            }

            /// <inheritdoc/>
            public Task LanguageUpdate(IReadOnlyList<string> users, IEnumerable<GXLanguage> language)
            {
                return _hostedService.LanguageUpdate(users, language);
            }

            /// <inheritdoc/>
            public Task ModuleDelete(IReadOnlyList<string> users, IEnumerable<GXModule> modules)
            {
                return _hostedService.ModuleDelete(users, modules);
            }

            /// <inheritdoc/>
            public Task ModuleGroupDelete(IReadOnlyList<string> users, IEnumerable<GXModuleGroup> groups)
            {
                return _hostedService.ModuleGroupDelete(users, groups);
            }

            /// <inheritdoc/>
            public Task ModuleGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXModuleGroup> groups)
            {
                return _hostedService.ModuleGroupUpdate(users, groups);
            }

            /// <inheritdoc/>
            public Task ModuleSettingsSave(IReadOnlyList<string> users, GXModule module)
            {
                return _hostedService.ModuleSettingsSave(users, module);
            }

            /// <inheritdoc/>
            public Task ModuleUpdate(IReadOnlyList<string> users, IEnumerable<GXModule> modules)
            {
                return _hostedService.ModuleUpdate(users, modules);
            }

            /// <inheritdoc/>
            public Task ObjectDelete(IReadOnlyList<string> users, IEnumerable<GXObject> objects)
            {
                return _hostedService.ObjectDelete(users, objects);
            }

            /// <inheritdoc/>
            public Task ObjectTemplateDelete(IReadOnlyList<string> users, IEnumerable<GXObjectTemplate> templates)
            {
                return _hostedService.ObjectTemplateDelete(users, templates);
            }

            /// <inheritdoc/>
            public Task ObjectTemplateUpdate(IReadOnlyList<string> users, IEnumerable<GXObjectTemplate> templates)
            {
                return _hostedService.ObjectTemplateUpdate(users, templates);
            }

            /// <inheritdoc/>
            public Task AttributeTemplateDelete(IReadOnlyList<string> users, IEnumerable<GXAttributeTemplate> templates)
            {
                return _hostedService.AttributeTemplateDelete(users, templates);
            }

            /// <inheritdoc/>
            public Task AttributeTemplateUpdate(IReadOnlyList<string> users, IEnumerable<GXAttributeTemplate> templates)
            {
                return _hostedService.AttributeTemplateUpdate(users, templates);
            }

            /// <inheritdoc/>
            public Task ObjectUpdate(IReadOnlyList<string> users, IEnumerable<GXObject> objects)
            {
                return _hostedService.ObjectUpdate(users, objects);
            }

            /// <inheritdoc/>
            public Task RestStatisticAdd(IReadOnlyList<string> users, IEnumerable<GXRestStatistic> statistics)
            {
                return _hostedService.RestStatisticAdd(users, statistics);
            }

            /// <inheritdoc/>
            public Task RestStatisticClear(IReadOnlyList<string> users, IEnumerable<GXUser>? Users)
            {
                return _hostedService.RestStatisticClear(users, Users);
            }

            /// <inheritdoc/>
            public Task RoleDelete(IReadOnlyList<string> users, IEnumerable<string> roles)
            {
                return _hostedService.RoleDelete(users, roles);
            }

            /// <inheritdoc/>
            public Task RoleUpdate(IReadOnlyList<string> users, IEnumerable<GXRole> roles)
            {
                return _hostedService.RoleUpdate(users, roles);
            }

            /// <inheritdoc/>
            public Task ScheduleDelete(IReadOnlyList<string> users, IEnumerable<GXSchedule> schedules)
            {
                return _hostedService.ScheduleDelete(users, schedules);
            }

            /// <inheritdoc/>
            public Task ScheduleStart(IReadOnlyList<string> users, IEnumerable<GXSchedule> schedules)
            {
                return _hostedService.ScheduleStart(users, schedules);
            }

            /// <inheritdoc/>
            public Task ScheduleCompleate(IReadOnlyList<string> users, IEnumerable<GXSchedule> schedules)
            {
                return _hostedService.ScheduleCompleate(users, schedules);
            }

            /// <inheritdoc/>
            public Task ScheduleGroupDelete(IReadOnlyList<string> users, IEnumerable<GXScheduleGroup> groups)
            {
                return _hostedService.ScheduleGroupDelete(users, groups);
            }

            /// <inheritdoc/>
            public Task ScheduleGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXScheduleGroup> groups)
            {
                return _hostedService.ScheduleGroupUpdate(users, groups);
            }

            /// <inheritdoc/>
            public Task ScheduleUpdate(IReadOnlyList<string> users, IEnumerable<GXSchedule> schedules)
            {
                return _hostedService.ScheduleUpdate(users, schedules);
            }

            /// <inheritdoc/>
            public Task ScriptDelete(IReadOnlyList<string> users, IEnumerable<GXScript> scripts)
            {
                return _hostedService.ScriptDelete(users, scripts);
            }

            /// <inheritdoc/>
            public Task ScriptGroupDelete(IReadOnlyList<string> users, IEnumerable<GXScriptGroup> groups)
            {
                return _hostedService.ScriptGroupDelete(users, groups);
            }

            /// <inheritdoc/>
            public Task ScriptGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXScriptGroup> groups)
            {
                return _hostedService.ScriptGroupUpdate(users, groups);
            }

            /// <inheritdoc/>
            public Task ScriptUpdate(IReadOnlyList<string> users, IEnumerable<GXScript> scripts)
            {
                return _hostedService.ScriptUpdate(users, scripts);
            }

            /// <inheritdoc/>
            public Task TaskAdd(IReadOnlyList<string> users, IEnumerable<GXTask> tasks)
            {
                return _hostedService.TaskAdd(users, tasks);
            }

            /// <inheritdoc/>
            public Task TaskDelete(IReadOnlyList<string> users, IEnumerable<GXTask> tasks)
            {
                return _hostedService.TaskDelete(users, tasks);
            }

            /// <inheritdoc/>
            public Task TaskUpdate(IReadOnlyList<string> users, IEnumerable<GXTask> tasks)
            {
                return _hostedService.TaskUpdate(users, tasks);
            }

            /// <inheritdoc/>
            public Task TaskClear(IReadOnlyList<string> users, IEnumerable<GXUser>? items)
            {
                return _hostedService.TaskClear(users, items);
            }

            /// <inheritdoc/>
            public Task TriggerDelete(IReadOnlyList<string> users, IEnumerable<GXTrigger> triggers)
            {
                return _hostedService.TriggerDelete(users, triggers);
            }

            /// <inheritdoc/>
            public Task TriggerGroupDelete(IReadOnlyList<string> users, IEnumerable<GXTriggerGroup> groups)
            {
                return _hostedService.TriggerGroupDelete(users, groups);
            }

            /// <inheritdoc/>
            public Task TriggerGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXTriggerGroup> groups)
            {
                return _hostedService.TriggerGroupUpdate(users, groups);
            }

            /// <inheritdoc/>
            public Task TriggerUpdate(IReadOnlyList<string> users, IEnumerable<GXTrigger> triggers)
            {
                return _hostedService.TriggerUpdate(users, triggers);
            }

            /// <inheritdoc/>
            public Task UserActionAdd(IReadOnlyList<string> users, IEnumerable<GXUserAction> userActions)
            {
                return _hostedService.UserActionAdd(users, userActions);
            }

            /// <inheritdoc/>
            public Task UserActionDelete(IReadOnlyList<string> users, IEnumerable<GXUserAction> userActions)
            {
                return _hostedService.UserActionDelete(users, userActions);
            }

            /// <inheritdoc/>
            public Task UserActionsClear(IReadOnlyList<string> users, IEnumerable<GXUser>? Users)
            {
                return _hostedService.UserActionsClear(users, Users);
            }

            /// <inheritdoc/>
            public Task UserDelete(IReadOnlyList<string> users, IEnumerable<GXUser> Users)
            {
                return _hostedService.UserDelete(users, Users);
            }

            /// <inheritdoc/>
            public Task UserGroupDelete(IReadOnlyList<string> users, IEnumerable<GXUserGroup> groups)
            {
                return _hostedService.UserGroupDelete(users, groups);
            }

            /// <inheritdoc/>
            public Task UserGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXUserGroup> groups)
            {
                return _hostedService.UserGroupUpdate(users, groups);
            }

            /// <inheritdoc/>
            public Task UserUpdate(IReadOnlyList<string> users, IEnumerable<GXUser> Users)
            {
                return _hostedService.UserUpdate(users, Users);
            }

            /// <inheritdoc/>
            public Task WorkflowDelete(IReadOnlyList<string> users, IEnumerable<GXWorkflow> workflows)
            {
                return _hostedService.WorkflowDelete(users, workflows);
            }

            /// <inheritdoc/>
            public Task WorkflowGroupDelete(IReadOnlyList<string> users, IEnumerable<GXWorkflowGroup> groups)
            {
                return _hostedService.WorkflowGroupDelete(users, groups);
            }

            /// <inheritdoc/>
            public Task WorkflowGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXWorkflowGroup> groups)
            {
                return _hostedService.WorkflowGroupUpdate(users, groups);
            }

            /// <inheritdoc/>
            public Task WorkflowUpdate(IReadOnlyList<string> users, IEnumerable<GXWorkflow> workflows)
            {
                return _hostedService.WorkflowUpdate(users, workflows);
            }

            /// <inheritdoc/>
            public Task CronStart(IReadOnlyList<string> users)
            {
                return _hostedService.CronStart(users);
            }

            /// <inheritdoc/>
            public Task CronCompleate(IReadOnlyList<string> users)
            {
                return _hostedService.CronCompleate(users);
            }

            /// <inheritdoc/>
            public Task UserSettingUpdate(IReadOnlyList<string> users, IEnumerable<GXUserSetting> settings)
            {
                return _hostedService.UserSettingUpdate(users, settings);
            }

            /// <inheritdoc/>
            public Task UserSettingDelete(IReadOnlyList<string> users, IEnumerable<GXUserSetting> settings)
            {
                return _hostedService.UserSettingDelete(users, settings);
            }

            /// <summary>
            /// New manufacturer is added or modified.
            /// </summary>
            /// <param name="users">Notified users.</param>
            /// <param name="manufacturers">Updated manufacturers.</param>
            public Task ManufacturerUpdate(IReadOnlyList<string> users, IEnumerable<GXManufacturer> manufacturers)
            {
                return _hostedService.ManufacturerUpdate(users, manufacturers);
            }

            /// <summary>
            /// Manufacturer is deleted.
            /// </summary>
            /// <param name="users">Notified users.</param>
            /// <param name="manufacturers">Deleted manufacturers.</param>
            public Task ManufacturerDelete(IReadOnlyList<string> users, IEnumerable<GXManufacturer> manufacturers)
            {
                return _hostedService.ManufacturerDelete(users, manufacturers);
            }

            /// <summary>
            /// New manufacturer group is added or modified.
            /// </summary>
            /// <param name="users">Notified users.</param>
            /// <param name="groups">Updated manufacturer groups.</param>
            public Task ManufacturerGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXManufacturerGroup> groups)
            {
                return _hostedService.ManufacturerGroupUpdate(users, groups);
            }
            /// <summary>
            /// Manufacturer group is deleted.
            /// </summary>
            /// <param name="users">Notified users.</param>
            /// <param name="groups">Deleted manufacturer groups.</param>
            public Task ManufacturerGroupDelete(IReadOnlyList<string> users, IEnumerable<GXManufacturerGroup> groups)
            {
                return _hostedService.ManufacturerGroupDelete(users, groups);
            }

            /// <summary>
            /// New favorite is added or modified.
            /// </summary>
            /// <param name="users">Notified users.</param>
            /// <param name="favorites">Updated favorites.</param>
            public Task FavoriteUpdate(IReadOnlyList<string> users, IEnumerable<GXFavorite> favorites)
            {
                return _hostedService.FavoriteUpdate(users, favorites);
            }

            /// <summary>
            /// Favorite is deleted.
            /// </summary>
            /// <param name="users">Notified users.</param>
            /// <param name="favorites">Deleted favorites.</param>
            public Task FavoriteDelete(IReadOnlyList<string> users, IEnumerable<GXFavorite> favorites)
            {
                return _hostedService.FavoriteDelete(users, favorites);
            }

            /// <summary>
            /// New key management is added or modified.
            /// </summary>
            /// <param name="users">Notified users.</param>
            /// <param name="keys">Updated key managements.</param>
            public Task KeyManagementUpdate(IReadOnlyList<string> users, IEnumerable<GXKeyManagement> keys)
            {
                return _hostedService.KeyManagementUpdate(users, keys);
            }

            /// <summary>
            /// KeyManagement is deleted.
            /// </summary>
            /// <param name="users">Notified users.</param>
            /// <param name="keys">Deleted key managements.</param>
            public Task KeyManagementDelete(IReadOnlyList<string> users, IEnumerable<GXKeyManagement> keys)
            {
                return _hostedService.KeyManagementDelete(users, keys);
            }
            /// <summary>
            /// New key management group is added or modified.
            /// </summary>
            /// <param name="users">Notified users.</param>
            /// <param name="groups">Updated key management groups.</param>
            public Task KeyManagementGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXKeyManagementGroup> groups)
            {
                return _hostedService.KeyManagementGroupUpdate(users, groups);
            }
            /// <summary>
            /// KeyManagement group is deleted.
            /// </summary>
            /// <param name="users">Notified users.</param>
            /// <param name="groups">Deleted key management groups.</param>
            public Task KeyManagementGroupDelete(IReadOnlyList<string> users, IEnumerable<GXKeyManagementGroup> groups)
            {
                return _hostedService.KeyManagementGroupDelete(users, groups);
            }

            /// <inheritdoc/>
            public Task AddKeyManagementLogs(IReadOnlyList<string> users, IEnumerable<GXKeyManagementLog> errors)
            {
                return _hostedService.AddKeyManagementLogs(users, errors);
            }

            /// <inheritdoc/>
            public Task ClearKeyManagementLogs(IReadOnlyList<string> users, IEnumerable<GXKeyManagement>? keys)
            {
                return _hostedService.ClearKeyManagementLogs(users, keys);
            }

            /// <inheritdoc/>
            public Task CloseKeyManagementLogs(IReadOnlyList<string> users, IEnumerable<GXKeyManagementLog> errors)
            {
                return _hostedService.CloseKeyManagementLogs(users, errors);
            }

            /// <inheritdoc/>
            public Task ClearCache(IReadOnlyList<string> users, Guid id, string[] names)
            {
                //Only agent needs this.
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Check if expression contains given parameter.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool Contains(Expression? expression, string name)
        {
            // if (expression is MemberExpression)
            if (expression is LambdaExpression lambdaEx)
            {
                if (lambdaEx.Body is MemberExpression me)
                {
                    return me.Member.Name == name;
                }
                if (lambdaEx.Body is NewExpression ne)
                {
                    foreach (var it in ne.Arguments)
                    {
                        if ((it is MemberExpression me2) && me2.Member.Name == name)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
