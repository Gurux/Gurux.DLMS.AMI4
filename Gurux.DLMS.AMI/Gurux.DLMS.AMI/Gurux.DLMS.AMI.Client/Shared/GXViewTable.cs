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

using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Client.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.View;
using Gurux.UI.Components;
using Gurux.UI.Components.Enums;
using Gurux.UI.Components.Table;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Gurux.DLMS.AMI.Client.Shared;

/// <summary>A GXTable with cached shared defaults and the AMI column editor.</summary>
[CascadingTypeParameter(nameof(TItem))]
public class GXViewTable<TItem> : GXTable<TItem>, IDisposable, IAsyncDisposable
{
    /// <summary>The stable name shared by all instances of this table's defaults.</summary>
    [Parameter, EditorRequired] public string ViewName { get; set; } = default!;
    /// <summary>The original code-defined columns, before shared or personal changes.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<GXViewColumn> ViewColumns { get; set; } = [];
    /// <summary>Access policies assigned when shared defaults are first saved.</summary>
    [Parameter] public List<string>? ViewPolicies { get; set; }
    /// <summary>Display title assigned when shared defaults are first saved.</summary>
    [Parameter] public string? ViewTitle { get; set; }
    [CascadingParameter] private Task<AuthenticationState>? AuthenticationState { get; set; }

    private IAmiViewStorage? _viewStorage;
    private GXResourceStorage? _cultureStorage;
    private GXView? _view;
    private bool _isAdmin;
    private bool _initialized;
    private bool _tableRendered;
    private bool _disposed;
    private long _generation;
    private SelectionMode _callerSelectionMode = SelectionMode.Single;
    private int _callerPageSize = 10;
    private bool _callerShowAllData;
    private bool _callerAllowColumnCustomization = true;
    private string? _automaticPreferencesId;

    /// <summary>Restores a selected row without invoking user interaction callbacks.</summary>
    public void RestoreSelection(TItem? item)
    {
        Active = item;
    }

    /// <summary>Resets pagination before the caller refreshes the table data.</summary>
    public void ResetPage()
    {
        CurrentPage = 0;
    }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        // The owner may supply dynamic values again after shared settings were applied.
        if (parameters.TryGetValue<SelectionMode>(nameof(SelectionMode), out var selection))
        {
            _callerSelectionMode = selection;
        }
        if (parameters.TryGetValue<int>(nameof(PageSize), out var size))
        {
            _callerPageSize = size;
        }
        if (parameters.TryGetValue<bool>(nameof(ShowAllData), out var all))
        {
            _callerShowAllData = all;
        }
        if (parameters.TryGetValue<bool>(nameof(AllowColumnCustomization), out var customize))
        {
            _callerAllowColumnCustomization = customize;
        }
        return base.SetParametersAsync(parameters);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        EnsurePreferencesId();
        _cultureStorage = Services.GetService<GXResourceStorage>();
        if (_cultureStorage != null)
        {
            _cultureStorage.OnLanguageChanged += CultureChanged;
        }
        _viewStorage = Services.GetService<IAmiViewStorage>();
        // Subscribe before either authentication or the cache read can yield.
        if (_viewStorage != null)
        {
            _viewStorage.ViewChanged += OnViewChanged;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        var generation = Interlocked.Read(ref _generation);
        try
        {
            await ReadAuthenticationAsync();
            var view = await GXViewSettings.ReadAsync(Services.GetRequiredService<HttpClient>(), ViewName, _viewStorage);
            if (!_disposed && generation == Interlocked.Read(ref _generation))
            {
                _view = ActiveView(view);
            }
        }
        catch (AccessTokenNotAvailableException exception)
        {
            exception.Redirect();
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
        if (_disposed)
        {
            return;
        }
        ApplyViewDefaults();
        // Shared defaults must exist before GXTable loads personal preferences.
        await base.OnInitializedAsync();
        if (!_disposed)
        {
            _initialized = true;
        }
    }

    protected override void OnParametersSet()
    {
        EnsurePreferencesId();
        ApplyViewDefaults();
        base.OnParametersSet();
    }

    protected override async Task OnParametersSetAsync()
    {
        await ReadAuthenticationAsync();
        if (_disposed)
        {
            return;
        }
        ApplyViewDefaults();
        base.OnParametersSet();
        await base.OnParametersSetAsync();
    }

    private async Task ReadAuthenticationAsync()
    {
        var authentication = AuthenticationState;
        var state = authentication == null ? null : await authentication;
        if (!_disposed && ReferenceEquals(authentication, AuthenticationState))
        {
            _isAdmin = state?.User.IsInRole(GXRoles.Admin) == true;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // GXTable's children can request data as soon as they render.
        if (_initialized && !_disposed)
        {
            base.BuildRenderTree(builder);
        }
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        // Blazor can complete an empty render while shared defaults are still loading.
        // Defer both browser resizing setup and the initial provider call until the table exists.
        if (!_initialized || _disposed)
        {
            return Task.CompletedTask;
        }
        bool firstTableRender = !_tableRendered;
        _tableRendered = true;
        return base.OnAfterRenderAsync(firstTableRender);
    }

    private void EnsurePreferencesId()
    {
        if (PreferencesId == _automaticPreferencesId)
        {
            PreferencesId = null;
        }
        if (!string.IsNullOrWhiteSpace(PreferencesId))
        {
            return;
        }
        if (AdditionalAttributes?.Any(attribute => string.Equals(attribute.Key, "id", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(Convert.ToString(attribute.Value))) == true)
        {
            return;
        }
        PreferencesId = _automaticPreferencesId = "view:" + ViewName;
    }

    private static GXView? ActiveView(GXView? view) => view?.Enabled == false ? null : view;

    private void ApplyViewDefaults()
    {
        var columns = GXViewColumnDefaults.Merge(ViewColumns, _view?.Columns);
        DefaultColumns = [.. columns.Where(column => column.Visible).Select(column => column.Name!)];
        DefaultColumnOrder = [.. columns.Select(column => column.Name!)];
        DefaultColumnVisibility = columns.Where(column => column.Visibility.HasValue && Enum.IsDefined((Visibility)column.Visibility.Value))
            .ToDictionary(column => column.Name!, column => (Visibility)column.Visibility!.Value, StringComparer.Ordinal);
        AllowColumnCustomization = _callerAllowColumnCustomization && GXViewSettings.CanCustomizeColumns(_view, _isAdmin);
        SelectionMode = _view?.SelectionMode is byte mode && Enum.IsDefined((SelectionMode)mode)
            ? (SelectionMode)mode : _callerSelectionMode;
        PageSize = _view?.PageSize ?? _callerPageSize;
        ShowAllData = _view?.ShowAllData ?? _callerShowAllData;
        ColumnEditorContent = editor => builder =>
        {
            builder.OpenComponent<GXViewColumnEditor>(0);
            builder.AddAttribute(1, nameof(GXViewColumnEditor.Editor), editor);
            builder.AddAttribute(2, nameof(GXViewColumnEditor.ViewName), ViewName);
            builder.AddAttribute(3, nameof(GXViewColumnEditor.Title), ViewTitle);
            builder.AddAttribute(4, nameof(GXViewColumnEditor.DefaultColumns), ViewColumns);
            builder.AddAttribute(5, nameof(GXViewColumnEditor.View), _view);
            builder.AddAttribute(6, nameof(GXViewColumnEditor.Policies), ViewPolicies);
            builder.AddAttribute(7, nameof(GXViewColumnEditor.ViewChanged), EventCallback.Factory.Create<GXView?>(this, ApplyChangedView));
            builder.CloseComponent();
        };
        OnColumnEditorReset = EventCallback.Factory.Create<GXTableColumnEditorContext>(this,
            editor => GXViewColumnEditor.ResetAsync(editor, ViewColumns));
    }

    private void ApplyChangedView(GXView? view)
    {
        if (_disposed)
        {
            return;
        }
        Interlocked.Increment(ref _generation);
        _view = ActiveView(view);
        ApplyViewDefaults();
        // StateHasChanged alone does not rerun this component's parameter lifecycle.
        base.OnParametersSet();
        StateHasChanged();
    }

    private async void OnViewChanged(object? sender, GXViewChangedEventArgs args)
    {
        if (_disposed || !string.Equals(args.Name, ViewName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        var generation = Interlocked.Increment(ref _generation);
        try
        {
            await InvokeAsync(() =>
            {
                if (_disposed || generation != Interlocked.Read(ref _generation))
                {
                    return;
                }
        
                _view = ActiveView(args.View);
                ApplyViewDefaults();
                base.OnParametersSet();
                StateHasChanged();
            });
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }

    private void ReportError(Exception exception)
    {
        if (!_disposed)
        {
            Services.GetService<IGXNotificationService>()?.ReportError(exception, "GXViewTable." + ViewName);
        }
    }

    private void CultureChanged(object? sender, string? language)
    {
        if (!_disposed)
        {
            _ = InvokeAsync(() =>
            {
                if (!_disposed)
                {
                    StateHasChanged();
                }
            });
        }
    }

    private void DetachViewStorage()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        if (_cultureStorage != null)
        {
            _cultureStorage.OnLanguageChanged -= CultureChanged;
        }
        GXNotificationRefresh.Remove(this);
        Interlocked.Increment(ref _generation);
        if (_viewStorage != null)
        {
            _viewStorage.ViewChanged -= OnViewChanged;
        }
    }

    /// <inheritdoc />
    public new void Dispose()
    {
        GC.SuppressFinalize(this);
        DetachViewStorage();
        base.Dispose();
    }

    /// <inheritdoc />
    public new async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        DetachViewStorage();
        await base.DisposeAsync();
    }
}
