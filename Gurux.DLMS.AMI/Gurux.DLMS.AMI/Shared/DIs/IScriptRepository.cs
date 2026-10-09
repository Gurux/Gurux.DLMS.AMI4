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

using System.Linq.Expressions;
using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle scripts.
    /// </summary>
    public interface IScriptRepository
    {
        /// <summary>
        /// Get all users that can access this script.
        /// </summary>
        /// <param name="scriptId">Script id.</param>
        /// <returns>User Ids that can access this script.</returns>
        /// <remarks>
        /// If script is null all the users who can access the scripts are returned.
        /// </remarks>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(Guid scriptId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access this script.
        /// </summary>
        /// <param name="Ids">Script ids.</param>
        /// <returns>User Ids that can access this scripts.</returns>
        /// <remarks>
        /// If script is null all the users who can access the scripts are returned.
        /// </remarks>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(IEnumerable<Guid> Ids, CancellationToken cancellationToken = default);

        /// <summary>
        /// List scripts.
        /// </summary>
        /// <returns>Scripts.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        Task<IEnumerable<GXScript>> ListAsync(
            ListScripts? request = null,
            ListScriptsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read script details.
        /// </summary>
        /// <param name="id">Script id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXScript> ReadAsync(Guid id, Expression<Func<GXScript, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add or update scripts.
        /// </summary>
        /// <param name="scripts">Updated script(s).</param>
        /// <param name="columns">Updated columns.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<IEnumerable<Guid>> UpdateAsync(
            IEnumerable<GXScript> scripts,
            Expression<Func<GXScript, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete script(s).
        /// </summary>
        /// <param name="scripts">Deleted script(s).</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, users are notified about the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteAsync(
            IEnumerable<Guid> scripts,
            bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validate script.
        /// </summary>
        /// <param name="fileName">File name.</param>
        /// <param name="script">Validated script.</param>
        /// <param name="additionalNamespaces">Additional name spaces.</param>
        /// <param name="methods">The methods of the script.</param>
        /// <param name="errorJson">Errors as JSON.</param>
        /// <param name="compileTime">Compile time in ms.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Byte assembly if compile succeeded.</returns>
        byte[]? Compile(
            string fileName,
            string script,
            string? additionalNamespaces,
            List<GXScriptMethod> methods,
            out string? errorJson, out int compileTime, CancellationToken cancellationToken = default);

        /// <summary>
        /// Run script.
        /// </summary>
        /// <param name="methodId">Script method ID to run.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Script output.</returns>
        Task<object?> RunAsync(Guid methodId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Rebuild scripts.
        /// </summary>
        /// <param name="scripts">Rebuild script IDs.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public Task RebuildAsync(IEnumerable<Guid>? scripts, CancellationToken cancellationToken = default);
    }
}
