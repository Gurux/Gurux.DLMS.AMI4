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

using Gurux.DLMS.AMI.Shared.DTOs.History;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Module;
using Gurux.UI.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Gurux.DLMS.AMI.Http;

namespace Gurux.DLMS.AMI.Client.Pages;

/// <summary>Configures history recording for entity types.</summary>
public partial class History : IDisposable
{
    [Inject] private HttpClient Http { get; set; } = default!;
    [Inject] private AuthenticationStateProvider Authentication { get; set; } = default!;
    [Inject] private IServiceProvider Services { get; set; } = default!;
    [Inject] private IGXAmiConfirmation Confirmation { get; set; } = default!;

    private IGXProgress? Progress => Services.GetService<IGXProgress>();
    private IGXNotificationService? NotificationService => Services.GetService<IGXNotificationService>();

    private readonly CancellationTokenSource _lifetime = new();
    private readonly HashSet<Guid> _saving = [];
    private GXChangeHistoryType[] _types = [];
    private bool _isAdmin;
    private bool _identityLoaded;
    private bool _loadingTypes;
    private bool _disposed;
    private bool _typesLoaded;
    private bool _applyingDefaults;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        _isAdmin = (await Authentication.GetAuthenticationStateAsync()).User.IsInRole(Shared.GXRoles.Admin);
        if (_disposed)
        {
            return;
        }

        _identityLoaded = true;
        await LoadTypesAsync();
    }

    private async Task LoadTypesAsync()
    {
        if (_disposed || _loadingTypes || _saving.Count != 0)
        {
            return;
        }

        _loadingTypes = true;
        using var progress = Progress?.ProgressStart(GXProgress.RetrievingHistory);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            _lifetime.Token, progress?.CancellationToken ?? CancellationToken.None);
        NotificationService?.RemoveByKey("Pages/History.LoadTypesAsync");
        try
        {
            var response = await Http.PostAsJson<ListChangeHistoryTypesResponse>("api/ChangeHistoryType/List",
                new ListChangeHistoryTypes { OrderBy = nameof(GXChangeHistoryType.Name) }, cancellation.Token);
            if (!_disposed)
            {
                _types = [.. response.Items ?? []];
                _typesLoaded = true;
            }
        }
        catch (AccessTokenNotAvailableException error) { error.Redirect(); }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception error) { NotificationService?.ReportError(error, "Pages/History.LoadTypesAsync"); }
        finally { _loadingTypes = false; }
    }

    private async Task SaveRecordingAsync(GXChangeHistoryType type, bool value)
    {
        if (_disposed || !_isAdmin || _applyingDefaults || type.RecordHistory == value || !_saving.Add(type.Id))
        {
            return;
        }

        using var progress = Progress?.ProgressStart(GXProgress.SavingHistory);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            _lifetime.Token, progress?.CancellationToken ?? CancellationToken.None);
        var notificationKey = $"Pages/History.SaveRecordingAsync/{type.Id}";
        NotificationService?.RemoveByKey(notificationKey);
        try
        {
            // Do not send navigation items or mutate the displayed state before the save succeeds.
            await Http.PostAsJson<UpdateChangeHistoryTypeResponse>("api/ChangeHistoryType/Update",
                new UpdateChangeHistoryType
                {
                    ChangeHistoryTypes = [new() { Id = type.Id, Name = type.Name, RecordHistory = value }]
                }, cancellation.Token);
            if (!_disposed)
            {
                type.RecordHistory = value;
            }
        }
        catch (AccessTokenNotAvailableException error) { error.Redirect(); }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception error) { NotificationService?.ReportError(error, notificationKey); }
        finally { _saving.Remove(type.Id); }
    }

    private async Task ApplyDefaultsAsync()
    {
        if (_disposed || !_isAdmin || _loadingTypes || _applyingDefaults || _saving.Count != 0)
        {
            return;
        }

        _applyingDefaults = true;
        const string notificationKey = "Pages/History.ApplyDefaultsAsync";
        try
        {
            var result = await Confirmation.ConfirmAsync(Properties.Resources.ApplyBuiltInDefaults,
                Properties.Resources.SelectedItemsWillBeResetToDefaultsWithoutASelectionOnlyMissingDefaultsWillBeAdded,
                new AmiConfirmationOptions { ConfirmText = Properties.Resources.ApplyDefaults, AllowDelete = false });
            if (!result.Confirm || _disposed)
            {
                return;
            }

            using var progress = Progress?.ProgressStart(Properties.Resources.ApplyDefaults);
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
                _lifetime.Token, progress?.CancellationToken ?? CancellationToken.None);
            NotificationService?.RemoveByKey(notificationKey);
            try
            {
                // Without a row selection the shared defaults endpoint adds only missing types.
                await Http.PostAsJson<RegenerateChangeHistoryTypeResponse>("api/ChangeHistoryType/Regenerate",
                    new RegenerateChangeHistoryType(), cancellation.Token);
                cancellation.Token.ThrowIfCancellationRequested();
                await LoadTypesAsync();
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        }
        catch (AccessTokenNotAvailableException error) { error.Redirect(); }
        catch (Exception error) { NotificationService?.ReportError(error, notificationKey); }
        finally { _applyingDefaults = false; }
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
