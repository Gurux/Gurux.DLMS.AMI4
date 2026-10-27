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

using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs;

/// <summary>An immutable snapshot of a background execution.</summary>
[System.ComponentModel.Description("An immutable snapshot of a background execution.")]
public sealed record GXBackgroundTaskStatus
{
    /// <summary>Gets the unique identifier of this execution.</summary>
    [System.ComponentModel.Description("Gets the unique identifier of this execution.")]
    public Guid RunId { get; init; }
    /// <summary>Gets the identifier of the background task being executed.</summary>
    [System.ComponentModel.Description("Gets the identifier of the background task being executed.")]
    public string TaskId { get; init; } = string.Empty;
    /// <summary>Gets the current execution state.</summary>
    [System.ComponentModel.Description("Gets the current execution state.")]
    public BackgroundTaskState State { get; init; }
    /// <summary>Gets the time the execution was queued.</summary>
    [System.ComponentModel.Description("Gets the time the execution was queued.")]
    public DateTimeOffset QueuedAt { get; init; }
    /// <summary>Gets the time execution started, or null if it has not started.</summary>
    [System.ComponentModel.Description("Gets the time execution started, or null if it has not started.")]
    public DateTimeOffset? StartedAt { get; init; }
    /// <summary>Gets the time execution finished, or null if it has not finished.</summary>
    [System.ComponentModel.Description("Gets the time execution finished, or null if it has not finished.")]
    public DateTimeOffset? FinishedAt { get; init; }
    /// <summary>Gets the execution error description, if available.</summary>
    [System.ComponentModel.Description("Gets the execution error description, if available.")]
    public string? Error { get; init; }

    /// <summary>Used for server-side authorization; excluded from the HTTP response.</summary>
    [System.ComponentModel.Description("Used for server-side authorization; excluded from the HTTP response.")]
    [JsonIgnore]
    public string OwnerId { get; init; } = string.Empty;
}
