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
using System.Text.RegularExpressions;
namespace Gurux.DLMS.AMI.Shared;

/// <summary>Normalizes and compares release versions, including prerelease identifiers and optional fourth version components.</summary>
[System.ComponentModel.Description("Normalizes and compares release versions, including prerelease identifiers and optional fourth version components.")]
public sealed partial class GXReleaseVersionComparer : IComparer<string?>
{
    /// <summary>Gets the shared release version comparer.</summary>
    [System.ComponentModel.Description("Gets the shared release version comparer.")]
    public static GXReleaseVersionComparer Instance { get; } = new();

    static readonly Regex Pattern = MyRegex();

    /// <summary>Attempts to normalize a release version, padding missing core components and preserving prerelease and build metadata.</summary>
    /// <param name="value">Version text, optionally prefixed by v or V.</param>
    /// <param name="normalized">Normalized version text, or an empty string if validation fails.</param>
    /// <returns>True if the value is a supported release version; otherwise false.</returns>
    [System.ComponentModel.Description("Attempts to normalize a release version, padding missing core components and preserving prerelease and build metadata.")]
    public static bool TryNormalize(string? value, out string normalized)
    {
        normalized = "";
        if (string.IsNullOrWhiteSpace(value) || value.Length > 128)
        {
            return false;
        }

        var m = Pattern.Match(value.Trim());
        if (!m.Success)
        {
            return false;
        }

        var numbers = new List<uint>();
        foreach (var part in m.Groups["core"].Value.Split('.'))
        {
            if (!uint.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
            {
                return false;
            }

            numbers.Add(number);
        } while (numbers.Count < 3)
        {
            numbers.Add(0);
        }

        if (numbers.Count == 4 && numbers[3] == 0)
        {
            numbers.RemoveAt(3);
        }

        var pre = m.Groups["pre"].Value;
        if (pre.Split('.').Any(p => p.Length > 1 && p.All(char.IsAsciiDigit) && p[0] == '0'))
        {
            return false;
        }

        normalized = string.Join('.', numbers.Select(n => n.ToString(CultureInfo.InvariantCulture))) + (pre.Length == 0 ? "" : "-" + pre) + (m.Groups["build"].Success ? "+" + m.Groups["build"].Value : "");
        return true;
    }

    /// <summary>Determines whether the version is valid and includes a prerelease identifier.</summary>
    /// <param name="value">Version text to validate and inspect.</param>
    /// <returns>True if the version is valid and has a prerelease component.</returns>
    [System.ComponentModel.Description("Determines whether the version is valid and includes a prerelease identifier.")]
    public static bool IsPrerelease(string value) => TryNormalize(value, out var n) && n.Split('+')[0].Contains('-');

    /// <summary>Compares release versions, ignoring build metadata and sorting invalid versions before valid versions.</summary>
    /// <param name="left">First version to compare.</param>
    /// <param name="right">Second version to compare.</param>
    /// <returns>A negative value, zero, or a positive value when the first version precedes, equals, or follows the second.</returns>
    [System.ComponentModel.Description("Compares release versions, ignoring build metadata and sorting invalid versions before valid versions.")]
    public int Compare(string? left, string? right)
    {
        if (!TryNormalize(left, out var a))
        {
            return TryNormalize(right, out _) ? -1 : 0;
        }

        if (!TryNormalize(right, out var b))
        {
            return 1;
        }

        var am = Pattern.Match(a);
        var bm = Pattern.Match(b);
        var ac = am.Groups["core"].Value.Split('.').Select(uint.Parse).ToArray();
        var bc = bm.Groups["core"].Value.Split('.').Select(uint.Parse).ToArray();
        for (var i = 0; i < 4; i++)
        {
            var cmp = (i < ac.Length ? ac[i] : 0).CompareTo(i < bc.Length ? bc[i] : 0);
            if (cmp != 0)
            {
                return cmp;
            }
        }
        var ap = am.Groups["pre"].Value;
        var bp = bm.Groups["pre"].Value;
        if (ap.Length == 0 || bp.Length == 0)
        {
            return ap.Length == bp.Length ? 0 : ap.Length == 0 ? 1 : -1;
        }

        var aa = ap.Split('.');
        var ba = bp.Split('.');
        for (var i = 0; i < Math.Min(aa.Length, ba.Length); i++)
        {
            bool an = aa[i].All(char.IsAsciiDigit), bn = ba[i].All(char.IsAsciiDigit);
            int cmp = an && bn ? aa[i].Length.CompareTo(ba[i].Length) : an != bn ? an ? -1 : 1 : 0;
            if (cmp == 0)
            {
                cmp = string.Compare(aa[i], ba[i], StringComparison.Ordinal);
            }

            if (cmp != 0)
            {
                return cmp;
            }
        }
        return aa.Length.CompareTo(ba.Length);
    }

    /// <summary>Matches supported release versions and separates numeric, prerelease, and build components.</summary>
    /// <returns>The cached, culture-invariant regular expression generated for release-version parsing.</returns>
    [GeneratedRegex(@"^[vV]?(?<core>\d+(?:\.\d+){1,3})(?:-(?<pre>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+(?<build>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$", RegexOptions.CultureInvariant)]
    private static partial Regex MyRegex();
}
