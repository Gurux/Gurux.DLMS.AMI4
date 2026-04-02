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

using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle system log.
    /// </summary>
    public interface ISystemLogRepository
    {
        /// <summary>
        /// List system logs.
        /// </summary>
        /// <param name="request">Request filter.</param>
        /// <param name="response">Response filter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of system logs.</returns>
        Task<GXSystemLog[]> ListAsync(
            ListSystemLogs? request,
            ListSystemLogsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read system log information.
        /// </summary>
        /// <param name="id">System log id.</param>
        /// <returns>System log information.</returns>
        Task<GXSystemLog> ReadAsync(Guid id);

        /// <summary>
        /// Clear system logs.
        /// </summary>
        Task ClearAsync();

        /// <summary>
        /// Add system logs.
        /// </summary>
        /// <param name="type">Log type.</param>
        /// <param name="logs">New logs.</param>
        Task AddAsync(string type, IEnumerable<GXSystemLog> logs);

        /// <summary>
        /// Add new exception.
        /// </summary>
        /// <param name="type">Log type.</param>
        /// <param name="ex">Occurred exception.</param>
        Task<GXSystemLog> AddAsync(string type, Exception ex);

        /// <summary>
        /// Close system log(s).
        /// </summary>
        /// <param name="logs">Logs to close.</param>
        Task CloseAsync(IEnumerable<Guid>? logs);

        /// <summary>
        /// Delete system logs items.
        /// </summary>
        /// <param name="logs">Deleted logs.</param>
        Task DeleteAsync(IEnumerable<GXSystemLog>? logs);
    }
}
