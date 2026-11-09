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

namespace Gurux.DLMS.AMI.Client.Shared;

/// <summary>Notification identifiers carried by the cron policy's existing GXTarget messages.</summary>
public static class GXCronNotifications
{
    /// <summary>A task has started; Settings contains GXBackgroundTaskStatus.</summary>
    public const string Started = "cron.start";
    /// <summary>A task reached its terminal state; Settings contains GXBackgroundTaskStatus, including failure/cancellation.</summary>
    public const string Completed = "cron.complete";
    /// <summary>The cron configuration's last-run timestamp changed; Settings contains CronSettings.</summary>
    public const string ScheduleUpdated = "cron.schedule-updated";
}
