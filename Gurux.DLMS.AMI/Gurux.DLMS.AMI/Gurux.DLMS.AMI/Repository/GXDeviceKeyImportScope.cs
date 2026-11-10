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

using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Identifies credentials already installed on a newly registered meter.</summary>
/// <remarks>Only the device repository opens this scope. Request DTOs cannot opt into activation.</remarks>
internal sealed class GXDeviceKeyImportScope : IDisposable
{
    private static readonly AsyncLocal<HashSet<GXKeyManagementKey>?> Current = new();
    private readonly HashSet<GXKeyManagementKey>? previous;

    internal GXDeviceKeyImportScope(IEnumerable<GXKeyManagementKey> keys)
    {
        previous = Current.Value;
        Current.Value = new HashSet<GXKeyManagementKey>(keys, ReferenceEqualityComparer.Instance);
    }

    internal static bool IsInstalled(GXKeyManagementKey key) => Current.Value?.Contains(key) == true;

    public void Dispose()
    {
        Current.Value = previous;
    }
}