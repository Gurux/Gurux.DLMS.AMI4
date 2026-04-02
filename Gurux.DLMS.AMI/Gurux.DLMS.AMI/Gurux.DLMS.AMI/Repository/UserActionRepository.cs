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

using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using System.Diagnostics;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class UserActionRepository : IUserActionRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly GXPerformanceSettings _performanceSettings;
        private readonly IEnumTypeRepository _enumTypeRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public UserActionRepository(
            IGXHost host,
            IGXAmiContextAccessor contextAccessor,
            IUserRepository userRepository,
            IGXEventsNotifier eventsNotifier,
            IEnumTypeRepository enumTypeRepository,
            GXPerformanceSettings performanceSettings)
        {
            var user = contextAccessor?.User;
            if (user == null ||
                (!user.IsInRole(GXRoles.Admin) &&
                !user.IsInRole(GXRoles.UserAction) &&
                !user.IsInRole(GXRoles.UserActionManager)))
            {
                throw new UnauthorizedAccessException();
            }
            User = user;
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
            _performanceSettings = performanceSettings;
            _enumTypeRepository = enumTypeRepository;
        }

        /// <inheritdoc />
        public async Task<GXUserAction[]> ListAsync(
            ListUserAction? request,
            ListUserActionResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && User.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the errors.
                arg = GXSelectArgs.SelectAll<GXUserAction>();
                arg.Joins.AddInnerJoin<GXUserAction, GXUser>(j => j.User, j => j.Id);
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetUserActionsByUser(userId);
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXUserAction>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXUserAction>(w => request.Included.Contains(w.Id));
                }
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXUserAction>(q => GXSql.DistinctCount(q.Id));
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
                arg.OrderBy.Add<GXUserAction>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXUserAction>(q => q.CreationTime);
                arg.Descending = true;
            }
            arg.Columns.Add<GXUser>(c => new { c.Id, c.UserName });
            arg.Columns.Exclude<GXUser>(e => e.Actions);
            //Data amd reply are excluded. Reading those might take a long time.
            arg.Columns.Exclude<GXUserAction>(e => new { e.Data, e.Reply });
            GXUserAction[] actions = (await _host.Connection.SelectAsync<GXUserAction>(arg)).ToArray();
            if (response != null)
            {
                response.Actions = actions;
                if (response.Count == 0)
                {
                    response.Count = actions.Length;
                }
            }
            return actions;
        }

        /// <inheritdoc />
        public async Task<GXUserAction> ReadAsync(Guid id)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserAction>(where => where.Id == id);
            //Get user.
            arg.Columns.Add<GXUser>(c => new { c.Id, c.UserName });
            arg.Joins.AddInnerJoin<GXUserAction, GXUser>(x => x.User, y => y.Id);
            //Actions are ignored from the user 
            //so there is no reference relation that is causing problems with JSON parser.
            arg.Columns.Exclude<GXUser>(e => e.Actions);
            GXUserAction userAction = (await _host.Connection.SingleOrDefaultAsync<GXUserAction>(arg));
            if (userAction == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            return userAction;
        }

        /// <inheritdoc />
        public async Task AddAsync(string type, IEnumerable<GXUserAction> actions)
        {
            GXUser user = new GXUser() { Id = ServerHelpers.GetUserId(User) };
            Dictionary<GXUserAction, string> updates = new Dictionary<GXUserAction, string>();
            foreach (GXUserAction it in actions)
            {
                it.CreationTime = DateTime.Now;
                if (it.User == null || string.IsNullOrEmpty(it.User.Id))
                {
                    it.User = user;
                }
                //Update enum types.
                await _enumTypeRepository.GetLogTypeAsync(TargetType.UserAction, type);
                updates[it] = it.User.Id;
            }
            await _host.Connection.InsertAsync(GXInsertArgs.InsertRange(actions));
            foreach (var it in updates)
            {
                var users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                  [it.Value], TargetType.UserAction, NotificationAction.Add);
                if (users != null && users.Any())
                {
                    await _eventsNotifier.UserActionAdd(users, actions);
                }
            }
        }

        /// <inheritdoc />
        public async Task ClearAsync(IEnumerable<string>? users)
        {
            if (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.UserActionManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg;
            bool admin = User.IsInRole(GXRoles.Admin);
            if (!admin)
            {
                string id = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetUsersByUser(s => s.Id, id);
            }
            else
            {
                arg = GXSelectArgs.SelectAll<GXUser>(w => w.Removed == null);
            }
            arg.Distinct = true;
            arg.Columns.Clear();
            arg.Columns.Add<GXUser>(s => new { s.Id, s.UserName });
            arg.Joins.AddInnerJoin<GXUser, GXUserAction>(y => y.Id, x => x.User);
            if (users != null && users.Any())
            {
                arg.Where.And<GXUser>(w => users.Contains(w.Id));
            }
            List<GXUser>? clearedUsers = await _host.Connection.SelectAsync<GXUser>(arg);
            //Notification users if their actions are cleared.
            List<string>? list = clearedUsers.Select(s => s.Id).ToList();
            if (admin && (users == null || !users.Any()))
            {
                //Admin clears all user actions.
                _host.Connection.Truncate<GXUserAction>();
                clearedUsers = null;
            }
            else if (clearedUsers.Any())
            {
                GXUser tmp = clearedUsers[0];
                GXDeleteArgs args = GXDeleteArgs.Delete<GXUserAction>(w => w.User == tmp);
                await _host.Connection.DeleteAsync(args);
            }
            //Clear enum types.
            await _enumTypeRepository.DeleteAsync(TargetType.UserAction);
            list = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                list, TargetType.UserAction, NotificationAction.Clear);
            if (list != null && list.Any())
            {
                await _eventsNotifier.UserActionsClear(list, clearedUsers);
            }
        }
    }
}