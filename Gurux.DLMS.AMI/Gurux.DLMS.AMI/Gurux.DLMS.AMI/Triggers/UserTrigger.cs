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

using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Client.Shared;

namespace Gurux.DLMS.AMI.Server.Triggers
{
    /// <summary>
    /// User trigger type.
    /// </summary>
    public class UserTrigger : ITriggerAction
    {
        /// <inheritdoc />
        public string Name => "User";

        /// <inheritdoc />
        public Type? ConfigurationUI => null;

        /// <inheritdoc />
        public string? Icon => "user";

        /// <summary>
        /// A new User has created.
        /// </summary>
        [GXTriggerEvent(GXUserPolicies.Add)]
        public const string Create = "Create";
        /// <summary>
        /// User has login from unknown IP address.
        /// </summary>
        public const string NewLogin = "NewLogin";
        /// <summary>
        /// User has deleted.
        /// </summary>
        public const string Delete = "Delete";
        /// <summary>
        /// User has login.
        /// </summary>
        [GXTriggerEvent(GXUserPolicies.Login)]
        public const string Login = "Login";
        /// <summary>
        /// User has logout.
        /// </summary>
        [GXTriggerEvent(GXUserPolicies.Logout)]
        public const string Logout = "Logout";
        /// <summary>
        /// User has modified the account.
        /// </summary>
        public const string Modify = "Modify";
        /// <summary>
        /// User is locked out.
        /// </summary>
        [GXTriggerEvent(GXUserPolicies.LockedOut)]
        public const string LockedOut = "LockedOut";
    }
}
