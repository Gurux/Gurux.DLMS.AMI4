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

using Gurux.DLMS.AMI.Components.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Gurux.DLMS.AMI.Components
{
    /// <summary>
    /// This component is used to time span.
    /// </summary>
    public class GXInputTimeSpan<TValue> : InputBase<TValue>
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
        /// Value is get as a milliseconds.
        /// </summary>
        [Parameter]
        public TimeSpanUnit TimeSpanUnit { get; set; } = TimeSpanUnit.Second;

        /// <summary>
        /// Element.
        /// </summary>
        [DisallowNull]
        public ElementReference? Element { get; protected set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXInputTimeSpan()
        {
            Type type = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
            if (type != typeof(TimeSpan) && type != typeof(int) && type != typeof(UInt32) && type != typeof(TimeOnly))
            {
                throw new InvalidOperationException($"Unsupported {GetType()} type param '{type}'.");
            }
        }
        /// <summary>
        /// Update default cookie value.
        /// </summary>
        protected override async Task OnInitializedAsync()
        {
            try
            {
                if (UseCookie && !string.IsNullOrEmpty(Id) && cookieStorage != null)
                {
                    TValue? result;
                    string? value = await cookieStorage.GetValueAsync(Id);
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (BindConverter.TryConvertTo<TValue>(value, CultureInfo.InvariantCulture, out result))
                        {
                            Value = result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex.Message);
            }
            await base.OnInitializedAsync();
        }

        private int Step
        {
            get
            {
                int value = 60;
                switch (TimeSpanUnit)
                {
                    case TimeSpanUnit.Second:
                        value = 1;
                        break;
                    case TimeSpanUnit.Minute:
                        value = 60;
                        break;
                    case TimeSpanUnit.Hour:
                        value = 3600;
                        break;
                    default:
                        break;
                }
                return value;
            }
        }

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "input");
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "type", "time");
            if (!string.IsNullOrEmpty(NameAttributeValue))
            {
                builder.AddAttribute(3, "name", NameAttributeValue);
            }
            if (!string.IsNullOrEmpty(CssClass))
            {
                builder.AddAttribute(4, "class", CssClass);
            }
            builder.AddAttribute(5, "value", BindConverter.FormatValue(CurrentValueAsString));
            builder.AddAttribute(6, "onchange", EventCallback.Factory.CreateBinder<string?>(this, __value => CurrentValueAsString = __value, CurrentValueAsString));
            // builder.AddAttribute(5, "value", CurrentValueAsString); Mikko
            // builder.AddAttribute(6, "onchange", EventCallback.Factory.CreateBinder<string?>(this, __value => CurrentValueAsString = __value, CurrentValueAsString));
            builder.AddAttribute(7, "step", Step.ToString());
            builder.SetUpdatesAttributeName("value");
            builder.AddElementReferenceCapture(8, __inputReference => Element = __inputReference);
            builder.CloseElement();
        }

        /// <inheritdoc />
        protected override string? FormatValueAsString(TValue? value)
        {
            if (value == null)
            {
                return string.Empty;
            }
            Type type = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
            if (value is int @int)
            {
                string result;
                switch (TimeSpanUnit)
                {
                    case TimeSpanUnit.Second:
                        result = TimeSpan.FromSeconds(@int).ToString();
                        break;
                    case TimeSpanUnit.Minute:
                        result = TimeSpan.FromMinutes(@int).ToString("hh\\:mm");
                        break;
                    case TimeSpanUnit.Hour:
                        result = TimeSpan.FromHours(@int).ToString();
                        break;
                    default:
                        result = "";
                        break;
                }
                return result;
            }
            if (value is UInt32 @uInt32)
            {
                string result;
                switch (TimeSpanUnit)
                {
                    case TimeSpanUnit.Second:
                        result = TimeSpan.FromSeconds(@uInt32).ToString();
                        break;
                    case TimeSpanUnit.Minute:
                        result = TimeSpan.FromMinutes(@uInt32).ToString("hh\\:mm");
                        break;
                    case TimeSpanUnit.Hour:
                        result = TimeSpan.FromHours(@uInt32).ToString();
                        break;
                    default:
                        result = "";
                        break;
                }
                return result;
            }
            return base.FormatValueAsString(value);
        }

        /// <inheritdoc />
        protected override bool TryParseValueFromString(string? value,
            [MaybeNullWhen(false)] out TValue result,
            [NotNullWhen(false)] out string? validationErrorMessage)
        {
            validationErrorMessage = "";
            if (value != null)
            {
                Type type = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
                if (type == typeof(int) || type == typeof(UInt32))
                {
                    try
                    {
                        int tmp = (int)TimeSpan.Parse(value).TotalSeconds;
                        if (!BindConverter.TryConvertTo(tmp.ToString(CultureInfo.InvariantCulture),
                            CultureInfo.InvariantCulture, out result))
                        {
                            validationErrorMessage = "Invalid time span.";
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        result = default;
                        validationErrorMessage = ex.Message;
                        return false;
                    }
                }
                else if (!BindConverter.TryConvertTo<TValue>(value, CultureInfo.InvariantCulture, out result))
                {
                    validationErrorMessage = "Invalid time span.";
                    return false;
                }
                bool change = Value?.GetHashCode() != result?.GetHashCode();
                if (change)
                {
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
                        return false;
                    }
                }
            }
            else
            {
                result = default;
                return false;
            }
            return true;
        }
    }
}
