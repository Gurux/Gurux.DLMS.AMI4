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
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Server;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Repository;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;

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
        private readonly IGXEventsNotifier _eventsNotifier;
        private StatisticSettings? statistic;
        private readonly IGXHost _host;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXSchedulerService(ILogger<GXSchedulerService> logger,
            IServiceProvider serviceProvider,
            IGXHost host,
            IConfigurationRepository configurationRepository,
            IGXEventsNotifier eventsNotifier)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configurationRepository = configurationRepository;
            _eventsNotifier = eventsNotifier;
            _host = host;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (ServerSettings.GetDefaultAdminUser(_host) == null)
            {
                await StopAsync(cancellationToken);
                return;
            }
            ListConfiguration req = new ListConfiguration()
            {
                Filter = new GXConfiguration() { Name = GXConfigurations.Statistic }
            };
            GXConfiguration[] confs = _configurationRepository.ListAsync(
                req, null, CancellationToken.None).Result;
            if (confs.Length == 1)
            {
                statistic = JsonSerializer.Deserialize<StatisticSettings>(confs[0].Settings);
            }
            _configurationRepository.Updated += (configurations) =>
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
                var scope = _serviceProvider.CreateScope();
                var enumTypeRepository = scope.ServiceProvider.GetRequiredService<IEnumTypeRepository>();
                log.Type = await enumTypeRepository.GetLogTypeAsync(TargetType.SystemLog, TargetType.Schedule);
                log.Message = ServerHelpers.GetInvariantString(nameof(Properties.Resources.SchedulerServiceIsStarting));
                var repository = scope.ServiceProvider.GetRequiredService<ISystemLogRepository>();
                await repository.AddAsync(TargetType.Schedule, [log]);
            }
            _logger.LogInformation(ServerHelpers.GetInvariantString(nameof(Properties.Resources.SchedulerServiceIsStarting)));
            TimeSpan start = DateTime.Now.AddSeconds(60 - DateTime.Now.Second) - DateTime.Now;
            _timer = new Timer(DoWork, null, start, TimeSpan.FromMinutes(1));
        }

        /// <inheritdoc />
        public async Task RunAsync(ClaimsPrincipal user, GXSchedule schedule)
        {
            using var scope = _serviceProvider.CreateScope();
            List<GXTask> tasks = new List<GXTask>();
            if (statistic != null && statistic.ScheduleActions)
            {
                GXScheduleLog log = new GXScheduleLog(TraceLevel.Verbose);
                log.Schedule = schedule;
                log.Message = Properties.Resources.SchedulerStarted;
                var repository = scope.ServiceProvider.GetRequiredService<IScheduleLogRepository>();
                await repository.AddAsync("Run", [log]);
            }

            await RunSchedule(schedule, tasks, DateTime.Now);
            if (tasks.Count != 0)
            {
                ITaskRepository taskRepository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
                await taskRepository.AddAsync(tasks);
            }
            if (statistic != null && statistic.ScheduleActions)
            {
                GXScheduleLog log = new GXScheduleLog(TraceLevel.Verbose);
                log.Schedule = schedule;
                log.Message = Properties.Resources.SchedulerEnded;
                var repository = scope.ServiceProvider.GetRequiredService<IScheduleLogRepository>();
                await repository.AddAsync("Run", [log]);
            }
        }

        private async Task AddDeviceObjects(
            IDbTransaction transaction,
            GXUser? creator,
            GXSchedule schedule,
            GXDevice dev,
            GXObjectTemplate ot,
            List<GXTask> tasks)
        {
            //Get object.
            GXSelectArgs arg = GXSelectArgs.Select<GXObject>(s => new { s.Id, s.Template });
            arg.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(s => s.Template, o => o.Id);
            arg.Joins.AddInnerJoin<GXObject, GXDevice>(j => j.Device, j => j.Id);
            arg.Where.And<GXObjectTemplate>(q => q.Id == ot.Id);
            arg.Where.And<GXDevice>(q => q.Id == dev.Id);
            GXObject obj = (await _host.Connection.SingleOrDefaultAsync<GXObject>(transaction, arg));
            if (obj == null)
            {
                arg = GXQuery.GetDevicesByUser(s => s.Id, creator.Id, dev.Id);
                arg.Joins.AddInnerJoin<GXDevice, GXDeviceTemplate>(j => j.Template, j => j.Id);
                arg.Joins.AddInnerJoin<GXDeviceTemplate, GXObjectTemplate>(j => j.Id, j => j.DeviceTemplate);
                arg.Where.And<GXObjectTemplate>(q => q.Id == ot.Id);
                dev = (await _host.Connection.SingleOrDefaultAsync<GXDevice>(transaction, arg));
                if (dev == null)
                {
                    //If device template is not included for this device.
                    return;
                }
                obj = await ObjectRepository.CreateLateBindObject(_host, transaction, creator.Id, dev, ot.Id);
            }
            //Add device objects to read.
            GXTask t = new GXTask()
            {
                TriggerSchedule = schedule,
                Object = obj,
                TargetDevice = dev.Id,
                TaskType = TaskType.Read,
            };
            tasks.Add(t);

        }

        private async Task AddDeviceAttributes(
           IDbTransaction transaction,
           GXUser? creator,
           GXSchedule schedule,
           GXDevice dev,
           GXAttributeTemplate at,
           List<GXTask> tasks)
        {
            //Get attribute with device id, object logical name and attribute index.
            GXAttribute? att2 = await ObjectRepository.GetDeviceAttributeUsingLogicalName(_host,
                transaction, creator.Id, dev.Id, at.ObjectTemplate.LogicalName, at.Index);
            if (att2 != null)
            {
                at.Id = att2.Id;
            }
            else
            {
                //If object is not created yet.
            }
            //Add attribute to read.
            GXTask t = new GXTask()
            {
                TriggerSchedule = schedule,
                Attribute = att2,
                TargetDevice = dev.Id,
                TaskType = TaskType.Read,
            };
            tasks.Add(t);
        }

        private async Task RunSchedule(GXSchedule schedule, List<GXTask> tasks, DateTime now)
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IScheduleLogRepository>();
            var scheduleRepository = scope.ServiceProvider.GetRequiredService<IScheduleRepository>();
            ClaimsPrincipal user = ServerHelpers.CreateClaimsPrincipalFromUser(schedule.Creator);
            List<string> users = await scheduleRepository.GetUsersAsync(schedule.Id);
            if (schedule.TraceLevel == TraceLevel.Verbose)
            {
                GXScheduleLog log = new GXScheduleLog(TraceLevel.Verbose);
                log.Schedule = schedule;
                log.Message = Properties.Resources.SchedulerStarted;
                await repository.AddAsync("Cron", [log]);
            }
            await _eventsNotifier.ScheduleStart(users, [schedule]);
            //Save schedule invoker.
            GXUser? creator = schedule.Creator;
            //Read all data for the schedule.
            schedule = await scheduleRepository.ReadAsync(schedule.Id);
            schedule.Creator = creator;
            var deviceGroupRepository = scope.ServiceProvider.GetRequiredService<IDeviceGroupRepository>();
            if (schedule.DeviceGroups != null)
            {
                bool handled = false;
                if ((schedule.DeviceGroupObjectTemplates != null && schedule.DeviceGroupObjectTemplates.Any()) ||
                    (schedule.DeviceGroupAttributeTemplates != null && schedule.DeviceGroupAttributeTemplates.Any()))
                {
                    handled = true;
                    using IDbTransaction transaction = _host.Connection.BeginTransaction();
                    try
                    {
                        foreach (GXDeviceGroup dg in schedule.DeviceGroups)
                        {
                            //Get device group devices.
                            dg.Devices = (await deviceGroupRepository.ReadAsync(dg.Id)).Devices;
                            if (dg.Devices != null && schedule.DeviceGroupObjectTemplates != null)
                            {
                                foreach (GXDevice it in dg.Devices)
                                {
                                    foreach (GXObjectTemplate ot in schedule.DeviceGroupObjectTemplates)
                                    {
                                        await AddDeviceObjects(transaction, creator, schedule, it, ot, tasks);
                                    }
                                }
                            }
                            if (dg.Devices != null && schedule.DeviceGroupAttributeTemplates != null)
                            {
                                foreach (GXDevice it in dg.Devices)
                                {
                                    foreach (GXAttributeTemplate at in schedule.DeviceGroupAttributeTemplates)
                                    {
                                        await AddDeviceAttributes(transaction, creator, schedule, it, at, tasks);
                                    }
                                }
                            }
                        }
                        _host.Connection.CommitTransaction(transaction);
                    }
                    catch (Exception)
                    {
                        _host.Connection.RollbackTransaction(transaction);
                        throw;
                    }
                }
                if (schedule.DeviceAttributeTemplates != null && schedule.DeviceAttributeTemplates.Any())
                {
                    handled = true;
                }
                if (!handled)
                {
                    foreach (GXDeviceGroup dg in schedule.DeviceGroups)
                    {
                        if (schedule.Objects != null && schedule.Objects.Any())
                        {
                            handled = true;
                            GXDeviceGroup g = await deviceGroupRepository.ReadAsync(dg.Id);
                            if (g.Devices != null)
                            {
                                var objectRepository = _serviceProvider.GetRequiredService<IObjectRepository>();
                                foreach (GXDevice dev in g.Devices)
                                {
                                    ListObjects req = new ListObjects()
                                    {
                                        Filter = new GXObject() { Device = dev },
                                    };
                                    var objects = await objectRepository.ListAsync(req, null, default);
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
                            GXDeviceGroup g = await deviceGroupRepository.ReadAsync(dg.Id);
                            //List of create objects.
                            if (g.Devices != null)
                            {
                                var attributeRepository = _serviceProvider.GetRequiredService<IAttributeRepository>();
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
                                    var attributes = await attributeRepository.ListAsync(req, null, default);
                                    //Device attrbutes are read in one batch.
                                    Guid batch = Guid.NewGuid();
                                    foreach (GXAttribute a in schedule.Attributes)
                                    {
                                        GXAttribute? at = attributes.Where(w => w.Template.Id == a.Template.Id).SingleOrDefault();
                                        if (at != null)
                                        {
                                            if (at.CreationTime == null)
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
            }
            if (schedule.Devices != null)
            {
                bool handled = false;
                if ((schedule.DeviceObjectTemplates != null && schedule.DeviceObjectTemplates.Any()) ||
                    (schedule.DeviceAttributeTemplates != null && schedule.DeviceAttributeTemplates.Any()))
                {
                    handled = true;
                    using IDbTransaction transaction = _host.Connection.BeginTransaction();
                    try
                    {
                        foreach (GXDevice it in schedule.Devices)
                        {
                            if (schedule.DeviceObjectTemplates != null)
                            {
                                foreach (GXObjectTemplate ot in schedule.DeviceObjectTemplates)
                                {
                                    await AddDeviceObjects(transaction, creator, schedule, it, ot, tasks);
                                }
                            }
                            if (schedule.DeviceAttributeTemplates != null)
                            {
                                foreach (GXAttributeTemplate at in schedule.DeviceAttributeTemplates)
                                {
                                    await AddDeviceAttributes(transaction, creator, schedule, it, at, tasks);
                                }
                            }
                        }
                        _host.Connection.CommitTransaction(transaction);
                    }
                    catch (Exception)
                    {
                        _host.Connection.RollbackTransaction(transaction);
                        throw;
                    }
                }
                if (schedule.DeviceAttributeTemplates != null && schedule.DeviceAttributeTemplates.Any())
                {
                    handled = true;
                }
                if (!handled)
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
            scheduleRepository.UpdateExecutionTime(schedule);
            await _eventsNotifier.ScheduleCompleate(users, [new GXSchedule() { Id = schedule.Id,
            Name = schedule.Name}]);
            if (schedule.TraceLevel == TraceLevel.Verbose)
            {
                GXScheduleLog log = new GXScheduleLog(TraceLevel.Verbose);
                log.Schedule = schedule;
                log.Message = Properties.Resources.SchedulerEnded;
                await repository.AddAsync("Cron", [log]);
            }
        }

        private async void DoWork(object? state)
        {
            //If admin is not created yet.
            if (ServerSettings.GetDefaultAdminUser(_host) == null)
            {
                return;
            }
            if (statistic != null && statistic.ScheduleActions)
            {
                GXSystemLog log = new GXSystemLog(TraceLevel.Verbose);
                var scope = _serviceProvider.CreateScope();
                var enumTypeRepository = scope.ServiceProvider.GetRequiredService<IEnumTypeRepository>();
                log.Type = await enumTypeRepository.GetLogTypeAsync(TargetType.SystemLog, TargetType.Schedule);
                log.Message = ServerHelpers.GetInvariantString(nameof(Properties.Resources.SchedulerServiceIsWorking));
                var repository = scope.ServiceProvider.GetRequiredService<ISystemLogRepository>();
                await repository.AddAsync(TargetType.Schedule, [log]);
            }
            _logger.LogInformation(ServerHelpers.GetInvariantString(nameof(Properties.Resources.SchedulerServiceIsWorking)));
            try
            {
                //Don't retrieve removed items.
                ListSchedules req = new ListSchedules()
                {
                    Filter = new GXSchedule()
                    {
                        Removed = null,
                        Active = true
                    },
                    //Select user and roles information.
                    Select = ["User", "Role"]
                };
                List<GXTask> tasks = new List<GXTask>();
                using var scope = _serviceProvider.CreateScope();
                var scheduleRepository = scope.ServiceProvider.GetRequiredService<IScheduleRepository>();
                GXSchedule[] schedules = await scheduleRepository.ListAsync(req, null, CancellationToken.None);
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
                    ITaskRepository taskRepository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
                    await taskRepository.AddAsync(tasks);
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
                    await errors.AddAsync(TargetType.Schedule, [error]);
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
                log.Message = ServerHelpers.GetInvariantString(nameof(Properties.Resources.SchedulerServiceIsStopping));
                var repository = _serviceProvider.GetRequiredService<ISystemLogRepository>();
                await repository.AddAsync(TargetType.Schedule, [log]);
            }
            _logger.LogWarning(ServerHelpers.GetInvariantString(nameof(Properties.Resources.SchedulerServiceIsStopping)));
            _timer?.Change(Timeout.Infinite, 0);
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
