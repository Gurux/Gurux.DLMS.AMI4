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

using Gurux.UI.Components;
using System.Drawing;
using System.Globalization;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Maps the existing AMI theme payload to the shared appearance catalog.</summary>
public static class AmiThemeAdapter
{
    public const string ThemeId = "ami-current";

    private const string BundledPrefix = "_content/Gurux.UI.Components/Appearance/themes/";

    /// <summary>Use the component library catalog for AMI's default theme resources.</summary>
    public static IReadOnlyList<GXThemeInfo> GetDefaults(IGXThemeService appearance)
    {
        return appearance.GetThemes()
            .Where(x => x.Id != ThemeId && x.CssUrl.StartsWith(BundledPrefix, StringComparison.Ordinal))
            .Select(x => new GXThemeInfo { Name = x.Name, CssFile = x.CssUrl })
            .ToList();
    }

    public static GXThemeDefinition? Create(GXThemeInfo? theme, IGXThemeService appearance)
    {
        if (theme == null || (string.IsNullOrWhiteSpace(theme.Name) && string.IsNullOrWhiteSpace(theme.CssFile) && theme.Styles?.Count is not > 0))
        {
            return null;
        }
        var url = theme.CssFile?.Trim() ?? "";
        var builtIn = appearance.GetThemes().FirstOrDefault(x => x.Id != ThemeId &&
            x.CssUrl.StartsWith(BundledPrefix, StringComparison.Ordinal) &&
            (string.Equals(url.TrimStart('/'), x.CssUrl, StringComparison.Ordinal) ||
             string.Equals(url, $"https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/{x.Id}/bootstrap.min.css", StringComparison.OrdinalIgnoreCase) ||
             (url.Length == 0 && x.Id == "default" && string.Equals(theme.Name, x.Name, StringComparison.OrdinalIgnoreCase))));
        if (builtIn != null && theme.Styles?.Any(x => !string.IsNullOrWhiteSpace(x.Value)) is not true)
        {
            return builtIn;
        }
        var result = new GXThemeDefinition
        {
            Id = ThemeId,
            Name = string.IsNullOrWhiteSpace(theme.Name) ? "AMI" : theme.Name,
            CssUrl = builtIn?.CssUrl ?? url,
            // External and additive CSS is reserved for user-created themes.
            AllowExternalStylesheet = builtIn == null,
            ReplaceBootstrap = builtIn != null,
            IsDark = builtIn?.IsDark ?? false,
            SupportsColorModes = builtIn?.SupportsColorModes ?? true
        };
        foreach (var style in theme.Styles ?? [])
        {
            if (string.IsNullOrWhiteSpace(style.Value))
            {
                continue;
            }
            result.CssVariables[style.Name] = style.Value;
            if (style.Type == 0 && TryColor(style.Value, out var color))
            {
                result.CssVariables[style.Name + "-rgb"] = $"{color.R},{color.G},{color.B}";
            }
        }
        return result;
    }

    private static bool TryColor(string value, out Color color)
    {
        if (value.StartsWith('#'))
        {
            var hex = value[1..];
            if (hex.Length == 3)
            {
                hex = string.Concat(hex.Select(c => new string(c, 2)));
            }
            if (hex.Length == 6 && int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
            {
                color = Color.FromArgb((rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);
                return true;
            }
            color = default;
            return false;
        }
        color = Color.FromName(value);
        return color.IsKnownColor;
    }
}
