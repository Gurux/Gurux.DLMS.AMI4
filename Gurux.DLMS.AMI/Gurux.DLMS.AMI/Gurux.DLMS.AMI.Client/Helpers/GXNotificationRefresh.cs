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

using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.UI.Components;
using Gurux.UI.Components.Table;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Notification-driven table reads, scoped to the table's own loaded versions and lifetime.</summary>
public static partial class GXNotificationRefresh
{
    /// <summary>Refresh coordinators held only while their owning table or view remains alive.</summary>
    private static readonly ConditionalWeakTable<object, GXUpdateRefreshCoordinator> _coordinators = [];

    /// <summary>Combines overlapping notification reads without changing manual searches or event delivery.</summary>
    /// <param name="table">Table whose loaded rows and lifetime determine whether a refresh is needed.</param>
    /// <param name="updates">Resource update notifications that may invalidate the currently loaded view.</param>
    /// <param name="refresh">Callback that reloads the displayed data.</param>
    /// <typeparam name="T">Type of table row whose identifier and concurrency stamp are inspected.</typeparam>
    /// <returns>A task that completes after the coordinated refresh has processed the supplied updates.</returns>
    public static Task RefreshAsync<T>(GXTable<T>? table, IEnumerable<GXTarget> updates, Func<Task>? refresh = null)
    {
        if (table == null)
        {
            return refresh == null ? Task.CompletedTask
                    : RefreshViewAsync(refresh.Target ?? refresh, updates, refresh);
        }
        return _coordinators.GetValue(table, _ => new(
            events => NeedsRefresh(table.Items, events),
            refresh ?? (() => table.RefreshDataAsync(true)))).RequestAsync(updates);
    }

    /// <summary>Combines invalidations for a view without a row-version snapshot.</summary>
    /// <param name="view">Owning view instance used to scope the refresh coordinator.</param>
    /// <param name="updates">Resource update notifications that may invalidate the currently loaded view.</param>
    /// <param name="refresh">Callback that reloads the displayed data.</param>
    /// <returns>A task that completes after the coordinated view refresh.</returns>
    public static Task RefreshViewAsync(object view, IEnumerable<GXTarget> updates, Func<Task> refresh)
        => _coordinators.GetValue(view, _ => new(_ => true, refresh)).RequestAsync(updates);

    /// <summary>Stops queued notification reads for a disposed table.</summary>
    /// <param name="table">Table whose loaded rows and lifetime determine whether a refresh is needed.</param>
    public static void Remove(object table)
    {
        _coordinators.GetValue(table, _ => new(_ => false, () => Task.CompletedTask)).Dispose();
        // Retain the stopped entry while the table is alive so late events cannot restart it.
    }

    /// <summary>Only an exact edit of an already loaded row version may skip a read.</summary>
    /// <param name="rows">Currently loaded rows and their concurrency stamps, or null when not loaded.</param>
    /// <param name="updates">Resource update notifications that may invalidate the currently loaded view.</param>
    /// <typeparam name="T">Type of table row whose identifier and concurrency stamp are inspected.</typeparam>
    /// <returns>False only when every update is an exact edit of an already loaded row version; otherwise true.</returns>
    public static bool NeedsRefresh<T>(IEnumerable<T>? rows, IEnumerable<GXTarget> updates)
    {
        var items = rows?.ToArray();
        foreach (var update in updates)
        {
            if (update.Type != Row<T>.EditType || !string.IsNullOrEmpty(update.Source) ||
                string.IsNullOrEmpty(update.Id) || string.IsNullOrEmpty(update.ConcurrencyStamp) ||
                items == null || Row<T>.Id == null || Row<T>.Version == null)
            {
                return true;
            }
            var loaded = items.Any(item => item != null && Row<T>.MatchesId(item, update.Id) &&
                string.Equals(Row<T>.Version.GetValue(item) as string, update.ConcurrencyStamp, StringComparison.Ordinal));
            if (!loaded)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Caches reflected row identity and version metadata used to recognize already loaded edits.</summary>
    /// <typeparam name="T">Type of table row whose identifier and concurrency stamp are inspected.</typeparam>
    private static partial class Row<T>
    {
        /// <summary>Reflected public row identifier, or null when the model does not expose one.</summary>
        internal static readonly PropertyInfo? Id = typeof(T).GetProperty("Id");
        /// <summary>Reflected concurrency stamp property, or null when the model does not expose one.</summary>
        internal static readonly PropertyInfo? Version = typeof(T).GetProperty("ConcurrencyStamp");
        /// <summary>Notification edit event name derived from the row&apos;s resource model name.</summary>
        internal static readonly string EditType = ResourceName() + ".edit";

        /// <summary>Converts the row model name to the resource prefix used by notification event identifiers.</summary>
        /// <returns>The lowercase hyphen-separated model name without its GX prefix.</returns>
        private static string ResourceName()
        {
            var name = typeof(T).Name;
            if (name.StartsWith("GX", StringComparison.Ordinal))
            {
                name = name[2..];
            }
            return MyRegex().Replace(name, "$1-$2").ToLowerInvariant();
        }

        /// <summary>Compares a row identifier with a notification identifier using GUID or invariant string semantics.</summary>
        /// <param name="item">Row or resource whose identifier is inspected.</param>
        /// <param name="id">Identifier of the requested resource or pending confirmation.</param>
        /// <returns>True when the row and notification identify the same resource.</returns>
        internal static bool MatchesId(T item, string id)
        {
            var value = Id!.GetValue(item);
            return value is Guid guid ? Guid.TryParse(id, out var parsed) && parsed == guid
                : string.Equals(Convert.ToString(value, CultureInfo.InvariantCulture), id, StringComparison.Ordinal);
        }

        /// <summary>Matches lowercase-or-digit to uppercase boundaries for notification resource name conversion.</summary>
        /// <returns>The generated reusable model-name boundary expression.</returns>
        [GeneratedRegex("([a-z0-9])([A-Z])")]
        private static partial Regex MyRegex();
    }
}
