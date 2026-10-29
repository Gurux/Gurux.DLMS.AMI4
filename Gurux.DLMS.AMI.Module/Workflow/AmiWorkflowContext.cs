//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

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
