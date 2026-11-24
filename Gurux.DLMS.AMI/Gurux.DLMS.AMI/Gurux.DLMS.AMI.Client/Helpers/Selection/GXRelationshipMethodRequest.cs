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

namespace Gurux.DLMS.AMI.Client.Helpers.Selection;

/// <summary>Requests a bounded page of accessible script methods for relationship selection.</summary>
public sealed class GXRelationshipMethodRequest
{
    /// <summary>Gets or sets the zero-based method offset.</summary>
    public int Index { get; set; }
    /// <summary>Gets or sets the requested method count, limited to 1000 by the server.</summary>
    public int Count { get; set; } = 25;
    /// <summary>Gets or sets the parent script name filter used by the existing selector.</summary>
    public string? ScriptName { get; set; }
    /// <summary>Gets or sets whether an administrator explicitly requests records across users.</summary>
    public bool AllUsers { get; set; }
    /// <summary>Gets or sets method identities to include, or null for any accessible method.</summary>
    public Guid[]? Included { get; set; }
    /// <summary>Gets or sets method identities to exclude from the available segment.</summary>
    public Guid[]? Excluded { get; set; }
}
