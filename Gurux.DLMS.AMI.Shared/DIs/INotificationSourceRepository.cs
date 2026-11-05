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

using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// Lists registered notification sources and manages their server-wide delivery settings.
    /// </summary>
    [System.ComponentModel.Description("Lists registered notification sources and manages their server-wide delivery settings.")]
    public interface INotificationSourceRepository
    {
        /// <summary>Lists registered notification sources matching the request.</summary>
        /// <param name="request">Optional filtering, ordering, and paging parameters.</param>
        /// <param name="response">Optional response to populate with the selected sources and total matching count before paging.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the selected notification sources, including their delivery settings.</returns>
        [System.ComponentModel.Description("Lists registered notification sources and their server-wide delivery settings.")]
        Task<IEnumerable<GXNotificationSource>> ListAsync(
            ListNotificationSources? request = null,
            ListNotificationSourcesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>Enables or disables server-wide delivery for the specified registered sources.</summary>
        /// <remarks>
        /// Requires permission to manage server-wide notification settings.
        /// Changes only the delivery setting; source names and producer ownership are preserved.
        /// The implementation must refresh the server notification cache after successful persistence.
        /// Enabling a source does not bypass recipient authorization or subscription and delivery channel settings.
        /// </remarks>
        /// <param name="ids">Identifiers of the registered notification sources to update.</param>
        /// <param name="enabled">Whether delivery is permitted for the selected sources.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the settings update.</returns>
        [System.ComponentModel.Description("Enables or disables server-wide delivery for selected registered notification sources.")]
        Task SetEnabledAsync(
            IEnumerable<int> ids,
            bool enabled,
            CancellationToken cancellationToken = default);
    }
}
