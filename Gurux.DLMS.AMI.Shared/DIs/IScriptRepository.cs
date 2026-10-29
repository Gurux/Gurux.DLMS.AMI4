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
    [System.ComponentModel.Description("This interface is used to handle scripts.")]
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
        [System.ComponentModel.Description("Get all users that can access this script.")]
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
        [System.ComponentModel.Description("Get all users that can access this script.")]
        Task<List<string>> GetUsersAsync(IEnumerable<Guid> Ids, CancellationToken cancellationToken = default);

        /// <summary>
        /// List scripts.
        /// </summary>
        /// <returns>Scripts.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List scripts.")]
        Task<IEnumerable<GXScript>> ListAsync(
            ListScripts? request = null,
            ListScriptsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read script details.
        /// </summary>
        /// <param name="id">Script id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested script.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read script details.")]
        Task<GXScript> ReadAsync(Guid id, Expression<Func<GXScript, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new scripts or updates existing ones.
        /// </summary>
        /// <param name="scripts">The records to insert or update.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new scripts or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXScript> scripts,
            Expression<Func<GXScript, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete scripts.</summary>
        /// <param name="scripts">Deleted script(s).</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, users are notified about the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete scripts.</returns>
        [System.ComponentModel.Description("Delete scripts.")]
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
        [System.ComponentModel.Description("Validate script.")]
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
        [System.ComponentModel.Description("Run script.")]
        Task<object?> RunAsync(Guid methodId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Rebuild scripts.
        /// </summary>
        /// <param name="scripts">Rebuild script IDs.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when the selected scripts have been rebuilt.</returns>
        [System.ComponentModel.Description("Rebuild scripts.")]
        public Task RebuildAsync(IEnumerable<Guid>? scripts, CancellationToken cancellationToken = default);
    }
}
