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

using System.Collections.Concurrent;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Server.Internal;
using Microsoft.AspNetCore.SignalR;

namespace Gurux.DLMS.AMI.Server.Services;

/// <summary>Disconnects SignalR clients whose personal access token has been revoked or expired.</summary>
public sealed class GXPatConnections(IDbConnectionFactory database, ILogger<GXPatConnections> logger) : BackgroundService, IHubFilter
{
    private sealed record Connection(string TokenId, string? UserId, Action Abort);
    private readonly ConcurrentDictionary<string, Connection> _connections = new();

    /// <summary>Disconnects local connections after a committed PAT revocation.</summary>
    public void Revoke(string tokenId)
    {
        foreach (var entry in _connections)
            {
            if (entry.Value.TokenId == tokenId) {
                Disconnect(entry.Key);
            }
        }
    }

    private void Disconnect(string connectionId)
    {
        if (_connections.TryRemove(connectionId, out var connection)) {
            connection.Abort();
        }
    }

    private async Task ValidateAsync(HubCallerContext context)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            context.Abort();
            throw new HubException("Authentication is required.");
        }
        var tokenId = context.User?.FindFirst("jti")?.Value;
        if (tokenId == null) {
            return;
        } // Cookie authentication has no PAT identity.
        await using var connection = await database.GetConnectionAsync(context.ConnectionAborted);
        if (!await GXPatValidator.IsActiveAsync(connection, tokenId, GXPatValidator.UserId(context.User), context.ConnectionAborted))
        {
            context.Abort();
            throw new HubException("Personal access token is no longer active.");
        }
        var userId = GXPatValidator.UserId(context.User)!;
        GXTokenPermissionValidator.Restrict(context.User!, await UserPermissions.ReadEffectiveAsync(connection, userId),
            await UserPermissions.ReadRolesAsync(connection, userId));
    }

    /// <inheritdoc />
    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        var caller = context.Context;
        var tokenId = caller.User?.FindFirst("jti")?.Value;
        if (tokenId != null)
            {
            _connections[caller.ConnectionId] = new(tokenId, GXPatValidator.UserId(caller.User), caller.Abort);
        }
        try
        {
            await ValidateAsync(caller);
            await next(context);
        }
        catch
        {
            Disconnect(caller.ConnectionId);
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext context, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        await ValidateAsync(context.Context);
        return await next(context);
    }

    /// <inheritdoc />
    public Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
    {
        _connections.TryRemove(context.Context.ConnectionId, out _);
        return next(context, exception);
    }

    internal async Task CheckConnectionsAsync(CancellationToken token)
    {
        if (_connections.IsEmpty) {
            return;
        }
        await using var connection = await database.GetConnectionAsync(token);
        foreach (var entry in _connections)
        {
            if (!await GXPatValidator.IsActiveAsync(connection, entry.Value.TokenId, entry.Value.UserId, token))
                {
                Disconnect(entry.Key);
            }
        }
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { await CheckConnectionsAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error)
            {
                // If validation is unavailable, authenticated PAT streams must not keep receiving data.
                foreach (var entry in _connections) {
                    Disconnect(entry.Key);
                }
                logger.LogError(error, "Unable to validate personal access token connections.");
            }
        }
    }
}
