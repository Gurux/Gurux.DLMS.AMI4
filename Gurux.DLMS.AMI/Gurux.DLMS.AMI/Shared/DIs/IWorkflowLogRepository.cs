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
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle workflow logs.
    /// </summary>
    public interface IWorkflowLogRepository
    {
        /// <summary>
        /// List workflow logs.
        /// </summary>
        /// <returns>List of workflow logs.</returns>
        Task<GXWorkflowLog[]> ListAsync(
            ClaimsPrincipal User, 
            ListWorkflowLogs? request, 
            ListWorkflowLogsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read workflow log information.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="id">Workflow log id.</param>
        /// <returns>Workflow information.</returns>
        Task<GXWorkflowLog> ReadAsync(ClaimsPrincipal User, Guid id);

        /// <summary>
        /// Clear workflow logs.
        /// </summary>
        Task ClearAsync(ClaimsPrincipal User, IEnumerable<Guid>? workflows);

        /// <summary>
        /// Add workflow log.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="errors">New log.</param>
        Task AddAsync(ClaimsPrincipal User, IEnumerable<GXWorkflowLog> errors);

        /// <summary>
        /// Add new exception.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="workflow">Workflow.</param>
        /// <param name="ex">Exception.</param>
        Task<GXWorkflowLog> AddAsync(ClaimsPrincipal User, GXWorkflow workflow, Exception ex);

        /// <summary>
        /// Close workflow log(s).
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="errors">Logs to close.</param>
        Task CloseAsync(ClaimsPrincipal User, IEnumerable<Guid> errors);
    }
}
