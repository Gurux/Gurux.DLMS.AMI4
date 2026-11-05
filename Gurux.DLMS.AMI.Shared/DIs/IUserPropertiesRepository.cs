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
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle user properties.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle user properties.")]
    public interface IUserPropertiesRepository
    {
        /// <summary>
        /// List user properties.
        /// </summary>
        /// <returns>List of user properties.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List user properties.")]
        Task<IEnumerable<GXUserProperty>> ListAsync(
            ListUserProperties? request = null,
            ListUserPropertiesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read user properties.
        /// </summary>
        /// <param name="name">Property name.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>User property.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="userId">Property owner; defaults to the current user. Another owner requires administrator access.</param>
        [System.ComponentModel.Description("Read user properties.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068", Justification = "Preserves the existing public repository signature and positional callers.")]
        Task<GXUserProperty?> ReadAsync(string name, Expression<Func<GXUserProperty, object>>? columns = null, CancellationToken cancellationToken = default, string? userId = null);

        /// <summary>
        /// Inserts new user properties or updates existing ones.
        /// </summary>
        /// <param name="settings">The user properties to insert or update, matched by owner and name. Module-owned properties must be saved through the module interface.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to insert new user properties or update existing ones.</returns>
        [System.ComponentModel.Description("Inserts new user properties or updates existing ones.")]
        Task UpsertAsync(
            IEnumerable<GXUserProperty> settings,
            Expression<Func<GXUserProperty, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete user settings.
        /// </summary>
        /// <param name="ids">User settings to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete user settings.</returns>
        [System.ComponentModel.Description("Delete user settings.")]
        Task DeleteAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    }
}
