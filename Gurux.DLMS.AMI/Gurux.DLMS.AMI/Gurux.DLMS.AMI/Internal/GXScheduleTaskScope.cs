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

using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Validates task origins against a schedule invocation established only by the server.</summary>
internal static class GXScheduleTaskScope
{
    /// <summary>Identifies the schedule and durable operation allowed in the current asynchronous flow.</summary>
    private static readonly AsyncLocal<(Guid ScheduleId, Guid OperationId)?> _current = new();

    /// <summary>Runs task creation with the identity of a trusted scheduler invocation.</summary>
    /// <typeparam name="T">Result returned by task creation.</typeparam>
    /// <param name="scheduleId">Schedule whose tasks may be created.</param>
    /// <param name="operationId">Durable operation to which those tasks must belong.</param>
    /// <param name="action">Creates tasks while normal repository authorization remains active.</param>
    /// <returns>The result of task creation.</returns>
    internal static async Task<T> RunAsync<T>(Guid scheduleId, Guid operationId, Func<Task<T>> action)
    {
        var previous = _current.Value;
        _current.Value = (scheduleId, operationId);
        try
        {
            return await action();
        }
        finally
        {
            _current.Value = previous;
        }
    }

    /// <summary>Rejects forged origins and attributes accepted tasks to the executing user.</summary>
    /// <param name="task">Task whose submitted origin is checked before expansion or queuing.</param>
    /// <param name="userId">Authenticated user whose task and target permissions are checked by the repository.</param>
    /// <exception cref="GXAmiForbiddenException">The task claims an origin outside the trusted schedule invocation.</exception>
    internal static void SetTriggerUser(GXTask task, string userId)
    {
        if (task.TriggerScript != null || (task.TriggerSchedule != null &&
            (_current.Value is not { } scope || scope.ScheduleId == Guid.Empty || scope.OperationId == Guid.Empty ||
             scope.ScheduleId != task.TriggerSchedule.Id || scope.OperationId != task.WorkflowOperationId)))
        {
            throw new GXAmiForbiddenException();
        }
        task.TriggerUser = new GXUser { Id = userId };
    }
}