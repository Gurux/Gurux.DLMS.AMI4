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
using System.Diagnostics;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class UserStampRepository : IUserStampRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly ISystemLogRepository _systemLogRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly IDeviceErrorRepository _deviceErrorRepository;
        private readonly IAgentLogRepository _agentLogRepository;
        private readonly IModuleLogRepository _moduleLogRepository;
        private readonly IScheduleLogRepository _scheduleLogRepository;
        private readonly IGatewayLogRepository _gatewayLogRepository;
        private readonly IScriptLogRepository _scriptLogRepository;
        private readonly IWorkflowLogRepository _workflowLogRepository;
        private readonly IKeyManagementLogRepository _keyManagementLogRepository;


        /// <summary>
        /// Constructor.
        /// </summary>
        public UserStampRepository(
            IGXHost host,
             IGXEventsNotifier eventsNotifier,
             ISystemLogRepository systemLogRepository,
             ITaskRepository taskRepository,
             IDeviceErrorRepository deviceErrorRepository,
             IAgentLogRepository agentLogRepository,
             IModuleLogRepository moduleLogRepository,
             IScheduleLogRepository scheduleLogRepository,
             IGatewayLogRepository gatewayLogRepository,
             IScriptLogRepository scriptLogRepository,
             IWorkflowLogRepository workflowLogRepository,
             IKeyManagementLogRepository keyManagementLogRepository)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _systemLogRepository = systemLogRepository;
            _taskRepository = taskRepository;
            _deviceErrorRepository = deviceErrorRepository;
            _agentLogRepository = agentLogRepository;
            _moduleLogRepository = moduleLogRepository;
            _scheduleLogRepository = scheduleLogRepository;
            _gatewayLogRepository = gatewayLogRepository;
            _scriptLogRepository = scriptLogRepository;
            _workflowLogRepository = workflowLogRepository;
            _keyManagementLogRepository = keyManagementLogRepository;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> stamps)
        {
            if (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.User))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg;
            if (User.IsInRole(GXRoles.Admin))
            {
                arg = GXSelectArgs.Select<GXUserStamp>(a => a.Id, q => stamps.Contains(q.Id));
            }
            else
            {
                GXUser user = new GXUser() { Id = ServerHelpers.GetUserId(User) };
                arg = GXSelectArgs.Select<GXUserStamp>(a => a.Id,
                    q => q.Creator == user && stamps.Contains(q.Id));
            }
            List<GXUserStamp> list = _host.Connection.Select<GXUserStamp>(arg);
            foreach (GXUserStamp it in list)
            {
                await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXUserStamp>(it.Id));
            }
        }

        /// <inheritdoc />
        public async Task<GXUserStamp[]> ListAsync(
            ClaimsPrincipal User,
            ListUserStamps? request,
            ListUserStampsResponse? response,
            CancellationToken cancellationToken)
        {
            GXUser user = new GXUser() { Id = ServerHelpers.GetUserId(User) };
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserStamp>(q => q.Creator == user);
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXUserStamp>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXUserStamp>(w => request.Included.Contains(w.Id));
                }
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXUserStamp>(q => GXSql.DistinctCount(q.Id));
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
                arg.OrderBy.Add<GXUserStamp>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXUserStamp>(q => q.CreationTime);
            }
            GXUserStamp[] stamps = (await _host.Connection.SelectAsync<GXUserStamp>(arg, cancellationToken)).ToArray();

            bool isAdmin = User.IsInRole(GXRoles.Admin);
            foreach (GXUserStamp stamp in stamps)
            {
                if (isAdmin && stamp.TargetType == "SystemLog")
                {
                    //Get system log errors.
                    ListSystemLogs req = new ListSystemLogs()
                    {
                        Filter = new GXSystemLog()
                        {
                            CreationTime = stamp.Updated.GetValueOrDefault().DateTime,
                        },
                        Count = 50,
                        Descending = true
                    };
                    ListSystemLogsResponse ret = new ListSystemLogsResponse();
                    await _systemLogRepository.ListAsync(User, req, ret, cancellationToken);
                    if (ret.Count < 50)
                    {
                        foreach (var it in ret.Errors)
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
                else if ((isAdmin || User.IsInRole(GXRoles.TaskManager)) &&
                    stamp.TargetType == TargetType.Task.ToString())
                {
                    //Get Tasks.
                    ListTasks req = new ListTasks()
                    {
                        Filter = new GXTask()
                        {
                            CreationTime = stamp.Updated.GetValueOrDefault().DateTime,
                        },
                        Count = 50,
                        Descending = true
                    };
                    ListTasksResponse ret = new ListTasksResponse();
                    await _taskRepository.ListAsync(User, req, ret, cancellationToken);
                    if (ret.Count < 50)
                    {
                        stamp.Informational = ret.Count;
                    }
                }
                else if ((isAdmin || User.IsInRole(GXRoles.DeviceLog)) &&
                    stamp.TargetType == TargetType.DeviceError.ToString())
                {
                    //Get device errors.
                    ListDeviceErrors req = new ListDeviceErrors()
                    {
                        Filter = new GXDeviceError()
                        {
                            CreationTime = stamp.Updated.GetValueOrDefault().DateTime,
                        },
                        Count = 50,
                        Descending = true
                    };
                    ListDeviceErrorsResponse ret = new ListDeviceErrorsResponse();
                    await _deviceErrorRepository.ListAsync(User, req, ret, cancellationToken);
                    if (ret.Errors != null)
                    {
                        foreach (var it in ret.Errors)
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
                else if ((isAdmin || User.IsInRole(GXRoles.AgentLog)) &&
                    stamp.TargetType == TargetType.AgentLog.ToString())
                {
                    //Get agent logs.
                    ListAgentLogs req = new ListAgentLogs()
                    {
                        Filter = new GXAgentLog()
                        {
                            CreationTime = stamp.Updated.GetValueOrDefault().DateTime,
                        },
                        Count = 50,
                        Descending = true
                    };
                    ListAgentLogsResponse ret = new ListAgentLogsResponse();
                    await _agentLogRepository.ListAsync(User, req, ret, cancellationToken);
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
                else if ((isAdmin || User.IsInRole(GXRoles.ModuleLog)) &&
                    stamp.TargetType == TargetType.ModuleLog.ToString())
                {
                    //Get module log.
                    ListModuleLogs req = new ListModuleLogs()
                    {
                        Filter = new GXModuleLog()
                        {
                            CreationTime = stamp.Updated.GetValueOrDefault().DateTime,
                        },
                        Count = 50,
                        Descending = true
                    };
                    ListModuleLogsResponse ret = new ListModuleLogsResponse();
                    await _moduleLogRepository.ListAsync(User, req, ret, cancellationToken);
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
                else if ((isAdmin || User.IsInRole(GXRoles.ScheduleLog)) &&
                    stamp.TargetType == TargetType.ScheduleLog.ToString())
                {
                    //Get schedule log.
                    ListScheduleLogs req = new ListScheduleLogs()
                    {
                        Filter = new GXScheduleLog()
                        {
                            CreationTime = stamp.Updated.GetValueOrDefault().DateTime,
                        },
                        Count = 50,
                        Descending = true
                    };
                    ListScheduleLogsResponse ret = new ListScheduleLogsResponse();
                    await _scheduleLogRepository.ListAsync(User, req, ret, cancellationToken);
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
                else if ((isAdmin || User.IsInRole(GXRoles.GatewayLog)) &&
                    stamp.TargetType == TargetType.GatewayLog.ToString())
                {
                    //Get gateway log.
                    ListGatewayLogs req = new ListGatewayLogs()
                    {
                        Filter = new GXGatewayLog()
                        {
                            CreationTime = stamp.Updated.GetValueOrDefault().DateTime,
                        },
                        Count = 50,
                        Descending = true
                    };
                    ListGatewayLogsResponse ret = new ListGatewayLogsResponse();
                    await _gatewayLogRepository.ListAsync(User, req, ret, cancellationToken);
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
                else if ((isAdmin || User.IsInRole(GXRoles.ScriptLog)) &&
                    stamp.TargetType == TargetType.ScriptLog.ToString())
                {
                    //Get module log.
                    ListScriptLogs req = new ListScriptLogs()
                    {
                        Filter = new GXScriptLog()
                        {
                            CreationTime = stamp.Updated.GetValueOrDefault().DateTime,
                        },
                        Count = 50,
                        Descending = true
                    };
                    ListScriptLogsResponse ret = new ListScriptLogsResponse();
                    await _scriptLogRepository.ListAsync(User, req, ret, cancellationToken);
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
                else if ((isAdmin || User.IsInRole(GXRoles.WorkflowLog)) &&
                    stamp.TargetType == TargetType.WorkflowLog.ToString())
                {
                    //Get workflow log.
                    ListWorkflowLogs req = new ListWorkflowLogs()
                    {
                        Filter = new GXWorkflowLog()
                        {
                            CreationTime = stamp.Updated.GetValueOrDefault().DateTime,
                        },
                        Count = 50,
                        Descending = true
                    };
                    ListWorkflowLogsResponse ret = new ListWorkflowLogsResponse();
                    await _workflowLogRepository.ListAsync(User, req, ret, cancellationToken);
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
                else if ((isAdmin || User.IsInRole(GXRoles.KeyManagementLog)) &&
                    stamp.TargetType == TargetType.KeyManagementLog.ToString())
                {
                    //Get key management log.
                    ListKeyManagementLogs req = new ListKeyManagementLogs()
                    {
                        Filter = new GXKeyManagementLog()
                        {
                            CreationTime = stamp.Updated.GetValueOrDefault().DateTime,
                        },
                        Count = 50,
                        Descending = true
                    };
                    ListKeyManagementLogsResponse ret = new ListKeyManagementLogsResponse();
                    await _keyManagementLogRepository.ListAsync(User, req, ret, cancellationToken);
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
            if (response != null)
            {
                response.Stamps = stamps;
            }
            return stamps;
        }

        /// <inheritdoc />
        public async Task UpdateAsync(
            ClaimsPrincipal User,
            IEnumerable<GXUserStamp> stamps)
        {
            GXUser user = new GXUser() { Id = ServerHelpers.GetUserId(User) };
            DateTime now = DateTime.Now;
            foreach (GXUserStamp it in stamps)
            {
                GXSelectArgs arg = GXSelectArgs.SelectAll<GXUserStamp>(q => q.TargetType == it.TargetType);
                arg.Joins.AddInnerJoin<GXUserStamp, GXUser>(j => j.Creator, j => j.Id);
                arg.Where.And<GXUser>(w => w.Id == user.Id);
                arg.Columns.Exclude<GXUserStamp>(e => new { e.Creator, e.CreationTime, e.Target });
                var stamp = (await _host.Connection.SingleOrDefaultAsync<GXUserStamp>(arg));
                if (stamp == null)
                {
                    it.CreationTime = now;
                    it.Creator = user;
                    GXInsertArgs args = GXInsertArgs.Insert(it);
                    args.Exclude<GXUserStamp>(q => q.Updated);
                    _host.Connection.Insert(args);
                }
                else
                {
                    if (!string.IsNullOrEmpty(stamp.ConcurrencyStamp) && stamp.ConcurrencyStamp != it.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    it.Updated = now;
                    it.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(it, c => new
                    {
                        c.Updated,
                        c.ConcurrencyStamp
                    });
                    _host.Connection.Update(args);
                }
            }
            await _eventsNotifier.UserStampUpdate(new string[] { user.Id }, stamps);
        }
    }
}