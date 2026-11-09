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

using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.UI.Components;

namespace Gurux.DLMS.AMI.Client
{
    /// <summary>
    /// This interface is used to listen and notification events.
    /// </summary>
    public interface IGXNotifier2 : IGXNotifier
    {
        /// <summary>
        /// Invoke that property has changed.
        /// </summary>
        Task ChangedAsync<T>(string methodName, T value);

        /// <summary>
        /// Mark user stamp as handled.
        /// </summary>
        /// <param name="userStampRepository">User stamp repository.</param>
        /// <param name="type">Target type.</param>
        /// <param name="datetime">Mark time.</param>
        Task Mark(string type,
            DateTimeOffset? datetime);

        /// <summary>
        /// Mark user stamp as handled.
        /// </summary>
        /// <param name="http">Http client.</param>
        /// <param name="type">Target type.</param>
        /// <param name="datetime">Mark time.</param>
        Task Mark(HttpClient http,
            string type,
            DateTimeOffset? datetime);

        /// <summary>
        /// Get user stamp by type.
        /// </summary>
        /// <param name="type">User stamp type.</param>
        /// <returns>User stamp.</returns>
        GXUserStamp GetUserStamps(string type);

        /// <summary>
        /// Get user stamps by types.
        /// </summary>
        /// <param name="types">User stamp types.</param>
        /// <returns>User stamps.</returns>
        IEnumerable<GXUserStamp> GetUserStamps(IEnumerable<string>? types);

        /// <summary>
        /// Maintenance mode is updated.
        /// </summary>
        event Action<bool>? OnMaintenanceMode;
    }
}
