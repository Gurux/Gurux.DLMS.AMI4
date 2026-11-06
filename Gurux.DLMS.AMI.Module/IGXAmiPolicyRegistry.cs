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

using Microsoft.AspNetCore.Authorization;

namespace Gurux.DLMS.AMI.Module
{
    /// <summary>
    /// Defines methods for managing authorization policies by name.
    /// </summary>
    public interface IGXAmiPolicyRegistry
    {
        /// <summary>
        /// Retrieves an authorization policy by its name.
        /// </summary>
        /// <param name="name">The name of the authorization policy to retrieve.</param>
        /// <returns>The authorization policy associated with the specified name, or null if not found.</returns>
        AuthorizationPolicy? Get(string name);

        /// <summary>
        /// Adds a new authorization policy associated with the specified name.
        /// </summary>
        /// <param name="name">The name of the authorization policy to add.</param>
        /// <param name="policy">The authorization policy to associate with the specified name.</param>
        void Add(string name, AuthorizationPolicy policy);

        /// <summary>
        /// Removes the authorization policy associated with the specified name.
        /// </summary>
        /// <param name="name">The name of the authorization policy to remove.</param>
        /// <returns><see langword="true"/> if the named policy was removed; otherwise, <see langword="false"/>.</returns>
        bool Remove(string name);

        /// <summary>
        /// Retrieves all policy names.
        /// </summary>
        /// <returns>A read-only collection of policy names.</returns>
        IReadOnlyCollection<string> GetAllPolicyNames();
    }
}
