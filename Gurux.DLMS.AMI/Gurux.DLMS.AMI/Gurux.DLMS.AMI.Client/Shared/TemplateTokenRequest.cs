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

using System.ComponentModel.DataAnnotations;

namespace Gurux.DLMS.AMI.Client.Shared;

/// <summary>Template preview input. The server supplies the authenticated user.</summary>
public sealed class TemplateTokenRequest
{
    /// <summary>Template text to resolve.</summary>
    [Required(AllowEmptyStrings = true), StringLength(65536)]
    public string Text { get; set; } = string.Empty;
    /// <summary>Optional page scope.</summary>
    [StringLength(128)]
    public string? Page { get; set; }
    /// <summary>Optional draft values for page-specific tokens.</summary>
    public Dictionary<string, string?>? Values { get; set; }
    /// <summary>Optional target category.</summary>
    [StringLength(128)]
    public string? TargetType { get; set; }
    /// <summary>Optional target identifier.</summary>
    [StringLength(256)]
    public string? TargetId { get; set; }
    /// <summary>Optional culture name for formatting.</summary>
    [StringLength(64)]
    public string? Culture { get; set; }
}
