//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

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

