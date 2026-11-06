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

using Microsoft.AspNetCore.Http;

namespace Gurux.DLMS.AMI.Module;

/// <summary>Host adapter to the existing user-action log store; does not capture request or response bodies.</summary>
public interface IGXAmiUserActivityStore
{
    /// <summary>Persists a completed action using the original authenticated request identity.</summary>
    /// <param name="context">Original request and host services.</param>
    /// <param name="entry">Bounded, filtered action details.</param>
    /// <param name="cancellationToken">Cancels persistence.</param>
    Task WriteAsync(HttpContext context, AmiUserActivity entry, CancellationToken cancellationToken);
}
