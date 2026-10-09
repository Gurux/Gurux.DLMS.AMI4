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

using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This public interface is used with device actions.
    /// </summary>
    public interface ILogRepository
    {
        /// <summary>
        /// Add logs.
        /// </summary>
        /// <param name="type">Log type.</param>
        /// <param name="logs">Updated logs.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<IEnumerable<Guid>> AddAsync(
            string type,
            IEnumerable<GXLog> logs,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// List log entries.
        /// </summary>
        /// <returns>Log entries.</returns>
        Task<IEnumerable<GXLog>> ListAsync(
            ListLogs? request = null,
            ListLogsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read log entry.
        /// </summary>
        /// <param name="id">Log id.</param>
        /// <param name="columns">Read columns.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Operation result.</returns>
        Task<GXLog> ReadAsync(Guid id,
            Expression<Func<GXLog, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete log entries.
        /// </summary>
        /// <param name="logs">Deleted logs.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteAsync(IEnumerable<Guid>? logs, bool delete = true, bool notify = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Close log entries.
        /// </summary>
        /// <param name="logs">Logs to close.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task CloseAsync(IEnumerable<Guid>? logs, CancellationToken cancellationToken);

        /// <summary>
        /// Clear all log entries.
        /// </summary>
        /// <param name="groups">Deleted log groups.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task ClearAsync(IEnumerable<Guid>? groups, CancellationToken cancellationToken);
    }
}
