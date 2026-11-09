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
                var error = tasks.FirstOrDefault(t => !string.IsNullOrEmpty(t.Result))?.Result;
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
