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

namespace Gurux.DLMS.AMI.Client.Shared;

public sealed class GXProfileReadIntent
{
    public const string Format = "profile-history-v1";
    public string Kind { get; set; } = Format;
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public bool Force { get; set; }

    /// <summary>Identifies input that must survive a task retry, unlike a previous result.</summary>
    public static bool IsSerializedIntent(string? data)
    {
        if (string.IsNullOrWhiteSpace(data)) {
            return false;
        }
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(data);
            if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object
                || !document.RootElement.TryGetProperty(nameof(Kind), out var kind)
                || kind.ValueKind != System.Text.Json.JsonValueKind.String || kind.GetString() != Format)
                {
                return false;
            }
            var intent = System.Text.Json.JsonSerializer.Deserialize<GXProfileReadIntent>(data);
            return intent != null && intent.Start < intent.End && intent.End - intent.Start <= TimeSpan.FromDays(366);
        }
        catch (System.Text.Json.JsonException) { return false; }
    }
}

