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
    /// <summary>A downloaded version is ready for a host reload.</summary>
    RestartPending,
    /// <summary>Shutdown is in progress.</summary>
    Stopping,
    /// <summary>Shutdown completed; create a new instance to restart.</summary>
    Stopped,
    /// <summary>Startup or service reconfiguration failed.</summary>
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
