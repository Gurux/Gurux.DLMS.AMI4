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

using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Hubs;

/// <summary>Applies an operation-owned notification transformation without changing global delivery settings.</summary>
internal sealed class GXNotificationMutationScope : IDisposable
{

    /// <summary>Tracks notification ownership only in the current asynchronous operation.</summary>
    private static readonly AsyncLocal<GXNotificationMutationScope?> _current = new();

    /// <summary>Restores enclosing notification ownership after this scope ends.</summary>
    private readonly GXNotificationMutationScope? _previous;

    /// <summary>Stops inherited background contexts from retaining notification ownership after completion.</summary>
    private bool _disposed;

    /// <summary>Transforms notifications emitted by this asynchronous mutation.</summary>
    private readonly Action<GXTarget> _transform;

    /// <summary>Starts transforming notifications for a repository mutation.</summary>
    /// <param name="transform">Operation-owned transformation applied before delivery.</param>
    internal GXNotificationMutationScope(Action<GXTarget> transform)
    {
        _transform = transform;
        _previous = _current.Value;
        _current.Value = this;
    }

    /// <summary>Applies the active mutation transformation to an original notification.</summary>
    /// <param name="notification">The original event retained for each delivery channel.</param>
    internal static void Mark(GXTarget notification)
    {
        var scope = _current.Value;
        if (scope != null && !Volatile.Read(ref scope._disposed))
        {
            scope._transform(notification);
        }
    }

    /// <summary>Restores the enclosing asynchronous notification scope.</summary>
    public void Dispose()
    {
        Volatile.Write(ref _disposed, true);
        _current.Value = _previous;
    }
}
