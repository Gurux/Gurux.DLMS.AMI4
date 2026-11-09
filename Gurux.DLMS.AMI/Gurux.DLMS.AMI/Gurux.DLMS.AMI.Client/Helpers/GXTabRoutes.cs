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

using Microsoft.AspNetCore.Components;
using Gurux.UI.Components.Tab;
using System.Web;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Stable URL names for management tabs and nested editor tabs.</summary>
public static class GXTabRoutes
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Groups = CreateGroups();

    private static Dictionary<string, IReadOnlyDictionary<string, string>> CreateGroups()
    {
        var groups = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in new[] { "Agent", "Gateway", "Device", "DeviceTemplate", "User", "Schedule",
            "config/Script", "config/Workflow", "config/KeyManagement", "config/Block", "config/Menu", "config/View", "config/Component", "config/Trigger", "config/Module" })
        {
            var tabs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [root] = root, [root + "Group"] = root + "/Groups" };
            if (root is not ("User" or "DeviceTemplate" or "config/Block" or "config/Menu" or "config/View" or "config/Component"))
            {
                tabs[root + "Log"] = root + "/Logs";
            }
            if (root is "Device" or "User")
            {
                tabs[root + "Action"] = root + "/Actions";
            }
            if (root == "Device")
            {
                tabs[root + "Trace"] = root + "/Traces";
            }
            if (root == "User")
            {
                tabs[root + "Error"] = root + "/Errors";
            }
            groups[root] = tabs;
        }
        groups["Notification"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Types"] = "Notification",
            ["ServerNotifications"] = "Notification/ServerNotifications"
        };
        groups["config"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["config"] = "Config",
            ["config/settings"] = "Config/settings"
        };
        groups["config/access-control"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Roles"] = "config/access-control/Roles",
            ["Policies"] = "config/access-control/Policies"
        };
        groups["config/language"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Language"] = "config/language/Language",
            ["Localize"] = "config/language/Localize"
        };
        return groups;
    }

    public static string QueryKey(int depth) => TabUrlState.QueryKey(depth);

    public static bool IsTabNavigation(string source, string target)
    {
        if (!Uri.TryCreate(source, UriKind.Absolute, out var before) || !Uri.TryCreate(before, target, out var after)
            || before.GetLeftPart(UriPartial.Authority) != after.GetLeftPart(UriPartial.Authority) || before.Fragment != after.Fragment)
        {
            return false;
        }
        if (before.AbsolutePath != after.AbsolutePath &&
            (EditorTabHost(before) is not { } host || host != EditorTabHost(after)))
        {
            return false;
        }
        var oldQuery = HttpUtility.ParseQueryString(before.Query);
        var newQuery = HttpUtility.ParseQueryString(after.Query);
        static bool IsTab(string key) => key.Equals("tab", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("tab", StringComparison.OrdinalIgnoreCase) && int.TryParse(key.AsSpan(3), out int level) && level >= 2;
        var oldKeys = oldQuery.AllKeys.OfType<string>().Where(k => !IsTab(k)).Order(StringComparer.Ordinal).ToArray();
        var newKeys = newQuery.AllKeys.OfType<string>().Where(k => !IsTab(k)).Order(StringComparer.Ordinal).ToArray();
        return oldKeys.SequenceEqual(newKeys) && oldKeys.All(key =>
            (oldQuery.GetValues(key) ?? []).SequenceEqual(newQuery.GetValues(key) ?? []));
    }

    private static string? EditorTabHost(Uri uri)
    {
        if (NotificationEditorHost(uri.AbsolutePath) is { } notification) {
            return notification.ToLowerInvariant();
        }
        foreach (var (host, routes) in new[]
        {
            ("Notification", new[] { "Notification", "Notification/ServerNotifications" }),
            ("config/access-control", new[] { "config/access-control", "config/access-control/Roles", "config/access-control/Policies", "config/roles", "config/policy" }),
            ("config/language", new[] { "config/language", "config/language/Language", "config/language/Localize" })
        })
        {
            foreach (var route in routes)
            {
                var suffix = "/" + route;
                if (uri.AbsolutePath.TrimEnd('/').EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    var path = uri.AbsolutePath.TrimEnd('/');
                    return (path[..^suffix.Length] + "/" + host).ToLowerInvariant();
                }
            }
        }
        return null;
    }

    private static string? NotificationEditorHost(string path)
    {
        path = path.TrimEnd('/');
        var segments = path.Split('/');
        if (segments.Last() is "Notifications" or "GeneralInformation")
        {
            path = path[..path.LastIndexOf('/')];
            segments = path.Split('/');
        }
        return segments.Length >= 4 && Guid.TryParse(segments[^1], out _)
            && segments[^3].Equals("Notification", StringComparison.OrdinalIgnoreCase)
            && (segments[^2].Equals("Edit", StringComparison.OrdinalIgnoreCase)
                || segments[^2].Equals("Remove", StringComparison.OrdinalIgnoreCase)
                || segments[^2].Equals("Add", StringComparison.OrdinalIgnoreCase)) ? path : null;
    }
    public static string? Active(NavigationManager navigation, string? group, int depth, string? fallback)
    {
        var uri = new Uri(navigation.Uri);
        if (TabUrlState.Active(navigation, depth, null) is { } selected) {
            return selected;
        }
        if (group == "notification-editor" && NotificationEditorHost(uri.AbsolutePath) is { } host)
            {
            return uri.AbsolutePath.TrimEnd('/').Length > host.Length ? uri.AbsolutePath.TrimEnd('/')[(host.Length + 1)..] : fallback;
        }
        if (group != null && Groups.TryGetValue(group, out var tabs))
        {
            var path = navigation.ToBaseRelativePath(uri.GetLeftPart(UriPartial.Path)).Trim('/');
            path = GXConfigurationRoutes.LegacyPath(path);
            return tabs.FirstOrDefault(t => t.Value.Equals(path, StringComparison.OrdinalIgnoreCase) || t.Key.Equals(path, StringComparison.OrdinalIgnoreCase)).Key
                ?? tabs.FirstOrDefault(t => path.StartsWith(t.Key + "/", StringComparison.OrdinalIgnoreCase)).Key
                ?? fallback ?? group;
        }
        return fallback;
    }

    public static string SelectionUrl(NavigationManager navigation, string? group, int depth, string tab)
    {
        var uri = new Uri(navigation.Uri);
        var target = uri.GetLeftPart(UriPartial.Path);
        string? queryTab = tab;
        if (group != null)
        {
            if (group == "notification-editor" && NotificationEditorHost(uri.AbsolutePath) is { } host)
            {
                target = uri.GetLeftPart(UriPartial.Authority) + host + "/" + Uri.EscapeDataString(tab);
                queryTab = null;
            }
            else if (Groups.TryGetValue(group, out var tabs) && tabs.TryGetValue(tab.Trim('/'), out var route))
            {
                target = navigation.ToAbsoluteUri(GXConfigurationRoutes.Canonicalize(route)).GetLeftPart(UriPartial.Path);
                queryTab = null;
            }
            else if (group is "logs" or "appearance" or "updates" or "manufacturer" && !tab.StartsWith("module/", StringComparison.OrdinalIgnoreCase))
            {
                target = navigation.ToAbsoluteUri(tab.TrimStart('/')).GetLeftPart(UriPartial.Path);
                queryTab = null;
            }
        }
        return TabUrlState.SelectionUrl(navigation, depth, queryTab, target);
    }
}