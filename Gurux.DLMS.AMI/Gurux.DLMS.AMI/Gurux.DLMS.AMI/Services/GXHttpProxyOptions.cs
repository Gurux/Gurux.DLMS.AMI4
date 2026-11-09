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

using System.ComponentModel.DataAnnotations;

namespace Gurux.DLMS.AMI.Server.Services;

/// <summary>Server-only resource limits configured under HttpProxyLimits.</summary>
public sealed class GXHttpProxyOptions
{
    /// <summary>Maximum concurrent downloads, including response bodies.</summary>
    [Range(1, 64)] public int MaxConcurrentDownloads { get; set; } = 8;
    /// <summary>Maximum queued downloads; excess work is rejected.</summary>
    [Range(0, 1024)] public int MaxQueuedDownloads { get; set; } = 32;
    /// <summary>Budget reserved for download buffers and response assembly; cached and caller-owned bodies are separate.</summary>
    [Range(1048576L, 2147483647L)] public long MaxBufferedBytes { get; set; } = 256L * 1024 * 1024;
    /// <summary>Per authenticated identity, per server instance.</summary>
    [Range(1, 100000)] public int RequestsPerMinute { get; set; } = 120;
}
