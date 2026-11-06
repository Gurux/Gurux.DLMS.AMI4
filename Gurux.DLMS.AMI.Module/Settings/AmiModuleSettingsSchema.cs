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
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Gurux.DLMS.AMI.Module;

/// <summary>Opt-in settings schema owned by a single module generation.</summary>
/// <remarks>The host reserves $schemaVersion, $state and $resources. Missing version metadata means legacy version zero.
/// Migrations run from N to N+1 before validation and must not perform external writes.</remarks>
public abstract class AmiModuleSettingsSchema
{
    /// <summary>Reserved JSON property containing the settings schema version.</summary>
    public const string VersionKey = "$schemaVersion";
    /// <summary>Current settings schema version.</summary>
    public abstract int Version { get; }
    /// <summary>Migrates and validates settings without modifying the input or external state.</summary>
    /// <param name="settings">Serialized configuration, including any host metadata.</param>
    /// <returns>Validated configuration with current version metadata.</returns>
    public abstract string Normalize(string? settings);
}

/// <summary>Deserializes and validates one settings model without global serializer caches.</summary>
/// <typeparam name="T">Settings model with a parameterless constructor.</typeparam>
public sealed class AmiModuleSettingsSchema<T> : AmiModuleSettingsSchema where T : class, new()
{
    private readonly Dictionary<int, Func<JsonObject, JsonObject>> migrations;
    private readonly Action<T>? validator;
    private readonly JsonSerializerOptions json = new() { PropertyNameCaseInsensitive = true };
    /// <inheritdoc />
    public override int Version { get; }

    /// <summary>Creates a schema with optional sequential N to N+1 migrations and custom validation.</summary>
    /// <param name="version">Current nonnegative version.</param>
    /// <param name="migrations">Migration callbacks keyed by their source version.</param>
    /// <param name="validator">Additional validation; throw ValidationException when invalid.</param>
    public AmiModuleSettingsSchema(int version, IReadOnlyDictionary<int, Func<JsonObject, JsonObject>>? migrations = null, Action<T>? validator = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(version);
        Version = version;
        this.migrations = migrations == null ? [] : new(migrations);
        if (this.migrations.Any(pair => pair.Key < 0 || pair.Key >= version || pair.Value == null))
        {
            throw new ArgumentException("Migration source versions must precede the current version.", nameof(migrations));
        }
        this.validator = validator;
    }

    /// <inheritdoc />
    public override string Normalize(string? settings)
    {
        bool fresh = string.IsNullOrWhiteSpace(settings);
        var value = fresh ? new JsonObject() : JsonNode.Parse(settings!) as JsonObject
            ?? throw new InvalidOperationException("Module settings must be a JSON object.");
        var state = value["$state"]?.DeepClone();
        var resources = value["$resources"]?.DeepClone();
        int source = fresh ? Version : value[VersionKey]?.GetValue<int>() ?? 0;
        if (source < 0 || source > Version)
        {
            throw new InvalidOperationException("Unsupported module settings schema version.");
        }
        while (source < Version)
        {
            if (!migrations.TryGetValue(source, out var migrate))
            {
                throw new InvalidOperationException($"Missing module settings migration from version {source}.");
            }
            value = migrate(value) ?? throw new InvalidOperationException("Settings migration returned null.");
            ++source;
        }
        value.Remove("$state");
        value.Remove("$resources");
        value.Remove(VersionKey);
        T model = fresh ? new T() : value.Deserialize<T>(json) ?? new T();
        Validator.ValidateObject(model, new ValidationContext(model), validateAllProperties: true);
        validator?.Invoke(model);
        // Serialize defaults as well so the persisted representation matches the validated model.
        var normalized = JsonSerializer.SerializeToNode(model, json) as JsonObject
            ?? throw new InvalidOperationException("Module settings model must serialize as a JSON object.");
        normalized[VersionKey] = Version;
        if (state != null)
        {
            normalized["$state"] = state;
        }
        if (resources != null)
        {
            normalized["$resources"] = resources;
        }
        return normalized.ToJsonString();
    }
}
