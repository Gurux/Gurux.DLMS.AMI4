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

/// <summary>Optional module observer for successfully dispatched SignalR messages.</summary>
public interface IGXAmiSignalRRecorder
{
    /// <summary>Records a successfully dispatched SignalR message and its addressed users.</summary>
    /// <remarks>Dispatch does not imply that clients acknowledged receipt.</remarks>
    /// <param name="method">SignalR client method invoked by the host.</param>
    /// <param name="arguments">Arguments sent to the client method.</param>
    /// <param name="recipients">User identifiers addressed by the notification.</param>
    /// <param name="cancellationToken">Token used to cancel recording.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task RecordSentAsync(string method, object?[] arguments, IReadOnlyList<string> recipients,
        CancellationToken cancellationToken);
}
