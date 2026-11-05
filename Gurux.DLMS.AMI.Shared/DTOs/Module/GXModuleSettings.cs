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

using System.Text.Json.Nodes;

namespace Gurux.DLMS.AMI.Shared.DTOs.Module;

/// <summary>Converts named properties to the JSON settings consumed by modules.</summary>
public static class GXModuleSettings
{
    /// <summary>Builds a JSON object from the active properties; values contain JSON fragments.</summary>
    /// <param name="module">Module whose active named properties are converted to settings.</param>
    /// <returns>A JSON object string, or null when the property list has not been loaded or assigned.</returns>
    /// <exception cref="System.Text.Json.JsonException">An active property value is not a valid JSON fragment.</exception>
    public static string? GetSettings(this GXModule module)
    {
        if (module.Properties == null) return null;
        var settings = new JsonObject();
        foreach (var property in module.Properties.Where(p => p.Removed == null && p.Name != null))
            settings[property.Name!] = property.Value == null ? null : JsonNode.Parse(property.Value);
        return settings.ToJsonString();
    }

    /// <summary>Creates named properties from a JSON configuration object.</summary>
    /// <param name="value">A JSON object, or null or whitespace for an empty property list.</param>
    /// <returns>New property records whose values contain the corresponding JSON fragments.</returns>
    /// <exception cref="ArgumentException">The configuration is valid JSON but is not an object.</exception>
    /// <exception cref="System.Text.Json.JsonException">The configuration is not valid JSON.</exception>
    public static List<GXModuleProperty> FromSettings(string? value)
    {
        var module = new GXModule();
        module.SetSettings(value);
        return module.Properties!;
    }

    /// <summary>Replaces settings while retaining identifiers and concurrency tokens for matching properties.</summary>
    /// <param name="module">Module whose property list is replaced.</param>
    /// <param name="value">A JSON object, or null or whitespace to clear the property list.</param>
    /// <remarks>Matching active records are reused by name. Names absent from the configuration are omitted from the replacement list.</remarks>
    /// <exception cref="ArgumentException">The configuration is valid JSON but is not an object.</exception>
    /// <exception cref="System.Text.Json.JsonException">The configuration is not valid JSON.</exception>
    public static void SetSettings(this GXModule module, string? value)
    {
        var settings = string.IsNullOrWhiteSpace(value) ? new JsonObject() :
            JsonNode.Parse(value) as JsonObject ?? throw new ArgumentException("Module settings must be a JSON object.", nameof(value));
        var previous = module.Properties ?? [];
        module.Properties = settings.Select(pair =>
        {
            var property = previous.FirstOrDefault(p => p.Name == pair.Key && p.Removed == null)
                ?? new GXModuleProperty { Name = pair.Key };
            property.Value = pair.Value?.ToJsonString();
            return property;
        }).ToList();
    }
}
