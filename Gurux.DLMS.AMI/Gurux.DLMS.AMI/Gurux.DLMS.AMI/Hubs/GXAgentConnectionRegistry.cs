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

namespace Gurux.DLMS.AMI.Hubs;

internal sealed class GXAgentConnectionInUseException() : InvalidOperationException(
    "The agent connection is already in use. Use a separate access token for another agent.");

/// <summary>Serializes registered agent presence changes with their persisted status updates.</summary>
internal sealed class GXAgentConnectionRegistry
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, Guid> _connections = [];
    private readonly Dictionary<string, string> _tokens = [];

    internal async Task<bool> IsTokenInUseAsync(string? tokenId)
    {
        if (string.IsNullOrEmpty(tokenId)) {
            return false;
        }
        await _gate.WaitAsync();
        try { return _tokens.Values.Contains(tokenId); }
        finally { _gate.Release(); }
    }

    internal async Task RegisterAsync(Guid agentId, string connectionId, Func<Guid, bool, Task> update, string? tokenId = null)
    {
        await _gate.WaitAsync();
        try
        {
            if (_connections.TryGetValue(connectionId, out var current))
            {
                if (current != agentId) {
                    throw new InvalidOperationException("Connection is already registered to another agent.");
                }
                return;
            }
            if (_connections.Values.Contains(agentId) ||
                (!string.IsNullOrEmpty(tokenId) && _tokens.Values.Contains(tokenId)))
                {
                throw new GXAgentConnectionInUseException();
            }
            await update(agentId, true);
            _connections.Add(connectionId, agentId);
            if (!string.IsNullOrEmpty(tokenId)) {
                _tokens.Add(connectionId, tokenId);
            }
        }
        finally { _gate.Release(); }
    }

    internal async Task DisconnectAsync(string connectionId, Func<Guid, bool, Task> update)
    {
        await _gate.WaitAsync();
        try
        {
            if (!_connections.Remove(connectionId, out var agentId)) {
                return;
            }
            _tokens.Remove(connectionId);
            if (!_connections.Values.Contains(agentId)) {
                await update(agentId, false);
            }
        }
        finally { _gate.Release(); }
    }
}
