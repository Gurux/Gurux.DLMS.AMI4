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

using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.History;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.UI.Components;
using Gurux.UI.Components.Table;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Gurux.DLMS.AMI.Client.Pages;

/// <summary>Lists accessible entity changes independently of history configuration.</summary>
public partial class ChangeHistory : IDisposable
{
    [Inject] private HttpClient Http { get; set; } = default!;
    [Inject] private AuthenticationStateProvider Authentication { get; set; } = default!;

    private readonly CancellationTokenSource _lifetime = new();
    private GXTable<GXChangeHistoryItem>? _changesTable;
    private bool _isAdmin;
    private bool _identityLoaded;
    private bool _changesLoaded;
    private bool _disposed;
    private long _totalChanges;
    private string? _changesError;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        _isAdmin = (await Authentication.GetAuthenticationStateAsync()).User.IsInRole(Shared.GXRoles.Admin);
        if (!_disposed)
        {
            _identityLoaded = true;
        }
    }

    private async ValueTask<ItemsProviderResult<GXChangeHistoryItem>> GetChangesAsync(GXItemsProviderRequest request)
    {
        if (_disposed)
        {
            return new([], 0);
        }

        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token, request.CancellationToken);
        try
        {
            var response = await Http.PostAsJson<ListChangeHistoryItemsResponse>("api/ChangeHistory/List",
                new ListChangeHistoryItems
                {
                    Index = request.StartIndex,
                    Count = request.Count,
                    OrderBy = nameof(GXChangeHistoryItem.ChangedAt),
                    Descending = true,
                    AllUsers = _isAdmin
                }, cancellation.Token);
            _changesError = null;
            _totalChanges = response.Count;
            _changesLoaded = true;
            if (!_disposed)
            {
                await InvokeAsync(StateHasChanged);
            }

            return new(response.Items ?? [], Helpers.ClientHelpers.ToVirtualizedCount(response.Count));
        }
        catch (OperationCanceledException) { throw; }
        catch (AccessTokenNotAvailableException error) { error.Redirect(); }
        catch (Exception)
        {
            _changesError = "Changes could not be loaded. Please try again.";
            if (!_disposed)
            {
                await InvokeAsync(StateHasChanged);
            }
        }
        return new([], 0);
    }

    private Task RefreshChangesAsync() => _changesTable?.RefreshDataAsync(true) ?? Task.CompletedTask;

    private static string ActorName(GXUser? actor)
    {
        if (actor == null)
        {
            return string.Empty;
        }

        var name = string.Join(" ", new[] { actor.GivenName, actor.Surname }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return name.Length != 0 ? name : actor.UserName ?? actor.Id ?? string.Empty;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _lifetime.Cancel();
        _lifetime.Dispose();
    }
}
