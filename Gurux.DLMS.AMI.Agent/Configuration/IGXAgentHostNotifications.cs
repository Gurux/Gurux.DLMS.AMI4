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

/// <summary>Changes the host must handle. Multiple changes may be delivered together.</summary>
[Flags]
public enum GXAgentHostChange
{
    /// <summary>No pending change.</summary>
    None = 0,
    /// <summary>A complete application release is staged for external installation.</summary>
    VersionAvailable = 1,
    /// <summary>The current configuration must be persisted.</summary>
    SettingsChanged = 2,
    /// <summary>The server rejected the PAT; stop the runtime and request new credentials.</summary>
    AuthenticationRequired = 4
}

/// <summary>Notifies the host without exposing thread synchronization primitives.</summary>
public interface IGXAgentHostNotifications
{
    /// <summary>Requests handoff of a staged application release to the external updater.</summary>
    /// <exception cref="ObjectDisposedException">The notification source has been disposed.</exception>
    void NotifyVersionAvailable();
    /// <summary>Requests persistence of updated settings.</summary>
    /// <exception cref="ObjectDisposedException">The notification source has been disposed.</exception>
    void NotifySettingsChanged();
    /// <summary>Reports HTTP 401 without exposing credentials.</summary>
    void NotifyAuthenticationRequired();
    /// <summary>Waits asynchronously for pending changes. The host is the single consumer.</summary>
    /// <param name="cancellationToken">Cancels the wait without consuming pending changes.</param>
    /// <returns>Pending changes, with repeated changes coalesced.</returns>
    /// <remarks>The returned flags consume all changes accumulated for this single consumer.</remarks>
    /// <exception cref="OperationCanceledException">The wait is canceled before pending changes are consumed.</exception>
    /// <exception cref="ObjectDisposedException">The notification source is disposed before changes can be read.</exception>
    ValueTask<GXAgentHostChange> WaitForChangeAsync(CancellationToken cancellationToken = default);
}
