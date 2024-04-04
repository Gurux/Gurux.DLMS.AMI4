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
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Client.Shared;
using System.Diagnostics;
using Gurux.DLMS.AMI.Shared.DTOs.Report;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    class ReportLogRepository : IReportLogRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IReportRepository _reportRepository;
        private readonly IUserRepository _userRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ReportLogRepository(IGXHost host,
            IGXEventsNotifier eventsNotifier,
            IReportRepository reportRepository,
            IUserRepository userRepository)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _reportRepository = reportRepository;
            _userRepository = userRepository;
        }

        /// <inheritdoc />
        public async Task AddAsync(ClaimsPrincipal User, IEnumerable<GXReportLog> errors)
        {
            DateTime now = DateTime.Now;
            Dictionary<GXReportLog, List<string>> updates = new Dictionary<GXReportLog, List<string>>();
            foreach (GXReportLog it in errors)
            {
                it.CreationTime = now;
                updates[it] = await _reportRepository.GetUsersAsync(User, it.Report.Id);
            }
            await _host.Connection.InsertAsync(GXInsertArgs.InsertRange(errors));
            foreach (var it in updates)
            {
                GXReportLog tmp = new GXReportLog()
                {
                    Id = it.Key.Id,
                    CreationTime = now,
                    Level = it.Key.Level,
                };
                await _eventsNotifier.AddReportLogs(it.Value, new GXReportLog[] { tmp });
            }
        }

        /// <inheritdoc />
        public async Task<GXReportLog> AddAsync(ClaimsPrincipal User, GXReport report, Exception ex)
        {
            DateTime now = DateTime.Now;
            Dictionary<GXReportLog, List<string>> updates = new Dictionary<GXReportLog, List<string>>();
            GXReportLog error = new GXReportLog(TraceLevel.Error)
            {
                CreationTime = now,
                Message = ex.Message,
                Report = report
            };
            await _host.Connection.InsertAsync(GXInsertArgs.Insert(error));
            updates[error] = await _reportRepository.GetUsersAsync(User, report.Id);
            foreach (var it in updates)
            {
                await _eventsNotifier.AddReportLogs(it.Value, new GXReportLog[] { it.Key });
            }
            return error;
        }

        /// <inheritdoc />
        public async Task ClearAsync(ClaimsPrincipal User, Guid[]? reports)
        {
            if (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.ReportLogManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg;
            bool admin = User.IsInRole(GXRoles.Admin);
            if (!admin)
            {
                string id = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetReportsByUser(id);
            }
            else
            {
                arg = GXSelectArgs.SelectAll<GXReport>(w => w.Removed == null);
            }
            arg.Distinct = true;
            arg.Columns.Clear();
            arg.Columns.Add<GXReport>(s => new { s.Id, s.Name });
            arg.Columns.Exclude<GXReport>(s => s.Logs);
            arg.Joins.AddInnerJoin<GXReport, GXReportLog>(y => y.Id, x => x.Report);
            if (reports != null && reports.Any())
            {
                arg.Where.And<GXReport>(w => reports.Contains(w.Id));
            }
            List<GXReport>? logs = await _host.Connection.SelectAsync<GXReport>(arg);
            List<string> list = await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin });
            //Notify users if their actions are cleared.
            list.AddDistinct(await _reportRepository.GetUsersAsync(User, reports));
            if (admin && (reports == null || !reports.Any()))
            {
                //Admin clears all report logs.
                _host.Connection.Truncate<GXReportLog>();
                logs = null;
            }
            else if (logs.Any())
            {
                GXDeleteArgs args = GXDeleteArgs.Delete<GXReportLog>(w => logs.Contains(w.Report));
                await _host.Connection.DeleteAsync(args);
            }
            await _eventsNotifier.ClearReportLogs(list, logs);
        }

        /// <inheritdoc />
        public async Task CloseAsync(ClaimsPrincipal User, IEnumerable<Guid> errors)
        {
            if (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.ReportLogManager))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXReportLog>(where => errors.Contains(where.Id));
            arg.Columns.Add<GXReport>(s => new { s.Id, s.Name });
            arg.Columns.Exclude<GXReport>(s => s.Logs);
            arg.Joins.AddInnerJoin<GXReportLog, GXReport>(j => j.Report, j => j.Id);
            List<GXReportLog> list = (await _host.Connection.SelectAsync<GXReportLog>(arg));
            Dictionary<GXReportLog, List<string>> updates = new Dictionary<GXReportLog, List<string>>();
            foreach (var it in list)
            {
                updates[it] = await _reportRepository.GetUsersAsync(User, it.Report.Id);
            }
            DateTime now = DateTime.Now;
            foreach (GXReportLog it in list)
            {
                it.Closed = now;
            }
            if (list.Count != 0)
            {
                await _host.Connection.UpdateAsync(GXUpdateArgs.UpdateRange(list, q => q.Closed));
                foreach (var it in updates)
                {
                    await _eventsNotifier.CloseReportLogs(it.Value, new GXReportLog[] { it.Key });
                }
            }
        }

        /// <inheritdoc />
        public async Task<GXReportLog[]> ListAsync(
            ClaimsPrincipal user,
            ListReportLogs? request,
            ListReportLogsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && user.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the errors.
                arg = GXSelectArgs.SelectAll<GXReportLog>();
                arg.Joins.AddInnerJoin<GXReportLog, GXReport>(j => j.Report, j => j.Id);
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetReportErrorsByUser(userId, null);
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXReportLog>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXReportLog>(w => request.Included.Contains(w.Id));
                }
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXReportLog>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = await _host.Connection.SingleOrDefaultAsync<int>(total, cancellationToken);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            arg.Columns.Add<GXReport>(c => new { c.Id, c.Name });
            arg.Columns.Exclude<GXReport>(e => e.Logs);
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXReportLog>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXReportLog>(q => q.CreationTime);
            }
            GXReportLog[] logs = (await _host.Connection.SelectAsync<GXReportLog>(arg, cancellationToken)).ToArray();
            if (response != null)
            {
                response.Logs = logs;
                if (response.Count == 0)
                {
                    response.Count = logs.Length;
                }
            }
            return logs;
        }

        /// <inheritdoc />
        public async Task<GXReportLog> ReadAsync(ClaimsPrincipal User, Guid id)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXReportLog>(where => where.Id == id);
            //Get report.
            arg.Columns.Add<GXReport>();
            arg.Joins.AddInnerJoin<GXReportLog, GXReport>(x => x.Report, y => y.Id);
            //Errors are ignored from the report
            //so there is no reference relation that is causing problems with JSON parser.
            arg.Columns.Exclude<GXReport>(e => e.Logs);
            GXReportLog error = (await _host.Connection.SingleOrDefaultAsync<GXReportLog>(arg));
            if (error == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            return error;
        }
    }
}
