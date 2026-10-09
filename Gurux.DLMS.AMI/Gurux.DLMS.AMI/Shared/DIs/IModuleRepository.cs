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

using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle modules.
    /// </summary>
    public interface IModuleRepository
    {
        /// <summary>
        /// List modules.
        /// </summary>
        /// <returns>Modules.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        Task<IEnumerable<GXModule>> ListAsync(
            ListModules? request = null,
            ListModulesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// List modules.
        /// </summary>
        /// <returns>Modules.</returns>
        Task<IEnumerable<GXModule>> ListWithVersionsAsync();

        /// <summary>
        /// Read module.
        /// </summary>
        /// <param name="id">Module id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXModule> ReadAsync(string id, Expression<Func<GXModule, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update module.
        /// </summary>
        /// <param name="module">Updated module.</param>
        /// <param name="columns">Updated columns.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task UpdateAsync(
            GXModule module,
            Expression<Func<GXModule, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add new module.
        /// </summary>
        /// <param name="modules">Added modules.</param>
        Task AddAsync(IEnumerable<GXModule> modules);

        /// <summary>
        /// Delete module(s).
        /// </summary>
        /// <param name="modules">Module(s) to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteAsync(IEnumerable<string> modules, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access this module.
        /// </summary>
        /// <param name="moduleId">Module id.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(string? moduleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access modules.
        /// </summary>
        /// <param name="moduleIds">Module ids.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(IEnumerable<string>? moduleIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all scripts that belong for the given module.
        /// </summary>
        /// <param name="moduleId">Module id.</param>
        /// <returns>Operation result.</returns>
        Task<List<GXScript>> GetScriptsAsync(string? moduleId);
    }
}
