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

using System.Net;

namespace Gurux.DLMS.AMI.Agent.Worker;

/// <summary>Shares rejection of a PAT across API requests and SignalR negotiation.</summary>
/// <param name="rejected">Notifies the host when the server first rejects the access token.</param>
internal sealed class GXAuthenticationState(Action rejected)
{
    /// <summary>Atomically records whether the server rejected the access token.</summary>
    private int _failed;
    /// <summary>Gets whether the server has rejected the shared access token.</summary>
    internal bool IsRejected => Volatile.Read(ref _failed) != 0;

    /// <summary>Permanently marks authentication as rejected and notifies the host once.</summary>
    internal void Reject()
    {
        if (Interlocked.Exchange(ref _failed, 1) == 0)
        {
            rejected();
        }
    }

    /// <summary>Stops HTTP work after the server has rejected the access token.</summary>
    internal void ThrowIfRejected()
    {
        if (IsRejected)
        {
            throw new HttpRequestException("AMI authentication failed (HTTP 401).", null, HttpStatusCode.Unauthorized);
        }
    }
}
