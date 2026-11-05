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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle users.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle users.")]
    public interface IUserRepository
    {
        /// <summary>
        /// List users.
        /// </summary>
        /// <returns>Users.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List users.")]
        Task<IEnumerable<GXUser>> ListAsync(
            ListUsers? request = null,
            ListUsersResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read user.
        /// </summary>
        /// <param name="id">User id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>User information.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read user.")]
        Task<GXUser> ReadAsync(string? id, Expression<Func<GXUser, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new users or updates existing ones.
        /// </summary>
        /// <param name="users">Updated users.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new users or updates existing ones.")]
        Task<IEnumerable<string>> UpsertAsync(
            IEnumerable<GXUser> users,
            Expression<Func<GXUser, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete users.</summary>
        /// <param name="users">Users to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete users.</returns>
        [System.ComponentModel.Description("Delete users.")]
        Task DeleteAsync(IEnumerable<string> users, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns distinct identifiers of non-removed users assigned to any requested, non-removed role.
        /// </summary>
        /// <remarks>
        /// Administrators can query any role. Other callers can query only roles assigned to them.
        /// Role names are matched without regard to case.
        /// </remarks>
        /// <param name="roles">Role names to look up. An empty collection returns no users.</param>
        /// <returns>Distinct identifiers of the users assigned to accessible requested roles.</returns>
        [System.ComponentModel.Description("Return users that are in the given role.")]
        Task<List<string>> GetUserIdsInRoleAsync(IEnumerable<string> roles);
    }
}
