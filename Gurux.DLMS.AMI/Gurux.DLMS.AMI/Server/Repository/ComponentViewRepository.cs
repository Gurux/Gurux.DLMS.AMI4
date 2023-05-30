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
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Client.Shared;
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class ComponentViewRepository : IComponentViewRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IComponentViewGroupRepository _componentViewGroupRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ComponentViewRepository(IGXHost host,
            IUserRepository userRepository,
            IServiceProvider serviceProvider,
            IComponentViewGroupRepository componentViewGroupRepository,
            IGXEventsNotifier eventsNotifier)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
            _serviceProvider = serviceProvider;
            _componentViewGroupRepository = componentViewGroupRepository;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal user, Guid? componentViewId)
        {
            GXSelectArgs args = GXQuery.GetUsersByComponentView(ServerHelpers.GetUserId(user), componentViewId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (user != null && user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal user, IEnumerable<Guid>? componentViewIds)
        {
            GXSelectArgs args = GXQuery.GetUsersByComponentViews(ServerHelpers.GetUserId(user), componentViewIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (user != null && user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal User,
            IEnumerable<Guid> componentViews,
            bool delete)
        {
            if (User == null || (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.ComponentViewManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXComponentView>(a => a.Id, q => componentViews.Contains(q.Id));
            List<GXComponentView> list = _host.Connection.Select<GXComponentView>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXComponentView, List<string>> updates = new Dictionary<GXComponentView, List<string>>();
            foreach (GXComponentView it in list)
            {
                it.Removed = now;
                List<string> users = await GetUsersAsync(User, it.Id);
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXComponentView>(it.Id));
                }
                else
                {
                    _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
                }
                updates[it] = users;
            }
            foreach (var it in updates)
            {
                GXComponentView tmp = new GXComponentView() { Id = it.Key.Id };
                await _eventsNotifier.ComponentViewDelete(it.Value, new GXComponentView[] { tmp });
            }
        }

        /// <inheritdoc />
        public async Task<GXComponentView[]> ListAsync(
            ClaimsPrincipal User,
            ListComponentViews? request,
            ListComponentViewsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the component views.
                arg = GXSelectArgs.SelectAll<GXComponentView>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetComponentViewsByUser(userId, null);
                arg.Columns.Clear();
                arg.Columns.Add<GXComponentView>();
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXComponentView>(w => request.Exclude.Contains(w.Id) == false);
                }
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXComponentView>(q => GXSql.DistinctCount(q.Id));
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
                arg.OrderBy.Add<GXComponentView>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXComponentView>(q => q.Id);
            }
            GXComponentView[] componentViews = (await _host.Connection.SelectAsync<GXComponentView>(arg)).ToArray();
            if (response != null)
            {
                response.ComponentViews = componentViews;
                if (response.Count == 0)
                {
                    response.Count = componentViews.Length;
                }
            }
            return componentViews;
        }

        /// <inheritdoc />
        public async Task<GXComponentView> ReadAsync(
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
                //Admin can see all the component views.
                arg = GXSelectArgs.SelectAll<GXComponentView>(w => w.Id == id);
                arg.Joins.AddLeftJoin<GXComponentView, GXComponentViewGroupComponentView>(x => x.Id, y => y.ComponentViewId);
                arg.Joins.AddLeftJoin<GXComponentViewGroupComponentView, GXComponentViewGroup>(j => j.ComponentViewGroupId, j => j.Id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetComponentViewsByUser(userId, id);
                arg.Joins.AddInnerJoin<GXComponentViewGroupComponentView, GXComponentViewGroup>(j => j.ComponentViewGroupId, j => j.Id);
            }
            arg.Columns.Add<GXComponentViewGroup>();
            arg.Columns.Exclude<GXComponentViewGroup>(e => e.ComponentViews);
            arg.Distinct = true;
            GXComponentView view = await _host.Connection.SingleOrDefaultAsync<GXComponentView>(arg);
            if (view == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }           
            return view;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal User,
            IEnumerable<GXComponentView> componentViews,
            Expression<Func<GXComponentView, object?>>? columns)
        {
            DateTime now = DateTime.Now;
            List<Guid> list = new List<Guid>();
            Dictionary<GXComponentView, List<string>> updates = new Dictionary<GXComponentView, List<string>>();
            foreach (GXComponentView componentView in componentViews)
            {
                if (string.IsNullOrEmpty(componentView.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                if (componentView.ComponentViewGroups == null || !componentView.ComponentViewGroups.Any())
                {
                    ListComponentViewGroups request = new ListComponentViewGroups()
                    {
                        Filter = new GXComponentViewGroup() { Default = true }
                    };
                    componentView.ComponentViewGroups = new List<GXComponentViewGroup>();
                    componentView.ComponentViewGroups.AddRange(await _componentViewGroupRepository.ListAsync(User, request, null, CancellationToken.None));
                    //It's OK if component doesn't belong to component group.
                }
                if (componentView.Id == Guid.Empty)
                {
                    componentView.CreationTime = now;
                    GXInsertArgs args = GXInsertArgs.Insert(componentView);
                    args.Exclude<GXComponentView>(q => new { q.CreationTime, q.ComponentViewGroups });
                    _host.Connection.Insert(args);
                    list.Add(componentView.Id);
                    AddComponentViewToComponentViewGroups(componentView.Id, componentView.ComponentViewGroups);
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXComponentView>(q => q.Updated, where => where.Id == componentView.Id);
                    DateTimeOffset? updated = _host.Connection.SingleOrDefault<DateTimeOffset?>(m);
                    if (updated != null && updated != componentView.Updated)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    List<string> users = await GetUsersAsync(User, componentView.Id);
                    GXUpdateArgs args = GXUpdateArgs.Update(componentView, columns);
                    args.Exclude<GXComponentView>(q => new { q.CreationTime, q.ComponentViewGroups });
                    _host.Connection.Update(args);
                    //Map component view groups to component view.
                    List<GXComponentViewGroup> componentViewGroups;
                    using (IServiceScope scope = _serviceProvider.CreateScope())
                    {
                        IComponentViewGroupRepository componentViewGroupRepository = scope.ServiceProvider.GetRequiredService<IComponentViewGroupRepository>();
                        componentViewGroups = await componentViewGroupRepository.GetJoinedComponentViewGroups(User, componentView.Id);
                    }
                    var comparer = new UniqueComparer<GXComponentViewGroup, Guid>();
                    List<GXComponentViewGroup> removedComponentViewGroups = componentViewGroups.Except(componentView.ComponentViewGroups, comparer).ToList();
                    List<GXComponentViewGroup> addedComponentViewGroups = componentView.ComponentViewGroups.Except(componentViewGroups, comparer).ToList();
                    if (removedComponentViewGroups.Any())
                    {
                        RemoveComponentViewsFromComponentViewGroup(componentView.Id, removedComponentViewGroups);
                    }
                    if (addedComponentViewGroups.Any())
                    {
                        AddComponentViewToComponentViewGroups(componentView.Id, addedComponentViewGroups);
                    }
                    updates[componentView] = users;
                }
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.ComponentViewUpdate(it.Value, new GXComponentView[] { it.Key });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map component view group to component view.
        /// </summary>
        /// <param name="componentViewId">Component view ID.</param>
        /// <param name="groups">Group IDs of the component view groups where the component view is added.</param>
        public void AddComponentViewToComponentViewGroups(Guid componentViewId, IEnumerable<GXComponentViewGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXComponentViewGroupComponentView> list = new List<GXComponentViewGroupComponentView>();
            foreach (GXComponentViewGroup it in groups)
            {
                list.Add(new GXComponentViewGroupComponentView()
                {
                    ComponentViewId = componentViewId,
                    ComponentViewGroupId = it.Id,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between component view group and component view.
        /// </summary>
        /// <param name="componentViewId">Component view ID.</param>
        /// <param name="groups">Group IDs of the component view groups where the component view is removed.</param>
        public void RemoveComponentViewsFromComponentViewGroup(Guid componentViewId, IEnumerable<GXComponentViewGroup> groups)
        {
            foreach (var it in groups)
            {
                GXDeleteArgs args = GXDeleteArgs.Delete<GXComponentViewGroupComponentView>(
                    w => w.ComponentViewId == componentViewId && w.ComponentViewGroupId == it.Id);
                _host.Connection.Delete(args);
            }
        }


        /// <summary>
        /// Refresh component view(s).
        /// </summary>
        public async Task<bool> RefrestAsync(ClaimsPrincipal User)
        {
            Dictionary<GXComponentView, List<string>> updates = new Dictionary<GXComponentView, List<string>>();
            List<GXComponentView> insertList = new List<GXComponentView>();
            List<GXComponentView> updateList = new List<GXComponentView>();
            foreach (Type it in typeof(Client.Pages.Admin.ActiveDeviceErrors).Assembly.GetTypes())
            {
                if (typeof(IGXComponentView).IsAssignableFrom(it) && it.FullName != null)
                {
                    GXSelectArgs args = GXSelectArgs.SelectAll<GXComponentView>(where => where.ClassName == it.FullName);
                    GXComponentView? item = await _host.Connection.SingleOrDefaultAsync<GXComponentView>(args);
                    IGXComponentView? tmp = (IGXComponentView?)Activator.CreateInstance(it);
                    string? configurationUI = null;
                    if (tmp.ConfigurationUI != null)
                    {
                        configurationUI = tmp.ConfigurationUI.FullName;
                    }
                    if (item == null)
                    {
                        item = new GXComponentView()
                        {
                            ClassName = it.FullName,
                            Name = tmp.Name,
                            ConfigurationUI = configurationUI,
                            Icon = tmp.Icon
                        };
                        insertList.Add(item);
                        updates[item] = await GetUsersAsync(User, item.Id);
                    }
                    else
                    {
                        if (item.Icon != tmp.Icon || item.Name != tmp.Name || item.ConfigurationUI != configurationUI)
                        {
                            item.Icon = tmp.Icon;
                            item.Name = tmp.Name;
                            item.ConfigurationUI = configurationUI;
                            updateList.Add(item);
                            updates[item] = await GetUsersAsync(User, item.Id);
                        }
                    }
                }
            }
            await _host.Connection.InsertAsync(GXInsertArgs.InsertRange(insertList));
            await _host.Connection.UpdateAsync(GXUpdateArgs.UpdateRange(updateList));
            foreach (var it in updates)
            {
                await _eventsNotifier.ComponentViewUpdate(it.Value, new GXComponentView[] { it.Key });
            }
            return insertList.Any();
        }
    }
}
