//
// --------------------------------------------------------------------------
//  Gurux Ltd
// 
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License 
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2. 
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using System.Globalization;
using System.Text.RegularExpressions;
namespace Gurux.DLMS.AMI.Shared;

/// <summary>Normalizes and compares release versions, including prerelease identifiers and optional fourth version components.</summary>
public sealed class GXReleaseVersionComparer : IComparer<string?>
{
    /// <summary>Gets the shared release version comparer.</summary>
    public static GXReleaseVersionComparer Instance { get; } = new();

    static readonly Regex Pattern = new(@"^[vV]?(?<core>\d+(?:\.\d+){1,3})(?:-(?<pre>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+(?<build>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$", RegexOptions.CultureInvariant);

    /// <summary>Attempts to normalize a release version, padding missing core components and preserving prerelease and build metadata.</summary>
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
    public static bool IsPrerelease(string value) => TryNormalize(value, out var n) && n.Split('+')[0].Contains('-');

    /// <summary>Compares release versions, ignoring build metadata and sorting invalid versions before valid versions.</summary>
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
}
