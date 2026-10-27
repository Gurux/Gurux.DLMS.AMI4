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

namespace Gurux.DLMS.AMI.Shared.DTOs.Enums;

/// <summary>
/// The lifecycle state of one background execution.
/// </summary>
///
[System.ComponentModel.Description("The lifecycle state of one background execution.")]
public enum BackgroundTaskState
{
    /// <summary>The execution is queued and has not started.</summary>
    [System.ComponentModel.Description("The execution is queued and has not started.")]
    Queued,
    /// <summary>The execution is in progress.</summary>
    [System.ComponentModel.Description("The execution is in progress.")]
    Running,
    /// <summary>The execution completed successfully.</summary>
    [System.ComponentModel.Description("The execution completed successfully.")]
    Succeeded,
    /// <summary>The execution ended with an error.</summary>
    [System.ComponentModel.Description("The execution ended with an error.")]
    Failed,
    /// <summary>The execution was cancelled.</summary>
    [System.ComponentModel.Description("The execution was cancelled.")]
    Cancelled
}