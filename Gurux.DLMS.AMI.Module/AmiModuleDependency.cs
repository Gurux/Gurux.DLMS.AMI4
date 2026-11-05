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

using Gurux.DLMS.AMI.Module.Enums;

namespace Gurux.DLMS.AMI.Shared.DTOs.Module;

/// <summary>Required identity and inclusive minimum/exclusive maximum compatibility versions.</summary>
/// <param name="Kind">Whether the requirement identifies a host assembly, another module, or an exported service.</param>
/// <param name="Id">Assembly name, module identifier, or fully qualified service type name, according to <paramref name="Kind"/>.</param>
/// <param name="MinimumVersion">Inclusive minimum compatibility version, or <see langword="null"/> for no lower bound.</param>
/// <param name="MaximumVersionExclusive">Exclusive maximum compatibility version, or <see langword="null"/> for no upper bound.</param>
public sealed record AmiModuleDependency(AmiModuleDependencyKind Kind, string Id, string? MinimumVersion = null, string? MaximumVersionExclusive = null);
