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

namespace Gurux.DLMS.AMI.Client.Rest;

/// <summary>Server-owned static installation and its durable update outcome.</summary>
public sealed class GXDeploymentResponse
{
    public string Mode { get; set; } = "static";
    public string? RequestId { get; set; }
    public string? Version { get; set; }
    public bool MaintenanceRequired { get; set; }
    public string? Message { get; set; }
    public bool Configured { get; set; }
    public bool AgentAvailable { get; set; }
    public bool IncludesBackend { get; set; }
    public string? CurrentVersion { get; set; }
    public string? ApiVersion { get; set; }
    public string? AvailableVersion { get; set; }
    public string? RestoreVersion { get; set; }
    public GXDeploymentRelease[] Releases { get; set; } = [];
    public bool Busy { get; set; }
    public string State { get; set; } = "Idle";
    public string? Error { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>A catalog version and its server-validated deployment policy.</summary>
public sealed class GXDeploymentRelease
{
    public string Version { get; set; } = "";
    public bool MaintenanceRequired { get; set; }
}

/// <summary>The browser selects a catalog version, never a URL or filesystem path.</summary>
public sealed class GXDeploymentVersion
{
    public string Version { get; set; } = "";
}
