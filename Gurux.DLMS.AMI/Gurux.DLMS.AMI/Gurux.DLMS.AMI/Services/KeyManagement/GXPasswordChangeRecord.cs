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

using System.ComponentModel.DataAnnotations;
using Gurux.Service.Orm.Common;

namespace Gurux.DLMS.AMI.Server.Services.KeyManagement;

/// <summary>Durable password-change intent. Passwords remain in encrypted key rows.</summary>
public sealed class GXPasswordChangeRecord : IUnique<Guid>
{
    /// <summary>Operation or key-management identity.</summary>
    [Key] public Guid Id { get; set; }
    /// <summary>Meter identity.</summary>
    [Index(false)] public Guid DeviceId { get; set; }
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
    /// <summary>Requesting user identity.</summary>
    [StringLength(450)] public string? RequestedBy { get; set; }
    /// <summary>Durable operation phase.</summary>
    [StringLength(32)] public string? State { get; set; }
    /// <summary>Selected Association Logical Name for the LLS secret.</summary>
    [StringLength(64)] public string? AssociationLogicalName { get; set; }
    /// <summary>Fingerprint of the connection profile when the operation was requested.</summary>
    [StringLength(64)] public string? ProfileHash { get; set; }
    /// <summary>Request creation time.</summary>
    public DateTimeOffset Created { get; set; }
    /// <summary>Time a fresh association was acknowledged.</summary>
    public DateTimeOffset? Verified { get; set; }
}

/// <summary>A database-enforced single pending operation per device.</summary>
public sealed class GXPasswordChangeLock : IUnique<Guid>
{
    /// <summary>Operation or key-management identity.</summary>
    [Key] public Guid Id { get; set; }
    /// <summary>Pending operation owning the meter lock.</summary>
    public Guid OperationId { get; set; }
}
