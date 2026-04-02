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
namespace Gurux.DLMS.AMI.Shared.DTOs.Enums
{
    /// <summary>
    /// The field type of the content type.
    /// </summary>
    public enum FieldType : byte
    {
        /// <summary>
        /// Field is plain text in single row.
        /// </summary>
        Text,
        /// <summary>
        /// Html.
        /// </summary>
        Html,
        /// <summary>
        /// Field is number.
        /// </summary>
        Number,
        /// <summary>
        /// Field is auto increament.
        /// </summary>
        AutoIncrement,
        /// <summary>
        /// Field is file.
        /// </summary>
        File,
        /// <summary>
        /// The field is single selection list.
        /// </summary>
        SelectionList,
        /// <summary>
        /// The field is multiple selection list.
        /// </summary>
        MultipleSelectionList,
        /// <summary>
        /// The field is a single selection dropdown list from the targets.
        /// </summary>
        DropdownList,
        /// <summary>
        /// The field is multiple selection dropdown list.
        /// </summary>
        MultipleDropdownList,
        /// <summary>
        /// The field search from targets.
        /// </summary>
        Search,
        /// <summary>
        /// Field is date and time value.
        /// </summary>
        DateTime,
        /// <summary>
        /// Field is datevalue.
        /// </summary>
        Date,
        /// <summary>
        /// Field is time value.
        /// </summary>
        Time,
        /// <summary>
        /// Field is timespan value.
        /// </summary>
        TimeSpan,
        /// <summary>
        /// Field is boolean value.
        /// </summary>
        Boolean,
        /// <summary>
        /// Field is comment field.
        /// </summary>
        Comment,
        /// <summary>
        /// Field is email field.
        /// </summary>
        Email,
        /// <summary>
        /// Field is phone number field.
        /// </summary>
        Phone,
        /// <summary>
        /// Field is link field.
        /// </summary>
        Link,
        /// <summary>
        /// Field is link button.
        /// </summary>
        LinkButton,
        /// <summary>
        /// Field is hex field.
        /// </summary>
        Hex,
        /// <summary>
        /// Field is code field.
        /// </summary>
        Code,
        /// <summary>
        /// Field is slider field.
        /// </summary>
        Slider,
        /// <summary>
        /// Field is text area.
        /// </summary>
        TextArea,
        /// <summary>
        /// Field is image field.
        /// </summary>
        Image,
    }
}