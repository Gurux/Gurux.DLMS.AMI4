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
using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Restores edit identities from compact importer notifications for existing editor subscriptions.</summary>
public static class GXDeviceImportUpdates
{

    /// <summary>Preserves list invalidations and expands compact edits without discarding unrelated notifications.</summary>
    /// <param name="notifications">A recipient's SignalR update payload.</param>
    /// <returns>Updates understood by existing list and individual device views.</returns>
    /// <remarks>The transport-only importer source marker is removed from expanded edits, restoring ordinary device event semantics.</remarks>
    public static GXTarget[] Expand(IEnumerable<GXTarget> notifications)
    {
        var result = new List<GXTarget>();
        foreach (var item in notifications)
        {
            string[]? ids = null;
            if (item.Source == "Gurux.AMI.Device.Importer" && item.Type == "device.edit" && item.Id == null && item.Settings != null)
            {
                try
                {
                    using var document = JsonDocument.Parse(item.Settings);
                    if (document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("ids", out var values) && values.ValueKind == JsonValueKind.Array)
                    {
                        ids = values.EnumerateArray().Where(value => value.ValueKind == JsonValueKind.String)
                            .Select(value => value.GetString()!).Where(value => Guid.TryParse(value, out _)).Distinct().ToArray();
                    }
                }
                catch (JsonException)
                {
                    // Retain the conservative list invalidation if optional edit metadata is malformed.
                }
            }
            if (ids?.Length > 0)
            {
                foreach (string id in ids)
                {
                    result.Add(new GXTarget(item.Type, id) { CreationTime = item.CreationTime });
                }
            }
            else
            {
                result.Add(item);
            }
        }
        return result.ToArray();
    }
}
