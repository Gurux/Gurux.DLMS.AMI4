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

using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace Gurux.DLMS.AMI.Module
{
    /// <summary>
    /// Provides a Blazor settings editor with JSON initialization, validation hooks, and serialized save results.
    /// </summary>
    /// <typeparam name="TSettings">Settings model with a public parameterless constructor.</typeparam>
    public abstract class GXAMIModuleSettingsBase<TSettings> : ComponentBase, IAmiModuleSettings
        where TSettings : class, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GXAMIModuleSettingsBase{TSettings}"/> class with default
        /// settings.
        /// </summary>
        /// <remarks>The <c>Settings</c> property is initialized to a new instance of <typeparamref
        /// name="TSettings"/> using its parameterless constructor.</remarks>
        public GXAMIModuleSettingsBase()
        {
            Settings = new TSettings();
        }

        /// <summary>Tracks whether serialized settings have already been initialized.</summary>
        private bool _initialized = false;
        /// <summary>Tracks whether the current settings have already been saved.</summary>
        private bool _saved = false;
        /// <summary>Discards changes when overridden; the default implementation does nothing.</summary>
        public virtual void Cancel()
        {
        }
        /// <summary>Discards changes asynchronously when overridden; the default implementation completes without changes.</summary>
        /// <returns>A task that completes when the operation has finished.</returns>
        public virtual Task CancelAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>Loads nonempty JSON settings and invokes the synchronous initialization hook.</summary>
        /// <param name="settings">JSON settings, or <see langword="null"/> or empty to retain the current settings model.</param>
        public virtual void Initialize(string? settings)
        {
            _initialized = true;
            var s = GetType().GetProperty("Settings");
            if (!string.IsNullOrEmpty(settings) && s != null && s.CanRead && s.CanWrite)
            {
                Settings = JsonSerializer.Deserialize<TSettings>(settings)!;
                Settings ??= new TSettings();
            }
            Initialize();
        }

        /// <summary>Loads nonempty JSON settings and invokes the asynchronous hook only if initialization has not already occurred.</summary>
        /// <param name="settings">JSON settings, or <see langword="null"/> or empty to retain the current settings model.</param>
        /// <returns>The initialization task, or a completed task when already initialized.</returns>
        public Task InitializeAsync(string? settings)
        {
            if (!_initialized)
            {
                _initialized = true;
                var s = GetType().GetProperty("Settings");
                if (!string.IsNullOrEmpty(settings) && s != null && s.CanRead && s.CanWrite)
                {
                    Settings = JsonSerializer.Deserialize<TSettings>(settings)!;
                    Settings ??= new TSettings();
                }
                return InitializeAsync();
            }
            return Task.CompletedTask;
        }

        /// <summary>Runs custom initialization after loading settings; the default implementation does nothing.</summary>
        public virtual void Initialize()
        {
        }

        /// <summary>Runs custom asynchronous initialization after loading settings; the default implementation completes immediately.</summary>
        /// <returns>A task that completes when the operation has finished.</returns>
        public virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        /// <summary>Validates and serializes settings once, resetting the saved flag when validation or serialization fails.</summary>
        /// <returns>Serialized settings, or null when this instance has already saved its settings or exposes no readable settings property.</returns>
        string? IAmiModuleSettings.Save()
        {
            try
            {
                if (!_saved)
                {
                    _saved = true;
                    if (Save())
                    {
                        var s = GetType().GetProperty("Settings");
                        if (s != null && s.CanRead && s.CanWrite)
                        {
                            return JsonSerializer.Serialize(Settings);
                        }
                    }
                    else
                    {
                        throw new Exception("The module settings are invalid.");
                    }
                }
            }
            catch (Exception)
            {
                _saved = false;
                throw;
            }
            return null;
        }

        /// <summary>Validates or prepares settings before serialization; override to allow saving.</summary>
        /// <returns><see langword="true"/> when settings may be saved; the default is <see langword="false"/>.</returns>
        public virtual bool Save()
        {
            return false;
        }

        /// <inheritdoc/>
        /// <summary>Asynchronously validates and serializes settings once, resetting the saved flag when the operation fails.</summary>
        /// <returns>A task containing serialized settings, or null when this instance has already saved its settings or exposes no readable settings property.</returns>
        async Task<string?> IAmiModuleSettings.SaveAsync()
        {
            if (!_saved)
            {
                _saved = true;
                try
                {
                    if (await SaveAsync())
                    {
                        var s = GetType().GetProperty("Settings");
                        if (s != null && s.CanRead && s.CanWrite)
                        {
                            return JsonSerializer.Serialize(Settings);
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Save operation failed.");
                    }
                }
                catch
                {
                    _saved = false;
                    throw;
                }
            }
            return null;
        }
        /// <summary>Validates or prepares settings asynchronously by invoking <see cref="Save()"/>.</summary>
        /// <returns>Whether the current settings may be serialized for saving.</returns>
        public virtual Task<bool> SaveAsync()
        {
            return Task.FromResult(Save());
        }

        /// <summary>
        /// Gets or sets the settings model edited by the component and serialized when saving succeeds.
        /// </summary>
        public TSettings Settings { get; set; } = default!;
    }
}
