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

using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// Performance information.
    /// </summary>
    [System.ComponentModel.Description("Performance information.")]
    public interface IPerformanceRepository
    {
        /// <summary>
        /// Performance list.
        /// </summary>
        /// <param name="User">Calling user.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Performances.</returns>
        [System.ComponentModel.Description("Performance list.")]
        Task<IEnumerable<GXPerformance>> ListAsync(
            ClaimsPrincipal User,
            ListPerformances? request = null,
            ListPerformancesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Add new performances.
        /// </summary>
        /// <param name="User">The user performing the operation.</param>
        /// <param name="performances">Added performances.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the added records.</returns>
        [System.ComponentModel.Description("Add new performances.")]
        Task<IEnumerable<Guid>> AddAsync(ClaimsPrincipal User, IEnumerable<GXPerformance> performances,
            CancellationToken cancellationToken = default);

        /// <summary>Delete performances.</summary>
        /// <param name="User">The user performing the operation.</param>
        /// <param name="performances">Performance(s) to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete performances.</returns>
        [System.ComponentModel.Description("Delete performances.")]
        Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> performances, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clear performances.
        /// </summary>
        /// <param name="User">The user performing the operation.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to clear performances.</returns>
        [System.ComponentModel.Description("Clear performances.")]
        Task ClearAsync(ClaimsPrincipal User, CancellationToken cancellationToken = default);

        /// <summary>
        /// Snapshot values.
        /// </summary>
        [System.ComponentModel.Description("Snapshot values.")]
        Dictionary<string, GXPerformance> Snapshots
        {
            get;
        }
    }
}
