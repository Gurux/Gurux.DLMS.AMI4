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
using Gurux.DLMS.AMI.Http;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.UI.Component.Toaster;
using Gurux.UI.Components;
using Gurux.UI.Components.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;
using System.Text;

namespace Gurux.DLMS.AMI.Client
{
    /// <summary>
    /// AMI SignalR notifications, user badge and toaster preferences, and navigation events.
    /// </summary>
    public class GXAmiNotifierService : Gurux.UI.Components.GXNotifierService, IGXNotifier2, IAsyncDisposable
    {

        private readonly NavigationManager? _navigationManager;
        private readonly IDisposable? _locationChangingRegistration;
        private bool _disposed;
        private readonly object _disposalSync = new();
        private Task? _disposalTask;
        private readonly SemaphoreSlim _connectionLifecycle = new(1, 1);
        private readonly ILogger _logger;
        /// <summary>
        /// List of active BADGE notification sources.
        /// </summary>
        private List<string> _userBadgeNotifications = [];
        /// <summary>
        /// List of toaster notifications that are used to show or hide toasts.
        /// </summary>
        private List<string> _userToasterNotifications = [];
        private readonly IGXToasterService? _toasterService;
        private readonly IServiceProvider _services;
        private readonly SemaphoreSlim _notificationRefresh = new(1, 1);

        /// <summary>
        /// UI Notifications.
        /// </summary>
        public string[]? Notifications;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXAmiNotifierService(IServiceProvider services)
        {
            _services = services;
            ILoggerFactory lf = services.GetRequiredService<ILoggerFactory>();
            _logger = lf.CreateLogger(nameof(GXAmiNotifierService));
            _toasterService = services.GetService<IGXToasterService>();
            NavigationManager? navigationManager = services.GetService<NavigationManager>();
            if (navigationManager != null)
            {
                try
                {
                    _locationChangingRegistration = navigationManager.RegisterLocationChangingHandler(OnLocationChanging);
                    _navigationManager = navigationManager;
                    navigationManager.LocationChanged += OnLocationChanged;
                }
                catch
                {
                    //NavigationManager is not available on the server side.
                    navigationManager = null;
                }
            }
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
        {
            if (!_disposed)
            {
                OnPageChanged?.Invoke();
            }
        }

        /// <summary>Releases AMI navigation registrations, base subscriptions and the server connection.</summary>
        public ValueTask DisposeAsync()
        {
            lock (_disposalSync)
            {
                _disposalTask ??= DisposeCoreAsync();
                return new ValueTask(_disposalTask);
            }
        }

        private async Task DisposeCoreAsync()
        {
            _disposed = true;
            _locationChangingRegistration?.Dispose();
            if (_navigationManager != null)
            {
                _navigationManager.LocationChanged -= OnLocationChanged;
            }
            OnPageChanged = null;
            OnPageChanging = null;
            _onProgressStart = null;
            _onProgressEnd = null;
            _onClearStatus = null;
            _onProcessError = null;
            _onProcessErrors = null;
            _onShowInformation = null;
            _onMaintenanceMode = null;
            base.Dispose();
            // In-flight operations can still release their semaphores during scope shutdown.
            await StopAsync();
        }

        /// <summary>Releases managed registrations immediately and starts connection cleanup.</summary>
        /// <remarks>Use DisposeAsync when connection cleanup must be awaited.</remarks>
        public override void Dispose()
        {
            var cleanup = DisposeAsync();
            if (!cleanup.IsCompletedSuccessfully)
            {
                _ = ObserveCleanupAsync(cleanup);
            }
        }

        private async Task ObserveCleanupAsync(ValueTask cleanup)
        {
            try
            {
                await cleanup;
            }
            catch (Exception error)
            {
                _logger.LogError(error, "Unable to dispose the AMI notification connection.");
            }
        }

        private ValueTask OnLocationChanging(LocationChangingContext context)
        {
            if (!_disposed)
            {
                OnPageChanging?.Invoke(context);
            }
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Listen events from the server.
        /// </summary>
        private HubConnection? hubConnection;

        /// <summary>
        /// List of user stamps.
        /// </summary>
        readonly List<GXUserStamp> _stamps = [];

        public Task UpdateUserStamp(
            string type,
            DateTimeOffset? datetime,
            int errors,
            int warnings,
            int informational,
            int verbose)
            => UpdateUserStamp(type, datetime, errors, warnings, informational, verbose, type);

        private async Task UpdateUserStamp(
            string type,
            DateTimeOffset? datetime,
            int errors,
            int warnings,
            int informational,
            int verbose,
            string source)
        {
            if (!_userBadgeNotifications.Contains(source) && !_userBadgeNotifications.Contains(type))
            {
                return;
            }
            GXUserStamp? stamp = null;
            lock (_stamps)
            {
                foreach (var it in _stamps)
                {
                    if (it.TargetType == type)
                    {
                        //If new stamp is added.
                        if (datetime == null)
                        {
                            it.CreationTime = datetime;
                            it.Errors = 0;
                            it.Warnings = 0;
                            it.Informational = 0;
                            it.Verboses = 0;
                            stamp = it;
                            break;
                        }
                        else if (it.CreationTime == null || datetime > it.CreationTime)
                        {
                            it.CreationTime = datetime;
                            it.Errors += errors;
                            it.Warnings += warnings;
                            it.Informational += informational;
                            it.Verboses += verbose;
                            stamp = it;
                            break;
                        }
                        else if (datetime == it.CreationTime)
                        {
                            // An earlier event in this batch already applied its counters.
                            // A selected event must still be able to publish the updated stamp.
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
            await PublishBadgeStampsAsync([stamp]);
        }

        ///<inheritdoc />
        public async Task Mark(string type,
            DateTimeOffset? datetime)
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
                    stamp ??= new GXUserStamp()
                    {
                        TargetType = type,
                        CreationTime = datetime
                    };
                    await PublishBadgeStampsAsync([stamp]);
                    if (!reset)
                    {
                        var userStampRepository = _services.GetRequiredService<IUserStampRepository>();
                        await userStampRepository.UpsertAsync([stamp]);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        ///<inheritdoc />
        public async Task Mark(HttpClient http, string type, DateTimeOffset? datetime)
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
                    stamp ??= new GXUserStamp()
                    {
                        TargetType = type,
                        CreationTime = datetime
                    };
                    await PublishBadgeStampsAsync([stamp]);
                    if (!reset)
                    {
                        UpdateUserStamp req = new()
                        {
                            Stamps = [stamp]
                        };
                        await http.PostAsJson<ListUserStampsResponse>("api/UserStamp/Update", req);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User stamp update failed.");
            }
        }

        /// <inheritdoc/>
        private GXUserStamp BadgeStamp(GXUserStamp stamp)
        {
            var result = new GXUserStamp { TargetType = stamp.TargetType };
            if (_userBadgeNotifications.Any(source => source == stamp.TargetType ||
                source.StartsWith(stamp.TargetType + ".", StringComparison.Ordinal)))
            {
                stamp.CopyTo(result);
            }
            return result;
        }

        private Task PublishBadgeStampsAsync(IEnumerable<GXUserStamp> stamps)
            => ChangedAsync(nameof(IGXHubEvents.StampUpdate), stamps.Select(BadgeStamp).ToArray());

        public GXUserStamp GetUserStamps(string type)
        {
            lock (_stamps)
            {
                return BadgeStamp(_stamps.SingleOrDefault(w => w.TargetType == type)
                    ?? new GXUserStamp { TargetType = type });
            }
        }

        public IEnumerable<GXUserStamp> GetUserStamps(IEnumerable<string>? types)
            => types?.Distinct().Select(GetUserStamps).ToArray() ?? [];

        private static string ToString<T>(IEnumerable<T>? items)
        {
            StringBuilder sb = new();
            if (items != null)
            {
                //Show max 10 items.
                int count = 1;
                foreach (var it in items)
                {
                    sb.AppendLine(it?.ToString());
                    if (++count > 10)
                    {
                        sb.AppendLine("...");
                        break;
                    }
                }
            }
            return sb.ToString();
        }

        bool IsIgnored(string type)
        {
            return Notifications == null || !Notifications.Contains(type);
        }

        private static string ToString(IEnumerable<GXTarget> items)
        {
            return string.Join(", ", items);
        }

        private static Color GetColor(GXTarget value)
        {
            return (TraceLevel?)value.Level switch
            {
                TraceLevel.Error => Color.Danger,
                TraceLevel.Warning => Color.Warning,
                TraceLevel.Info => Color.Info,
                TraceLevel.Verbose => Color.Secondary,
                _ => Color.Primary,
            };
        }

        private async Task UpdateUserStamps(IGXResourceStorage resourceStorage, IEnumerable<GXTarget> items)
        {
            foreach (var it in items)
            {
                int ret = it.Type.IndexOf('.');
                if (ret != -1)
                {
                    string target = it.Type[..ret];
                    string action = it.Type[(1 + ret)..];
                    var badgeItems = items.Where(item => item.Type.StartsWith(target + ".", StringComparison.Ordinal)
                        && (item.Type.EndsWith(".add", StringComparison.Ordinal) || item.Type.EndsWith(".edit", StringComparison.Ordinal) || item.Type.EndsWith(".delete", StringComparison.Ordinal))
                        && (_userBadgeNotifications.Contains(item.Type) || _userBadgeNotifications.Contains(target))).ToArray();
                    switch (action)
                    {
                        case "clear":
                            resourceStorage.ClearEnumTypes(target);
                            await UpdateUserStamp(target, null, 0, 0, 0, 0, it.Type);
                            if ((_userToasterNotifications.Contains(it.Type) || _userToasterNotifications.Contains(target)))
                            {
                                _toasterService?.Add(new GXToast(it.Title ?? target + " cleared", it.ToString(), GetColor(it), 15));
                            }
                            break;
                        case "add":
                            await UpdateUserStamp(target,
                                badgeItems.Select(s => s.CreationTime).DefaultIfEmpty().Max(),
                                badgeItems.Where(w => w.Level == (int)TraceLevel.Error).Count(),
                                badgeItems.Where(w => w.Level == (int)TraceLevel.Warning).Count(),
                                badgeItems.Where(w => w.Level == (int)TraceLevel.Info).Count(),
                                badgeItems.Where(w => w.Level == (int)TraceLevel.Verbose).Count(), it.Type);
                            if ((_userToasterNotifications.Contains(it.Type) || _userToasterNotifications.Contains(target)))
                            {
                                _toasterService?.Add(new GXToast(it.Title ?? target + " added", it.ToString(), GetColor(it), 15));
                            }
                            break;
                        case "edit":
                            await UpdateUserStamp(target,
                                badgeItems.Select(s => s.CreationTime).DefaultIfEmpty().Max(),
                                badgeItems.Where(w => w.Level == (int)TraceLevel.Error).Count(),
                                badgeItems.Where(w => w.Level == (int)TraceLevel.Warning).Count(),
                                badgeItems.Where(w => w.Level == (int)TraceLevel.Info).Count(),
                                badgeItems.Where(w => w.Level == (int)TraceLevel.Verbose).Count(), it.Type);
                            if ((_userToasterNotifications.Contains(it.Type) || _userToasterNotifications.Contains(target)))
                            {
                                _toasterService?.Add(new GXToast(it.Title ?? target + " edited", it.ToString(), GetColor(it), 15));
                            }
                            break;
                        case "close":
                            if ((_userToasterNotifications.Contains(it.Type) || _userToasterNotifications.Contains(target)))
                            {
                                _toasterService?.Add(new GXToast(it.Title ?? target + " closed", it.ToString(), GetColor(it), 15));
                            }
                            break;
                        case "delete":
                            await UpdateUserStamp(target,
                                badgeItems.Select(s => s.CreationTime).DefaultIfEmpty().Max(),
                                badgeItems.Where(w => w.Level == (int)TraceLevel.Error).Count(),
                                badgeItems.Where(w => w.Level == (int)TraceLevel.Warning).Count(),
                                badgeItems.Where(w => w.Level == (int)TraceLevel.Info).Count(),
                                badgeItems.Where(w => w.Level == (int)TraceLevel.Verbose).Count(), it.Type);
                            if ((_userToasterNotifications.Contains(it.Type) || _userToasterNotifications.Contains(target)))
                            {
                                _toasterService?.Add(new GXToast(it.Title ?? target + " deleted", it.ToString(), Color.Warning, 15));
                            }
                            break;
                        case "cron":
                            if ((_userToasterNotifications.Contains(it.Type) || _userToasterNotifications.Contains(target)))
                            {
                                _toasterService?.Add(new GXToast(it.Title ?? target, it.ToString(), GetColor(it), 15));
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }


        /// <summary>Reloads active notification sources, replacing the previous selections.</summary>
        public async Task RefreshNotificationsAsync(HttpClient http, CancellationToken cancellationToken = default)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            bool acquired = false;
            try
            {
                await _notificationRefresh.WaitAsync(timeout.Token);
                acquired = true;
                var response = await http.PostAsJson<ListNotificationResponse>("api/Notification/List",
                    new ListNotifications { Filter = new GXNotification { Enabled = true } }, timeout.Token);
                var notifications = response.Notifications ?? [];
                List<string> Sources(string type) => [.. notifications
                    .Where(n => n.Enabled == true && n.Type?.Code== type)
                    .SelectMany(n => n.Sources ?? [])
                    .Distinct(StringComparer.Ordinal)];
                var previousBadgeSources = _userBadgeNotifications;
                _userBadgeNotifications = Sources("BADGE");
                GXUserStamp[] badgeStamps;
                lock (_stamps)
                {
                    foreach (var stamp in _stamps)
                    {
                        bool Matches(string source) => source == stamp.TargetType ||
                            source.StartsWith(stamp.TargetType + ".", StringComparison.Ordinal);
                        var previous = previousBadgeSources.Where(Matches).ToHashSet(StringComparer.Ordinal);
                        if (previous.Count != 0 && !previous.SetEquals(_userBadgeNotifications.Where(Matches)))
                        {
                            stamp.Errors = stamp.Warnings = stamp.Informational = stamp.Verboses = 0;
                        }
                    }
                    badgeStamps = _stamps.Select(BadgeStamp).ToArray();
                }
                await ChangedAsync(nameof(IGXHubEvents.StampUpdate), badgeStamps);
                _userToasterNotifications = Sources("TOASTER");
                if (_toasterService != null)
                {
                    var setting = await http.GetAsJsonAsync<GetUserProperties>("api/UserProperties?name=Ami.Toaster.MaxCount", timeout.Token);
                    _toasterService.MaxCount = int.TryParse(setting?.Item?.Value, out var count) && count > 0 ? count : 20;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to reload notification settings.");
            }
            finally
            {
                if (acquired)
                {
                    _notificationRefresh.Release();
                }
            }
        }

        public async Task StartAsync(IServiceScope scope,
            HttpClient http,
            NavigationManager navigationManager)
        {
            await _connectionLifecycle.WaitAsync();
            try
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (hubConnection == null)
                {
                    hubConnection = new HubConnectionBuilder()
                   .WithUrl(navigationManager.ToAbsoluteUri("/guruxami"))
                   .WithAutomaticReconnect()
                   .Build();
                    var resourceStorage = scope.ServiceProvider.GetRequiredService<IGXResourceStorage>();
                    hubConnection.On<IEnumerable<GXTarget>>(nameof(IGXHubEvents.OnUpdate), async (items) =>
                    {
                        try
                        {
                            var updates = items.ToArray();
                            if (updates.Any(item => item.Type.StartsWith("notification.", StringComparison.Ordinal) || item.Type.StartsWith("user-setting.", StringComparison.Ordinal)))
                            {
                                await RefreshNotificationsAsync(http);
                            }
                            await UpdateUserStamps(resourceStorage, updates);
                            await ChangedAsync(nameof(IGXHubEvents.OnUpdate), updates);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Unable to process notification updates.");
                        }
                    });
                    hubConnection.Closed += ex =>
                    {
                        Console.WriteLine($"Closed: {ex?.Message}");
                        return Task.CompletedTask;
                    };
                    hubConnection.Reconnecting += ex =>
                    {
                        Console.WriteLine($"Reconnecting: {ex?.Message}");
                        return Task.CompletedTask;
                    };
                    hubConnection.Reconnected += async _ =>
                    {
                        await RefreshNotificationsAsync(http);
                    };
                    try
                    {
                        //Get user stamps.
                        ListUserStamps req2 = new();
                        _ = http.PostAsJson<ListUserStampsResponse>("api/UserStamp/List", req2).ContinueWith(t =>
                        {
                            if (t.IsCanceled)
                            {
                                return;
                            }
                            if (t.IsFaulted)
                            {
                                _logger.LogError(t.Exception, "User stamp retrieval failed.");
                                return;
                            }
                            IEnumerable<GXUserStamp>? list2 = t.Result?.Stamps;
                            if (list2 != null)
                            {
                                lock (_stamps)
                                {
                                    var idsInList2 = _stamps.Select(x => x.Id).ToHashSet();
                                    list2 = [.. list2.Where(x => !idsInList2.Contains(x.Id))];
                                    _stamps.AddRange(list2);
                                    list2 = [.. _stamps];
                                }
                                _ = PublishBadgeStampsAsync(list2);
                            }
                        });
                    }
                    catch (Exception)
                    {
                    }
                    // Connect first: a slow preferences API must not prevent SignalR delivery.
                    await hubConnection.StartAsync();
                    await RefreshNotificationsAsync(http);
                }
            }
            catch (Exception ex) when (!_disposed)
            {
                _logger.LogError(ex, "Unable to start the notification connection.");
                var failed = hubConnection;
                hubConnection = null;
                if (failed != null)
                {
                    await failed.DisposeAsync();
                }
            }
            finally { _connectionLifecycle.Release(); }
        }

        /// <summary>
        /// Stop listening the server events.
        /// </summary>
        /// <returns></returns>
        public async Task StopAsync()
        {
            await _connectionLifecycle.WaitAsync();
            try
            {
                if (hubConnection != null)
                {
                    var connection = hubConnection;
                    hubConnection = null;
                    await connection.DisposeAsync();
                }
            }
            finally { _connectionLifecycle.Release(); }
        }
        /// <summary>
        /// Page has changed.
        /// </summary>
        public event Action? OnPageChanged;
        /// <summary>
        /// Progress has started.
        /// </summary>
        // Legacy compatibility event; notifications use On/ChangedAsync subscriptions.
        private Action<string?>? _onProgressStart;
        public event Action<string?>? OnProgressStart { add { _onProgressStart += value; } remove { _onProgressStart -= value; } }
        /// <summary>
        /// Progress has ended.
        /// </summary>
        // Legacy compatibility event; notifications use On/ChangedAsync subscriptions.
        private Action<string?>? _onProgressEnd;
        public event Action<string?>? OnProgressEnd { add { _onProgressEnd += value; } remove { _onProgressEnd -= value; } }
        /// <summary>
        /// Status has cleared.
        /// </summary>
        // Legacy compatibility event; notifications use On/ChangedAsync subscriptions.
        private Action? _onClearStatus;
        public event Action? OnClearStatus { add { _onClearStatus += value; } remove { _onClearStatus -= value; } }

        /// <summary>
        /// Page is changing.
        /// </summary>
        public event Action<LocationChangingContext>? OnPageChanging;

        /// <summary>
        /// Exception has occurred.
        /// </summary>
        // Legacy compatibility event; notifications use On/ChangedAsync subscriptions.
        private Action<Exception>? _onProcessError;
        public event Action<Exception>? OnProcessError { add { _onProcessError += value; } remove { _onProcessError -= value; } }
        /// <summary>
        /// Script logs has occurred.
        /// </summary>
        // Legacy compatibility event; notifications use On/ChangedAsync subscriptions.
        private Action<IEnumerable<object>>? _onProcessErrors;
        public event Action<IEnumerable<object>>? OnProcessErrors { add { _onProcessErrors += value; } remove { _onProcessErrors -= value; } }
        /// <summary>
        /// Show information.
        /// </summary>
        // Legacy compatibility event; notifications use On/ChangedAsync subscriptions.
        private Action<string, bool>? _onShowInformation;
        public event Action<string, bool>? OnShowInformation { add { _onShowInformation += value; } remove { _onShowInformation -= value; } }
        /// <summary>
        /// Maintenance mode is updated.
        /// </summary>
        // Legacy compatibility event; notifications use On/ChangedAsync subscriptions.
        private Action<bool>? _onMaintenanceMode;
        public event Action<bool>? OnMaintenanceMode { add { _onMaintenanceMode += value; } remove { _onMaintenanceMode -= value; } }
    }
}
