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

using System.Xml.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Enums
{
    /// <summary>
    /// Agent state.
    /// </summary>
    public enum AgentStatus : byte
    {
        /// <summary>
        /// Agent is disconnected.
        /// </summary>
        [XmlEnum("0")]
        Offline = 0,
        /// <summary>
        /// Agent is connected.
        /// </summary>
        [XmlEnum("1")]
        Connected = 1,
        /// <summary>
        /// Agent is idle.
        /// </summary>
        [XmlEnum("2")]
        Idle = 2,
        /// <summary>
        /// Agent is processing the task.
        /// </summary>
        [XmlEnum("3")]
        Process = 3,
        /// <summary>
        /// Agent is on error state.
        /// </summary>
        [XmlEnum("4")]
        Error = 4,
        /// <summary>
        /// The agent is downloading the new version.
        /// </summary>
        [XmlEnum("5")]
        Downloading = 5,
        /// <summary>
        /// The agent updates to the new version.
        /// </summary>
        [XmlEnum("6")]
        Updating = 6,
        /// <summary>
        /// The agent is restarting after the update.
        /// </summary>
        [XmlEnum("7")]
        Restarting = 7,
        /// <summary>
        /// Serial ports are added or removed from the agent.
        /// </summary>
        [XmlEnum("8")]
        SerialPortChange = 8,
    }
}
