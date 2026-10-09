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
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
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
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        /// <returns>Blocks.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<IEnumerable<GXComponentView>> ListAsync(
            ListComponentViews? request = null,
            ListComponentViewsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read component view.
        /// </summary>
        /// <param name="id">Component view ID.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXComponentView> ReadAsync(Guid id, Expression<Func<GXComponentView, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update component view(s).
        /// </summary>
        /// <param name="componentviews">Updated component view(s).</param>
        /// <param name="columns">Updated columns.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<IEnumerable<Guid>> UpdateAsync(
            IEnumerable<GXComponentView> componentviews,
            Expression<Func<GXComponentView, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete component view(s).
        /// </summary>
        /// <param name="componentviews">Component view(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteAsync(IEnumerable<Guid> componentviews, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access this component view.
        /// </summary>
        /// <param name="componentViewId">Component view id.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(Guid? componentViewId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access component views.
        /// </summary>
        /// <param name="Ids">Component view ids.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? Ids, CancellationToken cancellationToken = default);

        /// <summary>
        /// Refresh component view(s).
        /// </summary>
        Task<bool> RefrestAsync(CancellationToken cancellationToken);
    }
}
