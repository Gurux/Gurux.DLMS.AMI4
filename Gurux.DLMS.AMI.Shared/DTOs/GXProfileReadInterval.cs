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

/// <summary>An authorized interval that an agent reads completely before committing.</summary>
public sealed class GXProfileReadInterval
{
    /// <summary>Identifier of the persisted read interval.</summary>
    public Guid IntervalId { get; set; }
    /// <summary>Identifier of the claimed task.</summary>
    public Guid TaskId { get; set; }
    /// <summary>Identifier of the Profile Generic buffer attribute.</summary>
    public Guid AttributeId { get; set; }
    /// <summary>Inclusive start instant.</summary>
    public DateTimeOffset Start { get; set; }
    /// <summary>Exclusive end instant.</summary>
    public DateTimeOffset End { get; set; }
}
