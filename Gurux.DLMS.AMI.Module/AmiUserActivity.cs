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

namespace Gurux.DLMS.AMI.Module;

/// <summary>Completed user action, bounded and filtered by the UserActivity module before persistence.</summary>
/// <param name="Target">Normalized resource target.</param>
/// <param name="Action">Shared CrudAction byte value.</param>
/// <param name="CreationTime">Time the completed action was recorded.</param>
/// <param name="Status">HTTP response status code.</param>
/// <param name="Duration">Elapsed milliseconds.</param>
/// <param name="Data">Request text, limited to 1000 characters.</param>
/// <param name="Reply">Response text, limited to 1000 characters, or null for log-reading endpoints.</param>
public sealed record AmiUserActivity(string Target, byte Action, DateTimeOffset CreationTime,
    int Status, int Duration, string Data, string? Reply);
