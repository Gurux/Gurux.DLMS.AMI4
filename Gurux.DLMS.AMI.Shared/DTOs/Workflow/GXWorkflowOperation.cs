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

using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace Gurux.DLMS.AMI.Shared.DTOs.Workflow;

/// <summary>Stores the identity, lifecycle state, and execution data of a workflow operation.</summary>
/// <remarks>
/// One record represents one execution, such as a device read, device write, or schedule run.
/// Related executions can be linked using ParentId. Lifecycle changes are managed by IWorkflowOperationRepository.
/// </remarks>
public sealed class GXWorkflowOperation : IUnique<Guid>
{
    /// <summary>Stable identifier of this execution, reused for lifecycle retries.</summary>
    [PrimaryKey, Filter(FilterType.Exact)]
    public Guid Id { get; set; }

    /// <summary>Stable key identifying one logical request, independent of transport retries.</summary>
    /// <remarks>Assigned from Id when omitted. Trusted producers can bind it to an authoritative task claim.</remarks>
    [StringLength(256), Index(true), Filter(FilterType.Exact)]
    public string IdempotencyKey { get; set; } = "";

    /// <summary>Operation code, such as "device.read", "device.write", or "schedule.run".</summary>
    /// <remarks>Identifies the operation kind, rather than an individual lifecycle event.</remarks>
    [StringLength(128), Filter(FilterType.Exact)]
    public string Code { get; set; } = "";

    /// <summary>Identifier of the target resource, supporting both GUID and string identifiers.</summary>
    [StringLength(128), Filter(FilterType.Exact)]
    public string? TargetId { get; set; }

    /// <summary>Identifier of the parent operation, or null for an operation without a parent.</summary>
    /// <remarks>For example, a device read can reference the schedule run that created it.</remarks>
    [Filter(FilterType.Exact), Index(false)]
    public Guid? ParentId { get; set; }

    /// <summary>Identifier of the original initiating user, or null for a machine-initiated operation.</summary>
    /// <remarks>Resolved from trusted server context; does not identify the configured workflow execution account.</remarks>
    [StringLength(450), Filter(FilterType.Exact), Index(false)]
    public string? ActorId { get; set; }

    /// <summary>Current lifecycle state of the operation.</summary>
    [Filter(FilterType.Exact, WorkflowOperationState.Running)]
    public WorkflowOperationState State { get; set; }

    /// <summary>Time when the operation started, including its UTC offset.</summary>
    [TimeStorageUnit(TimeStorageUnit.Milliseconds), Filter(FilterType.GreaterOrEqual)]
    public DateTimeOffset Started { get; set; }

    /// <summary>Time when the operation reached a terminal state, or null while running.</summary>
    [TimeStorageUnit(TimeStorageUnit.Milliseconds), Filter(FilterType.LessOrEqual)]
    public DateTimeOffset? Finished { get; set; }

    /// <summary>Optional diagnostic code associated with the terminal outcome.</summary>
    [StringLength(128), Filter(FilterType.Exact)]
    public string? ErrorCode { get; set; }

    /// <summary>Optional diagnostic message associated with the terminal outcome.</summary>
    [StringLength(2048), Filter(FilterType.Contains)]
    public string? ErrorMessage { get; set; }

    /// <summary>Optional JSON containing operation-specific execution data.</summary>
    /// <remarks>
    /// The structure depends on Code and can include task, agent, device, or schedule identifiers and task claim time.
    /// Values received from clients must be validated against authoritative server state before use.
    /// </remarks>
    public string? Data { get; set; }

    /// <summary>Server-managed acknowledgement of the registered start event.</summary>
    public bool StartedPublished { get; set; }

    /// <summary>Server-managed acknowledgement of the registered terminal outcome event.</summary>
    public bool OutcomePublished { get; set; }

    /// <summary>Server-managed acknowledgement of the registered finish event.</summary>
    public bool FinishedPublished { get; set; }

    /// <summary>Server-owned lease identifying the worker currently processing an internal delivery.</summary>
    public Guid? LeaseId { get; set; }

    /// <summary>Lease expiry permitting recovery after a worker stops renewing its lease.</summary>
    [TimeStorageUnit(TimeStorageUnit.Milliseconds), Index(false)]
    public DateTimeOffset? LeaseExpires { get; set; }
}
