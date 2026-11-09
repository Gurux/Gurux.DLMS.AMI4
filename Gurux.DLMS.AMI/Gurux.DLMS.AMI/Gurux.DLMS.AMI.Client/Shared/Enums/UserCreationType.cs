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

namespace Gurux.DLMS.AMI.Client.Shared.Enums
{
    /// <summary>
    /// Defines How can create new users.
    /// </summary>
    public enum UserCreationType : byte
    {
        /// <summary>
        /// Anyone can register new user accounts.
        /// </summary>
        Anyone = 0,
        /// <summary>
        /// Anyone can register new user accounts, but the user must be manually approved 
        /// by an administrator or user manager before they can log in.
        /// </summary>
        AdminApprovalRequired = 1,
        /// <summary>
        /// Only an administrator is allowed to create new user accounts.
        /// Regular self-registration is disabled.
        /// </summary>
        AdminOnly = 2
    }
}
