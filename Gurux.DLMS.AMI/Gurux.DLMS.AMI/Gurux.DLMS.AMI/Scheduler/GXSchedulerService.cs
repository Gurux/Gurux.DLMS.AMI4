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

using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Db;
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
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Server.Services;

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
        Task RunAsync(ClaimsPrincipal user, GXSchedule schedule, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    internal class GXSchedulerService(ILogger<GXSchedulerService> logger,
        IServiceProvider serviceProvider,
        IDbConnectionFactory dbFactory) : IGXScheduleService, IHostedService, IDisposable
    {
        private readonly ILogger _logger = logger;
        private GXAsyncTimer? _timer;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly IDbConnectionFactory _dbFactory = dbFactory;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await using var connection = await _dbFactory.GetConnectionAsync(cancellationToken);
            var adminUser = await ServerSettings.GetDefaultAdminUserAsync(connection, cancellationToken: cancellationToken);
            if (adminUser == null)
            {
                await StopAsync(cancellationToken);
                return;
            }
            if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
            {
                _logger.LogInformation("{Message}", ServerHelpers.GetInvariantString(nameof(Properties.Resources.SchedulerServiceIsStarting)));
            }
            TimeSpan start = DateTime.Now.AddSeconds(60 - DateTime.Now.Second) - DateTime.Now;
            _timer = new GXAsyncTimer(DoWork, _logger);
            _timer.Change(start, TimeSpan.FromMinutes(1));
        }

        /// <inheritdoc />
        public Task RunAsync(ClaimsPrincipal user, GXSchedule schedule, CancellationToken cancellationToken)
            => _serviceProvider.RunAsUserAsync(user,
                services => RunAsUserAsync(services, schedule, user, cancellationToken), cancellationToken);

        private async Task RunAsUserAsync(IServiceProvider services, GXSchedule schedule, ClaimsPrincipal actor, CancellationToken cancellationToken)
        {
            List<GXTask> tasks = [];
            var operationId = Guid.NewGuid();
            var handler = services.GetRequiredService<IWorkflowHandler>();
            var factory = services.GetRequiredService<IDbConnectionFactory>();
            var operation = new GXWorkflowOperation { Id = operationId, Code = "schedule.run", TargetId = schedule.Id.ToString(),
                ParentId = GXWorkflowService.CurrentOperationId,
                ActorId = GXWorkflowService.CurrentOperationId != null ? GXWorkflowService.CurrentActorId :
                    actor.Identity?.IsAuthenticated == true ? ServerHelpers.GetUserId(actor) : null };
            bool dispatched = false;
            await WorkflowOperationRepository.StartTrustedAsync(factory, handler, operation, cancellationToken);
            try
            {

                await RunSchedule(schedule, tasks, DateTime.Now, cancellationToken);
                if (tasks.Count != 0)
                {
                    ITaskRepository taskRepository = services.GetRequiredService<ITaskRepository>();
                    foreach (var task in tasks)
                    {
                        task.WorkflowOperationId = operationId;
                        task.WorkflowActorId = operation.ActorId;
                    }
                    dispatched = (await taskRepository.AddAsync(tasks, cancellationToken)).Any();
                }
                if (!dispatched) {
                    await WorkflowOperationRepository.FinishTrustedAsync(factory, handler, operationId,
                    WorkflowOperationState.Completed, null, null, cancellationToken);
                }
            }
            catch (Exception error)
            {
                if (!dispatched)
                {
                    await using var connection = await factory.GetConnectionAsync(CancellationToken.None);
                    var current = await connection.SingleOrDefaultAsync<GXWorkflowOperation>(
                        GXSelectArgs.SelectAll<GXWorkflowOperation>(o => o.Id == operationId), CancellationToken.None);
                    if (current?.State == WorkflowOperationState.Running)
                        {
                        await WorkflowOperationRepository.FinishTrustedAsync(factory, handler, operationId, WorkflowOperationState.Failed,
                            error.GetType().Name, error.Message[..Math.Min(error.Message.Length, 2048)], CancellationToken.None);
                    }
                }
                throw;
            }
        }
        private async static Task AddDeviceObjects(
            GXDbConnection connection, IDbTransaction transaction,
            GXUser? creator,
            GXSchedule schedule,
            GXDevice dev,
            GXObjectTemplate ot,
            List<GXTask> tasks)
        {
            //Get object.
            GXSelectArgs arg = GXSelectArgs.Select<GXObject>(s => new { s.Id, s.Template });
            arg.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(s => s.Template!, o => o.Id);
            arg.Joins.AddInnerJoin<GXObject, GXDevice>(j => j.Device!, j => j.Id);
            arg.Where.And<GXObjectTemplate>(q => q.Id == ot.Id);
            arg.Where.And<GXDevice>(q => q.Id == dev.Id);
            GXObject? obj = (await connection.SingleOrDefaultAsync<GXObject>(transaction, arg));
            if (obj == null)
            {
                string creatorId = creator?.Id
                    ?? throw new InvalidOperationException("A scheduled object requires a creator.");
                arg = GXQuery.GetDevicesByUser(s => s.Id, creatorId, dev.Id);
                arg.Joins.AddInnerJoin<GXDevice, GXDeviceTemplate>(j => j.Template!, j => j.Id);
                arg.Joins.AddInnerJoin<GXDeviceTemplate, GXObjectTemplate>(j => j.Id, j => j.DeviceTemplate!);
                arg.Where.And<GXObjectTemplate>(q => q.Id == ot.Id);
                var selectedDevice = await connection.SingleOrDefaultAsync<GXDevice>(transaction, arg);
                if (selectedDevice == null)
                {
                    //If device template is not included for this device.
                    return;
                }
                dev = selectedDevice;
                obj = await ObjectRepository.CreateLateBindObject(connection, transaction, creatorId, dev, ot.Id);
            }
            //Add device objects to read.
            GXTask t = new()
            {
                TriggerSchedule = schedule,
                Object = obj,
                TargetDevice = dev.Id,
                TaskType = TaskType.Read,
            };
            tasks.Add(t);

        }

        private async static Task AddDeviceAttributes(
           GXDbConnection connection, IDbTransaction transaction,
           GXUser? creator,
           GXSchedule schedule,
           GXDevice dev,
           GXAttributeTemplate at,
           List<GXTask> tasks)
        {
            //Get attribute with device id, object logical name and attribute index.
            string creatorId = creator?.Id
                ?? throw new InvalidOperationException("A scheduled attribute requires a creator.");
            string logicalName = at.ObjectTemplate?.LogicalName
                ?? throw new InvalidOperationException("A scheduled attribute requires an object logical name.");
            GXAttribute? att2 = await ObjectRepository.GetDeviceAttributeUsingLogicalName(connection,
                transaction, creatorId, dev.Id, logicalName, at.Index);
            if (att2 != null)
            {
                at.Id = att2.Id;
            }
            else
            {
                //If object is not created yet.
            }
            //Add attribute to read.
            GXTask t = new()
            {
                TriggerSchedule = schedule,
                Attribute = att2,
                TargetDevice = dev.Id,
                TaskType = TaskType.Read,
            };
            tasks.Add(t);
        }

        private async Task RunSchedule(GXSchedule schedule, List<GXTask> tasks, DateTime now, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<ILogRepository>().ForSource("schedule");
            var scheduleRepository = scope.ServiceProvider.GetRequiredService<IScheduleRepository>();
            // RunAsUserAsync publishes schedule lifecycle events through the durable operation.
            //Save schedule invoker.
            GXUser? creator = schedule.Creator;
            //Read all data for the schedule.
            schedule = await scheduleRepository.ReadAsync(schedule.Id, null, cancellationToken);
            schedule.Creator = creator;
            if (schedule.TraceLevel == TraceLevel.Verbose)
            {
                GXLog log = new(TraceLevel.Verbose)
                {
                    Creator = schedule.Id,
                    Message = Properties.Resources.SchedulerStarted
                };
                await repository.AddAsync("Cron", [log], cancellationToken);
            }
            var deviceGroupRepository = scope.ServiceProvider.GetRequiredService<IDeviceGroupRepository>();
            await using var connection = await _dbFactory.GetConnectionAsync(cancellationToken);
            if (schedule.DeviceGroups != null)
            {
                bool handled = false;
                if ((schedule.DeviceGroupObjectTemplates != null && schedule.DeviceGroupObjectTemplates.Count != 0) ||
                    (schedule.DeviceGroupAttributeTemplates != null && schedule.DeviceGroupAttributeTemplates.Count != 0))
                {
                    handled = true;
                    await using var transaction = await connection.BeginTransactionAsync();
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
                                        await AddDeviceObjects(connection, transaction, creator, schedule, it, ot, tasks);
                                    }
                                }
                            }
                            if (dg.Devices != null && schedule.DeviceGroupAttributeTemplates != null)
                            {
                                foreach (GXDevice it in dg.Devices)
                                {
                                    foreach (GXAttributeTemplate at in schedule.DeviceGroupAttributeTemplates)
                                    {
                                        await AddDeviceAttributes(connection, transaction, creator, schedule, it, at, tasks);
                                    }
                                }
                            }
                        }
                        await transaction.CommitAsync();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
                if (schedule.DeviceAttributeTemplates != null && schedule.DeviceAttributeTemplates.Count != 0)
                {
                    handled = true;
                }
                if (!handled)
                {
                    foreach (GXDeviceGroup dg in schedule.DeviceGroups)
                    {
                        if (!handled)
                        {
                            GXTask t = new()
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
                if ((schedule.DeviceObjectTemplates != null && schedule.DeviceObjectTemplates.Count != 0) ||
                    (schedule.DeviceAttributeTemplates != null && schedule.DeviceAttributeTemplates.Count != 0))
                {
                    handled = true;
                    await using var transaction = await connection.BeginTransactionAsync();
                    try
                    {
                        foreach (GXDevice it in schedule.Devices)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (schedule.DeviceObjectTemplates != null)
                            {
                                foreach (GXObjectTemplate ot in schedule.DeviceObjectTemplates)
                                {
                                    await AddDeviceObjects(connection, transaction, creator, schedule, it, ot, tasks);
                                }
                            }
                            if (schedule.DeviceAttributeTemplates != null)
                            {
                                foreach (GXAttributeTemplate at in schedule.DeviceAttributeTemplates)
                                {
                                    await AddDeviceAttributes(connection, transaction, creator, schedule, it, at, tasks);
                                }
                            }
                        }
                        await transaction.CommitAsync();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
                if (schedule.DeviceAttributeTemplates != null && schedule.DeviceAttributeTemplates.Count != 0)
                {
                    handled = true;
                }
                if (!handled)
                {
                    foreach (GXDevice it in schedule.Devices)
                    {
                            cancellationToken.ThrowIfCancellationRequested();
                        //Add device read.
                        GXTask t = new()
                        {
                            TriggerSchedule = schedule,
                            Device = it,
                            TaskType = TaskType.Read,
                        };
                        tasks.Add(t);
                    }
                }
            }
            if (schedule.ScriptMethods != null)
            {
                foreach (GXScriptMethod it in schedule.ScriptMethods)
                {
                    GXTask t = new()
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
                    GXTask t = new()
                    {
                        TriggerSchedule = schedule,
                        Module = it,
                        TaskType = TaskType.Action,
                    };
                    tasks.Add(t);
                }
            }
            schedule.ExecutionTime = now;
            await scheduleRepository.UpdateExecutionTimeAsync(schedule);
            if (schedule.TraceLevel == TraceLevel.Verbose)
            {
                GXLog log = new(TraceLevel.Verbose)
                {
                    Creator = schedule.Id,
                    Message = Properties.Resources.SchedulerEnded
                };
                await repository.AddAsync("Cron", [log], cancellationToken);
            }
        }

        private async Task DoWork(CancellationToken cancellationToken)
        {
            ClaimsPrincipal? admin;
            await using (var connection = await _dbFactory.GetConnectionAsync(cancellationToken))
            {
                admin = await ServerSettings.GetDefaultAdminUserAsync(connection, cancellationToken: cancellationToken);
            }
            if (admin == null) {
                return;
            }
            await _serviceProvider.RunAsUserAsync(admin,
                services => DoWorkAsAdminAsync(services, cancellationToken), cancellationToken);
        }
        private async Task DoWorkAsAdminAsync(IServiceProvider services, CancellationToken cancellationToken)
        {
            try
            {
                if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
                {
                    _logger.LogInformation("{Message}", ServerHelpers.GetInvariantString(nameof(Properties.Resources.SchedulerServiceIsWorking)));
                }
                //Don't retrieve removed items.
                ListSchedules req = new()
                {
                    Filter = new GXSchedule()
                    {
                        Removed = null,
                        Enabled = true
                    },
                    // Select the creator; current roles are loaded before execution.
                    Select = ["User"]
                };
                var scheduleRepository = services.GetRequiredService<IScheduleRepository>();
                var userRepository = services.GetRequiredService<IUserRepository>();
                IEnumerable<GXSchedule> schedules = await scheduleRepository.ListAsync(req, null, cancellationToken);
                DateTime now = DateTime.Now;
                foreach (GXSchedule schedule2 in schedules)
                {
                    if (IsScheduledNow(schedule2.Start, now))
                    {
                        // Load the creator's current roles before entering the schedule's user scope.
                        string creatorId = schedule2.Creator?.Id
                            ?? throw new InvalidOperationException("A scheduled run requires a creator.");
                        schedule2.Creator = await userRepository.ReadAsync(creatorId);
                        var user = ServerHelpers.CreateClaimsPrincipalFromUser(schedule2.Creator);
                        await _serviceProvider.RunAsUserAsync(user,
                            scoped => RunAsUserAsync(scoped, schedule2, new ClaimsPrincipal(), cancellationToken), cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduler service failed while processing schedules.");
            }
        }
        // Schedules are evaluated once per minute, as before the Scheduling migration.
        private static bool IsScheduledNow(string? expression, DateTime now)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return false;
            }
            var schedule = new Gurux.Scheduling.GXDateTime(expression, CultureInfo.InvariantCulture);
            schedule.Skip |= Gurux.Scheduling.Enums.DateTimeSkips.Second | Gurux.Scheduling.Enums.DateTimeSkips.Ms;
            if (schedule.Compare(now) != 0)
            {
                return false;
            }
            var minute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, now.Kind);
            try
            {
                var next = Gurux.Scheduling.GXDateTime.GetNextScheduledDates(minute, schedule, 1)[0];
                return next == minute;
            }
            catch (ArgumentOutOfRangeException)
            {
                // A finite schedule can have no remaining occurrences.
                return false;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_timer != null) {
                await _timer.StopAsync(cancellationToken);
            }
            if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
            {
                _logger.LogWarning("{Message}", ServerHelpers.GetInvariantString(nameof(Properties.Resources.SchedulerServiceIsStopping)));
            }

        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
