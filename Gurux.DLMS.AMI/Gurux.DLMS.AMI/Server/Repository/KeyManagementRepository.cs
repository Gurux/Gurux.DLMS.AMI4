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
using Microsoft.CodeAnalysis;
using System.Linq.Expressions;
using System.Diagnostics;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Services;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Device;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class KeyManagementRepository : IKeyManagementRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IKeyManagementGroupRepository _deviceKeyGroupRepository;
        private readonly IAmiCryproService _cryproService;

        /// <summary>
        /// Constructor.
        /// </summary>
        public KeyManagementRepository(IGXHost host,
            IUserRepository userRepository,
            IServiceProvider serviceProvider,
            IKeyManagementGroupRepository deviceKeyGroupRepository,
            IGXEventsNotifier eventsNotifier,
            IAmiCryproService cryproService)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
            _serviceProvider = serviceProvider;
            _deviceKeyGroupRepository = deviceKeyGroupRepository;
            _cryproService = cryproService;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal User, Guid keyId)
        {
            GXSelectArgs args = GXQuery.GetUsersByKeyManagement(ServerHelpers.GetUserId(User), keyId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal User, IEnumerable<Guid>? keyIds)
        {
            GXSelectArgs args = GXQuery.GetUsersByKeyManagements(ServerHelpers.GetUserId(User), keyIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (User != null && User.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin }));
            }
            return ret;
        }


        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal User,
            IEnumerable<Guid> keys,
            bool delete)
        {
            if (keys != null && keys.Any())
            {
                if (User == null || (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.KeyManagementManager)))
                {
                    throw new UnauthorizedAccessException();
                }
                GXSelectArgs arg = GXSelectArgs.Select<GXKeyManagement>(a => a.Id, q => keys.Contains(q.Id));
                List<GXKeyManagement> list = await _host.Connection.SelectAsync<GXKeyManagement>(arg);
                DateTime now = DateTime.Now;
                Dictionary<GXKeyManagement, List<string>> updates = new();
                foreach (GXKeyManagement key in list)
                {
                    key.Removed = now;
                    List<string> users = await GetUsersAsync(User, key.Id);
                    if (delete)
                    {
                        await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXKeyManagement>(key.Id));
                    }
                    else
                    {
                        _host.Connection.Update(GXUpdateArgs.Update(key, q => q.Removed));
                    }
                    updates[key] = users;
                }
                foreach (var it in updates)
                {
                    GXKeyManagement tmp = new GXKeyManagement() { Id = it.Key.Id };
                    await _eventsNotifier.KeyManagementDelete(it.Value, new GXKeyManagement[] { tmp });
                }
                if (!delete)
                {
                    List<GXKeyManagementLog> logs = new List<GXKeyManagementLog>();
                    foreach (var it in updates.Keys)
                    {
                        logs.Add(new GXKeyManagementLog(TraceLevel.Info)
                        {
                            CreationTime = DateTime.Now,
                            KeyManagement = it,
                            Message = Properties.Resources.KeyManagementRemoved
                        });
                    }
                    using (IServiceScope scope = _serviceProvider.CreateScope())
                    {
                        var deviceKeyLogRepository = scope.ServiceProvider.GetRequiredService<IKeyManagementLogRepository>();
                        await deviceKeyLogRepository.AddAsync(User, logs);
                    }
                }
            }
        }

        /// <inheritdoc />
        public async Task<GXKeyManagement[]> ListAsync(
            ClaimsPrincipal user,
            ListKeyManagements? request,
            ListKeyManagementsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && user.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the keys.
                arg = GXSelectArgs.SelectAll<GXKeyManagement>();
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetKeyManagementsByUser(userId, null);
            }
            bool selectItemsBySystemTitle = false;
            if (request != null && request.Filter != null)
            {
                if (request.Filter.Device != null && request.Filter.Device.Id != Guid.Empty)
                {
                    //If user want to filter devices by ID.
                    Guid id = request.Filter.Device.Id;
                    arg.Joins.AddInnerJoin<GXKeyManagement, GXDevice>(j => j.Device, j => j.Id);
                    arg.Where.And<GXDevice>(w => w.Id == id);
                    request.Filter.Device = null;
                }
                if (!string.IsNullOrEmpty(request.Filter.SystemTitle))
                {
                    selectItemsBySystemTitle = true;
                    //Return key management items where system title is set ot it's null.
                    string st = request.Filter.SystemTitle;
                    arg.Where.And<GXKeyManagement>(w => w.SystemTitle == st || w.SystemTitle == "" ||
                     w.SystemTitle == null);
                    request.Filter.SystemTitle = null;
                }
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXKeyManagement>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXKeyManagement>(w => request.Included.Contains(w.Id));
                }
            }
            if (request?.Select != null && request.Select.Contains("Device"))
            {
                //Get device basic information.
                arg.Joins.AddLeftJoin<GXKeyManagement, GXDevice>(j => j.Device, j => j.Id);
                arg.Columns.Add<GXDevice>(s => new { s.Id, s.Name });
                arg.Columns.Exclude<GXDevice>(e => e.Keys);
            }

            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXKeyManagement>(q => GXSql.DistinctCount(q.Id));
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
                arg.OrderBy.Add<GXKeyManagement>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXKeyManagement>(q => q.CreationTime);
                arg.Descending = true;
            }
            GXKeyManagement[] list = (await _host.Connection.SelectAsync<GXKeyManagement>(arg)).ToArray();

            //Ignore default key management items if user has ask
            //key management by system title and it's set.
            if (selectItemsBySystemTitle)
            {
                var tmp = list.Where(w => !string.IsNullOrEmpty(w.SystemTitle)).ToArray();
                if (tmp != null && tmp.Any())
                {
                    list = tmp;
                }
            }

            if (request?.Select != null && request.Select.Contains("KeyManagementKey"))
            {
                foreach (GXKeyManagement it in list)
                {
                    arg = GXSelectArgs.SelectAll<GXKeyManagementKey>(w => w.KeyManagement == it);
                    it.Keys = (await _host.Connection.SelectAsync<GXKeyManagementKey>(arg)).ToList();
                    //Decrypt keys.
                    foreach (var key in it.Keys)
                    {
                        if (!string.IsNullOrEmpty(key.Data))
                        {
                            key.Data = _cryproService.Decrypt(key.Data);
                        }
                    }
                }
            }
            if (request?.Select != null && request.Select.Contains("Object"))
            {
                foreach (GXKeyManagement it in list)
                {
                    if (it.Device != null)
                    {
                        //Get objects.
                        GXDevice dev = it.Device;
                        arg = GXSelectArgs.SelectAll<GXObject>(w => w.Device == dev && w.Removed == null);
                        arg.Columns.Exclude<GXObject>(e => new
                        {
                            e.CreationTime,
                            e.Updated,
                            e.Removed
                        });
                        if (request?.Select != null && request.Select.Contains("ObjectTemplate"))
                        {
                            arg.Columns.Add<GXObjectTemplate>();
                            arg.Columns.Exclude<GXObjectTemplate>(e => new
                            {
                                e.CreationTime,
                                e.Updated,
                                e.Removed
                            });
                            arg.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(j => j.Template, j => j.Id);
                            arg.Where.And<GXObjectTemplate>(w => w.Removed == null);
                        }
                        if (request?.Select != null && request.Select.Contains("Attribute"))
                        {
                            arg.Columns.Add<GXAttribute>();
                            arg.Columns.Exclude<GXAttribute>(e => new
                            {
                                e.Object,
                                e.CreationTime,
                                e.Updated,
                                e.Removed
                            });
                            arg.Joins.AddInnerJoin<GXObject, GXAttribute>(j => j.Id, j => j.Object);
                            arg.Where.And<GXAttribute>(w => w.Removed == null);
                            if (request?.Select != null && request.Select.Contains("AttributeTemplate"))
                            {
                                arg.Columns.Add<GXAttributeTemplate>();
                                arg.Columns.Exclude<GXAttributeTemplate>(e => new
                                {
                                    e.ObjectTemplate,
                                    e.CreationTime,
                                    e.Updated,
                                    e.Removed
                                });
                                arg.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(j => j.Template, j => j.Id);
                                arg.Where.And<GXAttributeTemplate>(w => w.Removed == null);
                            }
                        }
                        it.Device.Objects = (await _host.Connection.SelectAsync<GXObject>(arg)).ToList();
                    }
                }
            }

            if (response != null)
            {
                response.KeyManagements = list;
                if (response.Count == 0)
                {
                    response.Count = list.Length;
                }
            }
            return list;
        }

        /// <inheritdoc />
        public async Task<GXKeyManagement> ReadAsync(
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
                //Admin can see all the keys.
                arg = GXSelectArgs.SelectAll<GXKeyManagement>(w => w.Id == id);
            }
            else
            {
                string userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetKeyManagementsByUser(userId, id);
            }
            arg.Columns.Add<GXDevice>(s => new { s.Id, s.Name });
            arg.Columns.Exclude<GXDevice>(s => s.Keys);
            arg.Joins.AddLeftJoin<GXKeyManagement, GXDevice>(x => x.Device, y => y.Id);
            arg.Distinct = true;
            GXKeyManagement key = await _host.Connection.SingleOrDefaultAsync<GXKeyManagement>(arg);
            if (key == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            //Get key management groups with own query.
            arg = GXSelectArgs.SelectAll<GXKeyManagementGroup>(w => w.Removed == null);
            arg.Joins.AddLeftJoin<GXKeyManagementGroup, GXKeyManagementGroupKeyManagement>(j => j.Id, j => j.KeyManagementGroupId);
            arg.Where.And<GXKeyManagementGroupKeyManagement>(w => w.KeyManagementId == key.Id);
            key.KeyManagementGroups = await _host.Connection.SelectAsync<GXKeyManagementGroup>(arg);

            //Get key management keys with own query.
            arg = GXSelectArgs.SelectAll<GXKeyManagementKey>(w => w.KeyManagement == key && w.Removed == null);
            key.Keys = await _host.Connection.SelectAsync<GXKeyManagementKey>(arg);
            if (key.Keys != null)
            {
                //Decrypt keys.
                foreach (var it in key.Keys)
                {
                    if (!string.IsNullOrEmpty(it.Data))
                    {
                        it.Data = _cryproService.Decrypt(it.Data);
                    }
                }
            }
            return key;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal User,
            IEnumerable<GXKeyManagement> keys,
            Expression<Func<GXKeyManagement, object?>>? columns)
        {
            DateTime now = DateTime.Now;
            List<Guid> list = new();
            Dictionary<GXKeyManagement, List<string>> updates = new();
            foreach (GXKeyManagement key in keys)
            {
                if (key.Device != null && key.Device.Id == Guid.Empty)
                {
                    key.Device = null;
                }
                if (string.IsNullOrEmpty(key.SystemTitle))
                {
                    key.SystemTitle = null;
                }
                else
                {
                    key.SystemTitle = key.SystemTitle.Replace(" ", "");
                    if (key.SystemTitle.Length != 16)
                    {
                        throw new ArgumentException(string.Format("Invalid system title '{0}'.", key.SystemTitle));
                    }
                }
                //Verify key management key system title if it's given.
                //If system title is not given key management is used as a default value.
                if (!string.IsNullOrEmpty(key.SystemTitle) &&
                    GXDLMSTranslator.HexToBytes(key.SystemTitle).Length != 8)
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                if (key.KeyManagementGroups == null || !key.KeyManagementGroups.Any())
                {
                    //Get default key groups.
                    ListKeyManagementGroups request = new ListKeyManagementGroups()
                    {
                        Filter = new GXKeyManagementGroup() { Default = true }
                    };
                    key.KeyManagementGroups = new List<GXKeyManagementGroup>();
                    key.KeyManagementGroups.AddRange(await _deviceKeyGroupRepository.ListAsync(User, request, null, CancellationToken.None));
                    //It's OK if key management doesn't belong to any group.
                }
                if (key.Id == Guid.Empty)
                {
                    key.CreationTime = now;
                    key.Creator = new GXUser() { Id = ServerHelpers.GetUserId(User) };
                    GXInsertArgs args = GXInsertArgs.Insert(key);
                    args.Exclude<GXKeyManagement>(q => new
                    {
                        q.Updated,
                        q.KeyManagementGroups,
                        q.ConcurrencyStamp,
                        q.Keys
                    });
                    _host.Connection.Insert(args);
                    list.Add(key.Id);
                    AddKeyManagementToKeyManagementGroups(key.Id, key.KeyManagementGroups);
                    AddKeyManagementKeysToKeyManagement(key, key.Keys);
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXKeyManagement>(q => q.ConcurrencyStamp, where => where.Id == key.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != key.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    key.Updated = now;
                    key.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(key, columns);
                    args.Exclude<GXKeyManagement>(q => new
                    {
                        q.CreationTime,
                        q.KeyManagementGroups,
                        q.Creator,
                        q.Keys
                    });
                    _host.Connection.Update(args);
                    //Map key groups to key.
                    List<GXKeyManagementGroup> groups;
                    using (IServiceScope scope = _serviceProvider.CreateScope())
                    {
                        IKeyManagementGroupRepository deviceKeyGroupRepository = scope.ServiceProvider.GetRequiredService<IKeyManagementGroupRepository>();
                        groups = await deviceKeyGroupRepository.GetJoinedKeyManagementGroups(User, key.Id);
                    }
                    {
                        var comparer = new UniqueComparer<GXKeyManagementGroup, Guid>();
                        List<GXKeyManagementGroup> removedKeyManagementGroups = groups.Except(key.KeyManagementGroups, comparer).ToList();
                        List<GXKeyManagementGroup> addedKeyManagementGroups = key.KeyManagementGroups.Except(groups, comparer).ToList();
                        if (removedKeyManagementGroups.Any())
                        {
                            RemoveKeyManagementsFromKeyManagementGroup(key.Id, removedKeyManagementGroups);
                        }
                        if (addedKeyManagementGroups.Any())
                        {
                            AddKeyManagementToKeyManagementGroups(key.Id, addedKeyManagementGroups);
                        }
                    }
                    //Map key management keys to key management.
                    {
                        GXKeyManagement iKeyManagement = await ReadAsync(User, key.Id);
                        var comparer = new UniqueComparer<GXKeyManagementKey, Guid>();
                        List<GXKeyManagementKey>? removedKeys = null;
                        if (iKeyManagement.Keys != null && key.Keys != null)
                        {
                            removedKeys = iKeyManagement.Keys.Except(key.Keys, comparer).ToList();
                        }
                        List<GXKeyManagementKey>? addedKeys = null;
                        if (iKeyManagement.Keys == null)
                        {
                            addedKeys = key.Keys;
                        }
                        else if (key.Keys != null)
                        {
                            addedKeys = key.Keys.Except(iKeyManagement.Keys, comparer).ToList();
                        }
                        if (removedKeys != null && removedKeys.Any())
                        {
                            RemoveKeyManagementKeysFromKeyManagement(removedKeys);
                        }
                        if (addedKeys != null && addedKeys.Any())
                        {
                            AddKeyManagementKeysToKeyManagement(key, addedKeys);
                        }
                        //Update changed keys.
                        if (key.Keys != null && iKeyManagement.Keys != null)
                        {
                            var updatedKeys = key.Keys.Union(iKeyManagement.Keys, comparer).ToList();
                            foreach (var it in updatedKeys)
                            {
                                if (it.KeyType == null || (int)it.KeyType.Value == 0)
                                {
                                    throw new ArgumentException(Properties.Resources.InvalidKeyType);
                                }
                                if (string.IsNullOrEmpty(it.Data))
                                {
                                    throw new ArgumentException(Properties.Resources.InvalidKeyData);
                                }
                                else
                                {
                                    //Management key is saved excrypted.
                                    it.Data = _cryproService.Encrypt(it.Data);
                                }
                                if (it.IsHex == null)
                                {
                                    it.IsHex = false;
                                }
                                GXUpdateArgs u = GXUpdateArgs.Update(it, c => new
                                {
                                    c.KeyType,
                                    c.IsHex,
                                    c.Data,
                                    c.Updated
                                });
                                await _host.Connection.UpdateAsync(u);
                            }
                        }
                    }
                }
                updates[key] = await GetUsersAsync(User, key.Id);
            }
            foreach (var it in updates)
            {
                //Only ID and name are notified.
                await _eventsNotifier.KeyManagementUpdate(it.Value, new GXKeyManagement[] {
                    new GXKeyManagement(){Id = it.Key.Id, Name = it.Key.Name }});
            }
            List<GXKeyManagementLog> logs = new List<GXKeyManagementLog>();
            foreach (var it in updates.Keys)
            {
                logs.Add(new GXKeyManagementLog(TraceLevel.Info)
                {
                    CreationTime = DateTime.Now,
                    KeyManagement = new GXKeyManagement() { Id = it.Id, Name = it.Name },
                    Message = it.CreationTime == now ? Properties.Resources.KeyManagementInstalled :
                    Properties.Resources.KeyManagementUpdated
                });
            }
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                var deviceKeyLogRepository = scope.ServiceProvider.GetRequiredService<IKeyManagementLogRepository>();
                await deviceKeyLogRepository.AddAsync(User, logs);
            }
            return list.ToArray();
        }

        /// <summary>
        /// Map key group to user groups.
        /// </summary>
        /// <param name="keyId">KeyManagement ID.</param>
        /// <param name="groups">Group IDs of the key groups where the key is added.</param>
        private void AddKeyManagementToKeyManagementGroups(Guid keyId, IEnumerable<GXKeyManagementGroup> groups)
        {
            DateTime now = DateTime.Now;
            List<GXKeyManagementGroupKeyManagement> list = new();
            foreach (GXKeyManagementGroup it in groups)
            {
                list.Add(new GXKeyManagementGroupKeyManagement()
                {
                    KeyManagementId = keyId,
                    KeyManagementGroupId = it.Id,
                    CreationTime = now
                });
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(list));
        }

        /// <summary>
        /// Remove map between key group and key.
        /// </summary>
        /// <param name="keyId">KeyManagement ID.</param>
        /// <param name="groups">Group IDs of the key groups where the key is removed.</param>
        private void RemoveKeyManagementsFromKeyManagementGroup(Guid keyId, IEnumerable<GXKeyManagementGroup> groups)
        {
            foreach (var it in groups)
            {
                _host.Connection.Delete(GXDeleteArgs.Delete<GXKeyManagementGroupKeyManagement>(w => w.KeyManagementId == keyId && w.KeyManagementGroupId == it.Id));
            }
        }

        /// <summary>
        /// Map keys to key management.
        /// </summary>
        /// <param name="keyManagement">Key management.</param>
        /// <param name="keys">Added keys.</param>
        public void AddKeyManagementKeysToKeyManagement(GXKeyManagement keyManagement, IEnumerable<GXKeyManagementKey>? keys)
        {
            if (keys != null && keys.Any())
            {
                DateTime now = DateTime.Now;
                foreach (GXKeyManagementKey it in keys)
                {
                    if (it.KeyType == null || (int)it.KeyType.Value == 0)
                    {
                        throw new ArgumentException(Properties.Resources.InvalidKeyType);
                    }
                    if (string.IsNullOrEmpty(it.Data))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidKeyData);
                    }
                    else
                    {
                        //Management key is saved excrypted.
                        it.Data = _cryproService.Encrypt(it.Data);
                    }
                    if (it.IsHex == null)
                    {
                        it.IsHex = false;
                    }
                    it.CreationTime = now;
                    it.KeyManagement = keyManagement;
                    GXInsertArgs args = GXInsertArgs.Insert(it);
                    args.Exclude<GXKeyManagementKey>(e => new { e.Updated, e.Removed });
                    _host.Connection.Insert(args);
                }
            }
        }

        /// <summary>
        /// Remove map between key management group and key management.
        /// </summary>
        /// <param name="keys">Removed keys</param>
        public void RemoveKeyManagementKeysFromKeyManagement(IEnumerable<GXKeyManagementKey> keys)
        {
            DateTime now = DateTime.Now;
            foreach (GXKeyManagementKey it in keys)
            {
                it.Removed = now;
            }
            GXUpdateArgs args = GXUpdateArgs.UpdateRange(keys, c => c.Removed);
            _host.Connection.UpdateAsync(args);
        }
    }
}
