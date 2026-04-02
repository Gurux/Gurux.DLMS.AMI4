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
using Gurux.DLMS.AMI.Shared.DTOs.ContentType;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Client.Shared.Enums;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class ContentTypeGroupRepository : IContentTypeGroupRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly IUserRepository _userRepository;
        private readonly GXPerformanceSettings _performanceSettings;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ContentTypeGroupRepository(
            IGXHost host,
            IGXAmiContextAccessor contextAccessor,
            IGXEventsNotifier eventsNotifier,
            IUserGroupRepository userGroupRepository,
            IUserRepository userRepository,
            GXPerformanceSettings performanceSettings)
        {
            var user = contextAccessor?.User;
            if (user == null)
            {
                throw new UnauthorizedAccessException();
            }
            User = user;
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userGroupRepository = userGroupRepository;
            _userRepository = userRepository;
            _performanceSettings = performanceSettings;
        }

        /// <inheritdoc />
        public async Task<List<GXContentTypeGroup>> GetJoinedContentTypeGroups(Guid moduleId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXContentTypeGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXContentTypeGroup, GXContentTypeGroupContentType>(a => a.Id, b => b.ContentTypeGroupId);
            arg.Joins.AddInnerJoin<GXContentTypeGroupContentType, GXContentType>(a => a.ContentTypeId, b => b.Id);
            arg.Where.And<GXContentType>(where => where.Removed == null && where.Id == moduleId);
            return (await _host.Connection.SelectAsync<GXContentTypeGroup>(arg));
        }


        private async Task<List<GXUserGroup>> GetJoinedUserGroups(Guid ContentTypeGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupContentTypeGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupContentTypeGroup, GXContentTypeGroup>(a => a.ContentTypeGroupId, b => b.Id);
            arg.Where.And<GXContentTypeGroup>(where => where.Removed == null && where.Id == ContentTypeGroupId);
            return (await _host.Connection.SelectAsync<GXUserGroup>(arg));
        }

        private async Task<List<GXContentType>> GetJoinedContentTypes(Guid ContentTypeGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXContentType>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXContentType, GXContentTypeGroupContentType>(a => a.Id, b => b.ContentTypeId);
            arg.Joins.AddInnerJoin<GXContentTypeGroupContentType, GXContentTypeGroup>(a => a.ContentTypeGroupId, b => b.Id);

            arg.Joins.AddInnerJoin<GXContentTypeGroup, GXUserGroupContentTypeGroup>(j => j.Id, j => j.ContentTypeGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupContentTypeGroup, GXUserGroup>(x => x.UserGroupId, y => y.Id);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(x => x.Id, y => y.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(x => x.UserId, y => y.Id);
            var id = ServerHelpers.GetUserId(User);
            arg.Where.And<GXUser>(q => q.Removed == null && q.Id == id);
            arg.Where.And<GXContentTypeGroup>(where => where.Removed == null && where.Id == ContentTypeGroupId);
            return (await _host.Connection.SelectAsync<GXContentType>(arg));
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? groupId)
        {
            GXSelectArgs args = GXQuery.GetUsersByContentTypeGroup(s => s.Id,
                ServerHelpers.GetUserId(User), groupId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? agentIds)
        {
            GXSelectArgs args = GXQuery.GetUsersByContentTypeGroups(s => s.Id,
                ServerHelpers.GetUserId(User), agentIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(IEnumerable<Guid> userGrouprs,
            bool delete)
        {
            if (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.ContentType) &&
                !User.HasScope(GXContentTypeGroupPolicies.Delete))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXContentTypeGroup>(a => new { a.Id, a.Name }, q => userGrouprs.Contains(q.Id));
            List<GXContentTypeGroup> list = _host.Connection.Select<GXContentTypeGroup>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXContentTypeGroup, List<string>> updates = new Dictionary<GXContentTypeGroup, List<string>>();
            foreach (GXContentTypeGroup it in list)
            {
                it.Removed = now;
                List<string> users = await GetUsersAsync(it.Id);
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXContentTypeGroup>(it.Id));
                }
                else
                {
                    _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
                }
                updates[it] = users;
            }
            foreach (var it in updates)
            {
                var users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                    it.Value, TargetType.ContentTypeGroup, NotificationAction.Remove);
                if (users == null)
                {
                    break;
                }
                GXContentTypeGroup tmp = new GXContentTypeGroup() { Id = it.Key.Id, Name = it.Key.Name };
                await _eventsNotifier.ContentTypeGroupDelete(it.Value, [tmp]);
            }
        }

        /// <inheritdoc />
        public async Task<GXContentTypeGroup[]> ListAsync(
            ListContentTypeGroups? request,
            ListContentTypeGroupsResponse? response,
            CancellationToken cancellationToken)
        {
            if (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.ContentType) &&
                !User.HasScope(GXContentTypeGroupPolicies.View))
            {
                throw new UnauthorizedAccessException();
            }

            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the groups.
                arg = GXSelectArgs.SelectAll<GXContentTypeGroup>();
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetContentTypeGroupsByUser(s => "*", userId, null);
            }
            if (request != null)
            {
                //If content type groups are filtered by user.
                if (request.Filter?.UserGroups != null)
                {
                    var ug = request.Filter.UserGroups.FirstOrDefault();
                    if (ug?.Users != null && ug.Users.Any())
                    {
                        var user2 = ug.Users.FirstOrDefault();
                        if (user2 != null)
                        {
                            arg.Joins.AddLeftJoin<GXUserGroupContentTypeGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
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
                    arg.Where.And<GXContentTypeGroup>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXContentTypeGroup>(w => request.Included.Contains(w.Id));
                }
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXContentTypeGroup>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXContentTypeGroup>(q => q.CreationTime);
                arg.Descending = true;
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXContentTypeGroup>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            GXContentTypeGroup[] groups = (await _host.Connection.SelectAsync<GXContentTypeGroup>(arg)).ToArray();
            if (response != null)
            {
                response.ContentTypeGroups = groups;
                if (response.Count == 0)
                {
                    response.Count = groups.Length;
                }
            }
            return groups;
        }

        /// <inheritdoc />
        public async Task<GXContentTypeGroup> ReadAsync(Guid id)
        {
            if (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.ContentType) &&
                !User.HasScope(GXContentTypeGroupPolicies.View))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg;
            if (User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the groups.
                arg = GXSelectArgs.SelectAll<GXContentTypeGroup>(w => w.Id == id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetContentTypeGroupsByUser(s => "*", userId, id);
            }
            arg.Columns.Add<GXUser>(s => new { s.Id, s.UserName });
            arg.Joins.AddInnerJoin<GXContentTypeGroup, GXUser>(j => j.Creator, j => j.Id);

            arg.Columns.Add<GXContentTypeGroup>();
            arg.Columns.Exclude<GXContentTypeGroup>(e => e.ContentTypes);
            arg.Distinct = true;
            var group = (await _host.Connection.SingleOrDefaultAsync<GXContentTypeGroup>(arg));
            if (group == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            ////////////////////////////////////////////////////
            //Get blocks that belong for this block group.
            arg = GXSelectArgs.Select<GXContentType>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            //It might be that there are no blocks in the group. For that reason left join is used.
            arg.Joins.AddLeftJoin<GXContentTypeGroupContentType, GXContentType>(j => j.ContentTypeId, j => j.Id);
            arg.Where.And<GXContentTypeGroupContentType>(q => q.Removed == null && q.ContentTypeGroupId == id);
            group.ContentTypes = await _host.Connection.SelectAsync<GXContentType>(arg);

            ////////////////////////////////////////////////////
            //Get user groups that belong for this block group.
            arg = GXSelectArgs.Select<GXUserGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            //It might be that there are no blocks in the group. For that reason left join is used.
            arg.Joins.AddInnerJoin<GXUserGroupContentTypeGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
            arg.Where.And<GXUserGroupContentTypeGroup>(q => q.Removed == null && q.ContentTypeGroupId == id);
            group.UserGroups = await _host.Connection.SelectAsync<GXUserGroup>(arg);
            //Get creator with own query. It's faster for some DBs.
            arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName }, q => q.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXContentTypeGroup, GXUser>(s => s.Creator, o => o.Id);
            arg.Where.And<GXContentTypeGroup>(w => w.Id == id);
            group.Creator = await _host.Connection.SingleOrDefaultAsync<GXUser>(arg);
            return group;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            IEnumerable<GXContentTypeGroup> ContentTypeGroups,
            Expression<Func<GXContentTypeGroup, object?>>? columns)
        {
            if (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.ContentType) &&
                !User.HasScope(GXContentTypeGroupPolicies.Edit))
            {
                throw new UnauthorizedAccessException();
            }
            DateTime now = DateTime.Now;
            GXUser creator = new GXUser() { Id = ServerHelpers.GetUserId(User) };
            List<Guid> list = new List<Guid>();
            Dictionary<GXContentTypeGroup, List<string>> updates = new Dictionary<GXContentTypeGroup, List<string>>();
            foreach (GXContentTypeGroup it in ContentTypeGroups)
            {
                if (string.IsNullOrEmpty(it.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                if (it.UserGroups == null || !it.UserGroups.Any())
                {
                    //Get default user groups.
                    it.UserGroups = await _userGroupRepository.GetDefaultUserGroups(
                        ServerHelpers.GetUserId(User));
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
                    args.Exclude<GXContentTypeGroup>(e => new { e.UserGroups });
                    _host.Connection.Insert(args);
                    list.Add(it.Id);
                    AddContentTypeGroupToUserGroups(it.Id, it.UserGroups.Select(s => s.Id).ToArray());
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXContentTypeGroup>(q => q.ConcurrencyStamp, where => where.Id == it.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    it.Updated = now;
                    it.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                    args.Exclude<GXContentTypeGroup>(q => new
                    {
                        q.UserGroups,
                        q.CreationTime,
                        q.ContentTypes
                    });
                    if (!User.IsInRole(GXRoles.Admin) ||
                        it.Creator == null ||
                        string.IsNullOrEmpty(it.Creator.Id))
                    {
                        //Only admin can update the creator.
                        args.Exclude<GXContentTypeGroup>(q => q.Creator);
                    }

                    _host.Connection.Update(args);
                    //Map user group to ContentType group.
                    List<GXUserGroup> list2 = await GetJoinedUserGroups(it.Id);
                    List<Guid> groups = list2.Select(s => s.Id).ToList();
                    Guid[] tmp = it.UserGroups.Select(s => s.Id).ToArray();
                    Guid[] removed = groups.Except(tmp).ToArray();
                    Guid[] added = tmp.Except(groups).ToArray();
                    if (added.Length != 0)
                    {
                        AddContentTypeGroupToUserGroups(it.Id, added);
                    }
                    if (removed.Length != 0)
                    {
                        RemoveContentTypeGroupFromUserGroups(it.Id, removed);
                    }
                    //Map blocks to ContentType group.
                    //Only adming can add blocks that are visible to all users.
                    if (it.ContentTypes != null && (User.IsInRole(GXRoles.Admin) || it.ContentTypes.Any()))
                    {
                        List<GXContentType> list3 = await GetJoinedContentTypes(it.Id);
                        List<Guid> groups2 = list3.Select(s => s.Id).ToList();
                        tmp = it.ContentTypes.Select(s => s.Id).ToArray();
                        removed = groups2.Except(tmp).ToArray();
                        added = tmp.Except(groups2).ToArray();
                        if (added.Length != 0)
                        {
                            AddContentTypesToContentTypeGroup(it.Id, added);
                        }
                        if (removed.Length != 0)
                        {
                            RemoveContentTypesFromContentTypeGroup(it.Id, removed);
                        }
                    }
                }
                updates[it] = await GetUsersAsync(it.Id);
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.ContentTypeGroupUpdate(it.Value, [it.Key]);
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map block group to user groups.
        /// </summary>
        /// <param name="ContentTypeGroupId">ContentType group ID.</param>
        /// <param name="groups">Group IDs of the block groups where the block is added.</param>
        public void AddContentTypeGroupToUserGroups(Guid ContentTypeGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupContentTypeGroup> list = new List<GXUserGroupContentTypeGroup>();
            foreach (var ug in groups)
            {
                list.Add(new GXUserGroupContentTypeGroup()
                {
                    ContentTypeGroupId = ContentTypeGroupId,
                    UserGroupId = ug,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between block group and user groups.
        /// </summary>
        /// <param name="ContentTypeGroupId">ContentType group ID.</param>
        /// <param name="groups">Group IDs of the block groups where the block is removed.</param>
        public void RemoveContentTypeGroupFromUserGroups(Guid ContentTypeGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupContentTypeGroup>();
            foreach (var ug in groups)
            {
                args.Where.Or<GXUserGroupContentTypeGroup>(w => w.UserGroupId == ug &&
                    w.ContentTypeGroupId == ContentTypeGroupId);
            }
            _host.Connection.Delete(args);
        }

        /// <summary>
        /// Map blocks to block group.
        /// </summary>
        /// <param name="ContentTypeGroupId">ContentType group ID.</param>
        /// <param name="groups">Group IDs of the block groups where the block is added.</param>
        public void AddContentTypesToContentTypeGroup(Guid ContentTypeGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXContentTypeGroupContentType> list = new List<GXContentTypeGroupContentType>();
            foreach (var ug in groups)
            {
                list.Add(new GXContentTypeGroupContentType()
                {
                    ContentTypeGroupId = ContentTypeGroupId,
                    ContentTypeId = ug,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between blocks and block group.
        /// </summary>
        /// <param name="ContentTypeGroupId">ContentType group ID.</param>
        /// <param name="groups">Group IDs of the block groups where the block is removed.</param>
        public void RemoveContentTypesFromContentTypeGroup(Guid ContentTypeGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXContentTypeGroupContentType>();
            foreach (var it in groups)
            {
                args.Where.Or<GXContentTypeGroupContentType>(w => w.ContentTypeId == it &&
                    w.ContentTypeGroupId == ContentTypeGroupId);
            }
            _host.Connection.Delete(args);
        }
    }
}