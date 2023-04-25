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
    /// This interface is used to handle component views.
    /// </summary>
    public interface IComponentViewRepository
    {
        /// <summary>
        /// List component views.
        /// </summary>
        /// <returns>Blocks.</returns>
        Task<GXComponentView[]> ListAsync(
            ClaimsPrincipal User, 
            ListComponentViews? request, 
            ListComponentViewsResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read component view.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="id">Component view ID.</param>
        /// <returns></returns>
        Task<GXComponentView> ReadAsync(ClaimsPrincipal user, Guid id);

        /// <summary>
        /// Update component view(s).
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="componentviews">Updated component view(s).</param>
        /// <param name="columns">Updated columns(s).</param>
        Task<Guid[]> UpdateAsync(
            ClaimsPrincipal User, 
            IEnumerable<GXComponentView> componentviews,
            Expression<Func<GXComponentView, object?>>? columns = null);

        /// <summary>
        /// Delete component view(s).
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="componentviews">Component view(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> componentviews, bool delete);

        /// <summary>
        /// Get all users that can access this component view.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="componentViewId">Component view id.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal user, Guid? componentViewId);
        /// <summary>
        /// Get all users that can access component views.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="componentViewIds">Component view ids.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal user, IEnumerable<Guid>? componentViewIds);

        /// <summary>
        /// Refresh component view(s).
        /// </summary>
        Task<bool> RefrestAsync(ClaimsPrincipal User);
    }
}
