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

using System.Security.Claims;
using System.Text.Json;
using Gurux.DLMS.AMI.Module.Enums;

namespace Gurux.DLMS.AMI.Module.Workflow;

/// <summary>Invocation context supplied by the host to a module workflow handler.</summary>
public sealed class AmiWorkflowContext
{
    /// <summary>Identifier of this invocation, shared with its history entries.</summary>
    public required Guid ExecutionId { get; init; }
    /// <summary>Originating event identifier; one event may invoke multiple workflows.</summary>
    public required Guid EventId { get; init; }
    /// <summary>Workflow being executed.</summary>
    public required Guid WorkflowId { get; init; }
    /// <summary>Stable trigger identifier.</summary>
    public required string TriggerCode { get; init; }
    /// <summary>Activity published by the trigger.</summary>
    public required string Activity { get; init; }
    /// <summary>The single phase being handled. Before executes inline; After follows commit.</summary>
    public AmiWorkflowPhase Phase { get; init; } = AmiWorkflowPhase.After;
    /// <summary>Time the originating event occurred.</summary>
    public required DateTimeOffset OccurredAt { get; init; }
    /// <summary>Principal under whose permissions the handler executes.</summary>
    public required ClaimsPrincipal User { get; init; }
    /// <summary>Account that caused the originating event, which may differ from the execution principal.</summary>
    public string? ActorId { get; init; }
    /// <summary>Optional resource category.</summary>
    public string? TargetType { get; init; }
    /// <summary>Optional resource identifier.</summary>
    public string? TargetId { get; init; }
    /// <summary>Snapshot before the change; null when not applicable.</summary>
    public JsonElement? OldValue { get; init; }
    /// <summary>Proposed value for Before, or committed value for After.</summary>
    public JsonElement? NewValue { get; init; }
}
