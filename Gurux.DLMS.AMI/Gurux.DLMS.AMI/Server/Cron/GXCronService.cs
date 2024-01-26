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
using Gurux.DLMS.AMI.Shared.Enums;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Cron
{
    /// <summary>
    /// This class implements cron service.
    /// </summary>
    internal class GXCronService : IGXCronService, IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private Timer? _timer;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXCronService(ILogger<GXCronService> logger,
            IServiceProvider serviceProvider,
            IConfigurationRepository configurationRepository,
            IGXEventsNotifier eventsNotifier,
            IUserRepository userRepository)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configurationRepository = configurationRepository;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
        }

        private async Task InitTimer(GXConfiguration[] configurations, CronSettings options)
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
                if (_timer == null)
                {
                    _timer = new Timer(RunCronTasks, null, start, TimeSpan.FromHours(options.Interval));
                }
                else
                {
                    _timer.Change(start, TimeSpan.FromHours(options.Interval));
                }
                options.EstimatedNextTime = now + start;
            }
            else
            {
                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;
                }
                options.EstimatedNextTime = null;
            }
            //Update the next estimated run time.
            configurations[0].Settings = JsonSerializer.Serialize(options);
            await _configurationRepository.UpdateAsync(null, configurations, false);
        }

        /// <inheritdoc />
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            CronSettings? options;
            _logger.LogInformation("Cron is starting.");
            ListConfiguration req = new ListConfiguration() { Filter = new GXConfiguration() { Name = GXConfigurations.Cron } };
            GXConfiguration[] ret = _configurationRepository.ListAsync(null, req, null, cancellationToken).Result;
            if (ret != null && ret.Length == 1 && !string.IsNullOrEmpty(ret[0].Settings))
            {
                options = JsonSerializer.Deserialize<CronSettings>(ret[0].Settings);
                _configurationRepository.Updated += async (configurations) =>
                {
                    //If cron configuration is updated.
                    foreach (GXConfiguration it in configurations)
                    {
                        if (it.Name == GXConfigurations.Cron && it.Settings != null)
                        {
                            options = JsonSerializer.Deserialize<CronSettings>(it.Settings);
                            if (options != null)
                            {
                                await InitTimer(ret, options);
                            }
                            break;
                        }
                    }
                };
                if (options != null)
                {
                    await InitTimer(ret, options);
                }
            }
        }

        /// <inheritdoc />
        public Task RunAsync(ClaimsPrincipal user)
        {
            return Task.Run(() => RunCronTasks(user));
        }

        /// <summary>
        /// Run cron tasks.
        /// </summary>
        /// <param name="state"></param>
        private void RunCronTasks(object? state)
        {
            ClaimsPrincipal user = (ClaimsPrincipal)state;
            _logger.LogInformation(Properties.Resources.CronStarted);
            var users = _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }).Result;
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                ISystemLogRepository errors = scope.ServiceProvider.GetRequiredService<ISystemLogRepository>();
                GXSystemLog error = new GXSystemLog(TraceLevel.Info);
                error.Message = Properties.Resources.CronStarted;
                errors.AddAsync(user, new GXSystemLog[] { error }).Wait();
            }
            _eventsNotifier.CronStart(users).Wait();
            try
            {
                foreach (var it in _serviceProvider.GetServices<IGXCronTask>())
                {
                    try
                    {
                        it.RunAsync(user).Wait();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                        using (IServiceScope scope = _serviceProvider.CreateScope())
                        {
                            ISystemLogRepository errors = scope.ServiceProvider.GetRequiredService<ISystemLogRepository>();
                            GXSystemLog error = new GXSystemLog();
                            error.Message = "Cron failed: " + ex.Message;
                            errors.AddAsync(user, new GXSystemLog[] { error }).Wait();
                        }
                    }
                    //Update last cron run time.
                    using (IServiceScope scope = _serviceProvider.CreateScope())
                    {
                        IConfigurationRepository configurationRepository = scope.ServiceProvider.GetRequiredService<IConfigurationRepository>();
                        ListConfiguration req = new ListConfiguration()
                        {
                            Filter = new GXConfiguration()
                            {
                                Name = GXConfigurations.Cron
                            }
                        };
                        GXConfiguration[] confs = configurationRepository.ListAsync(user, req, null, CancellationToken.None).Result;
                        foreach (GXConfiguration conf in confs)
                        {
                            if (conf.Name == GXConfigurations.Cron && !string.IsNullOrEmpty(conf.Settings))
                            {
                                CronSettings? s = JsonSerializer.Deserialize<CronSettings>(conf.Settings);
                                if (s != null)
                                {
                                    s.Run = DateTime.Now;
                                    conf.Settings = JsonSerializer.Serialize(s);
                                    configurationRepository.UpdateAsync(user, new GXConfiguration[] { conf }, false).Wait();
                                }
                                break;
                            }
                        }
                    }
                }
                _eventsNotifier.CronCompleate(users).Wait();
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    ISystemLogRepository errors = scope.ServiceProvider.GetRequiredService<ISystemLogRepository>();
                    GXSystemLog error = new GXSystemLog(TraceLevel.Info);
                    error.Message = Properties.Resources.CronCompleted;
                    errors.AddAsync(user, new GXSystemLog[] { error }).Wait();
                }
                _logger.LogInformation(Properties.Resources.CronCompleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    ISystemLogRepository errors = scope.ServiceProvider.GetRequiredService<ISystemLogRepository>();
                    GXSystemLog error = new GXSystemLog();
                    error.Message = "Cron failed: " + ex.Message;
                    errors.AddAsync(user, new GXSystemLog[] { error }).Wait();
                }
            }
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogWarning("Cron is stopping.");
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
