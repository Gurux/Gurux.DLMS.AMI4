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
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Gurux.DLMS.AMI.Components
{
    /// <summary>
    /// A search component.
    /// </summary>
    public class Search2 : ComponentBase
    {
        /// <summary>
        /// Value to search for.
        /// </summary>
        [Parameter]
        public string? Value
        {
            get;
            set;
        }

        /// <summary>
        /// Notified when the selected value is changed.
        /// </summary>
        [Parameter]
        public EventCallback<string?> ValueChanged
        {
            get;
            set;
        }

        /// <summary>
        /// Notified when the selected value is changed.
        /// </summary>
        [Parameter]
        public EventCallback<string?> OnValueChanged
        {
            get;
            set;
        }

        /// <summary>
        /// Whether to load and store the value used value in a local storage.
        /// </summary>
        [Parameter]
        public bool LastValue { get; set; } = true;


        [Inject]
        IGXLocalStorage? localStorage { get; set; }

        [Inject]
        ILogger<Search2>? Logger { get; set; }

        /// <summary>
        /// If immediate filtering is used, the query is executed when the user presses any key.
        /// If immediate is false, the query is executed when the user presses enter.
        /// </summary>
        [Parameter]
        public bool Immediate { get; set; }

        /// <summary>
        /// Gets or sets a collection of additional attributes that will be applied to the created element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

        /// <summary>
        /// Gets the value to be used for the input's "name" attribute.
        /// </summary>
        protected string NameAttributeValue
        {
            get
            {
                if (AdditionalAttributes?.TryGetValue("name", out var nameAttributeValue) ?? false)
                {
                    return Convert.ToString(nameAttributeValue, CultureInfo.InvariantCulture) ?? string.Empty;
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets a CSS class string that combines the <c>class</c> attribute and a string indicating
        /// the status of the field being edited (a combination of "modified", "valid", and "invalid").
        /// Derived components should typically use this value for the primary HTML element's 'class' attribute.
        /// </summary>
        protected string CssClass
        {
            get
            {
                if (AdditionalAttributes?.TryGetValue("class", out var nameAttributeValue) ?? false)
                {
                    return Convert.ToString(nameAttributeValue, CultureInfo.InvariantCulture) ?? string.Empty;
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets a CSS class string that combines the <c>class</c> attribute and a string indicating
        /// the status of the field being edited (a combination of "modified", "valid", and "invalid").
        /// Derived components should typically use this value for the primary HTML element's 'class' attribute.
        /// </summary>
        protected string Id
        {
            get
            {
                if (AdditionalAttributes?.TryGetValue("id", out var nameAttributeValue) ?? false)
                {
                    return Convert.ToString(nameAttributeValue, CultureInfo.InvariantCulture) ?? string.Empty;
                }
                return string.Empty;
            }
        }

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var seq = 0;
            builder.OpenElement(seq++, "input");
            builder.AddAttribute(seq++, "placeholder", Properties.Resources.Search);
            builder.AddAttribute(seq++, "type", "search");
            builder.AddMultipleAttributes(seq++, AdditionalAttributes);
            if (!string.IsNullOrEmpty(NameAttributeValue))
            {
                builder.AddAttribute(seq++, "name", NameAttributeValue);
            }
            if (!string.IsNullOrEmpty(CssClass))
            {
                builder.AddAttribute(seq++, "class", CssClass);
            }
            builder.AddAttribute(seq++, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, OnInputChanged));
            builder.AddAttribute(seq++, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, OnKeydown));
            builder.AddAttribute(seq++, "value", Value);
            builder.CloseElement();
        }

        private async Task OnInputChanged(ChangeEventArgs e)
        {
            Value = e.Value?.ToString();
            if (Immediate)
            {
                await UpdateValue();
            }
        }

        private async Task OnKeydown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                await UpdateValue();
            }
        }

        private async Task UpdateValue()
        {
            try
            {
                //Update the new value.
                await ValueChanged.InvokeAsync(Value);
                //Notify that value has been changed.
                await OnValueChanged.InvokeAsync(Value);
                if (localStorage != null && LastValue && !string.IsNullOrEmpty(Id))
                {
                    await localStorage.SetValueAsync(Id, Value);
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex.Message);
            }
        }

        /// <summary>
        /// Update default cookie value.
        /// </summary>
        protected override async Task OnInitializedAsync()
        {
            try
            {
                //Get the default value from the cookies
                // if it's not set.
                if (localStorage != null && LastValue && !string.IsNullOrEmpty(Id))
                {
                    string? value = await localStorage.GetValueAsync(Id);
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (Value != value)
                        {
                            Value = value;
                            if (ValueChanged.HasDelegate)
                            {
                                await ValueChanged.InvokeAsync(value);
                            }
                            if (OnValueChanged.HasDelegate)
                            {
                                await OnValueChanged.InvokeAsync(value);
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
    }
}