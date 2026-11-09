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
using System.Security.Claims;
using Gurux.DLMS.AMI.Client.Rest;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Logging;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Refreshes cookie session state when navigating between interactive pages.</summary>
public sealed class GXSessionStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly HttpClient http;
    private readonly NavigationManager navigation;
    private readonly ILogger<GXSessionStateProvider> logger;
    private Task<AuthenticationState>? state;
    private bool disposed;
    private int requestVersion;

    public GXSessionStateProvider(HttpClient http, NavigationManager navigation, ILogger<GXSessionStateProvider> logger)
    {
        this.http = http;
        this.navigation = navigation;
        this.logger = logger;
        navigation.LocationChanged += LocationChanged;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => state ??= LoadAsync();

    private async Task<AuthenticationState> LoadAsync()
    {
        try
        {
            var session = await http.GetFromJsonAsync<GXSessionState>("api/Session");
            if (session?.IsAuthenticated == true)
            {
                var identity = new ClaimsIdentity("Session", ClaimTypes.Name, ClaimTypes.Role);
                if (session.UserId != null)
                {
                    identity.AddClaim(new(ClaimTypes.NameIdentifier, session.UserId));
                }
            
                if (session.Name != null)
                {
                    identity.AddClaim(new(ClaimTypes.Name, session.Name));
                }
            
                identity.AddClaims(session.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
                return new AuthenticationState(new ClaimsPrincipal(identity));
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Cannot refresh the current session.");
        }
        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    private void LocationChanged(object? sender, LocationChangedEventArgs args)
    {
        if (disposed)
        {
            return;
        }
        var previous = state;
        state = RefreshAsync(previous, ++requestVersion);
    }

    private async Task<AuthenticationState> RefreshAsync(Task<AuthenticationState>? previous, int version)
    {
        var oldState = previous == null ? null : await previous;
        var current = await LoadAsync();
        if (!disposed && version == requestVersion && (oldState == null || GXClientAuthorizationCache.UserKey(oldState.User) != GXClientAuthorizationCache.UserKey(current.User)))
        {
            NotifyAuthenticationStateChanged(Task.FromResult(current));
        }
        return current;
    }

    public void Dispose()
    {
        disposed = true;
        navigation.LocationChanged -= LocationChanged;
    }
}
