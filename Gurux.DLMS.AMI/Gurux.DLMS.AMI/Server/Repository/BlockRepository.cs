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

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class BlockRepository : IBlockRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IBlockGroupRepository _blockGroupRepository;


        /// <summary>
        /// Constructor.
        /// </summary>
        public BlockRepository(IGXHost host,
            IUserRepository userRepository,
            IServiceProvider serviceProvider,
            IBlockGroupRepository blockGroupRepository,
            IGXEventsNotifier eventsNotifier)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
            _serviceProvider = serviceProvider;
            _blockGroupRepository = blockGroupRepository;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal user, Guid? blockId)
        {
            GXSelectArgs args = GXQuery.GetUsersByBlock(ServerHelpers.GetUserId(user), blockId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal user, IEnumerable<Guid>? blockIds)
        {
            GXSelectArgs args = GXQuery.GetUsersByBlocks(ServerHelpers.GetUserId(user), blockIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal User, 
            IEnumerable<Guid> blocks,
            bool delete)
        {
            if (User == null || (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.BlockManager)))
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
                List<string> users = await GetUsersAsync(User, it.Id);
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
                GXBlock tmp = new GXBlock() { Id = it.Key.Id };
                await _eventsNotifier.BlockDelete(it.Value, new GXBlock[] { tmp });
            }
        }

        /// <inheritdoc />
        public async Task<GXBlock[]> ListAsync(
            ClaimsPrincipal user,
            ListBlocks? request,
            ListBlocksResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && user.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the blocks.
                arg = GXSelectArgs.SelectAll<GXBlock>();
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetBlocksByUser(userId, null);
            }
            if (request != null && request.Filter != null)
            {
                //User is already filtered. It can be removed.
                GXUser? orig = request.Filter.User;
                try
                {
                    request.Filter.User = null;
                    arg.Where.FilterBy(request.Filter);
                    if (request.Exclude != null && request.Exclude.Any())
                    {
                        arg.Where.And<GXBlock>(w => !request.Exclude.Contains(w.Id));
                    }
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
                    case Shared.DTOs.Enums.BlockType.Html:
                        break;
                    case Shared.DTOs.Enums.BlockType.Component:
                        {
                            arg = GXSelectArgs.SelectAll<GXComponentView>();
                            arg.Joins.AddInnerJoin<GXComponentView, GXBlock>(j => j.Id, j => j.ComponentView);
                            arg.Where.And<GXBlock>(w => w.Id == block.Id);
                            block.ComponentView = await _host.Connection.SingleOrDefaultAsync<GXComponentView>(arg);
                        }
                        break;
                    case Shared.DTOs.Enums.BlockType.Script:
                        arg = GXSelectArgs.Select<GXScriptMethod>(s => new { s.Id, s.Name });
                        arg.Columns.Add<GXScript>(s => new { s.Id, s.Name });
                        arg.Joins.AddInnerJoin<GXScriptMethod, GXBlock>(j => j.Id, j => j.ScriptMethod);
                        arg.Joins.AddInnerJoin<GXScriptMethod, GXScript>(j => j.Script, j => j.Id);
                        arg.Where.And<GXBlock>(w => w.Id == block.Id);
                        block.ScriptMethod = await _host.Connection.SingleOrDefaultAsync<GXScriptMethod>(arg);
                        break;
                }
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
        public async Task<GXBlock> ReadAsync(
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
                //Admin can see all the blocks.
                arg = GXSelectArgs.SelectAll<GXBlock>(w => w.Id == id);
                arg.Joins.AddLeftJoin<GXBlock, GXBlockGroupBlock>(x => x.Id, y => y.BlockId);
                arg.Joins.AddLeftJoin<GXBlockGroupBlock, GXBlockGroup>(j => j.BlockGroupId, j => j.Id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetBlocksByUser(userId, id);
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
            if (block.BlockType == Shared.DTOs.Enums.BlockType.Script)
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
            else if (block.BlockType == Shared.DTOs.Enums.BlockType.Component)
            {
                //Get component view.
                arg = GXSelectArgs.SelectAll<GXComponentView>();
                arg.Joins.AddInnerJoin<GXComponentView, GXBlock>(y => y.Id, x => x.ComponentView);
                arg.Where.And<GXBlock>(w => w.Id == block.Id);
                block.ComponentView = await _host.Connection.SingleOrDefaultAsync<GXComponentView>(arg);
            }
            //Get localized strings.
            arg = GXSelectArgs.Select<GXLanguage>(c => new { c.Id });
            arg.Columns.Add<GXLocalizedResource>();
            arg.Columns.Exclude<GXLocalizedResource>(e => e.Language);
            arg.Joins.AddInnerJoin<GXLocalizedResource, GXLanguage>(y => y.Language, x => x.Id);
            arg.Where.And<GXLocalizedResource>(w => w.Block == block);
            block.Languages = (await _host.Connection.SelectAsync<GXLanguage>(arg)).ToArray();
            return block;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal user, 
            IEnumerable<GXBlock> blocks,
            Expression<Func<GXBlock, object?>>? columns)
        {
            DateTime now = DateTime.Now;
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
                    block.BlockGroups.AddRange(await _blockGroupRepository.ListAsync(user, request, null, CancellationToken.None));
                    if (!block.BlockGroups.Any())
                    {
                        throw new ArgumentException(Properties.Resources.TargetMustBelongToOneGroup);
                    }
                }
                if (block.BlockType == Shared.DTOs.Enums.BlockType.Html)
                {
                    block.ComponentView = null;
                }
                else if (block.BlockType == Shared.DTOs.Enums.BlockType.Component)
                {
                    if (block.ComponentView == null)
                    {
                        throw new ArgumentException("Component is not selected.");
                    }
                }
                else if (block.BlockType == Shared.DTOs.Enums.BlockType.Script)
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
                //Update resource language parent.
                if (block.Languages != null)
                {
                    foreach (var language in block.Languages)
                    {
                        if (language.Resources != null)
                        {
                            foreach (var it in language.Resources)
                            {
                                it.Language = language;
                            }
                        }
                    }
                }

                if (block.Id == Guid.Empty)
                {
                    block.CreationTime = now;
                    GXInsertArgs args = GXInsertArgs.Insert(block);
                    args.Exclude<GXBlock>(q => new {q.BlockGroups, block.Resources });
                    _host.Connection.Insert(args);
                    list.Add(block.Id);
                    AddBlockToBlockGroups(block.Id, block.BlockGroups);
                    await AddLocalizationStringsToBlockAsync(block, block.Resources);
                    updates[block] = await GetUsersAsync(user, block.Id);
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
                    args.Exclude<GXBlock>(q => new { q.CreationTime, q.BlockGroups, q.Resources });
                    _host.Connection.Update(args);
                    //Map block groups to block.
                    List<GXBlockGroup> blockGroups;
                    using (IServiceScope scope = _serviceProvider.CreateScope())
                    {
                        IBlockGroupRepository blockGroupRepository = scope.ServiceProvider.GetRequiredService<IBlockGroupRepository>();
                        blockGroups = await blockGroupRepository.GetJoinedBlockGroups(user, block.Id);
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
                    updates[block] = await GetUsersAsync(user, block.Id);

                    //Add localized block strings.
                    GXSelectArgs l = GXSelectArgs.SelectAll<GXLocalizedResource>(w => w.Block == block);
                    List<GXLocalizedResource> resources = await _host.Connection.SelectAsync<GXLocalizedResource>(l);
                    List<GXLocalizedResource> tmp;
                    if (block.Languages != null)
                    {
                        tmp = block.Languages.SelectMany(s => s.Resources).ToList();
                    }
                    else
                    {
                        tmp = new List<GXLocalizedResource>();
                    }
                    var comparer2 = new LocalizationHashComparer();
                    List<GXLocalizedResource> removedResources = resources.Except(tmp, comparer2).ToList();
                    List<GXLocalizedResource> addedResources = tmp.Except(resources, comparer2).ToList();
                    if (removedResources.Any())
                    {
                        await RemoveLocalizationStringsFromBlockAsync(removedResources);
                    }
                    if (addedResources.Any())
                    {
                        await AddLocalizationStringsToBlockAsync(block, addedResources);
                    }
                    //Get updated resource strings.                   
                    var comparer3 = new LocalizationStringComparer();
                    List<GXLocalizedResource> updatedResources = resources.Except(tmp, comparer3).ToList();
                    if (updatedResources.Any())
                    {
                        foreach (var it in updatedResources)
                        {
                            foreach (var it2 in tmp)
                            {
                                if (it.Hash == it2.Hash)
                                {
                                    it.Value = it2.Value;
                                    break;
                                }
                            }
                        }
                        await UpdateLocalizationStrings(updatedResources);
                    }
                }
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.BlockUpdate(it.Value, new GXBlock[] { it.Key });
            }
            return list.ToArray();
        }

        /// <summary>
        /// Add localization strings for the block.
        /// </summary>
        private async Task AddLocalizationStringsToBlockAsync(GXBlock block, IEnumerable<GXLocalizedResource>? resources)
        {
            if (resources != null)
            {
                foreach (GXLocalizedResource it in resources)
                {
                    //Update hash.
                    if (block.Title != null && it.Hash == ServerHelpers.GetHashCode(block.Title))
                    {
                        it.Hash = ServerHelpers.GetHashCode(block.Title);
                    }
                    else if (block.Body != null && it.Hash == ServerHelpers.GetHashCode(block.Body))
                    {
                        it.Hash = ServerHelpers.GetHashCode(block.Body);
                    }
                    else
                    {
                        throw new ArgumentException("Invalid hash.");
                    }
                    it.Block = block;
                }
                await _host.Connection.InsertAsync(GXInsertArgs.InsertRange(resources));
            }
        }

        /// <summary>
        /// Remove localization strings from the block.
        /// </summary>
        private async Task RemoveLocalizationStringsFromBlockAsync(IEnumerable<GXLocalizedResource>? resources)
        {
            if (resources != null)
            {
                foreach(var it in resources)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXLocalizedResource>(it.Id));
                }
            }
        }

        //Update block localization strings.
        private async Task UpdateLocalizationStrings(IEnumerable<GXLocalizedResource> resources)
        {
            if (resources != null)
            {
                foreach (GXLocalizedResource it in resources)
                {
                    await _host.Connection.UpdateAsync(GXUpdateArgs.Update(it, c => new { c.Value, c.Id }));
                }
            }
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
        public async Task CloseAsync(ClaimsPrincipal user, IEnumerable<Guid> blocks)
        {
            string? userId = ServerHelpers.GetUserId(user);
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
                List<string> users = await GetUsersAsync(user, it);
                updates[it] = users;
            }
            if (updated.Count != 0)
            {
                await _host.Connection.InsertAsync(GXInsertArgs.InsertRange(updated));
                foreach (var it in updates)
                {
                    await _eventsNotifier.BlockClose(it.Value, new GXBlock[] { new GXBlock() { Id = it.Key } });
                }
            }
            if (inserted.Count != 0)
            {
                await _host.Connection.InsertAsync(GXInsertArgs.InsertRange(inserted));
                foreach (var it in updates)
                {
                    await _eventsNotifier.BlockClose(it.Value, new GXBlock[] { new GXBlock() { Id = it.Key } });
                }
            }
        }
    }
}
