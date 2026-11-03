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

namespace Gurux.DLMS.AMI.Agent.Shared;

/// <summary>Changes the host must handle. Multiple changes may be delivered together.</summary>
[Flags]
public enum GXAgentHostChange
{
    /// <summary>No pending change.</summary>
    None = 0,
    /// <summary>A new worker version is ready to load.</summary>
    VersionAvailable = 1,
    /// <summary>The current configuration must be persisted.</summary>
    SettingsChanged = 2
}

/// <summary>Notifies the host without exposing thread synchronization primitives.</summary>
public interface IGXAgentHostNotifications
{
    /// <summary>Requests a worker version reload.</summary>
    void NotifyVersionAvailable();
    /// <summary>Requests persistence of updated settings.</summary>
    void NotifySettingsChanged();
    /// <summary>Waits asynchronously for pending changes. The host is the single consumer.</summary>
    /// <param name="cancellationToken">Cancels the wait without consuming pending changes.</param>
    /// <returns>Pending changes, with repeated changes coalesced.</returns>
    ValueTask<GXAgentHostChange> WaitForChangeAsync(CancellationToken cancellationToken = default);
}
