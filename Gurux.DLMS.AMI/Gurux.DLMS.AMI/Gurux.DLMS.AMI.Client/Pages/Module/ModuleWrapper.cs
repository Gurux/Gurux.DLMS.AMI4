//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using Gurux.DLMS.AMI.Module;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Gurux.DLMS.AMI.Client.Module
{
    public sealed class ModuleWrapper : ComponentBase
    {
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
            builder.OpenComponent(0, ComponentType);

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
        public async Task NotifyInitializedAsync()
        {
            if (_instance is IAmiModuleSettings n)
            {
                n.Initialize(Settings);
                await n.InitializeAsync(Settings);
            }
        }

        public async Task NotifySavedAsync()
        {
            if (_instance is IAmiModuleSettings n)
            {
                Settings = n.Save();
                if (string.IsNullOrEmpty(Settings))
                {
                    Settings = await n.SaveAsync();
                }
            }
            else
            {
                Settings = null;
            }
            await SettingsChanged.InvokeAsync(Settings);
        }

        public async Task NotifyCancelAsync()
        {
            if (_instance is IAmiModuleSettings n)
            {
                n.Cancel();
                await n.CancelAsync();
            }
        }
    }
}