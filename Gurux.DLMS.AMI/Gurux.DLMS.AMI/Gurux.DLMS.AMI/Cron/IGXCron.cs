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
using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Server.Cron
{
    /// <summary>
    /// This interface is used to run Cron service tasks.
    /// </summary>
    public interface IGXBackgroundService
    {
        /// <summary>
        /// Run cron tasks.
        /// </summary>
        Task RunAsync(ClaimsPrincipal user);

        /// <summary>Enqueue a task and return its execution ID without waiting for completion.</summary>
        Task<Guid> StartAsync(string taskId, ClaimsPrincipal user, CancellationToken cancellationToken = default);

        /// <summary>Get an execution snapshot, or null for an unknown or expired run.</summary>
        GXBackgroundTaskStatus? GetTaskStatus(Guid runId);

        /// <summary>
        /// Initialize server.
        /// </summary>
        /// <returns></returns>
        Task InitAsync();
    }

    /// <summary>
    /// Cron service uses this interface to find executed cron tasks.
    /// </summary>
    public interface IGXBackgroundTask
    {
        /// <summary>
        /// Stable identifier of this task.
        /// </summary>
        string TaskId { get; }

        /// <summary>
        /// Cron calls to execute the task.
        /// </summary>
        /// <returns></returns>
        Task RunAsync(CancellationToken cancellationToken = default);
    }

}
