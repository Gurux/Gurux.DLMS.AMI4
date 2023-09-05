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
using System.Linq.Expressions;
using Gurux.DLMS.AMI.Client.Pages.Agent;
using Gurux.DLMS.AMI.Client.Pages.Device;

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
        private readonly IDeviceRepository _deviceRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="userRepository">User repository.</param>
        /// <param name="userManager">User manager.</param>
        /// <param name="serviceProvider">Service provider.</param>
        /// <param name="eventsNotifier">Events notifier.</param>
        /// <param name="attributeRepository">Attribute repository.</param>
        /// <param name="objectRepository">Object repository.</param>
        /// <param name="deviceRepository">Device repository.</param>
        public TaskRepository(
            IGXHost host,
            IUserRepository userRepository,
            UserManager<ApplicationUser> userManager,
            IServiceProvider serviceProvider,
            IGXEventsNotifier eventsNotifier,
            IAttributeRepository attributeRepository,
            IObjectRepository objectRepository,
            IDeviceRepository deviceRepository)
        {
            _host = host;
            _userManager = userManager;
            _userRepository = userRepository;
            _eventsNotifier = eventsNotifier;
            _serviceProvider = serviceProvider;
            _attributeRepository = attributeRepository;
            _objectRepository = objectRepository;
            _deviceRepository = deviceRepository;
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
                throw new ArgumentException(Properties.Resources.UnknownTarget);
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
            if (User == null || (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.TaskManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXTask>(a => a.Id, q => tasks.Contains(q.Id));
            List<GXTask> list = await _host.Connection.SelectAsync<GXTask>(arg);
            Dictionary<GXTask, List<string>> updates = new Dictionary<GXTask, List<string>>();
            foreach (GXTask it in list)
            {
                updates.Add(it, await GetNofifiedUsersAsync(User, it));
                _host.Connection.Delete(GXDeleteArgs.DeleteById<GXTask>(it.Id));
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
            CancellationToken cancellationToken = default)
        {
            string userId = _userManager.GetUserId(User);
            //Get all tasks that user has triggered.
            GXUser user = new GXUser() { Id = userId };
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXTask>(w => w.TriggerUser == user);
            arg.Distinct = true;
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXTask>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXTask>(o => o.CreationTime);
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXTask>(w => !request.Exclude.Contains(w.Id));
                }
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
            List<GXTask> list = new List<GXTask>();
            GXTask[] tasks = new GXTask[0];
            if (request != null)
            {
                if ((request.Select & TargetType.Device) != 0)
                {
                    //Select devices.
                    arg = GXSelectArgs.SelectAll<GXTask>();
                    arg.Distinct = true;
                    arg.Where.FilterBy(request.Filter);
                    arg.Columns.Add<GXDevice>();
                    arg.Columns.Add<GXDeviceTemplate>();
                    arg.Columns.Add<GXObject>();
                    arg.Columns.Add<GXObjectTemplate>();
                    arg.Columns.Add<GXAttribute>(s => new { s.Id, s.Template, s.Object });
                    arg.Columns.Add<GXAttributeTemplate>();
                    arg.Joins.AddInnerJoin<GXTask, GXDevice>(a => a.Device, b => b.Id);
                    arg.Joins.AddInnerJoin<GXDevice, GXObject>(a => a.Id, b => b.Device);
                    arg.Joins.AddInnerJoin<GXObject, GXAttribute>(b => b.Id, a => a.Object);
                    arg.Joins.AddInnerJoin<GXDevice, GXDeviceTemplate>(a => a.Template, b => b.Id);
                    arg.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(a => a.Template, b => b.Id);
                    arg.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(a => a.Template, b => b.Id);
                    arg.Columns.Exclude<GXAttributeTemplate>(e => e.ObjectTemplate);
                    list.AddRange(await _host.Connection.SelectAsync<GXTask>(arg));
                }
                if ((request.Select & TargetType.Attribute) != 0)
                {
                    //Select attributes.
                    arg = GXSelectArgs.SelectAll<GXTask>();
                    arg.Distinct = true;
                    arg.Where.FilterBy(request.Filter);
                    arg.Columns.Add<GXAttribute>(s => new { s.Id, s.Template, s.Object });
                    arg.Columns.Add<GXObject>();
                    arg.Columns.Add<GXObjectTemplate>();
                    arg.Columns.Add<GXAttributeTemplate>();
                    arg.Joins.AddInnerJoin<GXTask, GXAttribute>(a => a.Attribute, b => b.Id);
                    arg.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(a => a.Template, b => b.Id);
                    arg.Joins.AddInnerJoin<GXAttribute, GXObject>(a => a.Object, b => b.Id);
                    arg.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(a => a.Template, b => b.Id);
                    arg.Columns.Exclude<GXTask>(e => e.Target);
                    arg.Columns.Exclude<GXObjectTemplate>(e => new { e.Updated, e.CreationTime, e.ExpirationTime, e.Name });
                    arg.Columns.Exclude<GXObject>(e => new { e.Tasks, e.Attributes, e.CreationTime, e.LastError, e.LastErrorMessage, e.LastRead, e.LastAction, e.Updated });
                    arg.Columns.Exclude<GXAttribute>(e => new { e.Tasks, e.CreationTime, e.Read, e.LastWrite, e.LastAction, e.LastError, e.Updated, e.Value });
                    arg.Columns.Exclude<GXAttributeTemplate>(e => new { e.ObjectTemplate, e.Updated, e.CreationTime, e.ExpirationTime, e.Name });
                    list.AddRange(await _host.Connection.SelectAsync<GXTask>(arg));
                }
                if ((request.Select & TargetType.Object) != 0)
                {
                    //Select objects.
                    arg = GXSelectArgs.SelectAll<GXTask>();
                    arg.Distinct = true;
                    arg.Where.FilterBy(request.Filter);
                    arg.Columns.Add<GXObject>();
                    arg.Joins.AddInnerJoin<GXTask, GXObject>(a => a.Object, b => b.Id);
                    arg.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(a => a.Template, b => b.Id);
                    arg.Joins.AddInnerJoin<GXObject, GXAttribute>(a => a.Id, b => b.Object);
                    arg.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(a => a.Template, b => b.Id);
                    arg.Columns.Add<GXAttribute>();
                    arg.Columns.Add<GXObjectTemplate>();
                    arg.Columns.Add<GXAttributeTemplate>();
                    arg.Columns.Exclude<GXTask>(e => e.Target);
                    arg.Columns.Exclude<GXObjectTemplate>(e => new { e.Updated, e.CreationTime, e.ExpirationTime, e.Name });
                    arg.Columns.Exclude<GXObject>(e => new { e.Tasks, e.CreationTime, e.LastError, e.LastErrorMessage, e.LastRead, e.LastAction, e.Updated });
                    arg.Columns.Exclude<GXAttribute>(e => new { e.Object, e.CreationTime, e.Read, e.LastWrite, e.LastAction, e.LastError, e.Updated, e.Value });
                    arg.Columns.Exclude<GXAttributeTemplate>(e => new { e.ObjectTemplate, e.Updated, e.CreationTime, e.ExpirationTime, e.Name, e.Weight });
                    list.AddRange(await _host.Connection.SelectAsync<GXTask>(arg));
                }
                tasks = list.OrderBy(o => o.Order).ToArray();
            }
            if (request == null || (request.Select & (TargetType.Attribute | TargetType.Object)) == 0)
            {
                tasks = (await _host.Connection.SelectAsync<GXTask>(arg)).ToArray();
            }
            if (response != null)
            {
                response.Tasks = tasks;
                if (response.Count == 0)
                {
                    response.Count = tasks.Length;
                }
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
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            //Get columns.
            //This will help to check what data is needed.
            GXTaskColumns columns = (await _host.Connection.SingleOrDefaultAsync<GXTaskColumns>(arg));
            if (columns == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
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
                arg.Columns.Exclude<GXObjectTemplate>(e => new { e.Updated, e.CreationTime, e.ExpirationTime, e.Name });
                arg.Columns.Exclude<GXObject>(e => new { e.CreationTime, e.LastError, e.LastErrorMessage, e.LastRead, e.LastAction, e.Updated });
                arg.Columns.Exclude<GXAttribute>(e => new { e.Object, e.CreationTime, e.Read, e.LastWrite, e.LastAction, e.LastError, e.Updated, e.Value });
                arg.Columns.Exclude<GXAttributeTemplate>(e => new { e.ObjectTemplate, e.Updated, e.CreationTime, e.ExpirationTime, e.Name });
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
                arg.Columns.Exclude<GXObjectTemplate>(e => new { e.Updated, e.CreationTime, e.ExpirationTime, e.Name });
                arg.Columns.Exclude<GXObject>(e => new { e.Attributes, e.CreationTime, e.LastError, e.LastErrorMessage, e.LastRead, e.LastAction, e.Updated });
                arg.Columns.Exclude<GXAttribute>(e => new { e.CreationTime, e.Read, e.LastWrite, e.LastAction, e.LastError, e.Updated, e.Value });
                arg.Columns.Exclude<GXAttributeTemplate>(e => new { e.ObjectTemplate, e.Updated, e.CreationTime, e.ExpirationTime, e.Name });
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
                if (it.Index == null)
                {
                    it.Index = 0;
                }
                if (it.Order == null)
                {
                    it.Order = 0;
                }
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
                    arg = GXSelectArgs.Select<GXAttributeTemplate>(s => new { s.Id, s.Name }, w => w.Removed == null);
                    arg.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(j => j.Template, j => j.Id);
                    arg.Where.And<GXAttribute>(w => w.Removed == null && w.Id == it.Attribute.Id);
                    it.Attribute.Template = await _host.Connection.SingleOrDefaultAsync<GXAttributeTemplate>(arg);
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

                    //Update the new attribute value.
                    if (it.TaskType == TaskType.Write)
                    {
                        if (string.IsNullOrEmpty(it.Attribute.Value) && !string.IsNullOrEmpty(it.Data))
                        {
                            it.Attribute.Value = it.Data;
                            it.Attribute.LastWrite = now;
                        }
                        await _host.Connection.UpdateAsync(GXUpdateArgs.Update(it.Attribute, u => u.Value));
                    }
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
                    throw new ArgumentException(Properties.Resources.UnknownTarget);
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
                        throw new ArgumentException(Properties.Resources.UnknownTarget);
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

        /// <summary>
        /// Update task execution times to attribute, object and device.
        /// </summary>
        /// <param name="User"></param>
        /// <param name="executedTask">Executed task</param>
        /// <param name="dbTask">Task from the database.</param>
        /// <param name="obj">Updated Object.</param>
        private async Task UpdateObjectExecutionTimes(ClaimsPrincipal User, GXTask executedTask, GXTask dbTask, GXObject obj)
        {
            //Device name is mandatory.
            GXSelectArgs args = GXSelectArgs.Select<GXDevice>(s => new { s.Id, s.Name });
            args.Joins.AddInnerJoin<GXDevice, GXObject>(j => j.Id, j => j.Device);
            args.Where.And<GXObject>(w => w.Id == obj.Id);
            obj.Device = await _host.Connection.SingleOrDefaultAsync<GXDevice>(args);
            if (obj.Device == null)
            {
                throw new Exception("Device is unknown.");
            }
            //Save occurred exception or clear the old one.
            if (!string.IsNullOrEmpty(executedTask.Result))
            {
                obj.LastError = executedTask.Ready;
                obj.LastErrorMessage = executedTask.Result;
            }
            else
            {
                obj.LastError = null;
                obj.LastErrorMessage = null;
            }
            {
                Expression<Func<GXObject, object?>> columns;
                switch (dbTask.TaskType)
                {
                    case TaskType.Read:
                        obj.LastRead = executedTask.Ready;
                        columns = q => new { q.LastRead, q.LastError, q.LastErrorMessage };
                        break;
                    case TaskType.Write:
                        obj.LastWrite = executedTask.Ready;
                        columns = q => new { q.LastWrite, q.LastError, q.LastErrorMessage };
                        break;
                    case TaskType.Action:
                        obj.LastAction = executedTask.Ready;
                        columns = q => new { q.LastAction, q.LastError, q.LastErrorMessage };
                        break;
                    default:
                        throw new Exception("Unknown task type.");
                }
                await _objectRepository.UpdateAsync(User, new GXObject[] { obj }, columns);
            }
            {
                Expression<Func<GXDevice, object?>> columns;
                switch (dbTask.TaskType)
                {
                    case TaskType.Read:
                        obj.Device.LastRead = executedTask.Ready;
                        columns = q => new { q.LastRead, q.LastError, q.LastErrorMessage };
                        break;
                    case TaskType.Write:
                        obj.Device.LastWrite = executedTask.Ready;
                        columns = q => new { q.LastWrite, q.LastError, q.LastErrorMessage };
                        break;
                    case TaskType.Action:
                        obj.Device.LastAction = executedTask.Ready;
                        columns = q => new { q.LastAction, q.LastError, q.LastErrorMessage };
                        break;
                    default:
                        throw new Exception("Unknown task type.");
                }
                await _deviceRepository.UpdateAsync(User, new GXDevice[] { obj.Device }, CancellationToken.None, columns);
            }
        }

        /// <inheritdoc />
        public async Task DoneAsync(ClaimsPrincipal User, IEnumerable<GXTask> tasks)
        {
            foreach (GXTask it in tasks)
            {
                try
                {
                    it.Ready = DateTime.Now;
                    //TODO: MySQL must handle this.
                    if (!string.IsNullOrEmpty(it.Result))
                    {
                        it.Result = it.Result.Replace("'", "");
                    }
                    if (!string.IsNullOrEmpty(it.Data))
                    {
                        it.Data = it.Data.Replace("'", "");
                    }
                    _host.Connection.Update(GXUpdateArgs.Update(it, q => new { q.Ready, q.Result, q.Data }));
                }
                catch (Exception ex)
                {
                    it.Result = it.Data = ex.Message;
                    _host.Connection.Update(GXUpdateArgs.Update(it, q => new { q.Ready, q.Result, q.Data }));
                }
            }
            //Update attribute exceptions.
            foreach (GXTask it in tasks)
            {
                GXTask task = await ReadAsync(User, it.Id);
                if (task.Attribute != null)
                {
                    if (!string.IsNullOrEmpty(it.Result))
                    {
                        task.Attribute.LastError = it.Ready;
                    }
                    else
                    {
                        task.Attribute.LastError = null;
                    }
                    task.Attribute.Exception = it.Result;
                    Expression<Func<GXAttribute, object?>> columns;
                    switch (task.TaskType)
                    {
                        case TaskType.Read:
                            task.Attribute.Read = it.Ready;
                            columns = q => new { q.Read, q.LastError, q.Exception };
                            break;
                        case TaskType.Write:
                            task.Attribute.LastWrite = it.Ready;
                            columns = q => new { q.LastWrite, q.LastError, q.Exception };
                            break;
                        case TaskType.Action:
                            task.Attribute.LastAction = it.Ready;
                            columns = q => new { q.LastAction, q.LastError, q.Exception };
                            break;
                        default:
                            throw new Exception("Unknown task type.");
                    }
                    await _attributeRepository.UpdateAsync(User, new GXAttribute[] { task.Attribute }, columns);

                    //Update object exception and execution time.
                    GXSelectArgs args = GXSelectArgs.Select<GXObject>(s => new { s.Id, s.Template });
                    args.Columns.Add<GXObjectTemplate>(s => new { s.Id, s.LogicalName, s.Name });
                    args.Joins.AddInnerJoin<GXObject, GXAttribute>(j => j.Id, j => j.Object);
                    args.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(j => j.Template, j => j.Id);
                    args.Where.And<GXAttribute>(w => w.Id == task.Attribute.Id);
                    args.Distinct = true;
                    GXObject obj = await _host.Connection.SingleOrDefaultAsync<GXObject>(args);
                    await UpdateObjectExecutionTimes(User, it, task, obj);
                }
                if (task.Object != null)
                {
                    if (task.Index != 0)
                    {
                        var att = task.Object.Attributes.Where(w => w.Template.Index == task.Index).SingleOrDefault();
                        if (att != null)
                        {
                            att.Object = task.Object;
                            if (!string.IsNullOrEmpty(it.Result))
                            {
                                att.LastError = it.Ready;
                            }
                            else
                            {
                                att.LastError = null;
                            }
                            att.Exception = it.Result;
                            Expression<Func<GXAttribute, object?>> columns;
                            switch (task.TaskType)
                            {
                                case TaskType.Read:
                                    att.Read = it.Ready;
                                    columns = q => new { q.Read, q.LastError, q.Exception };
                                    break;
                                case TaskType.Write:
                                    att.LastWrite = it.Ready;
                                    columns = q => new { q.LastWrite, q.LastError, q.Exception };
                                    break;
                                case TaskType.Action:
                                    att.LastAction = it.Ready;
                                    columns = q => new { q.LastAction, q.LastError, q.Exception };
                                    break;
                                default:
                                    throw new Exception("Unknown task type.");
                            }

                            await _attributeRepository.UpdateAsync(User, new GXAttribute[] { att }, columns);
                        }
                    }
                    await UpdateObjectExecutionTimes(User, it, task, task.Object);
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
                        throw new ArgumentException(Properties.Resources.UnknownTarget);
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
            Guid? DeviceId,
            bool listener)
        {
            DateTime start = DateTime.Now;
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
            arg.Joins.AddInnerJoin<GXTask, GXDevice>(a => a.TargetDevice.Value, b => b.Id);
            if (DeviceId != null && DeviceId != Guid.Empty)
            {
                Guid id = DeviceId.Value;
                arg.Where.And<GXDevice>(q => q.Removed == null && q.Id == id);
            }
            if (!listener)
            {
                arg.Where.And<GXDevice>(q => q.Removed == null && q.Dynamic == false);
            }
            // Don't get meters that are read by other agents.
            GXSelectArgs onDeviceProgress = GXSelectArgs.Select<GXDevice>(c => c.Id);
            onDeviceProgress.Where.And<GXTask>(q => q.Ready == null && q.OperatingAgent != null);
            arg.Where.And<GXDevice>(q => !GXSql.Exists<GXTask, GXDevice>(j => j.TargetDevice, j => j.Id, onDeviceProgress));
            //Don't select tasks assigned for the given agent group and the agent doesn't belong to that.
            //Check are device groups assigned for the agent group.
            //This is faster to check with own query.
            GXSelectArgs isAssigned = GXSelectArgs.Select<GXAgentGroupDeviceGroup>(c => GXSql.One, w => w.Removed == null);
            isAssigned.Joins.AddInnerJoin<GXAgentGroupDeviceGroup, GXAgentGroup>(j => j.AgentGroupId, j => j.Id);
            isAssigned.Joins.AddInnerJoin<GXAgentGroup, GXAgentGroupAgent>(j => j.Id, j => j.AgentGroupId);
            isAssigned.Joins.AddInnerJoin<GXAgentGroupAgent, GXAgent>(j => j.AgentId, j => j.Id);
            isAssigned.Where.And<GXAgent>(w => w.Id == agentId && w.Removed == null);
            int count = _host.Connection.SingleOrDefault<int>(isAssigned);
            if (count != 0)
            {
                //Device is assigned for the agent.
                //Return agent groups where the agent belongs.
                GXSelectArgs agentGroups = GXSelectArgs.Select<GXAgentGroup>(c => c.Id, q => q.Removed == null);
                agentGroups.Joins.AddInnerJoin<GXAgentGroup, GXAgentGroupAgent>(j => j.Id, j => j.AgentGroupId);
                agentGroups.Joins.AddInnerJoin<GXAgentGroupAgent, GXAgent>(j => j.AgentId, j => j.Id);
                agentGroups.Where.And<GXAgent>(w => w.Id == agentId && w.Removed == null);
                //Get devices that agent can access.
                arg.Joins.AddInnerJoin<GXDevice, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceId);
                arg.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDeviceGroup>(j => j.DeviceGroupId, j => j.Id);
                arg.Joins.AddInnerJoin<GXDeviceGroup, GXAgentGroupDeviceGroup>(j => j.Id, j => j.DeviceGroupId);
                arg.Where.And<GXAgentGroupDeviceGroup>(q => GXSql.Exists(agentGroups));
            }
            GXTask[] tasks = new GXTask[0];
            GXTask? task = _host.Connection.SingleOrDefault<GXTask>(arg);
            List<GXTask> notified = new List<GXTask>();
            DateTime now = DateTime.Now;
            //Get details for the task.
            if (task != null)
            {
                if (task.Batch != null)
                {
                    task.Start = now;
                    task.OperatingAgent = agent;
                    //Get all tasks that are created with the same batch and are not executed.
                    ListTasks lt = new ListTasks()
                    {
                        Select = TargetType.Device | TargetType.Object | TargetType.Attribute,
                        Filter = new GXTask()
                        {
                            Batch = task.Batch
                        }
                    };
                    tasks = await ListAsync(User, lt, null);
                    foreach (var it in tasks)
                    {
                        notified.Add(new GXTask() { Id = it.Id, Start = task.Start });
                    }
                    GXUpdateArgs args = GXUpdateArgs.Update(task, q => new { q.Start, q.OperatingAgent });
                    args.Where.And<GXTask>(w => w.Batch == task.Batch);
                    _host.Connection.Update(args);
                }
                else
                {
                    //Read task details.
                    task = await ReadAsync(User, task.Id);
                    task.Start = now;
                    task.OperatingAgent = agent;
                    //Reset non-needed information.
                    task.Target = null;
                    task.CreationTime = null;
                    task.TriggerUser = null;
                    if (task.Object != null)
                    {
                        task.Object.CreationTime = null;
                    }
                    tasks = new GXTask[] { task };
                    notified.Add(new GXTask() { Id = task.Id, Start = task.Start });
                    GXUpdateArgs args = GXUpdateArgs.Update(task, q => new { q.Start, q.OperatingAgent });
                    _host.Connection.Update(args);
                }
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    IDeviceRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
                    var users = (await repository.GetUsersAsync(User, task.TargetDevice.Value));
                }
                await NotifyUsers(User, notified);
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