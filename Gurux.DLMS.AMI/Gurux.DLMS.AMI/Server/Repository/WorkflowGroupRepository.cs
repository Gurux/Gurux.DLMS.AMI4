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
// This file is a part of Gurux Workflow Framework.
//
// Gurux Workflow Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Workflow Framework is distributed in the hope that it will be useful,
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
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Client.Pages.User;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc cref="IWorkflowGroupRepository"/>
    public class WorkflowGroupRepository : IWorkflowGroupRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IUserGroupRepository _userGroupRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public WorkflowGroupRepository(
            IGXHost host,
            IUserRepository userRepository,
        IUserGroupRepository userGroupRepository,
        IGXEventsNotifier eventsNotifier)
        {
            _host = host;
            _userRepository = userRepository;
            _eventsNotifier = eventsNotifier;
            _userGroupRepository = userGroupRepository;
        }

        private async Task<List<GXUserGroup>> GetJoinedUserGroups(Guid workflowGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupWorkflowGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupWorkflowGroup, GXWorkflowGroup>(a => a.WorkflowGroupId, b => b.Id);
            arg.Where.And<GXWorkflowGroup>(where => where.Removed == null && where.Id == workflowGroupId);
            return (await _host.Connection.SelectAsync<GXUserGroup>(arg));
        }

        private async Task<List<GXWorkflow>> GetJoinedWorkflows(Guid workflowGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXWorkflow>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXWorkflow, GXWorkflowGroupWorkflow>(a => a.Id, b => b.WorkflowId);
            arg.Joins.AddInnerJoin<GXWorkflowGroupWorkflow, GXWorkflowGroup>(a => a.WorkflowGroupId, b => b.Id);
            arg.Where.And<GXWorkflowGroup>(where => where.Removed == null && where.Id == workflowGroupId);
            return (await _host.Connection.SelectAsync<GXWorkflow>(arg));
        }


        /// <inheritdoc cref="IWorkflowGroupRepository.GetJoinedWorkflowGroups"/>
        public async Task<List<GXWorkflowGroup>> GetJoinedWorkflowGroups(ClaimsPrincipal User, Guid workflowId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXWorkflowGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXWorkflowGroup, GXWorkflowGroupWorkflow>(a => a.Id, b => b.WorkflowGroupId);
            arg.Joins.AddInnerJoin<GXWorkflowGroupWorkflow, GXWorkflow>(a => a.WorkflowId, b => b.Id);
            arg.Where.And<GXWorkflow>(where => where.Removed == null && where.Id == workflowId);
            return (await _host.Connection.SelectAsync<GXWorkflowGroup>(arg));
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal User, Guid? groupId)
        {
            GXSelectArgs args = GXQuery.GetUsersByWorkflowGroup(ServerHelpers.GetUserId(User), groupId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal User, IEnumerable<Guid>? groupIds)
        {
            GXSelectArgs args = GXQuery.GetUsersByWorkflowGroups(ServerHelpers.GetUserId(User), groupIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> userGrouprs)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.WorkflowGroupManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXWorkflowGroup>(a => new { a.Id, a.Name }, q => userGrouprs.Contains(q.Id));
            List<GXWorkflowGroup> list = _host.Connection.Select<GXWorkflowGroup>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXWorkflowGroup, List<string>> updates = new Dictionary<GXWorkflowGroup, List<string>>();
            foreach (GXWorkflowGroup it in list)
            {
                it.Removed = now;
                List<string> users = await GetUsersAsync(User, it.Id);
                _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
                updates[it] = users;
            }
            foreach (var it in updates)
            {
                GXWorkflowGroup tmp = new GXWorkflowGroup() { Id = it.Key.Id, Name = it.Key.Name };
                await _eventsNotifier.WorkflowGroupDelete(it.Value, new GXWorkflowGroup[] { tmp });
            }
        }

        /// <inheritdoc />
        public async Task<GXWorkflowGroup[]> ListAsync(
            ClaimsPrincipal User,
            ListWorkflowGroups? request,
            ListWorkflowGroupsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the workflow groups.
                arg = GXSelectArgs.SelectAll<GXWorkflowGroup>();
            }
            else
            {
                string userId = Internal.ServerHelpers.GetUserId(User);
                arg = GXQuery.GetWorkflowGroupsByUser(userId, null);
            }
            if (request != null && request.Filter != null)
            {
                arg.Where.FilterBy(request.Filter);
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXWorkflowGroup>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            arg.Descending = true;
            arg.OrderBy.Add<GXWorkflowGroup>(q => q.CreationTime);
            GXWorkflowGroup[] groups = (await _host.Connection.SelectAsync<GXWorkflowGroup>(arg)).ToArray();
            if (response != null)
            {
                response.WorkflowGroups = groups;
            }
            return groups;
        }

        /// <inheritdoc cref="IWorkflowGroupRepository.ReadAsync"/>
        public async Task<GXWorkflowGroup> ReadAsync(
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
                //Admin can see all the groups.
                arg = GXSelectArgs.SelectAll<GXWorkflowGroup>(w => w.Id == id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetWorkflowGroupsByUser(userId, id);
            }
            arg.Columns.Exclude<GXWorkflowGroup>(e => e.Workflows);
            arg.Distinct = true;
            GXWorkflow Workflow = await _host.Connection.SingleOrDefaultAsync<GXWorkflow>(arg);
            if (Workflow == null)
            {
                throw new ArgumentNullException(Properties.Resources.UnknownTarget);
            }
            var ret = (await _host.Connection.SingleOrDefaultAsync<GXWorkflowGroup>(arg));
            if (ret == null)
            {
                throw new ArgumentNullException(Properties.Resources.UnknownTarget);
            }
            ////////////////////////////////////////////////////
            //Get workflows that belongs for this workflow group.
            arg = GXSelectArgs.SelectAll<GXWorkflow>(w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXWorkflowGroupWorkflow, GXWorkflow>(j => j.WorkflowId, j => j.Id);
            arg.Where.And<GXWorkflowGroupWorkflow>(q => q.Removed == null && q.WorkflowGroupId == id);
            ret.Workflows = await _host.Connection.SelectAsync<GXWorkflow>(arg);

            ////////////////////////////////////////////////////
            //Get user groups that belongs for this workflow group.
            arg = GXSelectArgs.SelectAll<GXUserGroup>(w => w.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXUserGroupWorkflowGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
            arg.Where.And<GXUserGroupWorkflowGroup>(q => q.Removed == null && q.WorkflowGroupId == id);
            ret.UserGroups = await _host.Connection.SelectAsync<GXUserGroup>(arg);
            return ret;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(ClaimsPrincipal user, IEnumerable<GXWorkflowGroup> WorkflowGroups)
        {
            string userId = ServerHelpers.GetUserId(user);
            DateTime now = DateTime.Now;
            List<Guid> list = new List<Guid>();
            Dictionary<GXWorkflowGroup, List<string>> updates = new Dictionary<GXWorkflowGroup, List<string>>();
            foreach (GXWorkflowGroup it in WorkflowGroups)
            {
                if (string.IsNullOrEmpty(it.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                if (it.UserGroups == null || !it.UserGroups.Any())
                {
                    //Get default script groups if not admin.
                    if (user != null)
                    {
                        it.UserGroups = await _userGroupRepository.GetDefaultUserGroups(user,
                                                   ServerHelpers.GetUserId(user));
                    }
                    if (it.UserGroups == null || !it.UserGroups.Any())
                    {
                        throw new ArgumentException(Properties.Resources.TargetMustBelongToOneGroup);
                    }
                }
                if (it.Workflows != null)
                {
                    //Update creator and creation time for all workflows.
                    foreach (var workflow in it.Workflows)
                    {
                        workflow.CreationTime = now;
                        workflow.Creator = new GXUser() { Id = userId };
                    }
                }

                if (it.Id == Guid.Empty)
                {
                    it.CreationTime = now;
                    GXInsertArgs args = GXInsertArgs.Insert(it);
                    //User groups must hanlde separetly because users are identified with name and not Guid.
                    args.Exclude<GXWorkflowGroup>(e => e.UserGroups);
                    _host.Connection.Insert(args);
                    list.Add(it.Id);
                    AddWorkflowGroupToUserGroups(it.Id, it.UserGroups.Select(s => s.Id).ToArray());
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXWorkflowGroup>(q => q.ConcurrencyStamp, where => where.Id == it.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    it.Updated = now;
                    it.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(it);
                    args.Exclude<GXWorkflowGroup>(q => new { q.UserGroups, q.CreationTime });
                    _host.Connection.Update(args);
                    //Map user group to Workflow group.
                    List<GXUserGroup> list2 = await GetJoinedUserGroups(it.Id);
                    List<Guid> groups = list2.Select(s => s.Id).ToList();
                    Guid[] tmp = it.UserGroups.Select(s => s.Id).ToArray();
                    Guid[] removed = groups.Except(tmp).ToArray();
                    Guid[] added = tmp.Except(groups).ToArray();
                    if (added.Length != 0)
                    {
                        AddWorkflowGroupToUserGroups(it.Id, added);
                    }
                    if (removed.Length != 0)
                    {
                        RemoveWorkflowGroupFromUserGroups(it.Id, removed);
                    }
                    //Map workflows to Workflow group.
                    if (it.Workflows != null && it.Workflows.Count != 0)
                    {
                        List<GXWorkflow> list3 = await GetJoinedWorkflows(it.Id);
                        List<Guid> groups2 = list3.Select(s => s.Id).ToList();
                        tmp = it.Workflows.Select(s => s.Id).ToArray();
                        removed = groups2.Except(tmp).ToArray();
                        added = tmp.Except(groups2).ToArray();
                        if (added.Length != 0)
                        {
                            AddWorkflowsToWorkflowGroup(it.Id, added);
                        }
                        if (removed.Length != 0)
                        {
                            RemoveWorkflowsFromWorkflowGroup(it.Id, removed);
                        }
                    }
                }
                updates[it] = await GetUsersAsync(user, it.Id);
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.WorkflowGroupUpdate(it.Value, new GXWorkflowGroup[] { it.Key });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map workflow group to user groups.
        /// </summary>
        /// <param name="workflowGroupId">Workflow group ID.</param>
        /// <param name="group">Group IDs of the workflow groups where the workflow is added.</param>
        public void AddWorkflowGroupToUserGroups(Guid workflowGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupWorkflowGroup> list = new List<GXUserGroupWorkflowGroup>();
            foreach (var ug in groups)
            {
                list.Add(new GXUserGroupWorkflowGroup()
                {
                    WorkflowGroupId = workflowGroupId,
                    UserGroupId = ug,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between workflow group and user groups.
        /// </summary>
        /// <param name="workflowGroupId">Workflow group ID.</param>
        /// <param name="group">Group IDs of the workflow groups where the workflow is removed.</param>
        public void RemoveWorkflowGroupFromUserGroups(Guid workflowGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupWorkflowGroup>();
            foreach (var ug in groups)
            {
                args.Where.Or<GXUserGroupWorkflowGroup>(w => w.UserGroupId == ug &&
                    w.WorkflowGroupId == workflowGroupId);
            }
            _host.Connection.Delete(args);
        }

        /// <summary>
        /// Map workflows to workflow group.
        /// </summary>
        /// <param name="workflowGroupId">Workflow group ID.</param>
        /// <param name="group">Group IDs of the workflow groups where the workflow is added.</param>
        public void AddWorkflowsToWorkflowGroup(Guid workflowGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXWorkflowGroupWorkflow> list = new List<GXWorkflowGroupWorkflow>();
            foreach (var ug in groups)
            {
                list.Add(new GXWorkflowGroupWorkflow()
                {
                    WorkflowGroupId = workflowGroupId,
                    WorkflowId = ug,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between workflows and workflow group.
        /// </summary>
        /// <param name="workflowGroupId">Workflow group ID.</param>
        /// <param name="group">Group IDs of the workflow groups where the workflow is removed.</param>
        public void RemoveWorkflowsFromWorkflowGroup(Guid workflowGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXWorkflowGroupWorkflow>();
            foreach (var it in groups)
            {
                args.Where.Or<GXWorkflowGroupWorkflow>(w => w.WorkflowId == it &&
                    w.WorkflowGroupId == workflowGroupId);
            }
            _host.Connection.Delete(args);
        }
    }
}