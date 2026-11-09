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

namespace Gurux.DLMS.AMI.Hubs
{
    /// <summary>A buffered notification and its intended recipients.</summary>
    public sealed class CachedNotification
    {
        /// <summary>
        /// Notified user ID.
        /// </summary>
        public string UserId { get; set; } = default!;
        /// <summary>The agent recipient for an internal command; excludes user delivery.</summary>
        public Guid? AgentId { get; set; }
        /// <summary>
        /// Notification details.
        /// </summary>
        public GXTarget Notification { get; set; } = default!;

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"UserId: {UserId}, Notification: {Notification.Type} ({Notification.Id})";
        }
    }
}
