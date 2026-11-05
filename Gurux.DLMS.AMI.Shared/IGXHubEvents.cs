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
using Gurux.DLMS.AMI.Shared.DTOs.User;

namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// Defines hub callbacks used to broadcast server-side updates.
    /// </summary>
    [System.ComponentModel.Description("Defines hub callbacks used to broadcast server-side updates.")]
    public interface IGXHubEvents
    {
        /// <summary>
        /// Notifies clients that one or more target entities were created or updated.
        /// </summary>
        /// <param name="items">Updated target entities.</param>
        /// <returns>A task representing delivery of the entity update callback.</returns>
        [System.ComponentModel.Description("Notifies clients that one or more target entities were created or updated.")]
        Task OnUpdate(IEnumerable<GXTarget> items);

        /// <summary>
        /// Notifies clients that one or more user stamps were created or updated.
        /// </summary>
        /// <param name="stamps">Updated user stamps.</param>
        /// <returns>A task representing delivery of the user-stamp update callback.</returns>
        [System.ComponentModel.Description("Notifies clients that one or more user stamps were created or updated.")]
        Task StampUpdate(IEnumerable<GXUserStamp> stamps);
    }
}
