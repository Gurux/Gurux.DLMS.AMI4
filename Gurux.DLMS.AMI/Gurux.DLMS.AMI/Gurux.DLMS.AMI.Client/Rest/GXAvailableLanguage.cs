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

namespace Gurux.DLMS.AMI.Client.Rest;

/// <summary>A language offered by the application's localization catalog.</summary>
public sealed class GXAvailableLanguage
{
    /// <summary>Culture identifier, retaining any regional suffix.</summary>
    public string Id { get; set; } = "";
    /// <summary>English display name.</summary>
    public string EnglishName { get; set; } = "";
    /// <summary>Native display name.</summary>
    public string NativeName { get; set; } = "";
    /// <summary>Available localization package version; null for built-in English.</summary>
    public string? Version { get; set; }
    /// <summary>Whether the language is already present in the installation.</summary>
    public bool Installed { get; set; }
}
