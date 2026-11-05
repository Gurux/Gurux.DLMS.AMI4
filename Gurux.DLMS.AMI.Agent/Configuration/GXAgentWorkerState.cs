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

namespace Gurux.DLMS.AMI.Agent.Shared;

/// <summary>Worker lifecycle state, independent of server connectivity.</summary>
public enum GXAgentWorkerState
{
    /// <summary>Not initialized.</summary>
    Created,
    /// <summary>Ready for registration or startup.</summary>
    Initialized,
    /// <summary>Startup is in progress.</summary>
    Starting,
    /// <summary>Worker services are running, possibly while disconnected.</summary>
    Running,
    /// <summary>A complete application update is staged and awaits host handoff to the external updater.</summary>
    RestartPending,
    /// <summary>Shutdown is in progress.</summary>
    Stopping,
    /// <summary>Shutdown completed; create a new instance to restart.</summary>
    Stopped,
    /// <summary>Startup, service reconfiguration or shutdown failed.</summary>
    Faulted,
    /// <summary>Worker-owned resources were released.</summary>
    Disposed
}

/// <summary>Server notification connection state; a disconnection does not mean the worker has stopped.</summary>
public enum GXAgentConnectionState
{
    /// <summary>No active server notification connection.</summary>
    Disconnected,
    /// <summary>Initial connection or registration is in progress.</summary>
    Connecting,
    /// <summary>The server notification connection is registered.</summary>
    Connected,
    /// <summary>Restoring a lost server notification connection.</summary>
    Reconnecting
}
