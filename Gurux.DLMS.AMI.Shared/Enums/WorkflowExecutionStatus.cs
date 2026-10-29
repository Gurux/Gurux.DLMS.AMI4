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

/// <summary>The lifecycle state of a single workflow execution.</summary>
[DataContract]
public enum WorkflowExecutionStatus
{
    /// <summary>Waiting to run.</summary>
    [EnumMember] Queued,

    /// <summary>Currently running.</summary>
    [EnumMember] Running,

    /// <summary>Completed successfully.</summary>
    [EnumMember] Succeeded,

    /// <summary>Did not run because its condition did not match.</summary>
    [EnumMember] Skipped,

    /// <summary>Failed while evaluating or executing.</summary>
    [EnumMember] Failed,

    /// <summary>A Before handler rejected the proposed change.</summary>
    [EnumMember] Rejected,

    /// <summary>Stopped by cancellation.</summary>
    [EnumMember] Cancelled,

    /// <summary>Exceeded its execution time limit.</summary>
    [EnumMember] TimedOut
}