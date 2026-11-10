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

namespace Gurux.DLMS.AMI.Client.Shared.KeyManagement;

/// <summary>A new password or a request to restore the source of an earlier verified change.</summary>
public sealed class GXPasswordChangeRequest
{
    /// <summary>Device-specific key-management identity.</summary>
    public Guid ManagementId { get; set; }
    /// <summary>Meter identity.</summary>
    public Guid DeviceId { get; set; }
    /// <summary>Selected Association Logical Name for the LLS secret.</summary>
    public string AssociationLogicalName { get; set; } = "";
    /// <summary>New credential material; absent on restore requests.</summary>
    public byte[]? NewPassword { get; set; }
    public Guid? RestoreChangeId { get; set; }
}

/// <summary>Metadata-only status; no material is returned to the browser.</summary>
public sealed class GXPasswordChangeInfo
{
    /// <summary>Operation or key-management identity.</summary>
    public Guid Id { get; set; }
    /// <summary>Meter identity.</summary>
    public Guid DeviceId { get; set; }
    /// <summary>Device-specific key-management identity.</summary>
    public Guid ManagementId { get; set; }
    /// <summary>Durable agent task identity.</summary>
    public Guid TaskId { get; set; }
    /// <summary>Retained source key identity.</summary>
    public Guid SourceKeyId { get; set; }
    /// <summary>Fresh pending key identity.</summary>
    public Guid TargetKeyId { get; set; }
    /// <summary>Earlier verified change whose source material is restored.</summary>
    public Guid? RestoredFromId { get; set; }
    /// <summary>Durable operation phase.</summary>
    public string State { get; set; } = "";
    /// <summary>Request creation time.</summary>
    public DateTimeOffset Created { get; set; }
    /// <summary>Time a fresh association was acknowledged.</summary>
    public DateTimeOffset? Verified { get; set; }
    /// <summary>Whether the failed task may be retried.</summary>
    public bool CanRetry { get; set; }
    /// <summary>Whether the request may be cancelled before writing.</summary>
    public bool CanCancel { get; set; }
}

/// <summary>Identifies the persisted task claim; never contains material.</summary>
public sealed class GXPasswordChangeClaim
{
    /// <summary>Durable agent task identity.</summary>
    public Guid TaskId { get; set; }
    /// <summary>Agent holding the persisted claim.</summary>
    public Guid AgentId { get; set; }
    /// <summary>Persisted task claim start time.</summary>
    public DateTimeOffset Start { get; set; }
}
/// <summary>A selectable association from the meter template.</summary>
public sealed record GXPasswordAssociation(string LogicalName, string? Name);
