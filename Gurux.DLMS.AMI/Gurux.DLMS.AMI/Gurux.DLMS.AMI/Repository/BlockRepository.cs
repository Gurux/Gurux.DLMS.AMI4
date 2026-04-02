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
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Client.Shared;
using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Client.Shared.Enums;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class BlockRepository : IBlockRepository
    {
        private readonly ClaimsPrincipal? User;
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IBlockGroupRepository _blockGroupRepository;
        private readonly GXPerformanceSettings _performanceSettings;

        /// <summary>
        /// Constructor.
        /// </summary>
        public BlockRepository(IGXHost host,
            IGXAmiContextAccessor contextAccessor,
            IUserRepository userRepository,
            IServiceProvider serviceProvider,
            IBlockGroupRepository blockGroupRepository,
            IGXEventsNotifier eventsNotifier,
            GXPerformanceSettings performanceSettings)
        {
            User = contextAccessor.User;
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
            _serviceProvider = serviceProvider;
            _blockGroupRepository = blockGroupRepository;
            _performanceSettings = performanceSettings;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(Guid? blockId)
        {
            GXSelectArgs args = GXQuery.GetUsersByBlock(s => s.Id,
                ServerHelpers.GetUserId(User), blockId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(IEnumerable<Guid>? blockIds)
        {
            GXSelectArgs args = GXQuery.GetUsersByBlocks(s => s.Id,
                ServerHelpers.GetUserId(User), blockIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(IEnumerable<Guid> blocks, bool delete)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.BlockManager) &&
                !User.IsInRole(GXRoles.BlockManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXBlock>(a => a.Id, q => blocks.Contains(q.Id));
            List<GXBlock> list = _host.Connection.Select<GXBlock>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXBlock, List<string>> updates = new();
            foreach (GXBlock it in list)
            {
                it.Removed = now;
                List<string> users = await GetUsersAsync(it.Id);
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXBlock>(it.Id));
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
                    it.Value, TargetType.Block, NotificationAction.Remove);
                if (users == null)
                {
                    break;
                }
                GXBlock tmp = new GXBlock() { Id = it.Key.Id };
                await _eventsNotifier.BlockDelete(it.Value, [tmp]);
            }
        }

        /// <inheritdoc />
        public async Task<GXBlock[]> ListAsync(
            ListBlocks? request,
            ListBlocksResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers &&
                User?.IsInRole(GXRoles.Admin) == true)
            {
                //Admin can see all the blocks.
                arg = GXSelectArgs.SelectAll<GXBlock>();
                if (request?.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXBlock>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXBlock>(w => request.Included.Contains(w.Id));
                }
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(User, false);
                if (userId == null)
                {
                    //Return a list of blocks that anonymous users can access.
                    return [];
                }
                arg = GXQuery.GetBlocksByUser(s => "*", userId, null, request?.Exclude,
                    request?.Included);
            }            
            if (request?.Filter != null)
            {
                //User is already filtered. It can be removed.
                GXUser? orig = request.Filter.User;
                try
                {
                    request.Filter.User = null;
                    arg.Where.FilterBy(request.Filter);
                }
                finally
                {
                    request.Filter.User = orig;
                }
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXBlock>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXBlock>(q => q.CreationTime);
                arg.Descending = true;
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXBlock>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            GXBlock[] blocks = (await _host.Connection.SelectAsync<GXBlock>(arg)).ToArray();
            foreach (var block in blocks)
            {
                switch (block.BlockType)
                {
                    case BlockType.Html:
                        break;
                    case BlockType.Component:
                        {
                            arg = GXSelectArgs.SelectAll<GXComponentView>();
                            arg.Joins.AddInnerJoin<GXComponentView, GXBlock>(j => j.Id, j => j.ComponentView);
                            arg.Where.And<GXBlock>(w => w.Id == block.Id);
                            block.ComponentView = await _host.Connection.SingleOrDefaultAsync<GXComponentView>(arg);
                        }
                        break;
                    case BlockType.Script:
                        arg = GXSelectArgs.Select<GXScriptMethod>(s => new { s.Id, s.Name });
                        arg.Columns.Add<GXScript>(s => new { s.Id, s.Name });
                        arg.Joins.AddInnerJoin<GXScriptMethod, GXBlock>(j => j.Id, j => j.ScriptMethod);
                        arg.Joins.AddInnerJoin<GXScriptMethod, GXScript>(j => j.Script, j => j.Id);
                        arg.Where.And<GXBlock>(w => w.Id == block.Id);
                        block.ScriptMethod = await _host.Connection.SingleOrDefaultAsync<GXScriptMethod>(arg);
                        break;
                }
            }
            if (request?.Filter?.Active.GetValueOrDefault() == true)
            {
                //TODO: Fix active issue.
                blocks = blocks.Where(w => w.Active == true).ToArray();
            }

            if (response != null)
            {
                response.Blocks = blocks;
                if (response.Count == 0)
                {
                    response.Count = blocks.Length;
                }
            }
            return blocks;
        }

        /// <inheritdoc />
        public async Task<GXBlock> ReadAsync(Guid id)
        {
            GXSelectArgs arg;
            if (User?.IsInRole(GXRoles.Admin) == true)
            {
                //Admin can see all the blocks.
                arg = GXSelectArgs.SelectAll<GXBlock>(w => w.Id == id);
                arg.Joins.AddLeftJoin<GXBlock, GXBlockGroupBlock>(x => x.Id, y => y.BlockId);
                arg.Joins.AddLeftJoin<GXBlockGroupBlock, GXBlockGroup>(j => j.BlockGroupId, j => j.Id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetBlocksByUser(s => "*", userId, id);
                arg.Joins.AddInnerJoin<GXBlockGroupBlock, GXBlockGroup>(j => j.BlockGroupId, j => j.Id);
            }
            arg.Columns.Add<GXBlockGroup>();
            arg.Columns.Exclude<GXBlockGroup>(e => e.Blocks);
            arg.Distinct = true;
            GXBlock block = await _host.Connection.SingleOrDefaultAsync<GXBlock>(arg);
            if (block == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            if (block.BlockType == BlockType.Script)
            {
                //Get script and method.
                arg = GXSelectArgs.SelectAll<GXScriptMethod>();
                arg.Columns.Add<GXScript>();
                arg.Columns.Exclude<GXScript>(e => e.Methods);
                arg.Joins.AddInnerJoin<GXScriptMethod, GXBlock>(y => y.Id, x => x.ScriptMethod);
                arg.Joins.AddInnerJoin<GXScriptMethod, GXScript>(y => y.Script, x => x.Id);
                arg.Where.And<GXBlock>(w => w.Id == block.Id);
                block.ScriptMethod = await _host.Connection.SingleOrDefaultAsync<GXScriptMethod>(arg);
            }
            else if (block.BlockType == BlockType.Component)
            {
                //Get component view.
                arg = GXSelectArgs.SelectAll<GXComponentView>();
                arg.Joins.AddInnerJoin<GXComponentView, GXBlock>(y => y.Id, x => x.ComponentView);
                arg.Where.And<GXBlock>(w => w.Id == block.Id);
                block.ComponentView = await _host.Connection.SingleOrDefaultAsync<GXComponentView>(arg);
            }
            //Get creator with own query. It's faster for some DBs.
            arg = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.UserName }, q => q.Removed == null);
            arg.Distinct = true;
            arg.Joins.AddInnerJoin<GXBlock, GXUser>(s => s.Creator, o => o.Id);
            arg.Where.And<GXBlock>(w => w.Id == id);
            block.Creator = await _host.Connection.SingleOrDefaultAsync<GXUser>(arg);
            return block;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            IEnumerable<GXBlock> blocks,
            Expression<Func<GXBlock, object?>>? columns)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.BlockManager) &&
                !User.IsInRole(GXRoles.BlockManager)))
            {
                throw new UnauthorizedAccessException();
            }
            DateTime now = DateTime.Now;
            GXUser creator = new GXUser() { Id = ServerHelpers.GetUserId(User) };
            List<Guid> list = new();
            Dictionary<GXBlock, List<string>> updates = new();
            foreach (GXBlock block in blocks)
            {
                if (string.IsNullOrEmpty(block.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                if (block.BlockGroups == null || !block.BlockGroups.Any())
                {
                    ListBlockGroups request = new ListBlockGroups()
                    {
                        Filter = new GXBlockGroup() { Default = true }
                    };
                    block.BlockGroups = new List<GXBlockGroup>();
                    block.BlockGroups.AddRange(await _blockGroupRepository.ListAsync(request, null, CancellationToken.None));
                    if (!block.BlockGroups.Any())
                    {
                        throw new ArgumentException(Properties.Resources.TargetMustBelongToOneGroup);
                    }
                }
                if (block.BlockType == BlockType.Html)
                {
                    block.ComponentView = null;
                }
                else if (block.BlockType == BlockType.Component)
                {
                    if (block.ComponentView == null)
                    {
                        throw new ArgumentException("Component is not selected.");
                    }
                }
                else if (block.BlockType == BlockType.Script)
                {
                    if (block.ScriptMethod == null)
                    {
                        throw new ArgumentException("Script is not selected.");
                    }
                }
                else
                {
                    throw new ArgumentException("Invalid block type.");
                }
                if (block.Id == Guid.Empty)
                {
                    block.Creator = creator;
                    block.CreationTime = now;
                    GXInsertArgs args = GXInsertArgs.Insert(block);
                    args.Exclude<GXBlock>(q => new
                    {
                        q.BlockGroups,
                    });
                    _host.Connection.Insert(args);
                    list.Add(block.Id);
                    AddBlockToBlockGroups(block.Id, block.BlockGroups);
                    updates[block] = await GetUsersAsync(block.Id);
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXBlock>(q => q.ConcurrencyStamp, where => where.Id == block.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != block.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    block.Updated = now;
                    block.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(block, columns);
                    args.Exclude<GXBlock>(q => new
                    {
                        q.CreationTime,
                        q.BlockGroups,
                    });
                    if (!User.IsInRole(GXRoles.Admin) ||
                        block.Creator == null ||
                        string.IsNullOrEmpty(block.Creator.Id))
                    {
                        //Only admin can update the creator.
                        args.Exclude<GXBlock>(q => q.Creator);
                    }
                    _host.Connection.Update(args);
                    //Map block groups to block.
                    List<GXBlockGroup> blockGroups;
                    using (IServiceScope scope = _serviceProvider.CreateScope())
                    {
                        IBlockGroupRepository blockGroupRepository = scope.ServiceProvider.GetRequiredService<IBlockGroupRepository>();
                        blockGroups = await blockGroupRepository.GetJoinedBlockGroups(block.Id);
                    }
                    var comparer = new UniqueComparer<GXBlockGroup, Guid>();
                    List<GXBlockGroup> removedBlockGroups = blockGroups.Except(block.BlockGroups, comparer).ToList();
                    List<GXBlockGroup> addedBlockGroups = block.BlockGroups.Except(blockGroups, comparer).ToList();
                    if (removedBlockGroups.Any())
                    {
                        RemoveBlocksFromBlockGroup(block.Id, removedBlockGroups);
                    }
                    if (addedBlockGroups.Any())
                    {
                        AddBlockToBlockGroups(block.Id, addedBlockGroups);
                    }
                    updates[block] = await GetUsersAsync(block.Id);
                }
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.BlockUpdate(it.Value, new GXBlock[] { it.Key });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map block group to user groups.
        /// </summary>
        /// <param name="blockId">Block ID.</param>
        /// <param name="groups">Group IDs of the block groups where the block is added.</param>
        public void AddBlockToBlockGroups(Guid blockId, IEnumerable<GXBlockGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXBlockGroupBlock> list = new();
            foreach (GXBlockGroup it in groups)
            {
                list.Add(new GXBlockGroupBlock()
                {
                    BlockId = blockId,
                    BlockGroupId = it.Id,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between block group and block.
        /// </summary>
        /// <param name="blockId">Block ID.</param>
        /// <param name="groups">Group IDs of the block groups where the block is removed.</param>
        public void RemoveBlocksFromBlockGroup(Guid blockId, IEnumerable<GXBlockGroup> groups)
        {
            foreach (GXBlockGroup it in groups)
            {
                _host.Connection.Delete(GXDeleteArgs.Delete<GXBlockGroupBlock>(w => w.BlockId == blockId && w.BlockGroupId == it.Id));
            }
        }

        /// <inheritdoc />
        public async Task CloseAsync(IEnumerable<Guid> blocks)
        {
            string? userId = ServerHelpers.GetUserId(User);
            if (userId == null)
            {
                //User doesn't have access rights for this block.
                throw new UnauthorizedAccessException();
            }
            Dictionary<Guid, List<string>> updates = new();
            DateTime now = DateTime.Now;
            List<GXUserBlockSettings> inserted = new();
            List<GXUserBlockSettings> updated = new();
            foreach (Guid it in blocks)
            {
                GXSelectArgs args = GXSelectArgs.SelectAll<GXUserBlockSettings>(where => where.BlockId == it && where.UserId == userId);
                GXUserBlockSettings s = await _host.Connection.SingleOrDefaultAsync<GXUserBlockSettings>(args);
                if (s == null)
                {
                    s = new GXUserBlockSettings() { UserId = userId, BlockId = it, Closed = now };
                    inserted.Add(s);
                }
                else
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.Delete<GXUserBlockSettings>(where => where.BlockId == it && where.UserId == userId));
                    s = new GXUserBlockSettings() { UserId = userId, BlockId = it, Closed = now };
                    updated.Add(s);
                }
                List<string> users = await GetUsersAsync(it);
                updates[it] = users;
            }
            if (updated.Count != 0)
            {
                await _host.Connection.InsertAsync(GXInsertArgs.InsertRange(updated));
                foreach (var it in updates)
                {
                    await _eventsNotifier.BlockClose(it.Value, [new GXBlock() { Id = it.Key }]);
                }
            }
            if (inserted.Count != 0)
            {
                await _host.Connection.InsertAsync(GXInsertArgs.InsertRange(inserted));
                foreach (var it in updates)
                {
                    await _eventsNotifier.BlockClose(it.Value, [new GXBlock() { Id = it.Key }]);
                }
            }
        }
    }
}
