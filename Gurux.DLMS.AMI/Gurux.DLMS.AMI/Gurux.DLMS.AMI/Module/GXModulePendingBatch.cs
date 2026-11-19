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
/// <summary>Durable update journal describing prepared packages and the state required for recovery.</summary>
internal sealed class GXModulePendingBatch
{
    /// <summary>Identifier shared by the journal and its retained package directory.</summary>
    public Guid Id { get; set; }
    /// <summary>Persisted stage of the pending update lifecycle.</summary>
    public string State { get; set; } = "PendingRestart";
    /// <summary>UTC time at which this update batch was prepared.</summary>
    public DateTimeOffset PreparedAt { get; set; }
    /// <summary>Failure details retained when update application or recovery fails.</summary>
    public string? Error { get; set; }
    /// <summary>Replacement packages that belong to this atomic update batch.</summary>
    public List<GXModulePendingPackage> Packages { get; set; } = [];
    /// <summary>Module identifiers that must be active before the batch can complete.</summary>
    public List<string> RequiredActive { get; set; } = [];
    /// <summary>Serialized settings snapshots indexed by module identifier.</summary>
    public Dictionary<string, string?> PreviousSettings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Projects the journal into update status without exposing archive or rollback metadata.</summary>
    /// <returns>The batch identity, preparation state, error, and proposed module versions.</returns>
    public GXModulePendingUpdateStatus ToResponse() => new()
    {
        Id = Id, State = State, PreparedAt = PreparedAt, Error = Error,
        Modules = [.. Packages.Select(item => new GXModulePendingVersion(item.Id, item.Name, item.CurrentVersion, item.Version))]
    };
}
