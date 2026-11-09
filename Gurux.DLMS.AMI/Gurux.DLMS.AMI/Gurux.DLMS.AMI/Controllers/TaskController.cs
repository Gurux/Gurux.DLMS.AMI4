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

using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the tasks.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class TaskController(ITaskRepository taskRepository) : ControllerBase
    {
        private readonly ITaskRepository _taskRepository = taskRepository;

        /// <summary>
        /// List Tasks
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXTaskPolicies.View)]
        public async Task<ActionResult<ListTasksResponse>> Post(
            ListTasks request,
            CancellationToken cancellationToken)
        {
            ListTasksResponse ret = new();
            await _taskRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// New task is added.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Add")]
        [Authorize(Policy = GXTaskPolicies.Add)]
        public async Task<ActionResult<AddTaskResponse>> Post(AddTask request, CancellationToken cancellationToken = default)
        {
            if (request.Tasks == null || !request.Tasks.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            // Schedule correlation is host-owned; API callers cannot impersonate a schedule initiator.
            foreach (var task in request.Tasks)
            {
                task.WorkflowOperationId = null;
                task.WorkflowActorId = null;
            }
            AddTaskResponse ret = new()
            {
                Ids = await _taskRepository.AddAsync(request.Tasks, cancellationToken)
            };
            return ret;
        }

        /// <summary>
        /// Get next task to execute.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Next")]
        [Authorize(Policy = GXTaskPolicies.Edit)]
        public async Task<ActionResult<GetNextTaskResponse>> Post(GetNextTask request, CancellationToken cancellationToken = default)
        {
            GetNextTaskResponse ret = new()
            {
                Tasks = await _taskRepository.GetNextAsync(
                    ServerHelpers.GetRemoteIpAddress(Request, HttpContext),
                    request.AgentId,
                    request.DeviceId,
                    request.GatewayId,
                    request.Listener, cancellationToken)
            };
            return ret;
        }

        /// <summary>
        /// Task is done.
        /// </summary>
        [HttpPost("Done")]
        [Authorize(Policy = GXTaskPolicies.Edit)]
        public async Task<ActionResult<TaskDoneResponse>> Post(TaskDone request, CancellationToken cancellationToken = default)
        {
            if (request.Tasks == null)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _taskRepository.DoneAsync(request.Tasks, cancellationToken);
            return new TaskDoneResponse();
        }

        /// <summary>
        /// Restart tasks.
        /// </summary>
        [HttpPost("Restart")]
        [Authorize(Policy = GXTaskPolicies.Edit)]
        public async Task<ActionResult<TaskRestartResponse>> Post(TaskRestart request, CancellationToken cancellationToken = default)
        {
            if (request.Tasks == null)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _taskRepository.RestartAsync(request.Tasks, cancellationToken);
            return new TaskRestartResponse();
        }

        /// <summary>Handles the delete task request.</summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Delete")]
        [Authorize(Policy = GXTaskPolicies.Delete)]
        public async Task<ActionResult<DeleteTaskResponse>> Post(DeleteTask request, CancellationToken cancellationToken = default)
        {
            if (request.Ids == null)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _taskRepository.DeleteAsync(request.Ids, cancellationToken);
            return new DeleteTaskResponse();
        }

        /// <summary>Handles the clear task request.</summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Clear")]
        [Authorize(Policy = GXTaskPolicies.Clear)]
        public async Task<ActionResult<ClearTaskResponse>> Post(ClearTask request, CancellationToken cancellationToken = default)
        {
            _ = request;
            await _taskRepository.ClearAsync(cancellationToken);
            return new ClearTaskResponse();
        }
    }
}
