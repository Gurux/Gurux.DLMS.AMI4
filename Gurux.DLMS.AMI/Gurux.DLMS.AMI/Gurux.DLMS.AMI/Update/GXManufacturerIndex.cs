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
using Gurux.DLMS.AMI.Shared.DTOs.Device;
namespace Gurux.DLMS.AMI.Update;
/// <summary>Converts the public DeviceProfiles index into AMI's manufacturer hierarchy.</summary>
public static class GXManufacturerIndex
{
    /// <summary>Performs the parse profiles operation.</summary>
    /// <param name="content">The content.</param>
    /// <param name="name">The name.</param>
    public static List<GXDeviceTemplate> ParseProfiles(string content, string name)
    {
        if (!content.TrimStart('\uFEFF', ' ', '\r', '\n', '\t').StartsWith('<'))
        {
            return [ParseProfile(content, name)];
        }

        var templates = Gurux.DLMS.AMI.Client.Helpers.ClientHelpers.ConvertToTemplates(null, content);
        foreach (var template in templates)
        {
            template.Name = name;
        }

        return templates;
    }
    /// <summary>Performs the parse profile operation.</summary>
    /// <param name="json">The json.</param>
    /// <param name="name">The name.</param>
    public static GXDeviceTemplate ParseProfile(string json, string name)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("Device profile must be a JSON object.");
        }

        return new GXDeviceTemplate { Name = name, Settings = document.RootElement.GetRawText() };
    }
    /// <summary>Performs the merge profile operation.</summary>
    /// <param name="installed">The installed.</param>
    /// <param name="available">The available.</param>
    public static bool MergeProfile(List<GXDeviceSettings> installed, GXDeviceSettings available)
    {
        var existing = installed.SingleOrDefault(s => s.ProfileId == available.ProfileId);
        if (existing == null)
        {
            installed.Add(available);
            return true;
        }
        if (existing.Location == available.Location && existing.Name == available.Name &&
            (available.Settings == null || existing.Settings == available.Settings))
        {
            return false;
        }

        existing.Location = available.Location;
        existing.Name = available.Name;
        if (available.Settings != null)
        {
            existing.Settings = available.Settings;
        }

        return true;
    }
    /// <summary>Performs the parse operation.</summary>
    /// <param name="json">The json.</param>
    public static GXManufacturer[] Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.GetProperty("SchemaVersion").GetInt32() != 2)
        {
            throw new InvalidDataException("Unsupported DeviceProfiles index schema. Expected SchemaVersion 2.");
        }

        var manufacturers = new List<GXManufacturer>();
        var identities = new HashSet<string>(StringComparer.Ordinal);
        foreach (var manufacturer in root.GetProperty("Manufacturers").EnumerateArray())
        {
            var models = new List<GXDeviceModel>();
            foreach (var model in manufacturer.GetProperty("Models").EnumerateArray())
            {
                var versions = new List<GXDeviceVersion>();
                foreach (var connection in model.GetProperty("Interfaces").EnumerateArray())
                {
                    foreach (var version in connection.GetProperty("Versions").EnumerateArray())
                    {
                        string name = connection.GetProperty("Name").GetString()!;
                        string variant = version.GetProperty("Name").GetString()!;
                        if (!string.IsNullOrEmpty(variant))
                        {
                            name += "/" + variant;
                        }

                        var settings = new List<GXDeviceSettings>();
                        foreach (var profile in version.GetProperty("Settings").EnumerateArray())
                        {
                            string? identity = profile.GetProperty("Id").GetString();
                            if (string.IsNullOrWhiteSpace(identity) || identity.Length > 1024 || !identities.Add(identity))
                            {
                                throw new InvalidDataException("Device profile IDs must be nonempty and unique (maximum 1024 characters).");
                            }

                            string location = profile.GetProperty("Location").GetString()!;
                            if (!Uri.TryCreate(location, UriKind.Absolute, out var uri) || uri.Scheme != "https")
                            {
                                throw new InvalidDataException("Device profile Location must be an absolute HTTPS URL.");
                            }

                            settings.Add(new GXDeviceSettings
                            {
                                ProfileId = identity,
                                Name = profile.GetProperty("Name").GetString(),
                                Location = location,
                                Settings = profile.TryGetProperty("Settings", out var value) ? value.GetRawText() : null
                            });
                        }
                        versions.Add(new GXDeviceVersion { Name = name, Settings = settings });
                    }
                }

                models.Add(new GXDeviceModel { Name = model.GetProperty("Name").GetString(), Versions = versions });
            }
            manufacturers.Add(new GXManufacturer { Name = manufacturer.GetProperty("Name").GetString(), Template = true,
                Models = models, AvailableVersion = GXManufacturerRevision.Revision(manufacturer) });
        }
        return [.. manufacturers];
    }
}
