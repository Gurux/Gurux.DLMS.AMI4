// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;


namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class UserStampRepository : IUserStampRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Constructor.
        /// </summary>
        public UserStampRepository(
            IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor,
            IServiceProvider serviceProvider)
        {
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            User = httpContextAccessor.HttpContext.User;
            _dbFactory = dbFactory;
            _serviceProvider = serviceProvider;
        }

        /// <summary>Gets or sets the user1.</summary>
        public ClaimsPrincipal User1 => User;

        /// <inheritdoc />
        public async Task DeleteAsync(IEnumerable<Guid> stamps, CancellationToken cancellationToken = default)
        {
            try
            {

                if (!User1.HasClaim("Permission", GXRoles.Admin) && !User1.HasClaim("Permission", GXRoles.Authenticated))
                {
                    throw new UnauthorizedAccessException();
                }
                GXSelectArgs arg;
                if (User1.HasClaim("Permission", GXRoles.Admin))
                {
                    arg = GXSelectArgs.Select<GXUserStamp>(a => a.Id, q => stamps.Contains(q.Id));
                }
                else
                {
                    GXUser user2 = new() { Id = ServerHelpers.GetUserId(User1) };
                    arg = GXSelectArgs.Select<GXUserStamp>(a => a.Id,
                        q => q.Creator == user2 && stamps.Contains(q.Id));
                }
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                List<GXUserStamp> list = connection.Select<GXUserStamp>(arg);
                foreach (GXUserStamp it in list)
                {
                    await connection.DeleteAsync(GXDeleteArgs.DeleteById<GXUserStamp>(it.Id), cancellationToken);
                }

            }
            catch (GXDatabaseException ex)
            {
                if (ex.ErrorType == DatabaseErrorType.Deadlock)
                {
                    //Max time is 5 seconds to avoid infinite retry when there is some other issue that cause deadlock and not just temporary deadlock.
                    CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(5));
                    try
                    {
                        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                        // If deadlock, wait max 1 second and retry.
                        await Task.Delay(1000, linkedCts.Token);
                        await DeleteAsync(stamps, linkedCts.Token);
                    }
                    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                    {
                        throw ex;
                    }
                }
                else
                {
                    throw;
                }
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXUserStamp>> ListAsync(
            ListUserStamps? request,
            ListUserStampsResponse? response,
            CancellationToken cancellationToken = default)
        {
            var systemLogRepository = _serviceProvider.GetRequiredService<ILogRepository>().ForSource("system");
            var taskRepository = _serviceProvider.GetRequiredService<ITaskRepository>();
            var deviceErrorRepository = _serviceProvider.GetRequiredService<ILogRepository>().ForSource("device-error");
            var agentLogRepository = _serviceProvider.GetRequiredService<ILogRepository>().ForSource("agent");
            var moduleLogRepository = _serviceProvider.GetRequiredService<ILogRepository>().ForSource("module");
            var scheduleLogRepository = _serviceProvider.GetRequiredService<ILogRepository>().ForSource("schedule");
            var gatewayLogRepository = _serviceProvider.GetRequiredService<ILogRepository>().ForSource("gateway");
            var scriptLogRepository = _serviceProvider.GetRequiredService<ILogRepository>().ForSource("script");
            var workflowLogRepository = _serviceProvider.GetRequiredService<ILogRepository>().ForSource("workflow");
            var keyManagementLogRepository = _serviceProvider.GetRequiredService<ILogRepository>().ForSource("key-management");
            GXUser user = new() { Id = ServerHelpers.GetUserId(User1) };
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserStamp>(q => q.Creator == user);
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude?.Any() == true)
                {
                    arg.Where.And<GXUserStamp>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included?.Any() == true)
                {
                    arg.Where.And<GXUserStamp>(w => request.Included.Contains(w.Id));
                }
            }
            GXDateTimeFilters.Apply<GXUserStamp>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXUserStamp>(q => GXSql.Count(1));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = await connection.SingleOrDefaultAsync<long>(total, cancellationToken);
                    if (response.Count == 0)
                    {
                        //No need to retrieve data when none is available.
                        return [];
                    }
                }
                arg.Index = request.Index;
                arg.Count = request.Count;
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXUserStamp>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXUserStamp>(q => q.CreationTime!);
            }
            GXUserStamp[] stamps = [.. (await connection.SelectAsync<GXUserStamp>(arg, cancellationToken))];

            bool isAdmin = User1.HasClaim("Permission", GXRoles.Admin);
            foreach (GXUserStamp stamp in stamps)
            {
                if (isAdmin && stamp.TargetType == "SystemLog")
                {
                    //Get system log errors.
                    ListLogs req = new()
                    {
                        Filter = new GXLog()
                        {
                            CreationTime = stamp.Updated,
                        },
                        Count = 50,
                        Descending = true
                    };
                    ListLogsResponse ret = new();
                    await systemLogRepository.ListAsync(req, ret, cancellationToken);
                    if (ret.Logs != null && ret.Count < 50)
                    {
                        foreach (var it in ret.Logs)
                        {
                            switch (it.Level)
                            {
                                case (int)TraceLevel.Error:
                                    ++stamp.Errors;
                                    break;
                                case (int)TraceLevel.Warning:
                                    ++stamp.Warnings;
                                    break;
                                case (int)TraceLevel.Info:
                                    ++stamp.Informational;
                                    break;
                                case (int)TraceLevel.Verbose:
                                    ++stamp.Verboses;
                                    break;
                            }
                        }
                    }
                }
                else if ((isAdmin || User1.HasClaim("Permission", GXTaskPolicies.View)) &&
                    stamp.TargetType == TargetType.Task.ToString())
                {
                    //Get Tasks.
                    ListTasks req = new()
                    {
                        Filter = new GXTask()
                        {
                            CreationTime = stamp.Updated,
                        },
                        Count = 50,
                        Descending = true
                    };
                    ListTasksResponse ret = new();
                    await taskRepository.ListAsync(req, ret, cancellationToken);
                    if (ret.Count < 50)
                    {
                        stamp.Informational = checked((int)ret.Count);
                    }
                }
                else if ((isAdmin || User1.HasClaim("Permission", GXDeviceLogPolicies.View)) &&
                    stamp.TargetType == TargetType.DeviceLog.ToString())
                {
                    //Get device errors.
                    ListLogs req = new()
                    {
                        Filter = new GXLog()
                        {
                            CreationTime = stamp.Updated,
                        },
                        Count = 50,
                        Descending = true
                    };
                    ListLogsResponse ret = new();
                    await deviceErrorRepository.ListAsync(req, ret, cancellationToken);
                    if (ret.Logs != null)
                    {
                        foreach (var it in ret.Logs)
                        {
                            switch (it.Level)
                            {
                                case (int)TraceLevel.Error:
                                    ++stamp.Errors;
                                    break;
                                case (int)TraceLevel.Warning:
                                    ++stamp.Warnings;
                                    break;
                                case (int)TraceLevel.Info:
                                    ++stamp.Informational;
                                    break;
                                case (int)TraceLevel.Verbose:
                                    ++stamp.Verboses;
                                    break;
                            }
                        }
                    }
                }
                else if ((isAdmin || User1.HasClaim("Permission", GXAgentLogPolicies.View)) &&
                    stamp.TargetType == TargetType.AgentLog.ToString())
                {
                    //Get agent logs.
                    ListLogs req = new()
                    {
                        Filter = new GXLog()
                        {
                            CreationTime = stamp.Updated,
                        },
                        Count = 50,
                        Descending = true
                    };
                    ListLogsResponse ret = new();
                    await agentLogRepository.ListAsync(req, ret, cancellationToken);
                    if (ret.Logs != null)
                    {
                        foreach (var it in ret.Logs)
                        {
                            switch (it.Level)
                            {
                                case (int)TraceLevel.Error:
                                    ++stamp.Errors;
                                    break;
                                case (int)TraceLevel.Warning:
                                    ++stamp.Warnings;
                                    break;
                                case (int)TraceLevel.Info:
                                    ++stamp.Informational;
                                    break;
                                case (int)TraceLevel.Verbose:
                                    ++stamp.Verboses;
                                    break;
                            }
                        }
                    }
                }
                else if ((isAdmin || User1.HasClaim("Permission", GXModuleLogPolicies.View)) &&
                    stamp.TargetType == TargetType.ModuleLog.ToString())
                {
                    //Get module log.
                    ListLogs req = new()
                    {
                        Filter = new GXLog()
                        {
                            CreationTime = stamp.Updated,
                        },
                        Count = 50,
                        Descending = true
                    };
                    ListLogsResponse ret = new();
                    await moduleLogRepository.ListAsync(req, ret, cancellationToken);
                    if (ret.Logs != null)
                    {
                        foreach (var it in ret.Logs)
                        {
                            switch (it.Level)
                            {
                                case (int)TraceLevel.Error:
                                    ++stamp.Errors;
                                    break;
                                case (int)TraceLevel.Warning:
                                    ++stamp.Warnings;
                                    break;
                                case (int)TraceLevel.Info:
                                    ++stamp.Informational;
                                    break;
                                case (int)TraceLevel.Verbose:
                                    ++stamp.Verboses;
                                    break;
                            }
                        }
                    }
                }
                else if ((isAdmin || User1.HasClaim("Permission", GXScheduleLogPolicies.View)) &&
                    stamp.TargetType == TargetType.ScheduleLog.ToString())
                {
                    //Get schedule log.
                    ListLogs req = new()
                    {
                        Filter = new GXLog()
                        {
                            CreationTime = stamp.Updated,
                        },
                        Count = 50,
                        Descending = true
                    };
                    ListLogsResponse ret = new();
                    await scheduleLogRepository.ListAsync(req, ret, cancellationToken);
                    if (ret.Logs != null)
                    {
                        foreach (var it in ret.Logs)
                        {
                            switch (it.Level)
                            {
                                case (int)TraceLevel.Error:
                                    ++stamp.Errors;
                                    break;
                                case (int)TraceLevel.Warning:
                                    ++stamp.Warnings;
                                    break;
                                case (int)TraceLevel.Info:
                                    ++stamp.Informational;
                                    break;
                                case (int)TraceLevel.Verbose:
                                    ++stamp.Verboses;
                                    break;
                            }
                        }
                    }
                }
                else if ((isAdmin || User1.HasClaim("Permission", GXGatewayLogPolicies.View)) &&
                    stamp.TargetType == TargetType.GatewayLog.ToString())
                {
                    //Get gateway log.
                    ListLogs req = new()
                    {
                        Filter = new GXLog()
                        {
                            CreationTime = stamp.Updated,
                        },
                        Count = 50,
                        Descending = true
                    };
                    ListLogsResponse ret = new();
                    await gatewayLogRepository.ListAsync(req, ret, cancellationToken);
                    if (ret.Logs != null)
                    {
                        foreach (var it in ret.Logs)
                        {
                            switch (it.Level)
                            {
                                case (int)TraceLevel.Error:
                                    ++stamp.Errors;
                                    break;
                                case (int)TraceLevel.Warning:
                                    ++stamp.Warnings;
                                    break;
                                case (int)TraceLevel.Info:
                                    ++stamp.Informational;
                                    break;
                                case (int)TraceLevel.Verbose:
                                    ++stamp.Verboses;
                                    break;
                            }
                        }
                    }
                }
                else if ((isAdmin || User1.HasClaim("Permission", GXScriptLogPolicies.View)) &&
                    stamp.TargetType == TargetType.ScriptLog.ToString())
                {
                    //Get module log.
                    ListLogs req = new()
                    {
                        Filter = new GXLog()
                        {
                            CreationTime = stamp.Updated,
                        },
                        Count = 50,
                        Descending = true
                    };
                    ListLogsResponse ret = new();
                    await scriptLogRepository.ListAsync(req, ret, cancellationToken);
                    if (ret.Logs != null)
                    {
                        foreach (var it in ret.Logs)
                        {
                            switch (it.Level)
                            {
                                case (int)TraceLevel.Error:
                                    ++stamp.Errors;
                                    break;
                                case (int)TraceLevel.Warning:
                                    ++stamp.Warnings;
                                    break;
                                case (int)TraceLevel.Info:
                                    ++stamp.Informational;
                                    break;
                                case (int)TraceLevel.Verbose:
                                    ++stamp.Verboses;
                                    break;
                            }
                        }
                    }
                }
                else if ((isAdmin || User1.HasClaim("Permission", GXWorkflowLogPolicies.View)) &&
                    stamp.TargetType == TargetType.WorkflowLog.ToString())
                {
                    //Get workflow log.
                    ListLogs req = new()
                    {
                        Filter = new GXLog()
                        {
                            CreationTime = stamp.Updated,
                        },
                        Count = 50,
                        Descending = true
                    };
                    ListLogsResponse ret = new();
                    await workflowLogRepository.ListAsync(req, ret, cancellationToken);
                    if (ret.Logs != null)
                    {
                        foreach (var it in ret.Logs)
                        {
                            switch (it.Level)
                            {
                                case (int)TraceLevel.Error:
                                    ++stamp.Errors;
                                    break;
                                case (int)TraceLevel.Warning:
                                    ++stamp.Warnings;
                                    break;
                                case (int)TraceLevel.Info:
                                    ++stamp.Informational;
                                    break;
                                case (int)TraceLevel.Verbose:
                                    ++stamp.Verboses;
                                    break;
                            }
                        }
                    }
                }
                else if ((isAdmin || User1.HasClaim("Permission", GXKeyManagementLogPolicies.View)) &&
                    stamp.TargetType == TargetType.KeyManagementLog.ToString())
                {
                    //Get key management log.
                    ListLogs req = new()
                    {
                        Filter = new GXLog()
                        {
                            CreationTime = stamp.Updated,
                        },
                        Count = 50,
                        Descending = true
                    };
                    ListLogsResponse ret = new();
                    await keyManagementLogRepository.ListAsync(req, ret, cancellationToken);
                    if (ret.Logs != null)
                    {
                        foreach (var it in ret.Logs)
                        {
                            switch (it.Level)
                            {
                                case (int)TraceLevel.Error:
                                    ++stamp.Errors;
                                    break;
                                case (int)TraceLevel.Warning:
                                    ++stamp.Warnings;
                                    break;
                                case (int)TraceLevel.Info:
                                    ++stamp.Informational;
                                    break;
                                case (int)TraceLevel.Verbose:
                                    ++stamp.Verboses;
                                    break;
                            }
                        }
                    }
                }
            }
            response?.Stamps = stamps;
            return stamps;
        }

        /// <inheritdoc />
        public async Task UpsertAsync(IEnumerable<GXUserStamp> stamps, CancellationToken cancellationToken = default)
        {
            try
            {
                GXUser user = new() { Id = ServerHelpers.GetUserId(User1) };
                DateTime now = DateTime.Now;
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                foreach (GXUserStamp it in stamps)
                {
                    GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserStamp>(q => q.TargetType == it.TargetType);
                    arg.Joins.AddInnerJoin<GXUserStamp, GXUser>(j => j.Creator!, j => j.Id!);
                    arg.Where.And<GXUser>(w => w.Id == user.Id);
                    arg.Columns.Exclude<GXUserStamp>(e => new { e.Creator, e.CreationTime, e.Target });
                    var stamp = (await connection.SingleOrDefaultAsync<GXUserStamp>(arg));
                    if (stamp == null)
                    {
                        it.CreationTime = now;
                        it.Creator = user;
                        GXInsertArgs args = GXInsertArgs.Insert(it);
                        args.Exclude<GXUserStamp>(q => q.Updated!);
                        connection.Insert(args);
                    }
                    else
                    {
                        // Clients can mark a target without knowing the persisted stamp id.
                        it.Id = stamp.Id;
                        //Is's OK if user ConcurrencyStamp has been changed.
                        it.Updated = now;
                        it.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs args = GXUpdateArgs.Update(it, c => new
                        {
                            c.Updated,
                            c.ConcurrencyStamp
                        });
                        connection.Update(args);
                    }
                }
            }
            catch (GXDatabaseException ex)
            {
                if (ex.ErrorType == DatabaseErrorType.Deadlock)
                {
                    //Max time is 5 seconds to avoid infinite retry when there is some other issue that cause deadlock and not just temporary deadlock.
                    CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(5));
                    try
                    {
                        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                        // If deadlock, wait max 1 second and retry.
                        await Task.Delay(1000, linkedCts.Token);
                        await UpsertAsync(stamps, linkedCts.Token);
                    }
                    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                    {
                        throw ex;
                    }
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
