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

using Gurux.DLMS.AMI.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Collections;
using System.Globalization;

namespace Gurux.DLMS.AMI.Client.Helpers
{
    public class GXInputEnumSelector<TValue> : GXInputDropdown<TValue>
    {
        private readonly bool _isMultipleSelect;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXInputEnumSelector()
        {
            _isMultipleSelect = typeof(TValue).IsArray;
        }

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
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
                builder.SetUpdatesAttributeName("value");
            }
            else
            {
                builder.AddAttribute(7, "value", CurrentValueAsString);
                builder.AddAttribute(8, "onchange", EventCallback.Factory.CreateBinder<string?>(this, __value => CurrentValueAsString = __value, default));
                builder.SetUpdatesAttributeName("value");
            }

            builder.AddElementReferenceCapture(9, __selectReference => Element = __selectReference);
            builder.AddContent(10, ChildContent);
            int index = 11;
            IEnumerable? tmp;
            if (Values != null && Values.Any())
            {
                tmp = Values;
            }
            else
            {
                var enumType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
                if (!enumType.IsEnum)
                {
                    throw new ArgumentException("Value is not enumerated type.");
                }
                tmp = Enum.GetValues(enumType);
            }
            foreach (var value in tmp)
            {
                builder.OpenElement(index++, "option");
                builder.AddAttribute(index++, "value", value.ToString());
                builder.AddContent(index++, value.ToString());
                builder.CloseElement();
            }
            builder.CloseElement();
        }

        private void SetCurrentValueAsStringArray(string?[]? value)
        {
            CurrentValue = BindConverter.TryConvertTo<TValue>(value, CultureInfo.CurrentCulture, out var result)
                ? result
                : default;
        }

        /// <summary>
        /// Available enumerated values.
        /// </summary>
        [Parameter]
        public IEnumerable<TValue>? Values
        {
            get;
            set;
        }
    }
}
