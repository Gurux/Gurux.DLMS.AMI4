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

using System.Text.Json;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;

namespace Gurux.DLMS.AMI.Client.Shared.KeyManagement;

/// <summary>Prevents ordinary model saves from changing existing secret identities or lifecycle metadata.</summary>
public static class GXKeyUpdateGuard
{
    /// <summary>Rejects new material or changes to existing key metadata during an ordinary model save.</summary>
    /// <param name="stored">Persisted key whose identity and metadata must be preserved.</param>
    /// <param name="incoming">Proposed model update, with no replacement key material.</param>
    /// <exception cref="InvalidOperationException">The update contains material or changes protected metadata.</exception>
    /// <remarks>The management navigation property is excluded from the comparison.</remarks>
    public static void EnsureUnchanged(GXKeyManagementKey stored, GXKeyManagementKey incoming)
    {
        if (incoming.Key is { Length: > 0 })
        {
            throw new InvalidOperationException("Rotate the key using a new identity; existing material cannot be overwritten.");
        }
        var a = stored.ToMetadata();
        var b = incoming.ToMetadata();
        a.KeyManagement = null;
        b.KeyManagement = null;
        if (JsonSerializer.Serialize(a) != JsonSerializer.Serialize(b))
        {
            throw new InvalidOperationException("Use explicit key lifecycle operations to change an existing key.");
        }
    }
}

