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

using System.Data;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Server.Repository;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Services;

/// <summary>Completes a generic schedule operation once all its persisted tasks have finished.</summary>
internal static class GXScheduleTaskCompletion
{
    /// <summary>Finishes a schedule after all tasks complete, retaining failures from claims made before an upgrade.</summary>
    /// <param name="factory">Provides task and workflow database connections.</param>
    /// <param name="workflows">Publishes the completed schedule operation.</param>
    /// <param name="task">Completed task identifying the schedule operation.</param>
    /// <param name="cancellationToken">Cancels database access and gate acquisition.</param>
    /// <returns>A task completing after the schedule state is persisted and published.</returns>
    /// <exception cref="KeyNotFoundException">The schedule operation is unavailable.</exception>
    /// <exception cref="InvalidOperationException">The task does not belong to the schedule operation.</exception>
    internal static async Task CompleteAsync(IDbConnectionFactory factory, IWorkflowHandler workflows,
        GXTask task, CancellationToken cancellationToken)
    {
        if (task.TriggerSchedule == null || task.WorkflowOperationId == null) {
            return;
        }
        await WorkflowOperationRepository.Gate.WaitAsync(cancellationToken);
        try
        {
            Guid operationId = task.WorkflowOperationId.Value;
            string target = task.TriggerSchedule.Id.ToString();
            await using var connection = await factory.GetConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var operation = await connection.SingleOrDefaultAsync<GXWorkflowOperation>(transaction,
                GXSelectArgs.SelectAll<GXWorkflowOperation>(o => o.Id == operationId), cancellationToken)
                ?? throw new KeyNotFoundException("The schedule operation is unavailable.");
            if (operation.Code != "schedule.run" || operation.TargetId != target || operation.ActorId != task.WorkflowActorId)
                {
                throw new InvalidOperationException("The task does not belong to this schedule operation.");
            }
            if (operation.State == WorkflowOperationState.Running)
            {
                var tasks = await connection.SelectAsync<GXTask>(transaction,
                    GXSelectArgs.SelectAll<GXTask>(t => t.WorkflowOperationId == operationId), cancellationToken);
                if (tasks.Count == 0 || tasks.Any(t => t.Ready == null)) {
                    return;
                }
                var error = tasks.FirstOrDefault(t => !string.IsNullOrEmpty(t.Error))?.Error;
                if (error == null)
                {
                    // A schedule can span an upgrade. Only a failed operation for the
                    // current claim proves that an old Result is an error rather than output.
                    var legacy = tasks.Where(t => t.Start != null && !string.IsNullOrEmpty(t.Result))
                        .ToDictionary(TaskRepository.TaskOperationKey);
                    if (legacy.Count != 0)
                    {
                        var keys = legacy.Keys.ToArray();
                        var failures = await connection.SelectAsync<GXWorkflowOperation>(transaction,
                            GXSelectArgs.SelectAll<GXWorkflowOperation>(o =>
                                keys.Contains(o.IdempotencyKey) && o.State == WorkflowOperationState.Failed), cancellationToken);
                        var failure = failures.FirstOrDefault();
                        if (failure != null)
                        {
                            error = legacy[failure.IdempotencyKey!].Result;
                        }
                    }
                }
                operation = await WorkflowOperationRepository.FinishRecordAsync(connection, transaction, operationId,
                    error == null ? WorkflowOperationState.Completed : WorkflowOperationState.Failed, null,
                    error?[..Math.Min(error.Length, 2048)], cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
            // Publication derives the actor from the operation, never from the reporting agent.
            await WorkflowOperationRepository.PublishPendingAsync(connection, workflows, operation);
        }
        finally { WorkflowOperationRepository.Gate.Release(); }
    }
}
