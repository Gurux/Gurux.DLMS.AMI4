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

namespace Gurux.DLMS.AMI.Module;

/// <summary>Log data; the host assigns source, identity, log type and enum membership.</summary>
/// <param name="Category">Category identifying the kind of recorded event.</param>
/// <param name="UserId">Optional identifier of the user associated with the event; null represents a system event.</param>
/// <param name="Message">Human-readable event message.</param>
/// <param name="Data">Optional serialized details associated with the log entry.</param>
/// <param name="Level">Trace severity of the recorded event.</param>
/// <param name="Closable">Whether the log entry supports being marked as closed.</param>
/// <param name="Duration">Optional operation duration in milliseconds.</param>
/// <param name="CreationTime">Optional event time; the host supplies the time when omitted.</param>
public sealed record AmiModuleLog(string Category, string? UserId, string Message, string? Data = null,
    int Level = 4, bool Closable = true, int? Duration = null, DateTimeOffset? CreationTime = null);
