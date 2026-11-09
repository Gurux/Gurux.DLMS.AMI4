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

using Gurux.DLMS.AMI.Module;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Gurux.DLMS.AMI.Client.Layout
{
    public sealed class ModuleWrapper : ComponentBase
    {
        [Parameter]
        public string? ModuleId { get; set; }
        [Parameter] public object? Target { get; set; }
        [Parameter] public IDictionary<string, object>? Parameters { get; set; }
        [Inject] IServiceProvider Services { get; set; } = default!;
        [Parameter]
        public required Type ComponentType { get; set; }

        [Parameter]
        public required string? Settings { get; set; }

        /// <summary>
        /// Notified when the settings are changed.
        /// </summary>
        [Parameter]
        public required EventCallback<string?> SettingsChanged { get; set; }

        [Inject]
        ILogger<ModuleWrapper>? Logger { get; set; }

        private object? _instance;
        private bool _initializedNotified;
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (ComponentType != null)
            {
                builder.OpenComponent(0, ComponentType);
                var values = Parameters == null ? [] : new Dictionary<string, object>(Parameters);
                if (Target != null)
                {
                    values.TryAdd("Target", Target);
                    values.TryAdd("Active", Target);
                }
                builder.AddMultipleAttributes(1, Helpers.GXTabParameters.Filter(ComponentType, values));
                builder.AddComponentReferenceCapture(2, inst =>
                {
                    _instance = inst;
                    if (!_initializedNotified)
                    {
                        _initializedNotified = true;
                        _ = NotifyInitializedAsync();
                    }
                });
                builder.CloseComponent();
            }
        }

        public async Task NotifyInitializedAsync()
        {
            try
            {
                if (_instance is IGXAmiModuleSettings n)
                {
                    n.Initialize(Settings);
                    await n.InitializeAsync(Settings);
                }
            }
            catch (Exception error)
            {
                if (ModuleId != null)
                {
                    Services.GetService<Gurux.DLMS.AMI.Client.Helpers.ModuleLoader>()?.DisableClientModule(ModuleId, error);
                }

                await DispatchExceptionAsync(error);
            }
        }

        public async Task NotifySavedAsync()
        {
            if (_instance is IGXAmiModuleSettings n)
            {
                Settings = await n.SaveAsync();
            }
            else
            {
                Settings = null;
            }
            await SettingsChanged.InvokeAsync(Settings);
        }

        public async Task NotifyCancelAsync()
        {
            if (_instance is IGXAmiModuleSettings n)
            {
                n.Cancel();
                await n.CancelAsync();
            }
        }
    }
}
