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
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle user stamps.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle user stamps.")]
    public interface IUserStampRepository
    {
        /// <summary>
        /// List user stamps.
        /// </summary>
        /// <returns>List of user stamps.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List user stamps.")]
        Task<IEnumerable<GXUserStamp>> ListAsync(
            ListUserStamps? request = null,
            ListUserStampsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete user stamps.
        /// </summary>
        /// <param name="userStamps">User stamps.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete user stamps.</returns>
        [System.ComponentModel.Description("Delete user stamps.")]
        Task DeleteAsync(IEnumerable<Guid> userStamps, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new user stamps or refreshes existing stamps for the current user.
        /// </summary>
        /// <param name="userStamps">The user stamps to insert or refresh, matched by target type for the current user.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to insert new user stamps or refresh existing stamps for the current user.</returns>
        [System.ComponentModel.Description("Inserts new user stamps or refreshes existing stamps for the current user.")]
        Task UpsertAsync(IEnumerable<GXUserStamp> userStamps, CancellationToken cancellationToken = default);
    }
}
