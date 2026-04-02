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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Diagnostics;
using System.Text.Json;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Client.Shared.Enums;

namespace Gurux.DLMS.AMI.Server.Prune
{
    /// <summary>
    /// This class implements prune service.
    /// </summary>
    internal class GXPruneService : IGXPruneService, IHostedService, IDisposable
    {
        private readonly IGXHost _host;
        private readonly ILogger _logger;
        private Timer? _timer;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfigurationRepository _configurationRepository;
        private PruneSettings? _options;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXPruneService(ILogger<GXPruneService> logger,
            IGXHost host,
            IServiceProvider serviceProvider,
            IConfigurationRepository configurationRepository)
        {
            _logger = logger;
            _host = host;
            _serviceProvider = serviceProvider;
            _configurationRepository = configurationRepository;
        }

        private void InitTimer(GXConfiguration[] configurations)
        {
            //Start prune if it's not disabled.
            if (_options != null && _options.Enabled)
            {
                if (_timer == null)
                {
                    _timer = new Timer(RunPruneTasks, _options, 0, 60000);
                }
            }
            else
            {
                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;
                }
            }
        }

        /// <inheritdoc />
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (ServerSettings.GetDefaultAdminUser(_host) == null)
            {
                await StopAsync(cancellationToken);
                return;
            }
            _logger.LogInformation("Prune is starting.");
            ListConfiguration req = new ListConfiguration() { Filter = new GXConfiguration() { Name = GXConfigurations.Prune } };
            GXConfiguration[] ret = _configurationRepository.ListAsync(req, null, cancellationToken).Result;
            if (ret != null && ret.Length == 1 && !string.IsNullOrEmpty(ret[0].Settings))
            {
                _options = JsonSerializer.Deserialize<PruneSettings>(ret[0].Settings);
                _configurationRepository.Updated += async (configurations) =>
                {
                    //If prune configuration is updated.
                    foreach (GXConfiguration it in configurations)
                    {
                        if (it.Name == GXConfigurations.Prune && it.Settings != null)
                        {
                            _options = JsonSerializer.Deserialize<PruneSettings>(it.Settings);
                            if (_options != null)
                            {
                                InitTimer(ret);
                            }
                            break;
                        }
                    }
                };
                if (_options != null)
                {
                    InitTimer(ret);
                }
            }
        }

        /// <inheritdoc />
        public Task RunAsync()
        {
            return Task.Run(() => RunPruneTasks());
        }

        private void RunPruneTasks(object? state)
        {
            RunPruneTasks().Wait();
        }

        private async Task Prune<T>(PruneBase value, T? item, Expression<Func<T, object>>? updated,
            Expression<Func<T, object>>? where, string str)
        {
            UInt32 count;
            if (value.Interval != 0)
            {
                //Get amount of the pruned rows.
                GXSelectArgs total = GXSelectArgs.Select<T>(q => GXSql.Count(GXSql.One), where);
                count = _host.Connection.SingleOrDefault<UInt32>(total);
                if (value.Maximum > 0 && count > value.Maximum)
                {
                    count = (UInt32)value.Maximum;
                }
                if (count != 0)
                {
                    using (IServiceScope scope = _serviceProvider.CreateScope())
                    {
                        ISystemLogRepository errors = scope.ServiceProvider.GetRequiredService<ISystemLogRepository>();
                        GXSystemLog log = new GXSystemLog(TraceLevel.Info);
                        log.Message = string.Format("Prune removed {0} " + str + ".", count);
                        await errors.AddAsync(TargetType.Prune, [log]);
                    }
                    if (value.Delete || updated == null)
                    {
                        GXDeleteArgs del = GXDeleteArgs.Delete(where);
                        del.Count = count;
                        await _host.Connection.DeleteAsync(del);
                    }
                    else
                    {
                        GXUpdateArgs update = GXUpdateArgs.Update(item, updated);
                        update.Count = count;
                        update.Where.And(where);
                        await _host.Connection.UpdateAsync(update);
                    }
                }
            }
        }

        /// <summary>
        /// Run prune tasks.
        /// </summary>
        private async Task RunPruneTasks()
        {
            if (_options == null)
            {
                return;
            }
            _logger.LogInformation(Properties.Resources.PruneStarted);
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                ISystemLogRepository errors = scope.ServiceProvider.GetRequiredService<ISystemLogRepository>();
                GXSystemLog error = new GXSystemLog(TraceLevel.Info);
                error.Message = Properties.Resources.PruneStarted;
                await errors.AddAsync(TargetType.Prune, [error]);
            }
            try
            {
                DateTime now = DateTime.Now;
                if (_options.SystemLog.Interval != 0)
                {
                    DateTime dt = DateTime.Now.AddMinutes(-_options.SystemLog.Interval);
                    await Prune(_options.SystemLog, new GXSystemLog() { Closed = now },
                        u => u.Closed, w => w.Closed == null && w.CreationTime < dt, "System log");
                }
                if (_options.User.Interval != 0)
                {
                    DateTime dt = DateTime.Now.AddMinutes(-_options.User.Interval);
                    await Prune(_options.User, new GXUser() { Removed = now },
                        u => u.Removed, w => w.Removed == null && w.Removed < dt, "User");
                }
                if (_options.UserActions.Interval != 0)
                {
                    DateTime dt = DateTime.Now.AddMinutes(-_options.UserActions.Interval);
                    await Prune<GXUserAction>(_options.UserActions,
                        null,
                        null, w => w.CreationTime < dt, "User action");
                }
                if (_options.UserErrors.Interval != 0)
                {
                    DateTime dt = DateTime.Now.AddMinutes(-_options.UserErrors.Interval);
                    await Prune(_options.UserErrors, new GXUserError(TraceLevel.Error) { Closed = now },
                        u => u.Closed, w => w.Closed == null && w.CreationTime < dt, "User error");
                }
                if (_options.DeviceLogs.Interval != 0)
                {
                    DateTime dt = DateTime.Now.AddMinutes(-_options.DeviceLogs.Interval);
                    await Prune(_options.DeviceLogs, new GXDeviceError() { Closed = now },
                        u => u.Closed, w => w.Closed == null && w.CreationTime < dt, "Device error");
                }
                if (_options.DeviceActions.Interval != 0)
                {
                    DateTime dt = DateTime.Now.AddMinutes(-_options.DeviceActions.Interval);
                    await Prune<GXDeviceAction>(_options.DeviceActions, null, null, w => w.CreationTime < dt, "Device action");
                }
                if (_options.AgentLog.Interval != 0)
                {
                    DateTime dt = DateTime.Now.AddMinutes(-_options.AgentLog.Interval);
                    await Prune(_options.AgentLog, new GXAgentLog() { Closed = now },
                        u => u.Closed, w => w.Closed == null && w.CreationTime < dt, "Agent log");
                }
                if (_options.GatewayLog.Interval != 0)
                {
                    DateTime dt = DateTime.Now.AddMinutes(-_options.GatewayLog.Interval);
                    await Prune(_options.GatewayLog, new GXGatewayLog() { Closed = now },
                        u => u.Closed, w => w.Closed == null && w.CreationTime < dt, "Gateway log");
                }
                if (_options.ScheduleLog.Interval != 0)
                {
                    DateTime dt = DateTime.Now.AddMinutes(-_options.ScheduleLog.Interval);
                    await Prune(_options.ScheduleLog, new GXScheduleLog() { Closed = now },
                        u => u.Closed, w => w.Closed == null && w.CreationTime < dt, "Schedule log");
                }
                if (_options.Task.Interval != 0)
                {
                    DateTime dt = DateTime.Now.AddMinutes(-_options.Task.Interval);
                    switch (_options.Task.Type)
                    {
                        case PruneTaskRemoveType.All:
                            await Prune<GXTask>(_options.Task, null, null, w => w.CreationTime < dt, "Tasks");
                            break;
                        case PruneTaskRemoveType.NotStarted:
                            await Prune<GXTask>(_options.Task, null, null, w => w.Start == null && w.CreationTime < dt, "Tasks");
                            break;
                        case PruneTaskRemoveType.Compleated:
                            await Prune<GXTask>(_options.Task, null, null, w => w.Ready != null && w.CreationTime < dt, "Tasks");
                            break;
                        case PruneTaskRemoveType.Failed:
                            await Prune<GXTask>(_options.Task, null, null, w => w.Ready != null && w.Error != null && w.CreationTime < dt, "Tasks");
                            break;
                        case PruneTaskRemoveType.Successful:
                            await Prune<GXTask>(_options.Task, null, null, w => w.Ready != null && w.Error == null && w.CreationTime < dt, "Tasks");
                            break;
                    }
                }
                if (_options.PerformanceLog.Interval != 0)
                {
                    DateTime dt = DateTime.Now.AddMinutes(-_options.PerformanceLog.Interval);
                    await Prune<GXPerformance>(_options.PerformanceLog, null, null, w => w.End < dt, "Performance log");
                }
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    ISystemLogRepository errors = scope.ServiceProvider.GetRequiredService<ISystemLogRepository>();
                    GXSystemLog error = new GXSystemLog(TraceLevel.Info);
                    error.Message = Properties.Resources.PruneCompleted;
                    await errors.AddAsync(TargetType.Prune, [error]);
                }
                _logger.LogInformation(Properties.Resources.PruneCompleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    ISystemLogRepository errors = scope.ServiceProvider.GetRequiredService<ISystemLogRepository>();
                    GXSystemLog error = new GXSystemLog();
                    error.Message = "Prune failed: " + ex.Message;
                    errors.AddAsync(TargetType.Prune, [error]).Wait();
                }
            }
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogWarning("Prune is stopping.");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
