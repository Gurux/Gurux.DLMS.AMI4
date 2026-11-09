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

using System.Runtime.Serialization;
using System.Text.Json;

namespace Gurux.DLMS.AMI.Shared.DTOs.Workflow;

/// <summary>Server-authored event snapshot supplied to workflow matching and execution.</summary>
[DataContract]
public sealed class GXWorkflowEventContext
{
    /// <summary>Stable registered event code.</summary>
    [DataMember] public string EventCode { get; set; } = "";
    /// <summary>Identity of this event; shared by workflows handling the same event.</summary>
    [DataMember] public Guid EventId { get; set; } = Guid.NewGuid();
    /// <summary>Identity shared by all lifecycle events of one operation.</summary>
    [DataMember] public Guid OperationId { get; set; }
    /// <summary>When the event occurred.</summary>
    [DataMember] public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Resource identifier, including string IDs.</summary>
    [DataMember] public string? TargetId { get; set; }
    /// <summary>Device associated with the event.</summary>
    [DataMember] public Guid? DeviceId { get; set; }
    /// <summary>Server-captured memberships before deletion. Never accepted directly from an untrusted agent.</summary>
    [DataMember] public IReadOnlyList<Guid>? DeviceGroupIds { get; set; }
    /// <summary>Attribute associated with a value event.</summary>
    [DataMember] public Guid? AttributeId { get; set; }
    /// <summary>Task associated with the operation.</summary>
    [DataMember] public Guid? TaskId { get; set; }
    /// <summary>Schedule that originated the operation.</summary>
    [DataMember] public Guid? ScheduleId { get; set; }
    /// <summary>Operation outcome, when known.</summary>
    [DataMember] public string? Outcome { get; set; }
    /// <summary>Optional diagnostic code.</summary>
    [DataMember] public string? ErrorCode { get; set; }
    /// <summary>Optional diagnostic message.</summary>
    [DataMember] public string? ErrorMessage { get; set; }
    /// <summary>Snapshot before an attribute change.</summary>
    [DataMember] public JsonElement? OldValue { get; set; }
    /// <summary>Received or changed attribute value.</summary>
    [DataMember] public JsonElement? NewValue { get; set; }

    /// <summary>Server-owned identity linking events caused by the same workflow chain.</summary>
    [DataMember] public Guid ChainId { get; set; }

    /// <summary>Number of workflow-induced event transitions since the originating event.</summary>
    [DataMember] public int Depth { get; set; }
}
