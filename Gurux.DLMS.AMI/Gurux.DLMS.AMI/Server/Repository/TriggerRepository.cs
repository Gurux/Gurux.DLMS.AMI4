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
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.AMI.Server.Models;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Identity;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Triggers;
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class TriggerRepository : ITriggerRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly ITriggerGroupRepository _triggerGroupRepository;


        /// <summary>
        /// Constructor.
        /// </summary>
        public TriggerRepository(IGXHost host,
            IUserRepository userRepository,
            IServiceProvider serviceProvider,
            IGXEventsNotifier eventsNotifier,
            ITriggerGroupRepository triggerGroupRepository)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
            _serviceProvider = serviceProvider;
            _triggerGroupRepository = triggerGroupRepository;
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal User, Guid? triggerId)
        {
            GXSelectArgs args = GXQuery.GetUsersByTrigger(ServerHelpers.GetUserId(User), triggerId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal User, IEnumerable<Guid>? triggerId)
        {
            GXSelectArgs args = GXQuery.GetUsersByTriggers(ServerHelpers.GetUserId(User), triggerId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(ClaimsPrincipal User,
            IEnumerable<Guid> triggers,
            bool delete)
        {
            if (User == null || (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.TriggerManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXTrigger>(a => a.Id, q => triggers.Contains(q.Id));
            List<GXTrigger> list = _host.Connection.Select<GXTrigger>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXTrigger, List<string>> updates = new Dictionary<GXTrigger, List<string>>();
            foreach (GXTrigger it in list)
            {
                it.Removed = now;
                List<string> users = await GetUsersAsync(User, it.Id);
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXTrigger>(it.Id));
                }
                else
                {
                    _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
                }
                updates[it] = users;
            }
            foreach (var it in updates)
            {
                GXTrigger tmp = new GXTrigger() { Id = it.Key.Id };
                await _eventsNotifier.TriggerDelete(it.Value, new GXTrigger[] { tmp });
            }
        }

        /// <inheritdoc />
        public async Task<GXTrigger[]> ListAsync(
            ClaimsPrincipal User,
            ListTriggers? request,
            ListTriggersResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the triggers.
                arg = GXSelectArgs.SelectAll<GXTrigger>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetTriggersByUser(userId, null);
            }
            if (request != null && request.Filter != null)
            {
                arg.Where.FilterBy(request.Filter);
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXTrigger>(q => GXSql.DistinctCount(q.Id));
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
                arg.OrderBy.Add<GXTrigger>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXTrigger>(q => q.Id);
            }

            GXTrigger[] triggers = (await _host.Connection.SelectAsync<GXTrigger>(arg)).ToArray();
            //Get trigger activities.
            foreach (var trigger in triggers)
            {
                //There can be multiple activities per trigger for this reason own query is used.
                arg = GXSelectArgs.Select<GXTriggerActivity>(s => new { s.Id, s.Name }, w => w.Trigger == trigger);
                arg.Columns.Exclude<GXTriggerActivity>(e => e.Trigger);
                trigger.Activities = await _host.Connection.SelectAsync<GXTriggerActivity>(arg);
            }
            if (response != null)
            {
                response.Triggers = triggers;
                if (response.Count == 0)
                {
                    response.Count = triggers.Length;
                }
            }
            return triggers;
        }

        /// <inheritdoc />
        public async Task<GXTrigger> ReadAsync(
            ClaimsPrincipal User,
            Guid id)
        {
            bool isAdmin = false;
            if (User != null)
            {
                isAdmin = User.IsInRole(GXRoles.Admin);
            }
            GXSelectArgs arg;
            if (User == null || isAdmin)
            {
                //Admin can see all the triggers.
                arg = GXSelectArgs.SelectAll<GXTrigger>(w => w.Id == id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetTriggersByUser(userId, id);
            }
            //Get trigger activities.
            arg.Columns.Add<GXTriggerActivity>();
            arg.Columns.Exclude<GXTriggerActivity>(e => e.Trigger);
            arg.Joins.AddInnerJoin<GXTrigger, GXTriggerActivity>(j => j.Id, j => j.Trigger);
            GXTrigger trigger = await _host.Connection.SingleOrDefaultAsync<GXTrigger>(arg);
            if (trigger == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            return trigger;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal User,
            IEnumerable<GXTrigger> triggers,
            Expression<Func<GXTrigger, object?>>? columns)
        {
            DateTime now = DateTime.Now;
            List<Guid> list = new List<Guid>();
            Dictionary<GXTrigger, List<string>> updates = new Dictionary<GXTrigger, List<string>>();
            foreach (GXTrigger trigger in triggers)
            {
                if (string.IsNullOrEmpty(trigger.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                if (trigger.TriggerGroups == null || !trigger.TriggerGroups.Any())
                {
                    trigger.TriggerGroups = new List<GXTriggerGroup>();
                    ListTriggerGroups request = new ListTriggerGroups() { Filter = new GXTriggerGroup() { Default = true } };
                    trigger.TriggerGroups.AddRange(await _triggerGroupRepository.ListAsync(User, request, null, CancellationToken.None));
                    if (!trigger.TriggerGroups.Any())
                    {
                        throw new ArgumentNullException(Properties.Resources.ArrayIsEmpty);
                    }
                }
                if (trigger.Id == Guid.Empty)
                {
                    trigger.CreationTime = now;
                    GXInsertArgs args = GXInsertArgs.Insert(trigger);
                    args.Exclude<GXTrigger>(q => new { q.Updated, q.TriggerGroups });
                    _host.Connection.Insert(args);
                    list.Add(trigger.Id);
                    if (trigger.TriggerGroups != null && trigger.TriggerGroups.Count != 0)
                    {
                        AddTriggerToTriggerGroups(trigger.Id, trigger.TriggerGroups);
                    }
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXTrigger>(q => q.ConcurrencyStamp, where => where.Id == trigger.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != trigger.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    trigger.Updated = now;
                    trigger.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(trigger, columns);
                    args.Exclude<GXTrigger>(q => new { q.CreationTime, q.TriggerGroups });
                    _host.Connection.Update(args);
                    //Map trigger groups to trigger.
                    List<GXTriggerGroup> triggerGroups;
                    using (IServiceScope scope = _serviceProvider.CreateScope())
                    {
                        ITriggerGroupRepository triggerGroupRepository = scope.ServiceProvider.GetRequiredService<ITriggerGroupRepository>();
                        triggerGroups = await triggerGroupRepository.GetJoinedTriggerGroups(User, trigger.Id);
                    }
                    var comparer = new UniqueComparer<GXTriggerGroup, Guid>();
                    List<GXTriggerGroup> removedTriggerGroups = triggerGroups.Except(trigger.TriggerGroups, comparer).ToList();
                    List<GXTriggerGroup> addedTriggerGroups = trigger.TriggerGroups.Except(triggerGroups, comparer).ToList();
                    if (removedTriggerGroups.Any())
                    {
                        RemoveTriggersFromTriggerGroup(trigger.Id, removedTriggerGroups);
                    }
                    if (addedTriggerGroups.Any())
                    {
                        AddTriggerToTriggerGroups(trigger.Id, addedTriggerGroups);
                    }
                    updates[trigger] = await GetUsersAsync(User, trigger.Id);
                }
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.TriggerUpdate(it.Value, new GXTrigger[] { it.Key });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map trigger group to user groups.
        /// </summary>
        /// <param name="triggerId">Trigger ID.</param>
        /// <param name="groups">Group IDs of the trigger groups where the trigger is added.</param>
        public void AddTriggerToTriggerGroups(Guid triggerId, IEnumerable<GXTriggerGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXTriggerGroupTrigger> list = new List<GXTriggerGroupTrigger>();
            foreach (GXTriggerGroup it in groups)
            {
                list.Add(new GXTriggerGroupTrigger()
                {
                    TriggerId = triggerId,
                    TriggerGroupId = it.Id,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between trigger group and trigger.
        /// </summary>
        /// <param name="triggerId">Trigger ID.</param>
        /// <param name="groups">Group IDs of the trigger groups where the trigger is removed.</param>
        public void RemoveTriggersFromTriggerGroup(Guid triggerId, IEnumerable<GXTriggerGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXTriggerGroupTrigger> list = new List<GXTriggerGroupTrigger>();
            foreach (var it in groups)
            {
                list.Add(new GXTriggerGroupTrigger()
                {
                    TriggerId = triggerId,
                    TriggerGroupId = it.Id,
                    Removed = now
                });
            }
            _host.Connection.Delete(GXDeleteArgs.DeleteRange(list));
        }

        /// <summary>
        /// Refresh component view(s).
        /// </summary>
        public async Task RefrestAsync(ClaimsPrincipal User)
        {
            Dictionary<GXTrigger, List<string>> updates = new Dictionary<GXTrigger, List<string>>();
            List<GXTrigger> list = (await ServerHelpers.UpdateTriggersAsync(_host, typeof(AgentTrigger).Assembly, true));
            foreach (var it in list)
            {
                updates[it] = await GetUsersAsync(User, it.Id);
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.TriggerUpdate(it.Value, new GXTrigger[] { it.Key });
            }
        }
    }
}
