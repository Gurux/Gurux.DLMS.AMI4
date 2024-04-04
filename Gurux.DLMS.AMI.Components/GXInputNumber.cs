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
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Gurux.DLMS.AMI.Components
{
    /// <summary>
    /// This component is used to shown numbers.
    /// </summary>
    public class GXInputNumber<TValue> : InputBase<TValue>
    {
        private TValue? _min;
        private TValue? _max;

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

        /// <summary>
        /// Minimum value.
        /// </summary>
        [Parameter]
        public TValue? Min
        {
            get
            {
                return _min;
            }
            set
            {
                _min = value;
            }
        }

        /// <summary>
        /// Maximum value.
        /// </summary>
        [Parameter]
        public TValue? Max
        {
            get
            {
                return _max;
            }
            set
            {
                _max = value;
            }
        }

        /// <summary>
        /// Step.
        /// </summary>
        [Parameter]
        public int Step { get; set; } = 1;

        [Inject]
        IGXCookieStorage? cookieStorage { get; set; }

        [Inject]
        ILogger<GXInputNumber<TValue>>? Logger { get; set; }

        /// <summary>
        /// Parsing error message.
        /// </summary>
        [Parameter]
        public string ParsingErrorMessage { get; set; } = "The {0} field must be a number.";

        /// <summary>
        /// Element.
        /// </summary>
        [DisallowNull]
        public ElementReference? Element { get; protected set; }

        /// <summary>
        /// Update default cookie value and min and max values.
        /// </summary>
        protected override async Task OnInitializedAsync()
        {
            //Initialize min and max values if not set.
            if (typeof(TValue) == typeof(byte))
            {
                if (Min == null)
                {
                    BindConverter.TryConvertTo<TValue>(byte.MinValue, CultureInfo.InvariantCulture, out _min);
                }
                if (Max == null)
                {
                    BindConverter.TryConvertTo<TValue>(byte.MaxValue, CultureInfo.InvariantCulture, out _max);
                }
            }
            else if (typeof(TValue) == typeof(UInt16))
            {
                if (Min == null)
                {
                    BindConverter.TryConvertTo<TValue>(UInt16.MinValue, CultureInfo.InvariantCulture, out _min);
                }
                if (Max == null)
                {
                    BindConverter.TryConvertTo<TValue>(UInt16.MaxValue, CultureInfo.InvariantCulture, out _max);
                }
            }
            else if (typeof(TValue) == typeof(UInt32))
            {
                if (Min == null)
                {
                    BindConverter.TryConvertTo<TValue>(UInt32.MinValue, CultureInfo.InvariantCulture, out _min);
                }
                if (Max == null)
                {
                    BindConverter.TryConvertTo<TValue>(UInt32.MaxValue, CultureInfo.InvariantCulture, out _max);
                }
            }
            else if (typeof(TValue) == typeof(UInt64))
            {
                if (Min == null)
                {
                    BindConverter.TryConvertTo<TValue>(UInt64.MinValue, CultureInfo.InvariantCulture, out _min);
                }
                if (Max == null)
                {
                    BindConverter.TryConvertTo<TValue>(UInt64.MaxValue, CultureInfo.InvariantCulture, out _max);
                }
            }
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

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "input");
            builder.AddAttribute(1, "step", Step == 0 ? "any" : Step);
            builder.AddMultipleAttributes(2, AdditionalAttributes);
            builder.AddAttribute(3, "type", "number");
            if (!string.IsNullOrEmpty(NameAttributeValue))
            {
                builder.AddAttribute(4, "name", NameAttributeValue);
            }
            if (!string.IsNullOrEmpty(CssClass))
            {
                builder.AddAttribute(5, "class", CssClass);
            }
            builder.AddAttribute(6, "value", CurrentValueAsString);
            builder.AddAttribute(7, "onchange", EventCallback.Factory.CreateBinder<string?>(this, __value => CurrentValueAsString = __value, CurrentValueAsString));
            builder.SetUpdatesAttributeName("value");
            builder.AddElementReferenceCapture(8, __inputReference => Element = __inputReference);
            if (Min != null)
            {
                builder.AddAttribute(5, "min", Min);
            }
            if (Max != null)
            {
                builder.AddAttribute(5, "max", Max);
            }
            builder.CloseElement();
        }

        /// <inheritdoc />
        protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage)
        {
            if (BindConverter.TryConvertTo<TValue>(value, CultureInfo.InvariantCulture, out result))
            {
                validationErrorMessage = null;
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
                    }
                }
                return true;
            }
            else
            {
                validationErrorMessage = string.Format(CultureInfo.InvariantCulture, ParsingErrorMessage, DisplayName ?? FieldIdentifier.FieldName);
                return false;
            }
        }

        /// <summary>
        /// Formats the value as a string. Derived classes can override this to determine the formatting used for <c>CurrentValueAsString</c>.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <returns>A string representation of the value.</returns>
        protected override string? FormatValueAsString(TValue? value)
        {
            // Avoiding a cast to IFormattable to avoid boxing.
            switch (value)
            {
                case null:
                    return null;

                case int @int:
                    return BindConverter.FormatValue(@int, CultureInfo.InvariantCulture);

                case long @long:
                    return BindConverter.FormatValue(@long, CultureInfo.InvariantCulture);

                case short @short:
                    return BindConverter.FormatValue(@short, CultureInfo.InvariantCulture);

                case float @float:
                    return BindConverter.FormatValue(@float, CultureInfo.InvariantCulture);

                case double @double:
                    return BindConverter.FormatValue(@double, CultureInfo.InvariantCulture);

                case decimal @decimal:
                    return BindConverter.FormatValue(@decimal, CultureInfo.InvariantCulture);
                case byte @byte:
                    return Convert.ToString(BindConverter.FormatValue(@byte, CultureInfo.InvariantCulture));
                case UInt16 @uInt16:
                    return Convert.ToString(BindConverter.FormatValue(@uInt16, CultureInfo.InvariantCulture));
                case UInt32 @uInt32:
                    return Convert.ToString(BindConverter.FormatValue(@uInt32, CultureInfo.InvariantCulture));
                case UInt64 @uInt64:
                    return Convert.ToString(BindConverter.FormatValue(@uInt64, CultureInfo.InvariantCulture));
                default:
                    throw new InvalidOperationException($"Unsupported type {value.GetType()}");
            }
        }
    }
}
