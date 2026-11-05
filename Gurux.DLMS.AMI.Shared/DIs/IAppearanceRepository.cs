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

using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle appearances.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle appearances.")]
    public interface IAppearanceRepository
    {
        /// <summary>
        /// List appearances.
        /// </summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Appearances.</returns>
        [System.ComponentModel.Description("List appearances.")]
        Task<IEnumerable<GXAppearance>> ListAsync(
            ListAppearances? request = null,
            ListAppearancesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read appearance.
        /// </summary>
        /// <param name="type">Appearance type.</param>
        /// <param name="id">Appearance id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested appearance, or <see langword="null"/> when no match is available.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read appearance.")]
        Task<GXAppearance?> ReadAsync(int type, Guid id, Expression<Func<GXAppearance, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Reads a resource by its unique symbolic name.</summary>
        /// <param name="type">Appearance type.</param>
        /// <param name="name">The resource name.</param>
        /// <param name="columns">Read columns.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result is the matching resource, or <see langword="null"/> when no resource has that name.</returns>
        [System.ComponentModel.Description("Reads a resource by its unique symbolic name.")]
        Task<GXAppearance?> ReadByNameAsync(int type, string name, Expression<Func<GXAppearance, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new appearances or updates existing ones.
        /// </summary>
        /// <param name="appearances">The records to insert or update.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new appearances or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXAppearance> appearances,
            Expression<Func<GXAppearance, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>Delete appearances.</summary>
        /// <param name="appearances">Appearance(s) to delete.</param>
        /// <param name="delete">If true, appearances are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete appearances.</returns>
        [System.ComponentModel.Description("Delete appearances.")]
        Task DeleteAsync(IEnumerable<Guid> appearances,
            bool delete = true,
            bool notify = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// When the appearance was last changed.
        /// </summary>
        /// <param name="type">Appearance type</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result is the latest resource change timestamp, or <see langword="null"/> when no version is available.</returns>
        [System.ComponentModel.Description("When the appearance was last changed.")]
        Task<DateTimeOffset?> LastChanged(byte type, CancellationToken cancellationToken = default);

        /// <summary>Create missing built-in appearances without modifying existing rows.</summary>
        /// <returns>A task representing completion of the request to create missing built-in appearances without modifying existing rows.</returns>
        /// <param name="type">The appearance category to restore or populate, or <see langword="null"/> for all supported categories.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [System.ComponentModel.Description("Create missing built-in appearances without modifying existing rows.")]
        Task EnsureDefaultsAsync(int? type, CancellationToken cancellationToken = default);

        /// <summary>Restore selected built-ins in place. Empty selections are rejected.</summary>
        /// <returns>A task representing completion of the request to restore selected built-ins in place. Empty selections are rejected.</returns>
        /// <param name="type">The appearance category to restore or populate, or <see langword="null"/> for all supported categories.</param>
        /// <param name="ids">Identifiers of existing built-in resources to reset. The selection must not be empty.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [System.ComponentModel.Description("Restore selected built-ins in place. Empty selections are rejected.")]
        Task ResetToDefaultsAsync(int? type, IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    }
}
