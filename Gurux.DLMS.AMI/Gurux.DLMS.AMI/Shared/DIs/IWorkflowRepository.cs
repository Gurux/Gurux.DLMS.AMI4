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

using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle workflows.
    /// </summary>
    public interface IWorkflowRepository
    {
        /// <summary>
        /// List workflows.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="includeActivity"></param>
        /// <returns>Workflows.</returns>
        Task<GXWorkflow[]> ListAsync(
            ListWorkflows? request,
            ListWorkflowsResponse? response,
            bool includeActivity);

        /// <summary>
        /// Read workflow.
        /// </summary>
        /// <param name="id">Workflow id.</param>
        /// <param name="includeScripts">Are script byte assemblies included.</param>
        /// <returns></returns>
        Task<GXWorkflow> ReadAsync(
            Guid id,
            bool includeScripts);

        /// <summary>
        /// Update workflow(s).
        /// </summary>
        /// <param name="workflows">Updated workflow(s).</param>
        /// <param name="columns">Updated columns(s).</param>
        Task<Guid[]> UpdateAsync(
            IEnumerable<GXWorkflow> workflows,
            Expression<Func<GXWorkflow, object?>>? columns = null);

        /// <summary>
        /// Delete workflow(s).
        /// </summary>
        /// <param name="workflows">Workflow(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        Task DeleteAsync(IEnumerable<Guid> workflows, bool delete);

        /// <summary>
        /// Get all users that can access this workflow.
        /// </summary>
        /// <param name="workflowId">Workflow id.</param>
        /// <returns>User Ids that can access this workflow.</returns>
        /// <remarks>
        /// If workflowId is null all the users who can access the workflow are returned.
        /// </remarks>
        Task<List<string>> GetUsersAsync(Guid? workflowId);

        /// <summary>
        /// Get all users that can access given workflows.
        /// </summary>
        /// <param name="workflowIds">Workflow ids.</param>
        /// <returns>User Ids that can access this workflow.</returns>
        /// <remarks>
        /// If workflowId is null all the users who can access the workflow are returned.
        /// </remarks>
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? workflowIds);

        /// <summary>
        /// Run the workflow.
        /// </summary>
        /// <param name="id">Workflow id.</param>
        Task RunAsync(Guid id);
    }
}
