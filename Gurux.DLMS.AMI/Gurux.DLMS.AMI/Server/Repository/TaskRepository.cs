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
// This file is a part of Gurux Schedule Framework.
//
// Gurux Schedule Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Schedule Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using System.Security.Claims;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.AMI.Server.Models;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Identity;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Server.Internal;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class TaskRepository : ITaskRepository
    {
        private readonly IGXHost _host;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IAttributeRepository _attributeRepository;
        private readonly IObjectRepository _objectRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="scheduleRepository">Schedule repository.</param>
        /// <param name="userRepository">User repository.</param>
        /// <param name="userManager">User manager.</param>
        /// <param name="serviceProvider">Service provider.</param>
        /// <param name="eventsNotifier">Events notifier.</param>
        /// <param name="objectRepository">Object repository.</param>
        /// <param name="attributeRepository">Attribute repository.</param>
        public TaskRepository(
            IGXHost host,
            IScheduleRepository scheduleRepository,
            IUserRepository userRepository,
            UserManager<ApplicationUser> userManager,
            IServiceProvider serviceProvider,
            IGXEventsNotifier eventsNotifier,
            IObjectRepository objectRepository,
            IAttributeRepository attributeRepository)
        {
            _host = host;
            _userManager = userManager;
            _userRepository = userRepository;
            _eventsNotifier = eventsNotifier;
            _serviceProvider = serviceProvider;
            _objectRepository = objectRepository;
            _attributeRepository = attributeRepository;
        }

        /// <summary>
        /// Get task creator and admin users to notify.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="task"></param>
        /// <returns></returns>
        private async Task<List<string>> GetNofifiedUsersAsync(ClaimsPrincipal user, GXTask task)
        {
            List<string> users = new List<string>();
            //Get columns.
            //This will help to check what data is needed.
            GXSelectArgs arg = GXSelectArgs.SelectById<GXTask>(task.Id);
            GXTaskColumns columns = (await _host.Connection.SingleOrDefaultAsync<GXTaskColumns>(arg));
            if (columns == null)
            {
                throw new ArgumentNullException(Properties.Resources.UnknownTarget);
            }
            if (columns.TriggerUser != null)
            {
                users.Add(columns.TriggerUser);
                if (user == null || user.IsInRole(GXRoles.Admin))
                {
                    users.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }));
                }
            }
            else if (columns.TriggerSchedule != null)
            {

            }
            else if (columns.TriggerScript != null)
            {

            }
            if (columns.Object != null)
            {
                //Notify object owners (users).
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    IObjectRepository repository = scope.ServiceProvider.GetRequiredService<IObjectRepository>();
                    users.AddRange(await repository.GetUsersAsync(user, columns.Object));
                }
            }
            if (columns.TargetDevice != null)
            {
                //Notify device owners (users).
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    IDeviceRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
                    users.AddRange(await repository.GetUsersAsync(user, columns.TargetDevice));
                }
            }
            return users;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> tasks)
        {
            if (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.TaskManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXTask>(a => a.Id, q => tasks.Contains(q.Id));
            List<GXTask> list = await _host.Connection.SelectAsync<GXTask>(arg);
            Dictionary<GXTask, List<string>> updates = new Dictionary<GXTask, List<string>>();
            foreach (GXTask it in list)
            {
                updates.Add(it, await GetNofifiedUsersAsync(User, it));
                _host.Connection.Delete(GXDeleteArgs.Delete(it));
            }
            foreach (var it in updates)
            {
                GXTask tmp = new GXTask() { Id = it.Key.Id };
                await _eventsNotifier.TaskDelete(it.Value, new GXTask[] { tmp });
            }
        }

        /// <inheritdoc />
        public async Task<GXTask[]> ListAsync(
            ClaimsPrincipal User,
            ListTasks? request,
            ListTasksResponse? response,
            CancellationToken cancellationToken)
        {
            string userId = _userManager.GetUserId(User);
            //Get all tasks that user has triggered.
            GXUser user = new GXUser() { Id = userId };
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXTask>(w => w.TriggerUser == user);
            arg.Distinct = true;
            arg.Descending = true;
            arg.OrderBy.Add<GXTask>(o => o.CreationTime);
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXTask>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            DateTime now = DateTime.Now;
            GXTask[] tasks = (await _host.Connection.SelectAsync<GXTask>(arg)).ToArray();
            if (response != null)
            {
                response.Tasks = tasks;
            }
            return tasks;
        }

        /// <inheritdoc />
        public async Task<GXTask> ReadAsync(
            ClaimsPrincipal User,
            Guid id)
        {
            string userId = _userManager.GetUserId(User);
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXTask>(where => where.Id == id);
            arg.Distinct = true;
            GXTask task = (await _host.Connection.SingleOrDefaultAsync<GXTask>(arg));
            if (task == null)
            {
                throw new ArgumentNullException(Properties.Resources.UnknownTarget);
            }
            //Get columns.
            //This will help to check what data is needed.
            GXTaskColumns columns = (await _host.Connection.SingleOrDefaultAsync<GXTaskColumns>(arg));
            if (columns == null)
            {
                throw new ArgumentNullException(Properties.Resources.UnknownTarget);
            }
            if (columns.ScriptMethod != null)
            {
                //Get modules with own query. It's faster with some DBs.
                arg = GXSelectArgs.Select<GXScriptMethod>(s => new { s.Id }, where => where.Id == columns.ScriptMethod);
                task.ScriptMethod = await _host.Connection.SingleOrDefaultAsync<GXScriptMethod>(arg);
            }
            if (columns.DeviceGroup != null)
            {
                //Get device group with own query. It's faster with some DBs.
                arg = GXSelectArgs.Select<GXDeviceGroup>(s => new { s.Id }, where => where.Id == columns.DeviceGroup);
                task.DeviceGroup = await _host.Connection.SingleOrDefaultAsync<GXDeviceGroup>(arg);
            }
            if (columns.Device != null)
            {
                //Get device with own query. It's faster with some DBs.
                arg = GXSelectArgs.Select<GXDevice>(s => new { s.Id }, where => where.Id == columns.Device);
                arg.Columns.Add<GXObject>();
                arg.Columns.Add<GXObjectTemplate>();
                arg.Columns.Add<GXAttribute>();
                arg.Columns.Add<GXAttributeTemplate>();
                arg.Joins.AddInnerJoin<GXDevice, GXObject>(a => a.Id, b => b.Device);
                arg.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(a => a.Template, b => b.Id);
                arg.Joins.AddInnerJoin<GXObject, GXAttribute>(a => a.Id, b => b.Object);
                arg.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(a => a.Template, b => b.Id);
                arg.Columns.Exclude<GXObject>(e => e.Device);
                arg.Columns.Exclude<GXAttribute>(e => e.Object);
                arg.Columns.Exclude<GXAttributeTemplate>(e => e.ObjectTemplate);
                task.Device = await _host.Connection.SingleOrDefaultAsync<GXDevice>(arg);
            }
            if (columns.Object != null)
            {
                //Get object with own query. It's faster with some DBs.
                arg = GXSelectArgs.SelectAll<GXObject>(where => where.Id == columns.Object);
                arg.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(a => a.Template, b => b.Id);
                arg.Joins.AddInnerJoin<GXObject, GXAttribute>(a => a.Id, b => b.Object);
                arg.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(a => a.Template, b => b.Id);
                arg.Columns.Add<GXAttribute>();
                arg.Columns.Add<GXObjectTemplate>();
                arg.Columns.Add<GXAttributeTemplate>();
                arg.Columns.Exclude<GXAttribute>(e => e.Object);
                arg.Columns.Exclude<GXAttributeTemplate>(e => e.ObjectTemplate);
                task.Object = await _host.Connection.SingleOrDefaultAsync<GXObject>(arg);
            }
            if (columns.Attribute != null)
            {
                //Get attribute with own query. It's faster with some DBs.
                arg = GXSelectArgs.Select<GXAttribute>(s => new { s.Id, s.Template, s.Object }, where => where.Id == columns.Attribute);
                arg.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(a => a.Template, b => b.Id);
                arg.Joins.AddInnerJoin<GXAttribute, GXObject>(a => a.Object, b => b.Id);
                arg.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(a => a.Template, b => b.Id);
                arg.Columns.Add<GXAttributeTemplate>();
                arg.Columns.Add<GXObject>();
                arg.Columns.Add<GXObjectTemplate>();
                arg.Columns.Exclude<GXObject>(e => e.Attributes);
                arg.Columns.Exclude<GXAttributeTemplate>(e => e.ObjectTemplate);
                task.Attribute = await _host.Connection.SingleOrDefaultAsync<GXAttribute>(arg);
            }
            if (columns.TriggerSchedule != null)
            {
                //Get attribute with own query. It's faster with some DBs.
                arg = GXSelectArgs.Select<GXSchedule>(s => new { s.Id }, where => where.Id == columns.TriggerSchedule);
                task.TriggerSchedule = await _host.Connection.SingleOrDefaultAsync<GXSchedule>(arg);
            }
            if (columns.TriggerUser != null)
            {
                //Get attribute with own query. It's faster with some DBs.
                arg = GXSelectArgs.Select<GXUser>(s => new { s.Id }, where => where.Id == columns.TriggerUser);
                task.TriggerUser = await _host.Connection.SingleOrDefaultAsync<GXUser>(arg);
            }
            if (columns.TriggerScript != null)
            {
                //Get attribute with own query. It's faster with some DBs.
                arg = GXSelectArgs.Select<GXScript>(s => new { s.Id }, where => where.Id == columns.TriggerScript);
                task.TriggerScript = await _host.Connection.SingleOrDefaultAsync<GXScript>(arg);
            }
            if (columns.OperatingAgent != null)
            {
                //Get agent with own query. It's faster with some DBs.
                arg = GXSelectArgs.Select<GXAgent>(s => new { s.Id }, where => where.Id == columns.OperatingAgent);
                task.OperatingAgent = await _host.Connection.SingleOrDefaultAsync<GXAgent>(arg);
            }
            return task;
        }

        /// <inheritdoc />
        public async Task<Guid[]> AddAsync(ClaimsPrincipal user, IEnumerable<GXTask> tasks)
        {
            GXUser creator = new GXUser() { Id = ServerHelpers.GetUserId(user) };
            DateTime now = DateTime.Now;
            GXSelectArgs arg;
            foreach (GXTask it in tasks)
            {
                if (it.Device == null && it.Object == null && it.Attribute == null)
                {
                    throw new ArgumentNullException(Properties.Resources.UnknownTaskTarget);
                }
                if (it.Device != null)
                {
                    it.TargetDevice = it.Device.Id;
                    //Get device name from the database. Client can send all kind of names.
                    arg = GXSelectArgs.Select<GXDevice>(s => s.Name, w => w.Id == it.Device.Id);
                    it.Target = (await _host.Connection.SingleOrDefaultAsync<GXDevice>(arg)).Name;
                }
                else if (it.Object != null && it.Object.Id != Guid.Empty)
                {
                    //Get device ID from the database.
                    arg = GXSelectArgs.Select<GXDevice>(s => new { s.Id, s.Name });
                    arg.Joins.AddInnerJoin<GXDevice, GXObject>(s => s.Id, o => o.Device);
                    arg.Where.And<GXObject>(q => q.Id == it.Object.Id);
                    GXDevice dev = (await _host.Connection.SingleOrDefaultAsync<GXDevice>(arg));
                    it.TargetDevice = dev.Id;
                    //Get object name from the database. Client can send all kind of names.
                    arg = GXSelectArgs.Select<GXObjectTemplate>(s => s.Name);
                    arg.Joins.AddInnerJoin<GXObjectTemplate, GXObject>(j => j.Id, j => j.Template);
                    arg.Where.And<GXObject>(w => w.Id == it.Object.Id);
                    it.Target = dev.Name + " " + (await _host.Connection.SingleOrDefaultAsync<string>(arg));
                }
                else if (it.Attribute != null)
                {
                    if (it.Attribute.Template == null)
                    {
                        throw new ArgumentException("Invalid template");
                    }
                    //Get device ID from the database.
                    arg = GXSelectArgs.Select<GXDevice>(s => new { s.Id, s.Name });
                    arg.Joins.AddInnerJoin<GXDevice, GXObject>(s => s.Id, o => o.Device);
                    arg.Joins.AddInnerJoin<GXObject, GXAttribute>(s => s.Id, o => o.Object);
                    arg.Where.And<GXAttribute>(q => q.Id == it.Attribute.Id);
                    GXDevice dev = (await _host.Connection.SingleOrDefaultAsync<GXDevice>(arg));
                    it.TargetDevice = dev.Id;

                    //Get attribute and object names from the database. Client can send all kind of names.
                    arg = GXSelectArgs.Select<GXAttributeTemplate>(s => new { s.Id, s.Index, s.Name, s.ObjectTemplate });
                    arg.Columns.Add<GXObjectTemplate>(s => new { s.Id, s.Name });
                    arg.Joins.AddInnerJoin<GXAttributeTemplate, GXObjectTemplate>(j => j.ObjectTemplate, j => j.Id);
                    arg.Joins.AddInnerJoin<GXAttributeTemplate, GXAttribute>(j => j.Id, j => j.Template);
                    arg.Where.And<GXAttribute>(w => w.Id == it.Attribute.Id);
                    GXAttributeTemplate att = await _host.Connection.SingleOrDefaultAsync<GXAttributeTemplate>(arg);
                    it.Target = dev.Name + " " + att.ObjectTemplate.Name + " " + att.Name + ":" + att.Index;
                }
                if (it.TargetDevice == null)
                {
                    throw new ArgumentNullException(Properties.Resources.UnknownTaskTarget);
                }
                //If script is triggered by user.
                if (user != null)
                {
                    it.TriggerUser = new GXUser() { Id = ServerHelpers.GetUserId(user) };
                }
                else if (it.TriggerSchedule == null && it.TriggerScript == null)
                {
                    throw new ArgumentNullException(Properties.Resources.UnknownTarget);
                }
                if (it.TriggerUser == null)
                {
                    if (it.TriggerSchedule != null)
                    {
                        it.TriggerUser = it.TriggerSchedule.Creator;
                    }
                    else if (it.TriggerScript != null)
                    {
                        it.TriggerUser = it.TriggerScript.Creator;
                    }
                    else if (it.TriggerModule != null)
                    {
                        it.TriggerUser = it.TriggerModule.Creator;
                    }
                    if (it.TriggerUser == null)
                    {
                        throw new ArgumentNullException(Properties.Resources.UnknownTarget);
                    }
                }
                it.Creator = creator;
                it.CreationTime = now;
            }

            await _host.Connection.InsertAsync(GXInsertArgs.InsertRange(tasks));
            IEnumerable<Guid> list = tasks.Select(s => s.Id);
            await NotifyUsers(user, tasks, true);
            return list.ToArray();
        }

        /// <inheritdoc />
        public async Task DoneAsync(ClaimsPrincipal User, IEnumerable<GXTask> tasks)
        {
            foreach (GXTask it in tasks)
            {
                it.Ready = DateTime.Now;
                _host.Connection.Update(GXUpdateArgs.Update(it, q => new { q.Ready, q.Result, q.Data }));
            }
            //Update attribute exceptions.
            foreach (GXTask it in tasks)
            {
                GXTask task = await ReadAsync(User, it.Id);
                if (task.Attribute != null)
                {
                    task.Attribute.Exception = it.Result;
                    task.Attribute.Read = it.Ready;
                    await _attributeRepository.UpdateAsync(User,
                        new GXAttribute[] { task.Attribute },
                        u => new { u.Exception, u.Read });
                }
                if (task.Object != null && task.Index != 0)
                {
                    var att = task.Object.Attributes.Where(w => w.Template.Index == task.Index).SingleOrDefault();
                    if (att != null)
                    {
                        att.Read = it.Ready;
                        att.Exception = it.Result;
                        await _attributeRepository.UpdateAsync(User,
                            new GXAttribute[] { att },
                            u => new { u.Exception, u.Read });
                    }
                }
            }
            await NotifyUsers(User, tasks);
        }

        /// <inheritdoc />
        public async Task RestartAsync(ClaimsPrincipal User, IEnumerable<GXTask> tasks)
        {
            foreach (GXTask it in tasks)
            {
                it.OperatingAgent = null;
                it.Start = it.Ready = null;
                it.Result = null;
                it.Data = null;
                await _host.Connection.UpdateAsync(GXUpdateArgs.Update(it, q => new { q.Start, q.Ready, q.Result, q.Data, q.OperatingAgent }));
            }
            await NotifyUsers(User, tasks);
        }

        /// <summary>
        /// Notify users who can access the device that status of the task has changed.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="tasks">Notified tasks.</param>
        /// <param name="add">Is task added.</param>
        private async Task NotifyUsers(ClaimsPrincipal User, IEnumerable<GXTask> tasks, bool add = false)
        {
            List<string> users = new List<string>();
            List<GXTask> list = new List<GXTask>();
            foreach (var it in tasks)
            {
                users.AddDistinct(await GetNofifiedUsersAsync(User, it));
                if (!add)
                {
                    //Only basic task information is notified for the security reasons.
                    GXSelectArgs arg = GXSelectArgs.SelectAll<GXTask>(where => where.Id == it.Id);
                    GXTask task = (await _host.Connection.SingleOrDefaultAsync<GXTask>(arg));
                    if (task == null)
                    {
                        throw new ArgumentNullException(Properties.Resources.UnknownTarget);
                    }
                    list.Add(task);
                }
                else
                {
                    list.Add(it);
                    it.Creator = null;
                }
            }
            if (users.Any())
            {
                if (add)
                {
                    await _eventsNotifier.TaskAdd(users, list);
                }
                else
                {
                    await _eventsNotifier.TaskUpdate(users, list);
                }
            }
        }

        /// <inheritdoc />
        public async Task<GXTask[]> GetNextAsync(
            ClaimsPrincipal User,
            Guid agentId,
            Guid DeviceId,
            bool listener)
        {
            //Check that agent is active and not removed.
            GXSelectArgs arg = GXSelectArgs.Select<GXAgent>(c => c.Id, q => q.Id == agentId && q.Active == true && q.Removed == null);
            GXAgent agent = _host.Connection.SingleOrDefault<GXAgent>(arg);
            if (agent == null)
            {
                throw new UnauthorizedAccessException();
            }
            arg = GXSelectArgs.Select<GXTask>(c => new
            {
                c.Id,
                c.CreationTime,
                c.TargetDevice,
                c.Batch
            }, where => where.Start == null && where.Ready == null);
            arg.OrderBy.Add<GXTask>(o => o.CreationTime);
            arg.Joins.AddLeftJoin<GXTask, GXDevice>(a => a.TargetDevice, b => b.Id);
            if (DeviceId != Guid.Empty)
            {
                arg.Where.And<GXDevice>(q => q.Id == DeviceId);
            }
            if (!listener)
            {
                arg.Where.And<GXDevice>(q => q.Dynamic == false);
            }
            // Don't get meters that are read by other agents.
            GXSelectArgs onProgress = GXSelectArgs.Select<GXTask>(c => c.Id, q => q.Ready == null && q.OperatingAgent != null);
            arg.Where.And<GXTask>(q => !GXSql.Exists(onProgress));
            GXTask[] tasks = new GXTask[0];
            GXTask task = _host.Connection.SingleOrDefault<GXTask>(arg);
            //Get details for the task.
            if (task != null)
            {
                if (task.Batch != null)
                {
                    //Get all tasks that are created with the same batch.
                    arg = GXSelectArgs.SelectAll<GXTask>(q => q.Batch == task.Batch);
                    tasks = _host.Connection.Select<GXTask>(arg).ToArray();
                    tasks = tasks.OrderBy(o => o.Order).ToArray();
                }
                else
                {
                    tasks = new GXTask[] { task };
                }
                DateTime now = DateTime.Now;
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    IDeviceRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
                    var users = (await repository.GetUsersAsync(User, task.TargetDevice.Value));
                    for (int pos = 0; pos != tasks.Length; ++pos)
                    {
                        tasks[pos].Start = now;
                        tasks[pos].OperatingAgent = agent;
                        GXUpdateArgs args = GXUpdateArgs.Update(tasks[pos], q => new { q.Start, q.OperatingAgent });
                        _host.Connection.Update(args);
                        //Read task target information.
                        tasks[pos] = await ReadAsync(User, tasks[pos].Id);
                    }
                }
                await NotifyUsers(User, tasks);
            }
            return tasks;
        }

        /// <inheritdoc />
        public async Task ClearAsync(ClaimsPrincipal user)
        {
            if (!user.IsInRole(GXRoles.Admin) && !user.IsInRole(GXRoles.TaskManager))
            {
                throw new UnauthorizedAccessException();
            }
            bool admin = user.IsInRole(GXRoles.Admin);
            GXSelectArgs arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName }, w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddLeftJoin<GXUser, GXTask>(y => y.Id, x => x.TriggerUser);
            if (!admin)
            {
                string id = ServerHelpers.GetUserId(user);
                arg.Where.And<GXUser>(w => w.Id == id);
            }
            List<GXUser> clearedUsers = (await _host.Connection.SelectAsync<GXUser>(arg));
            var list = await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin });
            //Notify users if their tasks are cleared.
            list.AddDistinct(clearedUsers.Select(s => s.Id));
            if (admin)
            {
                //Admin clears all tasks.
                _host.Connection.Truncate<GXTask>();
                clearedUsers.Clear();
            }
            else if (clearedUsers.Any())
            {
                GXUser tmp = clearedUsers[0];
                GXDeleteArgs args = GXDeleteArgs.Delete<GXTask>(w => w.TriggerUser == tmp);
                await _host.Connection.DeleteAsync(args);
            }
            await _eventsNotifier.TaskClear(list, clearedUsers);
        }
    }
}