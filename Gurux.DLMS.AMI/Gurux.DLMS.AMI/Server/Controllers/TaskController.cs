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
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Mvc;
using Gurux.DLMS.AMI.Shared.DIs;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Server.Models;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the tasks.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly ITaskRepository _taskRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public TaskController(ITaskRepository taskRepository)
        {
            _taskRepository = taskRepository;
        }

        /// <summary>
        /// List Tasks
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXTaskPolicies.View)]
        public async Task<ActionResult<ListTasksResponse>> Post(
            ListTasks request, 
            CancellationToken cancellationToken)
        {
            ListTasksResponse ret = new ListTasksResponse();
            await _taskRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// New task is added.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Add")]
        [Authorize(Policy = GXTaskPolicies.Add)]
        public async Task<ActionResult<AddTaskResponse>> Post(AddTask request)
        {
            if (request.Tasks == null || request.Tasks.Length == 0)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            AddTaskResponse ret = new AddTaskResponse();
            ret.TaskIds = await _taskRepository.AddAsync(User, request.Tasks);
            return ret;
        }

        /// <summary>
        /// Get next task to execute.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Next")]
        [Authorize(Policy = GXTaskPolicies.Edit)]
        public async Task<ActionResult<GetNextTaskResponse>> Post(GetNextTask request)
        {
            GetNextTaskResponse ret = new GetNextTaskResponse();
            ret.Tasks = await _taskRepository.GetNextAsync(User, request.AgentId, request.DeviceId, request.Listener);
            return ret;
        }

        /// <summary>
        /// Task is done.
        /// </summary>
        [HttpPost("Done")]
        [Authorize(Policy = GXTaskPolicies.Edit)]
        public async Task<ActionResult<TaskDoneResponse>> Post(TaskDone request)
        {
            if (request.Tasks == null)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _taskRepository.DoneAsync(User, request.Tasks);
            return new TaskDoneResponse();
        }

        /// <summary>
        /// Restart tasks.
        /// </summary>
        [HttpPost("Restart")]
        [Authorize(Policy = GXTaskPolicies.Edit)]
        public async Task<ActionResult<TaskRestartResponse>> Post(TaskRestart request)
        {
            if (request.Tasks == null)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _taskRepository.RestartAsync(User, request.Tasks);
            return new TaskRestartResponse();
        }
        
        [HttpPost("Delete")]
        [Authorize(Policy = GXTaskPolicies.Delete)]
        public async Task<ActionResult<DeleteTaskResponse>> Post(DeleteTask request)
        {
            if (request.Ids == null)
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _taskRepository.DeleteAsync(User, request.Ids);
            return new DeleteTaskResponse();
        }

        [HttpPost("Clear")]
        [Authorize(Policy = GXTaskPolicies.Delete)]
        public async Task<ActionResult<ClearTaskResponse>> Post(ClearTask request)
        {
            await _taskRepository.ClearAsync(User);
            return new ClearTaskResponse();
        }
    }
}
