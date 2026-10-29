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
    [System.ComponentModel.Description("This interface is used to handle Views.")]
    public interface IViewRepository
    {
        /// <summary>
        /// List Views.
        /// </summary>
        /// <returns>Views.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List Views.")]
        Task<IEnumerable<GXView>> ListAsync(
            ListViews? request = null,
            ListViewsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read View.
        /// </summary>
        /// <param name="id">View id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested view.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read View.")]
        Task<GXView> ReadAsync(Guid id, Expression<Func<GXView, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Read View by name.
        /// </summary>
        /// <param name="id">View name.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested view.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read View by name.")]
        Task<GXView> ReadAsync(string id, Expression<Func<GXView, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new views or updates existing ones.
        /// </summary>
        /// <param name="Views">The records to insert or update.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Newly inserted view IDs. Updates, resets and no-ops do not add IDs.</returns>
        [System.ComponentModel.Description("Inserts new views or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXView> Views,
            Expression<Func<GXView, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete Views.</summary>
        /// <param name="Views">View(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete Views.</returns>
        [System.ComponentModel.Description("Delete Views.")]
        Task DeleteAsync(IEnumerable<Guid> Views, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access this View.
        /// </summary>
        /// <param name="ViewId">View id.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access this View.")]
        Task<List<string>> GetUsersAsync(Guid? ViewId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access Views.
        /// </summary>
        /// <param name="ViewIds">View ids.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access Views.")]
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? ViewIds, CancellationToken cancellationToken = default);

        /// <summary>Ensure component-defined defaults remain available without changing persisted overrides.
        /// Built-in views use virtual defaults and require no database rows.</summary>
        /// <returns>A task representing completion of the request to ensure component-defined defaults remain available without changing persisted overrides. Built-in views use virtual defaults and require no database rows.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [System.ComponentModel.Description("Ensure component-defined defaults remain available without changing persisted overrides. Built-in views use virtual defaults and require no database rows.")]
        Task EnsureDefaultsAsync(CancellationToken cancellationToken = default);

        /// <summary>Reset selected existing built-in resources in place, preserving identity and references.
        /// The selection must be nonempty and every target must be valid before any writes.</summary>
        /// <returns>A task representing completion of the request to reset selected existing built-in resources in place, preserving identity and references. The selection must be nonempty and every target must be valid before any writes.</returns>
        /// <param name="ids">Identifiers of existing built-in resources to reset. The selection must not be empty.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [System.ComponentModel.Description("Reset selected existing built-in resources in place, preserving identity and references. The selection must be nonempty and every target must be valid before any writes.")]
        Task ResetToDefaultsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    }
}
