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
using System.Globalization;
using System.Security.Claims;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Scheduler;
using System.Linq.Expressions;
using System.Diagnostics;
using System.Data;
using Gurux.DLMS.AMI.Client.Pages.Schedule;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc/>
    public class ScheduleRepository : IScheduleRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IServiceProvider _serviceProvider;
        private readonly IScheduleGroupRepository _scheduleGroupRepository;


        /// <summary>
        /// Constructor.
        /// </summary>
        public ScheduleRepository(IGXHost host,
            IServiceProvider serviceProvider,
            IGXEventsNotifier eventsNotifier,
            IScheduleGroupRepository scheduleGroupRepository)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _serviceProvider = serviceProvider;
            _scheduleGroupRepository = scheduleGroupRepository;
        }

        /// <summary>
        /// Returns device groups that are added for the schedule.
        /// </summary>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <returns>List of device groups.</returns>
        private async Task<List<GXDeviceGroup>> GetDeviceGroupsByScheduleId(Guid scheduleId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXDeviceGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXDeviceGroup, GXScheduleToDeviceGroup>(a => a.Id, b => b.DeviceGroupId);
            arg.Where.And<GXScheduleToDeviceGroup>(where => where.Removed == null && where.ScheduleId == scheduleId);
            return (await _host.Connection.SelectAsync<GXDeviceGroup>(arg));
        }

        /// <summary>
        /// Returns script methods that are added for the schedule.
        /// </summary>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <returns>List of script methods.</returns>
        private async Task<List<GXScriptMethod>> GetScriptsByScheduleId(Guid scheduleId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXScriptMethod>();
            arg.Joins.AddInnerJoin<GXScriptMethod, GXScheduleScript>(a => a.Id, b => b.ScriptMethodId);
            arg.Where.And<GXScheduleScript>(where => where.Removed == null && where.ScheduleId == scheduleId);
            return (await _host.Connection.SelectAsync<GXScriptMethod>(arg));
        }

        /// <summary>
        /// Returns devices that are added for the schedule.
        /// </summary>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <returns>List of devices.</returns>
        private async Task<List<GXDevice>> GetDevicesByScheduleId(Guid scheduleId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXDevice>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXDevice, GXScheduleToDevice>(a => a.Id, b => b.DeviceId);
            arg.Where.And<GXScheduleToDevice>(where => where.Removed == null && where.ScheduleId == scheduleId);
            return (await _host.Connection.SelectAsync<GXDevice>(arg));
        }

        /// <summary>
        /// Returns objects that are added for the schedule.
        /// </summary>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <returns>List of objects.</returns>
        private async Task<List<GXObject>> GetObjectsByScheduleId(Guid scheduleId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXObject>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXObject, GXScheduleToObject>(a => a.Id, b => b.ObjectId);
            arg.Where.And<GXScheduleToObject>(where => where.Removed == null && where.ScheduleId == scheduleId);
            return (await _host.Connection.SelectAsync<GXObject>(arg));
        }

        /// <summary>
        /// Returns attributes that are added for the schedule.
        /// </summary>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <returns>List of devices.</returns>
        private async Task<List<GXAttribute>> GetAttributesByScheduleId(Guid scheduleId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXAttribute>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXAttribute, GXScheduleToAttribute>(a => a.Id, b => b.AttributeId);
            arg.Where.And<GXScheduleToAttribute>(where => where.Removed == null && where.ScheduleId == scheduleId);
            return (await _host.Connection.SelectAsync<GXAttribute>(arg));
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal user, Guid? scheduleId)
        {
            GXSelectArgs args = GXQuery.GetUsersBySchedule(ServerHelpers.GetUserId(user), scheduleId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            return users.Select(s => s.Id).ToList();
        }
        /// <inheritdoc/>
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal user, IEnumerable<Guid>? scheduleIds)
        {
            GXSelectArgs args = GXQuery.GetUsersBySchedules(ServerHelpers.GetUserId(user), scheduleIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            return users.Select(s => s.Id).ToList();
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(ClaimsPrincipal User,
            IEnumerable<Guid> schedulers,
            bool delete)
        {
            if (User == null || (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.ScheduleManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXSchedule>(a => a.Id, q => schedulers.Contains(q.Id));
            List<GXSchedule> list = _host.Connection.Select<GXSchedule>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXSchedule, List<string>> updates = new Dictionary<GXSchedule, List<string>>();
            using IDbTransaction transaction = _host.Connection.BeginTransaction();
            try
            {
                foreach (var it in list)
                {
                    List<string> users = await GetUsersAsync(User, it.Id);
                    if (!delete)
                    {
                        it.Removed = now;
                        _host.Connection.Update(transaction, GXUpdateArgs.Update(it, q => q.Removed));
                    }
                    updates[it] = users;
                }
                if (delete)
                {
                    await _host.Connection.DeleteAsync(transaction, GXDeleteArgs.DeleteRange(list));
                }
                _host.Connection.CommitTransaction(transaction);
            }
            catch (Exception)
            {
                _host.Connection.RollbackTransaction(transaction);
                throw;
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.ScheduleDelete(it.Value, new GXSchedule[] { it.Key });
            }
            if (!delete)
            {
                List<GXScheduleLog> logs = new List<GXScheduleLog>();
                foreach (var it in updates.Keys)
                {
                    logs.Add(new GXScheduleLog(TraceLevel.Info)
                    {
                        CreationTime = DateTime.Now,
                        Schedule = it,
                        Message = Properties.Resources.ScheduleRemoved
                    });
                }
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    var scheduleLogRepository = scope.ServiceProvider.GetRequiredService<IScheduleLogRepository>();
                    await scheduleLogRepository.AddAsync(User, logs);
                }
            }
        }

        /// <inheritdoc/>
        public async Task<GXSchedule[]> ListAsync(
        ClaimsPrincipal user,
            ListSchedules? request,
            ListSchedulesResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && user.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the schedules.
                arg = GXSelectArgs.SelectAll<GXSchedule>();
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetSchedulesByUser(userId, null);
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXSchedule>(w => !request.Exclude.Contains(w.Id));
                }
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXSchedule>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXSchedule>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXSchedule>(q => q.CreationTime);
                arg.Descending = true;
            }
            if (request != null && (request.Select & TargetType.User) != 0)
            {
                //User info is also read.
                arg.Columns.Add<GXUser>(s => new { s.Id, s.UserName });
                arg.Joins.AddInnerJoin<GXSchedule, GXUser>(j => j.Creator, j => j.Id);
            }
            GXSchedule[] schedules = (await _host.Connection.SelectAsync<GXSchedule>(arg)).ToArray();
            if (response != null)
            {
                response.Schedules = schedules;
                if (response.Count == 0)
                {
                    response.Count = schedules.Length;
                }
            }
            return schedules;
        }

        /// <inheritdoc/>
        public async Task<GXSchedule> ReadAsync(
            ClaimsPrincipal user,
            Guid id)
        {
            bool isAdmin = false;
            if (user != null)
            {
                isAdmin = user.IsInRole(GXRoles.Admin);
            }
            GXSelectArgs arg;
            if (user == null || isAdmin)
            {
                //Admin can see all the schedules.
                arg = GXSelectArgs.SelectAll<GXSchedule>(w => w.Id == id);
                arg.Joins.AddInnerJoin<GXSchedule, GXScheduleGroupSchedule>(x => x.Id, y => y.ScheduleId);
                arg.Joins.AddInnerJoin<GXScheduleGroupSchedule, GXScheduleGroup>(j => j.ScheduleGroupId, j => j.Id);
                arg.Where.And<GXScheduleGroup>(w => w.Removed == null);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetSchedulesByUser(userId, id);
                arg.Joins.AddInnerJoin<GXScheduleGroupSchedule, GXScheduleGroup>(j => j.ScheduleGroupId, j => j.Id);
            }
            arg.Columns.Add<GXScheduleGroup>();
            arg.Columns.Exclude<GXScheduleGroup>(e => e.Schedules);
            arg.Distinct = true;
            GXSchedule schedule = await _host.Connection.SingleOrDefaultAsync<GXSchedule>(arg);
            if (schedule == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            //Get objects with own query. It's faster for some DBs.
            arg = GXSelectArgs.Select<GXObject>(s => new { s.Id, s.Template }, q => q.Removed == null);
            arg.Distinct = true;
            arg.Columns.Add<GXObjectTemplate>(s => new { s.Id, s.Name, s.LogicalName });
            arg.Joins.AddInnerJoin<GXObject, GXScheduleToObject>(s => s.Id, o => o.ObjectId);
            arg.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(s => s.Template, o => o.Id);
            arg.Where.And<GXScheduleToObject>(w => w.ScheduleId == id && w.Removed == null);
            schedule.Objects = _host.Connection.Select<GXObject>(arg);
            //Get attributes with own query. It's faster for some DBs.
            arg = GXSelectArgs.Select<GXAttribute>(s => new { s.Id, s.Object, s.Template }, q => q.Removed == null);
            arg.Distinct = true;
            arg.Columns.Add<GXObject>(s => new { s.Id, s.Template });
            arg.Columns.Add<GXObjectTemplate>(s => new { s.Id, s.Name, s.LogicalName });
            arg.Columns.Add<GXAttributeTemplate>(s => new { s.Id, s.Name, s.Index });
            arg.Joins.AddInnerJoin<GXAttribute, GXObject>(s => s.Object, o => o.Id);
            arg.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(s => s.Template, o => o.Id);
            arg.Joins.AddInnerJoin<GXAttribute, GXScheduleToAttribute>(s => s.Id, o => o.AttributeId);
            arg.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(s => s.Template, o => o.Id);
            arg.Where.And<GXScheduleToAttribute>(w => w.ScheduleId == id && w.Removed == null);
            arg.Columns.Exclude<GXObject>(e => e.Attributes);
            schedule.Attributes = _host.Connection.Select<GXAttribute>(arg);
            //Get devices with own query. It's faster for some DBs.
            arg = GXSelectArgs.Select<GXDevice>(s => new { s.Id, s.Name, s.Template }, q => q.Removed == null);
            arg.Distinct = true;
            arg.Columns.Add<GXDeviceTemplate>(s => new { s.Id, s.Name });
            arg.Joins.AddInnerJoin<GXDevice, GXScheduleToDevice>(s => s.Id, o => o.DeviceId);
            arg.Joins.AddInnerJoin<GXDevice, GXDeviceTemplate>(s => s.Template, o => o.Id);
            arg.Where.And<GXScheduleToDevice>(w => w.ScheduleId == id && w.Removed == null);
            schedule.Devices = _host.Connection.Select<GXDevice>(arg);

            //Get devices groups with own query. It's faster for some DBs.
            arg = GXSelectArgs.Select<GXDeviceGroup>(s => new { s.Id, s.Name }, q => q.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXDeviceGroup, GXScheduleToDeviceGroup>(s => s.Id, o => o.DeviceGroupId);
            arg.Where.And<GXScheduleToDeviceGroup>(w => w.ScheduleId == id && w.Removed == null);
            schedule.DeviceGroups = _host.Connection.Select<GXDeviceGroup>(arg);
            //Get script methods with own query. It's faster for some DBs.
            arg = GXSelectArgs.Select<GXScriptMethod>(s => new { s.Id });
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXScriptMethod, GXScheduleScript>(s => s.Id, o => o.ScriptMethodId);
            arg.Where.And<GXScheduleScript>(w => w.ScheduleId == id && w.Removed == null);
            schedule.ScriptMethods = _host.Connection.Select<GXScriptMethod>(arg);
            return schedule;
        }

        /// <inheritdoc/>
        public void UpdateExecutionTime(GXSchedule schedule)
        {
            GXUpdateArgs args = GXUpdateArgs.Update(schedule, c => c.ExecutionTime);
            _host.Connection.Update(args);
        }

        /// <inheritdoc/>
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal user,
            IEnumerable<GXSchedule> schedulers,
            Expression<Func<GXSchedule, object?>>? columns)
        {
            DateTime now = DateTime.Now;
            List<Guid> list = new List<Guid>();
            List<GXScheduleToAttribute> list2 = new List<GXScheduleToAttribute>();
            Dictionary<GXSchedule, List<string>> updates = new Dictionary<GXSchedule, List<string>>();
            List<GXScheduleGroup>? defaultGroups = null;
            var newGroups = schedulers.Where(w => w.Id == Guid.Empty).ToList();
            var updatedGroups = schedulers.Where(w => w.Id != Guid.Empty).ToList();

            //Map schedule groups to schedule.
            Dictionary<GXSchedule, List<GXScheduleGroup>> scheduleGroups = new Dictionary<GXSchedule, List<GXScheduleGroup>>();
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                IScheduleGroupRepository scheduleGroupRepository = scope.ServiceProvider.GetRequiredService<IScheduleGroupRepository>();
                foreach(var it in updatedGroups)
                {
                    scheduleGroups.Add(it, await scheduleGroupRepository.GetJoinedScheduleGroups(user, it.Id));
                }
            }
            //Get notified users.
            if (newGroups.Any())
            {
                var first = newGroups.First();
                var users = await GetUsersAsync(user, first.Id);
                foreach (var it in newGroups)
                {
                    updates[it] = users;
                }
            }
            foreach (var it in updatedGroups)
            {
                updates[it] = await GetUsersAsync(user, it.Id);
            }
            using IDbTransaction transaction = _host.Connection.BeginTransaction();

            foreach (GXSchedule schedule in schedulers)
            {
                //Verify the name.
                if (string.IsNullOrEmpty(schedule.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                //Verify start date time.
                try
                {
                    if (string.IsNullOrEmpty(schedule.Start))
                    {
                        throw new ArgumentNullException(Properties.Resources.InvalidStartTime);
                    }
                    if (schedule.ScheduleGroups == null || !schedule.ScheduleGroups.Any())
                    {
                        if (defaultGroups == null)
                        {
                            ListScheduleGroups request = new ListScheduleGroups() { Filter = new GXScheduleGroup() { Default = true } };
                            defaultGroups = new List<GXScheduleGroup>(await _scheduleGroupRepository.ListAsync(user, request, null, CancellationToken.None));
                        }
                        schedule.ScheduleGroups = defaultGroups;
                        if (!schedule.ScheduleGroups.Any())
                        {
                            throw new ArgumentNullException(Properties.Resources.ArrayIsEmpty);
                        }
                    }
                    //Schedule times are saved in InvariantCulture.
                    schedule.Start = new GXDateTime(schedule.Start, CultureInfo.InvariantCulture).ToFormatString(CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    throw new ArgumentException(string.Format("Invalid start time: '{0}'", schedule.Start));
                }
            }
            try
            {
                if (newGroups.Any())
                {
                    foreach (var it in newGroups)
                    {
                        it.CreationTime = now;
                        it.Creator = new GXUser() { Id = ServerHelpers.GetUserId(user) };
                    }
                    GXInsertArgs args = GXInsertArgs.InsertRange(newGroups);
                    args.Exclude<GXSchedule>(e => new
                    {
                        e.Updated,
                        e.Removed,
                    });
                    await _host.Connection.InsertAsync(transaction, args);
                    foreach (var it in newGroups)
                    {
                        list.Add(it.Id);
                    }
                }
                foreach (var schedule in updatedGroups)
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXSchedule>(q => q.ConcurrencyStamp, where => where.Id == schedule.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != schedule.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    schedule.Updated = now;
                    schedule.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(schedule, columns);
                    args.Exclude<GXSchedule>(q => new
                    {
                        q.CreationTime,
                        q.ExecutionTime,
                        q.ScheduleGroups,
                        q.Attributes,
                        q.Objects,
                        q.Devices,
                        q.DeviceGroups,
                        q.ScriptMethods,
                        q.Creator
                    });
                    _host.Connection.Update(transaction, args);
                    //Map schedule to schedule groups.
                    {
                        var comparer = new UniqueComparer<GXScheduleGroup, Guid>();
                        List<GXScheduleGroup> removed = scheduleGroups[schedule].Except(schedule.ScheduleGroups, comparer).ToList();
                        List<GXScheduleGroup> added = schedule.ScheduleGroups.Except(scheduleGroups[schedule], comparer).ToList();
                        if (removed.Any())
                        {
                            RemoveSchedulesFromScheduleGroup(transaction, schedule.Id, removed);
                        }
                        if (added.Any())
                        {
                            AddScheduleToScheduleGroups(transaction, schedule.Id, added);
                        }
                    }
                    //Map attributes to schedule.
                    {
                        List<GXAttribute> attributes = await GetAttributesByScheduleId(schedule.Id);
                        var comparer = new UniqueComparer<GXAttribute, Guid>();
                        List<GXAttribute> removed = attributes.Except(schedule.Attributes, comparer).ToList();
                        List<GXAttribute> added = schedule.Attributes.Except(attributes, comparer).ToList();
                        if (removed.Any())
                        {
                            RemoveAttributesFromSchedule(transaction, schedule.Id, removed);
                        }
                        if (added.Any())
                        {
                            AddAttributesToSchedule(transaction, schedule.Id, added);
                        }
                    }

                    //Map objects to schedule.
                    {
                        List<GXObject> objects = await GetObjectsByScheduleId(schedule.Id);
                        var comparer = new UniqueComparer<GXObject, Guid>();
                        List<GXObject> removed = objects.Except(schedule.Objects, comparer).ToList();
                        List<GXObject> added = schedule.Objects.Except(objects, comparer).ToList();
                        if (removed.Any())
                        {
                            RemoveObjectsFromSchedule(transaction, schedule.Id, removed);
                        }
                        if (added.Any())
                        {
                            AddObjectsToSchedule(transaction, schedule.Id, added);
                        }
                    }
                    //Map devices to schedule.
                    {
                        List<GXDevice> devices = await GetDevicesByScheduleId(schedule.Id);
                        var comparer = new UniqueComparer<GXDevice, Guid>();
                        List<GXDevice> removed = devices.Except(schedule.Devices, comparer).ToList();
                        List<GXDevice> added = schedule.Devices.Except(devices, comparer).ToList();
                        if (removed.Any())
                        {
                            RemoveDevicesFromSchedule(transaction, schedule.Id, removed);
                        }
                        if (added.Any())
                        {
                            AddDevicesToSchedule(transaction, schedule.Id, added);
                        }
                    }
                    //Map device groups to schedule.
                    {
                        List<GXDeviceGroup> devicegroups = await GetDeviceGroupsByScheduleId(schedule.Id);
                        var comparer = new UniqueComparer<GXDeviceGroup, Guid>();
                        List<GXDeviceGroup> removed = devicegroups.Except(schedule.DeviceGroups, comparer).ToList();
                        List<GXDeviceGroup> added = schedule.DeviceGroups.Except(devicegroups, comparer).ToList();
                        if (removed.Any())
                        {
                            RemoveDeviceGroupsFromSchedule(transaction, schedule.Id, removed);
                        }
                        if (added.Any())
                        {
                            AddDeviceGroupsToSchedule(transaction, schedule.Id, added);
                        }
                    }
                    //Map scripts to schedule.
                    {
                        List<GXScriptMethod> scripts = await GetScriptsByScheduleId(schedule.Id);
                        var comparer = new UniqueComparer<GXScriptMethod, Guid>();
                        List<GXScriptMethod> removed = scripts.Except(schedule.ScriptMethods, comparer).ToList();
                        List<GXScriptMethod> added = schedule.ScriptMethods.Except(scripts, comparer).ToList();
                        if (removed.Any())
                        {
                            RemoveScriptMethodsFromSchedule(transaction, schedule.Id, removed);
                        }
                        if (added.Any())
                        {
                            AddScriptMethodsToSchedule(transaction, schedule.Id, added);
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
            foreach (var it in updates)
            {
                await _eventsNotifier.ScheduleUpdate(it.Value, new GXSchedule[] { it.Key });
            }
            List<GXScheduleLog> logs = new List<GXScheduleLog>();
            foreach (var it in updates.Keys)
            {
                logs.Add(new GXScheduleLog(TraceLevel.Info)
                {
                    CreationTime = DateTime.Now,
                    Schedule = it,
                    Message = it.CreationTime == now ?
                    Properties.Resources.ScheduleInstalled :
                    Properties.Resources.ScheduleUpdated
                });
            }
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                var scheduleLogRepository = scope.ServiceProvider.GetRequiredService<IScheduleLogRepository>();
                await scheduleLogRepository.AddAsync(user, logs);
            }
            return list.ToArray();
        }

        /// <summary>
        /// Add objects to schedule.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="objects">Objects that are added for the schedule.</param>
        public void AddObjectsToSchedule(IDbTransaction transaction, Guid scheduleId, IEnumerable<GXObject> objects)
        {
            DateTime now = DateTime.Now;
            List<GXScheduleToObject> list = new List<GXScheduleToObject>();
            foreach (GXObject it in objects)
            {
                list.Add(new GXScheduleToObject()
                {
                    ScheduleId = scheduleId,
                    ObjectId = it.Id,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map objects from schedule.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="objects">Objects that are removed from the schedule.</param>
        public void RemoveObjectsFromSchedule(IDbTransaction transaction, Guid scheduleId, IEnumerable<GXObject> objects)
        {
            var args = GXDeleteArgs.DeleteAll<GXScheduleToObject>();
            foreach (var it in objects)
            {
                args.Where.Or<GXScheduleToObject>(w => w.ObjectId == it.Id &&
                    w.ScheduleId == scheduleId);
            }
            _host.Connection.Delete(transaction, args);
        }

        /// <summary>
        /// Add attributes to schedule.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="attributes">Objects that are added for the schedule.</param>
        public void AddAttributesToSchedule(IDbTransaction transaction, Guid scheduleId, IEnumerable<GXAttribute> attributes)
        {
            DateTime now = DateTime.Now;
            List<GXScheduleToAttribute> list = new List<GXScheduleToAttribute>();
            foreach (GXAttribute it in attributes)
            {
                list.Add(new GXScheduleToAttribute()
                {
                    ScheduleId = scheduleId,
                    AttributeId = it.Id,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove attributes from schedule.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="attributes">Objects that are removed from the schedule.</param>
        public void RemoveAttributesFromSchedule(IDbTransaction transaction, Guid scheduleId, IEnumerable<GXAttribute> attributes)
        {
            var args = GXDeleteArgs.DeleteAll<GXScheduleToAttribute>();
            foreach (var it in attributes)
            {
                args.Where.Or<GXScheduleToAttribute>(w => w.AttributeId == it.Id &&
                    w.ScheduleId == scheduleId);
            }
            _host.Connection.Delete(transaction, args);
        }

        /// <summary>
        /// Add devices to schedule.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="devices">Devices that are added for the schedule.</param>
        public void AddDevicesToSchedule(IDbTransaction transaction, Guid scheduleId, IEnumerable<GXDevice> devices)
        {
            DateTime now = DateTime.Now;
            List<GXScheduleToDevice> list = new List<GXScheduleToDevice>();
            foreach (GXDevice it in devices)
            {
                list.Add(new GXScheduleToDevice()
                {
                    ScheduleId = scheduleId,
                    DeviceId = it.Id,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove devices from schedule.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="devices">Devices  that are removed from the schedule.</param>
        public void RemoveDevicesFromSchedule(IDbTransaction transaction, Guid scheduleId, IEnumerable<GXDevice> devices)
        {
            var args = GXDeleteArgs.DeleteAll<GXScheduleToDevice>();
            foreach (var it in devices)
            {
                args.Where.Or<GXScheduleToDevice>(w => w.DeviceId == it.Id && w.ScheduleId == scheduleId);
            }
            _host.Connection.Delete(transaction, args);
        }

        /// <summary>
        /// Add script methods to schedule.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="methods">Script methods that are added for the schedule.</param>
        public void AddScriptMethodsToSchedule(IDbTransaction transaction, Guid scheduleId, IEnumerable<GXScriptMethod> methods)
        {
            DateTime now = DateTime.Now;
            List<GXScheduleScript> list = new List<GXScheduleScript>();
            foreach (GXScriptMethod it in methods)
            {
                list.Add(new GXScheduleScript()
                {
                    ScheduleId = scheduleId,
                    ScriptMethodId = it.Id,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove script methods from schedule.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="methods">Script methods that are removed from the schedule.</param>
        public void RemoveScriptMethodsFromSchedule(IDbTransaction transaction, Guid scheduleId, IEnumerable<GXScriptMethod> methods)
        {
            var args = GXDeleteArgs.DeleteAll<GXScheduleScript>();
            foreach (var it in methods)
            {
                args.Where.Or<GXScheduleScript>(w => w.ScriptMethodId == it.Id && w.ScheduleId == scheduleId);
            }
            _host.Connection.Delete(transaction, args);
        }

        /// <summary>
        /// Add devices groups to schedule.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="groups">Devices groups that are added for the schedule.</param>
        public void AddDeviceGroupsToSchedule(IDbTransaction transaction, Guid scheduleId, IEnumerable<GXDeviceGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXScheduleToDeviceGroup> list = new List<GXScheduleToDeviceGroup>();
            foreach (GXDeviceGroup it in groups)
            {
                list.Add(new GXScheduleToDeviceGroup()
                {
                    ScheduleId = scheduleId,
                    DeviceGroupId = it.Id,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove device groups from schedule.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="groups">Devices groups that are removed from the schedule.</param>
        public void RemoveDeviceGroupsFromSchedule(IDbTransaction transaction, Guid scheduleId, IEnumerable<GXDeviceGroup> groups)
        {
            var args = GXDeleteArgs.DeleteAll<GXScheduleToDeviceGroup>();
            foreach (GXDeviceGroup it in groups)
            {
                args.Where.Or<GXScheduleToDeviceGroup>(w => w.DeviceGroupId == it.Id &&
                    w.ScheduleId == scheduleId);
            }
            _host.Connection.Delete(transaction, args);
        }

        /// <summary>
        /// Map schedule group to user groups.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="groups">Schedule groups where the schedule is added.</param>
        public void AddScheduleToScheduleGroups(IDbTransaction transaction, Guid scheduleId, IEnumerable<GXScheduleGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXScheduleGroupSchedule> list = new List<GXScheduleGroupSchedule>();
            foreach (GXScheduleGroup it in groups)
            {
                list.Add(new GXScheduleGroupSchedule()
                {
                    ScheduleId = scheduleId,
                    ScheduleGroupId = it.Id,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(transaction, GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between schedule group and schedule.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="scheduleId">Schedule ID.</param>
        /// <param name="groups">Schedule groups where the schedule is removed.</param>
        public void RemoveSchedulesFromScheduleGroup(IDbTransaction transaction, Guid scheduleId, IEnumerable<GXScheduleGroup> groups)
        {
            var args = GXDeleteArgs.DeleteAll<GXScheduleGroupSchedule>();
            foreach (var it in groups)
            {
                args.Where.Or<GXScheduleGroupSchedule>(w => w.ScheduleGroupId == it.Id &&
                    w.ScheduleId == scheduleId);
            }
            _host.Connection.Delete(transaction, args);
        }

        /// <inheritdoc/>
        public async Task RunAsync(ClaimsPrincipal User, Guid id)
        {
            GXSchedule schedule = await ReadAsync(User, id);
            if (schedule == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            //Schedule creator is the user who runs the schedule.
            schedule.Creator = new GXUser() { Id = ServerHelpers.GetUserId(User) };
            IGXScheduleService scheduleHandler = _serviceProvider.GetRequiredService<IGXScheduleService>();
            await scheduleHandler.RunAsync(User, schedule);
        }
    }
}
