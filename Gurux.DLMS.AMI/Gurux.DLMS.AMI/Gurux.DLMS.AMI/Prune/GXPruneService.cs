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

using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Server.Internal;

using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json;

namespace Gurux.DLMS.AMI.Server.Prune
{
    /// <summary>
    /// This class implements prune service.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    internal class GXPruneService(ILogger<GXPruneService> logger,
        IDbConnectionFactory dbFactory,
        IServiceProvider serviceProvider,
        IConfigurationRepository configurationRepository) : IGXPruneService, IHostedService, IDisposable
    {
        private readonly IDbConnectionFactory _dbFactory = dbFactory;
        private readonly ILogger _logger = logger;
        private GXAsyncTimer? _timer;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly IConfigurationRepository _configurationRepository = configurationRepository;
        private PruneSettings? _options;
        private readonly object _settingsGate = new();
        private readonly SemaphoreSlim _runGate = new(1, 1);
        private readonly CancellationTokenSource _stopping = new();

        private void InitTimer(IEnumerable<GXConfiguration> configurations)
        {
            _ = configurations;
            lock (_settingsGate)
            {
                if (_stopping.IsCancellationRequested) {
                    return;
                }
                if (_options?.Enabled == true)
                {
                    _timer ??= new GXAsyncTimer(RunScheduledAsync, _logger);
                    _timer.Change(TimeSpan.Zero, TimeSpan.FromMinutes(1));
                }
                else
                {
                    _timer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                }
            }
        }
        /// <inheritdoc />
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            ClaimsPrincipal? admin;
            {
                await using var connection = await _dbFactory.GetConnectionAsync(cancellationToken);
                admin = await ServerSettings.GetDefaultAdminUserAsync(connection, cancellationToken: cancellationToken);
                if (admin == null)
                {
                    await StopAsync(cancellationToken);
                    return;
                }
            }
            _logger.LogInformation("Prune is starting.");
            ListConfiguration req = new() { Filter = new GXConfiguration() { Name = GXConfigurations.Prune } };
            IEnumerable<GXConfiguration> ret = await _configurationRepository.ListAsync(admin, req, cancellationToken: cancellationToken);
            if (ret != null && ret.Count() == 1 && ret.First().Settings is string settings && !string.IsNullOrEmpty(settings))
            {
                _options = JsonSerializer.Deserialize<PruneSettings>(settings);
                _configurationRepository.Updated += OnConfigurationUpdated;
                if (_options != null)
                {
                    InitTimer(ret);
                }
            }
        }

        private void OnConfigurationUpdated(IEnumerable<GXConfiguration> configurations)
        {
            var configuration = configurations.FirstOrDefault(c => c.Name == GXConfigurations.Prune);
            if (configuration?.Settings == null) {
                return;
            }
            try
            {
                _options = JsonSerializer.Deserialize<PruneSettings>(configuration.Settings);
                InitTimer([configuration]);
            }
            catch (JsonException ex) { _logger.LogError(ex, "Invalid prune configuration."); }
        }

        /// <inheritdoc />
        public Task RunAsync()
        {
            return RunPruneTasks();
        }

        private Task RunScheduledAsync(CancellationToken cancellationToken) => RunPruneTasks(cancellationToken);

        private async Task Prune<T>(PruneBase value, T? item, Expression<Func<T, object>>? updated,
            Expression<Func<T, bool>>? where, string str, CancellationToken cancellationToken) where T : class
        {
            UInt32 count;
            if (value.Interval != 0)
            {
                //Get amount of the pruned rows.
                ArgumentNullException.ThrowIfNull(where);
                GXSelectArgs total = GXSelectArgs.Select<T>(q => GXSql.Count(GXSql.One), where);
                await using var connection = await _dbFactory.GetConnectionAsync(cancellationToken);
                count = await connection.SingleOrDefaultAsync<UInt32>(total, cancellationToken);
                if (value.Maximum > 0 && count > value.Maximum)
                {
                    count = (UInt32)value.Maximum;
                }
                if (count != 0)
                {
                    using (IServiceScope scope = _serviceProvider.CreateScope())
                    {
                        ILogRepository errors = scope.ServiceProvider.GetRequiredService<ILogRepository>().ForSource("system");
                        GXLog log = new(TraceLevel.Info)
                        {
                            Message = string.Format("Prune removed {0} " + str + ".", count)
                        };
                        await errors.AddAsync(TargetType.Prune, [log], cancellationToken);
                    }
                    if (value.Delete || updated == null)
                    {
                        GXDeleteArgs del = GXDeleteArgs.Delete(where);
                        del.Count = count;
                        await connection.DeleteAsync(del, cancellationToken);
                    }
                    else
                    {
                        ArgumentNullException.ThrowIfNull(item);
                        GXUpdateArgs update = GXUpdateArgs.Update(item, updated);
                        update.Count = count;
                        update.Where.And(where);
                        await connection.UpdateAsync(update, cancellationToken);
                    }
                }
            }
        }

        /// <summary>
        /// Run prune tasks.
        /// </summary>
        private async Task RunPruneTasks(CancellationToken cancellationToken = default)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stopping.Token);
            await _runGate.WaitAsync(linked.Token);
            try { await RunPruneTasksCore(linked.Token); }
            finally { _runGate.Release(); }
        }

        private async Task RunPruneTasksCore(CancellationToken cancellationToken)
        {
            var options = _options;
            if (options == null)
            {
                return;
            }
            if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
            {
                _logger.LogInformation("{Message}", Properties.Resources.PruneStarted);
            }
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                ILogRepository errors = scope.ServiceProvider.GetRequiredService<ILogRepository>().ForSource("system");
                GXLog error = new(TraceLevel.Info)
                {
                    Message = Properties.Resources.PruneStarted
                };
                await errors.AddAsync(TargetType.Prune, [error], cancellationToken);
            }
            try
            {
                DateTime now = DateTime.Now;
                if (options.SystemLog.Interval != 0)
                {
                    DateTime dt = DateTime.Now.AddMinutes(-options.SystemLog.Interval);
                    await Prune(options.SystemLog, new GXLog() { Closed = now },
                        u => u.Closed!, GXLogStore.SourcePredicate("system", w => w.Closed == null && w.CreationTime < dt), "System log", cancellationToken);
                }
                if (options.User.Interval != 0)
                {
                    DateTime dt = DateTime.Now.AddMinutes(-options.User.Interval);
                    await Prune(options.User, new GXUser() { Removed = now },
                        u => u.Removed!, w => w.Removed == null && w.Removed < dt, "User", cancellationToken);
                }
                if (options.UserActions.Interval != 0)
                {
                    DateTime dt = DateTime.Now.AddMinutes(-options.UserActions.Interval);
                    await Prune<GXLog>(options.UserActions,
                        null,
                        null, GXLogStore.SourcePredicate("user-action", w => w.CreationTime < dt), "User action", cancellationToken);
                }
                if (options.UserErrors.Interval != 0)
                {
                    DateTime dt = DateTime.Now.AddMinutes(-options.UserErrors.Interval);
                    await Prune(options.UserErrors, new GXLog(TraceLevel.Error) { Closed = now },
                        u => u.Closed!, GXLogStore.SourcePredicate("user-error", w => w.Closed == null && w.CreationTime < dt), "User error", cancellationToken);
                }
                if (options.DeviceLogs.Interval != 0)
                {
                    DateTime dt = DateTime.Now.AddMinutes(-options.DeviceLogs.Interval);
                    await Prune(options.DeviceLogs, new GXLog() { Closed = now },
                        u => u.Closed!, GXLogStore.SourcePredicate("device-error", w => w.Closed == null && w.CreationTime < dt), "Device error", cancellationToken);
                }
                if (options.DeviceActions.Interval != 0)
                {
                    DateTime dt = DateTime.Now.AddMinutes(-options.DeviceActions.Interval);
                    await Prune<GXLog>(options.DeviceActions, null, null, GXLogStore.SourcePredicate("device-action", w => w.CreationTime < dt), "Device action", cancellationToken);
                }
                if (options.AgentLog.Interval != 0)
                {
                    DateTime dt = DateTime.Now.AddMinutes(-options.AgentLog.Interval);
                    await Prune(options.AgentLog, new GXLog() { Closed = now },
                        u => u.Closed!, GXLogStore.SourcePredicate("agent", w => w.Closed == null && w.CreationTime < dt), "Agent log", cancellationToken);
                }
                if (options.GatewayLog.Interval != 0)
                {
                    DateTime dt = DateTime.Now.AddMinutes(-options.GatewayLog.Interval);
                    await Prune(options.GatewayLog, new GXLog() { Closed = now },
                        u => u.Closed!, GXLogStore.SourcePredicate("gateway", w => w.Closed == null && w.CreationTime < dt), "Gateway log", cancellationToken);
                }
                if (options.ScheduleLog.Interval != 0)
                {
                    DateTime dt = DateTime.Now.AddMinutes(-options.ScheduleLog.Interval);
                    await Prune(options.ScheduleLog, new GXLog() { Closed = now },
                        u => u.Closed!, GXLogStore.SourcePredicate("schedule", w => w.Closed == null && w.CreationTime < dt), "Schedule log", cancellationToken);
                }
                if (options.Task.Interval != 0)
                {
                    DateTime dt = DateTime.Now.AddMinutes(-options.Task.Interval);
                    switch (options.Task.Type)
                    {
                        case PruneTaskRemoveType.All:
                            await Prune<GXTask>(options.Task, null, null, w => w.CreationTime < dt, "Tasks", cancellationToken);
                            break;
                        case PruneTaskRemoveType.NotStarted:
                            await Prune<GXTask>(options.Task, null, null, w => w.Start == null && w.CreationTime < dt, "Tasks", cancellationToken);
                            break;
                        case PruneTaskRemoveType.Completed:
                            await Prune<GXTask>(options.Task, null, null, w => w.Ready != null && w.CreationTime < dt, "Tasks", cancellationToken);
                            break;
                        case PruneTaskRemoveType.Failed:
                            await Prune<GXTask>(options.Task, null, null, w => w.Ready != null && w.Error != null && w.CreationTime < dt, "Tasks", cancellationToken);
                            break;
                        case PruneTaskRemoveType.Successful:
                            await Prune<GXTask>(options.Task, null, null, w => w.Ready != null && w.Error == null && w.CreationTime < dt, "Tasks", cancellationToken);
                            break;
                    }
                }
                if (options.PerformanceLog.Interval != 0)
                {
                    DateTime dt = DateTime.Now.AddMinutes(-options.PerformanceLog.Interval);
                    await Prune<GXPerformance>(options.PerformanceLog, null, null, w => w.End < dt, "Performance log", cancellationToken);
                }
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    ILogRepository errors = scope.ServiceProvider.GetRequiredService<ILogRepository>().ForSource("system");
                    GXLog error = new(TraceLevel.Info)
                    {
                        Message = Properties.Resources.PruneCompleted
                    };
                    await errors.AddAsync(TargetType.Prune, [error], cancellationToken);
                }
                if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
                {
                    _logger.LogInformation("{Message}", Properties.Resources.PruneCompleted);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
                {
                    _logger.LogError("{Message}", ex.Message);
                }
                using IServiceScope scope = _serviceProvider.CreateScope();
                ILogRepository errors = scope.ServiceProvider.GetRequiredService<ILogRepository>().ForSource("system");
                GXLog error = new()
                {
                    Message = "Prune failed: " + ex.Message
                };
                await errors.AddAsync(TargetType.Prune, [error], cancellationToken);
            }
        }

        /// <inheritdoc />
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogWarning("Prune is stopping.");
            _configurationRepository.Updated -= OnConfigurationUpdated;
            GXAsyncTimer? timer;
            lock (_settingsGate)
            {
                _stopping.Cancel();
                timer = _timer;
            }
            if (timer != null) {
                await timer.StopAsync(cancellationToken);
            }
            await _runGate.WaitAsync(cancellationToken);
            _runGate.Release();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _configurationRepository.Updated -= OnConfigurationUpdated;
            lock (_settingsGate)
            {
                _stopping.Cancel();
                _timer?.Dispose();
            }
        }
    }
}
