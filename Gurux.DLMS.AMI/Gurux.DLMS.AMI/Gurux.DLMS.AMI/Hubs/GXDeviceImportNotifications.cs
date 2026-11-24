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

namespace Gurux.DLMS.AMI.Hubs;

/// <summary>Compacts only importer device invalidations for one already-authorized SignalR recipient.</summary>
public static class GXDeviceImportNotifications
{

    /// <summary>Preserves ordinary events and combines importer additions or edits into list invalidations.</summary>
    /// <param name="notifications">Events belonging to one recipient, before client delivery.</param>
    /// <returns>Original ordinary events and at most one importer event per device action.</returns>
    /// <remarks>Original payloads remain available for workflow, email, and push delivery. Edit summaries retain authorized identifiers so Client can invalidate open device editors.</remarks>
    public static GXTarget[] ForClient(IEnumerable<GXTarget> notifications)
    {
        var result = new List<GXTarget>();
        var groups = new Dictionary<string, List<GXTarget>>(StringComparer.Ordinal);
        foreach (var item in notifications)
        {
            if (item.Source == "Gurux.AMI.Device.Importer" && item.Type is "device.add" or "device.edit")
            {
                if (!groups.TryGetValue(item.Type, out var group))
                {
                    group = [];
                    groups.Add(item.Type, group);
                }
                group.Add(item);
            }
            else
            {
                result.Add(item);
            }
        }
        foreach (var group in groups)
        {
            result.Add(new GXTarget(group.Key)
            {
                Source = "Gurux.AMI.Device.Importer",
                CreationTime = group.Value.Max(e => e.CreationTime),
                Settings = JsonSerializer.Serialize(new
                {
                    count = group.Value.Count,
                    ids = group.Key == "device.edit" ? group.Value.Select(item => item.Id).Where(id => id != null).Distinct().ToArray() : null
                })
            });
        }
        return result.ToArray();
    }
}
