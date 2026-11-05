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

namespace Gurux.DLMS.AMI.Shared.Rest;

/// <summary>Policies to evaluate for the current HTTP user.</summary>
[System.ComponentModel.Description("Policies to evaluate for the current HTTP user.")]
public sealed class CheckPolicies
{
    /// <summary>Gets or sets the names of the policies to evaluate for the current user.</summary>
    [System.ComponentModel.Description("Gets or sets the names of the policies to evaluate for the current user.")]
    public string[] PolicyNames { get; set; } = [];
}

/// <summary>Server authorization decisions, without policy definitions.</summary>
[System.ComponentModel.Description("Server authorization decisions, without policy definitions.")]
public sealed class CheckPoliciesResponse
{
    /// <summary>Gets or sets the authorization decision for each evaluated policy name.</summary>
    [System.ComponentModel.Description("Gets or sets the authorization decision for each evaluated policy name.")]
    public Dictionary<string, bool> Results { get; set; } = new(StringComparer.Ordinal);
}
