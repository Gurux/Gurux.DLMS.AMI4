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
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// Provides persistence operations for log type definitions.
    /// </summary>
    [System.ComponentModel.Description("Provides persistence operations for log type definitions.")]
    public interface ILogTypeRepository
    {
        /// <summary>Adds missing built-in log types without changing existing types or access policies.</summary>
        /// <returns>A task representing completion of the request to add missing built-in log types without changing existing types or access policies.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        Task EnsureDefaultsAsync(CancellationToken cancellationToken = default);

        /// <summary>Restores selected built-in log types and their default access policies, preserving identifiers and logs.</summary>
        /// <param name="ids">Public identifiers of the built-in log types to reset.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A task representing completion of the request to restore selected built-in log types and their default access policies, preserving identifiers and logs.</returns>
        Task ResetToDefaultsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

        /// <summary>
        /// List log types.
        /// </summary>
        /// <returns>Log types.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List log types.")]
        Task<IEnumerable<GXLogType>> ListAsync(
            ListLogTypes? request = null,
            ListLogTypesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read log type details.
        /// </summary>
        /// <param name="id">The log type PublicId GUID.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested log type.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read log type details.")]
        Task<GXLogType> ReadAsync(Guid id, Expression<Func<GXLogType, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new log types or updates existing ones.
        /// </summary>
        /// <param name="types">The records to insert or update.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new log types or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXLogType> types,
            Expression<Func<GXLogType, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes log type definitions.
        /// </summary>
        /// <param name="types">PublicId GUIDs of log types to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete log type definitions.</returns>
        [System.ComponentModel.Description("Deletes log type definitions.")]
        Task DeleteAsync(IEnumerable<Guid> types, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

    }
}
