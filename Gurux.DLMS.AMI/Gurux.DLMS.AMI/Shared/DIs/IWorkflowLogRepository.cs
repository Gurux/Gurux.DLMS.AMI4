using Gurux.DLMS.AMI.Shared.DTOs.Log;
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

using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Linq.Expressions;

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
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        Task<IEnumerable<GXLog>> ListAsync(
            ListWorkflowLogs? request = null,
            ListWorkflowLogsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read workflow log details.
        /// </summary>
        /// <param name="id">Workflow log id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Workflow information.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXLog> ReadAsync(Guid id, Expression<Func<GXLog, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clear workflow logs.
        /// </summary>
        /// <param name="workflows">Workflows to clear.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task ClearAsync(IEnumerable<Guid>? workflows, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add workflow log.
        /// </summary>
        /// <param name="type">Log type.</param>
        /// <param name="logs">New log items.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task AddAsync(string type, IEnumerable<GXLog> logs, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add new exception.
        /// </summary>
        /// <param name="type">Log type.</param>
        /// <param name="workflow">Workflow.</param>
        /// <param name="ex">Exception.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXLog> AddAsync(string type, GXWorkflow workflow, Exception ex, CancellationToken cancellationToken = default);

        /// <summary>
        /// Close workflow log(s).
        /// </summary>
        /// <param name="errors">Logs to close.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task CloseAsync(IEnumerable<Guid> errors, CancellationToken cancellationToken = default);
    }
}
