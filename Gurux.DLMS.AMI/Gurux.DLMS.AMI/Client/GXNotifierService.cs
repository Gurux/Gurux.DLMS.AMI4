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

using Gurux.DLMS.AMI.Client.Helpers.Toaster;
using Gurux.DLMS.AMI.Client.Pages.Admin;
using Gurux.DLMS.AMI.Client.Pages.Agent;
using Gurux.DLMS.AMI.Client.Pages.Device;
using Gurux.DLMS.AMI.Client.Pages.Module;
using Gurux.DLMS.AMI.Client.Pages.Schedule;
using Gurux.DLMS.AMI.Client.Pages.Script;
using Gurux.DLMS.AMI.Client.Pages.User;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Gurux.DLMS.AMI.Client
{
    /// <summary>
    /// Notification service is used to show the top menu, occurred exceptions and progress indicator.
    /// </summary>
    public class GXNotifierService : ComponentBase, IGXNotifier
    {
        private readonly ConcurrentDictionary<string, List<EventHandler>> _handlers = new ConcurrentDictionary<string, List<EventHandler>>(StringComparer.Ordinal);

        private List<string> pageList;
        private Dictionary<string, object?> dataList;
        private readonly ILogger _logger;
        private readonly IGXToasterService _toasterService;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXNotifierService(IServiceProvider services)
        {
            ILoggerFactory lf = services.GetRequiredService<ILoggerFactory>();
            _logger = lf.CreateLogger(nameof(GXNotifierService));
            _toasterService = services.GetRequiredService<IGXToasterService>();
            pageList = new List<string>();
            dataList = new Dictionary<string, object?>();
            Items = new List<GXMenuItem>();
        }

        /// <summary>
        /// Menu items.
        /// </summary>
        public List<GXMenuItem> Items { get; set; }

        /// <summary>
        /// Add new menu item.
        /// </summary>
        /// <param name="menu"></param>
        public void AddMenuItem(GXMenuItem menu)
        {
            Items.Add(menu);
            OnUpdateButtons?.Invoke();
        }

        /// <summary>
        /// Return selection path.
        /// </summary>
        /// <returns>Selection path</returns>
        public ReadOnlyCollection<string> GetSelectionPath()
        {
            return pageList.AsReadOnly();
        }

        /// <summary>
        /// Clear menu item.
        /// </summary>
        public void Clear()
        {
            Items.Clear();
            OnUpdateButtons?.Invoke();
        }

        /// <summary>
        /// Progress started.
        /// </summary>
        public void ProgressStart()
        {
            OnProgressStart?.Invoke();
        }

        /// <summary>
        /// Progress ended.
        /// </summary>
        public void ProgressEnd()
        {
            OnProgressEnd?.Invoke();
        }

        /// <summary>
        /// Clear status.
        /// </summary>
        public void ClearStatus()
        {
            OnClearStatus?.Invoke();
        }


        /// <summary>
        /// Show information.
        /// </summary>
        public void ShowInformation(string info, bool closable = false)
        {
            OnShowInformation?.Invoke(info, closable);
        }

        /// <inheritdoc />
        public void ProcessError(Exception ex)
        {
            OnProcessError?.Invoke(ex);
        }

        /// <inheritdoc />
        public void ProcessErrors(IEnumerable<object> errors)
        {
            OnProcessErrors?.Invoke(errors);
        }

        /// <inheritdoc />
        public void ClearHistory()
        {
            pageList.Clear();
        }

        /// <inheritdoc />
        public void ChangePage(string page, object? data)
        {
            ChangePage(page, data, false);
        }

        /// <inheritdoc />
        public void ChangePage(string page, object? data, bool isRoot)
        {
            if (isRoot)
            {
                pageList.Clear();
            }
            pageList.Add(page);
            dataList[page] = data;
            OnPageChanged?.Invoke();
            UpdateButtons();
        }

        /// <inheritdoc />
        public void UpdateData(string page, object? data)
        {
            dataList[page] = data;
        }


        /// <inheritdoc />
        public void RemovePage(string page)
        {
            foreach (var it in pageList)
            {
                if (it == page)
                {
                    pageList.Remove(it);
                    break;
                }
            }
        }

        /// <inheritdoc />
        public void RemoveLastPage(string page)
        {
            RemovePage(page);
        }

        /// <inheritdoc />
        public string? RemoveLastPage()
        {
            string? page = GetLastPage();
            if (page != null)
            {
                pageList.RemoveAt(pageList.Count - 1);
            }
            return page;
        }

        /// <inheritdoc />
        public string? GetLastPage()
        {
            if (pageList.Count == 0)
            {
                return null;
            }
            return pageList[pageList.Count - 1];
        }

        /// <inheritdoc />
        public object? GetData(string page)
        {
            if (dataList.ContainsKey(page))
            {
                return dataList[page];
            }
            return null;
        }

        /// <inheritdoc />
        public string? Title
        {
            get;
            set;
        }

        /// <summary>
        /// CRUD action.
        /// </summary>
        public CrudAction Action
        {
            get;
            set;
        }

        /// <inheritdoc />
        public void UpdateButtons()
        {
            OnUpdateButtons?.Invoke();
        }

        /// <inheritdoc />
        public async Task PageChanged()
        {
            if (OnPageChanged != null)
            {
                OnPageChanged?.Invoke();
            }
        }

        /// <inheritdoc />
        public void RemoveListener(object listener)
        {
            lock (_handlers)
            {
                foreach (var handler in _handlers)
                {
                    foreach (var it in handler.Value)
                    {
                        if (it.Listener == listener)
                        {
                            handler.Value.Remove(it);
                            break;
                        }
                    }
                }
            }
        }

        /// <inheritdoc cref="IGXNotifier.ChangedAsync"/>
        public async Task ChangedAsync<T>(string methodName, T value)
        {
            ReadOnlyCollection<EventHandler>? handlers = null;
            lock (_handlers)
            {
                if (_handlers.TryGetValue(methodName, out var handler))
                {
                    handlers = handler.AsReadOnly();
                }
            }
            if (handlers != null)
            {
                object?[] tmp = new object[] { value };
                foreach (var it in handlers)
                {
                    await it.InvokeAsync(tmp);
                }
            }
        }

        /// <summary>
        /// Invoke that property has changed.
        /// </summary>
        internal async Task ChangedAsync(string methodName)
        {
            ReadOnlyCollection<EventHandler>? handlers = null;
            lock (_handlers)
            {
                if (_handlers.TryGetValue(methodName, out var handler))
                {
                    handlers = handler.AsReadOnly();
                }
            }
            if (handlers != null)
            {
                foreach (var it in handlers)
                {
                    await it.InvokeAsync(null);
                }
            }
        }


        /// <summary>
        /// Event handler.
        /// </summary>
        /// <remarks>
        /// This is struct because it's read-only.
        /// </remarks>
        private readonly struct EventHandler
        {
            /// <summary>
            /// Listener.
            /// </summary>
            public readonly object Listener { get; }
            /// <summary>
            /// Parameter types.
            /// </summary>
            public Type[] ParameterTypes { get; }
            /// <summary>
            /// Notification function that is called when event occurs.
            /// </summary>
            private readonly Func<object?[]?, object, Task> _callback;

            /// <summary>
            /// Event parameters.
            /// </summary>
            private readonly object _params;


            public EventHandler(object source, Type[] parameterTypes, Func<object?[]?, object, Task> callback, object parameters)
            {
                Listener = source;
                _callback = callback;
                ParameterTypes = parameterTypes;
                _params = parameters;
            }

            /// <summary>
            /// Invoke listeners async.
            /// </summary>
            /// <param name="parameters"></param>
            /// <returns></returns>
            public Task InvokeAsync(object?[]? parameters)
            {
                return _callback(parameters, _params);
            }
        }

        /// <summary>
        /// Listen event notifications.
        /// </summary>
        /// <param name="listener">Listener</param>
        /// <param name="methodName">Method name.</param>
        /// <param name="parameterTypes">Parameter types.</param>
        /// <param name="handler">Handler.</param>
        /// <returns></returns>
        void On(object listener, string methodName, Type[] parameterTypes, Action<object?[]> handler)
        {
            On(listener, methodName, parameterTypes, (parameters, state) =>
            {
                var currentHandler = (Action<object?[]>)state;
                currentHandler(parameters);
                return Task.CompletedTask;
            }, handler);
        }

        /// <summary>
        /// Listen event notifications.
        /// </summary>
        /// <typeparam name="T1">Notification parameter type.</typeparam>
        /// <param name="listener">Listener.</param>
        /// <param name="methodName">Method name.</param>
        /// <param name="handler">Handler.</param>
        /// <returns></returns>
        public void On(object listener, string methodName, Action handler)
        {
            On(listener, methodName,
               new Type[0],
               args => handler());
        }

        /// <summary>
        /// Listen event notifications.
        /// </summary>
        /// <typeparam name="T1">Notification parameter type.</typeparam>
        /// <param name="listener">Listener.</param>
        /// <param name="methodName">Method name.</param>
        /// <param name="handler">Handler.</param>
        /// <returns></returns>
        public void On<T1>(object listener, string methodName, Action<T1> handler)
        {
            On(listener, methodName,
                new[] { typeof(T1) },
                args => handler((T1)args[0]!));
        }

        private void On(object listener, string methodName, Type[] parameterTypes, Func<object?[], object, Task> handler, object value)
        {
            var invocationHandler = new EventHandler(listener, parameterTypes, handler, value);
            List<EventHandler> list = new List<EventHandler>();
            list.Add(invocationHandler);
            lock (_handlers)
            {
                var invocationList = _handlers.AddOrUpdate(methodName, _ => list,
               (_, invocations) =>
               {
                   lock (invocations)
                   {
                       invocations.Add(invocationHandler);
                   }
                   return invocations;
               });
            }
        }

        /// <summary>
        /// Listen events from the server.
        /// </summary>
        private HubConnection? hubConnection;


        private string ToString<T>(IEnumerable<T> items)
        {
            StringBuilder sb = new StringBuilder();
            //Show max 10 items.
            int count = 1;
            foreach (var it in items)
            {
                sb.AppendLine(Convert.ToString(it));
                if (++count > 10)
                {
                    sb.AppendLine("...");
                    break;
                }
            }
            return sb.ToString();
        }


        public async Task StartAsync(NavigationManager navigationManager, IAccessTokenProvider accessTokenProvider)
        {
            try
            {
                if (hubConnection == null)
                {
                    hubConnection = new HubConnectionBuilder()
                   .WithUrl(navigationManager.ToAbsoluteUri("/guruxami"), options =>
                   {
                       options.AccessTokenProvider = async () =>
                       {
                           var accessTokenResult = await accessTokenProvider.RequestAccessToken();
                           accessTokenResult.TryGetToken(out var accessToken);
                           return accessToken.Value;
                       };
                   })
                   .WithAutomaticReconnect()
                    .Build();
                    hubConnection.On<IEnumerable<GXConfiguration>>(nameof(IGXHubEvents.ConfigurationSave), async (configurations) =>
                    {
                        _toasterService.Add(new GXToast("Configuration updated.", ToString(configurations), Color.Success, 15));
                        await ChangedAsync(nameof(IGXHubEvents.ConfigurationSave), configurations);
                    });

                    hubConnection.On<GXModule>(nameof(IGXHubEvents.ModuleSettingsSave), async (module) =>
                    {
                        _toasterService.Add(new GXToast("Module settings updated.", Convert.ToString(module), Color.Success, 15));
                        await ChangedAsync(nameof(IGXHubEvents.ModuleSettingsSave), module);
                    });
                    hubConnection.On(nameof(IGXHubEvents.ClearSystemLogs), async () =>
                    {
                        _toasterService.Add(new GXToast("System log cleared", "System log cleared.", Color.Success, 15));
                        await ChangedAsync(nameof(IGXHubEvents.ClearSystemLogs));
                    });
                    hubConnection.On<IEnumerable<GXSystemLog>>(nameof(IGXHubEvents.AddSystemLogs), async (items) =>
                    {
                        _toasterService.Add(new GXToast("System log item added", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.AddSystemLogs), items);
                    });
                    hubConnection.On<IEnumerable<GXSystemLog>>(nameof(IGXHubEvents.CloseSystemLogs), async (items) =>
                    {
                        _toasterService.Add(new GXToast("System log item closed", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.CloseSystemLogs), items);
                    });
                    hubConnection.On<IEnumerable<GXDeviceError>?>(nameof(IGXHubEvents.ClearDeviceErrors), async (devices) =>
                    {
                        if (devices == null || !devices.Any())
                        {
                            _toasterService.Add(new GXToast("Device error cleared", "All errors cleared.", Color.Info, 15));
                        }
                        else
                        {
                            _toasterService.Add(new GXToast("Device error clear", ToString(devices), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ClearDeviceErrors), devices);
                    });
                    hubConnection.On<IEnumerable<GXDeviceError>>(nameof(IGXHubEvents.AddDeviceErrors), async (errors) =>
                    {
                        _toasterService.Add(new GXToast("Device error added", ToString(errors), Color.Warning, 15));
                        await ChangedAsync(nameof(IGXHubEvents.AddDeviceErrors), errors);
                    });
                    hubConnection.On<IEnumerable<GXDeviceError>>(nameof(IGXHubEvents.CloseDeviceErrors), async (errors) =>
                    {
                        _toasterService.Add(new GXToast("Device error closed", ToString(errors), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.CloseDeviceErrors), errors);
                    });
                    hubConnection.On<IEnumerable<GXWorkflow>?>(nameof(IGXHubEvents.ClearWorkflowLogs), async (workflows) =>
                    {
                        if (workflows == null || !workflows.Any())
                        {
                            _toasterService.Add(new GXToast("Workflow log cleared", "All log cleared.", Color.Info, 15));
                        }
                        else
                        {
                            _toasterService.Add(new GXToast("Workflow log cleared", ToString(workflows), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ClearWorkflowLogs), workflows);
                    });
                    hubConnection.On<IEnumerable<GXWorkflowLog>>(nameof(IGXHubEvents.AddWorkflowLogs), async (errors) =>
                    {
                        _toasterService.Add(new GXToast("Workflow log added", ToString(errors), Color.Warning, 15));
                        await ChangedAsync(nameof(IGXHubEvents.AddWorkflowLogs), errors);
                    });
                    hubConnection.On<IEnumerable<GXWorkflowLog>>(nameof(IGXHubEvents.CloseWorkflowLogs), async (errors) =>
                    {
                        _toasterService.Add(new GXToast("Workflow log closed", ToString(errors), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.CloseWorkflowLogs), errors);
                    });
                    hubConnection.On<IEnumerable<GXScript>>(nameof(IGXHubEvents.ClearScriptLogs), async (scripts) =>
                    {
                        if (scripts == null || !scripts.Any())
                        {
                            _toasterService.Add(new GXToast("Script log cleared", "All log cleared.", Color.Info, 15));
                        }
                        else
                        {
                            _toasterService.Add(new GXToast("Script log cleared", ToString(scripts), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ClearScriptLogs), scripts);
                    });
                    hubConnection.On<IEnumerable<GXScriptLog>>(nameof(IGXHubEvents.AddScriptLogs), async (errors) =>
                    {
                        _toasterService.Add(new GXToast("Script log added", ToString(errors), Color.Warning, 15));
                        await ChangedAsync(nameof(IGXHubEvents.AddScriptLogs), errors);
                    });
                    hubConnection.On<IEnumerable<GXScriptLog>>(nameof(IGXHubEvents.CloseScriptLogs), async (errors) =>
                    {
                        _toasterService.Add(new GXToast("Script log closed", ToString(errors), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.CloseScriptLogs), errors);
                    });
                    hubConnection.On<IEnumerable<GXUser>>(nameof(IGXHubEvents.ClearUserErrors), async (users) =>
                    {
                        if (users == null || !users.Any())
                        {
                            _toasterService.Add(new GXToast("User errors cleared", "All errors cleared.", Color.Info, 15));
                        }
                        else
                        {
                            _toasterService.Add(new GXToast("User error cleared", ToString(users), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ClearUserErrors), users);
                    });
                    hubConnection.On<IEnumerable<GXUserError>>(nameof(IGXHubEvents.AddUserErrors), async (errors) =>
                    {
                        _toasterService.Add(new GXToast("User error added", ToString(errors), Color.Warning, 15));
                        await ChangedAsync(nameof(IGXHubEvents.AddUserErrors), errors);
                    });
                    hubConnection.On<IEnumerable<GXUserError>>(nameof(IGXHubEvents.CloseUserErrors), async (errors) =>
                    {
                        _toasterService.Add(new GXToast("User error closed", ToString(errors), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.CloseUserErrors), errors);
                    });
                    hubConnection.On<IEnumerable<GXModule>>(nameof(IGXHubEvents.ClearModuleLogs), async (modules) =>
                    {
                        if (modules == null || !modules.Any())
                        {
                            _toasterService.Add(new GXToast("Module logs cleared", "All logs cleared.", Color.Info, 15));
                        }
                        else
                        {
                            _toasterService.Add(new GXToast("Module logs cleared", ToString(modules), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ClearModuleLogs), modules);
                    });
                    hubConnection.On<IEnumerable<GXModuleLog>>(nameof(IGXHubEvents.AddModuleLogs), async (errors) =>
                    {
                        _toasterService.Add(new GXToast("Module log added", ToString(errors), Color.Warning, 15));
                        await ChangedAsync(nameof(IGXHubEvents.AddModuleLogs), errors);
                    });
                    hubConnection.On<IEnumerable<GXModuleLog>>(nameof(IGXHubEvents.CloseModuleLogs), async (errors) =>
                    {
                        _toasterService.Add(new GXToast("Module log closed", ToString(errors), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.CloseModuleLogs), errors);
                    });
                    hubConnection.On<IEnumerable<GXAgent>?>(nameof(IGXHubEvents.ClearAgentLogs), async (logs) =>
                    {
                        if (logs == null || !logs.Any())
                        {
                            _toasterService.Add(new GXToast("Agent logs cleared", "All logs cleared.", Color.Info, 15));
                        }
                        else
                        {
                            _toasterService.Add(new GXToast("Agent log clered", ToString(logs), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ClearAgentLogs), logs);
                    });
                    hubConnection.On<IEnumerable<GXAgentLog>>(nameof(IGXHubEvents.AddAgentLogs), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Agent log added", ToString(items), Color.Warning, 15));
                        await ChangedAsync(nameof(IGXHubEvents.AddAgentLogs), items);
                    });
                    hubConnection.On<IEnumerable<GXAgentLog>>(nameof(IGXHubEvents.CloseAgentLogs), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Agent log closed", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.CloseAgentLogs), items);
                    });
                    hubConnection.On<IEnumerable<GXSchedule>>(nameof(IGXHubEvents.ScheduleUpdate), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Schedule updated", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.ScheduleUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXSchedule>>(nameof(IGXHubEvents.ScheduleDelete), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Schedule delete", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.ScheduleDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXSchedule>>(nameof(IGXHubEvents.ScheduleStart), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Schedule start", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.ScheduleStart), items);
                    });
                    hubConnection.On<IEnumerable<GXSchedule>>(nameof(IGXHubEvents.ScheduleCompleate), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Schedule compleate", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.ScheduleCompleate), items);
                    });
                    hubConnection.On<IEnumerable<GXScheduleLog>>(nameof(IGXHubEvents.AddScheduleLog), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Schedule log added", ToString(items), Color.Warning, 15));
                        await ChangedAsync(nameof(IGXHubEvents.AddScheduleLog), items);
                    });
                    hubConnection.On<IEnumerable<GXSchedule>?>(nameof(IGXHubEvents.ClearScheduleLog), async (schedules) =>
                    {
                        if (schedules == null || !schedules.Any())
                        {
                            _toasterService.Add(new GXToast("Schedule errors cleared", "All errors cleared.", Color.Info, 15));
                        }
                        else
                        {
                            _toasterService.Add(new GXToast("Schedule log clear", ToString(schedules), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ClearScheduleLog), schedules);
                    });
                    hubConnection.On<IEnumerable<GXScheduleLog>>(nameof(IGXHubEvents.CloseScheduleLog), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Schedule log closed", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.CloseScheduleLog), items);
                    });
                    hubConnection.On<IEnumerable<GXScheduleGroup>>(nameof(IGXHubEvents.ScheduleGroupUpdate), async (groups) =>
                    {
                        _toasterService.Add(new GXToast("Schedule group updated", ToString(groups), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.ScheduleGroupUpdate), groups);
                    });
                    hubConnection.On<IEnumerable<GXScheduleGroup>>(nameof(IGXHubEvents.ScheduleGroupDelete), async (groups) =>
                    {
                        _toasterService.Add(new GXToast("Schedule group delete", ToString(groups), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.ScheduleGroupDelete), groups);
                    });
                    hubConnection.On<IEnumerable<GXUser>>(nameof(IGXHubEvents.UserUpdate), async (users) =>
                    {
                        _toasterService.Add(new GXToast("User updated", ToString(users), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.UserUpdate), users);
                    });
                    hubConnection.On<IEnumerable<GXUser>>(nameof(IGXHubEvents.UserDelete), async (users) =>
                    {
                        _toasterService.Add(new GXToast("User delete", ToString(users), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.UserDelete), users);
                    });
                    hubConnection.On<IEnumerable<GXUserGroup>>(nameof(IGXHubEvents.UserGroupUpdate), async (groups) =>
                    {
                        _toasterService.Add(new GXToast("User group update", ToString(groups), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.UserGroupUpdate), groups);
                    });
                    hubConnection.On<IEnumerable<GXUserGroup>>(nameof(IGXHubEvents.UserGroupDelete), async (groups) =>
                    {
                        _toasterService.Add(new GXToast("User group delete", ToString(groups), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.UserGroupDelete), groups);
                    });
                    hubConnection.On<IEnumerable<GXDevice>>(nameof(IGXHubEvents.DeviceUpdate), async (devices) =>
                    {
                        _toasterService.Add(new GXToast("Device updated", ToString(devices), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.DeviceUpdate), devices);
                    });
                    hubConnection.On<IEnumerable<GXDevice>>(nameof(IGXHubEvents.DeviceDelete), async (devices) =>
                    {
                        _toasterService.Add(new GXToast("Device deleted", ToString(devices), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.DeviceDelete), devices);
                    });
                    hubConnection.On<IEnumerable<GXDeviceGroup>>(nameof(IGXHubEvents.DeviceGroupUpdate), async (groups) =>
                    {
                        _toasterService.Add(new GXToast("Device group updated", ToString(groups), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.DeviceGroupUpdate), groups);
                    });
                    hubConnection.On<IEnumerable<GXDeviceGroup>>(nameof(IGXHubEvents.DeviceGroupDelete), async (groups) =>
                    {
                        _toasterService.Add(new GXToast("Device group deleted", ToString(groups), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.DeviceGroupDelete), groups);
                    });
                    hubConnection.On<IEnumerable<GXDeviceTemplateGroup>>(nameof(IGXHubEvents.DeviceTemplateGroupUpdate), async (groups) =>
                    {
                        _toasterService.Add(new GXToast("Device template group updated", ToString(groups), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.DeviceTemplateGroupUpdate), groups);
                    });
                    hubConnection.On<IEnumerable<GXDeviceTemplateGroup>>(nameof(IGXHubEvents.DeviceTemplateGroupDelete), async (groups) =>
                    {
                        _toasterService.Add(new GXToast("Device template group deleted", ToString(groups), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.DeviceTemplateGroupDelete), groups);
                    });
                    hubConnection.On<IEnumerable<GXDeviceTemplate>>(nameof(IGXHubEvents.DeviceTemplateUpdate), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Device template updated", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.DeviceTemplateUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXDeviceTemplate>>(nameof(IGXHubEvents.DeviceTemplateDelete), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Device template deleted", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.DeviceTemplateDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXAgent>>(nameof(IGXHubEvents.AgentUpdate), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Agent updated", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.AgentUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXAgent>>(nameof(IGXHubEvents.AgentDelete), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Agent deleted", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.AgentDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXAgent>>(nameof(IGXHubEvents.AgentStatusChange), async (agents) =>
                    {
                        _toasterService.Add(new GXToast("Agent status changed", ToString(agents), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.AgentStatusChange), agents);
                    });
                    hubConnection.On<IEnumerable<GXAgentGroup>>(nameof(IGXHubEvents.AgentGroupUpdate), async (groups) =>
                    {
                        _toasterService.Add(new GXToast("Agent group updated", ToString(groups), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.AgentGroupUpdate), groups);
                    });
                    hubConnection.On<IEnumerable<GXAgentGroup>>(nameof(IGXHubEvents.AgentGroupDelete), async (groups) =>
                    {
                        _toasterService.Add(new GXToast("Agent group deleted", ToString(groups), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.AgentGroupDelete), groups);
                    });
                    hubConnection.On<IEnumerable<GXWorkflow>>(nameof(IGXHubEvents.WorkflowUpdate), async (groups) =>
                    {
                        _toasterService.Add(new GXToast("Agent group updeted", ToString(groups), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.WorkflowUpdate), groups);
                    });
                    hubConnection.On<IEnumerable<GXWorkflow>>(nameof(IGXHubEvents.WorkflowDelete), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Workflow deleted", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.WorkflowDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXWorkflowGroup>>(nameof(IGXHubEvents.WorkflowGroupUpdate), async (groups) =>
                    {
                        _toasterService.Add(new GXToast("Workflow group updated", ToString(groups), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.WorkflowGroupUpdate), groups);
                    });
                    hubConnection.On<IEnumerable<GXWorkflowGroup>>(nameof(IGXHubEvents.WorkflowGroupDelete), async (groups) =>
                    {
                        _toasterService.Add(new GXToast("Workflow group deleted", ToString(groups), Color.Success, 15));
                        await ChangedAsync(nameof(IGXHubEvents.WorkflowGroupDelete), groups);
                    });
                    hubConnection.On<IEnumerable<GXTrigger>>(nameof(IGXHubEvents.TriggerUpdate), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Trigger updated", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.TriggerUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXTrigger>>(nameof(IGXHubEvents.TriggerDelete), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Trigger deleted", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.TriggerDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXTriggerGroup>>(nameof(IGXHubEvents.TriggerGroupUpdate), async (groups) =>
                    {
                        _toasterService.Add(new GXToast("Trigger group updated", ToString(groups), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.TriggerGroupUpdate), groups);
                    });
                    hubConnection.On<IEnumerable<GXTriggerGroup>>(nameof(IGXHubEvents.TriggerGroupDelete), async (groups) =>
                    {
                        _toasterService.Add(new GXToast("Trigger group deleted", ToString(groups), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.TriggerGroupDelete), groups);
                    });
                    hubConnection.On<IEnumerable<GXModule>>(nameof(IGXHubEvents.ModuleUpdate), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Module updated", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.ModuleUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXModule>>(nameof(IGXHubEvents.ModuleDelete), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Module deleted", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.ModuleDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXModuleGroup>>(nameof(IGXHubEvents.ModuleGroupUpdate), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Module group updated", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.ModuleGroupUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXModuleGroup>>(nameof(IGXHubEvents.ModuleGroupDelete), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Module group deleted", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.ModuleGroupDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXObject>>(nameof(IGXHubEvents.ObjectUpdate), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Object updated", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.ObjectUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXObject>>(nameof(IGXHubEvents.ObjectDelete), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Object deleted", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.ObjectDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXAttribute>>(nameof(IGXHubEvents.AttributeUpdate), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Attribute updated", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.AttributeUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXAttribute>>(nameof(IGXHubEvents.AttributeDelete), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Attribute deleted", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.AttributeDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXAttribute>>(nameof(IGXHubEvents.ValueUpdate), async (attributes) =>
                    {
                        _toasterService.Add(new GXToast("Value updated", ToString(attributes), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.ValueUpdate), attributes);
                    });

                    hubConnection.On<IEnumerable<GXTask>>(nameof(IGXHubEvents.TaskAdd), async (tasks) =>
                    {
                        _toasterService.Add(new GXToast("Task added", ToString(tasks), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.TaskAdd), tasks);
                    });
                    hubConnection.On<IEnumerable<GXTask>>(nameof(IGXHubEvents.TaskUpdate), async (tasks) =>
                    {
                        _toasterService.Add(new GXToast("Task updated", ToString(tasks), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.TaskUpdate), tasks);
                    });
                    hubConnection.On<IEnumerable<GXTask>>(nameof(IGXHubEvents.TaskDelete), async (tasks) =>
                    {
                        _toasterService.Add(new GXToast("Task deleted", ToString(tasks), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.TaskDelete), tasks);
                    });
                    hubConnection.On<IEnumerable<GXUser>?>(nameof(IGXHubEvents.TaskClear), async (users) =>
                    {
                        if (users == null || !users.Any())
                        {
                            _toasterService.Add(new GXToast("Tasks cleared", "All tasks cleared.", Color.Info, 15));
                        }
                        else
                        {
                            _toasterService.Add(new GXToast("Tasks cleared", ToString(users), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.TaskClear), users);
                    });
                    hubConnection.On<IEnumerable<GXBlock>>(nameof(IGXHubEvents.BlockUpdate), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Block updated", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.BlockUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXBlock>>(nameof(IGXHubEvents.BlockDelete), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Block deleted", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.BlockDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXBlock>>(nameof(IGXHubEvents.BlockClose), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Block closed", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.BlockClose), items);
                    });
                    hubConnection.On<IEnumerable<GXBlockGroup>>(nameof(IGXHubEvents.BlockGroupUpdate), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Block group updated", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.BlockGroupUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXBlockGroup>>(nameof(IGXHubEvents.BlockGroupDelete), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Block group deleted", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.BlockGroupDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXUserAction>>(nameof(IGXHubEvents.UserActionAdd), async (items) =>
                    {
                        _toasterService.Add(new GXToast("User action added", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.UserActionAdd), items);
                    });
                    hubConnection.On<IEnumerable<GXUserAction>>(nameof(IGXHubEvents.UserActionDelete), async (items) =>
                    {
                        _toasterService.Add(new GXToast("User action deleted", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.UserActionDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXUser>?>(nameof(IGXHubEvents.UserActionClear), async (users) =>
                    {
                        if (users == null || !users.Any())
                        {
                            _toasterService.Add(new GXToast("User action clear", "All actions cleared.", Color.Info, 15));
                        }
                        else
                        {
                            _toasterService.Add(new GXToast("User action clear", ToString(users), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.UserActionClear), users);
                    });
                    hubConnection.On<IEnumerable<GXComponentView>>(nameof(IGXHubEvents.ComponentViewUpdate), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Component view updated", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.ComponentViewUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXComponentView>>(nameof(IGXHubEvents.ComponentViewDelete), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Component view deleted", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.ComponentViewDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXComponentViewGroup>>(nameof(IGXHubEvents.ComponentViewGroupUpdate), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Component view group updated", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.ComponentViewGroupUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXComponentViewGroup>>(nameof(IGXHubEvents.ComponentViewGroupDelete), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Component view group deleted", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.ComponentViewGroupDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXScript>>(nameof(IGXHubEvents.ScriptUpdate), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Script updated", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.ScriptUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXScript>>(nameof(IGXHubEvents.ScriptDelete), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Script deleted", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.ScriptDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXScriptGroup>>(nameof(IGXHubEvents.ScriptGroupUpdate), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Script group updated", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.ScriptGroupUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXScriptGroup>>(nameof(IGXHubEvents.ScriptGroupDelete), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Script group deleted", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.ScriptGroupDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXDeviceTrace>>(nameof(IGXHubEvents.DeviceTraceAdd), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Device trace added", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.DeviceTraceAdd), items);
                    });
                    hubConnection.On<IEnumerable<GXDevice>>(nameof(IGXHubEvents.DeviceTraceClear), async (devices) =>
                    {
                        if (devices == null || !devices.Any())
                        {
                            _toasterService.Add(new GXToast("Device traces clear", "All errors cleared.", Color.Info, 15));
                        }
                        else
                        {
                            _toasterService.Add(new GXToast("Device traces clear", ToString(devices), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.DeviceTraceClear), devices);
                    });
                    hubConnection.On<IEnumerable<GXDeviceAction>>(nameof(IGXHubEvents.DeviceActionAdd), async (items) =>
                    {
                        _toasterService.Add(new GXToast("Device action added", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.DeviceActionAdd), items);
                    });
                    hubConnection.On<IEnumerable<GXDevice>>(nameof(IGXHubEvents.DeviceActionClear), async (devices) =>
                    {
                        if (devices == null || !devices.Any())
                        {
                            _toasterService.Add(new GXToast("Device actions clear", "All errors cleared.", Color.Info, 15));
                        }
                        else
                        {
                            _toasterService.Add(new GXToast("Device actions clear", ToString(devices), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.DeviceActionClear), devices);
                    });
                    hubConnection.On<IEnumerable<GXRestStatistic>>(nameof(IGXHubEvents.RestStatisticAdd), async (items) =>
                    {
                        _toasterService.Add(new GXToast("REST statistic added", ToString(items), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.RestStatisticAdd), items);
                    });
                    hubConnection.On<IEnumerable<GXUser>>(nameof(IGXHubEvents.RestStatisticClear), async (users) =>
                    {
                        if (users == null || !users.Any())
                        {
                            _toasterService.Add(new GXToast("REST statistics clear", "All statistics cleared.", Color.Info, 15));
                        }
                        else
                        {
                            _toasterService.Add(new GXToast("REST statistics clear", ToString(users), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.RestStatisticClear), users);
                    });
                    hubConnection.On<IEnumerable<GXLanguage>>(nameof(IGXHubEvents.LanguageUpdate), async (languages) =>
                    {
                        _toasterService.Add(new GXToast("Language updated", ToString(languages), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.LanguageUpdate), languages);
                    });

                    hubConnection.On(nameof(IGXHubEvents.CronStart), async () =>
                    {
                        _toasterService.Add(new GXToast("Cron started", DateTime.Now.ToString(), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.CronStart));
                    });
                    hubConnection.On(nameof(IGXHubEvents.CronCompleate), async () =>
                    {
                        _toasterService.Add(new GXToast("Cron compleated", DateTime.Now.ToString(), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.CronCompleate));
                    });
                    hubConnection.On<IEnumerable<GXRole>>(nameof(IGXHubEvents.RoleUpdate), async (roles) =>
                    {
                        _toasterService.Add(new GXToast("Role updated", ToString(roles), Color.Info, 15));
                        await ChangedAsync(nameof(IGXHubEvents.RoleUpdate));
                    });
                    hubConnection.On<IEnumerable<GXRole>>(nameof(IGXHubEvents.RoleDelete), async (roles) =>
                    {
                        _toasterService.Add(new GXToast("Role deleted", ToString(roles), Color.Warning, 15));
                        await ChangedAsync(nameof(IGXHubEvents.RoleDelete));
                    });
                    await hubConnection.StartAsync();
                }
            }
            catch (Exception)
            {
                hubConnection = null;
            }
        }

        /// <summary>
        /// Stop listening the server events.
        /// </summary>
        /// <returns></returns>
        public async Task StopAsync()
        {
            if (hubConnection != null)
            {
                await hubConnection.StopAsync();
                hubConnection = null;
            }
        }

        /// <summary>
        /// Buttons are updated.
        /// </summary>
        public event Action? OnUpdateButtons;
        /// <summary>
        /// Page has changed.
        /// </summary>
        public event Action? OnPageChanged;
        /// <summary>
        /// Progress has started.
        /// </summary>
        public event Action? OnProgressStart;
        /// <summary>
        /// Progress has ended.
        /// </summary>
        public event Action? OnProgressEnd;
        /// <summary>
        /// Status has cleared.
        /// </summary>
        public event Action? OnClearStatus;
        /// <summary>
        /// Exception has occurred.
        /// </summary>
        public Action<Exception> OnProcessError { get; set; }
        /// <summary>
        /// Script logs has occurred.
        /// </summary>
        public Action<IEnumerable<object>> OnProcessErrors { get; set; }
        /// <summary>
        /// Show information.
        /// </summary>
        public Action<string, bool> OnShowInformation { get; set; }
        /// <summary>
        /// Maintenance mode is updated.
        /// </summary>
        public Action<bool> OnMaintenanceMode { get; set; }
    }
}
