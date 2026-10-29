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

using MenuKind = Gurux.DLMS.AMI.Shared.DTOs.Enums.MenuKind;
using Gurux.DLMS.AMI.Shared.Enums;

using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle menus.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle menus.")]
    public interface IMenuRepository
    {
        /// <summary>
        /// List menus.
        /// </summary>
        /// <returns>Menus.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List menus.")]
        Task<IEnumerable<GXMenu>> ListAsync(
            ListMenus? request = null,
            ListMenusResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read menu.
        /// </summary>
        /// <param name="id">Menu id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested menu.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read menu.")]
        Task<GXMenu> ReadAsync(Guid id, Expression<Func<GXMenu, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Read menu by name.
        /// </summary>
        /// <param name="id">Menu name.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested menu.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read menu by name.")]
        Task<GXMenu> ReadAsync(string id, Expression<Func<GXMenu, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new menus or updates existing ones.
        /// </summary>
        /// <param name="menus">The records to insert or update.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new menus or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXMenu> menus,
            Expression<Func<GXMenu, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete menus.</summary>
        /// <param name="menus">Menu(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete menus.</returns>
        [System.ComponentModel.Description("Delete menus.")]
        Task DeleteAsync(IEnumerable<Guid> menus, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access this menu.
        /// </summary>
        /// <param name="menuId">Menu id.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access this menu.")]
        Task<List<string>> GetUsersAsync(Guid? menuId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access menus.
        /// </summary>
        /// <param name="menuIds">Menu ids.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access menus.")]
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? menuIds, CancellationToken cancellationToken = default);

        /// <summary>Inserts missing built-in menus of the requested kind, preserving existing rows.</summary>
        /// <returns>A task representing completion of the request to insert missing built-in menus of the requested kind, preserving existing rows.</returns>
        /// <param name="kind">The menu kind whose built-in definitions are used.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [System.ComponentModel.Description("Inserts missing built-in menus of the requested kind, preserving existing rows.")]
        Task EnsureDefaultsAsync(MenuKind kind, CancellationToken cancellationToken = default);

        /// <summary>Resets selected built-in menus in place. An empty selection is rejected.</summary>
        /// <returns>A task representing completion of the request to reset selected built-in menus in place. An empty selection is rejected.</returns>
        /// <param name="kind">The menu kind whose built-in definitions are used.</param>
        /// <param name="menus">Identifiers of existing built-in menus to reset. The selection must not be empty.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [System.ComponentModel.Description("Resets selected built-in menus in place. An empty selection is rejected.")]
        Task ResetToDefaultsAsync(MenuKind kind, IEnumerable<Guid> menus, CancellationToken cancellationToken = default);
    }
}
