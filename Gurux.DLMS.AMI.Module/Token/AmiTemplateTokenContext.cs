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
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Module;

/// <summary>Identifies the user, culture and optional target for template token resolution.</summary>
public sealed class AmiTemplateTokenContext
{
    /// <summary>User whose permissions must be enforced when resolving token values.</summary>
    public required ClaimsPrincipal User { get; init; }

    /// <summary>Culture used for localized text, numbers and dates.</summary>
    public CultureInfo Culture { get; init; } = CultureInfo.InvariantCulture;

    /// <summary>Optional page scope, for example maintenance. This does not grant authorization.</summary>
    public string? Page { get; init; }
    /// <summary>Optional page values supplied for a draft or a known server-side snapshot.</summary>
    public IReadOnlyDictionary<string, string?>? Values { get; init; }

    /// <summary>Optional target category, for example Device.</summary>
    public string? TargetType { get; init; }

    /// <summary>Optional identifier of the target associated with the template.</summary>
    public string? TargetId { get; init; }
}
