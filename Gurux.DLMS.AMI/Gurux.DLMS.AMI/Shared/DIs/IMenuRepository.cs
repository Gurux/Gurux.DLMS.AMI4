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
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle menus.
    /// </summary>
    public interface IMenuRepository
    {
        /// <summary>
        /// List menus.
        /// </summary>
        /// <returns>Menus.</returns>
        Task<GXMenu[]> ListAsync(
            ListMenus? request,
            ListMenusResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read menu.
        /// </summary>
        /// <param name="id">Menu id.</param>
        /// <returns></returns>
        Task<GXMenu> ReadAsync(Guid id);

        /// <summary>
        /// Read menu by name.
        /// </summary>
        /// <param name="id">Menu name.</param>
        /// <returns></returns>
        Task<GXMenu> ReadAsync(string id);

        /// <summary>
        /// Update menu(s).
        /// </summary>
        /// <param name="menus">Updated menu(s).</param>
        /// <param name="columns">Updated columns(s).</param>
        Task<Guid[]> UpdateAsync(
            IEnumerable<GXMenu> menus,
            Expression<Func<GXMenu, object?>>? columns = null);

        /// <summary>
        /// Delete menu(s).
        /// </summary>
        /// <param name="menus">Menu(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        Task DeleteAsync(IEnumerable<Guid> menus, bool delete);

        /// <summary>
        /// Get all users that can access this menu.
        /// </summary>
        /// <param name="menuId">Menu id.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(Guid? menuId);

        /// <summary>
        /// Get all users that can access menus.
        /// </summary>
        /// <param name="menuIds">Menu ids.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? menuIds);
    }
}
