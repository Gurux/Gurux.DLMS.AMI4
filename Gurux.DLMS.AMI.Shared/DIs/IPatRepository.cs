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

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle personal access tokens.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle personal access tokens.")]
    public interface IPatRepository
    {
        /// <summary>Get personal token by identifier that belongs for the user.</summary>
        /// <param name="id">Token ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Personal token by ID.</returns>
        [System.ComponentModel.Description("Get personal token by identifier that belongs for the user.")]
        Task<GXPersonalToken?> GetPersonalTokenByIdAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get personal tokens that belongs for the user.
        /// </summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Collection of personal tokens.</returns>
        [System.ComponentModel.Description("Get personal tokens that belongs for the user.")]
        Task<IEnumerable<GXPersonalToken>> GetPersonalTokensAsync(ListTokens? request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add new personal access token.
        /// </summary>
        /// <param name="token">Token details to create.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task whose result is the newly generated personal access token value.</returns>
        [System.ComponentModel.Description("Add new personal access token.")]
        Task<string> AddPersonalTokenAsync(GXPersonalToken token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove personal token.
        /// </summary>
        /// <param name="id">Removed token ID</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task whose result describes the removed personal access token.</returns>
        [System.ComponentModel.Description("Remove personal token.")]
        Task<GXPersonalToken> RemovePersonalTokenAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>Extends an active owned token's expiration without changing its value or permissions.</summary>
        [System.ComponentModel.Description("Extends an active owned token's expiration without changing its value or permissions.")]
        Task<GXPersonalToken> ExtendPersonalTokenAsync(string id, DateTimeOffset expiration, CancellationToken cancellationToken = default);
    }
}
