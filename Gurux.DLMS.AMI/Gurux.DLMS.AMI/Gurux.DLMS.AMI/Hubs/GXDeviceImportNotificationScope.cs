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

/// <summary>Marks device notifications produced by the current import mutation without changing global delivery settings.</summary>
internal sealed class GXDeviceImportNotificationScope : IDisposable
{

    /// <summary>Tracks import ownership only in the current asynchronous operation.</summary>
    private static readonly AsyncLocal<GXDeviceImportNotificationScope?> _current = new();

    /// <summary>Restores enclosing import ownership after this scope ends.</summary>
    private readonly GXDeviceImportNotificationScope? _previous;

    /// <summary>Stops inherited background contexts from retaining import ownership after completion.</summary>
    private bool _disposed;

    /// <summary>Starts marking notifications for an import mutation.</summary>
    internal GXDeviceImportNotificationScope()
    {
        _previous = _current.Value;
        _current.Value = this;
    }

    /// <summary>Marks only source-free device additions and edits from the current import.</summary>
    /// <param name="notification">The original event retained for each delivery channel.</param>
    internal static void Mark(GXTarget notification)
    {
        var scope = _current.Value;
        if (scope != null && !Volatile.Read(ref scope._disposed) && notification.Source == null && notification.Type is "device.add" or "device.edit")
        {
            notification.Source = "Gurux.AMI.Device.Importer";
        }
    }

    /// <summary>Restores the enclosing asynchronous notification scope.</summary>
    public void Dispose()
    {
        Volatile.Write(ref _disposed, true);
        _current.Value = _previous;
    }
}
