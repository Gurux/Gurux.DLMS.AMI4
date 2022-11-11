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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle script logs.
    /// </summary>
    public interface IScriptLogRepository
    {
        /// <summary>
        /// List script logs.
        /// </summary>
        /// <returns>List of script logs.</returns>
        Task<GXScriptLog[]> ListAsync(
            ClaimsPrincipal user, 
            ListScriptLogs? request, 
            ListScriptLogsResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read script log information.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="id">Script log id.</param>
        /// <returns>Script information.</returns>
        Task<GXScriptLog> ReadAsync(ClaimsPrincipal User, Guid id);

        /// <summary>
        /// Clear script logs.
        /// </summary>
        Task ClearAsync(ClaimsPrincipal User, Guid[]? scripts);

        /// <summary>
        /// Add script logs.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="errors">New script logs.</param>
        Task AddAsync(ClaimsPrincipal User, IEnumerable<GXScriptLog> errors);

        /// <summary>
        /// Add new exception.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="script">Script.</param>
        /// <param name="ex">Exception.</param>
        Task<GXScriptLog> AddAsync(ClaimsPrincipal User, GXScript script, Exception ex);

        /// <summary>
        /// Close script log(s).
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="errors">Errors to close.</param>
        Task CloseAsync(ClaimsPrincipal User, IEnumerable<Guid> errors);
    }
}
