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
    /// <summary>Identifies the type of source that initiated a recorded entity change.</summary>
    /// <remarks>Describes the source category, not the identifier of a specific user, agent, scheduled task, or module.</remarks>
    [System.ComponentModel.Description("Identifies the type of source that initiated a recorded entity change.")]
    public enum ChangeSource : byte
    {
        /// <summary>The change was initiated by a user.</summary>
        [System.ComponentModel.Description("The change was initiated by a user.")]
        [XmlEnum("0")]
        User,
        /// <summary>The change was initiated by an agent.</summary>
        [System.ComponentModel.Description("The change was initiated by an agent.")]
        [XmlEnum("1")]
        Agent,
        /// <summary>The change was initiated by a scheduled task.</summary>
        [System.ComponentModel.Description("The change was initiated by a scheduled task.")]
        [XmlEnum("2")]
        Scheduler,
        /// <summary>The change was initiated by an internal system process.</summary>
        [System.ComponentModel.Description("The change was initiated by an internal system process.")]
        [XmlEnum("3")]
        System,
        /// <summary>The change was initiated by a module.</summary>
        [System.ComponentModel.Description("The change was initiated by a module.")]
        [XmlEnum("4")]
        Module
    }
}
