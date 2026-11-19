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
    /// <summary>Transport used to retrieve the current cookie-backed session.</summary>
    private readonly HttpClient _http;
    /// <summary>Navigation source whose page changes trigger a session refresh.</summary>
    private readonly NavigationManager _navigation;
    /// <summary>Records failures while refreshing session claims.</summary>
    private readonly ILogger<GXSessionStateProvider> _logger;
    /// <summary>Shared authentication-state request for the current page generation.</summary>
    private Task<AuthenticationState>? _state;
    /// <summary>Prevents navigation callbacks from publishing authentication changes after disposal.</summary>
    private bool _disposed;
    /// <summary>Monotonic request generation used to discard superseded refresh notifications.</summary>
    private int _requestVersion;

    /// <summary>Creates a session provider and subscribes to page navigation changes.</summary>
    /// <param name="http">HTTP transport used to access the AMI API.</param>
    /// <param name="navigation">Navigation service used to observe page transitions or redirect to login.</param>
    /// <param name="logger">Logger used to report recoverable client failures.</param>
    public GXSessionStateProvider(HttpClient http, NavigationManager navigation, ILogger<GXSessionStateProvider> logger)
    {
        this._http = http;
        this._navigation = navigation;
        this._logger = logger;
        navigation.LocationChanged += LocationChanged;
    }

    /// <summary>Returns the cached session load or starts it for the current page generation.</summary>
    /// <returns>The authentication state reconstructed from the session API.</returns>
    public override Task<AuthenticationState> GetAuthenticationStateAsync() => _state ??= LoadAsync();

    /// <summary>Loads session claims and falls back to an anonymous principal when the API is unavailable.</summary>
    /// <returns>The authenticated session principal or an anonymous authentication state.</returns>
    private async Task<AuthenticationState> LoadAsync()
    {
        try
        {
            var session = await _http.GetFromJsonAsync<GXSessionState>("api/Session");
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
            _logger.LogWarning(exception, "Cannot refresh the current session.");
        }
        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    /// <summary>Starts a new session refresh after a page transition while the provider is active.</summary>
    /// <param name="sender">Object that raised the navigation event; it can be null.</param>
    /// <param name="args">Details of the navigation change.</param>
    private void LocationChanged(object? sender, LocationChangedEventArgs args)
    {
        if (_disposed)
        {
            return;
        }
        var previous = _state;
        _state = RefreshAsync(previous, ++_requestVersion);
    }

    /// <summary>Reloads session claims and announces a changed user only for the newest active request.</summary>
    /// <param name="previous">Previous authentication-state request, or null before the first load.</param>
    /// <param name="version">Request generation that must still be current before publishing a change.</param>
    /// <returns>The freshly loaded authentication state.</returns>
    private async Task<AuthenticationState> RefreshAsync(Task<AuthenticationState>? previous, int version)
    {
        var oldState = previous == null ? null : await previous;
        var current = await LoadAsync();
        if (!_disposed && version == _requestVersion && (oldState == null || GXClientAuthorizationCache.UserKey(oldState.User) != GXClientAuthorizationCache.UserKey(current.User)))
        {
            NotifyAuthenticationStateChanged(Task.FromResult(current));
        }
        return current;
    }

    /// <summary>Detaches page navigation notifications and prevents later refresh publications.</summary>
    public void Dispose()
    {
        _disposed = true;
        _navigation.LocationChanged -= LocationChanged;
    }
}
