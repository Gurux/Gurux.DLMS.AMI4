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

using System.ComponentModel;

namespace Gurux.DLMS.AMI.Client.Shared;

/// <summary>Permissions for receiving and processing raw meter notifications.</summary>
[Description("User can control raw notifications.")]
public static class GXRawNotificationPolicies
{
    /// <summary>View accessible raw notifications.</summary>
    [Description("User can view raw notifications.")]
    public const string View = "raw-notification.view";

    /// <summary>Store received notification chunks.</summary>
    [Description("User can add raw notifications.")]
    public const string Add = "raw-notification.add";

    /// <summary>Reserve, renew and acknowledge notification processing.</summary>
    [Description("User can process raw notifications.")]
    public const string Edit = "raw-notification.edit";

    /// <summary>Delete selected notification connections.</summary>
    [Description("User can delete raw notifications.")]
    public const string Delete = "raw-notification.delete";

    /// <summary>Remove completed notifications by retention cutoff.</summary>
    [Description("User can clear completed raw notifications.")]
    public const string Clear = "raw-notification.clear";
}
