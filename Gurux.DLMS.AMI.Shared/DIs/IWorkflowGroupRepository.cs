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
    /// This interface is used to hanfle workflow group.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to hanfle workflow group.")]
    public interface IWorkflowGroupRepository
    {
        /// <summary>
        /// List workflow groups.
        /// </summary>
        /// <returns>User groups.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List workflow groups.")]
        Task<IEnumerable<GXWorkflowGroup>> ListAsync(
            ListWorkflowGroups? request = null,
            ListWorkflowGroupsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read workflow group details.
        /// </summary>
        /// <param name="id">Workflow group id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested workflow group.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read workflow group details.")]
        Task<GXWorkflowGroup> ReadAsync(Guid id, Expression<Func<GXWorkflowGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new workflow groups or updates existing ones.
        /// </summary>
        /// <param name="groups">Updated workflow groups.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new workflow groups or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXWorkflowGroup> groups,
            Expression<Func<GXWorkflowGroup, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete workflow groups.</summary>
        /// <param name="groups">Workflow groups to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete workflow groups.</returns>
        [System.ComponentModel.Description("Delete workflow groups.")]
        Task DeleteAsync(IEnumerable<Guid> groups, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns workflow groups list where workflow belongs.
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <returns>List of workflow groups.</returns>
        [System.ComponentModel.Description("Returns workflow groups list where workflow belongs.")]
        Task<List<GXWorkflowGroup>> GetJoinedWorkflowGroups(Guid workflowId);
    }
}
