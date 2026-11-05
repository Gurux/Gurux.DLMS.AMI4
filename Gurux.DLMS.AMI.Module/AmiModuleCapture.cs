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

/// <summary>Defaults for a module's protected capture view.</summary>
/// <param name="Policy">Access policy for the module's capture view.</param>
/// <param name="TargetType">Target type identifying the module's capture view.</param>
/// <param name="Icon">Icon identifier displayed for the module's log menu entry.</param>
/// <param name="DefaultCategory">Optional log category to create when capture defaults are initialized.</param>
public sealed record AmiModuleCapture(string Policy, string TargetType, string Icon, string? DefaultCategory = null);
