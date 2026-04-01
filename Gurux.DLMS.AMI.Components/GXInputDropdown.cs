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
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Gurux.DLMS.AMI.Components
{
    /// <summary>
    /// This component is used to shown enumerated items.
    /// </summary>
    public class GXInputDropdown<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue> : InputBase<TValue>
    {
        /// <summary>
        /// Cancellation token.
        /// </summary>
        protected CancellationTokenSource? _cts;
        /// <summary>
        /// Filter.
        /// </summary>
        protected string? Filter;

        /// <summary>
        /// Gets or sets the child content to be rendering inside the select element.
        /// </summary>
        [Parameter] public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the <c>select</c> <see cref="ElementReference"/>.
        /// <para>
        /// May be <see langword="null"/> if accessed before the component is rendered.
        /// </para>
        /// </summary>
        [DisallowNull] public ElementReference? Element { get; protected set; }

        /// <summary>
        /// Format event handler is used to convert class to string.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public delegate string? FormatEventHandler(TValue value);

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
        public bool LastValue { get; set; } = true;

        [Inject]
        IGXLocalStorage? localStorage { get; set; }

        [Inject]
        ILogger<GXInputDropdown<TValue>>? Logger { get; set; }

        /// <summary>
        /// Notified when the selected item is changed.
        /// </summary>
        [Parameter]
        public EventCallback<TValue> OnSelected { get; set; }

        /// <summary>
        /// Available values.
        /// </summary>
        [Parameter]
        public IEnumerable<TValue>? Items
        {
            get;
            set;
        }

        /// <summary>
        /// Items in the list.
        /// </summary>
        protected readonly List<TValue> _items = new List<TValue>();


        /// <summary>
        /// Gets or sets the items provider.
        /// </summary>
        [Parameter]
        public GXItemsProviderDelegate<TValue>? ItemsProvider { get; set; }


        /// <summary>
        /// Update default cookie value.
        /// </summary>
        protected override async Task OnInitializedAsync()
        {
            try
            {
                if (ItemsProvider != null)
                {
                    await RefreshDataAsync(false);
                }
                //Get the default value from the cookies
                // if it's not set.
                if (localStorage != null && LastValue && !string.IsNullOrEmpty(Id))
                {
                    string? value = await localStorage.GetValueAsync(Id);
                    if (!string.IsNullOrEmpty(value))
                    {
                        Logger?.LogWarning("Default value for {0} is loaded from local storage '{1}'.", Id, value);
                        if (TryParseValueFromString(value, out TValue? value2, out var _))
                        {
                            Value = value2;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex.Message);
            }
        }

        /// <summary>
        /// Read values async.
        /// </summary>
        /// <param name="renderOnSuccess">Is UI render after success operation.</param>
        public async Task RefreshDataAsync(bool renderOnSuccess = true)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            CancellationToken cancellationToken = _cts.Token;
            GXItemsProviderRequest req = new GXItemsProviderRequest(0, 0,
            true,
            false,
            null,
            false,
            Filter,
            cancellationToken);
            if (ItemsProvider == null)
            {
                throw new Exception("ItemsProvider not set.");
            }
            var result = await ItemsProvider(req);
            _items.Clear();
            if (result.Items != null)
            {
                _items.AddRange(result.Items);
            }
            if ((Value == null || !_items.Contains(Value)) && _items.Any())
            {
                Value = _items.First();
            }
            // Only apply result if the task was not canceled.
            if (!cancellationToken.IsCancellationRequested && renderOnSuccess)
            {
                StateHasChanged();
            }
        }
        private readonly bool _isMultipleSelect;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXInputDropdown()
        {
            _isMultipleSelect = typeof(TValue).IsArray;
            var enumType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
            if (enumType.IsEnum)
            {
                foreach (TValue it in Enum.GetValues(enumType))
                {
                    _items.Add(it);
                }
            }
        }

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (!_items.Any() && Items?.Any() == true)
            {
                _items.AddRange(Items);
            }
            if (Value == null && Items?.Any() == true)
            {
                Value = Items.FirstOrDefault();
                if (ValueChanged.HasDelegate)
                {
                    ValueChanged.InvokeAsync(Value);
                }
            }
            builder.OpenElement(0, "select");
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            if (!string.IsNullOrEmpty(NameAttributeValue))
            {
                builder.AddAttribute(2, "name", NameAttributeValue);
            }
            if (!string.IsNullOrEmpty(CssClass))
            {
                builder.AddAttribute(3, "class", CssClass);
            }
            builder.AddAttribute(4, "multiple", _isMultipleSelect);
            if (_isMultipleSelect)
            {
                builder.AddAttribute(5, "value", BindConverter.FormatValue(CurrentValue)?.ToString());
                builder.AddAttribute(6, "onchange", EventCallback.Factory.CreateBinder<string?[]?>(this, SetCurrentValueAsStringArray, default));
            }
            else
            {
                builder.AddAttribute(7, "value", _items.IndexOf(Value));
                builder.AddAttribute(8, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(
                this, async e => await SetCurrentValueAsStringAsync(e.Value)));
            }
            builder.AddAttribute(9, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, OnKeyDown));
            builder.SetUpdatesAttributeName("value");
            builder.AddElementReferenceCapture(10, __selectReference => Element = __selectReference);
            builder.AddContent(11, ChildContent);

            int index = 12;
            int pos = 0;
            foreach (var value in _items)
            {
                builder.OpenElement(index++, "option");
                if (Template != null)
                {
                    builder.AddAttribute(index++, "id", pos.ToString());
                    builder.AddAttribute(index++, "value", pos.ToString());
                    builder.AddContent(index++, Template(value));
                }
                else
                {
                    builder.AddAttribute(index++, "id", pos.ToString());
                    builder.AddAttribute(index++, "value", pos.ToString());
                    builder.AddContent(index++, value);
                }
                ++pos;
                builder.CloseElement();
            }
            builder.CloseElement();
        }
        private TValue? _original;

        /// <summary>
        /// Handle key down event.
        /// </summary>
        protected void OnKeyDown(KeyboardEventArgs e)
        {
            if (_original == null)
            {
                //Remember the selected item.
                _original = Value;
            }
            if (e.Key == "ArrowUp")
            {
                //Select the previos item.
                int index = _items.IndexOf(Value);
                if (index > 0 && index < _items.Count)
                {
                    Value = _items[index - 1];
                }
            }
            else if (e.Key == "ArrowDown")
            {
                //Select the next item.
                int index = _items.IndexOf(Value);
                if (index != -1 && index < _items.Count - 1)
                {
                    Value = _items[index + 1];
                }
            }
            else if (e.Key == "Enter")
            {
                _original = default!;
                if (ValueChanged.HasDelegate)
                {
                    ValueChanged.InvokeAsync(Value);
                }
                OnSelected.InvokeAsync(Value);
                if (localStorage != null && LastValue && !string.IsNullOrEmpty(Id))
                {
                    localStorage.SetValueAsync(Id, Value?.ToString());
                }
                Filter = null;
            }
            else if (e.Key == "Escape")
            {
                //Reset to original value.
                Filter = null;
                Value = _original;
                _original = default!;
                ValueChanged.InvokeAsync(Value);
                OnSelected.InvokeAsync(Value);
                StateHasChanged();
            }
        }

        protected void SetCurrentValueAsStringArray(string?[]? value)
        {
            _original = default!;
            CurrentValue = BindConverter.TryConvertTo<TValue>(value, CultureInfo.CurrentCulture, out var result)
                ? result
                : default;
        }

        protected void SetCurrentValueAsString(object? value)
        {
            _original = default!;
            int index = Convert.ToInt32(value);
            Value = _items[index];
            CurrentValue = Value;
            ValueChanged.InvokeAsync(Value);
            OnSelected.InvokeAsync(Value);
        }

        private async Task SetCurrentValueAsStringAsync(object? value)
        {
            SetCurrentValueAsString(value);
            if (localStorage != null && LastValue && !string.IsNullOrEmpty(Id))
            {
                if (Value != null)
                {
                    await localStorage.SetValueAsync(Id, Value.ToString());
                }
                else
                {
                    await localStorage.SetValueAsync(Id, string.Empty);
                }
            }
        }

        /// <inheritdoc />
        protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage)
        {
            validationErrorMessage = null;
            result = default;
            bool ret = false;
            if (!BindConverter.TryConvertTo<TValue>(value, CultureInfo.InvariantCulture, out result))
            {
                throw new Exception("Invalid value.");
            }
            if (Value?.GetHashCode() != result?.GetHashCode())
            {
                Value = result;
                if (ValueChanged.HasDelegate)
                {
                    ValueChanged.InvokeAsync(result);
                }
                if (OnSelected.HasDelegate)
                {
                    OnSelected.InvokeAsync(result);
                }
                try
                {
                    if (localStorage != null && LastValue && !string.IsNullOrEmpty(Id))
                    {
                        localStorage.SetValueAsync(Id, result?.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex.Message);
                }
            }
            return ret;
        }

        /// <summary>
        /// Formatter is used to format class to string.
        /// </summary>
        [Parameter]
        public FormatEventHandler? Formatter { get; set; }

        /// <summary>
        /// Item template.
        /// </summary>
        [Parameter]
        public RenderFragment<TValue>? Template { get; set; }

    }
}
