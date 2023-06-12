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
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared;
using System.Diagnostics;
using System.Linq;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class ModuleRepository : IModuleRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IModuleGroupRepository _moduleGroupRepository;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ModuleRepository(IGXHost host,
            IUserRepository userRepository,
            IGXEventsNotifier eventsNotifier,
            IModuleGroupRepository moduleGroupRepository,
            IServiceProvider serviceProvider)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
            _moduleGroupRepository = moduleGroupRepository;
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal User,
            string? moduleId)
        {
            GXSelectArgs args = GXQuery.GetUsersByModule(ServerHelpers.GetUserId(User), moduleId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal User,
            IEnumerable<string>? moduleIds)
        {
            GXSelectArgs args = GXQuery.GetUsersByModules(ServerHelpers.GetUserId(User), moduleIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal User, IEnumerable<string> modules)
        {
            if (User == null || (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.ModuleManager)))
            {
                throw new UnauthorizedAccessException();
            }
            Dictionary<GXModule, List<string>> updates = new();
            foreach (string it in modules)
            {
                GXModule module = _host.Connection.SelectById<GXModule>(it);
                if (module == null)
                {
                    throw new ArgumentException(string.Format("Module {0} don't exists.", it));
                }
                updates[module] = await GetUsersAsync(User, it);
                if ((module.Status & ModuleStatus.CustomBuild) != 0)
                {
                    //Delete module if module is custom module.
                    await _host.Connection.DeleteAsync(GXDeleteArgs.Delete<GXModule>(where => where.Id == module.Id));
                }
                else
                {
                    //If installable module is removed.
                    module.Status = ModuleStatus.Installable;
                    module.Active = false;
                    module.Updated = DateTime.Now;
                    module.Version = null;
                    await _host.Connection.UpdateAsync(GXUpdateArgs.Update(module, c => new
                    {
                        c.Status,
                        c.Active,
                        c.Updated,
                        c.Version
                    }));
                    //Delete module assemblies.
                    await _host.Connection.DeleteAsync(GXDeleteArgs.Delete<GXModuleAssembly>(where => where.Module == module));
                }
            }
            foreach (var it in updates)
            {
                GXModule tmp = new GXModule() { Id = it.Key.Id };
                await _eventsNotifier.ModuleDelete(it.Value, new GXModule[] { tmp });
            }


            List<GXModuleLog> list = new List<GXModuleLog>();
            foreach (var it in updates.Keys)
            {
                list.Add(new GXModuleLog(TraceLevel.Info)
                {
                    CreationTime = DateTime.Now,
                    Module = it,
                    Message = Properties.Resources.ModuleRemoved
                });
            }
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                IModuleLogRepository moduleLogRepository = scope.ServiceProvider.GetRequiredService<IModuleLogRepository>();
                await moduleLogRepository.AddAsync(User, list);
            }
        }

        /// <inheritdoc />
        public async Task<GXModule[]> ListAsync(
            ClaimsPrincipal User,
            ListModules? request,
            ListModulesResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the modules.
                arg = GXSelectArgs.SelectAll<GXModule>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetModulesByUser(userId, null);
                arg.Columns.Clear();
                arg.Columns.Add<GXModule>();
            }
            ModuleStatus? status = null;
            if (request != null && request.Filter != null && request.Filter.Status != null)
            {
                status = request.Filter.Status;
                request.Filter.Status = null;
            }

            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXModule>(w => !request.Exclude.Contains(w.Id));
                }
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXModule>(q => GXSql.DistinctCount(q.Id));
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
                arg.OrderBy.Add<GXModule>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXModule>(q => q.Id);
            }
            List<GXModule> modules = (await _host.Connection.SelectAsync<GXModule>(arg)).ToList();
            if (status != null)
            {
                modules.RemoveAll(w => (w.Status & status) == 0);
            }
            if (response != null)
            {
                response.Modules = modules.ToArray();
                if (response.Count == 0)
                {
                    response.Count = modules.Count;
                }
            }
            return modules.ToArray();
        }

        /// <inheritdoc />
        public async Task<GXModule[]> ListWithVersionsAsync(ClaimsPrincipal User)
        {
            bool isAdmin = true;
            if (User != null)
            {
                isAdmin = User.IsInRole(GXRoles.Admin);
            }
            GXSelectArgs arg;
            if (isAdmin || User == null)
            {
                //Admin can see all the modules.
                arg = GXSelectArgs.SelectAll<GXModule>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetModulesByUser(userId, null);
            }
            arg.Columns.Add<GXModuleVersion>();
            arg.Columns.Exclude<GXModuleVersion>(e => e.Module);
            arg.Joins.AddLeftJoin<GXModule, GXModuleVersion>(j => j.Id, j => j.Module);
            arg.Distinct = true;
            arg.OrderBy.Add<GXModule>(q => q.Id);
            GXModule[] modules = (await _host.Connection.SelectAsync<GXModule>(arg)).ToArray();
            return modules;
        }


        /// <inheritdoc />
        public async Task<GXModule> ReadAsync(ClaimsPrincipal User, string id)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXModule>(where => where.Id == id);
            arg.Columns.Add<GXModuleVersion>();
            arg.Columns.Exclude<GXModuleVersion>(e => e.Module);
            arg.Joins.AddLeftJoin<GXModule, GXModuleVersion>(j => j.Id, j => j.Module);
            var module = await _host.Connection.SingleOrDefaultAsync<GXModule>(arg);
            if (module == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            //Get script and methods.
            arg = GXSelectArgs.Select<GXScript>(s => new { s.Id, s.Name }, w => w.Module == module);
            arg.Columns.Add<GXScriptMethod>();
            arg.Columns.Exclude<GXScriptMethod>(e => e.Script);
            arg.Joins.AddLeftJoin<GXScript, GXScriptMethod>(j => j.Id, j => j.Script);
            module.Scripts = await _host.Connection.SelectAsync<GXScript>(arg);
            //Get module groups.
            arg = GXSelectArgs.Select<GXModuleGroup>(s => new { s.Id, s.Name }, where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXModuleGroup, GXModuleGroupModule>(j => j.Id, j => j.ModuleGroupId);
            arg.Where.And<GXModuleGroupModule>(w => w.ModuleId == id);
            module.ModuleGroups = await _host.Connection.SelectAsync<GXModuleGroup>(arg);
            //Get assemblies.
            arg = GXSelectArgs.Select<GXModuleAssembly>(s => new { s.Id, s.FileName }, where => where.Module == module);
            arg.Distinct = true;
            arg.Columns.Exclude<GXModuleAssembly>(e => e.Module);
            module.Assemblies = await _host.Connection.SelectAsync<GXModuleAssembly>(arg);
            return module;
        }

        /// <inheritdoc />
        public async Task UpdateAsync(ClaimsPrincipal user, GXModule module,
            Expression<Func<GXModule, object?>>? columns)
        {
            //Verify that module exists and check if active is changed.
            GXSelectArgs arg = GXSelectArgs.Select<GXModule>(s => s.Active,
                w => w.Id == module.Id);
            var mod = await _host.Connection.SingleOrDefaultAsync<GXModule>(arg);
            if (mod == null)
            {
                throw new GXAmiNotFoundException(Properties.Resources.Module + " " + Properties.Resources.Id + " " + module.Id);
            }
            bool activityChange = mod.Active != module.Active;
            GXUpdateArgs args = GXUpdateArgs.Update(module, columns);
            module.Updated = DateTime.Now;
            args.Exclude<GXModule>(e => new { e.Versions, e.ModuleGroups });
            await _host.Connection.UpdateAsync(args);
            //Add new versions.
            arg = GXSelectArgs.SelectAll<GXModuleVersion>(where => where.Module == module);
            List<GXModuleVersion> versions = _host.Connection.Select<GXModuleVersion>(arg);
            var comparer = new UniqueComparer<GXModuleVersion, Guid>();
            List<GXModuleVersion> addedVersions = module.Versions.Except(versions, comparer).ToList();
            if (addedVersions.Any())
            {
                GXInsertArgs i = GXInsertArgs.InsertRange(addedVersions);
                await _host.Connection.InsertAsync(i);
            }
            //Update groups.
            if (module.ModuleGroups != null)
            {
                arg = GXSelectArgs.SelectAll<GXModuleGroup>(where => where.Removed == null);
                arg.Joins.AddInnerJoin<GXModuleGroup, GXModuleGroupModule>(j => j.Id, j => j.ModuleGroupId);
                arg.Joins.AddInnerJoin<GXModuleGroupModule, GXModule>(j => j.ModuleId, j => j.Id);

                List<GXModuleGroup> groups = _host.Connection.Select<GXModuleGroup>(arg);
                var comparer2 = new UniqueComparer<GXModuleGroup, Guid>();
                List<GXModuleGroup> removedModuleGroups = groups.Except(module.ModuleGroups, comparer2).ToList();
                List<GXModuleGroup> addedModuleGroups = module.ModuleGroups.Except(groups, comparer2).ToList();
                if (removedModuleGroups.Any())
                {
                    RemoveModulesFromModuleGroup(module.Id, removedModuleGroups);
                }
                if (addedModuleGroups.Any())
                {
                    AddModuleToModuleGroups(module.Id, addedModuleGroups);
                }
                Dictionary<GXModule, List<string>> updates = new();
                updates[module] = await GetUsersAsync(user, module.Id);
                foreach (var it in updates)
                {
                    await _eventsNotifier.ModuleUpdate(it.Value, new GXModule[] { it.Key });
                }
            }
            if (activityChange)
            {
                string msg;
                if (module.Active.GetValueOrDefault())
                {
                    msg = Properties.Resources.ModuleEnabled;
                }
                else
                {
                    msg = Properties.Resources.ModuleDisabled;
                }
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    IModuleLogRepository moduleLogRepository = scope.ServiceProvider.GetRequiredService<IModuleLogRepository>();
                    await moduleLogRepository.AddAsync(user, new GXModuleLog[]
                    {
                    new GXModuleLog(TraceLevel.Info)
                    {
                        CreationTime = DateTime.Now,
                        Module = module,
                        Message = msg
                    }
                });
                }
            }
        }

        /// <inheritdoc />
        public async Task AddAsync(ClaimsPrincipal User, IEnumerable<GXModule> modules)
        {
            DateTime now = DateTime.Now;
            GXUser user = new GXUser() { Id = ServerHelpers.GetUserId(User) };
            foreach (var it in modules)
            {
                if (it.CreationTime == DateTime.MinValue)
                {
                    it.CreationTime = now;
                }
                if (!it.ModuleGroups.Any())
                {
                    ListModuleGroups request = new ListModuleGroups() { Filter = new GXModuleGroup() { Default = true } };
                    GXModuleGroup[] groups = await _moduleGroupRepository.ListAsync(User, request, null, CancellationToken.None);
                    it.ModuleGroups.AddRange(groups);
                }
                it.Creator = user;
            }
            GXInsertArgs args = GXInsertArgs.InsertRange(modules);
            await _host.Connection.InsertAsync(args);
            List<GXModuleLog> list = new List<GXModuleLog>();
            foreach (var it in modules)
            {
                list.Add(new GXModuleLog(TraceLevel.Info)
                {
                    CreationTime = DateTime.Now,
                    Module = it,
                    Message = Properties.Resources.ModuleRemoved
                });
            }
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                IModuleLogRepository moduleLogRepository = scope.ServiceProvider.GetRequiredService<IModuleLogRepository>();
                await moduleLogRepository.AddAsync(User, list);
            }
        }

        /// <inheritdoc />
        public async Task<List<GXScript>> GetScriptsAsync(ClaimsPrincipal User, string? moduleId)
        {
            GXModule module = new GXModule() { Id = moduleId };
            GXSelectArgs args = GXSelectArgs.SelectAll<GXScript>(w => w.Removed == null && w.Module == module);
            return await _host.Connection.SelectAsync<GXScript>(args);
        }

        /// <summary>
        /// Map module group to user groups.
        /// </summary>
        /// <param name="moduleId">Module ID.</param>
        /// <param name="groups">Group IDs of the module groups where the module is added.</param>
        public void AddModuleToModuleGroups(string moduleId, IEnumerable<GXModuleGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXModuleGroupModule> list = new();
            foreach (GXModuleGroup it in groups)
            {
                list.Add(new GXModuleGroupModule()
                {
                    ModuleId = moduleId,
                    ModuleGroupId = it.Id,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between module group and module.
        /// </summary>
        /// <param name="moduleId">Module ID.</param>
        /// <param name="groups">Group IDs of the module groups where the module is removed.</param>
        public void RemoveModulesFromModuleGroup(string moduleId, IEnumerable<GXModuleGroup> groups)
        {
            foreach (var it in groups)
            {
                _host.Connection.Delete(GXDeleteArgs.Delete<GXModuleGroupModule>(w => w.ModuleId == moduleId && w.ModuleGroupId == it.Id));
            }
        }
    }
}
