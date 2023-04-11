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
    public class UserSettingRepository : IUserSettingRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;

        /// <summary>
        /// Constructor.
        /// </summary>
        public UserSettingRepository(
            IGXHost host,
            IGXEventsNotifier eventsNotifier)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal User, IEnumerable<string> settings)
        {
            if (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.User))
            {
                throw new UnauthorizedAccessException();
            }
            GXUser user = new GXUser() { Id = ServerHelpers.GetUserId(User) };
            GXSelectArgs arg = GXSelectArgs.Select<GXUserSetting>(a => new { a.Id, a.Name },
                q => q.User == user && settings.Contains(q.Name));
            List<GXUserSetting> list = _host.Connection.Select<GXUserSetting>(arg);
            foreach (GXUserSetting it in list)
            {
                await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXUserSetting>(it.Id));
            }
            await _eventsNotifier.UserSettingDelete(new string[] { user.Id }, list);
        }

        /// <inheritdoc />
        public async Task<GXUserSetting[]> ListAsync(
            ClaimsPrincipal User,
            ListUserSettings? request,
            ListUserSettingsResponse? response,
            CancellationToken cancellationToken)
        {
            GXUser user = new GXUser() { Id = ServerHelpers.GetUserId(User) };
            GXSelectArgs arg = GXSelectArgs.Select<GXUserSetting>(a => new { a.Id, a.Name },
                q => q.User == user);
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXUserSetting>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = await _host.Connection.SingleOrDefaultAsync<int>(total, cancellationToken);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXUserSetting>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXUserSetting>(q => q.CreationTime);
            }
            GXUserSetting[] settings = (await _host.Connection.SelectAsync<GXUserSetting>(arg, cancellationToken)).ToArray();
            if (response != null)
            {
                response.Settings = settings;
            }
            return settings;
        }

        /// <inheritdoc />
        public async Task<GXUserSetting> ReadAsync(
         ClaimsPrincipal user,
         string name)
        {
            GXUser User = new GXUser() { Id = ServerHelpers.GetUserId(user) };
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserSetting>(
            q => q.User == User && q.Name == name);
            GXUserSetting ret = await _host.Connection.SingleOrDefaultAsync<GXUserSetting>(arg);
            return ret;
        }

        /// <inheritdoc />
        public async Task UpdateAsync(
            ClaimsPrincipal User,
            IEnumerable<GXUserSetting> settings,
            Expression<Func<GXUserSetting, object?>>? columns)
        {
            GXUser user = new GXUser() { Id = ServerHelpers.GetUserId(User) };
            DateTime now = DateTime.Now;
            Dictionary<GXUserSetting, List<string>> updates = new Dictionary<GXUserSetting, List<string>>();
            foreach (GXUserSetting it in settings)
            {
                if (string.IsNullOrEmpty(it.Name))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                GXUserSetting ret = await ReadAsync(User, it.Name);
                if (ret == null)
                {
                    it.CreationTime = now;
                    it.User = user;
                    GXInsertArgs args = GXInsertArgs.Insert(it);
                    args.Exclude<GXUserSetting>(q => new { q.Updated });
                    _host.Connection.Insert(args);
                }
                else
                {
                    it.Id = ret.Id;
                    GXSelectArgs m = GXSelectArgs.Select<GXUserSetting>(q => q.ConcurrencyStamp, where => where.Id == it.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != it.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    it.Updated = now;
                    it.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(it, columns);
                    args.Exclude<GXUserSetting>(q => new { q.CreationTime, q.User });
                    _host.Connection.Update(args);
                }
                List<string> users = new List<string>();
                users.Add(user.Id);
                updates[it] = users;
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.UserSettingUpdate(it.Value, new GXUserSetting[] { it.Key });
            }
        }
    }
}