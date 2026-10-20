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
using Gurux.DLMS.AMI.Shared.DTOs.View;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle Views.
    /// </summary>
    public interface IViewRepository
    {
        /// <summary>
        /// List Views.
        /// </summary>
        /// <returns>Views.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        Task<IEnumerable<GXView>> ListAsync(
            ListViews? request = null,
            ListViewsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read View.
        /// </summary>
        /// <param name="id">View id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXView> ReadAsync(Guid id, Expression<Func<GXView, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Read View by name.
        /// </summary>
        /// <param name="id">View name.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXView> ReadAsync(string id, Expression<Func<GXView, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update View(s).
        /// </summary>
        /// <param name="Views">Updated View(s).</param>
        /// <param name="columns">Updated columns.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<IEnumerable<Guid>> UpdateAsync(
            IEnumerable<GXView> Views,
            Expression<Func<GXView, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete View(s).
        /// </summary>
        /// <param name="Views">View(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteAsync(IEnumerable<Guid> Views, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access this View.
        /// </summary>
        /// <param name="ViewId">View id.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(Guid? ViewId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access Views.
        /// </summary>
        /// <param name="ViewIds">View ids.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? ViewIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Regenerate View(s).
        /// </summary>
        /// <param name="Views">View(s) to regenerate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task RegenerateAsync(IEnumerable<Guid>? Views, CancellationToken cancellationToken = default);
    }
}
