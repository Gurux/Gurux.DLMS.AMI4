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
