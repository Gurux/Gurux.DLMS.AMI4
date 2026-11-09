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
    public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(30);
    private sealed class Entry(long generation)
    {
        public long Generation { get; } = generation;
        public DateTimeOffset Expires = DateTimeOffset.MaxValue;
        public readonly TaskCompletionSource<bool?> Result = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private readonly Lock sync = new();
    private readonly Dictionary<(string User, string Policy), Entry> entries = [];
    private readonly HttpClient http;
    private readonly AuthenticationStateProvider? authentication;
    private readonly IGXNotifier? notifier;
    private readonly TimeProvider clock;
    private readonly ITimer timer;
    private DateTimeOffset retryAt = DateTimeOffset.MaxValue;
    private long generation;
    private bool disposed;
    public event Action? Changed;

    public GXClientAuthorizationCache(HttpClient http, AuthenticationStateProvider? authentication = null,
        IGXNotifier? notifier = null, TimeProvider? timeProvider = null)
    {
        this.http = http;
        this.authentication = authentication;
        this.notifier = notifier;
        clock = timeProvider ?? TimeProvider.System;
        timer = clock.CreateTimer(_ => Expire(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        if (authentication != null)
        {
            authentication.AuthenticationStateChanged += AuthenticationChanged;
        }

        notifier?.On<IEnumerable<GXTarget>>(this, nameof(IGXHubEvents.OnUpdate), ResourcesChanged);
    }

    public async Task<bool> CheckAsync(ClaimsPrincipal user, string policy)
    {
        if (string.IsNullOrWhiteSpace(policy) || policy.Length > 128)
        {
            return false;
        }

        var key = (User: UserKey(user), Policy: policy);
        Entry entry;
        bool start = false;
        lock (sync)
        {
            if (disposed)
            {
                return false;
            }

            if (!entries.TryGetValue(key, out entry!) || entry.Expires <= clock.GetUtcNow())
            {
                entry = new Entry(generation);
                entries[key] = entry;
                start = true;
            }
        }
        if (start)
        {
            _ = FetchAsync(key, entry);
        }

        var result = await entry.Result.Task;
        lock (sync)
        {
            return !disposed && entry.Generation == generation && result == true;
        }
    }

    private async Task FetchAsync((string User, string Policy) key, Entry entry)
    {
        bool? allowed = null;
        try
        {
            using var response = await http.PostAsJsonAsync("api/Policy/Check", new CheckPolicies { PolicyNames = [key.Policy] });
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
            lock (sync)
            {
                if (!disposed && entry.Generation == generation && entries.GetValueOrDefault(key) == entry)
                {
                    var expires = clock.GetUtcNow() + Lifetime;
                    if (allowed.HasValue)
                    {
                        entry.Expires = expires;
                    }
                    else
                    {
                        entries.Remove(key);
                        retryAt = retryAt < expires ? retryAt : expires;
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

    public void Invalidate()
    {
        lock (sync)
        {
            if (disposed)
            {
                return;
            }

            ++generation;
            foreach (var entry in entries.Values)
            {
                entry.Result.TrySetResult(null);
            }

            entries.Clear();
            retryAt = DateTimeOffset.MaxValue;
            Schedule();
        }
        Changed?.Invoke();
    }

    private void AuthenticationChanged(Task<AuthenticationState> _) => Invalidate();

    private void ResourcesChanged(IEnumerable<GXTarget> items)
    {
        if (items.Any(item => IsPermissionChange(item.Type)))
        {
            Invalidate();
        }
    }

    internal static bool IsPermissionChange(string type) =>
        type == GXPolicyPolicies.Add || type == GXPolicyPolicies.Edit || type == GXPolicyPolicies.Delete ||
        type == GXRolePolicies.Add || type == GXRolePolicies.Edit || type == GXRolePolicies.Delete ||
        type == GXUserPolicies.Add || type == GXUserPolicies.Edit || type == GXUserPolicies.Delete ||
        type == GXModulePolicies.Add || type == GXModulePolicies.Edit || type == GXModulePolicies.Delete ||
        type == GXUserGroupPolicies.Add || type == GXUserGroupPolicies.Edit || type == GXUserGroupPolicies.Delete;

    private void Expire()
    {
        bool changed;
        lock (sync)
        {
            if (disposed)
            {
                return;
            }

            var now = clock.GetUtcNow();
            var expired = entries.Where(pair => pair.Value.Expires <= now).Select(pair => pair.Key).ToArray();
            foreach (var key in expired)
            {
                entries.Remove(key);
            }

            changed = expired.Length != 0 || retryAt <= now;
            if (retryAt <= now)
            {
                retryAt = DateTimeOffset.MaxValue;
            }

            Schedule();
        }
        if (changed)
        {
            Changed?.Invoke();
        }
    }

    // Called under sync. A one-shot timer wakes components at the first decision expiry.
    private void Schedule()
    {
        var next = entries.Values.Select(entry => entry.Expires).Append(retryAt).Min();
        timer.Change(next == DateTimeOffset.MaxValue ? Timeout.InfiniteTimeSpan :
            TimeSpan.FromTicks(Math.Max(0, (next - clock.GetUtcNow()).Ticks)), Timeout.InfiniteTimeSpan);
    }

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

    public void Dispose()
    {
        lock (sync)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            ++generation;
            foreach (var entry in entries.Values)
            {
                entry.Result.TrySetResult(null);
            }

            entries.Clear();
            timer.Dispose();
        }
        if (authentication != null)
        {
            authentication.AuthenticationStateChanged -= AuthenticationChanged;
        }

        notifier?.RemoveListener(this);
    }
}
