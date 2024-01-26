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
using Gurux.DLMS.AMI.Client.Helpers.Toaster;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Components;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs.Subtotal;
using static System.Net.WebRequestMethods;
using System;
using Gurux.DLMS.AMI.Client.Pages.Config;
using Gurux.DLMS.AMI.Client.Pages.Admin;
using Microsoft.VisualBasic;
using System.Diagnostics;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.User;

namespace Gurux.DLMS.AMI.Client
{
    /// <summary>
    /// Notification service is used to show the top menu, occurred exceptions and progress indicator.
    /// </summary>
    public class GXNotifierService : ComponentBase, IGXNotifier2
    {
        private readonly ConcurrentDictionary<string, List<EventHandler>> _handlers = new ConcurrentDictionary<string, List<EventHandler>>(StringComparer.Ordinal);

        /// <summary>
        /// Url is saved before the page is changed.
        /// </summary>
        public string? LastUrl { get; set; }
        private Dictionary<string, object?> dataList;
        private readonly ILogger _logger;
        private readonly IGXToasterService _toasterService;

        public void ChangePage(string page)
        {
            LastUrl = page;
            if (OnPageChanged != null)
            {
                OnPageChanged?.Invoke();
            }
        }

        /// <summary>
        /// Aount of the rows per page.
        /// </summary>
        public int RowsPerPage { get; set; } = 10;

        /// <summary>
        /// Ignored notifications.
        /// </summary>
        public string[]? IgnoreNotification;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXNotifierService(IServiceProvider services)
        {
            ILoggerFactory lf = services.GetRequiredService<ILoggerFactory>();
            _logger = lf.CreateLogger(nameof(GXNotifierService));
            _toasterService = services.GetRequiredService<IGXToasterService>();
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
            LastUrl = null;
        }

        /// <inheritdoc />
        public void UpdateData(string page, object? data)
        {
            dataList[page] = data;
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

        /// <inheritdoc />
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

        /// <summary>
        /// List of user stamps.
        /// </summary>
        List<GXUserStamp> _stamps = new List<GXUserStamp>();

        public async Task UpdateUserStamp(
            string type,
            DateTime? datetime,
            int errors,
            int warnings,
            int informational,
            int verbose)
        {
            GXUserStamp? stamp = null;
            lock (_stamps)
            {
                foreach (var it in _stamps)
                {
                    if (it.TargetType == type)
                    {
                        if (datetime > it.CreationTime)
                        {
                            it.CreationTime = datetime;
                            it.Errors += errors;
                            it.Warnings += warnings;
                            it.Informational += informational;
                            it.Verboses += verbose;
                            stamp = it;
                            break;
                        }
                        else
                        {
                            return;
                        }
                    }
                }
                if (stamp == null)
                {
                    //New user stamp.
                    stamp = new GXUserStamp()
                    {
                        TargetType = type,
                        CreationTime = datetime,
                        Errors = errors,
                        Warnings = warnings,
                        Informational = informational,
                        Verboses = verbose
                    };
                    _stamps.Add(stamp);
                }
            }
            await ChangedAsync(nameof(IGXHubEvents.StampUpdate), new GXUserStamp[] { stamp });
        }

        ///<inheritdoc />
        public async Task Mark(HttpClient http, string type, DateTime? datetime)
        {
            bool reset = false;
            try
            {
                if (datetime != null)
                {
                    //Update user stamp.
                    GXUserStamp? stamp = null;
                    lock (_stamps)
                    {
                        foreach (var it in _stamps)
                        {
                            if (it.TargetType == type)
                            {
                                if (it.CreationTime >= datetime)
                                {
                                    //Reset counters if user stamp is already marked.
                                    it.Errors = 0;
                                    it.Warnings = 0;
                                    it.Informational = 0;
                                    it.Verboses = 0;
                                    reset = true;
                                }
                                it.CreationTime = datetime;
                                stamp = it;
                                break;
                            }
                        }
                    }
                    if (stamp == null)
                    {
                        stamp = new GXUserStamp()
                        {
                            TargetType = type,
                            CreationTime = datetime
                        };
                    }
                    await ChangedAsync(nameof(IGXHubEvents.StampUpdate), new GXUserStamp[] { stamp });
                    if (!reset)
                    {
                        UpdateUserStamp req = new UpdateUserStamp();
                        req.Stamps = new GXUserStamp[] { stamp };
                        await http.PostAsJson<ListUserStampsResponse>("api/UserStamp/Update", req);
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        /// <inheritdoc/>
        public GXUserStamp GetUserStamps(string type)
        {
            lock (_stamps)
            {
                var item = _stamps.Where(w => w.TargetType == type).SingleOrDefault();
                if (item == null)
                {
                    item = new GXUserStamp();
                }
                return item;
            }
        }

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

        bool IsIgnored(string type)
        {
            return IgnoreNotification == null || IgnoreNotification.Contains(type);
        }

        public async Task StartAsync(ClaimsPrincipal User, HttpClient http, NavigationManager navigationManager, IAccessTokenProvider accessTokenProvider)
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
                    //Load performance settings from the server.
                    ListConfiguration req = new ListConfiguration()
                    {
                        Filter = new GXConfiguration()
                        {
                            Name = GXConfigurations.Performance
                        }
                    };
                    GetUserResponse? ret = await http.GetAsJsonAsync<GetUserResponse>("api/User");
                    var settings = ret.Item?.Settings?.Where(w => w.Name == GXConfigurations.Performance).SingleOrDefault();
                    if (!string.IsNullOrEmpty(settings?.Value))
                    {
                        try
                        {
                            var s = JsonSerializer.Deserialize<PerformanceSettings>(settings.Value);
                            if (s != null)
                            {
                                IgnoreNotification = s.IgnoreNotification;
                            }
                            else
                            {
                                //All notifications are ignored as a default.
                                IgnoreNotification = null;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex.Message);
                            IgnoreNotification = null;
                        }
                    }
                    else
                    {
                        //All notifications are ignored as a default.
                        IgnoreNotification = null;
                    }
                    _logger.LogInformation("IgnoreNotification: " + IgnoreNotification);
                    settings = ret.Item?.Settings?.Where(w => w.Name == GXConfigurations.System).SingleOrDefault();
                    if (!string.IsNullOrEmpty(settings?.Value))
                    {
                        try
                        {
                            var s = JsonSerializer.Deserialize<SystemSettings>(settings.Value);
                            if (s != null)
                            {
                                RowsPerPage = s.RowsPerPage;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex.Message);
                            RowsPerPage = 10;
                        }
                    }
                    hubConnection.On<GXModule>(nameof(IGXHubEvents.ModuleSettingsSave), async (module) =>
                    {
                        _toasterService.Add(new GXToast("Module settings updated.", Convert.ToString(module), Color.Success, 15));
                        await ChangedAsync(nameof(IGXHubEvents.ModuleSettingsSave), module);
                    });
                    hubConnection.On(nameof(IGXHubEvents.ClearSystemLogs), async () =>
                    {
                        if (!IsIgnored(TargetType.SystemLog))
                        {
                            _toasterService.Add(new GXToast("System log cleared", "System log cleared.", Color.Success, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ClearSystemLogs));
                        await UpdateUserStamp(TargetType.SystemLog,
                       null,
                       0,
                       0,
                       0,
                       0);
                    });
                    hubConnection.On<IEnumerable<GXSystemLog>>(nameof(IGXHubEvents.AddSystemLogs), async (items) =>
                    {
                        if (!IsIgnored(TargetType.SystemLog))
                        {
                            _toasterService.Add(new GXToast("System log item added", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.AddSystemLogs), items);
                        await UpdateUserStamp(TargetType.SystemLog,
                        items.Max(s => s.CreationTime),
                        items.Count(),
                        0,
                        0,
                        0);
                    });
                    hubConnection.On<IEnumerable<GXSystemLog>>(nameof(IGXHubEvents.CloseSystemLogs), async (items) =>
                    {
                        if (!IsIgnored(TargetType.SystemLog))
                        {
                            _toasterService.Add(new GXToast("System log item closed", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.CloseSystemLogs), items);
                    });
                    hubConnection.On<IEnumerable<GXDeviceError>?>(nameof(IGXHubEvents.ClearDeviceErrors), async (devices) =>
                    {
                        if (!IsIgnored(TargetType.DeviceError))
                        {
                            if (devices == null || !devices.Any())
                            {
                                _toasterService.Add(new GXToast("Device error cleared", "All errors cleared.", Color.Info, 15));
                            }
                            else
                            {
                                _toasterService.Add(new GXToast("Device error clear", ToString(devices), Color.Info, 15));
                            }
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ClearDeviceErrors), devices);
                    });
                    hubConnection.On<IEnumerable<GXDeviceError>>(nameof(IGXHubEvents.AddDeviceErrors), async (errors) =>
                    {
                        if (!IsIgnored(TargetType.DeviceError))
                        {
                            _toasterService.Add(new GXToast("Device error added", ToString(errors), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.AddDeviceErrors), errors);
                        await UpdateUserStamp(TargetType.DeviceError,
                            errors.Max(s => s.CreationTime),
                            errors.Where(w => w.Level == (int)TraceLevel.Error).Count(),
                            errors.Where(w => w.Level == (int)TraceLevel.Warning).Count(),
                            errors.Where(w => w.Level == (int)TraceLevel.Info).Count(),
                            errors.Where(w => w.Level == (int)TraceLevel.Verbose).Count());
                    });
                    hubConnection.On<IEnumerable<GXDeviceError>>(nameof(IGXHubEvents.CloseDeviceErrors), async (errors) =>
                    {
                        if (!IsIgnored(TargetType.DeviceError))
                        {
                            _toasterService.Add(new GXToast("Device error closed", ToString(errors), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.CloseDeviceErrors), errors);
                    });
                    hubConnection.On<IEnumerable<GXWorkflow>?>(nameof(IGXHubEvents.ClearWorkflowLogs), async (workflows) =>
                    {
                        if (!IsIgnored(TargetType.WorkflowLog))
                        {
                            if (workflows == null || !workflows.Any())
                            {
                                _toasterService.Add(new GXToast("Workflow log cleared", "All log cleared.", Color.Info, 15));
                            }
                            else
                            {
                                _toasterService.Add(new GXToast("Workflow log cleared", ToString(workflows), Color.Info, 15));
                            }
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ClearWorkflowLogs), workflows);
                    });
                    hubConnection.On<IEnumerable<GXWorkflowLog>>(nameof(IGXHubEvents.AddWorkflowLogs), async (errors) =>
                    {
                        if (!IsIgnored(TargetType.WorkflowLog))
                        {
                            _toasterService.Add(new GXToast("Workflow log added", ToString(errors), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.AddWorkflowLogs), errors);
                        await UpdateUserStamp(TargetType.DeviceError,
                            errors.Max(s => s.CreationTime),
                            errors.Where(w => w.Level == (int)TraceLevel.Error).Count(),
                            errors.Where(w => w.Level == (int)TraceLevel.Warning).Count(),
                            errors.Where(w => w.Level == (int)TraceLevel.Info).Count(),
                            errors.Where(w => w.Level == (int)TraceLevel.Verbose).Count());
                    });
                    hubConnection.On<IEnumerable<GXWorkflowLog>>(nameof(IGXHubEvents.CloseWorkflowLogs), async (errors) =>
                    {
                        if (!IsIgnored(TargetType.WorkflowLog))
                        {
                            _toasterService.Add(new GXToast("Workflow log closed", ToString(errors), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.CloseWorkflowLogs), errors);
                    });

                    hubConnection.On<IEnumerable<GXGateway>?>(nameof(IGXHubEvents.ClearGatewayLogs), async (gateways) =>
                    {
                        if (!IsIgnored(TargetType.GatewayLog))
                        {
                            if (gateways == null || !gateways.Any())
                            {
                                _toasterService.Add(new GXToast("Gateway log cleared", "All log cleared.", Color.Info, 15));
                            }
                            else
                            {
                                _toasterService.Add(new GXToast("Gateway log cleared", ToString(gateways), Color.Info, 15));
                            }
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ClearGatewayLogs), gateways);
                    });
                    hubConnection.On<IEnumerable<GXGatewayLog>>(nameof(IGXHubEvents.AddGatewayLogs), async (errors) =>
                    {
                        if (!IsIgnored(TargetType.GatewayLog))
                        {
                            _toasterService.Add(new GXToast("Gateway log added", ToString(errors), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.AddGatewayLogs), errors);
                        await UpdateUserStamp(TargetType.DeviceError,
                            errors.Max(s => s.CreationTime),
                            errors.Where(w => w.Level == (int)TraceLevel.Error).Count(),
                            errors.Where(w => w.Level == (int)TraceLevel.Warning).Count(),
                            errors.Where(w => w.Level == (int)TraceLevel.Info).Count(),
                            errors.Where(w => w.Level == (int)TraceLevel.Verbose).Count());
                    }
                    );
                    hubConnection.On<IEnumerable<GXGatewayLog>>(nameof(IGXHubEvents.CloseGatewayLogs), async (errors) =>
                    {
                        if (!IsIgnored(TargetType.GatewayLog))
                        {
                            _toasterService.Add(new GXToast("Gateway log closed", ToString(errors), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.CloseGatewayLogs), errors);
                    }
                    );

                    hubConnection.On<IEnumerable<GXScript>>(nameof(IGXHubEvents.ClearScriptLogs), async (scripts) =>
                        {
                            if (!IsIgnored(TargetType.ScriptLog))
                            {
                                if (scripts == null || !scripts.Any())
                                {
                                    _toasterService.Add(new GXToast("Script log cleared", "All log cleared.", Color.Info, 15));
                                }
                                else
                                {
                                    _toasterService.Add(new GXToast("Script log cleared", ToString(scripts), Color.Info, 15));
                                }
                            }
                            await ChangedAsync(nameof(IGXHubEvents.ClearScriptLogs), scripts);
                        });
                    hubConnection.On<IEnumerable<GXScriptLog>>(nameof(IGXHubEvents.AddScriptLogs), async (errors) =>
                    {
                        if (!IsIgnored(TargetType.ScriptLog))
                        {
                            _toasterService.Add(new GXToast("Script log added", ToString(errors), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.AddScriptLogs), errors);
                    });
                    hubConnection.On<IEnumerable<GXScriptLog>>(nameof(IGXHubEvents.CloseScriptLogs), async (errors) =>
                    {
                        if (!IsIgnored(TargetType.ScriptLog))
                        {
                            _toasterService.Add(new GXToast("Script log closed", ToString(errors), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.CloseScriptLogs), errors);
                    });
                    hubConnection.On<IEnumerable<GXUser>>(nameof(IGXHubEvents.ClearUserErrors), async (users) =>
                    {
                        if (!IsIgnored(TargetType.UserError))
                        {
                            if (users == null || !users.Any())
                            {
                                _toasterService.Add(new GXToast("User errors cleared", "All errors cleared.", Color.Info, 15));
                            }
                            else
                            {
                                _toasterService.Add(new GXToast("User error cleared", ToString(users), Color.Info, 15));
                            }
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ClearUserErrors), users);
                    });
                    hubConnection.On<IEnumerable<GXUserError>>(nameof(IGXHubEvents.AddUserErrors), async (errors) =>
                    {
                        if (!IsIgnored(TargetType.UserError))
                        {
                            _toasterService.Add(new GXToast("User error added", ToString(errors), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.AddUserErrors), errors);
                    });
                    hubConnection.On<IEnumerable<GXUserError>>(nameof(IGXHubEvents.CloseUserErrors), async (errors) =>
                    {
                        if (!IsIgnored(TargetType.UserError))
                        {
                            _toasterService.Add(new GXToast("User error closed", ToString(errors), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.CloseUserErrors), errors);
                    });
                    hubConnection.On<IEnumerable<GXModule>>(nameof(IGXHubEvents.ClearModuleLogs), async (modules) =>
                    {
                        if (!IsIgnored(TargetType.ModuleLog))
                        {
                            if (modules == null || !modules.Any())
                            {
                                _toasterService.Add(new GXToast("Module logs cleared", "All logs cleared.", Color.Info, 15));
                            }
                            else
                            {
                                _toasterService.Add(new GXToast("Module logs cleared", ToString(modules), Color.Info, 15));
                            }
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ClearModuleLogs), modules);
                    });
                    hubConnection.On<IEnumerable<GXModuleLog>>(nameof(IGXHubEvents.AddModuleLogs), async (logs) =>
                    {
                        if (!IsIgnored(TargetType.ModuleLog))
                        {
                            _toasterService.Add(new GXToast("Module log added", ToString(logs), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.AddModuleLogs), logs);
                        await UpdateUserStamp(TargetType.ModuleLog,
                            logs.Max(s => s.CreationTime),
                            logs.Where(w => w.Level == (int)TraceLevel.Error).Count(),
                            logs.Where(w => w.Level == (int)TraceLevel.Warning).Count(),
                            logs.Where(w => w.Level == (int)TraceLevel.Info).Count(),
                            logs.Where(w => w.Level == (int)TraceLevel.Verbose).Count());
                    });
                    hubConnection.On<IEnumerable<GXModuleLog>>(nameof(IGXHubEvents.CloseModuleLogs), async (errors) =>
                    {
                        if (!IsIgnored(TargetType.ModuleLog))
                        {
                            _toasterService.Add(new GXToast("Module log closed", ToString(errors), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.CloseModuleLogs), errors);
                    });
                    hubConnection.On<IEnumerable<GXAgent>?>(nameof(IGXHubEvents.ClearAgentLogs), async (logs) =>
                    {
                        if (!IsIgnored(TargetType.AgentLog))
                        {
                            if (!IsIgnored(TargetType.Agent))
                            {
                                if (logs == null || !logs.Any())
                                {
                                    _toasterService.Add(new GXToast("Agent logs cleared", "All logs cleared.", Color.Info, 15));
                                }
                                else
                                {
                                    _toasterService.Add(new GXToast("Agent log clered", ToString(logs), Color.Info, 15));
                                }
                            }
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ClearAgentLogs), logs);
                    });
                    hubConnection.On<IEnumerable<GXAgentLog>>(nameof(IGXHubEvents.AddAgentLogs), async (logs) =>
                    {
                        if (!IsIgnored(TargetType.AgentLog))
                        {
                            _toasterService.Add(new GXToast("Agent log added", ToString(logs), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.AddAgentLogs), logs);
                        await UpdateUserStamp(TargetType.AgentLog,
                            logs.Max(s => s.CreationTime),
                            logs.Where(w => w.Level == (int)TraceLevel.Error).Count(),
                            logs.Where(w => w.Level == (int)TraceLevel.Warning).Count(),
                            logs.Where(w => w.Level == (int)TraceLevel.Info).Count(),
                            logs.Where(w => w.Level == (int)TraceLevel.Verbose).Count());
                    });
                    hubConnection.On<IEnumerable<GXAgentLog>>(nameof(IGXHubEvents.CloseAgentLogs), async (items) =>
                    {
                        if (!IsIgnored(TargetType.AgentLog))
                        {
                            _toasterService.Add(new GXToast("Agent log closed", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.CloseAgentLogs), items);
                    });
                    hubConnection.On<IEnumerable<GXSchedule>>(nameof(IGXHubEvents.ScheduleUpdate), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Schedule))
                        {
                            _toasterService.Add(new GXToast("Schedule updated", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ScheduleUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXSchedule>>(nameof(IGXHubEvents.ScheduleDelete), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Schedule))
                        {
                            _toasterService.Add(new GXToast("Schedule delete", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ScheduleDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXSchedule>>(nameof(IGXHubEvents.ScheduleStart), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Schedule))
                        {
                            _toasterService.Add(new GXToast("Schedule start", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ScheduleStart), items);
                    });
                    hubConnection.On<IEnumerable<GXSchedule>>(nameof(IGXHubEvents.ScheduleCompleate), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Schedule))
                        {
                            _toasterService.Add(new GXToast("Schedule compleate", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ScheduleCompleate), items);
                    });
                    hubConnection.On<IEnumerable<GXScheduleLog>>(nameof(IGXHubEvents.AddScheduleLog), async (items) =>
                    {
                        if (!IsIgnored(TargetType.ScheduleLog))
                        {
                            _toasterService.Add(new GXToast("Schedule log added", ToString(items), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.AddScheduleLog), items);
                        await UpdateUserStamp(TargetType.ScheduleLog,
                            items.Max(s => s.CreationTime),
                            items.Where(w => w.Level == (int)TraceLevel.Error).Count(),
                            items.Where(w => w.Level == (int)TraceLevel.Warning).Count(),
                            items.Where(w => w.Level == (int)TraceLevel.Info).Count(),
                            items.Where(w => w.Level == (int)TraceLevel.Verbose).Count());
                    });
                    hubConnection.On<IEnumerable<GXSchedule>?>(nameof(IGXHubEvents.ClearScheduleLog), async (schedules) =>
                    {
                        if (!IsIgnored(TargetType.ScheduleLog))
                        {
                            if (schedules == null || !schedules.Any())
                            {
                                _toasterService.Add(new GXToast("Schedule errors cleared", "All errors cleared.", Color.Info, 15));
                            }
                            else
                            {
                                _toasterService.Add(new GXToast("Schedule log clear", ToString(schedules), Color.Info, 15));
                            }
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ClearScheduleLog), schedules);
                    });
                    hubConnection.On<IEnumerable<GXScheduleLog>>(nameof(IGXHubEvents.CloseScheduleLog), async (items) =>
                    {
                        if (!IsIgnored(TargetType.ScheduleLog))
                        {
                            _toasterService.Add(new GXToast("Schedule log closed", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.CloseScheduleLog), items);
                    });
                    hubConnection.On<IEnumerable<GXScheduleGroup>>(nameof(IGXHubEvents.ScheduleGroupUpdate), async (groups) =>
                    {
                        if (!IsIgnored(TargetType.ScheduleGroup))
                        {
                            _toasterService.Add(new GXToast("Schedule group updated", ToString(groups), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ScheduleGroupUpdate), groups);
                    });
                    hubConnection.On<IEnumerable<GXScheduleGroup>>(nameof(IGXHubEvents.ScheduleGroupDelete), async (groups) =>
                    {
                        if (!IsIgnored(TargetType.ScheduleGroup))
                        {
                            _toasterService.Add(new GXToast("Schedule group delete", ToString(groups), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ScheduleGroupDelete), groups);
                    });
                    hubConnection.On<IEnumerable<GXUser>>(nameof(IGXHubEvents.UserUpdate), async (users) =>
                    {
                        if (!IsIgnored(TargetType.User))
                        {
                            _toasterService.Add(new GXToast("User updated", ToString(users), Color.Info, 15));
                        }
                        foreach (var user in users)
                        {
                            if (user.Id == ClientHelpers.GetUserId(User))
                            {
                                //If user has updated performance settings.
                                var settings = ret.Item.Settings.Where(w => w.Name == GXConfigurations.Performance).SingleOrDefault();
                                if (settings != null && !string.IsNullOrEmpty(settings.Value))
                                {
                                    var s = JsonSerializer.Deserialize<PerformanceSettings>(settings.Value);
                                    if (s != null)
                                    {
                                        IgnoreNotification = s.IgnoreNotification;
                                    }
                                }
                            }
                        }
                        await ChangedAsync(nameof(IGXHubEvents.UserUpdate), users);
                    });
                    hubConnection.On<IEnumerable<GXUser>>(nameof(IGXHubEvents.UserDelete), async (users) =>
                    {
                        if (!IsIgnored(TargetType.User))
                        {
                            _toasterService.Add(new GXToast("User delete", ToString(users), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.UserDelete), users);
                    });
                    hubConnection.On<IEnumerable<GXUserGroup>>(nameof(IGXHubEvents.UserGroupUpdate), async (groups) =>
                    {
                        if (!IsIgnored(TargetType.UserGroup))
                        {
                            _toasterService.Add(new GXToast("User group update", ToString(groups), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.UserGroupUpdate), groups);
                    });
                    hubConnection.On<IEnumerable<GXUserGroup>>(nameof(IGXHubEvents.UserGroupDelete), async (groups) =>
                    {
                        if (!IsIgnored(TargetType.UserGroup))
                        {
                            _toasterService.Add(new GXToast("User group delete", ToString(groups), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.UserGroupDelete), groups);
                    });
                    hubConnection.On<IEnumerable<GXDevice>>(nameof(IGXHubEvents.DeviceUpdate), async (devices) =>
                    {
                        if (!IsIgnored(TargetType.Device))
                        {
                            _toasterService.Add(new GXToast("Device updated", ToString(devices), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.DeviceUpdate), devices);
                    });
                    hubConnection.On<IEnumerable<GXDevice>>(nameof(IGXHubEvents.DeviceDelete), async (devices) =>
                    {
                        if (!IsIgnored(TargetType.Device))
                        {
                            _toasterService.Add(new GXToast("Device deleted", ToString(devices), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.DeviceDelete), devices);
                    });
                    hubConnection.On<IEnumerable<GXDevice>>(nameof(IGXHubEvents.DeviceStatusChange), async (devices) =>
                    {
                        if (!IsIgnored(TargetType.Device))
                        {
                            _toasterService.Add(new GXToast("Device status changed", ToString(devices), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.DeviceStatusChange), devices);
                    });

                    hubConnection.On<IEnumerable<GXDeviceGroup>>(nameof(IGXHubEvents.DeviceGroupUpdate), async (groups) =>
                    {
                        if (!IsIgnored(TargetType.DeviceGroup))
                        {
                            _toasterService.Add(new GXToast("Device group updated", ToString(groups), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.DeviceGroupUpdate), groups);
                    });
                    hubConnection.On<IEnumerable<GXDeviceGroup>>(nameof(IGXHubEvents.DeviceGroupDelete), async (groups) =>
                    {
                        if (!IsIgnored(TargetType.DeviceGroup))
                        {
                            _toasterService.Add(new GXToast("Device group deleted", ToString(groups), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.DeviceGroupDelete), groups);
                    });
                    hubConnection.On<IEnumerable<GXDeviceTemplateGroup>>(nameof(IGXHubEvents.DeviceTemplateGroupUpdate), async (groups) =>
                    {
                        if (!IsIgnored(TargetType.DeviceTemplateGroup))
                        {
                            _toasterService.Add(new GXToast("Device template group updated", ToString(groups), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.DeviceTemplateGroupUpdate), groups);
                    });
                    hubConnection.On<IEnumerable<GXDeviceTemplateGroup>>(nameof(IGXHubEvents.DeviceTemplateGroupDelete), async (groups) =>
                    {
                        if (!IsIgnored(TargetType.DeviceTemplateGroup))
                        {
                            _toasterService.Add(new GXToast("Device template group deleted", ToString(groups), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.DeviceTemplateGroupDelete), groups);
                    });
                    hubConnection.On<IEnumerable<GXDeviceTemplate>>(nameof(IGXHubEvents.DeviceTemplateUpdate), async (items) =>
                    {
                        if (!IsIgnored(TargetType.DeviceTemplate))
                        {
                            _toasterService.Add(new GXToast("Device template updated", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.DeviceTemplateUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXDeviceTemplate>>(nameof(IGXHubEvents.DeviceTemplateDelete), async (items) =>
                    {
                        if (!IsIgnored(TargetType.DeviceTemplate))
                        {
                            _toasterService.Add(new GXToast("Device template deleted", ToString(items), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.DeviceTemplateDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXAgent>>(nameof(IGXHubEvents.AgentUpdate), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Agent))
                        {
                            _toasterService.Add(new GXToast("Agent updated", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.AgentUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXAgent>>(nameof(IGXHubEvents.AgentDelete), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Agent))
                        {
                            _toasterService.Add(new GXToast("Agent deleted", ToString(items), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.AgentDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXAgent>>(nameof(IGXHubEvents.AgentStatusChange), async (agents) =>
                    {
                        if (!IsIgnored(TargetType.Agent))
                        {
                            _toasterService.Add(new GXToast("Agent status changed", ToString(agents), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.AgentStatusChange), agents);
                    });
                    hubConnection.On<IEnumerable<GXAgentGroup>>(nameof(IGXHubEvents.AgentGroupUpdate), async (groups) =>
                    {
                        if (!IsIgnored(TargetType.AgentGroup))
                        {
                            _toasterService.Add(new GXToast("Agent group updated", ToString(groups), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.AgentGroupUpdate), groups);
                    });
                    hubConnection.On<IEnumerable<GXAgentGroup>>(nameof(IGXHubEvents.AgentGroupDelete), async (groups) =>
                    {
                        if (!IsIgnored(TargetType.AgentGroup))
                        {
                            _toasterService.Add(new GXToast("Agent group deleted", ToString(groups), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.AgentGroupDelete), groups);
                    });
                    hubConnection.On<IEnumerable<GXWorkflow>>(nameof(IGXHubEvents.WorkflowUpdate), async (groups) =>
                    {
                        if (!IsIgnored(TargetType.Workflow))
                        {
                            _toasterService.Add(new GXToast("Workflow updeted", ToString(groups), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.WorkflowUpdate), groups);
                    });
                    hubConnection.On<IEnumerable<GXWorkflow>>(nameof(IGXHubEvents.WorkflowDelete), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Workflow))
                        {
                            _toasterService.Add(new GXToast("Workflow deleted", ToString(items), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.WorkflowDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXWorkflowGroup>>(nameof(IGXHubEvents.WorkflowGroupUpdate), async (groups) =>
                    {
                        if (!IsIgnored(TargetType.Workflow))
                        {
                            _toasterService.Add(new GXToast("Workflow group updated", ToString(groups), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.WorkflowGroupUpdate), groups);
                    });
                    hubConnection.On<IEnumerable<GXWorkflowGroup>>(nameof(IGXHubEvents.WorkflowGroupDelete), async (groups) =>
                    {
                        if (!IsIgnored(TargetType.Workflow))
                        {
                            _toasterService.Add(new GXToast("Workflow group deleted", ToString(groups), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.WorkflowGroupDelete), groups);
                    });
                    hubConnection.On<IEnumerable<GXTrigger>>(nameof(IGXHubEvents.TriggerUpdate), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Trigger))
                        {
                            _toasterService.Add(new GXToast("Trigger updated", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.TriggerUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXTrigger>>(nameof(IGXHubEvents.TriggerDelete), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Trigger))
                        {
                            _toasterService.Add(new GXToast("Trigger deleted", ToString(items), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.TriggerDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXTriggerGroup>>(nameof(IGXHubEvents.TriggerGroupUpdate), async (groups) =>
                    {
                        if (!IsIgnored(TargetType.TriggerGroup))
                        {
                            _toasterService.Add(new GXToast("Trigger group updated", ToString(groups), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.TriggerGroupUpdate), groups);
                    });
                    hubConnection.On<IEnumerable<GXTriggerGroup>>(nameof(IGXHubEvents.TriggerGroupDelete), async (groups) =>
                    {
                        if (!IsIgnored(TargetType.TriggerGroup))
                        {
                            _toasterService.Add(new GXToast("Trigger group deleted", ToString(groups), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.TriggerGroupDelete), groups);
                    });
                    hubConnection.On<IEnumerable<GXModule>>(nameof(IGXHubEvents.ModuleUpdate), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Module))
                        {
                            _toasterService.Add(new GXToast("Module updated", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ModuleUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXModule>>(nameof(IGXHubEvents.ModuleDelete), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Module))
                        {
                            _toasterService.Add(new GXToast("Module deleted", ToString(items), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ModuleDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXModuleGroup>>(nameof(IGXHubEvents.ModuleGroupUpdate), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Module))
                        {
                            _toasterService.Add(new GXToast("Module group updated", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ModuleGroupUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXModuleGroup>>(nameof(IGXHubEvents.ModuleGroupDelete), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Module))
                        {
                            _toasterService.Add(new GXToast("Module group deleted", ToString(items), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ModuleGroupDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXObject>>(nameof(IGXHubEvents.ObjectUpdate), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Object))
                        {
                            _toasterService.Add(new GXToast("Object updated", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ObjectUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXObject>>(nameof(IGXHubEvents.ObjectDelete), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Object))
                        {
                            _toasterService.Add(new GXToast("Object deleted", ToString(items), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ObjectDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXAttribute>>(nameof(IGXHubEvents.AttributeUpdate), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Attribute))
                        {
                            _toasterService.Add(new GXToast("Attribute updated", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.AttributeUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXAttribute>>(nameof(IGXHubEvents.AttributeDelete), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Attribute))
                        {
                            _toasterService.Add(new GXToast("Attribute deleted", ToString(items), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.AttributeDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXAttribute>>(nameof(IGXHubEvents.ValueUpdate), async (attributes) =>
                    {
                        if (!IsIgnored(TargetType.Value))
                        {
                            _toasterService.Add(new GXToast("Value updated", ToString(attributes), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ValueUpdate), attributes);
                    });

                    hubConnection.On<IEnumerable<GXTask>>(nameof(IGXHubEvents.TaskAdd), async (tasks) =>
                    {
                        if (!IsIgnored(TargetType.Task))
                        {
                            _toasterService.Add(new GXToast("Task added", ToString(tasks), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.TaskAdd), tasks);
                        await UpdateUserStamp(TargetType.Task,
                            tasks.Max(s => s.CreationTime),
                            0,
                            0,
                            tasks.Count(),
                            0);
                    });
                    hubConnection.On<IEnumerable<GXTask>>(nameof(IGXHubEvents.TaskUpdate), async (tasks) =>
                    {
                        if (!IsIgnored(TargetType.Task))
                        {
                            _toasterService.Add(new GXToast("Task updated", ToString(tasks), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.TaskUpdate), tasks);
                    });
                    hubConnection.On<IEnumerable<GXTask>>(nameof(IGXHubEvents.TaskDelete), async (tasks) =>
                    {
                        if (!IsIgnored(TargetType.Task))
                        {
                            _toasterService.Add(new GXToast("Task deleted", ToString(tasks), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.TaskDelete), tasks);
                    });
                    hubConnection.On<IEnumerable<GXUser>?>(nameof(IGXHubEvents.TaskClear), async (users) =>
                    {
                        if (!IsIgnored(TargetType.Task))
                        {
                            if (users == null || !users.Any())
                            {
                                _toasterService.Add(new GXToast("Tasks cleared", "All tasks cleared.", Color.Info, 15));
                            }
                            else
                            {
                                _toasterService.Add(new GXToast("Tasks cleared", ToString(users), Color.Info, 15));
                            }
                        }
                        await ChangedAsync(nameof(IGXHubEvents.TaskClear), users);
                    });
                    hubConnection.On<IEnumerable<GXBlock>>(nameof(IGXHubEvents.BlockUpdate), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Block))
                        {
                            _toasterService.Add(new GXToast("Block updated", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.BlockUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXBlock>>(nameof(IGXHubEvents.BlockDelete), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Block))
                        {
                            _toasterService.Add(new GXToast("Block deleted", ToString(items), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.BlockDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXBlock>>(nameof(IGXHubEvents.BlockClose), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Block))
                        {
                            _toasterService.Add(new GXToast("Block closed", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.BlockClose), items);
                    });
                    hubConnection.On<IEnumerable<GXBlockGroup>>(nameof(IGXHubEvents.BlockGroupUpdate), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Block))
                        {
                            _toasterService.Add(new GXToast("Block group updated", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.BlockGroupUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXBlockGroup>>(nameof(IGXHubEvents.BlockGroupDelete), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Block))
                        {
                            _toasterService.Add(new GXToast("Block group deleted", ToString(items), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.BlockGroupDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXUserAction>>(nameof(IGXHubEvents.UserActionAdd), async (items) =>
                    {
                        if (!IsIgnored(TargetType.UserAction))
                        {
                            _toasterService.Add(new GXToast("User action added", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.UserActionAdd), items);
                    });
                    hubConnection.On<IEnumerable<GXUserAction>>(nameof(IGXHubEvents.UserActionDelete), async (items) =>
                    {
                        if (!IsIgnored(TargetType.UserAction))
                        {
                            _toasterService.Add(new GXToast("User action deleted", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.UserActionDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXUser>?>(nameof(IGXHubEvents.UserActionClear), async (users) =>
                    {
                        if (!IsIgnored(TargetType.UserAction))
                        {
                            if (users == null || !users.Any())
                            {
                                _toasterService.Add(new GXToast("User action clear", "All actions cleared.", Color.Info, 15));
                            }
                            else
                            {
                                _toasterService.Add(new GXToast("User action clear", ToString(users), Color.Info, 15));
                            }
                        }
                        await ChangedAsync(nameof(IGXHubEvents.UserActionClear), users);
                    });
                    hubConnection.On<IEnumerable<GXConfiguration>>(nameof(IGXHubEvents.ConfigurationSave), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Configuration))
                        {
                            _toasterService.Add(new GXToast("Configuration saved.", ToString(items), Color.Info, 15));
                        }
                        foreach (var conf in items)
                        {
                            //If rows per page is updated.
                            if (conf.Name == GXConfigurations.System && !string.IsNullOrEmpty(conf.Settings))
                            {
                                var s = JsonSerializer.Deserialize<SystemSettings>(conf.Settings);
                                if (s != null)
                                {
                                    RowsPerPage = s.RowsPerPage;
                                }
                            }
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ConfigurationSave), items);
                    });

                    hubConnection.On<IEnumerable<GXComponentView>>(nameof(IGXHubEvents.ComponentViewUpdate), async (items) =>
                    {
                        if (!IsIgnored(TargetType.ComponentView))
                        {
                            _toasterService.Add(new GXToast("Component view updated", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ComponentViewUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXComponentView>>(nameof(IGXHubEvents.ComponentViewDelete), async (items) =>
                    {
                        if (!IsIgnored(TargetType.ComponentView))
                        {
                            _toasterService.Add(new GXToast("Component view deleted", ToString(items), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ComponentViewDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXComponentViewGroup>>(nameof(IGXHubEvents.ComponentViewGroupUpdate), async (items) =>
                    {
                        if (!IsIgnored(TargetType.ComponentViewGroup))
                        {
                            _toasterService.Add(new GXToast("Component view group updated", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ComponentViewGroupUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXComponentViewGroup>>(nameof(IGXHubEvents.ComponentViewGroupDelete), async (items) =>
                    {
                        if (!IsIgnored(TargetType.ComponentViewGroup))
                        {
                            _toasterService.Add(new GXToast("Component view group deleted", ToString(items), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ComponentViewGroupDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXScript>>(nameof(IGXHubEvents.ScriptUpdate), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Script))
                        {
                            _toasterService.Add(new GXToast("Script updated", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ScriptUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXScript>>(nameof(IGXHubEvents.ScriptDelete), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Script))
                        {
                            _toasterService.Add(new GXToast("Script deleted", ToString(items), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ScriptDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXScriptGroup>>(nameof(IGXHubEvents.ScriptGroupUpdate), async (items) =>
                    {
                        if (!IsIgnored(TargetType.ScriptGroup))
                        {
                            _toasterService.Add(new GXToast("Script group updated", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ScriptGroupUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXScriptGroup>>(nameof(IGXHubEvents.ScriptGroupDelete), async (items) =>
                    {
                        if (!IsIgnored(TargetType.ScriptGroup))
                        {
                            _toasterService.Add(new GXToast("Script group deleted", ToString(items), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ScriptGroupDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXDeviceTrace>>(nameof(IGXHubEvents.DeviceTraceAdd), async (items) =>
                    {
                        if (!IsIgnored(TargetType.DeviceTrace))
                        {
                            _toasterService.Add(new GXToast("Device trace added", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.DeviceTraceAdd), items);
                    });
                    hubConnection.On<IEnumerable<GXDevice>>(nameof(IGXHubEvents.DeviceTraceClear), async (devices) =>
                    {
                        if (!IsIgnored(TargetType.DeviceTrace))
                        {
                            if (devices == null || !devices.Any())
                            {
                                _toasterService.Add(new GXToast("Device traces clear", "All errors cleared.", Color.Info, 15));
                            }
                            else
                            {
                                _toasterService.Add(new GXToast("Device traces clear", ToString(devices), Color.Info, 15));
                            }
                        }
                        await ChangedAsync(nameof(IGXHubEvents.DeviceTraceClear), devices);
                    });
                    hubConnection.On<IEnumerable<GXDeviceAction>>(nameof(IGXHubEvents.DeviceActionAdd), async (items) =>
                    {
                        if (!IsIgnored(TargetType.DeviceAction))
                        {
                            _toasterService.Add(new GXToast("Device action added", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.DeviceActionAdd), items);
                        await UpdateUserStamp(TargetType.DeviceAction,
                            items.Max(s => s.CreationTime),
                            0,
                            0,
                            items.Count(),
                            0);
                    });
                    hubConnection.On<IEnumerable<GXDevice>>(nameof(IGXHubEvents.DeviceActionClear), async (devices) =>
                    {
                        if (!IsIgnored(TargetType.DeviceAction))
                        {
                            if (devices == null || !devices.Any())
                            {
                                _toasterService.Add(new GXToast("Device actions clear", "All errors cleared.", Color.Info, 15));
                            }
                            else
                            {
                                _toasterService.Add(new GXToast("Device actions clear", ToString(devices), Color.Info, 15));
                            }
                        }
                        await ChangedAsync(nameof(IGXHubEvents.DeviceActionClear), devices);
                    });
                    hubConnection.On<IEnumerable<GXRestStatistic>>(nameof(IGXHubEvents.RestStatisticAdd), async (items) =>
                    {
                        if (!IsIgnored(TargetType.RestStatistic))
                        {
                            _toasterService.Add(new GXToast("REST statistic added", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.RestStatisticAdd), items);
                    });
                    hubConnection.On<IEnumerable<GXUser>>(nameof(IGXHubEvents.RestStatisticClear), async (users) =>
                    {
                        if (!IsIgnored(TargetType.RestStatistic))
                        {
                            if (users == null || !users.Any())
                            {
                                _toasterService.Add(new GXToast("REST statistics clear", "All statistics cleared.", Color.Info, 15));
                            }
                            else
                            {
                                _toasterService.Add(new GXToast("REST statistics clear", ToString(users), Color.Info, 15));
                            }
                        }
                        await ChangedAsync(nameof(IGXHubEvents.RestStatisticClear), users);
                    });
                    hubConnection.On<IEnumerable<GXLanguage>>(nameof(IGXHubEvents.LanguageUpdate), async (languages) =>
                    {
                        if (!IsIgnored(TargetType.Language))
                        {
                            _toasterService.Add(new GXToast("Language updated", ToString(languages), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.LanguageUpdate), languages);
                    });

                    hubConnection.On(nameof(IGXHubEvents.CronStart), async () =>
                    {
                        if (!IsIgnored(TargetType.Cron))
                        {
                            _toasterService.Add(new GXToast("Cron started", DateTime.Now.ToString(), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.CronStart));
                    });
                    hubConnection.On(nameof(IGXHubEvents.CronCompleate), async () =>
                    {
                        if (!IsIgnored(TargetType.Cron))
                        {
                            _toasterService.Add(new GXToast("Cron compleated", DateTime.Now.ToString(), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.CronCompleate));
                    });
                    hubConnection.On<IEnumerable<GXRole>>(nameof(IGXHubEvents.RoleUpdate), async (roles) =>
                    {
                        if (!IsIgnored(TargetType.Role))
                        {
                            _toasterService.Add(new GXToast("Role updated", ToString(roles), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.RoleUpdate), roles);
                    });
                    hubConnection.On<IEnumerable<GXRole>>(nameof(IGXHubEvents.RoleDelete), async (roles) =>
                    {
                        if (!IsIgnored(TargetType.Role))
                        {
                            _toasterService.Add(new GXToast("Role deleted", ToString(roles), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.RoleDelete), roles);
                    });

                    hubConnection.On<IEnumerable<GXObjectTemplate>>(nameof(IGXHubEvents.ObjectTemplateUpdate), async (templates) =>
                    {
                        if (!IsIgnored(TargetType.ObjectTemplate))
                        {
                            _toasterService.Add(new GXToast("Object template updated", ToString(templates), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ObjectTemplateUpdate), templates);
                    });
                    hubConnection.On<IEnumerable<GXObjectTemplate>>(nameof(IGXHubEvents.ObjectTemplateDelete), async (templates) =>
                    {
                        if (!IsIgnored(TargetType.ObjectTemplate))
                        {
                            _toasterService.Add(new GXToast("Object template deleted", ToString(templates), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ObjectTemplateDelete), templates);
                    });
                    hubConnection.On<IEnumerable<GXManufacturer>>(nameof(IGXHubEvents.ManufacturerUpdate), async (manufacturers) =>
                    {
                        if (!IsIgnored(TargetType.Manufacturer))
                        {
                            _toasterService.Add(new GXToast("Manufacturer updated", ToString(manufacturers), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ManufacturerUpdate), manufacturers);
                    });
                    hubConnection.On<IEnumerable<GXManufacturer>>(nameof(IGXHubEvents.ManufacturerDelete), async (manufacturers) =>
                    {
                        if (!IsIgnored(TargetType.Manufacturer))
                        {
                            _toasterService.Add(new GXToast("Manufacturer deleted", ToString(manufacturers), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ManufacturerDelete), manufacturers);
                    });

                    hubConnection.On<IEnumerable<GXManufacturerGroup>>(nameof(IGXHubEvents.ManufacturerGroupUpdate), async (manufacturers) =>
                    {
                        if (!IsIgnored(TargetType.ManufacturerGroup))
                        {
                            _toasterService.Add(new GXToast("Manufacturer group updated", ToString(manufacturers), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ManufacturerGroupUpdate), manufacturers);
                    });
                    hubConnection.On<IEnumerable<GXManufacturerGroup>>(nameof(IGXHubEvents.ManufacturerGroupDelete), async (manufacturers) =>
                    {
                        if (!IsIgnored(TargetType.ManufacturerGroup))
                        {
                            _toasterService.Add(new GXToast("Manufacturer group deleted", ToString(manufacturers), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ManufacturerGroupDelete), manufacturers);
                    });
                    hubConnection.On<IEnumerable<GXFavorite>>(nameof(IGXHubEvents.FavoriteUpdate), async (manufacturers) =>
                    {
                        if (!IsIgnored(TargetType.Favorite))
                        {
                            _toasterService.Add(new GXToast("Favorite updated", ToString(manufacturers), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.FavoriteUpdate), manufacturers);
                    });
                    hubConnection.On<IEnumerable<GXFavorite>>(nameof(IGXHubEvents.FavoriteDelete), async (manufacturers) =>
                    {
                        if (!IsIgnored(TargetType.Favorite))
                        {
                            _toasterService.Add(new GXToast("Favorite deleted", ToString(manufacturers), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.FavoriteDelete), manufacturers);
                    });
                    hubConnection.On<IEnumerable<GXKeyManagement>>(nameof(IGXHubEvents.KeyManagementUpdate), async (manufacturers) =>
                    {
                        if (!IsIgnored(TargetType.KeyManagement))
                        {
                            _toasterService.Add(new GXToast("Key management updated", ToString(manufacturers), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.KeyManagementUpdate), manufacturers);
                    });
                    hubConnection.On<IEnumerable<GXKeyManagement>>(nameof(IGXHubEvents.KeyManagementDelete), async (manufacturers) =>
                    {
                        if (!IsIgnored(TargetType.KeyManagement))
                        {
                            _toasterService.Add(new GXToast("Key management deleted", ToString(manufacturers), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.KeyManagementDelete), manufacturers);
                    });

                    hubConnection.On<IEnumerable<GXKeyManagementGroup>>(nameof(IGXHubEvents.KeyManagementGroupUpdate), async (manufacturers) =>
                    {
                        if (!IsIgnored(TargetType.KeyManagementGroup))
                        {
                            _toasterService.Add(new GXToast("Key management group updated", ToString(manufacturers), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.KeyManagementGroupUpdate), manufacturers);
                    });
                    hubConnection.On<IEnumerable<GXKeyManagementGroup>>(nameof(IGXHubEvents.KeyManagementGroupDelete), async (manufacturers) =>
                    {
                        if (!IsIgnored(TargetType.KeyManagementGroup))
                        {
                            _toasterService.Add(new GXToast("Key management group deleted", ToString(manufacturers), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.KeyManagementGroupDelete), manufacturers);
                    });

                    hubConnection.On<IEnumerable<GXPerformance>>(nameof(IGXHubEvents.PerformanceAdd), async (performances) =>
                    {
                        if (!IsIgnored(TargetType.Performance))
                        {
                            _toasterService.Add(new GXToast("Performance add", ToString(performances), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.PerformanceAdd), performances);
                    });
                    hubConnection.On<IEnumerable<GXPerformance>>(nameof(IGXHubEvents.PerformanceDelete), async (performances) =>
                    {
                        if (!IsIgnored(TargetType.Performance))
                        {
                            _toasterService.Add(new GXToast("Performance deleted", ToString(performances), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.PerformanceDelete), performances);
                    });
                    hubConnection.On(nameof(IGXHubEvents.PerformanceClear), async () =>
                    {
                        if (!IsIgnored(TargetType.Performance))
                        {
                            _toasterService.Add(new GXToast("Performance cleared.", null, Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.PerformanceClear));
                    });
                    hubConnection.On<IEnumerable<GXSubtotal>>(nameof(IGXHubEvents.SubtotalUpdate), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Subtotal))
                        {
                            _toasterService.Add(new GXToast("Subtotal updated", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.SubtotalUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXSubtotal>>(nameof(IGXHubEvents.SubtotalDelete), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Subtotal))
                        {
                            _toasterService.Add(new GXToast("Subtotal deleted", ToString(items), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.SubtotalDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXSubtotal>>(nameof(IGXHubEvents.SubtotalCalculate), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Subtotal))
                        {
                            _toasterService.Add(new GXToast("Subtotal calculated", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.SubtotalCalculate), items);
                    });
                    hubConnection.On<IEnumerable<GXSubtotalGroup>>(nameof(IGXHubEvents.SubtotalGroupUpdate), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Subtotal))
                        {
                            _toasterService.Add(new GXToast("Subtotal group updated", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.SubtotalGroupUpdate), items);
                    });
                    hubConnection.On<IEnumerable<GXSubtotalGroup>>(nameof(IGXHubEvents.SubtotalGroupDelete), async (items) =>
                    {
                        if (!IsIgnored(TargetType.Subtotal))
                        {
                            _toasterService.Add(new GXToast("Subtotal group deleted", ToString(items), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.SubtotalGroupDelete), items);
                    });
                    hubConnection.On<IEnumerable<GXSubtotal>?>(nameof(IGXHubEvents.ClearSubtotalLogs), async (logs) =>
                    {
                        if (!IsIgnored(TargetType.Subtotal))
                        {
                            if (!IsIgnored(TargetType.Subtotal))
                            {
                                if (logs == null || !logs.Any())
                                {
                                    _toasterService.Add(new GXToast("Subtotal logs cleared", "All logs cleared.", Color.Info, 15));
                                }
                                else
                                {
                                    _toasterService.Add(new GXToast("Subtotal log clered", ToString(logs), Color.Info, 15));
                                }
                            }
                        }
                        await ChangedAsync(nameof(IGXHubEvents.ClearSubtotalLogs), logs);
                        await UpdateUserStamp(TargetType.SubtotalLog, null, 0, 0, 0, 0);
                    });
                    hubConnection.On<IEnumerable<GXSubtotalLog>>(nameof(IGXHubEvents.AddSubtotalLogs), async (logs) =>
                    {
                        if (!IsIgnored(TargetType.SubtotalLog))
                        {
                            _toasterService.Add(new GXToast("Subtotal log added", ToString(logs), Color.Warning, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.AddSubtotalLogs), logs);
                        await UpdateUserStamp(TargetType.SubtotalLog,
                            logs.Max(s => s.CreationTime),
                            logs.Where(w => w.Level == (int)TraceLevel.Error).Count(),
                            logs.Where(w => w.Level == (int)TraceLevel.Warning).Count(),
                            logs.Where(w => w.Level == (int)TraceLevel.Info).Count(),
                            logs.Where(w => w.Level == (int)TraceLevel.Verbose).Count());
                    });
                    hubConnection.On<IEnumerable<GXSubtotalLog>>(nameof(IGXHubEvents.CloseSubtotalLogs), async (items) =>
                    {
                        if (!IsIgnored(TargetType.SubtotalLog))
                        {
                            _toasterService.Add(new GXToast("Subtotal log closed", ToString(items), Color.Info, 15));
                        }
                        await ChangedAsync(nameof(IGXHubEvents.CloseSubtotalLogs), items);
                    });
                    hubConnection.On<IEnumerable<GXUserStamp>>(nameof(IGXHubEvents.StampUpdate), async (stamps) =>
                    {
                        lock (_stamps)
                        {
                            foreach (var it in stamps)
                            {
                                var item = _stamps.Where(w => w.TargetType == it.TargetType).SingleOrDefault();
                                if (item == null)
                                {
                                    _stamps.Add(it);
                                }
                                else
                                {
                                    item.Id = it.Id;
                                    item.Updated = it.Updated;
                                    item.ConcurrencyStamp = it.ConcurrencyStamp;
                                }
                            }
                        }
                        await ChangedAsync(nameof(IGXHubEvents.StampUpdate), stamps);
                    });
                    //Get user stamps.
                    ListUserStamps req2 = new ListUserStamps();
                    var ret2 = await http.PostAsJson<ListUserStampsResponse>("api/UserStamp/List", req2);
                    if (ret2.Stamps != null)
                    {
                        lock (_stamps)
                        {
                            _stamps.AddRange(ret2.Stamps);
                        }
                        await ChangedAsync(nameof(IGXHubEvents.StampUpdate), _stamps);
                    }
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
        /// Page is changing.
        /// </summary>
        public event Action<IGXLocationChangingContext>? OnPageChanging;

        /// <summary>
        /// Exception has occurred.
        /// </summary>
        public event Action<Exception>? OnProcessError;
        /// <summary>
        /// Script logs has occurred.
        /// </summary>
        public event Action<IEnumerable<object>>? OnProcessErrors;
        /// <summary>
        /// Show information.
        /// </summary>
        public event Action<string, bool>? OnShowInformation;
        /// <summary>
        /// Maintenance mode is updated.
        /// </summary>
        public event Action<bool>? OnMaintenanceMode;
    }
}
