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

using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>Records, queries, and removes workflow operation state.</summary>
    /// <remarks>Implementations must validate access and apply lifecycle transitions atomically.</remarks>
    [System.ComponentModel.Description("Records, queries, and removes workflow operation state.")]
    public interface IWorkflowOperationRepository
    {
        /// <summary>Records the start of an operation in the running state.</summary>
        /// <param name="operation">The operation with its stable identifier, code, target, and optional parent and execution data.</param>
        /// <param name="cancellationToken">Token used to cancel the request.</param>
        /// <returns>A task representing the asynchronous start operation.</returns>
        /// <remarks>
        /// Repeating a start with the same identifier and matching initial data must not create another operation
        /// or republish acknowledged events. Conflicting initial data must be rejected. An existing terminal operation
        /// must not be restarted. The initiating actor must be resolved from trusted server context.
        /// </remarks>
        [System.ComponentModel.Description("Records the start of a workflow operation.")]
        Task StartAsync(GXWorkflowOperation operation, CancellationToken cancellationToken = default);

        /// <summary>Records the terminal outcome of a running operation.</summary>
        /// <param name="operationId">Identifier of the operation to finish.</param>
        /// <param name="outcome">The completed, failed, or cancelled state. Running is not a valid outcome.</param>
        /// <param name="errorCode">Optional diagnostic code associated with the outcome.</param>
        /// <param name="errorMessage">Optional diagnostic message associated with the outcome.</param>
        /// <param name="cancellationToken">Token used to cancel the request.</param>
        /// <returns>A task representing the asynchronous finish operation.</returns>
        /// <remarks>
        /// Repeating the same terminal outcome and diagnostic details must not republish acknowledged events.
        /// Conflicting outcomes or diagnostic details must be rejected. The original actor and operation identity
        /// must be preserved. Cancelling this request does not itself cancel the underlying operation.
        /// </remarks>
        [System.ComponentModel.Description("Records the terminal outcome of a workflow operation.")]
        Task FinishAsync(
            Guid operationId,
            WorkflowOperationState outcome,
            string? errorCode,
            string? errorMessage,
            CancellationToken cancellationToken = default);

        /// <summary>Lists workflow operations visible to the current caller.</summary>
        /// <param name="request">Optional filtering, ordering, and paging parameters.</param>
        /// <param name="response">Optional response to populate with the matching operations and total count before paging.</param>
        /// <param name="cancellationToken">Token used to cancel the request.</param>
        /// <returns>The matching operations after applying access restrictions and paging.</returns>
        [System.ComponentModel.Description("Lists workflow operations visible to the current caller.")]
        Task<IEnumerable<GXWorkflowOperation>> ListAsync(
            ListWorkflowOperations? request = null,
            ListWorkflowOperationsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>Reads a workflow operation visible to the current caller.</summary>
        /// <param name="id">The operation identifier, corresponding to GXWorkflowOperation.Id.</param>
        /// <param name="columns">Properties to read, or null to use the default selection.</param>
        /// <param name="cancellationToken">Token used to cancel the request.</param>
        /// <returns>The requested workflow operation.</returns>
        [System.ComponentModel.Description("Reads a workflow operation visible to the current caller.")]
        Task<GXWorkflowOperation> ReadAsync(Guid id, Expression<Func<GXWorkflowOperation, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Permanently deletes selected terminal workflow operations.</summary>
        /// <param name="operations">Identifiers corresponding to GXWorkflowOperation.Id.</param>
        /// <param name="cancellationToken">Token used to cancel the request.</param>
        /// <returns>A task representing the asynchronous deletion.</returns>
        /// <remarks>
        /// Running operations must not be deleted. Deletion removes the state used to recognize repeated lifecycle
        /// requests; retention must therefore account for the supported retry period.
        /// </remarks>
        [System.ComponentModel.Description("Permanently deletes selected terminal workflow operations.")]
        Task DeleteAsync(IEnumerable<Guid> operations, CancellationToken cancellationToken = default);
    }
}
