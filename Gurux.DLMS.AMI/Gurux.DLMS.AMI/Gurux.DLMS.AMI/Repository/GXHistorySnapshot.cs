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

using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Version-one configuration snapshots with explicit root fields and identity-only references.</summary>
internal static class GXHistorySnapshot
{
    private static readonly ConditionalWeakTable<object, HashSet<string>> Presence = new();
    private static readonly ConditionalWeakTable<object, IReadOnlyList<GXHistoryModuleSetting>> ModuleSettings = new();
    private const string Common = "Id Name Description Enabled Default AccessVisibility Policies ProfilePicture Coordinates StreetAddress PostalCode StateOrProvince Country TraceLevel";
    private static readonly Dictionary<string, string> Fields = new(StringComparer.Ordinal)
    {
        ["GXAgent"] = "Type Template AgentGroups ReaderSettings ListenerSettings NotifySettings RawNotificationSettings GatewaySettings CacheExpiration Concurrently Properties",
        ["GXAgentGroup"] = "Agents UserGroups DeviceGroups",
        ["GXAgentProperty"] = "Agent Module Value",
        ["GXAppearance"] = "Category Value OriginalFileName ContentType ResourceType Priority Preload",
        ["GXAttribute"] = "Object Template Settings ExpirationTime Properties",
        ["GXAttributeTemplate"] = "ObjectTemplate Index ListItems ExpirationTime AccessLevel DataType UIDataType Weight DefaultValue Scaler Unit",
        ["GXBlock"] = "Title BlockType ComponentView ScriptMethod CssClass CssRole Style Body PageVisibility Location LocationOrder Pages Preload Closable BlockGroups PublishTime EndTime Parent Visibility",
        ["GXBlockGroup"] = "UserGroups Blocks",
        ["GXComponentView"] = "ClassName Icon ConfigurationUI ComponentViewGroups",
        ["GXComponentViewGroup"] = "UserGroups ComponentViews",
        ["GXConfiguration"] = "Settings",
        ["GXDevice"] = "SystemTitle MediaType MediaSettings Dc Template Type Settings WaitTime ResendCount LateBinding Properties Dynamic KeepAliveAttribute KeepAliveInterval DeviceGroups Gateways",
        ["GXDeviceGroup"] = "Properties Devices UserGroups AgentGroups Gateways",
        ["GXDeviceTemplate"] = "Type Protocol WaitTime ResendCount Settings MediaType MediaSettings Manufacturer Objects DeviceTemplateGroups",
        ["GXDeviceTemplateGroup"] = "UserGroups DeviceTemplates",
        ["GXEnumType"] = "Type Value",
        ["GXFavorite"] = "Type Path User",
        ["GXGateway"] = "Identifier ScriptMethod DeviceGroups Devices GatewayGroups Agent",
        ["GXGatewayGroup"] = "Gateways UserGroups",
        ["GXKeyManagement"] = "Device DeviceGroup Template SystemTitle ProviderId ProviderConnectionId KeyManagementGroups Properties Keys",
        ["GXKeyManagementGroup"] = "UserGroups KeyManagements",
        ["GXLanguage"] = "EnglishName NativeName",
        ["GXLocalizedResource"] = "Value Hash Status Language Preload",
        ["GXLogType"] = "PublicId Level",
        ["GXManufacturer"] = "Template Logo Url Models ManufacturerGroups",
        ["GXManufacturerGroup"] = "Manufacturers Public UserGroups",
        ["GXMenu"] = "Order Location Kind Visibility Icon Title MenuGroups Links PublishTime Parent Preload",
        ["GXMenuGroup"] = "UserGroups Menus",
        ["GXModule"] = "Icon Protocols Properties UserGroups ModuleGroups TraceLevel",
        ["GXModuleGroup"] = "UserGroups Modules",
        ["GXNotification"] = "ScriptMethod Type Sources Properties MinimumDeliveryTime",
        ["GXNotificationType"] = "Code CreationMode",
        ["GXNotificationSource"] = "Enabled",
        ["GXObject"] = "Device Template Latebind Properties",
        ["GXObjectTemplate"] = "DeviceTemplate ObjectType Version LogicalName ShortName ExpirationTime Attributes ActionAccessLevels",
        ["GXPolicy"] = "DisplayName RequireAuthenticatedUser RequirementOperator Requirements",
        ["GXRole"] = "Permissions",
        ["GXSchedule"] = "Devices DeviceGroups DeviceAttributeTemplates DeviceObjectTemplates DeviceGroupAttributeTemplates DeviceGroupObjectTemplates ScriptMethods Modules Triggers ScheduleGroups Concurrently Start ExecutionTime",
        ["GXScheduleGroup"] = "UserGroups Schedules",
        ["GXScript"] = "Properties Language SourceCode Namespaces ServerSide Module Methods ScriptGroups",
        ["GXScriptGroup"] = "UserGroups Scripts",
        ["GXTrigger"] = "ClassName Icon ConfigurationUI User UserGroup Device DeviceGroup Module Schedules Workflows TriggerGroups Properties",
        ["GXTriggerGroup"] = "UserGroups Triggers",
        ["GXUser"] = "UserName Email PhoneNumber IsApproved Notification Roles Permissions UserGroups Language GivenName Surname DateOfBirth TimeZone",
        ["GXUserGroup"] = "Users DeviceGroups ScheduleGroups DeviceTemplateGroups AgentGroups GatewayGroups ModuleGroups WorkflowGroups TriggerGroups BlockGroups MenuGroups ViewGroups ComponentViewGroups ScriptGroups ManufacturerGroups KeyManagementGroups",
        ["GXUserProperty"] = "User Module Value",
        ["GXView"] = "Title ViewGroups SelectionMode AllowColumnCustomization ShowAllData PageSize Columns Preload",
        ["GXViewGroup"] = "UserGroups Views",
        ["GXWorkflow"] = "Properties TriggerActivity Phase TriggerMethod User UserGroup Device DeviceGroup ExecutionIdentity ExecutionUserId ScriptMethods Modules WorkflowGroups",
        ["GXWorkflowGroup"] = "UserGroups Workflows"
    };
    private static readonly Dictionary<string, string> Owned = new(StringComparer.Ordinal)
    {
        ["GXAttributeListItem"] = "Id UIValue Value MaskSize Shift",
        ["GXDeviceModel"] = "Id Name Url Picture AccessVisibility ProfilePicture Versions",
        ["GXDeviceVersion"] = "Id Name Settings",
        ["GXDeviceSettings"] = "Id ProfileId Name Settings Location Template",
        ["GXAgentProperty"] = "Id Name Value Module",
        ["GXDeviceProperty"] = "Id Name Value Module",
        ["GXDeviceGroupProperty"] = "Id Name Value Module",
        ["GXAttributeProperty"] = "Id Name Value Module",
        ["GXObjectProperty"] = "Id Name Value Module",
        ["GXModuleProperty"] = "Id Name Value",
        ["GXScriptProperty"] = "Id Name Value Module",
        ["GXWorkflowProperty"] = "Id Name Value Module",
        ["GXKeyManagementProperty"] = "Id Name Value Module",
        ["GXKeyManagementKey"] = "Id KeyType State ActivationDate DeactivationDate CompromiseDate DestroyDate Algorithm Format CryptographicLength UsageMask Extractable",
        ["GXNotificationProperty"] = "Id Name Value Module",
        ["GXTriggerProperty"] = "Id Name Value Module",
        ["GXUserProperty"] = "Id Name Value Module",
        ["GXViewColumn"] = "Id Name Visible Enabled Position Description Policies Visibility",
        ["GXScriptMethod"] = "Id Name Description IsFunction IsAsynchronous",
        ["GXPolicyRequirement"] = "Id Type ClaimType Value Name Description",
        ["GXMenuLink"] = "Id Name Description MenuLinkType Order Enabled Preload Icon Url TargetType Configuration Policies Parent Visibility PadgeTitle Padges",
        ["GXObjectTemplate"] = "Id ObjectType Version Name LogicalName ShortName ExpirationTime Attributes ActionAccessLevels",
        ["GXAttributeTemplate"] = "Id Index Name ListItems ExpirationTime AccessLevel DataType UIDataType Weight DefaultValue Scaler Unit"
    };
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = false, MaxDepth = 64 };

    internal static string Serialize(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (!Fields.ContainsKey(entity.GetType().Name))
        {
            throw new ArgumentException("The entity type has no history snapshot contract.");
        }

        var node = (JsonObject)Project(entity, entity.GetType(), true, 0)!;
        AppendModuleSettings(entity, node);
        return node.ToJsonString(JsonOptions);
    }

    /// <summary>Captures scalar columns and known reference identities without inventing unloaded collection states.</summary>
    internal static string SerializeStored(object entity)
    {
        var node = (JsonObject)Project(entity, entity.GetType(), true, 0)!;
        foreach (var property in Properties(entity.GetType(), true))
        {
            if (Scalar(property.PropertyType))
            {
                continue;
            }

            var value = property.GetValue(entity);
            if (value == null || value is IEnumerable)
            {
                node.Remove(property.Name);
            }
            else if (value.GetType().GetProperty("Id") is { } identity && identity.GetValue(value) is { } id &&
                            !(id is Guid guid && guid == Guid.Empty) && !(id is string text && string.IsNullOrWhiteSpace(text)) &&
                            !(id is int number && number == 0) && !(id is long longNumber && longNumber == 0))
            {
                node[property.Name] = new JsonObject { ["Id"] = JsonSerializer.SerializeToNode(id, id.GetType(), JsonOptions) };
            }
            else
            {
                node.Remove(property.Name);
            }
        }
        // Stored image rows contain an empty Value placeholder; only the domain read loads the bytes.
        if (entity is Gurux.DLMS.AMI.Shared.DTOs.GXAppearance { StorageKey: not null })
        {
            node.Remove("Value");
        }

        AppendModuleSettings(entity, node);
        return node.ToJsonString(JsonOptions);
    }

    internal static object Deserialize(string json, Type entityType)
    {
        try
        {
            if (!Fields.ContainsKey(entityType.Name))
            {
                throw new ArgumentException("The entity type has no history snapshot contract.");
            }

            var node = JsonNode.Parse(json, documentOptions: new JsonDocumentOptions { MaxDepth = 64 }) as JsonObject
                ?? throw new ArgumentException("An entity snapshot must be a JSON object.");
            IReadOnlyList<GXHistoryModuleSetting>? settings = null;
            if (entityType == typeof(GXSchedule) && node.Remove("$moduleSettings", out var moduleSettings))
            {
                settings = ParseModuleSettings(moduleSettings);
            }

            Validate(node, entityType, true, 0);
            var entity = node.Deserialize(entityType, JsonOptions) ?? throw new ArgumentException("The snapshot contains no entity.");
            RegisterPresence(entity, node, entityType, true);
            if (settings != null)
            {
                ModuleSettings.AddOrUpdate(entity, settings);
            }

            return entity;
        }
        catch (JsonException e) { throw new ArgumentException("Invalid entity snapshot JSON.", nameof(json), e); }
    }

    internal static void SetModuleSettings(object entity, IEnumerable<GXScheduleModule> settings)
    {
        if (entity is not GXSchedule)
        {
            throw new ArgumentException("Module settings belong to a schedule snapshot.");
        }

        ModuleSettings.AddOrUpdate(entity, settings.OrderBy(s => s.ModuleId, StringComparer.Ordinal)
            .Select(s => new GXHistoryModuleSetting(s.ModuleId, s.Settings)).ToArray());
    }

    internal static IReadOnlyList<GXHistoryModuleSetting>? GetModuleSettings(object entity)
        => ModuleSettings.TryGetValue(entity, out var settings) ? settings : null;

    private static void AppendModuleSettings(object entity, JsonObject node)
    {
        if (!ModuleSettings.TryGetValue(entity, out var settings))
        {
            return;
        }

        var array = new JsonArray();
        foreach (var item in settings)
        {
            array.Add(new JsonObject { ["ModuleId"] = item.ModuleId, ["Settings"] = Project(item.Settings, typeof(string), false, 0) });
        }

        node["$moduleSettings"] = array;
    }

    private static IReadOnlyList<GXHistoryModuleSetting> ParseModuleSettings(JsonNode? node)
    {
        if (node is not JsonArray array)
        {
            throw new ArgumentException("Schedule module settings must be an array.");
        }

        var values = new List<GXHistoryModuleSetting>();
        foreach (var item in array)
        {
            if (item is not JsonObject obj || obj.Any(p => p.Key != "ModuleId" && p.Key != "Settings") ||
                obj["ModuleId"] is not JsonValue idValue || !idValue.TryGetValue<string>(out var id) || string.IsNullOrWhiteSpace(id) || id.Length > 64)
            {
                throw new ArgumentException("Invalid schedule module settings identity or shape.");
            }

            if (!obj.ContainsKey("Settings"))
            {
                throw new ArgumentException("Saved schedule module settings are required.");
            }

            Validate(obj["Settings"], typeof(string), false, 0);
            var text = obj["Settings"]?.GetValue<string>();
            if (values.Any(v => v.ModuleId == id))
            {
                throw new ArgumentException("Duplicate schedule module settings.");
            }

            values.Add(new GXHistoryModuleSetting(id, text));
        }
        return values;
    }

    internal static Expression<Func<T, object>> Columns<T>()
    {
        var p = Expression.Parameter(typeof(T), "entity");
        var values = Properties(typeof(T), true).Where(x => x.Name != "Id")
            .Select(x => Expression.Convert(Expression.Property(p, x), typeof(object)));
        return Expression.Lambda<Func<T, object>>(Expression.NewArrayInit(typeof(object), values), p);
    }

    /// <summary>Applies only saved configuration fields while preserving current server-owned fields and omitted secrets.</summary>
    internal static object MergeForRestore(object current, object saved)
    {
        if (current.GetType() != saved.GetType())
        {
            throw new ArgumentException("Snapshot and current entity types differ.");
        }

        MergeObject(current, saved, true, 0);
        if (ModuleSettings.TryGetValue(saved, out var settings))
        {
            ModuleSettings.AddOrUpdate(current, settings);
        }

        return current;
    }

    internal static PropertyInfo[] CaptureProperties(Type type, bool root = true) => Properties(type, root);
    internal static bool IsOwned(Type type) => Owned.ContainsKey(type.Name);

    private static PropertyInfo[] Properties(Type type, bool root, bool expandOwned = true)
    {
        string names;
        if (root && Fields.TryGetValue(type.Name, out var extra))
        {
            names = type.Name == "GXNotificationSource" ? "Id Enabled" : (type.Name == "GXModule" ? Common.Replace(" Enabled", "", StringComparison.Ordinal) : Common) + " " + extra;
        }
        else if (expandOwned && Owned.TryGetValue(type.Name, out var owned))
        {
            names = owned;
        }
        else if (type.GetProperty("Id") != null)
        {
            names = "Id";
        }
        else
        {
            return type.GetProperties().Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0 && !Secret(p.Name)).OrderBy(p => p.Name, StringComparer.Ordinal).ToArray();
        }

        var selection = names.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        return type.GetProperties().Where(p => p.CanRead && p.CanWrite && selection.Contains(p.Name) && !Secret(p.Name)).OrderBy(p => p.Name, StringComparer.Ordinal).ToArray();
    }

    private static bool Secret(string name)
    {
        var key = new string(name.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        return key.Contains("password") || key.Contains("secret") || key.Contains("token") || key.Contains("credential") ||
            key.Contains("privatekey") || key.Contains("cipherkey") || key.Contains("authenticationkey") || key.Contains("dedicatedkey") ||
            key.Contains("securitystamp") || key.Contains("concurrencystamp") || key is "connectionstring" or "apikey" or "keybytes" or "key" or "keyencryptionkey" or "presharedkey";
    }

    private static bool Scalar(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(Guid) ||
            type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(DateOnly) || type == typeof(TimeOnly) || type == typeof(byte[]);
    }

    private static JsonNode? Project(object? value, Type type, bool root, int depth, bool expandOwned = false)
    {
        if (value == null)
        {
            return null;
        }

        if (type == typeof(object))
        {
            type = value.GetType();
        }

        if (depth > 16)
        {
            throw new ArgumentException("The configuration graph exceeds the history snapshot contract depth.");
        }

        if (Scalar(type))
        {
            if (value is string text && TryJson(text, out var json)) { Scrub(json); return JsonValue.Create(json!.ToJsonString()); }
            return JsonSerializer.SerializeToNode(value, type, JsonOptions);
        }
        if (value is JsonElement element) { var node = JsonNode.Parse(element.GetRawText()); Scrub(node); return node; }
        if (value is IDictionary map)
        {
            var obj = new JsonObject();
            foreach (DictionaryEntry item in map)
            {
                if (item.Key is string key && !Secret(key))
                {
                    obj[key] = Project(item.Value, item.Value?.GetType() ?? typeof(object), false, depth + 1);
                }
            }

            return obj;
        }
        if (value is IEnumerable sequence)
        {
            var array = new JsonArray();
            foreach (var item in sequence)
            {
                array.Add(Project(item, item?.GetType() ?? typeof(object), false, depth + 1, true));
            }

            return array;
        }
        var result = new JsonObject();
        var sensitiveProperty = value.GetType().GetProperty("Name")?.GetValue(value) is string name && Secret(name);
        foreach (var p in Properties(type, root, expandOwned))
        {
            if (Presence.TryGetValue(value, out var present) && !present.Contains(p.Name))
            {
                continue;
            }

            if (p.Name == "Value" && sensitiveProperty)
            {
                continue;
            }

            result[p.Name] = Project(p.GetValue(value), p.PropertyType, false, depth + 1);
        }
        return result;
    }

    private static bool TryJson(string text, out JsonNode? node)
    {
        node = null;
        var trimmed = text.AsSpan().TrimStart();
        if (trimmed.Length == 0 || (trimmed[0] != '{' && trimmed[0] != '['))
        {
            return false;
        }

        try { node = JsonNode.Parse(text, documentOptions: new JsonDocumentOptions { MaxDepth = 64 }); return node != null; }
        catch (JsonException) { return false; }
    }

    private static void Scrub(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            foreach (var key in obj.Select(p => p.Key).ToArray())
            {
                if (Secret(key) || SecretValue(obj, key))
                {
                    obj.Remove(key);
                }
                else
                {
                    Scrub(obj[key]);
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                Scrub(item);
            }
        }
    }

    private static void Validate(JsonNode? node, Type type, bool root, int depth, bool expandOwned = false)
    {
        if (node == null)
        {
            return;
        }

        if (type == typeof(string) && node is JsonValue scalar && scalar.TryGetValue<string>(out var text) && TryJson(text, out var embedded))
        {
            RejectSecrets(embedded);
            return;
        }
        if (Scalar(type))
        {
            return;
        }

        if (type == typeof(object) || type == typeof(JsonElement)) { RejectSecrets(node); return; }
        if (depth > 16)
        {
            throw new ArgumentException("The configuration graph exceeds the history snapshot contract depth.");
        }

        if (node is JsonArray array)
        {
            var child = type.IsArray ? type.GetElementType()! : type.GetGenericArguments().FirstOrDefault() ?? typeof(object);
            foreach (var item in array)
            {
                Validate(item, child, false, depth + 1, true);
            }

            return;
        }
        if (node is not JsonObject obj)
        {
            throw new ArgumentException("Invalid configuration snapshot shape.");
        }

        if (obj.Any(p => SecretValue(obj, p.Key)))
        {
            throw new ArgumentException("Secret property values cannot be restored from history.");
        }

        var properties = Properties(type, root, expandOwned).ToDictionary(p => p.Name, StringComparer.Ordinal);
        foreach (var p in obj)
        {
            if (!properties.TryGetValue(p.Key, out var property))
            {
                throw new ArgumentException($"Snapshot field {type.Name}.{p.Key} is not restorable.");
            }

            Validate(p.Value, property.PropertyType, false, depth + 1);
        }
    }

    private static void RegisterPresence(object? value, JsonNode? node, Type type, bool root, bool expandOwned = false)
    {
        if (value == null || node == null || Scalar(type) || type.IsValueType || type == typeof(object))
        {
            return;
        }

        if (node is JsonArray array && value is IList list)
        {
            var childType = type.IsArray ? type.GetElementType()! : type.GetGenericArguments().FirstOrDefault() ?? typeof(object);
            for (var index = 0; index < array.Count && index < list.Count; ++index)
            {
                RegisterPresence(list[index], array[index], childType, false, true);
            }
        }
        else if (node is JsonObject obj)
        {
            Presence.AddOrUpdate(value, obj.Select(p => p.Key).ToHashSet(StringComparer.Ordinal));
            foreach (var property in Properties(type, root, expandOwned))
            {
                if (obj.TryGetPropertyValue(property.Name, out var child))
                {
                    RegisterPresence(property.GetValue(value), child, property.PropertyType, false);
                }
            }
        }
    }

    private static void RejectSecrets(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            foreach (var entry in obj)
            {
                if (Secret(entry.Key) || SecretValue(obj, entry.Key))
                {
                    throw new ArgumentException("Secret values cannot be restored from history.");
                }

                RejectSecrets(entry.Value);
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                RejectSecrets(item);
            }
        }
    }

    private static void MergeObject(object current, object saved, bool root, int depth)
    {
        if (depth > 16)
        {
            throw new ArgumentException("The configuration graph exceeds the history snapshot contract depth.");
        }

        var sensitiveProperty = saved.GetType().GetProperty("Name")?.GetValue(saved) is string name && Secret(name);
        foreach (var p in Properties(saved.GetType(), root))
        {
            if (Presence.TryGetValue(saved, out var present) && !present.Contains(p.Name))
            {
                continue;
            }

            if (p.Name == "Id" || p.Name == "Value" && sensitiveProperty)
            {
                continue;
            }

            var old = p.GetValue(current); var value = p.GetValue(saved);
            if (value is string text && old is string oldText && TryJson(text, out var savedJson) && TryJson(oldText, out var currentJson))
            {
                MergeJson(currentJson, savedJson); p.SetValue(current, currentJson!.ToJsonString());
            }
            else if (value is IList savedList && old is IList oldList && !Scalar(p.PropertyType))
            {
                for (var index = 0; index != savedList.Count; ++index)
                {
                    var item = savedList[index];
                    if (item == null || !Owned.ContainsKey(item.GetType().Name))
                    {
                        continue;
                    }

                    var identity = item.GetType().GetProperty("Id");
                    var id = identity?.GetValue(item);
                    var previous = id == null ? null : oldList.Cast<object>().FirstOrDefault(candidate => Equals(identity!.GetValue(candidate), id));
                    if (previous == null)
                    {
                        continue;
                    }

                    MergeObject(previous, item, false, depth + 1);
                    savedList[index] = previous;
                }
                p.SetValue(current, savedList);
            }
            else if (value != null && old != null && !Scalar(p.PropertyType) && value is not IEnumerable && p.PropertyType.GetProperty("Id") == null)
            {
                MergeObject(old, value, false, depth + 1);
            }
            else
            {
                p.SetValue(current, value);
            }
        }
    }

    private static bool SecretValue(JsonObject obj, string key)
    {
        if (!key.Equals("Value", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var name = obj.FirstOrDefault(p => p.Key.Equals("Name", StringComparison.OrdinalIgnoreCase)).Value;
        return name is JsonValue value && value.TryGetValue<string>(out var text) && Secret(text);
    }

    internal static string? RestoreSettings(string? current, string? saved)
    {
        if (current != null && saved != null && TryJson(current, out var old) && TryJson(saved, out var state))
        {
            RejectSecrets(state);
            MergeJson(old, state);
            return old!.ToJsonString();
        }
        return saved;
    }

    private static void MergeJson(JsonNode? current, JsonNode? saved)
    {
        if (current is JsonArray targetArray && saved is JsonArray sourceArray)
        {
            var merged = new List<JsonNode?>();
            for (var index = 0; index < sourceArray.Count; ++index)
            {
                var incoming = sourceArray[index];
                JsonNode? previous = index < targetArray.Count ? targetArray[index] : null;
                if (incoming is JsonObject incomingObject)
                {
                    var identity = incomingObject.FirstOrDefault(p => p.Key.Equals("Id", StringComparison.OrdinalIgnoreCase) || p.Key.Equals("Name", StringComparison.OrdinalIgnoreCase));
                    if (identity.Key != null)
                    {
                        previous = targetArray.OfType<JsonObject>().FirstOrDefault(p => p.TryGetPropertyValue(identity.Key, out var candidate) && JsonNode.DeepEquals(candidate, identity.Value));
                    }
                }
                var result = previous?.DeepClone();
                if (result is JsonObject && incoming is JsonObject || result is JsonArray && incoming is JsonArray)
                {
                    MergeJson(result, incoming);
                }
                else
                {
                    result = incoming?.DeepClone();
                }

                merged.Add(result);
            }
            targetArray.Clear();
            foreach (var item in merged)
            {
                targetArray.Add(item);
            }

            return;
        }
        if (current is not JsonObject target || saved is not JsonObject source)
        {
            return;
        }

        foreach (var key in target.Select(p => p.Key).ToArray())
        {
            if (!Secret(key) && !SecretValue(target, key) && !SecretValue(source, key) && !source.ContainsKey(key))
            {
                target.Remove(key);
            }
        }

        foreach (var entry in source)
        {
            if (Secret(entry.Key) || SecretValue(source, entry.Key))
            {
                throw new ArgumentException("Secret values cannot be restored from history.");
            }

            if (target[entry.Key] is JsonObject old && entry.Value is JsonObject incoming)
            {
                MergeJson(old, incoming);
            }
            else if (target[entry.Key] is JsonArray oldArray && entry.Value is JsonArray incomingArray)
            {
                MergeJson(oldArray, incomingArray);
            }
            else
            {
                target[entry.Key] = entry.Value?.DeepClone();
            }
        }
    }
}

internal sealed record GXHistoryModuleSetting(string ModuleId, string? Settings);
