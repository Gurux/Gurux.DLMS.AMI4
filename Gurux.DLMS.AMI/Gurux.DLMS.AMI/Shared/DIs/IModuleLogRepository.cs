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

using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle module log.
    /// </summary>
    public interface IModuleLogRepository
    {
        /// <summary>
        /// List module errors.
        /// </summary>
        /// <returns>List of module log.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        Task<IEnumerable<GXLog>> ListAsync(
            ListModuleLogs? request = null,
            ListModuleLogsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read module log information.
        /// </summary>
        /// <param name="id">Module log id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Module error information.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXLog> ReadAsync(Guid id, Expression<Func<GXLog, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Clear module log.
        /// </summary>
        /// <param name="modules">Modules to clear.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task ClearAsync(IEnumerable<string>? modules, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add module log.
        /// </summary>
        /// <param name="type">Module log type.</param>
        /// <param name="errors">New log.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task AddAsync(string type, IEnumerable<GXLog> errors, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add new exception.
        /// </summary>
        /// <param name="type">Module log type.</param>
        /// <param name="module">Module.</param>
        /// <param name="ex">Exception.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXLog> AddAsync(string type, GXModule module, Exception ex, CancellationToken cancellationToken = default);

        /// <summary>
        /// Close module log(s).
        /// </summary>
        /// <param name="errors">Log items to close.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task CloseAsync(IEnumerable<Guid> errors, CancellationToken cancellationToken = default);
    }
}
