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
/// <summary>Installed module metadata captured before applying a pending replacement.</summary>
internal sealed class GXModulePredecessor
{
    /// <summary>Assembly version of the predecessor generation.</summary>
    public string? Version { get; set; }
    /// <summary>Catalog release version of the predecessor generation.</summary>
    public string? ReleaseVersion { get; set; }
    /// <summary>Predecessor assembly filename.</summary>
    public string? FileName { get; set; }
    /// <summary>Predecessor module directory used during recovery.</summary>
    public string? Path { get; set; }
    /// <summary>Previous enabled state, when the module already existed.</summary>
    public bool? Enabled { get; set; }
    /// <summary>Persisted lifecycle status before the replacement.</summary>
    public ModuleStatus? Status { get; set; }
    /// <summary>Previous display name.</summary>
    public string? Name { get; set; }
    /// <summary>Previous human-readable module description.</summary>
    public string? Description { get; set; }
    /// <summary>Previous icon reference.</summary>
    public string? Icon { get; set; }
    /// <summary>Previous advertised protocol identifiers.</summary>
    public string? Protocols { get; set; }
    /// <summary>Previous module classification.</summary>
    public ModuleType? Type { get; set; }
}
