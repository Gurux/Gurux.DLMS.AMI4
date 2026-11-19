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
using System.Text.Json;
using System.Text.RegularExpressions;
using Gurux.DLMS.AMI.Client.Rest;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Enums;

namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>An inspected replacement ZIP to retain for the next startup.</summary>
/// <param name="Id">Module identity.</param>
/// <param name="Version">Prepared catalog version.</param>
/// <param name="Zip">Local ZIP source.</param>
/// <param name="CurrentVersion">Currently installed version.</param>
/// <param name="Name">Display name.</param>
public sealed record GXModuleUpdatePackage(string Id, string Version, string Zip, string? CurrentVersion = null, string? Name = null);
