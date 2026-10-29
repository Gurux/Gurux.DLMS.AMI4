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

using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Enums;

/// <summary>Execution phases supported by an activity. A workflow selects exactly one phase.</summary>
[Flags]
[DataContract]
public enum TriggerPhase
{
    /// <summary>Runs synchronously before a change and may reject it.</summary>
    [EnumMember]
    Before = 1,

    /// <summary>Runs after a change has been committed successfully.</summary>
    [EnumMember]
    After = 2
}