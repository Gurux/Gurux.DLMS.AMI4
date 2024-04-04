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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared.DTOs.Report;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Client.Pages.User;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class ReportGroupRepository : IReportGroupRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly IUserRepository _userRepository;



        /// <summary>
        /// Constructor.
        /// </summary>
        public ReportGroupRepository(
            IGXHost host,
            IGXEventsNotifier eventsNotifier,
            IUserGroupRepository userGroupRepository,
            IUserRepository userRepository)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userGroupRepository = userGroupRepository;
            _userRepository = userRepository;
        }

        /// <inheritdoc />
        public async Task<List<GXReportGroup>> GetJoinedReportGroups(ClaimsPrincipal user, Guid moduleId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXReportGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXReportGroup, GXReportGroupReport>(a => a.Id, b => b.ReportGroupId);
            arg.Joins.AddInnerJoin<GXReportGroupReport, GXReport>(a => a.ReportId, b => b.Id);
            arg.Where.And<GXReport>(where => where.Removed == null && where.Id == moduleId);
            return (await _host.Connection.SelectAsync<GXReportGroup>(arg));
        }

        private async Task<List<GXUserGroup>> GetJoinedUserGroups(Guid reportGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupReportGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupReportGroup, GXReportGroup>(a => a.ReportGroupId, b => b.Id);
            arg.Where.And<GXReportGroup>(where => where.Removed == null && where.Id == reportGroupId);
            return (await _host.Connection.SelectAsync<GXUserGroup>(arg));
        }

        private async Task<List<GXReport>> GetJoinedReports(ClaimsPrincipal user, Guid reportGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXReport>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXReport, GXReportGroupReport>(a => a.Id, b => b.ReportId);
            arg.Joins.AddInnerJoin<GXReportGroupReport, GXReportGroup>(a => a.ReportGroupId, b => b.Id);

            arg.Joins.AddInnerJoin<GXReportGroup, GXUserGroupReportGroup>(j => j.Id, j => j.ReportGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupReportGroup, GXUserGroup>(x => x.UserGroupId, y => y.Id);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(x => x.Id, y => y.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(x => x.UserId, y => y.Id);
            var id = ServerHelpers.GetUserId(user);
            arg.Where.And<GXUser>(q => q.Removed == null && q.Id == id);
            arg.Where.And<GXReportGroup>(where => where.Removed == null && where.Id == reportGroupId);
            return (await _host.Connection.SelectAsync<GXReport>(arg));
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal user, Guid? groupId)
        {
            GXSelectArgs args = GXQuery.GetUsersByReportGroup(ServerHelpers.GetUserId(user), groupId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal user, IEnumerable<Guid>? agentIds)
        {
            GXSelectArgs args = GXQuery.GetUsersByReportGroups(ServerHelpers.GetUserId(user), agentIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> userGrouprs,
            bool delete)
        {
            if (User == null || (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.ReportGroupManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXReportGroup>(a => new { a.Id, a.Name }, q => userGrouprs.Contains(q.Id));
            List<GXReportGroup> list = _host.Connection.Select<GXReportGroup>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXReportGroup, List<string>> updates = new Dictionary<GXReportGroup, List<string>>();
            foreach (GXReportGroup it in list)
            {
                it.Removed = now;
                List<string> users = await GetUsersAsync(User, it.Id);
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXReportGroup>(it.Id));
                }
                else
                {
                    _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
                }
                updates[it] = users;
            }
            foreach (var it in updates)
            {
                GXReportGroup tmp = new GXReportGroup() { Id = it.Key.Id, Name = it.Key.Name };
                await _eventsNotifier.ReportGroupDelete(it.Value, new GXReportGroup[] { tmp });
            }
        }

        /// <inheritdoc />
        public async Task<GXReportGroup[]> ListAsync(
            ClaimsPrincipal user,
            ListReportGroups? request,
            ListReportGroupsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && user.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the groups.
                arg = GXSelectArgs.SelectAll<GXReportGroup>();
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetReportGroupsByUser(userId, null);
            }
            if (request != null)
            {
                //If report groups are filtered by user.
                if (request.Filter?.UserGroups != null)
                {
                    var ug = request.Filter.UserGroups.FirstOrDefault();
                    if (ug?.Users != null && ug.Users.Any())
                    {
                        var user2 = ug.Users.FirstOrDefault();
                        if (user2 != null)
                        {
                            arg.Joins.AddLeftJoin<GXUserGroupReportGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                            arg.Joins.AddLeftJoin<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId);
                            arg.Joins.AddLeftJoin<GXUserGroupUser, GXUser>(j => j.UserId, j => j.Id);
                            arg.Where.FilterBy(user2);
                        }
                    }
                    request.Filter.UserGroups = null;
                }
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXReportGroup>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXReportGroup>(w => request.Included.Contains(w.Id));
                }
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXReportGroup>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXReportGroup>(q => q.CreationTime);
                arg.Descending = true;
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXReportGroup>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            GXReportGroup[] groups = (await _host.Connection.SelectAsync<GXReportGroup>(arg)).ToArray();
            if (response != null)
            {
                response.ReportGroups = groups;
                if (response.Count == 0)
                {
                    response.Count = groups.Length;
                }
            }
            return groups;
        }

        /// <inheritdoc />
        public async Task<GXReportGroup> ReadAsync(
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
                arg = GXSelectArgs.SelectAll<GXReportGroup>(w => w.Id == id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetReportGroupsByUser(userId, id);
            }
            arg.Columns.Add<GXReportGroup>();
            arg.Columns.Exclude<GXReportGroup>(e => e.Reports);
            arg.Distinct = true;
            var group = (await _host.Connection.SingleOrDefaultAsync<GXReportGroup>(arg));
            if (group == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            ////////////////////////////////////////////////////
            //Get reports that belong for this report group.
            arg = GXSelectArgs.Select<GXReport>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            //It might be that there are no reports in the group. For that reason left join is used.
            arg.Joins.AddLeftJoin<GXReportGroupReport, GXReport>(j => j.ReportId, j => j.Id);
            arg.Where.And<GXReportGroupReport>(q => q.Removed == null && q.ReportGroupId == id);
            group.Reports = await _host.Connection.SelectAsync<GXReport>(arg);

            ////////////////////////////////////////////////////
            //Get user groups that belong for this report group.
            arg = GXSelectArgs.Select<GXUserGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            //It might be that there are no reports in the group. For that reason left join is used.
            arg.Joins.AddInnerJoin<GXUserGroupReportGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
            arg.Where.And<GXUserGroupReportGroup>(q => q.Removed == null && q.ReportGroupId == id);
            group.UserGroups = await _host.Connection.SelectAsync<GXUserGroup>(arg);
            return group;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal user,
            IEnumerable<GXReportGroup> ReportGroups,
            Expression<Func<GXReportGroup, object?>>? columns)
        {
            DateTime now = DateTime.Now;
            GXUser creator = new GXUser() { Id = ServerHelpers.GetUserId(user) };
            List<Guid> list = new List<Guid>();
            Dictionary<GXReportGroup, List<string>> updates = new Dictionary<GXReportGroup, List<string>>();
            foreach (GXReportGroup it in ReportGroups)
            {
                if (string.IsNullOrEmpty(it.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                if (it.UserGroups == null || !it.UserGroups.Any())
                {
                    //Get default user groups.
                    it.UserGroups = await _userGroupRepository.GetDefaultUserGroups(user,
                                               ServerHelpers.GetUserId(user));
                    if (it.UserGroups == null || !it.UserGroups.Any())
                    {
                        throw new ArgumentException(Properties.Resources.TargetMustBelongToOneGroup);
                    }
                }
                if (it.Id == Guid.Empty)
                {
                    it.Creator = creator;
                    it.CreationTime = now;
                    GXInsertArgs args = GXInsertArgs.Insert(it);
                    //User groups must hanlde separetly because users are identified with name and not Guid.
                    args.Exclude<GXReportGroup>(e => new { e.UserGroups });
                    _host.Connection.Insert(args);
                    list.Add(it.Id);
                    AddReportGroupToUserGroups(it.Id, it.UserGroups.Select(s => s.Id).ToArray());
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXReportGroup>(q => q.ConcurrencyStamp, where => where.Id == it.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    it.Updated = now;
                    it.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                    args.Exclude<GXReportGroup>(q => new
                    {
                        q.UserGroups,
                        q.CreationTime,
                        q.Reports
                    });
                    if (!user.IsInRole(GXRoles.Admin) ||
                        it.Creator == null ||
                        string.IsNullOrEmpty(it.Creator.Id))
                    {
                        //Only admin can update the creator.
                        args.Exclude<GXReportGroup>(q => q.Creator);
                    }
                    _host.Connection.Update(args);
                    //Map user group to Report group.
                    List<GXUserGroup> list2 = await GetJoinedUserGroups(it.Id);
                    List<Guid> groups = list2.Select(s => s.Id).ToList();
                    Guid[] tmp = it.UserGroups.Select(s => s.Id).ToArray();
                    Guid[] removed = groups.Except(tmp).ToArray();
                    Guid[] added = tmp.Except(groups).ToArray();
                    if (added.Length != 0)
                    {
                        AddReportGroupToUserGroups(it.Id, added);
                    }
                    if (removed.Length != 0)
                    {
                        RemoveReportGroupFromUserGroups(it.Id, removed);
                    }
                    //Map reports to Report group.
                    //Only adming can add reports that are visible to all users.
                    if (it.Reports != null && (user.IsInRole(GXRoles.Admin) || it.Reports.Any()))
                    {
                        List<GXReport> list3 = await GetJoinedReports(user, it.Id);
                        List<Guid> groups2 = list3.Select(s => s.Id).ToList();
                        tmp = it.Reports.Select(s => s.Id).ToArray();
                        removed = groups2.Except(tmp).ToArray();
                        added = tmp.Except(groups2).ToArray();
                        if (added.Length != 0)
                        {
                            AddReportsToReportGroup(it.Id, added);
                        }
                        if (removed.Length != 0)
                        {
                            RemoveReportsFromReportGroup(it.Id, removed);
                        }
                    }
                }
                updates[it] = await GetUsersAsync(user, it.Id);
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.ReportGroupUpdate(it.Value, new GXReportGroup[] { it.Key });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map report group to user groups.
        /// </summary>
        /// <param name="reportGroupId">Report group ID.</param>
        /// <param name="groups">Group IDs of the report groups where the report is added.</param>
        public void AddReportGroupToUserGroups(Guid reportGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupReportGroup> list = new List<GXUserGroupReportGroup>();
            foreach (var ug in groups)
            {
                list.Add(new GXUserGroupReportGroup()
                {
                    ReportGroupId = reportGroupId,
                    UserGroupId = ug,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between report group and user groups.
        /// </summary>
        /// <param name="reportGroupId">Report group ID.</param>
        /// <param name="groups">Group IDs of the report groups where the report is removed.</param>
        public void RemoveReportGroupFromUserGroups(Guid reportGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupReportGroup>();
            foreach (var ug in groups)
            {
                args.Where.Or<GXUserGroupReportGroup>(w => w.UserGroupId == ug &&
                    w.ReportGroupId == reportGroupId);
            }
            _host.Connection.Delete(args);
        }

        /// <summary>
        /// Map reports to report group.
        /// </summary>
        /// <param name="reportGroupId">Report group ID.</param>
        /// <param name="groups">Group IDs of the report groups where the report is added.</param>
        public void AddReportsToReportGroup(Guid reportGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXReportGroupReport> list = new List<GXReportGroupReport>();
            foreach (var ug in groups)
            {
                list.Add(new GXReportGroupReport()
                {
                    ReportGroupId = reportGroupId,
                    ReportId = ug,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between reports and report group.
        /// </summary>
        /// <param name="reportGroupId">Report group ID.</param>
        /// <param name="groups">Group IDs of the report groups where the report is removed.</param>
        public void RemoveReportsFromReportGroup(Guid reportGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXReportGroupReport>();
            foreach (var it in groups)
            {
                args.Where.Or<GXReportGroupReport>(w => w.ReportId == it &&
                    w.ReportGroupId == reportGroupId);
            }
            _host.Connection.Delete(args);
        }
    }
}