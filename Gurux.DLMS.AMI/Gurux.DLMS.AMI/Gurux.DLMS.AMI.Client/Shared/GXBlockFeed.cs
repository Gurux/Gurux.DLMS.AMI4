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

namespace Gurux.DLMS.AMI.Client.Shared;

/// <summary>Display data from a block's RSS or Atom feed.</summary>
public sealed class GXBlockFeed
{
    /// <summary>Feed title.</summary>
    public string? Title { get; set; }
    /// <summary>Latest feed entries, in feed order.</summary>
    public List<GXBlockFeedItem> Items { get; set; } = [];
}

/// <summary>A feed entry rendered as text and an optional HTTP link.</summary>
public sealed class GXBlockFeedItem
{
    /// <summary>Entry title.</summary>
    public string? Title { get; set; }
    /// <summary>Validated HTTP or HTTPS entry address.</summary>
    public string? Link { get; set; }
    /// <summary>Entry publication time.</summary>
    public DateTimeOffset? Published { get; set; }
}

