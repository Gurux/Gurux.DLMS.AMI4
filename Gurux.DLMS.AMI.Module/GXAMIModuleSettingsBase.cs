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

using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace Gurux.DLMS.AMI.Module
{
    /// <summary>
    /// Gurux DLMS AMI module settings base class.
    /// </summary>
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

        private bool _initialized = false;
        private bool _saved = false;
        /// <inheritdoc/>
        public virtual void Cancel()
        {
        }
        /// <inheritdoc/>
        public virtual Task CancelAsync()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual void Initialize(string? settings)
        {
            _initialized = true;
            var s = GetType().GetProperty("Settings");
            if (!string.IsNullOrEmpty(settings) && s != null && s.CanRead && s.CanWrite)
            {
                Settings = JsonSerializer.Deserialize<TSettings>(settings)!;
                if (Settings == null)
                {
                    Settings = new TSettings();
                }
            }
            Initialize();
        }

        /// <inheritdoc/>
        public Task InitializeAsync(string? settings)
        {
            if (!_initialized)
            {
                _initialized = true;
                var s = GetType().GetProperty("Settings");
                if (!string.IsNullOrEmpty(settings) && s != null && s.CanRead && s.CanWrite)
                {
                    Settings = JsonSerializer.Deserialize<TSettings>(settings)!;
                    if (Settings == null)
                    {
                        Settings = new TSettings();
                    }
                }
                return InitializeAsync();
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual void Initialize()
        {
        }

        /// <inheritdoc/>
        public virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public virtual bool Save()
        {
            return false;
        }

        /// <inheritdoc/>
        Task<string?> IAmiModuleSettings.SaveAsync()
        {
            if (!_saved)
            {
                _saved = true;
                if (SaveAsync().Result == true)
                {
                    var s = GetType().GetProperty("Settings");
                    if (s != null && s.CanRead && s.CanWrite)
                    {
                        return Task.FromResult<string?>(JsonSerializer.Serialize(Settings));
                    }
                }
                else
                {
                    throw new InvalidOperationException("Save operation failed.");
                }
            }
            return Task.FromResult<string?>(null);
        }
        /// <inheritdoc/>
        public virtual Task<bool> SaveAsync()
        {
            return Task.FromResult(false);
        }

        /// <summary>
        /// Configuration settings.
        /// </summary>
        public TSettings Settings
        {
            get;
            set;
        } = default!;
    }
}
