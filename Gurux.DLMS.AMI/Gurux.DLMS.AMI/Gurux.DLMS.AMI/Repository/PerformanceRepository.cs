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
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.Enums;
using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Client.Shared.Enums;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class PerformanceRepository : IPerformanceRepository
    {
        private readonly ClaimsPrincipal? User;
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly GXPerformanceSettings _performanceSettings;
        private readonly IServiceProvider _serviceProvider;

        /// <inheritdoc />
        public Dictionary<string, GXPerformance> Snapshots
        {
            get;
            private set;
        } = new Dictionary<string, GXPerformance>();

        /// <summary>
        /// Constructor.
        /// </summary>
        public PerformanceRepository(IGXHost host,
            IGXAmiContextAccessor contextAccessor,
            IGXEventsNotifier eventsNotifier,
            IServiceProvider serviceProvider,
            GXPerformanceSettings performanceSettings)
        {
            _serviceProvider = serviceProvider;
            User = contextAccessor?.User;
            _host = host;
            _eventsNotifier = eventsNotifier;
            _performanceSettings = performanceSettings;
        }

        /// <inheritdoc />
        public async Task ClearAsync()
        {
            if (User == null ||
                !User.IsInRole(GXRoles.Admin))
            {
                throw new UnauthorizedAccessException();
            }

            lock (Snapshots)
            {
                Snapshots.Clear();
            }
            _host.Connection.Truncate<GXPerformance>();
            using IServiceScope scope = _serviceProvider.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var users = await userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]);
            await _eventsNotifier.PerformanceClear(users);
        }


        /// <inheritdoc />
        public async Task<GXPerformance[]> ListAsync(
            ListPerformances? request,
            ListPerformancesResponse? response,
            CancellationToken cancellationToken)
        {
            if (User == null ||
                !User.IsInRole(GXRoles.Admin))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXPerformance>();
            arg.Distinct = true;
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
            }
            if (request != null && request.Count != 0)
            {
                if (request.Count != 0)
                {
                    lock (Snapshots)
                    {
                        request.Index -= Snapshots.Count;
                        request.Count -= Snapshots.Count;
                    }
                    if (request.Index < 0)
                    {
                        request.Index = 0;
                    }
                    if (request.Count < 0)
                    {
                        request.Count = 0;
                    }
                }
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXPerformance>(q => GXSql.DistinctCount(q.Id));
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
                arg.OrderBy.Add<GXPerformance>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXPerformance>(q => q.Start);
            }
            List<GXPerformance> performances = (await _host.Connection.SelectAsync<GXPerformance>(arg)).ToList();
            lock (Snapshots)
            {
                if (Snapshots.Any())
                {
                    if (response != null)
                    {
                        response.Count += Snapshots.Count;
                    }
                    //Add snapshot values.
                    if (arg.Descending)
                    {
                        performances.InsertRange(0, Snapshots.Values);
                    }
                    else
                    {
                        performances.AddRange(Snapshots.Values);
                    }
                    if (!string.IsNullOrEmpty(request?.Filter?.Target))
                    {
                        //Remove snapshots using the filter.
                        performances.RemoveAll(w => w.Target != request.Filter.Target);
                    }
                }
            }
            if (response != null)
            {
                response.Performances = performances.ToArray();
                if (response.Count == 0)
                {
                    response.Count = performances.Count;
                }
            }
            return performances.ToArray();
        }

        /// <inheritdoc />
        public async Task<Guid[]> AddAsync(
            IEnumerable<GXPerformance> performances)
        {
            if (User == null ||
                !User.IsInRole(GXRoles.Admin))
            {
                throw new UnauthorizedAccessException();
            }
            List<Guid> list = new();
            foreach (GXPerformance it in performances)
            {
                await _host.Connection.InsertAsync(GXInsertArgs.Insert(it));
                list.Add(it.Id);
            }
            var userRepository = _serviceProvider.GetRequiredService<IUserRepository>();
            var users = await userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]);
            await _eventsNotifier.PerformanceAdd(users, performances);
            return list.ToArray();
        }

        /// <inheritdoc />
        public async Task DeleteAsync(
            IEnumerable<Guid> performances)
        {
            if (User == null ||
                !User.IsInRole(GXRoles.Admin))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXPerformance>(a => a.Id, q => performances.Contains(q.Id));
            List<GXPerformance> list = _host.Connection.Select<GXPerformance>(arg);
            DateTime now = DateTime.Now;
            foreach (GXPerformance it in list)
            {
                await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXPerformance>(it.Id));
            }
            var userRepository = _serviceProvider.GetRequiredService<IUserRepository>();
            var users = await userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]);
            users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                users, TargetType.Performance, NotificationAction.Remove);
            if (users != null && users.Any())
            {
                await _eventsNotifier.PerformanceDelete(users, performances);
            }
        }
    }
}
