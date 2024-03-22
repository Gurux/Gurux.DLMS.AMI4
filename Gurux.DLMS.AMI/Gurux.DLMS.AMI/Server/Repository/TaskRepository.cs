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
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using System.Data;
using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Swashbuckle.AspNetCore.SwaggerGen;

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
        private readonly ITaskLateBindHandler _taskHandler;
        private readonly IScriptRepository _scriptRepository;
        private readonly IGXModuleService _moduleService;
        private readonly IScheduleRepository _scheduleRepository;

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
        /// <param name="taskHandler">Task handler.</param>
        /// <param name="scriptRepository">Script repository.</param>
        /// <param name="moduleService">Module service.</param>
        /// <param name="scheduleRepository">Schedule repository.</param>
        public TaskRepository(
            IGXHost host,
            IUserRepository userRepository,
            UserManager<ApplicationUser> userManager,
            IServiceProvider serviceProvider,
            IGXEventsNotifier eventsNotifier,
            IAttributeRepository attributeRepository,
            IObjectRepository objectRepository,
            IDeviceRepository deviceRepository,
            ITaskLateBindHandler taskHandler,
            IScriptRepository scriptRepository,
            IGXModuleService moduleService,
            IScheduleRepository scheduleRepository)
        {
            _host = host;
            _userManager = userManager;
            _userRepository = userRepository;
            _eventsNotifier = eventsNotifier;
            _serviceProvider = serviceProvider;
            _attributeRepository = attributeRepository;
            _objectRepository = objectRepository;
            _deviceRepository = deviceRepository;
            _taskHandler = taskHandler;
            _scriptRepository = scriptRepository;
            _moduleService = moduleService;
            _scheduleRepository = scheduleRepository;
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
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the tasks.
                arg = GXSelectArgs.SelectAll<GXTask>();
            }
            else
            {
                //Get all tasks that user has created.
                string userId = ServerHelpers.GetUserId(User);
                GXUser user = new GXUser() { Id = userId };
                arg = GXSelectArgs.SelectAll<GXTask>(w => w.Creator == user);
            }
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
                if (request?.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXTask>(w => request.Included.Contains(w.Id));
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
                    try
                    {
                        response.Count = await _host.Connection.SingleOrDefaultAsync<int>(total,
                                                cancellationToken, 5);
                    }
                    catch (TimeoutException)
                    {
                        //Return -1 if amount of the tasks is too large.
                        response.Count = -1;
                    }
                    arg.Index = (UInt32)request.Index;
                    arg.Count = (UInt32)request.Count;
                }
            }
            DateTime now = DateTime.Now;
            List<GXTask> list = new List<GXTask>();
            GXTask[] tasks = new GXTask[0];
            if (request != null)
            {
                if (request.Select != null && request.Select.Contains("Device"))
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
                if (request.Select != null && request.Select.Contains("Attribute"))
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
                if (request.Select != null && request.Select.Contains("Object"))
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
            if (request == null || request.Select == null ||
                (!request.Select.Contains("Attribute") && !request.Select.Contains("Object")))
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
            GXDevice? dev;
            GXObject? obj;
            List<GXTask> tasks3 = new List<GXTask>();
            using IDbTransaction transaction = _host.Connection.BeginTransaction();
            try
            {
                //Handle device groups and create a own task for each device.
                List<GXTask> removed = new List<GXTask>();
                foreach (GXTask it in tasks)
                {
                    if (it.DeviceGroup != null)
                    {
                        //Get devices that belong to this device group.
                        var devices = DeviceGroupRepository.GetDevicessByDeviceGroupId(_host, user, it.DeviceGroup.Id);
                        //If user wants to read attributes from the selected device groups.
                        if (it.Object?.Attributes != null && it.Object.Attributes.Any())
                        {
                            foreach (var dev2 in devices)
                            {
                                List<GXTask> tasks2 = new List<GXTask>();
                                foreach (var att in it.Object.Attributes)
                                {
                                    GXTask t = ClientHelpers.Clone(it);
                                    t.TargetDevice = dev2.Id;
                                    t.DeviceGroup = null;
                                    t.Attribute = ClientHelpers.Clone(att);
                                    t.Index = att.Template.Index;
                                    //Update attribute object.
                                    t.Attribute.Object = it.Object;
                                    t.Creator = it.Creator;
                                    tasks2.Add(t);
                                }
                                _taskHandler.AddRange(user, tasks2);
                            }
                        }
                        else
                        {
                            List<GXTask> tasks2 = new List<GXTask>();
                            foreach (var dev2 in devices)
                            {
                                GXTask t = ClientHelpers.Clone(it);
                                t.DeviceGroup = null;
                                t.Device = dev2;
                                t.Creator = it.Creator;
                                tasks2.Add(t);
                            }
                            _taskHandler.AddRange(user, tasks2);
                        }
                        removed.Add(it);
                    }
                    if (it.Device != null)
                    {
                        //If user wants to read attributes from the selected device groups.
                        if (it.Object?.Attributes != null && it.Object.Attributes.Any())
                        {
                            List<GXTask> tasks2 = new List<GXTask>();
                            foreach (var att in it.Object.Attributes)
                            {
                                GXTask t = ClientHelpers.Clone(it);
                                t.TargetDevice = it.Device.Id;
                                t.DeviceGroup = null;
                                t.Attribute = ClientHelpers.Clone(att);
                                t.Index = att.Template.Index;
                                //Update attribute object.
                                t.Attribute.Object = it.Object;
                                t.Creator = it.Creator;
                                tasks2.Add(t);
                            }
                            _taskHandler.AddRange(user, tasks2);
                        }
                        else
                        {
                            List<GXTask> tasks2 = new List<GXTask>();
                            GXTask t = ClientHelpers.Clone(it);
                            t.Device = null;
                            t.TargetDevice = it.Device.Id;
                            t.Creator = it.Creator;
                            tasks2.Add(t);
                            _taskHandler.AddRange(user, tasks2);
                        }
                        removed.Add(it);
                    }
                    if (it.TargetDevice != null)
                    {
                        GXSelectArgs args = GXQuery.GetDevicesByUser(creator.Id, false, it.TargetDevice.Value);
                        dev = (await _host.Connection.SingleOrDefaultAsync<GXDevice>(transaction, args));
                        if (dev == null)
                        {
                            throw new ArgumentException(Properties.Resources.UnknownTarget);
                        }
                        it.Device = dev;
                        it.Target = dev.Name;
                        //Get all device attributes and read them one attribute at the time.
                        /*
                        if (it.Object == null && it.Attribute == null)
                        {
                            ListObjects req = new ListObjects()
                            {
                                Filter = new GXObject()
                                {
                                    Device = new GXDevice() { Id = it.TargetDevice.Value }
                                }
                                ,
                                Select = [TargetType.Attribute]
                            };
                            var list2 = await _objectRepository.ListAsync(user, req, null, default);
                            List<GXAttribute> attributes = new List<GXAttribute>();
                            if (list2 != null)
                            {
                                foreach (var o in list2)
                                {
                                    if (o.Latebind)
                                    {
                                        //If object is not created yet.
                                        var tmp2 = await ObjectRepository.CreateLateBindObject(_host,
                                            transaction, creator.Id, req.Filter.Device, o.Id);
                                        o.Latebind = false;
                                        if (tmp2.Attributes != null)
                                        {
                                            attributes.AddRange(tmp2.Attributes);
                                        }
                                    }
                                    else if (o.Attributes != null)
                                    {
                                        attributes.AddRange(o.Attributes);
                                    }
                                }
                                foreach (var a in attributes)
                                {
                                    GXTask t = ClientHelpers.Clone(it);
                                    t.Attribute = a;
                                    t.Creator = it.Creator;
                                    tasks3.Add(t);
                                }
                            }
                            removed.Add(it);
                        }
                        */
                    }
                }
                tasks3.AddRange(tasks);
                tasks3.RemoveAll(w => removed.Contains(w));
                SortedDictionary<Guid, GXObject> objects = new SortedDictionary<Guid, GXObject>();
                foreach (GXTask it in tasks3)
                {
                    bool dynamic = false;
                    if (it.Index == null)
                    {
                        it.Index = 0;
                    }
                    if (it.Order == null)
                    {
                        it.Order = 0;
                    }
                    if (it.TargetDevice == null && it.Object == null && it.Attribute == null &&
                        it.ScriptMethod == null && it.Module == null)
                    {
                        throw new ArgumentNullException(Properties.Resources.UnknownTaskTarget);
                    }
                    if (it.Object != null && it.Object.Id != Guid.Empty)
                    {
                        //Get device ID from the database.
                        arg = GXSelectArgs.Select<GXDevice>(s => new { s.Id, s.Name, s.Dynamic });
                        arg.Joins.AddInnerJoin<GXDevice, GXObject>(s => s.Id, o => o.Device);
                        arg.Where.And<GXObject>(q => q.Id == it.Object.Id);
                        dev = (await _host.Connection.SingleOrDefaultAsync<GXDevice>(transaction, arg));
                        if (dev == null)
                        {
                            //Target device must set when late binding is used.
                            if (it.TargetDevice == null)
                            {
                                throw new ArgumentException(Properties.Resources.UnknownTarget);
                            }
                            GXSelectArgs args = GXQuery.GetDevicesByUser(creator.Id, false, it.TargetDevice.Value);
                            dev = (await _host.Connection.SingleOrDefaultAsync<GXDevice>(transaction, args));
                            if (dev == null)
                            {
                                throw new ArgumentException(Properties.Resources.UnknownTarget);
                            }
                            it.Object = await ObjectRepository.CreateLateBindObject(_host, transaction, creator.Id, dev, it.Object.Id);
                            it.TargetDevice = dev.Id;
                            dynamic = dev.Dynamic.GetValueOrDefault(false);
                            it.Target = dev.Name + " " + it.Object.Template?.Name;
                        }
                        else
                        {
                            it.TargetDevice = dev.Id;
                            dynamic = dev.Dynamic.GetValueOrDefault(false);
                            //Get object name from the database. Client can send all kind of names.
                            arg = GXSelectArgs.Select<GXObjectTemplate>(s => s.Name);
                            arg.Joins.AddInnerJoin<GXObjectTemplate, GXObject>(j => j.Id, j => j.Template);
                            arg.Where.And<GXObject>(w => w.Id == it.Object.Id);
                            it.Target = dev.Name + " " + (await _host.Connection.SingleOrDefaultAsync<string>(transaction, arg));
                        }
                    }
                    else if (it.Attribute != null)
                    {
                        //Get device ID from the database.
                        arg = GXSelectArgs.Select<GXDevice>(s => new { s.Id, s.Name, s.Dynamic });
                        arg.Joins.AddInnerJoin<GXDevice, GXObject>(s => s.Id, o => o.Device);
                        arg.Joins.AddInnerJoin<GXObject, GXAttribute>(s => s.Id, o => o.Object);
                        if (it.Attribute.Id != Guid.Empty)
                        {
                            //Search by ID.
                            arg.Where.And<GXAttribute>(q => q.Id == it.Attribute.Id);
                            dev = (await _host.Connection.SingleOrDefaultAsync<GXDevice>(transaction, arg));
                        }
                        else
                        {
                            //Search by logical name and index.
                            if (it.TargetDevice == null ||
                                it.Attribute?.Object?.Template == null ||
                                string.IsNullOrEmpty(it.Attribute.Object.Template.LogicalName) ||
                                it.Attribute.Template == null ||
                                it.Attribute.Template.Index == 0)
                            {
                                throw new ArgumentException(Properties.Resources.UnknownTarget);
                            }
                            arg.Columns.Add<GXObject>(s => new { s.Id, s.Device });
                            arg.Columns.Add<GXObjectTemplate>(s => s.Id);
                            arg.Columns.Add<GXAttribute>();
                            arg.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(j => j.Template, j => j.Id);
                            arg.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(s => s.Template, o => o.Id);
                            arg.Where.And<GXDevice>(w => w.Removed == null && w.Id == it.TargetDevice.Value);
                            arg.Where.And<GXObjectTemplate>(w => w.Removed == null && w.LogicalName == it.Attribute.Object.Template.LogicalName);
                            arg.Where.And<GXAttributeTemplate>(w => w.Removed == null && w.Index == it.Attribute.Template.Index);
                            obj = (await _host.Connection.SingleOrDefaultAsync<GXObject>(transaction, arg));
                            if (obj != null)
                            {
                                it.Object = obj;
                                it.Attribute = it.Object?.Attributes?.FirstOrDefault();
                                dev = it.Object?.Device;
                            }
                            else
                            {
                                dev = null;
                            }
                        }
                        if (dev == null)
                        {
                            //Target device must set when late binding is used.
                            if (it.TargetDevice == null)
                            {
                                throw new ArgumentException(Properties.Resources.UnknownTarget);
                            }
                            GXSelectArgs args = GXQuery.GetDevicesByUser(creator.Id, false, it.TargetDevice.Value);
                            if (it.Attribute.Id == Guid.Empty)
                            {
                                args.Joins.AddInnerJoin<GXDevice, GXDeviceTemplate>(j => j.Template, j => j.Id);
                                args.Columns.Add<GXDeviceTemplate>(s => s.Id);
                            }
                            dev = (await _host.Connection.SingleOrDefaultAsync<GXDevice>(transaction, args));
                            if (dev == null)
                            {
                                throw new ArgumentException(Properties.Resources.UnknownTarget);
                            }
                            GXAttributeTemplate at;
                            if (it.Attribute.Id == Guid.Empty)
                            {
                                if (dev.Template == null ||
                                    it.Attribute.Template == null ||
                                    it.Attribute.Object?.Template == null)
                                {
                                    throw new ArgumentException(Properties.Resources.UnknownTarget);
                                }
                                //If attribute ID is unknown and logical name is used.
                                arg = GXSelectArgs.Select<GXAttributeTemplate>(s => new { s.Id, s.Index, s.ObjectTemplate },
                                    w => w.Removed == null && w.Index == it.Attribute.Template.Index);
                                arg.Columns.Add<GXObjectTemplate>(s => s.Id);
                                arg.Joins.AddInnerJoin<GXObjectTemplate, GXAttributeTemplate>(j => j.Id, j => j.ObjectTemplate);
                                arg.Joins.AddInnerJoin<GXObjectTemplate, GXDeviceTemplate>(j => j.DeviceTemplate, j => j.Id);
                                arg.Where.And<GXObjectTemplate>(w => w.Removed == null && w.LogicalName == it.Attribute.Object.Template.LogicalName);
                                arg.Where.And<GXDeviceTemplate>(w => w.Removed == null && w.Id == dev.Template.Id);
                                at = (await _host.Connection.SingleOrDefaultAsync<GXAttributeTemplate>(transaction, arg));
                                it.Object = null;
                            }
                            else
                            {
                                arg = GXSelectArgs.Select<GXAttributeTemplate>(s => new { s.Id, s.Index, s.ObjectTemplate },
                                   w => w.Removed == null && w.Id == it.Attribute.Id);
                                arg.Columns.Add<GXObjectTemplate>(s => s.Id);
                                arg.Joins.AddInnerJoin<GXObjectTemplate, GXAttributeTemplate>(j => j.Id, j => j.ObjectTemplate);
                                at = (await _host.Connection.SingleOrDefaultAsync<GXAttributeTemplate>(transaction, arg));
                            }
                            if (at?.ObjectTemplate == null)
                            {
                                throw new ArgumentException(Properties.Resources.UnknownTarget);
                            }
                            //Check that previous attribute didn't create the object.
                            //This happens when multiple attribute tasks handle the same object.
                            if (!objects.TryGetValue(at.ObjectTemplate.Id, out obj))
                            {
                                obj = await ObjectRepository.CreateLateBindObject(_host,
                                transaction, creator.Id, dev, at.ObjectTemplate.Id);
                                objects.Add(at.ObjectTemplate.Id, obj);
                            }
                            //Search attribute by index.
                            it.Attribute = obj.Attributes?.Where(w => w.Template.Index == at.Index).SingleOrDefault();
                            if (it.Attribute == null)
                            {
                                throw new ArgumentException(Properties.Resources.UnknownTarget);
                            }
                            dynamic = dev.Dynamic.GetValueOrDefault(false);
                            it.Target = dev.Name + " " + it.Attribute.Object?.Template?.Name + " " + it.Attribute.Template?.Name;
                        }
                        else
                        {
                            it.TargetDevice = dev.Id;
                            dynamic = dev.Dynamic.GetValueOrDefault(false);
                            //Get object and attribute names from the database. Client can send all kind of names.
                            arg = GXSelectArgs.Select<GXAttributeTemplate>(s => new { s.Name, s.ObjectTemplate });
                            arg.Columns.Add<GXObjectTemplate>(s => new { s.Id, s.Name });
                            arg.Joins.AddInnerJoin<GXObjectTemplate, GXAttributeTemplate>(j => j.Id, j => j.ObjectTemplate);
                            arg.Joins.AddInnerJoin<GXAttributeTemplate, GXAttribute>(j => j.Id, j => j.Template);
                            arg.Where.And<GXAttribute>(w => w.Id == it.Attribute.Id);
                            var a = (await _host.Connection.SingleOrDefaultAsync<GXAttributeTemplate>(transaction, arg));
                            it.Target = dev.Name + " " + a.ObjectTemplate?.Name + " " + a.Name;
                        }
                        //Update the new attribute value.
                        if (it.TaskType == TaskType.Write)
                        {
                            if (string.IsNullOrEmpty(it.Attribute.Value) && !string.IsNullOrEmpty(it.Data))
                            {
                                it.Attribute.Value = it.Data;
                                it.Attribute.LastWrite = now;
                            }
                            await _host.Connection.UpdateAsync(transaction,
                                GXUpdateArgs.Update(it.Attribute, u => u.Value));
                        }
                    }
                    else if (it.TargetDevice == null &&
                        it.ScriptMethod != null &&
                        it.Module != null)
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
                    if (dynamic)
                    {
                        //Get notified agents.
                        arg = GXSelectArgs.Select<GXAgent>(s => new { s.Id });
                        arg.Joins.AddInnerJoin<GXAgent, GXAgentGroupAgent>(j => j.Id, j => j.AgentId);
                        arg.Joins.AddInnerJoin<GXAgentGroupAgent, GXAgentGroup>(j => j.AgentGroupId, j => j.Id);
                        arg.Joins.AddInnerJoin<GXAgentGroup, GXAgentGroupDeviceGroup>(j => j.Id, j => j.AgentGroupId);
                        arg.Joins.AddInnerJoin<GXAgentGroupDeviceGroup, GXDeviceGroup>(j => j.DeviceGroupId, j => j.Id);
                        arg.Joins.AddInnerJoin<GXDeviceGroup, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceGroupId);
                        arg.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDevice>(j => j.DeviceId, j => j.Id);
                        arg.Where.And<GXDevice>(w => w.Id == it.TargetDevice);
                        GXAgent? agent = await _host.Connection.SingleOrDefaultAsync<GXAgent>(transaction, arg);
                        if (agent != null)
                        {
                            it.TargetAgent = agent.Id;
                        }
                        //Get notified agents from gateway devices.
                        arg = GXSelectArgs.Select<GXGateway>(s => new { s.Id, s.Agent });
                        arg.Columns.Add<GXAgent>(s => s.Id);
                        arg.Joins.AddInnerJoin<GXGateway, GXAgent>(j => j.Agent, j => j.Id);
                        arg.Joins.AddInnerJoin<GXGateway, GXGatewayDevice>(j => j.Id, j => j.GatewayId);
                        arg.Joins.AddInnerJoin<GXGatewayDevice, GXDevice>(j => j.DeviceId, j => j.Id);
                        arg.Where.And<GXGateway>(w => w.Removed == null);
                        arg.Where.And<GXAgent>(w => w.Removed == null);
                        arg.Where.And<GXDevice>(w => w.Removed == null && w.Id == it.TargetDevice);
                        var gw = await _host.Connection.SingleOrDefaultAsync<GXGateway>(transaction, arg);
                        if (gw != null && gw.Agent != null)
                        {
                            it.TargetAgent = gw.Agent.Id;
                            it.TargetGateway = gw.Id;
                        }
                        else
                        {
                            //Get notified agents from gateway device groups.
                            arg = GXSelectArgs.Select<GXGateway>(s => new { s.Id, s.Agent });
                            arg.Columns.Add<GXAgent>(s => s.Id);
                            arg.Joins.AddInnerJoin<GXGateway, GXAgent>(j => j.Agent, j => j.Id);
                            arg.Joins.AddInnerJoin<GXGateway, GXGatewayDeviceGroup>(j => j.Id, j => j.GatewayId);
                            arg.Joins.AddInnerJoin<GXGatewayDeviceGroup, GXDeviceGroup>(j => j.DeviceGroupId, j => j.Id);
                            arg.Joins.AddInnerJoin<GXDeviceGroup, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceGroupId);
                            arg.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDevice>(j => j.DeviceId, j => j.Id);
                            arg.Where.And<GXGateway>(w => w.Removed == null);
                            arg.Where.And<GXAgent>(w => w.Removed == null);
                            arg.Where.And<GXDeviceGroup>(w => w.Removed == null);
                            arg.Where.And<GXDevice>(w => w.Removed == null && w.Id == it.TargetDevice);
                            gw = await _host.Connection.SingleOrDefaultAsync<GXGateway>(transaction, arg);
                            if (gw != null && gw.Agent != null)
                            {
                                it.TargetAgent = gw.Agent.Id;
                                it.TargetGateway = gw.Id;
                            }
                        }
                    }
                    else if (it.ScriptMethod != null)
                    {
                        if (user == null)
                        {
                            user = ServerHelpers.CreateClaimsPrincipalFromUser(it.TriggerUser);
                        }
                        await _scriptRepository.RunAsync(user, it.ScriptMethod.Id);
                    }
                    else if (it.Module != null)
                    {
                        if (user == null)
                        {
                            user = ServerHelpers.CreateClaimsPrincipalFromUser(it.TriggerUser);
                        }
                        GXScheduleModule? tmp = new GXScheduleModule()
                        {
                            ModuleId = it.Module.Id,
                            ScheduleId = it.TriggerSchedule.Id,
                        };
                        tmp = await _scheduleRepository.GetModuleSettingsAsync(user, tmp);
                        await _moduleService.ExecuteAsync(user, it.Module, tmp?.Settings);
                    }
                }
                var insert = GXInsertArgs.InsertRange(tasks3);
                insert.Exclude<GXTask>(e => new { e.Ready, e.Result, e.Updated, e.Start, e.OperatingAgent });
                insert.Exclude<GXAttribute>(e => new { e.Object, e.Template });
                insert.Exclude<GXObject>(e => new { e.Device, e.Template });
                await _host.Connection.InsertAsync(transaction, insert);
                _host.Connection.CommitTransaction(transaction);
            }
            catch (Exception)
            {
                _host.Connection.RollbackTransaction(transaction);
                throw;
            }
            IEnumerable<Guid> list = tasks3.Select(s => s.Id);
            await NotifyUsers(user, tasks3, true);
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
        private async Task NotifyUsers(ClaimsPrincipal User, IEnumerable<GXTask> tasks,
            bool add = false)
        {
            List<string> users = new List<string>();
            List<GXTask> list = new List<GXTask>();
            foreach (var it in tasks)
            {
                users.AddDistinct(await GetNofifiedUsersAsync(User, it));
                //Only basic information is send.
                list.Add(new GXTask()
                {
                    Id = it.Id,
                    CreationTime = it.CreationTime,
                    TargetAgent = it.TargetAgent,
                    TargetGateway = it.TargetGateway,
                    TargetDevice = it.TargetDevice
                });
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

        private static GXSelectArgs SelectTasks()
        {
            GXSelectArgs arg = GXSelectArgs.Select<GXTask>(c => new
            {
                c.Id,
                c.CreationTime,
                c.TargetDevice,
                c.Batch
            }, where => where.Start == null && where.Ready == null);
            arg.Distinct = true;
            arg.Count = 1;
            arg.OrderBy.Add<GXTask>(o => o.CreationTime);
            arg.Joins.AddInnerJoin<GXTask, GXDevice>(a => a.TargetDevice.Value, b => b.Id);
            return arg;
        }

        /// <inheritdoc />
        public async Task<GXTask[]> GetNextAsync(
            ClaimsPrincipal User,
            Guid agentId,
            Guid? DeviceId,
            Guid? gatewayId,
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
            //Update agent detected time.
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                IAgentRepository repository = scope.ServiceProvider.GetRequiredService<IAgentRepository>();
                await repository.UpdateStatusAsync(User, agent.Id, AgentStatus.Idle, null);
            }

            arg = SelectTasks();
            GXTask? task = null;
            if (DeviceId != null && DeviceId != Guid.Empty)
            {
                Guid id = DeviceId.Value;
                arg.Where.And<GXDevice>(q => q.Removed == null && q.Id == id);
                //Update device detected time.
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    IDeviceRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
                    await repository.UpdateStatusAsync(User, id, DeviceStatus.Connected);
                }
            }
            else if (gatewayId != null && gatewayId != Guid.Empty)
            {
                Guid id = gatewayId.Value;
                //Update gateway detected time.
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    IGatewayRepository repository = scope.ServiceProvider.GetRequiredService<IGatewayRepository>();
                    await repository.UpdateStatusAsync(User, id, GatewayStatus.Connected);
                }
                //Get devices that are mapped for this gateway.
                GXSelectArgs onGatewayDevices = GXSelectArgs.Select<GXDevice>(c => c.Id);
                onGatewayDevices.Joins.AddInnerJoin<GXDevice, GXGatewayDevice>(a => a.Id, b => b.DeviceId);
                onGatewayDevices.Joins.AddInnerJoin<GXGatewayDevice, GXGateway>(a => a.GatewayId, b => b.Id);
                onGatewayDevices.Where.And<GXGateway>(q => q.Removed == null && q.Id == id);
                arg.Where.And<GXDevice>(q => GXSql.Exists<GXTask, GXDevice>(j => j.TargetDevice, j => j.Id, onGatewayDevices));
                //Get device groups that are mapped for this gateway.
                GXSelectArgs onGatewayDeviceGroups = GXSelectArgs.Select<GXDeviceGroup>(c => c.Id);
                onGatewayDeviceGroups.Joins.AddInnerJoin<GXDeviceGroup, GXGatewayDeviceGroup>(a => a.Id, b => b.DeviceGroupId);
                onGatewayDeviceGroups.Joins.AddInnerJoin<GXGatewayDeviceGroup, GXGateway>(a => a.GatewayId, b => b.Id);
                onGatewayDeviceGroups.Where.And<GXGateway>(q => q.Removed == null && q.Id == id);
                arg.Where.And<GXDevice>(q => GXSql.Exists<GXTask, GXDevice>(j => j.TargetDevice, j => j.Id, onGatewayDevices));
                if (!listener)
                {
                    arg.Where.And<GXDevice>(q => q.Removed == null && q.Dynamic == false);
                }
                // Don't get meters that are read by other agents.
                GXSelectArgs onDeviceProgress = GXSelectArgs.Select<GXDevice>(c => c.Id);
                onDeviceProgress.Where.And<GXTask>(q => q.Ready == null && q.OperatingAgent != null);
                arg.Where.And<GXDevice>(q => !GXSql.Exists<GXTask, GXDevice>(j => j.TargetDevice, j => j.Id, onDeviceProgress));
            }
            else
            {
                //Select tasks that are mapped for calling agent.
                GXSelectArgs ag = GXSelectArgs.Select<GXAgentGroup>(s => GXSql.One, w => w.Removed == null);
                ag.Joins.AddInnerJoin<GXAgentGroup, GXAgentGroupAgent>(j => j.Id, j => j.AgentGroupId);
                ag.Joins.AddInnerJoin<GXAgentGroupAgent, GXAgent>(j => j.AgentId, j => j.Id);
                ag.Where.And<GXAgent>(w => w.Removed == null && w.Id == agentId);
                GXSelectArgs mapped = GXSelectArgs.Select<GXDeviceGroup>(s => GXSql.One, w => w.Removed == null);
                mapped.Joins.AddInnerJoin<GXAgentGroupDeviceGroup, GXDeviceGroup>(j => j.DeviceGroupId, j => j.Id);
                mapped.Where.And<GXAgentGroup>(q => GXSql.Exists<GXAgentGroupDeviceGroup, GXAgentGroup>(j => j.AgentGroupId, j => j.Id, ag));
                GXSelectArgs devs = GXSelectArgs.Select<GXDevice>(s => GXSql.One, w => w.Removed == null);
                devs.Joins.AddInnerJoin<GXDevice, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceId);
                devs.Where.And<GXDeviceGroup>(q => GXSql.Exists<GXDeviceGroup, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceGroupId, mapped));
                arg.Where.And<GXDevice>(q => GXSql.Exists<GXTask, GXDevice>(j => j.TargetDevice, j => j.Id, devs));
                if (!listener)
                {
                    arg.Where.And<GXDevice>(q => q.Removed == null && q.Dynamic == false);
                }
                // Don't get meters that are read by other agents.
                GXSelectArgs onDeviceProgress = GXSelectArgs.Select<GXDevice>(c => c.Id);
                onDeviceProgress.Where.And<GXTask>(q => q.Ready == null && q.OperatingAgent != null);
                arg.Where.And<GXDevice>(q => !GXSql.Exists<GXTask, GXDevice>(j => j.TargetDevice, j => j.Id, onDeviceProgress));
                task = _host.Connection.SingleOrDefault<GXTask>(arg);
                if (task == null)
                {
                    //Select tasks that are not mapped for any agent.
                    arg = SelectTasks();
                    devs = GXSelectArgs.Select<GXDevice>(s => GXSql.One, w => w.Removed == null);
                    devs.Joins.AddInnerJoin<GXDevice, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceId);
                    devs.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDeviceGroup>(j => j.DeviceGroupId, j => j.Id);
                    devs.Joins.AddInnerJoin<GXDeviceGroup, GXAgentGroupDeviceGroup>(j => j.Id, j => j.DeviceGroupId);
                    devs.Where.And<GXDeviceGroup>(q => q.Removed == null);
                    arg.Where.And<GXDevice>(q => !GXSql.Exists<GXTask, GXDevice>(j => j.TargetDevice, j => j.Id, devs));
                    if (!listener)
                    {
                        arg.Where.And<GXDevice>(q => q.Removed == null && q.Dynamic == false);
                    }
                    // Don't get meters that are read by other agents.
                    onDeviceProgress = GXSelectArgs.Select<GXDevice>(c => c.Id);
                    onDeviceProgress.Where.And<GXTask>(q => q.Ready == null && q.OperatingAgent != null);
                    arg.Where.And<GXDevice>(q => !GXSql.Exists<GXTask, GXDevice>(j => j.TargetDevice, j => j.Id, onDeviceProgress));
                }
            }

            GXTask[] tasks = new GXTask[0];
            if (task == null)
            {
                task = _host.Connection.SingleOrDefault<GXTask>(arg);
            }
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
                        Select = new string[] { "Device", "Object", "Attribute" },
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