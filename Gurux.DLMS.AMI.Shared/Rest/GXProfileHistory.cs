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

/// <summary>A half-open interval of UTC instants.</summary>
/// <param name="Start">Inclusive start instant.</param>
/// <param name="End">Exclusive end instant.</param>
public sealed record GXProfileRange(DateTimeOffset Start, DateTimeOffset End);

/// <summary>Queries or schedules collection of a bounded profile history interval.</summary>
public sealed class GXProfileHistoryRequest
{
    /// <summary>Identifier of the Profile Generic buffer attribute.</summary>
    public Guid AttributeId { get; set; }
    /// <summary>Inclusive start instant.</summary>
    public DateTimeOffset Start { get; set; }
    /// <summary>Exclusive end instant.</summary>
    public DateTimeOffset End { get; set; }
    /// <summary>Whether to read covered intervals again.</summary>
    public bool Force { get; set; }
    /// <summary>Zero-based offset of the stored result page.</summary>
    public int Index { get; set; }
    /// <summary>Requested page size or total matching row count.</summary>
    public int Count { get; set; } = 100;
}

/// <summary>Stored profile rows and the collection coverage of the requested interval.</summary>
public sealed class GXProfileHistoryResponse
{
    /// <summary>Stored measurement rows in the requested page.</summary>
    public GXProfileStoredRow[] Values { get; set; } = [];
    /// <summary>Requested page size or total matching row count.</summary>
    public long Count { get; set; }
    /// <summary>Intervals previously read successfully.</summary>
    public GXProfileRange[] Covered { get; set; } = [];
    /// <summary>Intervals not yet read successfully.</summary>
    public GXProfileRange[] Missing { get; set; } = [];
    /// <summary>Intervals known to be unavailable from the meter.</summary>
    public GXProfileRange[] Unavailable { get; set; } = [];
    /// <summary>Successfully read intervals that returned no rows.</summary>
    public GXProfileRange[] EmptyRanges { get; set; } = [];
    /// <summary>Previously read intervals due for another check.</summary>
    public GXProfileRange[] RefreshRanges { get; set; } = [];
    /// <summary>Identifiers of queued collection tasks.</summary>
    public Guid[] TaskIds { get; set; } = [];
}

/// <summary>A stored profile row returned to the user interface.</summary>
public sealed class GXProfileStoredRow
{
    /// <summary>Stored measurement row identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Capture instant reported by the meter.</summary>
    public DateTimeOffset CapturedAt { get; set; }
    /// <summary>Serialized DLMS row value.</summary>
    public string? Value { get; set; }
}

