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
// This file is a part of Gurux Script Framework.
//
// Gurux Script Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Script Framework is distributed in the hope that it will be useful,
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
    public interface IScriptRepository
    {
        /// <summary>
        /// Get all users that can access this script.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="scriptId">Script id.</param>
        /// <returns>User Ids that can access this script.</returns>
        /// <remarks>
        /// If script is null all the users who can access the scripts are returned.
        /// </remarks>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, Guid scriptId);

        /// <summary>
        /// Get all users that can access this script.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="scriptIds">Script ids.</param>
        /// <returns>User Ids that can access this scripts.</returns>
        /// <remarks>
        /// If script is null all the users who can access the scripts are returned.
        /// </remarks>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, IEnumerable<Guid> scriptIds);

        /// <summary>
        /// List scripts.
        /// </summary>
        /// <returns>Scripts.</returns>
        Task<GXScript[]> ListAsync(
            ClaimsPrincipal User,
            ListScripts? request,
            ListScriptsResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read script information.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="id">Script id.</param>
        /// <returns></returns>
        Task<GXScript> ReadAsync(ClaimsPrincipal User, Guid id);

        /// <summary>
        /// Add or update scripts.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="scripts">Updated script(s).</param>
        Task<Guid[]> UpdateAsync(
            ClaimsPrincipal User,
            IEnumerable<GXScript> scripts);

        /// <summary>
        /// Delete script(s).
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="scripts">Deleted script(s).</param>
        Task DeleteAsync(
            ClaimsPrincipal User,
            IEnumerable<Guid> scripts);

        /// <summary>
        /// Validate script.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="fileName">File name.</param>
        /// <param name="script">Validated script.</param>
        /// <param name="additionalNamespaces">Additional name spaces.</param>
        /// <param name="methods">The methods of the script.</param>
        /// <param name="errorJson">Errors as JSON.</param>
        /// <param name="compileTime">Compile time in ms.</param>
        /// <returns>Byte assembly if compile succeeded.</returns>
        public byte[]? Compile(ClaimsPrincipal User,
            string fileName,
            string script,
            string? additionalNamespaces,
            List<GXScriptMethod> methods,
            out string? errorJson, out int compileTime);

        /// <summary>
        /// Run script.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="methodId">Script method ID to run.</param>
        /// <returns>Script output.</returns>
        public Task<object?> RunAsync(ClaimsPrincipal User, Guid methodId);
    }
}
