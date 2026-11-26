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

using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Expands saved buffer snapshots into individual display rows without changing persisted XML.</summary>
public static class GXProfileHistoryRows
{
    /// <summary>Retains ordinary history rows and expands each complete XML buffer into its constituent rows.</summary>
    /// <param name="values">Persisted history values in their retrieval order.</param>
    /// <returns>Independent display rows retaining each source value's read timestamp.</returns>
    /// <remarks>Snapshot row identifiers are deterministic display keys and must never be used to update stored values.</remarks>
    public static IEnumerable<GXProfileStoredRow> Expand(IEnumerable<GXProfileStoredRow> values)
    {
        foreach (var value in values)
        {
            XElement? xml = null;
            try
            {
                xml = XElement.Parse(value.Value ?? string.Empty);
            }
            catch (System.Xml.XmlException)
            {
                // Unrecognized saved values remain visible through the existing raw-value fallback.
            }
            if (xml?.Name.LocalName != "Array")
            {
                yield return value;
                continue;
            }
            int index = 0;
            foreach (var row in xml.Elements())
            {
                byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes(value.Id.ToString("N") + ":" + index.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                yield return new GXProfileStoredRow
                {
                    Id = new Guid(key.AsSpan(0, 16)), CapturedAt = value.CapturedAt, Value = row.ToString(SaveOptions.DisableFormatting)
                };
                ++index;
            }
        }
    }
}
