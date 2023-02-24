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
using Gurux.DLMS.AMI.Shared.DTOs;
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc cref="IRoleRepository"/>
    public class RoleRepository : IRoleRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public RoleRepository(IGXHost host,
            IUserRepository userRepository,
            IGXEventsNotifier eventsNotifier)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal user, IEnumerable<string> rolers)
        {
            if (user == null || !user.IsInRole(GXRoles.Admin))
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
            var users = await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin });
            await _eventsNotifier.RoleDelete(users, rolers);
        }

        /// <inheritdoc />
        public async Task<GXRole[]> ListAsync(
            ClaimsPrincipal user,
            ListRoles? request,
            ListRolesResponse? response,
            CancellationToken cancellationToken)
        {
            bool isAdmin = user.IsInRole(GXRoles.Admin);
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXRole>(w => w.Removed == null);
            arg.OrderBy.Add<GXRole>(o => o.Name);
            if (!isAdmin)
            {
                //Return roles that user can access.
                string? userId = ServerHelpers.GetUserId(user);
                arg.Joins.AddInnerJoin<GXRole, GXUserRole>(a => a.Id, b => b.RoleId);
                arg.Where.And<GXUserRole>(where => where.UserId == userId);
            }
            if (request != null && request.Filter != null)
            {
                arg.Where.FilterBy(request.Filter);
            }
            arg.OrderBy.Add<GXRole>(q => q.Id);
            arg.Distinct = true;
            arg.Descending = true;
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
        public async Task<GXRole> ReadAsync(
            ClaimsPrincipal user,
            string id)
        {
            bool isAdmin = false;
            if (user != null)
            {
                isAdmin = user.IsInRole(GXRoles.Admin);
            }
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXRole>(w => w.Id == id && w.Removed == null);
            arg.Distinct = true;
            GXRole role = await _host.Connection.SingleOrDefaultAsync<GXRole>(arg);
            if (role == null)
            {
                throw new ArgumentNullException(Properties.Resources.UnknownTarget);
            }
            return role;
        }

        /// <inheritdoc />
        public async Task<string[]> UpdateAsync(
            ClaimsPrincipal user, 
            IEnumerable<GXRole> roles)
        {
            DateTime now = DateTime.Now;
            List<string> list = new();
            List<GXRole> updates = new();
            foreach (GXRole role in roles)
            {
                if (string.IsNullOrEmpty(role.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                if (string.IsNullOrEmpty(role.Id))
                {
                    GXInsertArgs args = GXInsertArgs.Insert(role);
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
                    if (old.Default != role.Default)
                    {
                        role.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs args = GXUpdateArgs.Update(role, u => new { u.Default, u.ConcurrencyStamp });
                        _host.Connection.Update(args);
                        updates.Add(role);
                    }
                }
            }
            if (updates.Any())
            {
                var users = await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin });
                await _eventsNotifier.RoleUpdate(users, updates);
            }
            return list.ToArray();
        }
    }
}
