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

using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle workflows.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle workflows.")]
    public interface IWorkflowRepository
    {
        /// <summary>
        /// List workflows.
        /// </summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        /// <param name="includeActivity">If true, includes workflow activity details.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Workflows.</returns>
        [System.ComponentModel.Description("List workflows.")]
        Task<IEnumerable<GXWorkflow>> ListAsync(
            ListWorkflows? request = null,
            ListWorkflowsResponse? response = null,
            bool includeActivity = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read workflow.
        /// </summary>
        /// <param name="id">Workflow id.</param>
        /// <param name="includeScripts">Are script byte assemblies included.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested workflow.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read workflow.")]
        Task<GXWorkflow> ReadAsync(
            Guid id,
            bool includeScripts,
            Expression<Func<GXWorkflow, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new workflows or updates existing ones.
        /// </summary>
        /// <param name="workflows">The records to insert or update.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new workflows or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXWorkflow> workflows,
            Expression<Func<GXWorkflow, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete workflows.</summary>
        /// <param name="workflows">Workflow(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete workflows.</returns>
        [System.ComponentModel.Description("Delete workflows.")]
        Task DeleteAsync(IEnumerable<Guid> workflows, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access this workflow.
        /// </summary>
        /// <param name="workflowId">Workflow id.</param>
        /// <returns>User Ids that can access this workflow.</returns>
        /// <remarks>
        /// If workflowId is null all the users who can access the workflow are returned.
        /// </remarks>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access this workflow.")]
        Task<List<string>> GetUsersAsync(Guid? workflowId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access given workflows.
        /// </summary>
        /// <param name="Ids">Workflow ids.</param>
        /// <returns>User Ids that can access this workflow.</returns>
        /// <remarks>
        /// If workflowId is null all the users who can access the workflow are returned.
        /// </remarks>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access given workflows.")]
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? Ids, CancellationToken cancellationToken = default);

        /// <summary>
        /// Run the workflow.
        /// </summary>
        /// <param name="id">Workflow id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to run the workflow.</returns>
        [System.ComponentModel.Description("Run the workflow.")]
        Task RunAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
