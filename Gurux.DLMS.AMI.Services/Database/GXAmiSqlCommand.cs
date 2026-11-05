// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

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
