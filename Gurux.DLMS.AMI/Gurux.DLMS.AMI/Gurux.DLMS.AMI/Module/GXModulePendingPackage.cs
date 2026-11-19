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
/// <summary>Persisted identity, archive checksum, and rollback metadata for one pending module replacement.</summary>
internal sealed class GXModulePendingPackage
{
    /// <summary>Stable identifier of the module being replaced.</summary>
    public string Id { get; set; } = "";
    /// <summary>Display name retained for update status responses.</summary>
    public string Name { get; set; } = "";
    /// <summary>Catalog version prepared for installation.</summary>
    public string Version { get; set; } = "";
    /// <summary>Version installed when the replacement was prepared.</summary>
    public string? CurrentVersion { get; set; }
    /// <summary>Archive path relative to the pending batch directory.</summary>
    public string Archive { get; set; } = "";
    /// <summary>Expected SHA-256 digest of the retained archive.</summary>
    public string Sha256 { get; set; } = "";
    /// <summary>Installed module metadata retained for recovery or rollback.</summary>
    public GXModulePredecessor? Previous { get; set; }
}
