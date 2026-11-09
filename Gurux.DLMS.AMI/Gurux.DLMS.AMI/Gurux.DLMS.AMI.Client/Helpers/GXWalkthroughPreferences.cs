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
using System.Text.Json;
using Gurux.DLMS.AMI.Client.Rest;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Components.Authorization;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Persists card dismissals in the signed-in user's application properties.</summary>
public sealed class GXWalkthroughPreferences : IDisposable
{
    public const string PropertyName = "Ami.Walkthroughs.Hidden";
    public const string GetStarted = "guide:get-started";
    public const string Videos = "guide:videos";
    public const string Fundamentals = "guide:learn-the-fundamentals";
    private readonly HttpClient _http;
    private readonly AuthenticationStateProvider _authentication;
    private readonly SemaphoreSlim _saving = new(1, 1);
    private HashSet<string> _hidden = new(StringComparer.Ordinal);
    private Task? _load;
    private long _generation;
    public event Action? Changed;

    public GXWalkthroughPreferences(HttpClient http, AuthenticationStateProvider authentication)
    {
        _http = http;
        _authentication = authentication;
        authentication.AuthenticationStateChanged += AuthenticationChanged;
    }

    public static string ReleaseKey(GXCatalogUpdateEntry item) => JsonSerializer.Serialize(new[] { "release", item.ProductId, item.Version });
    public bool IsHidden(string key) => _hidden.Contains(key);

    public Task LoadAsync()
    {
        if (_load?.IsFaulted == true || _load?.IsCanceled == true)
            {
            _load = null;
        }
        return _load ??= LoadCoreAsync(Interlocked.Read(ref _generation));
    }

    private async Task LoadCoreAsync(long generation)
    {
        var property = await ReadAsync();
        if (generation == Interlocked.Read(ref _generation))
            {
            _hidden = Decode(property);
        }
    }

    private async Task<GXUserProperty?> ReadAsync() =>
        (await _http.GetFromJsonAsync<GetUserProperties>("api/UserProperties?name=" + PropertyName))?.Item;

    private static HashSet<string> Decode(GXUserProperty? property) => string.IsNullOrWhiteSpace(property?.Value)
        ? new(StringComparer.Ordinal)
        : new((JsonSerializer.Deserialize<string[]>(property.Value) ?? []).Where(key => !string.IsNullOrWhiteSpace(key)), StringComparer.Ordinal);

    public async Task HideAsync(string key)
    {
        var generation = Interlocked.Read(ref _generation);
        await _saving.WaitAsync();
        try
        {
            // Merge the latest saved preferences so different cards never overwrite each other.
            var property = await ReadAsync();
            var hidden = Decode(property);
            hidden.Add(key);
            if (generation != Interlocked.Read(ref _generation))
                {
                throw new OperationCanceledException();
            }
            using var response = await _http.PostAsJsonAsync("api/UserProperties/Update", new UpdateUserProperties
            {
                Properties = [new GXUserProperty
                {
                    Id = property?.Id ?? Guid.Empty, Name = PropertyName, Value = JsonSerializer.Serialize(hidden.Order()),
                    ConcurrencyStamp = property?.ConcurrencyStamp
                }]
            });
            response.EnsureSuccessStatusCode();
            if (generation != Interlocked.Read(ref _generation))
                {
                return;
            }
            _hidden = hidden;
            _load = Task.CompletedTask;
            Changed?.Invoke();
        }
        finally { _saving.Release(); }
    }

    private void AuthenticationChanged(Task<AuthenticationState> state)
    {
        Interlocked.Increment(ref _generation);
        _hidden = new(StringComparer.Ordinal);
        _load = null;
        Changed?.Invoke();
    }

    public void Dispose() => _authentication.AuthenticationStateChanged -= AuthenticationChanged;
}
