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

using Gurux.DLMS.AMI.Shared.DTOs.View;

namespace Gurux.DLMS.AMI.Client.Internal;

/// <summary>User-specific table views stored by the client resource service.</summary>
public interface IAmiViewStorage
{
    /// <summary>Gets the named visible view, or null when no default is available.</summary>
    Task<GXView?> GetViewAsync(string name);

    /// <summary>Invalidates and refreshes previously requested views.</summary>
    Task RefreshViewsAsync();

    /// <summary>Raised when a view changes or is cleared after an identity or permission change.</summary>
    event EventHandler<GXViewChangedEventArgs>? ViewChanged;
}

/// <summary>A named view changed or was cleared.</summary>
public sealed class GXViewChangedEventArgs : EventArgs
{
    public required string Name { get; init; }
    public GXView? View { get; init; }
}
