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
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.UI.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Short-lived decisions scoped to the current client and user claims.</summary>
public sealed class GXClientAuthorizationCache : IGXAuthorizationRefreshSource, IDisposable
{
    /// <summary>Maximum age of a successful authorization decision and delay before retrying an unavailable decision.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(30);
    /// <summary>One shared authorization request associated with an invalidation generation.</summary>
    /// <param name="generation">Invalidation generation in which the pending request is created.</param>
    private sealed class Entry(long generation)
    {
        /// <summary>Invalidation generation in which this authorization request was created.</summary>
        public long Generation { get; } = generation;
        /// <summary>Time after which this cached decision must be requested again.</summary>
        public DateTimeOffset Expires = DateTimeOffset.MaxValue;
        /// <summary>Shared completion that distinguishes allow, deny and an unavailable or superseded decision.</summary>
        public readonly TaskCompletionSource<bool?> Result = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>Protects authorization entries, invalidation generations and timer scheduling.</summary>
    private readonly Lock _sync = new();
    /// <summary>Pending and completed decisions keyed by the user&apos;s claims fingerprint and policy name.</summary>
    private readonly Dictionary<(string User, string Policy), Entry> _entries = [];
    /// <summary>Transport used to ask the policy API for authorization decisions.</summary>
    private readonly HttpClient _http;
    /// <summary>Optional authentication source whose changes invalidate cached decisions.</summary>
    private readonly AuthenticationStateProvider? _authentication;
    /// <summary>Optional resource notification source used to invalidate permission decisions.</summary>
    private readonly IGXNotifier? _notifier;
    /// <summary>Clock used to determine decision expiry and create the refresh timer.</summary>
    private readonly TimeProvider _clock;
    /// <summary>One-shot timer that announces the earliest decision expiry or retry time.</summary>
    private readonly ITimer _timer;
    /// <summary>Earliest time at which a previously unavailable decision should be retried.</summary>
    private DateTimeOffset _retryAt = DateTimeOffset.MaxValue;
    /// <summary>Counter used to reject responses from an earlier authentication or permission state.</summary>
    private long _generation;
    /// <summary>Prevents requests, timer work and invalidation after shutdown.</summary>
    private bool _disposed;
    /// <summary>Raised when decisions are invalidated or expire so interested components can recheck authorization.</summary>
    public event Action? Changed;

    /// <summary>Creates a client-scoped decision cache and subscribes to authentication and permission changes.</summary>
    /// <param name="http">HTTP transport used to access the AMI API.</param>
    /// <param name="authentication">Optional authentication provider whose changes invalidate cached decisions.</param>
    /// <param name="notifier">Optional notification source for permission-related resource updates.</param>
    /// <param name="timeProvider">Optional clock and timer source; the system clock is used when null.</param>
    public GXClientAuthorizationCache(HttpClient http, AuthenticationStateProvider? authentication = null,
        IGXNotifier? notifier = null, TimeProvider? timeProvider = null)
    {
        this._http = http;
        this._authentication = authentication;
        this._notifier = notifier;
        _clock = timeProvider ?? TimeProvider.System;
        _timer = _clock.CreateTimer(_ => Expire(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        if (authentication != null)
        {
            authentication.AuthenticationStateChanged += AuthenticationChanged;
        }

        notifier?.On<IEnumerable<GXTarget>>(this, nameof(IGXHubEvents.OnUpdate), ResourcesChanged);
    }

    /// <summary>Shares a policy check for the current claims fingerprint and rejects stale or unavailable responses.</summary>
    /// <param name="user">Principal whose complete identities and claims define the cache partition.</param>
    /// <param name="policy">Stable policy identifier whose authorization decision is requested.</param>
    /// <returns>True only when the current generation contains a successful allow decision.</returns>
    public async Task<bool> CheckAsync(ClaimsPrincipal user, string policy)
    {
        if (string.IsNullOrWhiteSpace(policy) || policy.Length > 128)
        {
            return false;
        }

        var key = (User: UserKey(user), Policy: policy);
        Entry entry;
        bool start = false;
        lock (_sync)
        {
            if (_disposed)
            {
                return false;
            }

            if (!_entries.TryGetValue(key, out entry!) || entry.Expires <= _clock.GetUtcNow())
            {
                entry = new Entry(_generation);
                _entries[key] = entry;
                start = true;
            }
        }
        if (start)
        {
            _ = FetchAsync(key, entry);
        }

        var result = await entry.Result.Task;
        lock (_sync)
        {
            return !_disposed && entry.Generation == _generation && result == true;
        }
    }

    /// <summary>Loads one policy decision and schedules its expiry or a retry after an unavailable response.</summary>
    /// <param name="key">Claims fingerprint and policy identifier for the shared pending decision.</param>
    /// <param name="entry">Pending decision that receives the result if its generation is still current.</param>
    /// <returns>A task that completes after the shared decision has been resolved.</returns>
    private async Task FetchAsync((string User, string Policy) key, Entry entry)
    {
        bool? allowed = null;
        try
        {
            using var response = await _http.PostAsJsonAsync("api/Policy/Check", new CheckPolicies { PolicyNames = [key.Policy] });
            if (response.IsSuccessStatusCode)
            {
                var decisions = await response.Content.ReadFromJsonAsync<CheckPoliciesResponse>();
                if (decisions?.Results?.TryGetValue(key.Policy, out var decision) == true)
                {
                    allowed = decision;
                }
            }
        }
        catch (HttpRequestException)
        {
        }
        catch (JsonException)
        {
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            lock (_sync)
            {
                if (!_disposed && entry.Generation == _generation && _entries.GetValueOrDefault(key) == entry)
                {
                    var expires = _clock.GetUtcNow() + Lifetime;
                    if (allowed.HasValue)
                    {
                        entry.Expires = expires;
                    }
                    else
                    {
                        _entries.Remove(key);
                        _retryAt = _retryAt < expires ? _retryAt : expires;
                    }
                    Schedule();
                }
                else
                {
                    allowed = null;
                }

                entry.Result.TrySetResult(allowed);
            }
        }
    }

    /// <summary>Discards decisions and releases pending callers before notifying components to recheck authorization.</summary>
    public void Invalidate()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            ++_generation;
            foreach (var entry in _entries.Values)
            {
                entry.Result.TrySetResult(null);
            }

            _entries.Clear();
            _retryAt = DateTimeOffset.MaxValue;
            Schedule();
        }
        Changed?.Invoke();
    }

    /// <summary>Invalidates all decisions when the authentication provider reports a new state.</summary>
    /// <param name="_">Authentication-state task whose change notification invalidates this cache.</param>
    private void AuthenticationChanged(Task<AuthenticationState> _) => Invalidate();

    /// <summary>Invalidates decisions when notifications include a policy or permission-bearing resource change.</summary>
    /// <param name="items">Resource items or update notifications to process.</param>
    private void ResourcesChanged(IEnumerable<GXTarget> items)
    {
        if (items.Any(item => IsPermissionChange(item.Type)))
        {
            Invalidate();
        }
    }

    /// <summary>Recognizes policy, role, user, module and user-group mutations that may change authorization.</summary>
    /// <param name="type">Resource or notification source identifier.</param>
    /// <returns>True when the resource event can affect authorization decisions.</returns>
    internal static bool IsPermissionChange(string type) =>
        type == GXPolicyPolicies.Add || type == GXPolicyPolicies.Edit || type == GXPolicyPolicies.Delete ||
        type == GXRolePolicies.Add || type == GXRolePolicies.Edit || type == GXRolePolicies.Delete ||
        type == GXUserPolicies.Add || type == GXUserPolicies.Edit || type == GXUserPolicies.Delete ||
        type == GXModulePolicies.Add || type == GXModulePolicies.Edit || type == GXModulePolicies.Delete ||
        type == GXUserGroupPolicies.Add || type == GXUserGroupPolicies.Edit || type == GXUserGroupPolicies.Delete;

    /// <summary>Removes expired decisions, schedules the next timer and notifies affected components.</summary>
    private void Expire()
    {
        bool changed;
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            var now = _clock.GetUtcNow();
            var expired = _entries.Where(pair => pair.Value.Expires <= now).Select(pair => pair.Key).ToArray();
            foreach (var key in expired)
            {
                _entries.Remove(key);
            }

            changed = expired.Length != 0 || _retryAt <= now;
            if (_retryAt <= now)
            {
                _retryAt = DateTimeOffset.MaxValue;
            }

            Schedule();
        }
        if (changed)
        {
            Changed?.Invoke();
        }
    }

    // Called under sync. A one-shot timer wakes components at the first decision expiry.
    /// <summary>Schedules a single timer for the earliest expiry or retry while holding the cache lock.</summary>
    private void Schedule()
    {
        var next = _entries.Values.Select(entry => entry.Expires).Append(_retryAt).Min();
        _timer.Change(next == DateTimeOffset.MaxValue ? Timeout.InfiniteTimeSpan :
            TimeSpan.FromTicks(Math.Max(0, (next - _clock.GetUtcNow()).Ticks)), Timeout.InfiniteTimeSpan);
    }

    /// <summary>Hashes ordered identities and claims into a culture-independent cache partition key.</summary>
    /// <param name="user">Principal whose complete identities and claims define the cache partition.</param>
    /// <returns>A hexadecimal SHA-256 fingerprint of the user&apos;s complete claims state.</returns>
    internal static string UserKey(ClaimsPrincipal user)
    {
        var identities = user.Identities.Select(identity => JsonSerializer.Serialize(new
        {
            identity.AuthenticationType,
            identity.IsAuthenticated,
            identity.NameClaimType,
            identity.RoleClaimType,
            Claims = identity.Claims.Select(claim => JsonSerializer.Serialize(new
            {
                claim.Type,
                claim.Value,
                claim.ValueType,
                claim.Issuer,
                claim.OriginalIssuer
            })).Order(StringComparer.Ordinal).ToArray()
        })).Order(StringComparer.Ordinal).ToArray();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(identities))));
    }

    /// <summary>Stops the timer, releases pending requests and detaches authentication and notification listeners.</summary>
    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            ++_generation;
            foreach (var entry in _entries.Values)
            {
                entry.Result.TrySetResult(null);
            }

            _entries.Clear();
            _timer.Dispose();
        }
        if (_authentication != null)
        {
            _authentication.AuthenticationStateChanged -= AuthenticationChanged;
        }

        _notifier?.RemoveListener(this);
    }
}
