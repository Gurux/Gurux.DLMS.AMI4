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

namespace Gurux.DLMS.AMI.Shared.Enums
{
    /// <summary>
    /// Enumerated changed items.
    /// </summary>
    [Flags]
    public enum TargetType : UInt64
    {
        /// <summary>
        /// Nothing is changed.
        /// </summary>
        [XmlEnum("0")]
        None = 0x0,
        /// <summary>
        /// Devices are changed.
        /// </summary>
        [XmlEnum("1")]
        Device = 1,
        /// <summary>
        /// Objects are changed.
        /// </summary>
        [XmlEnum("2")]
        Object = 2,
        /// <summary>
        /// Attributes are changed.
        /// </summary>
        [XmlEnum("4")]
        Attribute = 4,
        /// <summary>
        /// Values are changed.
        /// </summary>
        [XmlEnum("8")]
        Value = 8,
        /// <summary>
        /// Tasks are changed.
        /// </summary>
        [XmlEnum("16")]
        Task = 0x10,
        /// <summary>
        /// Device errors are changed.
        /// </summary>
        [XmlEnum("32")]
        DeviceError = 0x20,
        /// <summary>
        /// System errors are changed.
        /// </summary>
        [XmlEnum("64")]
        SystemLog = 0x40,
        /// <summary>
        /// Schedules are changed.
        /// </summary>
        [XmlEnum("128")]
        Schedule = 0x80,
        /// <summary>
        /// Agent are changed.
        /// </summary>
        [XmlEnum("256")]
        Agent = 0x100,
        /// <summary>
        /// Device templates are changed.
        /// </summary>
        [XmlEnum("512")]
        DeviceTemplate = 0x200,
        /// <summary>
        /// Object templates are changed.
        /// </summary>
        [XmlEnum("1024")]
        ObjectTemplate = 0x400,
        /// <summary>
        /// Attribute templates are changed.
        /// </summary>
        [XmlEnum("2048")]
        AttributeTemplate = 0x800,
        /// <summary>
        /// Device Log is changed.
        /// </summary>
        [XmlEnum("4096")]
        DeviceLog = 0x1000,
        /// <summary>
        /// User group is changed.
        /// </summary>
        [XmlEnum("8192")]
        UserGroup = 0x2000,
        /// <summary>
        /// User is changed.
        /// </summary>
        [XmlEnum("16384")]
        User = 0x4000,
        /// <summary>
        /// User Log is changed.
        /// </summary>
        [XmlEnum("32768")]
        UserAction = 0x8000,
        /// <summary>
        /// Configuration is changed.
        /// </summary>
        [XmlEnum("65536")]
        Configuration = 0x10000,
        /// <summary>
        /// Token is changed.
        /// </summary>
        [XmlEnum("131072")]
        Token = 0x20000,
        /// <summary>
        /// Module is changed.
        /// </summary>
        [XmlEnum("262144")]
        Module = 0x40000,
        /// <summary>
        /// WorkFlow is changed.
        /// </summary>
        [XmlEnum("524288")]
        WorkFlow = 0x80000,
        /// <summary>
        /// Device action is changed.
        /// </summary>
        [XmlEnum("1048576")]
        DeviceAction = 0x100000,
        /// <summary>
        /// Script is changed.
        /// </summary>
        [XmlEnum("2097152")]
        Script = 0x200000,
        /// <summary>
        /// Script log is changed.
        /// </summary>
        [XmlEnum("4194304")]
        ScriptLog = 0x400000,
        /// <summary>
        /// Module log is changed.
        /// </summary>
        [XmlEnum("8388608")]
        ModuleLog = 0x800000,
        /// <summary>
        /// Agent error is changed.
        /// </summary>
        [XmlEnum("16777216")]
        AgentError = 0x1000000,
        /// <summary>
        /// Schedule error is changed.
        /// </summary>
        [XmlEnum("33554432")]
        ScheduleError = 0x2000000,
        /// <summary>
        /// Schedule error is changed.
        /// </summary>
        [XmlEnum("67108864")]
        WorkflowLog = 0x4000000,
        /// <summary>
        /// Block is changed.
        /// </summary>
        [XmlEnum("134217728")]
        Block = 0x8000000,
        /// <summary>
        /// User error is changed.
        /// </summary>
        [XmlEnum("268435456")]
        UserError = 0x10000000,
        /// <summary>
        /// Trigger is changed.
        /// </summary>
        [XmlEnum("536870912")]
        Trigger = 0x20000000,
        /// <summary>
        /// Trigger group is changed.
        /// </summary>
        [XmlEnum("1073741824")]
        TriggerGroup = 0x40000000,
        /// <summary>
        /// User group is changed.
        /// </summary>
        [XmlEnum("2147483648")]
        DeviceGroup = 0x80000000,
        /// <summary>
        /// User group is changed.
        /// </summary>
        [XmlEnum("4294967296")]
        ScheduleGroup = 0x100000000,
        /// <summary>
        /// Agent group is changed.
        /// </summary>
        [XmlEnum("8589934592")]
        AgentGroup = 0x200000000,
        /// <summary>
        /// Script group is changed.
        /// </summary>
        [XmlEnum("17179869184")]
        ScriptGroup = 0x400000000,
        /// <summary>
        /// Device template group is changed.
        /// </summary>
        [XmlEnum("34359738368")]
        DeviceTemplateGroup = 0x800000000,
        /// <summary>
        /// Component view is changed.
        /// </summary>
        [XmlEnum("68719476736")]
        ComponentView = 0x1000000000,
        /// <summary>
        /// Component view group is changed.
        /// </summary>
        [XmlEnum("137438953472")]
        ComponentViewGroup = 0x2000000000,
        /// <summary>
        /// Device trace is changed.
        /// </summary>
        [XmlEnum("274877906944")]
        DeviceTrace = 0x4000000000,
    }
}
