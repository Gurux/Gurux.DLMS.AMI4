﻿//
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
using System.Diagnostics;

namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// Reader settings.
    /// </summary>
    public class ReaderSettings
    {
        /// <summary>
        /// Is Reader active.
        /// </summary>
        public bool Active { get; set; } = true;
        /// <summary>
        /// Amount of the reading threads.
        /// </summary>
        public int Threads { get; set; } = 10;
        /// <summary>
        /// How long reader waits new task before give up. This is given in seconds.
        /// </summary>
        /// <remarks>
        /// This value is needed because server migh restarted and
        /// we don't want to wait new task until session is time-outed.
        /// </remarks>
        public int TaskWaitTime { get; set; } = 100000;
        /// <summary>
        /// How often reader sends alive message to the server in minutes.
        /// If value is zero only start message is sent and no alive messages are sent.
        /// </summary>
        public int AliveTime { get; set; } = 2;
        /// <summary>
        /// Used trace level.
        /// </summary>
        public TraceLevel TraceLevel
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Helpers.GetProperties(this);
        }
    }
}
