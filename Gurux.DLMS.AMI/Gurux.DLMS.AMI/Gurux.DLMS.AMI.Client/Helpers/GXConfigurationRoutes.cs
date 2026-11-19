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

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Canonical configuration landing-page routes and their legacy aliases.</summary>
public static class GXConfigurationRoutes
{
    /// <summary>Legacy configuration component selectors paired with their canonical landing-page routes.</summary>
    private static readonly (string Legacy, string Canonical)[] _targets =
    [
        ("system", "system"),
        ("status", "status"),
        ("security", "security"),
        ("language", "languages"),
        ("Module", "modules"),
        ("agent", "agents"),
        ("Component", "components"),
        ("Block", "blocks"),
        ("Menu", "menus"),
        ("Tab", "tabs"),
        ("log", "logs"),
        ("View", "views"),
        ("Trigger", "triggers"),
        ("Workflow", "workflows"),
        ("prune", "prune"),
        ("appearance", "appearance"),
        ("Account", "account"),
        ("Manufacturer", "manufacturers"),
        ("cron", "cron"),
        ("KeyManagement", "key-management"),
        ("authentications", "authentication"),
        ("statistic", "statistics"),
        ("maintenance", "maintenance"),
        ("Script", "scripts"),
        ("changehistory", "history"),
        ("http-proxy", "http-proxy"),
        ("access-control", "access-control"),
    ];

    /// <summary>Normalizes known landing pages, retaining query strings and fragments.</summary>
    /// <param name="path">Application path to normalize or translate.</param>
    /// <returns>The canonical route with its original query and fragment, or the unchanged input for unknown routes.</returns>
    public static string Canonicalize(string path)
    {
        int suffixIndex = path.IndexOfAny(['?', '#']);
        var route = suffixIndex < 0 ? path : path[..suffixIndex];
        var suffix = suffixIndex < 0 ? string.Empty : path[suffixIndex..];
        var parts = route.Trim('/').Split('/');
        var prefix = route.StartsWith('/') ? "/" : string.Empty;
        if (parts.Length == 1 && parts[0].Equals("config", StringComparison.OrdinalIgnoreCase))
        {
            return prefix + "config" + suffix;
        }
        if (parts.Length != 2 || !parts[0].Equals("config", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }
        foreach (var target in _targets)
        {
            if (parts[1].Equals(target.Legacy, StringComparison.OrdinalIgnoreCase)
                || parts[1].Equals(target.Canonical, StringComparison.OrdinalIgnoreCase)
                || (target.Canonical == "agents" && parts[1].Equals("AgentInstallers", StringComparison.OrdinalIgnoreCase))
                || (target.Canonical == "views" && parts[1].Equals("view", StringComparison.OrdinalIgnoreCase)))
            {
                return prefix + "config/" + target.Canonical + suffix;
            }
        }
        return path;
    }

    /// <summary>Resolves a canonical target to the existing component selector.</summary>
    /// <param name="target">Configuration route target to resolve.</param>
    /// <returns>The legacy selector for a known canonical target, or the original target when unknown.</returns>
    public static string? LegacyTarget(string? target)
    {
        foreach (var entry in _targets)
        {
            if (string.Equals(target, entry.Canonical, StringComparison.OrdinalIgnoreCase))
            {
                return entry.Legacy;
            }
        }
        return target;
    }

    /// <summary>Restores an internal landing-page path used by existing management components.</summary>
    /// <param name="path">Application path to normalize or translate.</param>
    /// <returns>The legacy configuration landing path, or the original path outside the configuration route.</returns>
    public static string LegacyPath(string path)
    {
        var parts = path.Trim('/').Split('/');
        return parts.Length == 2 && parts[0].Equals("config", StringComparison.OrdinalIgnoreCase)
            ? (path.StartsWith('/') ? "/" : string.Empty) + "config/" + LegacyTarget(parts[1]) : path;
    }
}
