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

namespace Gurux.DLMS.AMI.Services.Database;

/// <summary>One completed database command; parameter values are not expanded.</summary>
/// <param name="Sql">Command text as executed, retaining parameter placeholders.</param>
/// <param name="DurationMilliseconds">Elapsed command execution time in milliseconds.</param>
/// <param name="CreationTime">UTC time at which the host created the completion notification.</param>
/// <param name="UserId">Identifier of the current HTTP user, or <see langword="null"/> when unavailable.</param>
/// <param name="Path">Current HTTP request path, or <see langword="null"/> outside an HTTP request.</param>
/// <param name="Provider">Database provider identifier supplied by the command executor.</param>
public sealed record GXAmiSqlCommand(string Sql, double DurationMilliseconds, DateTimeOffset CreationTime,
    string? UserId, string? Path, string Provider);
