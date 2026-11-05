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
namespace Gurux.DLMS.AMI.Module;

/// <summary>Serializable UI registration; contains no runtime Type references.</summary>
/// <param name="Area">Host location in which the component is displayed.</param>
/// <param name="Component">Fully qualified component type name within the module.</param>
/// <param name="Policy">Authorization policy required to display the component.</param>
/// <param name="Key">Optional view identifier, unique within the module and area ignoring case.</param>
/// <param name="Title">Optional display title; the host uses the module identifier when omitted.</param>
/// <param name="Icon">Optional icon displayed with the view's tab.</param>
/// <param name="Order">Display order within the host location; lower values appear first.</param>
/// <param name="Display">How the component participates in the host location; defaults to a tab.</param>
public sealed record AmiModuleView(AmiViewArea Area, string Component, string Policy,
    string? Key = null, string? Title = null, string? Icon = null, int Order = 0,
    AmiViewDisplay Display = AmiViewDisplay.Tab);
