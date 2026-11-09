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
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
namespace Gurux.DLMS.AMI.Update;
/// <summary>Provides manufacturer revision.</summary>
public static class GXManufacturerRevision
{
    /// <summary>Performs the revision operation.</summary>
    /// <param name="manufacturer">The manufacturer.</param>
    public static string Revision(JsonElement manufacturer)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            void Write(JsonElement value)
            {
                if (value.ValueKind == JsonValueKind.Object)
                {
                    writer.WriteStartObject();
                    foreach (var property in value.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                    {
                        writer.WritePropertyName(property.Name);
                        Write(property.Value);
                    }
                
                    writer.WriteEndObject();
                }
                else if (value.ValueKind == JsonValueKind.Array)
                {
                    writer.WriteStartArray();
                    foreach (var item in value.EnumerateArray())
                    {
                        Write(item);
                    }

                    writer.WriteEndArray();
                }
                else
                {
                    value.WriteTo(writer);
                }
            }
            Write(manufacturer);
        }
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(buffer.ToArray())).ToLowerInvariant();
    }

    /// <summary>Performs the is update available operation.</summary>
    /// <param name="manufacturer">The manufacturer.</param>
    public static bool IsUpdateAvailable(GXManufacturer manufacturer) => manufacturer.Template == true && manufacturer.Removed == null && manufacturer.ProfilesInstalled && !string.IsNullOrWhiteSpace(manufacturer.Version) && !string.IsNullOrWhiteSpace(manufacturer.AvailableVersion) && manufacturer.Version != manufacturer.AvailableVersion;
}
