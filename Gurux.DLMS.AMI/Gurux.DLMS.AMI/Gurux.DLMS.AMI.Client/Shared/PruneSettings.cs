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

namespace Gurux.DLMS.AMI.Client.Shared
{
    /// <summary>
    /// Prune settings.
    /// </summary>
    public class PruneSettings
    {
        /// <summary>
        /// Prune settings for the system log.
        /// </summary>
        public PruneSystemLog SystemLog { get; set; } = new PruneSystemLog();


        /// <summary>
        /// Prune settings for the user.
        /// </summary>
        public PruneUser User { get; set; } = new PruneUser();

        /// <summary>
        /// Prune settings for the user log.
        /// </summary>
        public PruneUserAction UserActions { get; set; } = new PruneUserAction();

        /// <summary>
        /// Prune settings for the user errors log.
        /// </summary>
        public PruneUserError UserErrors { get; set; } = new PruneUserError();

        /// <summary>
        /// Prune settings for the device log.
        /// </summary>
        public PruneDeviceLog DeviceLogs { get; set; } = new PruneDeviceLog();

        /// <summary>
        /// Prune settings for the device action log.
        /// </summary>
        public PruneDeviceAction DeviceActions { get; set; } = new PruneDeviceAction();

        /// <summary>
        /// Prune settings for the agent log.
        /// </summary>
        public PruneAgentLog AgentLog { get; set; } = new PruneAgentLog();


        /// <summary>
        /// Prune settings for the gateway log.
        /// </summary>
        public PruneGatewayLog GatewayLog { get; set; } = new PruneGatewayLog();


        /// <summary>
        /// Prune settings for the schedule log.
        /// </summary>
        public PruneScheduleLog ScheduleLog { get; set; } = new PruneScheduleLog();


        /// <summary>
        /// Prune settings for the tasks.
        /// </summary>
        public PruneTask Task { get; set; } = new PruneTask();

        /// <summary>
        /// Prune settings for the performance log.
        /// </summary>
        public PrunePerformanceLog PerformanceLog { get; set; } = new PrunePerformanceLog();


        public bool Enabled
        {
            get
            {
                return SystemLog.Interval != 0 ||
                    User.Interval != 0 ||
                    UserActions.Interval != 0 ||
                    AgentLog.Interval != 0 ||
                    GatewayLog.Interval != 0 ||
                    ScheduleLog.Interval != 0 ||
                    Task.Interval != 0 ||
                    PerformanceLog.Interval != 0;
            }
        }
    }
}