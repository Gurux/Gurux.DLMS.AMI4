// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle modules.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle modules.")]
    public interface IModuleRepository
    {
        /// <summary>
        /// List modules.
        /// </summary>
        /// <returns>Modules.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List modules.")]
        Task<IEnumerable<GXModule>> ListAsync(
            ListModules? request = null,
            ListModulesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// List modules.
        /// </summary>
        /// <returns>Modules.</returns>
        [System.ComponentModel.Description("List modules.")]
        Task<IEnumerable<GXModule>> ListWithVersionsAsync();

        /// <summary>
        /// Read module.
        /// </summary>
        /// <param name="id">Module id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested module.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read module.")]
        Task<GXModule> ReadAsync(string id, Expression<Func<GXModule, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing module. This operation does not install or insert a missing module.
        /// </summary>
        /// <param name="module">The existing module and the values to update.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to update an existing module. This operation does not install or insert a missing module.</returns>
        [System.ComponentModel.Description("Updates an existing module. This operation does not install or insert a missing module.")]
        Task UpsertAsync(
            GXModule module,
            Expression<Func<GXModule, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add new module.
        /// </summary>
        /// <param name="modules">Added modules.</param>
        /// <returns>A task representing completion of the request to add new module.</returns>
        [System.ComponentModel.Description("Add new module.")]
        Task AddAsync(IEnumerable<GXModule> modules);

        /// <summary>Delete modules.</summary>
        /// <param name="modules">Module(s) to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete modules.</returns>
        [System.ComponentModel.Description("Delete modules.")]
        Task DeleteAsync(IEnumerable<string> modules, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access this module.
        /// </summary>
        /// <param name="moduleId">Module id.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access this module.")]
        Task<List<string>> GetUsersAsync(string? moduleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access modules.
        /// </summary>
        /// <param name="moduleIds">Module ids.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access modules.")]
        Task<List<string>> GetUsersAsync(IEnumerable<string>? moduleIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all scripts that belong for the given module.
        /// </summary>
        /// <param name="moduleId">Module id.</param>
        /// <returns>A task whose result contains the scripts belonging to the specified module.</returns>
        [System.ComponentModel.Description("Get all scripts that belong for the given module.")]
        Task<List<GXScript>> GetScriptsAsync(string? moduleId);
    }
}
