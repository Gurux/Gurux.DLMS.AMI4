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
    /// <summary>Identifies the current lifecycle state of a workflow operation.</summary>
    /// <remarks>An operation starts as Running and transitions to one terminal state: Completed, Failed, or Cancelled.</remarks>
    public enum WorkflowOperationState : byte
    {
        /// <summary>The operation has started and has no recorded terminal outcome.</summary>
        [XmlEnum("0")]
        Running = 0,
        /// <summary>The operation finished successfully.</summary>
        [XmlEnum("1")]
        Completed = 1,
        /// <summary>The operation finished with a failure.</summary>
        [XmlEnum("2")]
        Failed = 2,
        /// <summary>The operation ended because it was cancelled.</summary>
        [XmlEnum("3")]
        Cancelled = 3
    }
}

