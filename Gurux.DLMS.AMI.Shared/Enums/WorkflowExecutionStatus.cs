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

namespace Gurux.DLMS.AMI.Shared.Enums;

/// <summary>The lifecycle state of a single workflow execution.</summary>
[DataContract]
public enum WorkflowExecutionStatus
{
    /// <summary>Waiting to run.</summary>
    [EnumMember] Queued,

    /// <summary>Currently running.</summary>
    [EnumMember] Running,

    /// <summary>Completed successfully.</summary>
    [EnumMember] Succeeded,

    /// <summary>Did not run because its condition did not match.</summary>
    [EnumMember] Skipped,

    /// <summary>Failed while evaluating or executing.</summary>
    [EnumMember] Failed,

    /// <summary>A Before handler rejected the proposed change.</summary>
    [EnumMember] Rejected,

    /// <summary>Stopped by cancellation.</summary>
    [EnumMember] Cancelled,

    /// <summary>Exceeded its execution time limit.</summary>
    [EnumMember] TimedOut
}