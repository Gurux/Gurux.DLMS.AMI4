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
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.Rest;

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
        Task<GXModuleLog[]> ListAsync(ClaimsPrincipal User, 
            ListModuleLogs? request, 
            ListModuleLogsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read module log information.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="id">Module log id.</param>
        /// <returns>Module error information.</returns>
        Task<GXModuleLog> ReadAsync(ClaimsPrincipal User, Guid id);

        /// <summary>
        /// Clear module log.
        /// </summary>
        Task ClearAsync(ClaimsPrincipal User, string[]? modules);

        /// <summary>
        /// Add module log.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="errors">New log.</param>
        Task AddAsync(ClaimsPrincipal User, IEnumerable<GXModuleLog> errors);

        /// <summary>
        /// Add new exception.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="module">Module.</param>
        /// <param name="ex">Exception.</param>
        Task<GXModuleLog> AddAsync(ClaimsPrincipal User, GXModule module, Exception ex);

        /// <summary>
        /// Close module log(s).
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="errors">Log items to close.</param>
        Task CloseAsync(ClaimsPrincipal User, IEnumerable<Guid> errors);
    }
}
