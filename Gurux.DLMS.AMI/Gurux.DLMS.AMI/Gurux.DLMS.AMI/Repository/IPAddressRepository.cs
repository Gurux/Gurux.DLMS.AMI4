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
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc/>
    public class IPAddressRepository : IIpAddressRepository
    {
        private readonly IWorkflowHandler _workflowHandler;
        private readonly IGXHost _host;
        private readonly ClaimsPrincipal? User;

        /// <summary>        
        /// Update blocked items in constructor.
        /// </summary>
        public IPAddressRepository(IGXAmiContextAccessor contextAccessor,
            IGXHost host,
            IWorkflowHandler workflowHandler)
        {
            User = contextAccessor?.User;
            _host = host;
            _workflowHandler = workflowHandler;
        }

        /// <inheritdoc/>
        public async Task<GXIpAddress[]> ListAsync(ListIpAddress? request,
            ListIpAddressResponse? response = null,
            CancellationToken cancellationToken = default)
        {
            if (User == null ||
               (!User.IsInRole(GXRoles.Admin) &&
               !User.IsInRole(GXRoles.User)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg;
            if (request != null && User.IsInRole(GXRoles.Admin) == true)
            {
                //Admin can see all the IP address.
                arg = GXSelectArgs.SelectAll<GXIpAddress>();
                arg.Joins.AddLeftJoin<GXIpAddress, GXUser>(j => j.User, j => j.Id);
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(User, false);
                arg = GXSelectArgs.SelectAll<GXIpAddress>();
                arg.Joins.AddInnerJoin<GXIpAddress, GXUser>(j => j.User, j => j.Id);
                arg.Where.And<GXUser>(w => w.Id == userId);
            }
            arg.Columns.Add<GXUser>(s => new { s.Id, s.UserName });
            arg.Columns.Exclude<GXUser>(e => e.IpAddresses);
            if (request?.Exclude != null && request.Exclude.Any())
            {
                arg.Where.And<GXIpAddress>(w => !request.Exclude.Contains(w.Id));
            }
            if (request?.Included != null && request.Included.Any())
            {
                arg.Where.And<GXIpAddress>(w => request.Included.Contains(w.Id));
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
                arg.OrderBy.Add<GXIpAddress>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXIpAddress>(q => q.CreationTime);
                arg.Descending = true;
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXIpAddress>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            GXIpAddress[] addresses = (await _host.Connection.SelectAsync<GXIpAddress>(arg)).ToArray();
            if (response != null)
            {
                response.IpAddress = addresses;
                if (response.Count == 0)
                {
                    response.Count = addresses.Length;
                }
            }
            return addresses;
        }

        /// <inheritdoc/>
        public async Task<GXIpAddress> ReadAsync(Guid id)
        {
            if (User == null ||
              (!User.IsInRole(GXRoles.Admin) &&
              !User.IsInRole(GXRoles.User)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg;
            if (User.IsInRole(GXRoles.Admin) == true)
            {
                //Admin can see all the IP address.
                arg = GXSelectArgs.SelectAll<GXIpAddress>(w => w.Id == id);
                arg.Joins.AddLeftJoin<GXIpAddress, GXUser>(j => j.User, j => j.Id);
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(User, false);
                arg = GXSelectArgs.SelectAll<GXIpAddress>(w => w.Id == id);
                arg.Joins.AddInnerJoin<GXIpAddress, GXUser>(j => j.User, j => j.Id);
                arg.Where.And<GXUser>(w => w.Id == userId);
            }
            arg.Columns.Add<GXUser>(s => new { s.Id, s.UserName });
            var address = await _host.Connection.SingleOrDefaultAsync<GXIpAddress>(arg);
            if (address == null)
            {
                throw new GXAmiNotFoundException(Properties.Resources.IpAccessControl + " " + Properties.Resources.Id + " " + id.ToString());
            }
            return address;
        }

        /// <inheritdoc/>
        public async Task<Guid[]> UpdateAsync(IEnumerable<GXIpAddress> list,
            Expression<Func<GXIpAddress, object?>>? columns = null)
        {
            GXUpdateArgs args = GXUpdateArgs.UpdateRange(list, columns);
            args.Exclude<GXIpAddress>(q => new
            {
                q.CreationTime,
                q.User
            });
            await _host.Connection.UpdateAsync(args);
            return default!;
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(IEnumerable<Guid> list)
        {
            //Remove from the database.
            if (User == null ||
               (!User.IsInRole(GXRoles.Admin) &&
               !User.IsInRole(GXRoles.User)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg;
            if (User.IsInRole(GXRoles.Admin) == true)
            {
                //Admin can see all the IP address.
                arg = GXSelectArgs.SelectAll<GXIpAddress>();
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(User, false);
                arg = GXSelectArgs.SelectAll<GXIpAddress>();
                arg.Joins.AddInnerJoin<GXIpAddress, GXUser>(j => j.User, j => j.Id);
                arg.Where.And<GXUser>(w => w.Id == userId);
            }
            GXDeleteArgs del = GXDeleteArgs.Delete<GXIpAddress>(w => GXSql.Exists(arg));
            await _host.Connection.DeleteAsync(del);
        }
    }
}