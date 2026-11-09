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

using Gurux.DLMS.AMI.Services.Database;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Publishes SQL notifications without allowing recorder failures to affect database commands.</summary>
public sealed class GXSqlEvents(IHttpContextAccessor contexts, ILogger<GXSqlEvents> logger) : IGXAmiSqlEvents
{
    private readonly AsyncLocal<int> _suppression = new();
    /// <inheritdoc/>
    public event Action<GXAmiSqlCommand>? Executed;
    /// <inheritdoc/>
    public bool HasListeners => Executed != null && _suppression.Value == 0;

    /// <inheritdoc/>
    public IDisposable Suppress()
    {
        ++_suppression.Value;
        return new Suppression(this);
    }

    /// <summary>Notifies listeners of a completed SQL command.</summary>
    /// <param name="sql">Executed SQL text.</param>
    /// <param name="duration">Execution duration in milliseconds.</param>
    /// <param name="provider">Database provider name.</param>
    public void Publish(string sql, double duration, string provider)
    {
        if (!HasListeners)
        {
            return;
        }
        var context = contexts.HttpContext;
        var command = new GXAmiSqlCommand(sql, duration, DateTimeOffset.UtcNow,
            context == null ? null : ServerHelpers.GetUserId(context.User, false), context?.Request.Path.Value, provider);
        foreach (Action<GXAmiSqlCommand> listener in (Executed?.GetInvocationList() ?? []).Cast<Action<GXAmiSqlCommand>>())
        {
            try
            {
                listener(command);
            }
            catch (Exception error)
            {
                logger.LogError(error, "SQL recording subscriber failed.");
            }
        }
    }

    private sealed class Suppression(GXSqlEvents owner) : IDisposable
    {
        private bool _disposed;
        public void Dispose() { if (!_disposed) { _disposed = true; --owner._suppression.Value; } }
    }
}
