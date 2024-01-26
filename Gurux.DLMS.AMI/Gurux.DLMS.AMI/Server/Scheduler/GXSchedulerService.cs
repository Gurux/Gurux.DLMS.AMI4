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

using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.AMI.Shared.DIs;
using System.Globalization;
using Gurux.DLMS.AMI.Client.Shared;
using System.Text.Json;
using System.Diagnostics;
using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server;
using Gurux.DLMS.AMI.Server.Repository;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;

namespace Gurux.DLMS.AMI.Scheduler
{
    /// <summary>
    /// This interface is used to run schedules.
    /// </summary>
    public interface IGXScheduleService
    {
        /// <summary>
        /// Run selected schedule.
        /// </summary>
        Task RunAsync(ClaimsPrincipal user, GXSchedule schedule);
    }

    internal class GXSchedulerService : IGXScheduleService, IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private Timer? _timer;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly IScheduleLogRepository _scheduleLogRepository;
        private readonly IScheduleRepository _scheduleRepository;
        private readonly IObjectRepository _objectRepository;
        private readonly IAttributeRepository _attributeRepository;
        private readonly IDeviceGroupRepository _deviceGroupRepository;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly ISystemLogRepository _systemLogRepository;
        private StatisticSettings? statistic;
        private readonly IGXHost _host;
        private ClaimsPrincipal? User;
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXSchedulerService(ILogger<GXSchedulerService> logger,
            IServiceProvider serviceProvider,
            IGXHost host,
            IConfigurationRepository configurationRepository,
            IScheduleLogRepository scheduleLogRepository,
            IGXEventsNotifier eventsNotifier,
            ISystemLogRepository systemLogRepository,
            IScheduleRepository scheduleRepository,
            IObjectRepository objectRepository,
            IDeviceGroupRepository deviceGroupRepository,
            IAttributeRepository attributeRepository)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configurationRepository = configurationRepository;
            _scheduleLogRepository = scheduleLogRepository;
            _scheduleRepository = scheduleRepository;
            _eventsNotifier = eventsNotifier;
            _systemLogRepository = systemLogRepository;
            _host = host;
            _objectRepository = objectRepository;
            _deviceGroupRepository = deviceGroupRepository;
            _attributeRepository = attributeRepository;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            User = ServerSettings.GetDefaultAdminUser(_host);
            ListConfiguration req = new ListConfiguration() { Filter = new GXConfiguration() { Name = GXConfigurations.Statistic } };
            GXConfiguration[] confs = _configurationRepository.ListAsync(User, req, null, CancellationToken.None).Result;
            if (confs.Length == 1)
            {
                statistic = JsonSerializer.Deserialize<StatisticSettings>(confs[0].Settings);
            }
            _configurationRepository.Updated += async (configurations) =>
            {
                //If statistics configuration is updated.
                foreach (GXConfiguration it in configurations)
                {
                    if (it.Name == GXConfigurations.Statistic && it.Settings != null)
                    {
                        statistic = JsonSerializer.Deserialize<StatisticSettings>(it.Settings);
                        break;
                    }
                }
            };

            if (statistic != null && statistic.ScheduleActions)
            {
                GXSystemLog log = new GXSystemLog(TraceLevel.Verbose);
                log.Message = Server.Properties.Resources.SchedulerServiceIsStarting;
                await _systemLogRepository.AddAsync(User, new GXSystemLog[] { log });
            }
            _logger.LogInformation(Server.Properties.Resources.SchedulerServiceIsStarting);
            TimeSpan start = DateTime.Now.AddSeconds(60 - DateTime.Now.Second) - DateTime.Now;
            _timer = new Timer(DoWork, null, start, TimeSpan.FromMinutes(1));
        }

        /// <inheritdoc />
        public async Task RunAsync(ClaimsPrincipal user, GXSchedule schedule)
        {
            User = ServerSettings.GetDefaultAdminUser(_host);
            List<GXTask> tasks = new List<GXTask>();
            if (statistic != null && statistic.ScheduleActions)
            {
                GXScheduleLog log = new GXScheduleLog(TraceLevel.Verbose);
                log.Schedule = schedule;
                log.Message = "Scheduler started.";
                await _scheduleLogRepository.AddAsync(User, new GXScheduleLog[] { log });
            }

            await RunSchedule(schedule, tasks, DateTime.Now);
            if (tasks.Count != 0)
            {
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    ITaskRepository taskRepository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
                    await taskRepository.AddAsync(User, tasks);
                }
            }
            if (statistic != null && statistic.ScheduleActions)
            {
                GXScheduleLog log = new GXScheduleLog(TraceLevel.Verbose);
                log.Schedule = schedule;
                log.Message = "Scheduler ended.";
                await _scheduleLogRepository.AddAsync(User, new GXScheduleLog[] { log });
            }
        }

        private async Task RunSchedule(GXSchedule schedule, List<GXTask> tasks, DateTime now)
        {
            ClaimsPrincipal user = ServerHelpers.CreateClaimsPrincipalFromUser(schedule.Creator);
            List<string> users = await _scheduleRepository.GetUsersAsync(user, schedule.Id);
            await _eventsNotifier.ScheduleStart(users, new[] { schedule });
            //Save schedule invoker.
            GXUser? creator = schedule.Creator;
            //Read all data for the schedule.
            schedule = await _scheduleRepository.ReadAsync(user, schedule.Id);
            schedule.Creator = creator;
            if (schedule.DeviceGroups != null)
            {
                foreach (GXDeviceGroup dg in schedule.DeviceGroups)
                {
                    bool handled = false;
                    if (schedule.Objects != null && schedule.Objects.Any())
                    {
                        handled = true;
                        GXDeviceGroup g = await _deviceGroupRepository.ReadAsync(user, dg.Id);
                        if (g.Devices != null)
                        {
                            foreach (GXDevice dev in g.Devices)
                            {
                                ListObjects req = new ListObjects()
                                {
                                    Filter = new GXObject() { Device = dev },
                                };
                                var objects = await _objectRepository.ListAsync(user, req, null, default);
                                //Device objects are read in one batch.
                                Guid batch = Guid.NewGuid();
                                foreach (GXObject o in schedule.Objects)
                                {
                                    GXObject? ot = objects.Where(w => w.Template?.LogicalName == o.Template?.LogicalName).SingleOrDefault();
                                    if (ot != null)
                                    {
                                        if (ot.Latebind)
                                        {
                                            //If late binding.
                                            ot.Id = (await ObjectRepository.CreateLateBindObject(_host, null, schedule.Creator.Id, dev, ot.Id)).Id;
                                            ot.Device = dev;
                                        }
                                        GXTask t = new GXTask()
                                        {
                                            TriggerSchedule = schedule,
                                            Object = ot,
                                            TaskType = TaskType.Read,
                                            Batch = batch,
                                        };
                                        tasks.Add(t);
                                    }
                                }
                            }
                        }
                        //Reset objects.
                        schedule.Objects = null;
                    }
                    if (schedule.Attributes != null && schedule.Attributes.Any())
                    {
                        handled = true;
                        GXDeviceGroup g = await _deviceGroupRepository.ReadAsync(user, dg.Id);
                        //List of create objects.
                        if (g.Devices != null)
                        {
                            foreach (GXDevice device in g.Devices)
                            {
                                Dictionary<Guid, GXObject> objects = new Dictionary<Guid, GXObject>();
                                ListAttributes req = new ListAttributes()
                                {
                                    Filter = new GXAttribute()
                                    {
                                        Object = new GXObject() { Device = device },
                                    }
                                };
                                var attributes = await _attributeRepository.ListAsync(user, req, null, default);
                                //Device attrbutes are read in one batch.
                                Guid batch = Guid.NewGuid();
                                foreach (GXAttribute a in schedule.Attributes)
                                {
                                    GXAttribute? at = attributes.Where(w => w.Template.Id == a.Template.Id).SingleOrDefault();
                                    if (at != null)
                                    {
                                        if (at.CreationTime == DateTime.MinValue)
                                        {
                                            //If late binding is used and object is not created yet.
                                            if (objects.TryGetValue(at.Object.Id, out GXObject? obj))
                                            {
                                                at.Object = obj;
                                            }
                                            else
                                            {
                                                at.Object = (await ObjectRepository.CreateLateBindObject(_host, null, schedule.Creator.Id, device, at.Object.Id));
                                            }
                                            objects[at.Object.Template.Id] = at.Object;
                                            //Find attribute template and update it.
                                            var tmp = at.Object.Attributes.Where(w => w.Template.Index == a.Template.Index).SingleOrDefault();
                                            if (tmp != null)
                                            {
                                                //Update attribute ID.
                                                at.Id = tmp.Id;
                                            }
                                        }
                                        GXTask t = new GXTask()
                                        {
                                            TriggerSchedule = schedule,
                                            Attribute = at,
                                            TaskType = TaskType.Read,
                                            Batch = batch,
                                        };
                                        tasks.Add(t);
                                    }
                                }
                            }
                        }
                        //Reset attributes.
                        schedule.Attributes = null;
                    }
                    if (!handled)
                    {
                        GXTask t = new GXTask()
                        {
                            TriggerSchedule = schedule,
                            DeviceGroup = dg,
                            TaskType = TaskType.Read,
                        };
                        tasks.Add(t);
                    }
                }
            }

            if (schedule.Devices != null)
            {
                foreach (GXDevice it in schedule.Devices)
                {
                    //Add device read.
                    GXTask t = new GXTask()
                    {
                        TriggerSchedule = schedule,
                        Device = it,
                        TaskType = TaskType.Read,
                    };
                    tasks.Add(t);
                }
            }
            if (schedule.Objects != null)
            {
                foreach (GXObject it in schedule.Objects)
                {
                    GXTask t = new GXTask()
                    {
                        TriggerSchedule = schedule,
                        Object = it,
                        TaskType = TaskType.Read,
                    };
                    tasks.Add(t);
                }
            }
            if (schedule.Attributes != null)
            {
                foreach (GXAttribute it in schedule.Attributes)
                {
                    GXTask t = new GXTask()
                    {
                        TriggerSchedule = schedule,
                        Attribute = it,
                        TaskType = TaskType.Read,
                        Index = it.Template.Index
                    };
                    tasks.Add(t);
                }
            }
            if (schedule.ScriptMethods != null)
            {
                foreach (GXScriptMethod it in schedule.ScriptMethods)
                {
                    GXTask t = new GXTask()
                    {
                        TriggerSchedule = schedule,
                        ScriptMethod = it,
                        TaskType = TaskType.Read,
                    };
                    tasks.Add(t);
                }
            }
            if (schedule.Modules != null)
            {
                foreach (GXModule it in schedule.Modules)
                {
                    GXTask t = new GXTask()
                    {
                        TriggerSchedule = schedule,
                        Module = it,
                        TaskType = TaskType.Action,
                    };
                    tasks.Add(t);
                }
            }
            schedule.ExecutionTime = now;
            _scheduleRepository.UpdateExecutionTime(schedule);
            await _eventsNotifier.ScheduleCompleate(users, new[] { schedule });
        }

        private async void DoWork(object? state)
        {
            User = ServerSettings.GetDefaultAdminUser(_host);
            if (User == null)
            {
                //If admin user is not created yet.
                return;
            }
            if (statistic != null && statistic.ScheduleActions)
            {
                GXSystemLog log = new GXSystemLog(TraceLevel.Verbose);
                log.Message = Server.Properties.Resources.SchedulerServiceIsWorking;
                await _systemLogRepository.AddAsync(User, new GXSystemLog[] { log });
            }
            _logger.LogInformation(Server.Properties.Resources.SchedulerServiceIsWorking);
            try
            {
                //Don't retreave removed items.
                ListSchedules req = new ListSchedules()
                {
                    Filter = new GXSchedule()
                    {
                        Removed = null,
                        Active = true
                    },
                    //Select user and roles information.
                    Select = new string[] { "User", "Role" }
                };
                List<GXTask> tasks = new List<GXTask>();
                GXSchedule[] schedules = await _scheduleRepository.ListAsync(User, req, null, CancellationToken.None);
                DateTime now = DateTime.Now;
                foreach (GXSchedule schedule2 in schedules)
                {
                    GXDateTime dt = new GXDateTime(schedule2.Start, CultureInfo.InvariantCulture);
                    if (Equals(dt, now))
                    {
                        await RunSchedule(schedule2, tasks, now);
                    }
                }
                if (tasks.Any())
                {
                    using (IServiceScope scope = _serviceProvider.CreateScope())
                    {
                        ITaskRepository taskRepository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
                        await taskRepository.AddAsync(User, tasks);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    ISystemLogRepository errors = scope.ServiceProvider.GetRequiredService<ISystemLogRepository>();
                    GXSystemLog error = new GXSystemLog();
                    error.Message = ex.Message;
                    await errors.AddAsync(User, new GXSystemLog[] { error });
                }
            }
        }
        bool Equals(GXDateTime t, DateTime t2)
        {
            if ((t.Skip & Enums.DateTimeSkips.Minute) == 0 &&
                t.Value.Minute != t2.Minute)
            {
                return false;
            }
            if ((t.Skip & Enums.DateTimeSkips.Hour) == 0 &&
                t.Value.Hour != t2.Hour)
            {
                return false;
            }
            if ((t.Skip & Enums.DateTimeSkips.Day) == 0 &&
                t.Value.Day != t2.Day)
            {
                return false;
            }
            if ((t.Skip & Enums.DateTimeSkips.Month) == 0 &&
                t.Value.Month != t2.Month)
            {
                return false;
            }
            if ((t.Skip & Enums.DateTimeSkips.Year) == 0 &&
                t.Value.Year != t2.Year)
            {
                return false;
            }
            return true;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (statistic != null && statistic.ScheduleActions)
            {
                GXSystemLog log = new GXSystemLog(TraceLevel.Verbose);
                log.Message = Server.Properties.Resources.SchedulerServiceIsStopping;
                await _systemLogRepository.AddAsync(User, new GXSystemLog[] { log });
            }
            _logger.LogWarning(Server.Properties.Resources.SchedulerServiceIsStopping);
            _timer?.Change(Timeout.Infinite, 0);
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
