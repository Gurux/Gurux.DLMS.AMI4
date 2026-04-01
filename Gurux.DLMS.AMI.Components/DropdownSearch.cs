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
using System.Diagnostics.CodeAnalysis;

namespace Gurux.DLMS.AMI.Components
{
    /// <summary>
    /// A dropdown search component.
    /// </summary>
    public class DropdownSearch<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue>
        : GXInputDropdown<TValue>
    {
        /// <summary>
        /// If immediate filtering is used, the query is executed when the user presses any key.
        /// If immediate is false, the query is executed when the user presses enter.
        /// </summary>
        [Parameter]
        public bool Immediate { get; set; }

        [Inject]
        ILogger<DropdownSearch<TValue>>? Logger { get; set; }


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
            var seq = 0;
            builder.OpenElement(seq++, "div");
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

            if (string.IsNullOrEmpty(Filter))
            {
                if (Formatter != null && Value != null)
                {
                    builder.AddAttribute(seq++, "value", Formatter(Value));
                }
                else
                {
                    builder.AddAttribute(seq++, "value", Value);
                }
            }
            else
            {
                builder.AddAttribute(seq++, "value", Filter);
            }

            builder.SetUpdatesAttributeName("value");

            // Bind the input value to Filter so DOM and component state stay in sync.
            // Use a binder that also triggers the data refresh (fire-and-forget).
            builder.AddAttribute(seq++, "oninput", EventCallback.Factory.CreateBinder<string?>(this, v =>
            {
                Filter = v;
                _ = RefreshDataAsync(true);
            }, Filter));
            builder.AddAttribute(seq++, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, e => OnKeyDown(e)));

            // Capture the element reference for potential focus handling.
            builder.AddElementReferenceCapture(seq++, __inputRef => Element = __inputRef);

            builder.CloseElement();
            if (!string.IsNullOrEmpty(Filter))
            {
                if (_items.Any())
                {
                    builder.OpenElement(seq++, "div");
                    builder.AddAttribute(seq++, "class", "dropdown-menu show");
                    int pos = 0;
                    foreach (var item in _items)
                    {
                        builder.OpenElement(seq++, "a");
                        if (item?.Equals(Value) == true)
                        {
                            builder.AddAttribute(seq++, "class", "dropdown-item active");
                        }
                        else
                        {
                            builder.AddAttribute(seq++, "class", "dropdown-item");
                        }
                        builder.AddAttribute(seq++, "tabindex", "0");
                        builder.AddAttribute(seq++, "id", pos.ToString());
                        builder.AddAttribute(seq++, "value", pos.ToString());
                        builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create<MouseEventArgs?>(this, v => OnItemSelected(item)));
                        if (Template != null)
                        {
                            builder.AddContent(seq++, Template(item));
                        }
                        else
                        {
                            builder.AddContent(seq++, item);
                        }
                        builder.CloseElement(); // </dropdown-item>
                        ++pos;
                    }
                    builder.CloseElement(); // </dropdown-menu>
                }
            }
            builder.CloseElement(); // </dropdown>         
        }

        /// <summary>
        /// User has select the new item.
        /// </summary>
        /// <param name="e"></param>
        private void OnItemSelected(TValue? e)
        {
            Value = e;
            ValueChanged.InvokeAsync(e);
            OnSelected.InvokeAsync(e);
            Filter = null;
        }

        /// <summary>
        /// Refresh search values.
        /// </summary>
        /// <returns></returns>
        protected override async Task OnInitializedAsync()
        {
            if (ItemsProvider != null)
            {
                await RefreshDataAsync(false);
            }
            await base.OnInitializedAsync();
        }
    }
}