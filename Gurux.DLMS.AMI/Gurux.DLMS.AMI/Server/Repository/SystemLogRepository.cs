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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Internal;
using Org.BouncyCastle.Asn1.Ocsp;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    class SystemLogRepository : ISystemLogRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;

        /// <summary>
        /// Constructor.
        /// </summary>
        public SystemLogRepository(IGXHost host,
            IGXEventsNotifier eventsNotifier)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
        }

        /// <summary>
        /// Get all users that can access system errors.
        /// </summary>
        /// <returns></returns>
        List<string> GetUsers()
        {
            string[] names = { GXRoles.Admin, GXRoles.SystemLogManager };
            GXSelectArgs args = GXSelectArgs.Select<GXUserRole>(s => s.UserId);
            args.Joins.AddInnerJoin<GXUserRole, GXRole>(a => a.RoleId, b => b.Id);
            args.Where.And<GXRole>(where => GXSql.In(where.Name, names));
            List<GXUserRole> users = _host.Connection.Select<GXUserRole>(args);
            return users.Select(s => s.UserId).ToList();
        }

        /// <inheritdoc />
        public async Task<GXSystemLog[]> ListAsync(
            ClaimsPrincipal User,
            ListSystemLogs? request,
            ListSystemLogsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXSystemLog>();
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXSystemLog>(w => !request.Exclude.Contains(w.Id));
                }
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXSystemLog>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXSystemLog>(q => q.CreationTime);
                arg.Descending = true;
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXSystemLog>(q => GXSql.DistinctCount(q.Id));
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            GXSystemLog[] errors = (await _host.Connection.SelectAsync<GXSystemLog>(arg)).ToArray();
            if (response != null)
            {
                response.Errors = errors;
            }
            return errors;
        }

        /// <inheritdoc/>
        public async Task<GXSystemLog> ReadAsync(ClaimsPrincipal User, Guid id)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXSystemLog>(w => w.Id == id);
            arg.Distinct = true;
            GXSystemLog log = await _host.Connection.SingleOrDefaultAsync<GXSystemLog>(arg);
            if (log == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            return log;
        }

        /// <inheritdoc/>
        public async Task ClearAsync(ClaimsPrincipal User)
        {
            if (User != null && !User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.SystemLogManager))
            {
                throw new UnauthorizedAccessException();
            }
            _host.Connection.Truncate<GXSystemLog>();
            await _eventsNotifier.ClearSystemLogs(GetUsers());
        }

        /// <inheritdoc/>
        public async Task AddAsync(ClaimsPrincipal User, IEnumerable<GXSystemLog> errors)
        {
            if (User != null && !User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.SystemLogManager))
            {
                throw new UnauthorizedAccessException();
            }
            foreach (var it in errors)
            {
                it.CreationTime = DateTime.Now;
            }
            await _host.Connection.InsertAsync(GXInsertArgs.InsertRange(errors));
            await _eventsNotifier.AddSystemLogs(GetUsers(), errors);
        }

        /// <inheritdoc/>
        public async Task<GXSystemLog> AddAsync(ClaimsPrincipal User, Exception error)
        {
            GXSystemLog e = new GXSystemLog()
            {
                CreationTime = DateTime.Now,
                Message = error.Message,
            };
            await _host.Connection.InsertAsync(GXInsertArgs.Insert(e));
            GXSystemLog[] errors = new GXSystemLog[] { e };
            await _eventsNotifier.AddSystemLogs(GetUsers(), errors);
            return e;
        }

        /// <inheritdoc />
        public async Task CloseAsync(ClaimsPrincipal User, IEnumerable<Guid> errors)
        {
            if (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.SystemLogManager))
            {
                throw new UnauthorizedAccessException();
            }
            List<GXSystemLog> list = new List<GXSystemLog>();
            DateTime now = DateTime.Now;
            foreach (Guid it in errors)
            {
                list.Add(new GXSystemLog() { Id = it, Closed = now });
            }
            if (list.Count != 0)
            {
                await _host.Connection.UpdateAsync(GXUpdateArgs.UpdateRange(list, q => q.Closed));
                await _eventsNotifier.CloseSystemLogs(GetUsers(), list);
            }
        }
    }
}
