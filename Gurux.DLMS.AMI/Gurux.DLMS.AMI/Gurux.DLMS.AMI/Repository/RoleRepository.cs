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
using Gurux.DLMS.AMI.Server.Midlewares;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Client.Shared.Enums;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class RoleRepository : IRoleRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly GXLanguageService _languageService;
        private readonly ILocalizationRepository _localizationRepository;
        private readonly GXPerformanceSettings _performanceSettings;

        /// <summary>
        /// Constructor.
        /// </summary>
        public RoleRepository(IGXHost host,
            IGXAmiContextAccessor contextAccessor,
            IUserRepository userRepository,
            GXLanguageService languageService,
            ILocalizationRepository localizationRepository,
            IGXEventsNotifier eventsNotifier,
            GXPerformanceSettings performanceSettings)
        {
            var user = contextAccessor?.User;
            if (user == null ||
                !(user.IsInRole(GXRoles.Admin) || user.IsInRole(GXRoles.User)))
            {
                throw new UnauthorizedAccessException();
            }
            User = user;
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
            _languageService = languageService;
            _localizationRepository = localizationRepository;
            _performanceSettings = performanceSettings;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(IEnumerable<string> rolers)
        {
            if (!User.IsInRole(GXRoles.Admin))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXRole>(a => a.Id, q => rolers.Contains(q.Id));
            List<GXRole> list = _host.Connection.Select<GXRole>(arg);
            DateTime now = DateTime.Now;
            foreach (GXRole it in list)
            {
                it.Removed = now;
                _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
            }
            var users = await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]);
            users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                users, TargetType.Role, NotificationAction.Remove);
            if (users != null)
            {
                await _eventsNotifier.RoleDelete(users, rolers);
            }
        }

        /// <inheritdoc />
        public async Task<GXRole[]> ListAsync(
            ListRoles? request,
            ListRolesResponse? response,
            CancellationToken cancellationToken)
        {
            bool isAdmin = User.IsInRole(GXRoles.Admin);
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXRole>(w => w.Removed == null);
            arg.Columns.Add<GXScope>();
            arg.Columns.Exclude<GXScope>(e => e.Role);
            arg.Joins.AddLeftJoin<GXRole, GXScope>(j => j.Id, j => j.Role);
            arg.OrderBy.Add<GXRole>(o => o.Name);
            if (!isAdmin)
            {
                //Return roles that user can access.
                string? userId = ServerHelpers.GetUserId(User);
                arg.Joins.AddInnerJoin<GXRole, GXUserRole>(a => a.Id, b => b.RoleId);
                arg.Where.And<GXUserRole>(where => where.UserId == userId);
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXRole>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXRole>(q => q.Id);
                arg.Descending = true;
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXRole>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            GXRole[] roles = (await _host.Connection.SelectAsync<GXRole>(arg)).ToArray();           
            if (response != null)
            {
                response.Roles = roles;
                if (response.Count == 0)
                {
                    response.Count = roles.Length;
                }
            }
            return roles;
        }

        /// <inheritdoc />
        public async Task<GXRole> ReadAsync(string id)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXRole>(w => w.Id == id && w.Removed == null);
            arg.Columns.Add<GXScope>();
            arg.Joins.AddLeftJoin<GXRole, GXScope>(j => j.Id, j => j.Role);
            arg.OrderBy.Add<GXRole>(o => o.Name);
            arg.Distinct = true;
            GXRole role = await _host.Connection.SingleOrDefaultAsync<GXRole>(arg);
            if (role == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            return role;
        }

        private async Task<List<GXScope>> GetScopesByRoleIdAsync(string roleId)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXScope>();
            arg.Joins.AddInnerJoin<GXScope, GXRole>(j => j.Role, j => j.Id);
            arg.Where.And<GXRole>(w => w.Id == roleId);
            arg.Distinct = true;
            return await _host.Connection.SelectAsync<GXScope>(arg);
        }

        /// <inheritdoc />
        public async Task<string[]> UpdateAsync(IEnumerable<GXRole> roles)
        {
            if (!User.IsInRole(GXRoles.Admin))
            {
                throw new UnauthorizedAccessException();
            }
            DateTime now = DateTime.Now;
            List<string> list = new();
            List<GXRole> updates = new();
            foreach (GXRole role in roles)
            {
                if (string.IsNullOrEmpty(role.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                //Get role ID.
                var arg = GXSelectArgs.SelectAll<GXRole>(w => w.Name.Equals(role.Name));
                GXRole? tmp = await _host.Connection.SingleOrDefaultAsync<GXRole>(arg);
                if (tmp?.Id != null)
                {
                    role.Id = tmp.Id;
                }
                if (string.IsNullOrEmpty(role.Id))
                {
                    GXInsertArgs args = GXInsertArgs.Insert(role);
                    args.Exclude<GXRole>(e => e.Scopes);
                    _host.Connection.Insert(args);
                    list.Add(role.Id);
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXRole>(q => new { q.Default, q.ConcurrencyStamp }, where => where.Id == role.Id);
                    GXRole? old = _host.Connection.SingleOrDefault<GXRole>(m);
                    if (old == null || !string.IsNullOrEmpty(old.ConcurrencyStamp) && old.ConcurrencyStamp != role.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    //If content of the role has changed.
                    if (old.Default != role.Default && role.Default != null)
                    {
                        role.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs args = GXUpdateArgs.Update(role, u => new { u.Default, u.ConcurrencyStamp });
                        _host.Connection.Update(args);
                        updates.Add(role);
                    }
                }
                {
                    List<GXScope> scopes = await GetScopesByRoleIdAsync(role.Id);
                    var comparer1 = new UniqueComparer<GXScope, Guid>();
                    List<GXScope> addedScopes;
                    if (role.Scopes == null)
                    {
                        addedScopes = new List<GXScope>();
                    }
                    else
                    {
                        addedScopes = role.Scopes.Where(w => !scopes.Exists(q => q.Name == w.Name)).ToList();
                    }
                    List<GXScope> removedScopes;
                    if (role.Scopes == null)
                    {
                        removedScopes = new List<GXScope>();
                    }
                    else
                    {
                        removedScopes = scopes.Where(w => !role.Scopes.Exists(q => q.Name == w.Name)).ToList();
                    }
                    if (addedScopes.Any())
                    {
                        AddScopeToRole(role, addedScopes);
                    }
                    if (removedScopes.Any())
                    {
                        RemoveScope(removedScopes);
                    }
                    //Update scopes.
                    if (role.Scopes?.Any() == true)
                    {
                        role.Scopes.RemoveAll(w => addedScopes.Contains(w));
                        role.Scopes.RemoveAll(w => removedScopes.Contains(w));
                        //Set role before update and reset after.
                        foreach (var scope in role.Scopes)
                        {
                            scope.Role = role;
                        }
                        await _host.Connection.UpdateAsync(GXUpdateArgs.UpdateRange(role.Scopes));
                        foreach (var scope in role.Scopes)
                        {
                            scope.Role = null;
                        }
                    }
                }
            }
            if (updates.Any())
            {
                var users = await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]);
                await _eventsNotifier.RoleUpdate(users, updates);
            }
            return list.ToArray();
        }

        /// <summary>
        /// Add scope to role.
        /// </summary>
        /// <param name="role">Role.</param>
        /// <param name="scopes">Added scopes.</param>
        private void AddScopeToRole(GXRole role, IEnumerable<GXScope> scopes)
        {
            foreach (GXScope it in scopes)
            {
                it.Role = role;
            }
            _host.Connection.Insert(GXInsertArgs.InsertRange(scopes));
        }

        /// <summary>
        /// Remove scope.
        /// </summary>
        /// <param name="scopes">Removed scopes.</param>
        private void RemoveScope(IEnumerable<GXScope> scopes)
        {
            foreach (GXScope it in scopes)
            {
                _host.Connection.Delete(GXDeleteArgs.DeleteById<GXScope>(it));
            }
        }
    }
}
