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
using Gurux.DLMS.UI.Components.Help;
using Gurux.UI.Components.Help;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Maps active AMI topics to official manual chapters without implementing article loading.</summary>
public static class GXAmiManualHelpCatalog
{
    /// <summary>Base address of the online Gurux AMI manual.</summary>
    private static readonly Uri _address = new("https://www.gurux.fi/gurux.dlms.ami4/manual");

    /// <summary>Creates the shared article reader with AMI's trusted address allowlist.</summary>
    public static GXHelpArticleReader CreateReader(IHttpClientFactory clients) => new(
        () => clients.CreateClient("AmiManual"),
        IsAllowedAddress);

    /// <summary>Allows official manual chapters and known COSEM articles only.</summary>
    public static bool IsAllowedAddress(Uri address)
    {
        if (address.Scheme != "https" || !address.IsDefaultPort || address.Host != "www.gurux.fi" ||
            address.UserInfo.Length != 0 || address.Query.Length != 0 || address.Fragment.Length != 0)
        {
            return false;
        }
        if (address == _address || _chapters.Any(item => address == new Uri(_address + "/" + item.Chapter)))
        {
            return true;
        }
        var path = address.AbsolutePath;
        var finnish = path.StartsWith("/fi/", StringComparison.Ordinal);
        var prefix = finnish ? "/fi/Gurux.DLMS.Objects." : "/Gurux.DLMS.Objects.";
        return path.StartsWith(prefix, StringComparison.Ordinal) &&
            GXDLMSObjectHelpCatalog.GetAddress(new GXHelpRequest("cosem." + path[prefix.Length..], Culture: finnish ? "fi" : "en")) == address;
    }

    /// <summary>Manual chapters and topic identifiers used to build navigation entries.</summary>
    private static readonly (string Chapter, string[] Topics)[] _chapters =
    [
        ("getting-started", ["getting-started", "installation"]),
        ("gateways", ["gateway", "gateways"]),
        ("agents", ["agent", "agents", "reader-options", "auto-connect-options", "notify-options"]),
        ("devices", ["device", "devices"]),
        ("device-templates", ["device-template", "device-templates"]),
        ("schedules", ["schedule", "schedules"]),
        ("scripts", ["script", "scripts"]),
        ("workflows", ["workflow", "workflows"]),
        ("blocks", ["block", "blocks"]),
        ("modules", ["module", "modules"]),
        ("triggers", ["trigger", "triggers"]),
        ("menus", ["menu", "menus"]),
        ("views", ["view", "views"]),
        ("component-views", ["component-view", "component-views", "component", "components"]),
        ("manufacturers", ["manufacturer", "manufacturers"]),
        ("key-management", ["key-management", "key-managements"]),
        ("appearance", ["appearance", "theme", "themes", "image", "images", "icon", "icons", "icon-pack", "colors", "texts", "links"]),
        ("users-and-accounts", ["user", "users", "account", "accounts", "policy", "role", "personal-access-token"]),
        ("config", ["config", "configuration", "configurations", "language", "languages", "cron", "authentication", "authentications"]),
        ("notifications", ["notification", "notifications"]),
        ("objects-and-target-selection", ["object", "objects", "target-selection"]),
        ("logs-and-updates", ["log", "logs", "system-log", "task", "tasks", "update", "updates", "localization-updates"])
    ];

    /// <summary>Resolves active page/tab topics to a fixed official manual chapter.</summary>
    public static Uri GetAddress(string? topic)
    {
        if (string.IsNullOrWhiteSpace(topic) || topic.Length > 200) { return _address; }
        if (topic.StartsWith("tutorial/", StringComparison.OrdinalIgnoreCase)) { topic = topic[9..]; }
        if (topic.StartsWith("manual/", StringComparison.OrdinalIgnoreCase)) { topic = topic[7..]; }
        var key = Regex.Replace(topic, "([A-Z]+)([A-Z][a-z])", "$1-$2");
        key = Regex.Replace(key, "([a-z0-9])([A-Z])", "$1-$2");
        key = Regex.Replace(key, "[^a-zA-Z0-9.]+", "-").Trim('-').ToLowerInvariant();
        var match = _chapters.SelectMany(chapter => chapter.Topics.Select(alias => (Alias: alias, chapter.Chapter)))
            .OrderByDescending(item => item.Alias.Length)
            .FirstOrDefault(item => key == item.Alias || key.StartsWith(item.Alias + ".", StringComparison.Ordinal) || key.StartsWith(item.Alias + "-", StringComparison.Ordinal));
        return match.Chapter == null ? _address : new Uri(_address.AbsoluteUri + "/" + match.Chapter);
    }

}
