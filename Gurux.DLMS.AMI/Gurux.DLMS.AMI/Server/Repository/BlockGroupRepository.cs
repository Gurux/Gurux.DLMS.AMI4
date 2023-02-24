///
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
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class BlockGroupRepository : IBlockGroupRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly IUserRepository _userRepository;



        /// <summary>
        /// Constructor.
        /// </summary>
        public BlockGroupRepository(
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
        public async Task<List<GXBlockGroup>> GetJoinedBlockGroups(ClaimsPrincipal user, Guid moduleId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXBlockGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXBlockGroup, GXBlockGroupBlock>(a => a.Id, b => b.BlockGroupId);
            arg.Joins.AddInnerJoin<GXBlockGroupBlock, GXBlock>(a => a.BlockId, b => b.Id);
            arg.Where.And<GXBlock>(where => where.Removed == null && where.Id == moduleId);
            return (await _host.Connection.SelectAsync<GXBlockGroup>(arg));
        }


        private async Task<List<GXUserGroup>> GetJoinedUserGroups(Guid blockGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserGroup>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupBlockGroup>(a => a.Id, b => b.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupBlockGroup, GXBlockGroup>(a => a.BlockGroupId, b => b.Id);
            arg.Where.And<GXBlockGroup>(where => where.Removed == null && where.Id == blockGroupId);
            return (await _host.Connection.SelectAsync<GXUserGroup>(arg));
        }

        private async Task<List<GXBlock>> GetJoinedBlocks(ClaimsPrincipal user, Guid blockGroupId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXBlock>(where => where.Removed == null);
            arg.Joins.AddInnerJoin<GXBlock, GXBlockGroupBlock>(a => a.Id, b => b.BlockId);
            arg.Joins.AddInnerJoin<GXBlockGroupBlock, GXBlockGroup>(a => a.BlockGroupId, b => b.Id);

            arg.Joins.AddInnerJoin<GXBlockGroup, GXUserGroupBlockGroup>(j => j.Id, j => j.BlockGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupBlockGroup, GXUserGroup>(x => x.UserGroupId, y => y.Id);
            arg.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(x => x.Id, y => y.UserGroupId);
            arg.Joins.AddInnerJoin<GXUserGroupUser, GXUser>(x => x.UserId, y => y.Id);
            var id = ServerHelpers.GetUserId(user);
            arg.Where.And<GXUser>(q => q.Removed == null && q.Id == id);
            arg.Where.And<GXBlockGroup>(where => where.Removed == null && where.Id == blockGroupId);
            return (await _host.Connection.SelectAsync<GXBlock>(arg));
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal user, Guid? groupId)
        {
            GXSelectArgs args = GXQuery.GetUsersByBlockGroup(ServerHelpers.GetUserId(user), groupId);
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
            GXSelectArgs args = GXQuery.GetUsersByBlockGroups(ServerHelpers.GetUserId(user), agentIds);
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
            if (User == null || (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.BlockGroupManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXBlockGroup>(a => new { a.Id, a.Name }, q => userGrouprs.Contains(q.Id));
            List<GXBlockGroup> list = _host.Connection.Select<GXBlockGroup>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXBlockGroup, List<string>> updates = new Dictionary<GXBlockGroup, List<string>>();
            foreach (GXBlockGroup it in list)
            {
                it.Removed = now;
                List<string> users = await GetUsersAsync(User, it.Id);
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXBlockGroup>(it.Id));
                }
                else
                {
                    _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
                }
                updates[it] = users;
            }
            foreach (var it in updates)
            {
                GXBlockGroup tmp = new GXBlockGroup() { Id = it.Key.Id, Name = it.Key.Name };
                await _eventsNotifier.BlockGroupDelete(it.Value, new GXBlockGroup[] { tmp });
            }
        }

        /// <inheritdoc />
        public async Task<GXBlockGroup[]> ListAsync(
            ClaimsPrincipal user,
            ListBlockGroups? request,
            ListBlockGroupsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && user.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the groups.
                arg = GXSelectArgs.SelectAll<GXBlockGroup>();
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetBlockGroupsByUser(userId, null);
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
            }
            arg.OrderBy.Add<GXBlockGroup>(q => q.CreationTime);
            arg.Distinct = true;
            arg.Descending = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXBlockGroup>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            GXBlockGroup[] groups = (await _host.Connection.SelectAsync<GXBlockGroup>(arg)).ToArray();
            if (response != null)
            {
                response.BlockGroups = groups;
                if (response.Count == 0)
                {
                    response.Count = groups.Length;
                }
            }
            return groups;
        }

        /// <inheritdoc />
        public async Task<GXBlockGroup> ReadAsync(
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
                arg = GXSelectArgs.SelectAll<GXBlockGroup>(w => w.Id == id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetBlockGroupsByUser(userId, id);
            }
            arg.Columns.Add<GXBlockGroup>();
            arg.Columns.Exclude<GXBlockGroup>(e => e.Blocks);
            arg.Distinct = true;
            GXBlock block = await _host.Connection.SingleOrDefaultAsync<GXBlock>(arg);
            if (block == null)
            {
                throw new ArgumentNullException(Properties.Resources.UnknownTarget);
            }
            var ret = (await _host.Connection.SingleOrDefaultAsync<GXBlockGroup>(arg));
            if (ret == null)
            {
                throw new ArgumentNullException(Properties.Resources.UnknownTarget);
            }
            ////////////////////////////////////////////////////
            //Get blocks that belong for this block group.
            arg = GXSelectArgs.Select<GXBlock>(s => new {s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            //It might be that there are no blocks in the group. For that reason left join is used.
            arg.Joins.AddLeftJoin<GXBlockGroupBlock, GXBlock>(j => j.BlockId, j => j.Id);
            arg.Where.And<GXBlockGroupBlock>(q => q.Removed == null && q.BlockGroupId == id);
            ret.Blocks = await _host.Connection.SelectAsync<GXBlock>(arg);

            ////////////////////////////////////////////////////
            //Get user groups that belong for this block group.
            arg = GXSelectArgs.Select<GXUserGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
            arg.Distinct = true;
            //It might be that there are no blocks in the group. For that reason left join is used.
            arg.Joins.AddInnerJoin<GXUserGroupBlockGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
            arg.Where.And<GXUserGroupBlockGroup>(q => q.Removed == null && q.BlockGroupId == id);
            ret.UserGroups = await _host.Connection.SelectAsync<GXUserGroup>(arg);
            return ret;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal user,
            IEnumerable<GXBlockGroup> BlockGroups,
            Expression<Func<GXBlockGroup, object?>>? columns)
        {
            DateTime now = DateTime.Now;
            List<Guid> list = new List<Guid>();
            Dictionary<GXBlockGroup, List<string>> updates = new Dictionary<GXBlockGroup, List<string>>();
            foreach (GXBlockGroup it in BlockGroups)
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
                if (it.Id == Guid.Empty)
                {
                    it.CreationTime = now;
                    GXInsertArgs args = GXInsertArgs.Insert(it);
                    //User groups must hanlde separetly because users are identified with name and not Guid.
                    args.Exclude<GXBlockGroup>(e => new { e.UserGroups });
                    _host.Connection.Insert(args);
                    list.Add(it.Id);
                    AddBlockGroupToUserGroups(it.Id, it.UserGroups.Select(s => s.Id).ToArray());
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXBlockGroup>(q => q.ConcurrencyStamp, where => where.Id == it.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    it.Updated = now;
                    it.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                    args.Exclude<GXBlockGroup>(q => new { q.UserGroups, q.CreationTime, q.Blocks });
                    _host.Connection.Update(args);
                    //Map user group to Block group.
                    List<GXUserGroup> list2 = await GetJoinedUserGroups(it.Id);
                    List<Guid> groups = list2.Select(s => s.Id).ToList();
                    Guid[] tmp = it.UserGroups.Select(s => s.Id).ToArray();
                    Guid[] removed = groups.Except(tmp).ToArray();
                    Guid[] added = tmp.Except(groups).ToArray();
                    if (added.Length != 0)
                    {
                        AddBlockGroupToUserGroups(it.Id, added);
                    }
                    if (removed.Length != 0)
                    {
                        RemoveBlockGroupFromUserGroups(it.Id, removed);
                    }
                    //Map blocks to Block group.
                    if (it.Blocks != null && it.Blocks.Count != 0)
                    {
                        List<GXBlock> list3 = await GetJoinedBlocks(user, it.Id);
                        List<Guid> groups2 = list3.Select(s => s.Id).ToList();
                        tmp = it.Blocks.Select(s => s.Id).ToArray();
                        removed = groups2.Except(tmp).ToArray();
                        added = tmp.Except(groups2).ToArray();
                        if (added.Length != 0)
                        {
                            AddBlocksToBlockGroup(it.Id, added);
                        }
                        if (removed.Length != 0)
                        {
                            RemoveBlocksFromBlockGroup(it.Id, removed);
                        }
                    }
                }
                updates[it] = await GetUsersAsync(user, it.Id);
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.BlockGroupUpdate(it.Value, new GXBlockGroup[] { it.Key });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map block group to user groups.
        /// </summary>
        /// <param name="blockGroupId">Block group ID.</param>
        /// <param name="groups">Group IDs of the block groups where the block is added.</param>
        public void AddBlockGroupToUserGroups(Guid blockGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXUserGroupBlockGroup> list = new List<GXUserGroupBlockGroup>();
            foreach (var ug in groups)
            {
                list.Add(new GXUserGroupBlockGroup()
                {
                    BlockGroupId = blockGroupId,
                    UserGroupId = ug,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between block group and user groups.
        /// </summary>
        /// <param name="blockGroupId">Block group ID.</param>
        /// <param name="groups">Group IDs of the block groups where the block is removed.</param>
        public void RemoveBlockGroupFromUserGroups(Guid blockGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXUserGroupBlockGroup>();
            foreach (var ug in groups)
            {
                args.Where.Or<GXUserGroupBlockGroup>(w => w.UserGroupId == ug &&
                    w.BlockGroupId == blockGroupId);
            }
            _host.Connection.Delete(args);
        }

        /// <summary>
        /// Map blocks to block group.
        /// </summary>
        /// <param name="blockGroupId">Block group ID.</param>
        /// <param name="groups">Group IDs of the block groups where the block is added.</param>
        public void AddBlocksToBlockGroup(Guid blockGroupId, IEnumerable<Guid> groups)
        {
            DateTime now = DateTime.Now;
            List<GXBlockGroupBlock> list = new List<GXBlockGroupBlock>();
            foreach (var ug in groups)
            {
                list.Add(new GXBlockGroupBlock()
                {
                    BlockGroupId = blockGroupId,
                    BlockId = ug,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between blocks and block group.
        /// </summary>
        /// <param name="blockGroupId">Block group ID.</param>
        /// <param name="groups">Group IDs of the block groups where the block is removed.</param>
        public void RemoveBlocksFromBlockGroup(Guid blockGroupId, IEnumerable<Guid> groups)
        {
            GXDeleteArgs args = GXDeleteArgs.DeleteAll<GXBlockGroupBlock>();
            foreach (var it in groups)
            {
                args.Where.Or<GXBlockGroupBlock>(w => w.BlockId == it &&
                    w.BlockGroupId == blockGroupId);
            }
            _host.Connection.Delete(args);
        }
    }
}