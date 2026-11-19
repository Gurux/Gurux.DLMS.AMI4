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

using System.Threading.Channels;

namespace Gurux.DLMS.AMI.Agent.Worker.AutoConnect;

/// <summary>Routes task availability to independently owned device connection registrations.</summary>
internal sealed class GXTaskNotification : IGXTaskNotification
{
    /// <summary>Current task notification registrations indexed by device identifier.</summary>
    private readonly Dictionary<Guid, GXTaskRegistration> _registrations = [];

    /// <summary>Creates an independently owned task notification registration for the device.</summary>
    /// <param name="id">Identity of the agent, device or registered work being processed.</param>
    /// <returns>A registration owning its own task wake-up and close signals.</returns>
    public GXTaskRegistration Register(Guid id)
    {
        lock (_registrations)
        {
            if (_registrations.Remove(id, out var previous))
                {
                previous.Close();
            }
            var registration = new GXTaskRegistration(this, id);
            _registrations.Add(id, registration);
            return registration;
        }
    }

    /// <summary>Signals task availability for the selected registration.</summary>
    /// <param name="id">Identity of the agent, device or registered work being processed.</param>
    public void Set(Guid id)
    {
        lock (_registrations)
            if (_registrations.TryGetValue(id, out var registration))
                {
                registration.Set();
            }
    }

    /// <summary>Wakes every registered device connection to check for available tasks.</summary>
    public void NotifyAll()
    {
        lock (_registrations)
            foreach (var registration in _registrations.Values)
                {
                registration.Set();
            }
    }

    /// <summary>Closes notification input and wakes consumers so their waits can finish.</summary>
    public void Close()
    {
        lock (_registrations)
        {
            foreach (var registration in _registrations.Values)
                {
                registration.Close();
            }
            _registrations.Clear();
        }
    }

    /// <summary>Removes a task registration only if it still owns the device identity.</summary>
    /// <param name="registration">Owned device task registration to remove.</param>
    internal void Unregister(GXTaskRegistration registration)
    {
        lock (_registrations)
        {
            if (_registrations.TryGetValue(registration.Id, out var current) && ReferenceEquals(current, registration))
                {
                _registrations.Remove(registration.Id);
            }
            registration.Close();
        }
    }
}
