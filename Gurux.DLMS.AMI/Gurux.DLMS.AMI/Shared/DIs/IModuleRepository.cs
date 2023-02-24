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
using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs;
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
        Task<GXModule[]> ListAsync(
            ClaimsPrincipal User, 
            ListModules? request, 
            ListModulesResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// List modules.
        /// </summary>
        /// <returns>Modules.</returns>
        Task<GXModule[]> ListWithVersionsAsync(ClaimsPrincipal User);

        /// <summary>
        /// Read module.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="id">Module id.</param>
        /// <returns></returns>
        Task<GXModule> ReadAsync(ClaimsPrincipal User, string id);

        /// <summary>
        /// Update module.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="module">Updated module.</param>
        /// <param name="columns">Updated columns(s).</param>
        Task UpdateAsync(
            ClaimsPrincipal user, 
            GXModule module,
            Expression<Func<GXModule, object?>>? columns = null);

        /// <summary>
        /// Add new module.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="modules">Added modules.</param>
        Task AddAsync(ClaimsPrincipal User, IEnumerable<GXModule> modules);

        /// <summary>
        /// Delete module(s).
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="modules">Module(s) to delete.</param>
        Task DeleteAsync(ClaimsPrincipal User, IEnumerable<string> modules);

        /// <summary>
        /// Get all users that can access this module.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="moduleId">Module id.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, string? moduleId);

        /// <summary>
        /// Get all users that can access modules.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="moduleIds">Module ids.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, IEnumerable<string>? moduleIds);

        /// <summary>
        /// Get all scripts that belong for the given module.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="moduleId">Module id.</param>
        /// <returns></returns>
        Task<List<GXScript>> GetScriptsAsync(ClaimsPrincipal User, string? moduleId);
    }
}
