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

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Gurux.Service.Orm.Common;

namespace Gurux.DLMS.AMI.Shared.DTOs.History;

/// <summary>Private durable runtime-cache update awaiting delivery after a configuration transaction commits.</summary>
/// <remarks>This outbox is not user-visible change history. Its runtime payload is removed after delivery and must not be returned by history APIs.</remarks>
[Description("Private durable runtime-cache update awaiting delivery after a configuration transaction commits.")]
public sealed class GXChangeHistoryRuntimeEffect : IUnique<Guid>
{
    /// <summary>Gets or sets the deterministic key identifying the runtime resource family and target.</summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>Gets or sets the runtime resource family, Script or Workflow.</summary>
    [IsRequired, StringLength(16)]
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the target resource identifier.</summary>
    public Guid EntityId { get; set; }

    /// <summary>Gets or sets the delivery version used to prevent acknowledgement of a newer pending update.</summary>
    public Guid Version { get; set; }

    /// <summary>Gets or sets whether the runtime resource must be removed.</summary>
    public bool Deleted { get; set; }

    /// <summary>Gets or sets the private serialized runtime DTO needed to rebuild the workflow or script cache.</summary>
    [IsRequired]
    public string Payload { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC time at which this latest pending update was recorded.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
