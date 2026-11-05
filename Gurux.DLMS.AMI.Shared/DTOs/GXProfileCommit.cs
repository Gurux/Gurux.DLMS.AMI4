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

namespace Gurux.DLMS.AMI.Shared.DTOs;

/// <summary>Commits a completely read interval together with its measurement rows.</summary>
public sealed class GXProfileCommit
{
    /// <summary>Identifier of the persisted read interval.</summary>
    public Guid SpanId { get; set; }
    /// <summary>Identifier of the claimed task.</summary>
    public Guid TaskId { get; set; }
    /// <summary>Whether the entire interval was read successfully.</summary>
    public bool Completed { get; set; }
    /// <summary>Reliably known earliest instant still available from the meter.</summary>
    public DateTimeOffset? AvailableFrom { get; set; }
    /// <summary>All rows returned for the completed interval.</summary>
    public GXProfileRow[] Rows { get; set; } = [];
}
