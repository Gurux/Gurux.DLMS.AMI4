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

namespace Gurux.DLMS.AMI.Shared.DTOs.Enums;

/// <summary>
/// The lifecycle state of one background execution.
/// </summary>
///
[System.ComponentModel.Description("The lifecycle state of one background execution.")]
public enum BackgroundTaskState
{
    /// <summary>The execution is queued and has not started.</summary>
    [System.ComponentModel.Description("The execution is queued and has not started.")]
    Queued,
    /// <summary>The execution is in progress.</summary>
    [System.ComponentModel.Description("The execution is in progress.")]
    Running,
    /// <summary>The execution completed successfully.</summary>
    [System.ComponentModel.Description("The execution completed successfully.")]
    Succeeded,
    /// <summary>The execution ended with an error.</summary>
    [System.ComponentModel.Description("The execution ended with an error.")]
    Failed,
    /// <summary>The execution was cancelled.</summary>
    [System.ComponentModel.Description("The execution was cancelled.")]
    Cancelled
}