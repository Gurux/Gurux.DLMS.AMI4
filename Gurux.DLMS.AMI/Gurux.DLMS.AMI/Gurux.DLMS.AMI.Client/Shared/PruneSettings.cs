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
        public PruneSystemLog SystemLog
        {
            get;
            set;
        } = new PruneSystemLog();


        /// <summary>
        /// Prune settings for the user.
        /// </summary>
        public PruneUser User
        {
            get;
            set;
        } = new PruneUser();

        /// <summary>
        /// Prune settings for the user log.
        /// </summary>
        public PruneUserAction UserActions
        {
            get;
            set;
        } = new PruneUserAction();

        /// <summary>
        /// Prune settings for the user errors log.
        /// </summary>
        public PruneUserError UserErrors
        {
            get;
            set;
        } = new PruneUserError();

        /// <summary>
        /// Prune settings for the device log.
        /// </summary>
        public PruneDeviceLog DeviceLogs
        {
            get;
            set;
        } = new PruneDeviceLog();

        /// <summary>
        /// Prune settings for the device action log.
        /// </summary>
        public PruneDeviceAction DeviceActions
        {
            get;
            set;
        } = new PruneDeviceAction();

        /// <summary>
        /// Prune settings for the agent log.
        /// </summary>
        public PruneAgentLog AgentLog
        {
            get;
            set;
        } = new PruneAgentLog();


        /// <summary>
        /// Prune settings for the gateway log.
        /// </summary>
        public PruneGatewayLog GatewayLog
        {
            get;
            set;
        } = new PruneGatewayLog();


        /// <summary>
        /// Prune settings for the schedule log.
        /// </summary>
        public PruneScheduleLog ScheduleLog
        {
            get;
            set;
        } = new PruneScheduleLog();


        /// <summary>
        /// Prune settings for the tasks.
        /// </summary>
        public PruneTask Task
        {
            get;
            set;
        } = new PruneTask();

        /// <summary>
        /// Prune settings for the performance log.
        /// </summary>
        public PrunePerformanceLog PerformanceLog
        {
            get;
            set;
        } = new PrunePerformanceLog();


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