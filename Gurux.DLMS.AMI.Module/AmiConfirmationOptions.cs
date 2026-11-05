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

namespace Gurux.DLMS.AMI.Module;

/// <summary>Presentation and choices for a host confirmation dialog.</summary>
public sealed record AmiConfirmationOptions
{
    /// <summary>Action button text. The host uses its localized OK text when omitted.</summary>
    public string? ConfirmText { get; init; }

    /// <summary>Whether the user can choose permanent deletion instead of disabling an item.</summary>
    public bool AllowDelete { get; init; }

    /// <summary>Names of the selected items, displayed as encoded text.</summary>
    public IReadOnlyList<string?>? Items { get; init; }
}
