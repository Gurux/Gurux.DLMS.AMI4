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

using Gurux.DLMS.AMI.Shared.DTOs.Block;

namespace Gurux.DLMS.AMI.Client.Internal;

/// <summary>User-specific active layout blocks stored by the client resource service.</summary>
public interface IAmiBlockStorage
{
    /// <summary>Gets active, visible blocks, excluding blocks closed by this user.</summary>
    Task<IEnumerable<GXBlock>> GetBlocksAsync();

    /// <summary>Invalidates and refreshes previously requested layout blocks.</summary>
    Task RefreshBlocksAsync();

    /// <summary>Raised when the visible blocks change or are cleared after an identity or permission change.</summary>
    event EventHandler<GXBlocksChangedEventArgs>? BlocksChanged;
}

/// <summary>The current user's visible layout blocks changed.</summary>
public sealed class GXBlocksChangedEventArgs : EventArgs
{
    /// <summary>The currently visible blocks; empty when the previous data is cleared.</summary>
    public IEnumerable<GXBlock> Blocks { get; init; } = [];
}
