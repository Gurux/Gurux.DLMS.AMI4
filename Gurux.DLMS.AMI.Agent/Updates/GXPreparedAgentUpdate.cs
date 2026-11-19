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

using Microsoft.Extensions.Configuration;

namespace Gurux.DLMS.AMI.Agent;

/// <summary>Describes a verified application package staged for external installation.</summary>
/// <param name="Version">Requested release version verified against the signed manifest and application metadata.</param>
/// <param name="PackagePath">Path of the retained ZIP package outside the live installation directory.</param>
/// <param name="Sha256">Lowercase hexadecimal SHA-256 digest verified against the release signature.</param>
internal sealed record GXPreparedAgentUpdate(string Version, string PackagePath, string Sha256);
