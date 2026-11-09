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

using System.Text.RegularExpressions;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Maps stable stored tab keys to contextual documentation topics.</summary>
internal static class GXHelpTopics
{
    internal const string ScopeName = Gurux.UI.Components.Help.GXHelpContext.ScopeName;
    private sealed record Menu(string Topic, string? Primary = null, string? Groups = null, string? Logs = null, bool Nested = false);
    private static readonly Dictionary<string, Menu> Menus = new(StringComparer.Ordinal)
    {
        ["DeviceTab"] = new("devices", "device", "device-group", "device-log"),
        ["DeviceTemplateTab"] = new("device-templates", "device-template", "device-template-group"),
        ["ScheduleTab"] = new("schedules", "schedule", "schedule-group", "schedule-log"),
        ["AgentTab"] = new("agents", "agent", "agent-group", "agent-log"),
        ["GatewayTab"] = new("gateways", "gateway", "gateway-group", "gateway-log"),
        ["ModuleTab"] = new("modules", "module", "module-group", "module-log"),
        ["UserTab"] = new("users", "user", "user-group"),
        ["ScriptTab"] = new("scripts", "script", "script-group", "script-log"),
        ["WorkflowTab"] = new("workflows", "workflow", "workflow-group", "workflow-log"),
        ["BlockTab"] = new("blocks", "block", "block-group"),
        ["MenuTab"] = new("menus", "menu", "menu-group"),
        ["ViewTab"] = new("views", "view", "view-group"),
        ["TriggerTab"] = new("triggers", "trigger"),
        ["ComponentViewTab"] = new("components", "component"),
        ["KeyManagementTab"] = new("key-managements", "key-management", "key-management-group", "key-management-log"),
        ["ManufacturerTab"] = new("manufacturers", "manufacturer"),
        ["AppearanceTab"] = new("appearance"),
        ["ConfigurationTab"] = new("config", "config"),
        ["UpdateTab"] = new("updates", "updates"),
        ["LogTab"] = new("logs"),
        ["DeviceDetailsTab"] = new("device", Groups: "device-group"),
        ["DeviceTemplateDetailsTab"] = new("device-template", Groups: "device-template-group"),
        ["ScheduleDetailsTab"] = new("schedule", Groups: "schedule-group"),
        ["AgentDetailsTab"] = new("agent", Groups: "agent-group"),
        ["GatewayDetailsTab"] = new("gateway", Groups: "gateway-group"),
        ["ModuleDetailsTab"] = new("module", Groups: "module-group"),
        ["UserDetailsTab"] = new("user"),
        ["ScriptDetailsTab"] = new("script", Groups: "script-group"),
        ["WorkflowDetailsTab"] = new("workflow", Groups: "workflow-group"),
        ["TriggerDetailsTab"] = new("trigger"),
        ["ViewDetailsTab"] = new("view", Groups: "view-group"),
        ["ComponentViewDetailsTab"] = new("component-view", Groups: "component-view-group"),
        ["BlockDetailsTab"] = new("block", Groups: "block-group"),
        ["MenuDetailsTab"] = new("menu", Groups: "menu-group"),
        ["KeyManagementDetailsTab"] = new("key-management", Groups: "key-management-group"),
        ["ManufacturerDetailsTab"] = new("manufacturer"),
        ["ThemeEditorTab"] = new("theme"),
        ["WorkflowGroupTab"] = new("workflow-group"),
        ["GatewayGroupTab"] = new("gateway-group"),
        ["ModuleGroupTab"] = new("module-group"),
        ["MenuGroupTab"] = new("menu-group"),
        ["DeviceTemplateGroupTab"] = new("device-template-group"),
        ["ViewGroupTab"] = new("view-group"),
        ["TriggerGroupTab"] = new("trigger-group"),
        ["ScriptGroupTab"] = new("script-group"),
        ["KeyManagementGroupTab"] = new("key-management-group"),
        ["ScheduleGroupTab"] = new("schedule-group"),
        ["ComponentViewGroupTab"] = new("component-view-group"),
        ["BlockGroupTab"] = new("block-group"),
        ["AgentGroupTab"] = new("agent-group"),
        ["DeviceGroupTab"] = new("device-group"),
        ["UserGroupTab"] = new("user-group"),
        ["AgentSettingsTab"] = new("agent"),
        ["ThemeDetailsTab"] = new("theme", "theme"),
        ["DeviceSelectorTab"] = new("device-selection", "devices", Nested: true),
        ["DeviceGroupSelectorTab"] = new("device-group-selection", "device-groups", Nested: true),
        ["DeviceTemplateSettingsTab"] = new("device-template"),
        ["ObjectTemplateDetailsTab"] = new("object-template"),
        ["ObjectDetailsTab"] = new("object"),
        ["ChargeDetailsTab"] = new("charge"),
        ["ConfigurationLanguageTab"] = new("config.languages", "language"),
        ["AccessControlTab"] = new("config.access-control"),
        ["LogTypeDetailsTab"] = new("log-type"),
        ["NotificationTab"] = new("notifications"),
        ["NotificationDetailsTab"] = new("notification"),
        ["PolicyTab"] = new("policy", Nested: true)
    };

    internal static string ForTab(string menuName, GXMenuLink link, string? containingTopic = null, string? pageTopic = null)
    {
        var path = (link.Url ?? string.Empty).Split('?', '#')[0].Trim('/');
        var key = Key(path.Split('/').LastOrDefault() ?? string.Empty);
        var definition = Menus.GetValueOrDefault(menuName) ?? new Menu(Key(menuName.EndsWith("Tab", StringComparison.Ordinal) ? menuName[..^3] : menuName));
        var topic = definition.Nested && !string.IsNullOrWhiteSpace(containingTopic) ? containingTopic : definition.Topic;
        if (menuName == "PolicyTab" && string.IsNullOrWhiteSpace(containingTopic) && !string.IsNullOrWhiteSpace(pageTopic))
        {
            topic = string.Join('.', pageTopic.Split('.').Select(Key));
        }
        if (menuName == "LogTab" && key == "general")
        {
            return "logs.general";
        }
        if (key is "general" or "general-information" || Matches(key, definition.Primary))
        {
            return topic;
        }
        if (Matches(key, definition.Groups))
        {
            key = "groups";
        }
        else if (Matches(key, definition.Logs))
        {
            key = "logs";
        }
        else
        {
            key = (menuName, key) switch
            {
                ("ScheduleDetailsTab", "device") => "devices",
                ("ScheduleDetailsTab", "device-group") => "device-groups",
                ("DeviceTab", "device-action") => "actions",
                ("DeviceTab", "device-trace") => "traces",
                ("UserTab", "user-error") => "errors",
                ("UserTab", "user-action") => "actions",
                ("ConfigurationTab", "settings") => "layout",
                ("AppearanceTab", "image") => "icons",
                ("AppearanceTab", "theme") => "themes",
                ("AppearanceTab", "iconpack" or "icon-pack") => "icon-packs",
                ("LogTab", "system-log") => "system",
                ("LogTab", "task") => "tasks",
                ("LogTab", "schedule-log") => "schedules",
                ("LogTab", "module-log") => "modules",
                ("LogTab", "script-log") => "scripts",
                ("LogTab", "workflow-log") => "workflows",
                ("LogTab", "key-management-log") => "key-management",
                ("LogTab", "gateway-log") => "gateways",
                ("LogTab", "agent-log") => "agents",
                ("LogTab", "device-log") => "devices",
                ("LogTab", "device-action") => "device-actions",
                ("LogTab", "device-trace") => "device-traces",
                ("LogTab", "changehistory") => "change-history",
                _ => key
            };
        }
        if (menuName == "LogTab" && key == "updates")
        {
            return "updates";
        }
        return string.IsNullOrEmpty(key) ? topic : topic + "." + key;
    }

    private static bool Matches(string key, string? expected)
        => expected != null && (key == expected || key == expected + "s");

    private static string Key(string value)
    {
        value = Regex.Replace(value, "([A-Z]+)([A-Z][a-z])", "$1-$2");
        value = Regex.Replace(value, "([a-z0-9])([A-Z])", "$1-$2");
        var key = Regex.Replace(value, "[^a-zA-Z0-9]+", "-").Trim('-').ToLowerInvariant();
        return key switch
        {
            "pushevents" => "push-events",
            "autoconnect" => "auto-connect",
            "defaultvalues" => "default-values",
            "unitchargeactive" => "active-unit-charge",
            "unitchargepassive" => "passive-unit-charge",
            _ => key
        };
    }
}
