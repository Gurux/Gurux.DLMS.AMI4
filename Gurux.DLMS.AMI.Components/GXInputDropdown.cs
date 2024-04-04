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
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Gurux.DLMS.AMI.Components
{
    /// <summary>
    /// This component is used to shown enumerated items.
    /// </summary>
    public class GXInputDropdown<TValue> : InputSelect<TValue>
    {
        /// <summary>
        /// ID is used to save cookie value.
        /// </summary>
        [Parameter]
        public string? Id
        {
            get;
            set;
        }

        /// <summary>
        /// Whether to load and store the value in a cookie.
        /// </summary>
        /// <seealso cref="Id"/>see
        [Parameter]
        public bool UseCookie { get; set; } = true;

        [Inject]
        IGXCookieStorage? cookieStorage { get; set; }

        [Inject]
        ILogger<GXInputNumber<TValue>>? Logger { get; set; }

        /// <summary>
        /// Notified when the selected item is changed.
        /// </summary>
        [Parameter]
        public EventCallback<TValue> OnSelected { get; set; }

        /// <summary>
        /// Update default cookie value.
        /// </summary>
        protected override async Task OnInitializedAsync()
        {
            try
            {
                //Get the default value from the cookies
                // if it's not set.
                if (cookieStorage != null && UseCookie && !string.IsNullOrEmpty(Id))
                {
                    string? value = await cookieStorage.GetValueAsync(Id);
                    if (!string.IsNullOrEmpty(value))
                    {
                        TValue? result;
                        string? validationErrorMessage;
                        if (base.TryParseValueFromString(value, out result, out validationErrorMessage))
                        {
                            if (Value?.GetHashCode() != result?.GetHashCode())
                            {
                                Value = result;
                                if (ValueChanged.HasDelegate)
                                {
                                    await ValueChanged.InvokeAsync(result);
                                }
                                if (OnSelected.HasDelegate)
                                {
                                    await OnSelected.InvokeAsync(result);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex.Message);
            }
        }

        /// <inheritdoc />
        protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage)
        {
            bool ret = base.TryParseValueFromString(value, out result, out validationErrorMessage);
            if (ret)
            {
                bool change = Value?.GetHashCode() != result?.GetHashCode();
                if (change)
                {
                    if (OnSelected.HasDelegate)
                    {
                        OnSelected.InvokeAsync(result);
                    }
                    try
                    {
                        if (cookieStorage != null && UseCookie && !string.IsNullOrEmpty(Id))
                        {
                            cookieStorage.SetValueAsync(Id, result?.ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger?.LogError(ex.Message);
                    }
                }
            }
            return ret;
        }
    }
}
