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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.Enums;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class RestStatisticRepository : IRestStatisticRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public RestStatisticRepository(IGXHost host,
            IGXEventsNotifier eventsNotifier,
            IUserRepository userRepository)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
        }


        /// <inheritdoc />
        public async Task ClearAsync(ClaimsPrincipal user)
        {
            if (!user.IsInRole(GXRoles.Admin))
            {
                throw new UnauthorizedAccessException();
            }
            _host.Connection.Truncate<GXRestStatistic>();
            var users = await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin });
            await _eventsNotifier.RestStatisticClear(users, null);
        }


        /// <inheritdoc />
        public async Task<GXRestStatistic[]> ListAsync(
            ClaimsPrincipal user,
            ListRestStatistics? request,
            ListRestStatisticsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXRestStatistic>();
            arg.Distinct = true;
            GXUser u = new GXUser() { Id = ServerHelpers.GetUserId(user) };
            arg.Where.And<GXRestStatistic>(q => q.User == u);
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXRestStatistic>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXRestStatistic>(w => request.Included.Contains(w.Id));
                }
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXRestStatistic>(q => GXSql.DistinctCount(q.Id));
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
                arg.OrderBy.Add<GXRestStatistic>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXRestStatistic>(q => q.Start);
            }
            GXRestStatistic[] statistics = (await _host.Connection.SelectAsync<GXRestStatistic>(arg)).ToArray();
            if (response != null)
            {
                response.Statistics = statistics;
                if (response.Count == 0)
                {
                    response.Count = statistics.Length;
                }
            }
            return statistics;
        }

        /// <inheritdoc />
        public async Task<Guid[]> AddAsync(ClaimsPrincipal user, IEnumerable<GXRestStatistic> statistics)
        {
            List<Guid> list = new();
            foreach (GXRestStatistic it in statistics)
            {
                await _host.Connection.InsertAsync(GXInsertArgs.Insert(it));
                list.Add(it.Id);
            }
            var users = await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin });
            await _eventsNotifier.RestStatisticAdd(users, statistics);
            return list.ToArray();
        }
    }
}
