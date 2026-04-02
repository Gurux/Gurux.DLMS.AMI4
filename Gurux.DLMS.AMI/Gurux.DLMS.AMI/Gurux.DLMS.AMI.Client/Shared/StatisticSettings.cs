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
    /// Statistic settings.
    /// </summary>
    public class StatisticSettings
    {
        /// <summary>
        /// Are user actions saved.
        /// </summary>
        public bool UserActions
        {
            get;
            set;
        } = true;

        /// <summary>
        /// Log REST operations if it takes at least the given time in milliseconds.
        /// </summary>
        public int RestTrigger
        {
            get;
            set;
        } = 5000;

        /// <summary>
        /// How often performance values are saved to database.
        /// </summary>
        /// <remarks>
        /// At default values are kept on the memory and 
        /// are not saved to the database.
        /// </remarks>
        public int PerformanceInterval
        {
            get;
            set;
        } = 0;

        /// <summary>
        /// Are device actions saved.
        /// </summary>
        public bool DeviceActions
        {
            get;
            set;
        } = true;

        /// <summary>
        /// Are Schedule actions saved.
        /// </summary>
        public bool ScheduleActions
        {
            get;
            set;
        } = true;

        /// <summary>
        /// Are Agent actions saved.
        /// </summary>
        public bool AgentActions
        {
            get;
            set;
        } = true;

    }
}