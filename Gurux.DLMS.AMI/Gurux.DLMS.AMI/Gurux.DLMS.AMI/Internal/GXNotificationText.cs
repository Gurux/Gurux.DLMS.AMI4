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

using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Supplies readable event text while preserving descriptions provided by repositories and modules.</summary>
internal static class GXNotificationText
{
    /// <summary>Fills missing notification text without changing routing, item identity, or custom producer text.</summary>
    internal static void Fill(GXTarget target)
    {
        int separator = target.Type?.LastIndexOf('.') ?? -1;
        string resource = separator > 0 ? target.Type![..separator] : target.Type ?? "notification";
        if (string.IsNullOrWhiteSpace(resource))
        {
            resource = "notification";
        }
        string action = separator >= 0 ? target.Type![(separator + 1)..] : "";
        string label = resource switch
        {
            "performance" => "Performance data",
            "system-log" => "System log",
            "user-action" => "User activity log",
            "user-error" => "User error log",
            "device-action" => "Device activity log",
            "device-trace" => "Device trace",
            "value" => "Measurement value",
            "configuration" => "Server configuration",
            "sql" => "SQL log",
            "signalr" => "SignalR log",
            _ => char.ToUpperInvariant(resource[0]) + resource[1..].Replace('-', ' ')
        };
        string verb = action switch
        {
            "add" => resource == "performance" ? "saved" : "added",
            "edit" or "update" => "updated",
            "delete" or "remove" => "deleted",
            "clear" => "cleared",
            "close" => "closed",
            "execute" or "run" => "executed",
            "start" => "started",
            "end" => "completed",
            "error" => "error",
            "status" or "status-change" => "status changed",
            "refresh" => "refreshed",
            "regenerate" => "regenerated",
            "capture" => "recorded",
            _ => "changed"
        };
        if (string.IsNullOrWhiteSpace(target.Title))
        {
            target.Title = label + " " + verb;
        }
        if (!string.IsNullOrWhiteSpace(target.Description))
        {
            return;
        }
        target.Description = (resource, action) switch
        {
            ("performance", "add") => "Completed performance measurements were saved.",
            ("performance", "clear") => "Performance history was cleared and current measurement counters were reset.",
            ("performance", "delete") => "The selected performance measurements were deleted.",
            (_, "clear") when resource.EndsWith("log", StringComparison.Ordinal) => "The selected entries in the " + label.ToLowerInvariant() + " were cleared.",
            (_, "add") when resource.EndsWith("log", StringComparison.Ordinal) => "A new entry was added to the " + label.ToLowerInvariant() + ".",
            (_, "add") => "A new " + label.ToLowerInvariant() + " was added.",
            (_, "edit" or "update") => "Changes to the selected " + label.ToLowerInvariant() + " were saved.",
            (_, "delete" or "remove") => "The selected " + label.ToLowerInvariant() + " was deleted.",
            (_, "clear") => "The selected " + label.ToLowerInvariant() + " data was cleared.",
            (_, "error") => "An error occurred while processing the " + label.ToLowerInvariant() + ".",
            _ => label + " " + verb + "."
        };
    }
}
