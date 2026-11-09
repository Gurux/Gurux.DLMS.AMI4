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
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using MenuKind = Gurux.DLMS.AMI.Shared.DTOs.Enums.MenuKind;

namespace Gurux.DLMS.AMI.Server.Cron
{
    /// <summary>
    /// This class implements cron service.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    internal class GXCronService(ILogger<GXCronService> logger,
        IDbConnectionFactory dbFactory,
        IServiceProvider serviceProvider,
        IConfigurationRepository configurationRepository,
        NotificationCache notificationCache,
        GXBackgroundTaskRunner runner) : IGXBackgroundService, IHostedService, IDisposable
    {
        private readonly IDbConnectionFactory _dbFactory = dbFactory;
        private readonly ILogger _logger = logger;
        private GXAsyncTimer? _timer;
        private GXAsyncTimer? _configurationTimer;
        private GXConfiguration[]? _pendingConfiguration;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly IConfigurationRepository _configurationRepository = configurationRepository;
        private readonly NotificationCache _notificationCache = notificationCache;
        private readonly GXBackgroundTaskRunner _runner = runner;

        private async Task InitTimer(IEnumerable<GXConfiguration> configurations, CronSettings options, CancellationToken cancellationToken = default)
        {
            //The start time is at midnight.
            if (options.Interval < 0)
            {
                options.Interval = 0;
            }
            //Start cron if it's not disabled.
            if (options.Interval != 0)
            {
                DateTime now = DateTime.Now;
                TimeSpan start = now.Date.AddHours(now.Hour + (options.Interval - (now.Hour % options.Interval))) - now;
                _timer!.Change(start, TimeSpan.FromHours(options.Interval));
                options.EstimatedNextTime = now + start;
            }
            else
            {
                _timer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                options.EstimatedNextTime = null;
            }
            //Update the next estimated run time.
            configurations.First().Settings = JsonSerializer.Serialize(options);
            await using var connection = await _dbFactory.GetConnectionAsync(cancellationToken);
            var admin = await ServerSettings.GetDefaultAdminUserAsync(connection, cancellationToken: cancellationToken);
            await _configurationRepository.UpsertAsync(admin!, configurations, false, cancellationToken);
        }

        /// <inheritdoc />
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await using var connection = await _dbFactory.GetConnectionAsync(cancellationToken);
            var admin = await ServerSettings.GetDefaultAdminUserAsync(connection, cancellationToken: cancellationToken);
            if (admin == null)
            {
                //Update role scopes if default admin user is not created yet.
                //This ensures that the default admin user has all permissions.
                await StopAsync(cancellationToken);
                return;
            }
            await ServerSettings.UpdateLanguagesAsync(connection);
            await _serviceProvider.RunAsUserAsync(admin, async services =>
            {
                var repo = services.GetRequiredService<INotificationTypeRepository>();
                await repo.EnsureDefaultsAsync(cancellationToken);
            }, cancellationToken);
            //Update shared version.
            ServerSettings.UpdateScriptCommonSharedVersion(connection);
            CronSettings? options;
            _logger.LogInformation("Cron is starting.");
            ListConfiguration req = new() { Filter = new GXConfiguration() { Name = GXConfigurations.Cron } };
            IEnumerable<GXConfiguration> ret = await _configurationRepository.ListAsync(admin, req, null, cancellationToken);
            if (ret != null && ret.Count() == 1 && ret.First().Settings is string settings)
            {
                _timer = new GXAsyncTimer(RunScheduledAsync, _logger);
                options = JsonSerializer.Deserialize<CronSettings>(settings);
                _configurationTimer = new GXAsyncTimer(async token =>
                {
                    var pending = Interlocked.Exchange(ref _pendingConfiguration, null);
                    if (pending == null) {
                        return;
                    }
                    var configuration = pending.FirstOrDefault(c => c.Name == GXConfigurations.Cron);
                    var updated = configuration?.Settings == null ? null : JsonSerializer.Deserialize<CronSettings>(configuration.Settings);
                    if (updated != null) {
                        await InitTimer([configuration!], updated, token);
                    }
                }, _logger);
                _configurationRepository.Updated += OnConfigurationUpdated;
                if (options != null)
                {
                    await InitTimer(ret, options, cancellationToken);
                }
            }
        }

        private void OnConfigurationUpdated(IEnumerable<GXConfiguration> configurations)
        {
            var cron = configurations.Where(c => c.Name == GXConfigurations.Cron).ToArray();
            if (cron.Length == 0) {
                return;
            }
            Interlocked.Exchange(ref _pendingConfiguration, cron);
            _configurationTimer?.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);
        }

        /// <inheritdoc />
        public Task RunAsync(ClaimsPrincipal user)
            => _serviceProvider.RunAsUserAsync(user, services => RunCronTasks(services, user));

        public Task<Guid> StartAsync(string taskId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
            => _runner.StartAsync(taskId, user, cancellationToken);

        public GXBackgroundTaskStatus? GetTaskStatus(Guid runId) => _runner.GetTaskStatus(runId);

        /// <inheritdoc />
        public async Task InitAsync()
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            var appereanceRepository = scope.ServiceProvider.GetRequiredService<IAppearanceRepository>();
            await appereanceRepository.EnsureDefaultsAsync(null);
            var menuRepository = scope.ServiceProvider.GetRequiredService<IMenuRepository>();
            await menuRepository.EnsureDefaultsAsync(MenuKind.All);
            var policyRepository = scope.ServiceProvider.GetRequiredService<IPolicyRepository>();
            await policyRepository.EnsureDefaultsAsync();
            await scope.ServiceProvider.GetRequiredService<IRoleRepository>().EnsureDefaultsAsync();
            await scope.ServiceProvider.GetRequiredService<ILogTypeRepository>().EnsureDefaultsAsync();
            await scope.ServiceProvider.GetRequiredService<IEnumTypeRepository>().EnsureDefaultsAsync();
            var blockRepository = scope.ServiceProvider.GetRequiredService<IBlockRepository>();
            await blockRepository.EnsureDefaultsAsync();
            var notificationTypeRepository = scope.ServiceProvider.GetRequiredService<INotificationTypeRepository>();
            await notificationTypeRepository.EnsureDefaultsAsync();
        }

        /// <summary>
        /// Run cron tasks.
        /// </summary>
        /// <param name="cancellationToken">The service shutdown token.</param>
        private async Task RunScheduledAsync(CancellationToken cancellationToken)
        {
            ClaimsPrincipal? admin;
            await using (var connection = await _dbFactory.GetConnectionAsync(cancellationToken))
            {
                admin = await ServerSettings.GetDefaultAdminUserAsync(connection, cancellationToken: cancellationToken);
            }
            if (admin == null)
            {
                _logger.LogWarning("Cron cannot start because no default administrator is available.");
                return;
            }
            await _serviceProvider.RunAsUserAsync(admin,
                services => RunCronTasks(services, admin, cancellationToken), cancellationToken);
        }
        /// <summary>
        /// Run cron tasks.
        /// </summary>
        private async Task RunCronTasks(IServiceProvider services, ClaimsPrincipal user, CancellationToken cancellationToken = default)
        {
            if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
            {
                _logger.LogInformation("{Message}", ServerHelpers.GetInvariantString(nameof(Properties.Resources.CronStarted)));
            }
            List<string> users;
            var userRepository = services.GetRequiredService<IUserRepository>();
            users = await userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]);
            ILogRepository repository = services.GetRequiredService<ILogRepository>().ForSource("system");
            GXLog error = new(TraceLevel.Info)
            {
                Message = ServerHelpers.GetInvariantString(nameof(Properties.Resources.CronStarted))
            };
            await repository.AddAsync(TargetType.Cron, [error], cancellationToken);
            try
            {
                foreach (var taskId in _runner.ScheduledTaskIds)
                {
                    try
                    {
                        if (taskId == GXBackgroundTaskIds.Update &&
                            !await services.GetRequiredService<Gurux.DLMS.AMI.Update.GXUpdateSettingsStore>().IsDueAsync())
                        {
                            continue;
                        }
                        await _runner.RunAsync(taskId, user, cancellationToken);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                    catch (Exception ex)
                    {
                        if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
                        {
                            _logger.LogError("{Message}", ex.Message);
                        }
                        repository = services.GetRequiredService<ILogRepository>().ForSource("system");
                        error = new GXLog
                        {
                            Message = "Cron failed: " + ex.Message
                        };
                        await repository.AddAsync(TargetType.Cron, [error], cancellationToken);
                    }
                    //Update last cron run time.
                    IConfigurationRepository configurationRepository = services.GetRequiredService<IConfigurationRepository>();
                    ListConfiguration req = new()
                    {
                        Filter = new GXConfiguration()
                        {
                            Name = GXConfigurations.Cron
                        }
                    };
                    IEnumerable<GXConfiguration> confs = await configurationRepository.ListAsync(user, req, cancellationToken: cancellationToken);
                    foreach (GXConfiguration conf in confs)
                    {
                        if (string.Compare(conf.Name, GXConfigurations.Cron, StringComparison.OrdinalIgnoreCase) == 0 && !string.IsNullOrEmpty(conf.Settings))
                        {
                            CronSettings? s = JsonSerializer.Deserialize<CronSettings>(conf.Settings);
                            if (s != null)
                            {
                                s.Run = DateTime.Now;
                                conf.Settings = JsonSerializer.Serialize(s);
                                await configurationRepository.UpsertAsync(user, [conf], false, cancellationToken);
                                _notificationCache.Add(users, new GXTarget(GXConfigurationPolicies.Cron, GXCronNotifications.ScheduleUpdated, "Cron schedule updated.")
                                {
                                    Settings = conf.Settings
                                });
                            }
                            break;
                        }
                    }
                }
                repository = services.GetRequiredService<ILogRepository>().ForSource("system");
                error = new GXLog(TraceLevel.Info)
                {
                    Message = ServerHelpers.GetInvariantString(nameof(Properties.Resources.CronCompleted))
                };
                await repository.AddAsync(TargetType.Cron, [error], cancellationToken);
                if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
                {
                    _logger.LogInformation("{Message}", ServerHelpers.GetInvariantString(nameof(Properties.Resources.CronCompleted)));
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
                {
                    _logger.LogError("{Message}", ex.Message);
                }
                repository = services.GetRequiredService<ILogRepository>().ForSource("system");
                error = new GXLog
                {
                    Message = "Cron failed: " + ex.Message
                };
                await repository.AddAsync(TargetType.Cron, [error], cancellationToken);
            }
        }

        /// <inheritdoc />
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogWarning("Cron is stopping.");
            _configurationRepository.Updated -= OnConfigurationUpdated;
            if (_configurationTimer != null) {
                await _configurationTimer.StopAsync(cancellationToken);
            }
            if (_timer != null) {
                await _timer.StopAsync(cancellationToken);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _configurationRepository.Updated -= OnConfigurationUpdated;
            _configurationTimer?.Dispose();
            _timer?.Dispose();
        }
    }
}
