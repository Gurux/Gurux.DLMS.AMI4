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

using System.Net.Http.Json;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.UI.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Caches available policy names for the current client and authentication state.</summary>
public sealed class GXPolicyNameCache : IGXPolicyNameProvider, IDisposable
{
    private readonly object sync = new();
    private readonly HttpClient http;
    private readonly AuthenticationStateProvider? authentication;
    private readonly IGXNotifier? notifier;
    private readonly CancellationTokenSource lifetime = new();
    private Task<string[]>? pending;
    private bool disposed;

    /// <summary>Creates a policy-name cache shared by editors in the current client scope.</summary>
    /// <param name="http">Transport used to load policy names available to the signed-in user.</param>
    /// <param name="authentication">Optional authentication source that invalidates names when the user changes.</param>
    /// <param name="notifier">Optional notification source that invalidates names when permissions change.</param>
    public GXPolicyNameCache(HttpClient http, AuthenticationStateProvider? authentication = null, IGXNotifier? notifier = null)
    {
        this.http = http;
        this.authentication = authentication;
        this.notifier = notifier;
        if (authentication != null)
        {
            authentication.AuthenticationStateChanged += AuthenticationChanged;
        }
        notifier?.On<IEnumerable<GXTarget>>(this, nameof(IGXHubEvents.OnUpdate), ResourcesChanged);
    }

    /// <summary>Returns an independent snapshot of available policy names, sharing any pending load with other editors.</summary>
    /// <param name="cancellationToken">Stops this caller's wait without cancelling a request shared by other editors.</param>
    /// <returns>Policy names loaded for the current authentication and permission state.</returns>
    /// <exception cref="ObjectDisposedException">The cache has been disposed.</exception>
    /// <remarks>Failed loads can be retried. Responses superseded by an invalidation are not returned.</remarks>
    public async Task<string[]> GetAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Task<string[]> request;
            lock (sync)
            {
                ObjectDisposedException.ThrowIf(disposed, this);
                request = pending ??= LoadAsync();
            }
            string[] names;
            try
            {
                // An editor going away must not cancel a request shared by another tab.
                names = await request.WaitAsync(cancellationToken);
            }
            catch
            {
                lock (sync)
                {
                    if (ReferenceEquals(pending, request) && request.IsCompleted && !request.IsCompletedSuccessfully)
                    {
                        pending = null;
                    }
                }
                throw;
            }
            lock (sync)
            {
                ObjectDisposedException.ThrowIf(disposed, this);
                if (ReferenceEquals(pending, request))
                {
                    return names.ToArray();
                }
            }
            // An authentication or policy change superseded the response while it was loading.
        }
    }

    private async Task<string[]> LoadAsync()
        => await http.GetFromJsonAsync<string[]>("api/Policy/Available", lifetime.Token) ?? [];

    /// <summary>Discards cached names so the next caller reloads the available policies.</summary>
    /// <remarks>Callers waiting on an older successful response join a fresh load instead of receiving stale names.</remarks>
    public void Invalidate()
    {
        lock (sync)
        {
            pending = null;
        }
    }

    private void AuthenticationChanged(Task<AuthenticationState> _) => Invalidate();

    private void ResourcesChanged(IEnumerable<GXTarget> items)
    {
        if (items.Any(item => GXClientAuthorizationCache.IsPermissionChange(item.Type)))
        {
            Invalidate();
        }
    }

    /// <summary>Unsubscribes from authentication and resource notifications and cancels the outstanding HTTP load.</summary>
    public void Dispose()
    {
        lock (sync)
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
            pending = null;
        }
        if (authentication != null)
        {
            authentication.AuthenticationStateChanged -= AuthenticationChanged;
        }
        notifier?.RemoveListener(this);
        lifetime.Cancel();
        lifetime.Dispose();
    }
}
