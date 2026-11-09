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

/// <summary>Permissions for viewing user change history, restoring saved configuration and deleting expired history.</summary>
[Description("User can view change history, restore saved configuration and manage history retention.")]
public static class GXChangeHistoryPolicies
{
    /// <summary>View accessible change history records.</summary>
    [Description("User can view accessible change history records.")]
    public const string View = "change-history.view";
    /// <summary>Restore accessible entity versions using the entity's normal edit permission.</summary>
    [Description("User can restore accessible saved entity versions.")]
    public const string Restore = "change-history.restore";
    /// <summary>Permanently remove expired change history across all users; administrator access is also required.</summary>
    [Description("Administrator can permanently remove expired change history across all users.")]
    public const string Delete = "change-history.delete";
}
