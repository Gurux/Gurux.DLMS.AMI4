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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Server.Internal;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    class UserErrorRepository : IUserErrorRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public UserErrorRepository(IGXHost host,
            IGXEventsNotifier eventsNotifier,
            IUserRepository userRepository)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
        }

        /// <inheritdoc/>
        public async Task AddAsync(ClaimsPrincipal User, IEnumerable<GXUserError> errors)
        {
            Dictionary<GXUserError, string> updates = new Dictionary<GXUserError, string>();
            foreach (GXUserError it in errors)
            {
                it.CreationTime = DateTime.Now;
                if (it.User == null || string.IsNullOrEmpty(it.User.Id))
                {
                    it.User = new GXUser() { Id = ServerHelpers.GetUserId(User) };
                }
                updates[it] = it.User.Id;
            }
            await _host.Connection.InsertAsync(GXInsertArgs.InsertRange(errors));
            foreach (var it in updates)
            {
                await _eventsNotifier.AddUserErrors(new string[] { it.Value }, new GXUserError[] { it.Key });
            }
        }

        /// <inheritdoc />
        public async Task<GXUserError> AddAsync(ClaimsPrincipal User, GXUser user, Exception ex)
        {
            DateTime now = DateTime.Now;
            if (user == null || string.IsNullOrEmpty(user.Id))
            {
                user = new GXUser() { Id = ServerHelpers.GetUserId(User) };
            }
            GXUserError error = new GXUserError()
            {
                CreationTime = now,
                Message = ex.Message,
                User = user
            };
            await _host.Connection.InsertAsync(GXInsertArgs.Insert(error));
            await _eventsNotifier.AddUserErrors(new string[] { user.Id }, new GXUserError[] { error });
            return error;
        }

        /// <inheritdoc />
        public async Task ClearAsync(ClaimsPrincipal user, IEnumerable<string>? users)
        {
            if (!user.IsInRole(GXRoles.Admin) && !user.IsInRole(GXRoles.UserErrorManager))
            {
                throw new UnauthorizedAccessException();
            }

            GXSelectArgs arg;
            bool admin = user.IsInRole(GXRoles.Admin);
            if (!admin)
            {
                string id = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetUsersByUser(id);
            }
            else
            {
                arg = GXSelectArgs.SelectAll<GXUser>(w => w.Removed == null);
            }
            arg.Distinct = true;
            arg.Columns.Clear();
            arg.Columns.Add<GXUser>(s => new { s.Id, s.UserName });
            arg.Joins.AddInnerJoin<GXUser, GXUserError>(y => y.Id, x => x.User);
            if (users != null && users.Any())
            {
                arg.Where.And<GXUser>(w => users.Contains(w.Id));
            }
            List<GXUser>? errors = await _host.Connection.SelectAsync<GXUser>(arg);
            List<string> list = await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin });
            //Notify users if their actions are cleared.
            list.AddDistinct(errors.Select(s => s.Id));
            if (admin && (users == null || !users.Any()))
            {
                //Admin clears all user actions.
                _host.Connection.Truncate<GXUserError>();
                errors = null;
            }
            else if (errors.Any())
            {
                GXDeleteArgs args = GXDeleteArgs.Delete<GXUserError>(w => errors.Contains(w.User));
                await _host.Connection.DeleteAsync(args);
            }
            await _eventsNotifier.ClearUserErrors(list, errors);
        }

        /// <inheritdoc />
        public async Task CloseAsync(ClaimsPrincipal User, IEnumerable<Guid> errors)
        {
            if (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.UserErrorManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserError>(where => errors.Contains(where.Id));
            arg.Columns.Add<GXUser>();
            arg.Joins.AddInnerJoin<GXUserError, GXUser>(j => j.User, j => j.Id);
            List<GXUserError> list = (await _host.Connection.SelectAsync<GXUserError>(arg));
            Dictionary<GXUserError, string> updates = new Dictionary<GXUserError, string>();
            foreach (var it in list)
            {
                updates[it] = it.User.Id;
            }
            DateTime now = DateTime.Now;
            foreach (GXUserError it in list)
            {
                it.Closed = now;
            }
            if (list.Count != 0)
            {
                await _host.Connection.UpdateAsync(GXUpdateArgs.UpdateRange(list, q => q.Closed));
                foreach (var it in updates)
                {
                    GXUserError tmp = new GXUserError(System.Diagnostics.TraceLevel.Error) { Id = it.Key.Id };
                    await _eventsNotifier.CloseUserErrors(new string[] { it.Value }, new GXUserError[] { tmp });
                }
            }
        }

        /// <inheritdoc />
        public async Task<GXUserError[]> ListAsync(
            ClaimsPrincipal user,
            ListUserErrors? request,
            ListUserErrorsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && user.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the errors.
                arg = GXSelectArgs.SelectAll<GXUserError>();
                arg.Joins.AddInnerJoin<GXUserError, GXUser>(j => j.User, j => j.Id);
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetUserErrorsByUser(userId);
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Filter != null && !string.IsNullOrEmpty(request.Filter?.User?.UserName))
                {
                    arg.Where.And<GXUser>(w => w.UserName.Contains(request.Filter.User.UserName));
                }
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXUserError>(w => request.Exclude.Contains(w.Id) == false);
                }
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXUserError>(q => GXSql.DistinctCount(q.Id));
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
                arg.OrderBy.Add<GXUserError>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXUserError>(q => q.CreationTime);
            }
            arg.Columns.Add<GXUser>(c => new { c.Id, c.UserName });
            arg.Columns.Exclude<GXUser>(e => e.Errors);
            GXUserError[] errors = (await _host.Connection.SelectAsync<GXUserError>(arg)).ToArray();
            if (response != null)
            {
                response.Errors = errors;
                if (response.Count == 0)
                {
                    response.Count = errors.Length;
                }
            }
            return errors;
        }

        /// <inheritdoc />
        public async Task<GXUserError> ReadAsync(ClaimsPrincipal User, Guid id)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserError>(where => where.Id == id);
            //Get user.
            arg.Columns.Add<GXUser>();
            arg.Joins.AddInnerJoin<GXUserError, GXUser>(x => x.User, y => y.Id);
            //Errors are ignored from the user 
            //so there is no reference relation that is causing problems with JSON parser.
            arg.Columns.Exclude<GXUser>(e => e.Errors);
            GXUserError error = (await _host.Connection.SingleOrDefaultAsync<GXUserError>(arg));
            if (error == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            return error;
        }
    }
}
