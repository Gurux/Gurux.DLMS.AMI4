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
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Properties;
using Gurux.DLMS.AMI.Services;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.Content;
using Gurux.DLMS.AMI.Shared.DTOs.ContentType;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
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
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Resources;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization.Metadata;

namespace Gurux.DLMS.AMI.Server.Internal
{
    /// <summary>
    /// Server helper methods.
    /// </summary>
    static class ServerHelpers
    {
        /// <summary>
        /// Retrieves the culture-invariant resource string associated with the specified key, or returns the key itself
        /// if no resource is found.
        /// </summary>
        /// <remarks>This method uses the invariant culture to retrieve resource strings, ensuring
        /// consistent results regardless of the current thread's culture settings.</remarks>
        /// <param name="value">The resource key to look up. Cannot be null.</param>
        /// <returns>The culture-invariant resource string corresponding to the specified key, or the key itself if no matching
        /// resource is found.</returns>
        public static string GetInvariantString(this string value)
        {
            string? tmp = Resources.ResourceManager.GetString(value, CultureInfo.InvariantCulture);
            if (tmp == null)
            {
                tmp = value;
            }
            return tmp;
        }

        /// <summary>
        /// Get remote IP address.
        /// </summary>
        /// <param name="req">HttpRequest.</param>
        /// <param name="http">HttpContext.</param>
        public static string? GetRemoteIpAddress(HttpRequest req, HttpContext http)
        {
            string? ip = req.Headers["X-Forwarded-For"]
                            .FirstOrDefault()?
                            .Split(',')[0]
                            .Trim();
            if (string.IsNullOrEmpty(ip))
            {
                ip = http.Connection.RemoteIpAddress?.ToString();
            }
            return ip;
        }

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
            }
            return id?.Value;
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
                    if (!it.IsAbstract && typeof(IAmiComponent).IsAssignableFrom(it))
                    {
                        GXSelectArgs args = GXSelectArgs.SelectAll<GXComponentView>(where => where.ClassName == it.FullName);
                        GXComponentView? item = host.Connection.SingleOrDefault<GXComponentView>(args);
                        if (item == null)
                        {
                            IAmiComponent tmp = (IAmiComponent)Activator.CreateInstance(it);
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

        /// <summary>
        /// Update localized strings.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="enumTypeRepository">Enum type repository</param>
        /// <param name="languages">Updated languages</param>
        /// <param name="creator">Creator</param>
        internal static async Task<List<GXLanguage>> UpdateLanguageResourcesAsync(
            IGXHost host,
            IEnumTypeRepository enumTypeRepository,
            IEnumerable<GXLanguage>? languages,
            GXUser creator)
        {
            await UpdateLanguageResourcesAsync(host, enumTypeRepository, typeof(AccountSettings), languages, creator);
            return await UpdateLanguageResourcesAsync(host, enumTypeRepository, typeof(Properties.Resources), languages, creator);
        }

        /// <summary>
        /// Update localized strings.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="enumTypeRepository">Enum type repository</param>
        /// <param name="type"></param>
        /// <param name="languages">Updated languages</param>
        /// <param name="creator">Creator</param>
        private static async Task<List<GXLanguage>> UpdateLanguageResourcesAsync(
            IGXHost host,
            IEnumTypeRepository enumTypeRepository,
            Type type,
            IEnumerable<GXLanguage>? languages,
            GXUser creator)
        {
            List<GXLanguage> updated = new List<GXLanguage>();
            List<GXLocalizedResource> strings = new List<GXLocalizedResource>();
            List<GXAppearance> images = new List<GXAppearance>();
            GXSelectArgs args;
            //Update activated languages.
            args = GXSelectArgs.SelectAll<GXLanguage>(w => w.Active == true);
            if (languages?.Any() == true)
            {
                var ids = languages.Select(s => s.Id).ToList();
                args.Where.And<GXLanguage>(w => ids.Contains(w.Id));
            }
            languages = host.Connection.Select<GXLanguage>(args).ToArray();
            DateTime now = DateTime.Now;
            string name = type.Assembly.GetName().Name + ".Properties.Resources";
            //Update resources.
            ResourceManager rm = new ResourceManager(name, type.Assembly);
            try
            {
                ResourceSet? invariantResourceSet = rm.GetResourceSet(CultureInfo.InvariantCulture, true, true);
                foreach (var lang in languages)
                {

                    CultureInfo culture;
                    if (lang.Id == "en")
                    {
                        culture = CultureInfo.InvariantCulture;
                    }
                    else
                    {
                        culture = CultureInfo.GetCultureInfo(lang.Id);
                    }
                    ResourceSet? localResourceSet = rm.GetResourceSet(culture, true, false);
                    if (invariantResourceSet != null && localResourceSet != null)
                    {
                        try
                        {
                            foreach (DictionaryEntry it in invariantResourceSet)
                            {
                                if (it.Key is string id)
                                {
                                    if (it.Value is string value)
                                    {                                    
                                        string? localString = localResourceSet.GetString(id);
                                        string status = "Success";
                                        if (localString == null)
                                        {
                                            status = "Localized value missing";
                                            localString = value;
                                        }
                                        //Add new localized strings.
                                        string hash = ClientHelpers.GetHashCode(value);
                                        args = GXSelectArgs.SelectAll<GXLocalizedResource>(w => w.Hash == hash);
                                        args.Joins.AddLeftJoin<GXLocalizedResource, GXLanguage>(j => j.Language, j => j.Id);
                                        args.Where.And<GXLanguage>(w => w.Id == lang.Id);
                                        GXLocalizedResource res = host.Connection.SingleOrDefault<GXLocalizedResource>(args);
                                        if (res == null)
                                        {
                                            res = new GXLocalizedResource()
                                            {
                                                Status = await enumTypeRepository.GetLogTypeAsync(TargetType.LocalizedResource, status),
                                                Hash = hash,
                                                Value = localString,
                                                Language = lang,
                                                Creator = creator,
                                                CreationTime = now
                                            };
                                            try
                                            {
                                                await host.Connection.InsertAsync(GXInsertArgs.Insert(res));
                                                strings.Add(res);
                                            }
                                            catch (Exception)
                                            {
                                                throw;
                                                //string is ignored if it already exists.
                                            }
                                        }
                                    }
                                    else if (it.Value is byte[] ba2)
                                    {
                                        //Update image.
                                        images.Add(new GXAppearance()
                                        {
                                            Id = id,
                                            Creator = creator,
                                            CreationTime = now,
                                            ResourceType = (byte)ResourceType.Image,
                                            Value = ASCIIEncoding.ASCII.GetString(ba2),
                                        });
                                    }
                                }
                            }

                        }
                        catch (Exception)
                        {
                            //TODO: Log error.
                        }
                    }
                }
            }
            finally
            {
                rm.ReleaseAllResources();
            }
            return updated;
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
            public Task ContentClose(IReadOnlyList<string> users, IEnumerable<GXContent> contents)
            {
                return _hostedService.ContentClose(users, contents);
            }

            /// <inheritdoc/>
            public Task ContentDelete(IReadOnlyList<string> users, IEnumerable<GXContent> contents)
            {
                return _hostedService.ContentDelete(users, contents);
            }

            /// <inheritdoc/>
            public Task ContentGroupDelete(IReadOnlyList<string> users, IEnumerable<GXContentGroup> groups)
            {
                return _hostedService.ContentGroupDelete(users, groups);
            }

            /// <inheritdoc/>
            public Task ContentGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXContentGroup> groups)
            {
                return _hostedService.ContentGroupUpdate(users, groups);
            }

            /// <inheritdoc/>
            public Task ContentUpdate(IReadOnlyList<string> users, IEnumerable<GXContent> contents)
            {
                return _hostedService.ContentUpdate(users, contents);
            }
            /// <inheritdoc/>
            public Task ContentTypeDelete(IReadOnlyList<string> users, IEnumerable<GXContentType> contentTypes)
            {
                return _hostedService.ContentTypeDelete(users, contentTypes);
            }

            /// <inheritdoc/>
            public Task ContentTypeGroupDelete(IReadOnlyList<string> users, IEnumerable<GXContentTypeGroup> groups)
            {
                return _hostedService.ContentTypeGroupDelete(users, groups);
            }

            /// <inheritdoc/>
            public Task ContentTypeGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXContentTypeGroup> groups)
            {
                return _hostedService.ContentTypeGroupUpdate(users, groups);
            }

            /// <inheritdoc/>
            public Task ContentTypeUpdate(IReadOnlyList<string> users, IEnumerable<GXContentType> contentTypes)
            {
                return _hostedService.ContentTypeUpdate(users, contentTypes);
            }

            /// <inheritdoc/>
            public Task MenuDelete(IReadOnlyList<string> users, IEnumerable<GXMenu> menus)
            {
                return _hostedService.MenuDelete(users, menus);
            }

            /// <inheritdoc/>
            public Task MenuGroupDelete(IReadOnlyList<string> users, IEnumerable<GXMenuGroup> groups)
            {
                return _hostedService.MenuGroupDelete(users, groups);
            }

            /// <inheritdoc/>
            public Task MenuGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXMenuGroup> groups)
            {
                return _hostedService.MenuGroupUpdate(users, groups);
            }

            /// <inheritdoc/>
            public Task MenuUpdate(IReadOnlyList<string> users, IEnumerable<GXMenu> menus)
            {
                return _hostedService.MenuUpdate(users, menus);
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

            public Task GatewayGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXGatewayGroup> groups)
            {
                return _hostedService.GatewayGroupUpdate(users, groups);
            }

            public Task GatewayGroupDelete(IReadOnlyList<string> users, IEnumerable<GXGatewayGroup> groups)
            {
                return _hostedService.GatewayGroupDelete(users, groups);
            }

            public Task ClearGatewayLogs(IReadOnlyList<string> users, IEnumerable<GXGateway>? gateways)
            {
                return _hostedService.ClearGatewayLogs(users, gateways);
            }

            public Task AddGatewayLogs(IReadOnlyList<string> users, IEnumerable<GXGatewayLog> gateways)
            {
                return _hostedService.AddGatewayLogs(users, gateways);
            }

            public Task CloseGatewayLogs(IReadOnlyList<string> users, IEnumerable<GXGatewayLog> gateways)
            {
                return _hostedService.CloseGatewayLogs(users, gateways);
            }

            public Task GatewayUpdate(IReadOnlyList<string> users, IEnumerable<GXGateway> gateways)
            {
                return _hostedService.GatewayUpdate(users, gateways);
            }

            public Task GatewayDelete(IReadOnlyList<string> users, IEnumerable<GXGateway> gateways)
            {
                return _hostedService.GatewayDelete(users, gateways);
            }

            public Task GatewayStatusChange(IReadOnlyList<string> users, IEnumerable<GXGateway> gateways)
            {
                return _hostedService.GatewayStatusChange(users, gateways);
            }

            public Task DeviceStatusChange(IReadOnlyList<string> users, IEnumerable<GXDevice> devices)
            {
                return _hostedService.DeviceStatusChange(users, devices);
            }

            /// <inheritdoc/>
            public Task PerformanceAdd(IReadOnlyList<string> users, IEnumerable<GXPerformance> performances)
            {
                return _hostedService.PerformanceAdd(users, performances);
            }

            /// <inheritdoc/>
            public Task PerformanceClear(IReadOnlyList<string> users)
            {
                return _hostedService.PerformanceClear(users);
            }

            /// <inheritdoc/>
            public Task PerformanceDelete(IReadOnlyList<string> users, IEnumerable<Guid> performances)
            {
                return _hostedService.PerformanceDelete(users, performances);
            }

            /// <inheritdoc/>
            public Task SubtotalCalculate(IReadOnlyList<string> users, IEnumerable<GXSubtotal> subtotals)
            {
                return _hostedService.SubtotalCalculate(users, subtotals);
            }

            /// <inheritdoc/>
            public Task SubtotalClear(IReadOnlyList<string> users, IEnumerable<GXSubtotal> subtotals)
            {
                return _hostedService.SubtotalClear(users, subtotals);
            }

            /// <inheritdoc/>
            public Task SubtotalDelete(IReadOnlyList<string> users, IEnumerable<GXSubtotal> subtotals)
            {
                return _hostedService.SubtotalDelete(users, subtotals);
            }

            /// <inheritdoc/>
            public Task SubtotalGroupDelete(IReadOnlyList<string> users, IEnumerable<GXSubtotalGroup> groups)
            {
                return _hostedService.SubtotalGroupDelete(users, groups);
            }

            /// <inheritdoc/>
            public Task SubtotalGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXSubtotalGroup> groups)
            {
                return _hostedService.SubtotalGroupUpdate(users, groups);
            }

            /// <inheritdoc/>
            public Task SubtotalUpdate(IReadOnlyList<string> users, IEnumerable<GXSubtotal> subtotals)
            {
                return _hostedService.SubtotalUpdate(users, subtotals);
            }

            /// <inheritdoc/>
            public Task SubtotalValueUpdate(IReadOnlyList<string> users, IEnumerable<GXSubtotalValue> values)
            {
                return _hostedService.SubtotalValueUpdate(users, values);
            }

            /// <inheritdoc/>
            public Task AddSubtotalLogs(IReadOnlyList<string> users, IEnumerable<GXSubtotalLog> subtotals)
            {
                return _hostedService.AddSubtotalLogs(users, subtotals);
            }

            /// <inheritdoc/>
            public Task ClearSubtotalLogs(IReadOnlyList<string> users, IEnumerable<GXSubtotal>? subtotals)
            {
                return _hostedService.ClearSubtotalLogs(users, subtotals);
            }

            /// <inheritdoc/>
            public Task CloseSubtotalLogs(IReadOnlyList<string> users, IEnumerable<GXSubtotalLog> subtotals)
            {
                return _hostedService.CloseSubtotalLogs(users, subtotals);
            }

            /// <inheritdoc/>
            public Task ReportCalculate(IReadOnlyList<string> users, IEnumerable<GXReport> reports)
            {
                return _hostedService.ReportCalculate(users, reports);
            }

            /// <inheritdoc/>
            public Task ReportClear(IReadOnlyList<string> users, IEnumerable<GXReport> reports)
            {
                return _hostedService.ReportClear(users, reports);
            }

            /// <inheritdoc/>
            public Task ReportDelete(IReadOnlyList<string> users, IEnumerable<GXReport> reports)
            {
                return _hostedService.ReportDelete(users, reports);
            }

            /// <inheritdoc/>
            public Task ReportGroupDelete(IReadOnlyList<string> users, IEnumerable<GXReportGroup> groups)
            {
                return _hostedService.ReportGroupDelete(users, groups);
            }

            /// <inheritdoc/>
            public Task ReportGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXReportGroup> groups)
            {
                return _hostedService.ReportGroupUpdate(users, groups);
            }

            /// <inheritdoc/>
            public Task ReportUpdate(IReadOnlyList<string> users, IEnumerable<GXReport> reports)
            {
                return _hostedService.ReportUpdate(users, reports);
            }

            /// <inheritdoc/>
            public Task AddReportLogs(IReadOnlyList<string> users, IEnumerable<GXReportLog> reports)
            {
                return _hostedService.AddReportLogs(users, reports);
            }

            /// <inheritdoc/>
            public Task ClearReportLogs(IReadOnlyList<string> users, IEnumerable<GXReport>? reports)
            {
                return _hostedService.ClearReportLogs(users, reports);
            }

            /// <inheritdoc/>
            public Task CloseReportLogs(IReadOnlyList<string> users, IEnumerable<GXReportLog> reports)
            {
                return _hostedService.CloseReportLogs(users, reports);
            }

            /// <inheritdoc/>
            public Task UserStampUpdate(IReadOnlyList<string> users, IEnumerable<GXUserStamp> stamps)
            {
                return _hostedService.UserStampUpdate(users, stamps);
            }

            /// <inheritdoc/>
            public Task NotificationClear(IReadOnlyList<string> users, IEnumerable<GXNotification> notifications)
            {
                return _hostedService.NotificationClear(users, notifications);
            }

            /// <inheritdoc/>
            public Task NotificationDelete(IReadOnlyList<string> users, IEnumerable<GXNotification> notifications)
            {
                return _hostedService.NotificationDelete(users, notifications);
            }

            /// <inheritdoc/>
            public Task NotificationGroupDelete(IReadOnlyList<string> users, IEnumerable<GXNotificationGroup> groups)
            {
                return _hostedService.NotificationGroupDelete(users, groups);
            }

            /// <inheritdoc/>
            public Task NotificationGroupUpdate(IReadOnlyList<string> users, IEnumerable<GXNotificationGroup> groups)
            {
                return _hostedService.NotificationGroupUpdate(users, groups);
            }

            /// <inheritdoc/>
            public Task NotificationUpdate(IReadOnlyList<string> users, IEnumerable<GXNotification> notifications)
            {
                return _hostedService.NotificationUpdate(users, notifications);
            }

            /// <inheritdoc/>
            public Task AddNotificationLogs(IReadOnlyList<string> users, IEnumerable<GXNotificationLog> notifications)
            {
                return _hostedService.AddNotificationLogs(users, notifications);
            }

            /// <inheritdoc/>
            public Task ClearNotificationLogs(IReadOnlyList<string> users, IEnumerable<GXNotification>? notifications)
            {
                return _hostedService.ClearNotificationLogs(users, notifications);
            }

            /// <inheritdoc/>
            public Task AppearanceDelete(IReadOnlyList<string>? users, IEnumerable<GXAppearance>? appearances)
            {
                return _hostedService.AppearanceDelete(users, appearances);
            }

            /// <inheritdoc/>
            public Task AppearanceUpdate(IReadOnlyList<string>? users, IEnumerable<GXAppearance> appearances)
            {
                return _hostedService.AppearanceUpdate(users, appearances);
            }

            /// <inheritdoc/>
            public Task LocalizedResourceUpdate(IReadOnlyList<string> users, IEnumerable<GXLocalizedResource> resources)
            {
                return _hostedService.LocalizedResourceUpdate(users, resources);
            }

            /// <inheritdoc/>
            public Task LocalizedResourceDelete(IReadOnlyList<string> users, IEnumerable<GXLocalizedResource> resources)
            {
                return _hostedService.LocalizedResourceDelete(users, resources);
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
                if (lambdaEx.Body is UnaryExpression ue)
                {
                    return ((MemberExpression)ue.Operand).Member.Name == name;
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

        /// <summary>
        /// Filter by.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="filter">Filter.</param>
        public static void FilterBy<T>(List<T> target, T? filter)
        {
            List<T> list = new List<T>();
            foreach (var it in target)
            {
                if (FilterBy(it, filter))
                {
                    list.Add(it);
                }
            }
            target.RemoveAll(w => list.Contains(w));
        }

        public static bool Compare(object val1, object val2)
        {
            if (val1.GetType() == typeof(string) || val1.GetType() == typeof(Guid))
            {

            }
            else if (typeof(System.Collections.IEnumerable).IsAssignableFrom(val1.GetType()))
            {
                IEnumerator v1 = ((IEnumerable)val1).GetEnumerator();
                IEnumerator v2 = ((IEnumerable)val2).GetEnumerator();
                while (v1.MoveNext() && v2.MoveNext())
                {
                    val1 = v1.Current;
                    val2 = v2.Current;
                    Compare(val1, val2);
                }
                if (v1.MoveNext() || v2.MoveNext())
                {
                    Debug.WriteLine("DIFFERENT amount of values. ");
                    return false;
                }
                return true;
            }
            else if (val1.GetType().IsClass &&
                val1.GetType() != typeof(DefaultJsonTypeInfoResolver) &&
                val1.GetType() != typeof(ModelAttributes) &&
                val1.GetType().Name != "RuntimeType" &&
                val1.GetType().Namespace != typeof(System.Text.Json.JsonCommentHandling).Namespace &&
                val1.GetType().Namespace != typeof(System.Text.EncodingProvider).Namespace &&
                val1.GetType() != typeof(StringOutputFormatter))
            {
                IEnumerator p1 = val1.GetType().GetProperties().GetEnumerator();
                IEnumerator p2 = val2.GetType().GetProperties().GetEnumerator();
                while (p1.MoveNext() && p2.MoveNext())
                {
                    object? value = ((PropertyInfo)p1.Current).GetValue(val1);
                    object? value2 = ((PropertyInfo)p2.Current).GetValue(val2);
                    if (value != null &&
                        value.GetType() != typeof(ModelAttributes) &&
                        value.GetType().Namespace != typeof(System.Text.Json.JsonCommentHandling).Namespace &&
                        value.GetType() != typeof(DefaultJsonTypeInfoResolver) &&
                        value.GetType() != typeof(StringOutputFormatter))
                    {
                        if (value.GetType() != typeof(string) &&
                            value.GetType() != typeof(Guid) &&
                            value.GetType().IsClass == true)
                        {
                            if (!Compare(value, value2))
                            {
                                return false;
                            }
                        }
                        if (value?.Equals(value2) != true)
                        {
                            Debug.WriteLine("DIFFERENT: " + value + ": " + value2);
                            return false;
                        }
                        else
                        {
                            Debug.WriteLine("SAME: " + value + ": " + value2);
                        }
                    }
                }
                if (p1.MoveNext() || p2.MoveNext())
                {
                    Debug.WriteLine("DIFFERENT amount of values. ");
                    return false;
                }
            }
            if (val1.Equals(val2) != true)
            {
                Debug.WriteLine("DIFFERENT" + val1 + ": " + val2);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Filter by.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="filter">Filter.</param>
        private static bool FilterBy<T>(T target, T? filter)
        {
            if (target != null && filter != null)
            {
                Dictionary<PropertyInfo, FilterAttribute> properties = new();
                foreach (var it in filter.GetType().GetProperties())
                {
                    var att = it.GetCustomAttribute<FilterAttribute>();
                    if (it != null && att != null)
                    {
                        properties.Add(it, att);
                    }
                }
                foreach (var it in properties)
                {
                    object? actual = it.Key?.GetValue(target);
                    object? filterValue = it.Key?.GetValue(filter);
                    if (actual == null && filterValue == null)
                    {
                    }
                    else if (actual != null && filterValue != null)
                    {
                        if (actual is DateTime d && filterValue is DateTime d2)
                        {
                            if (d2 != DateTime.MinValue && d != d2)
                            {
                                return true;
                            }
                            continue;
                        }
                        else if (actual is DateTimeOffset dto && filterValue is DateTimeOffset dto2)
                        {
                            if (dto2 != DateTimeOffset.MinValue && dto != dto2)
                            {
                                return true;
                            }
                            continue;
                        }
                        else if (actual is Guid q && filterValue is Guid q2)
                        {
                            if (q2 != Guid.Empty && q != q2)
                            {
                                return true;
                            }
                            continue;
                        }
                    }
                    if (actual != null)
                    {
                        if (!(actual is string))
                        {
                            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(actual.GetType()))
                            {
                                foreach (var e1 in (System.Collections.IEnumerable)actual)
                                {
                                    if (FilterBy(e1, filterValue))
                                    {
                                        return true;
                                    }
                                }
                                continue;
                            }
                            else if (actual.GetType().IsClass)
                            {
                                if (FilterBy(actual, filterValue))
                                {
                                    return true;
                                }
                                continue;
                            }
                        }
                    }
                    if (filterValue != null && Convert.ToString(filterValue) != Convert.ToString(actual))
                    {
                        if (actual != null)
                        {
                            if (actual.GetType().IsEnum)
                            {
                                actual = Convert.ToInt64(actual);
                            }
                            if (actual is bool b)
                            {
                                int val = b ? 1 : 0;
                                if (!val.Equals(filterValue))
                                {
                                    return true;
                                }
                            }
                            else if (actual is Guid)
                            {
                                if ((Guid)filterValue != Guid.Empty && !actual.Equals(filterValue))
                                {
                                    return true;
                                }
                            }
                            else
                            {
                                switch (it.Value.FilterType)
                                {
                                    case FilterType.Exact:
                                        if (filterValue != null && !actual.Equals(filterValue))
                                        {
                                            return true;
                                        }
                                        break;
                                    case FilterType.Equals:
                                        if (filterValue != null && !actual.Equals(filterValue))
                                        {
                                            return true;
                                        }
                                        break;
                                    case FilterType.Greater:
                                        //TODO: And<T>(q => GXSql.Greater(it.Value.Target, actual));
                                        break;
                                    case FilterType.Less:
                                        //TODO: And<T>(q => GXSql.Less(it.Value.Target, actual));
                                        break;
                                    case FilterType.GreaterOrEqual:
                                        //TODO: And<T>(q => GXSql.GreaterOrEqual(it.Value.Target, actual));
                                        break;
                                    case FilterType.LessOrEqual:
                                        //TODO: And<T>(q => GXSql.LessOrEqual(it.Value.Target, actual));
                                        break;
                                    case FilterType.StartsWith:
                                        if (filterValue != null && !Convert.ToString(actual).ToLower().StartsWith(Convert.ToString(filterValue).ToLower()))
                                        {
                                            return true;
                                        }
                                        break;
                                    case FilterType.EndsWith:
                                        if (filterValue != null && !Convert.ToString(actual).ToLower().EndsWith(Convert.ToString(filterValue).ToLower()))
                                        {
                                            return true;
                                        }
                                        break;
                                    case FilterType.Contains:
                                        if (filterValue != null && !Convert.ToString(actual).ToLower().Contains(Convert.ToString(filterValue).ToLower()))
                                        {
                                            return true;
                                        }
                                        break;
                                    case FilterType.Null:
                                        if (filterValue != null && actual != null)
                                        {
                                            return true;
                                        }
                                        break;
                                    case FilterType.NotNull:
                                        if (filterValue != null && actual == null)
                                        {
                                            return true;
                                        }
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException(nameof(it.Value.FilterType));
                                }
                            }
                        }
                    }
                    else if (it.Value.FilterType == FilterType.Null)
                    {
                        if (filterValue != null && actual != null)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static void OrderByInternal<T>(List<T> list, Type type, string[] orderBy, int index, bool descending)
        {
            PropertyInfo? prop = type.GetProperty(orderBy[index]);
            if (prop != null)
            {
                if (orderBy.Length == 1 + index)
                {
                    if (descending)
                    {
                        list = list.OrderByDescending(o => prop.GetValue(o, null)).ToList();
                    }
                    else
                    {
                        if (index == 0)
                        {
                            list = list.OrderBy(o => prop.GetValue(o, null)).ToList();
                        }
                        else
                        {
                            list = list.OrderBy(o => prop.GetValue(o, null)).ToList();
                        }
                    }
                }
                else
                {
                    OrderByInternal(list, prop.PropertyType, orderBy, 1 + index, descending);
                }
            }
        }

        /// <summary>
        /// GXComparer is used to sort classes using class properties.
        /// </summary>
        class GXComparer : IComparer<object>
        {
            private readonly bool _descending;
            private List<PropertyInfo>? _props = new List<PropertyInfo>();
            Dictionary<string, Type>? _enumTypes;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="enumTypes">Property enum types.</param>
            /// <param name="descending">Descending</param>
            /// <param name="orderBy">Dot separated order by name.</param>
            public GXComparer(Dictionary<string, Type>? enumTypes, bool descending, string orderBy)
            {
                _enumTypes = enumTypes;
                _descending = descending;
                Type type = typeof(GXObject);
                string[] list = orderBy.Split('.');
                foreach (var it in list)
                {
                    PropertyInfo? pi = type.GetProperty(it);
                    type = pi.PropertyType;
                    _props.Add(pi);
                }
            }

            /// <inheritdoc/>
            public int Compare(object? obj1, object? obj2)
            {
                string? propName = null;
                object? value1 = obj1, value2 = obj2;
                Comparer comparer = new Comparer(Thread.CurrentThread.CurrentCulture);
                if (_props != null)
                {
                    foreach (var it in _props)
                    {
                        value1 = it.GetValue(value1, null);
                        value2 = it.GetValue(value2, null);
                        propName = it.Name;
                    }
                }
                if (value1 != null && value2 != null && _enumTypes != null &&
                    propName != null && _enumTypes.TryGetValue(propName, out Type? type))
                {
                    value1 = Enum.GetName(type, value1);
                    value2 = Enum.GetName(type, value2);
                }
                if (value1 == null || value2 == null)
                {

                }
                if (_descending)
                {
                    return comparer.Compare(value2, value1);
                }
                return comparer.Compare(value1, value2);
            }
        }

        /// <summary>
        /// Order by.
        /// </summary>
        /// <param name="objects">Ordered objects</param>
        /// <param name="enumTypes">Property enum types.</param>
        /// <param name="orderBy">Order by.</param>
        /// <param name="descending">Descending.</param>
        public static List<T> OrderBy<T>(List<T> objects, string orderBy, bool descending = false, Dictionary<string, Type>? enumTypes = null)
        {
            var comparer = new GXComparer(enumTypes, descending, orderBy);
            return objects.OrderBy(o => o, comparer).ToList();
        }
    }
    static class ScopeHelpers
    {
        static public bool HasScope(this ClaimsPrincipal user, string requiredScope)
        {
            return user.Claims
                .Where(c => c.Type == "scope")
                .SelectMany(c => c.Value.Split(' '))
                .Contains(requiredScope);
        }
    }
}
