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

using System.Globalization;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.Enums;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Validates the existing attribute enum contract for client editors and server persistence.</summary>
public static class GXAttributeEnumValues
{
    /// <summary>Identifies numeric and bit-string value attributes that support named values.</summary>
    /// <param name="template">The attribute supplying its index and wire type.</param>
    /// <param name="objectType">The owning class, when not populated on the attribute template.</param>
    /// <returns>True for supported Data and register value attributes; false for all other metadata.</returns>
    public static bool CanEditValues(GXAttributeTemplate template, int? objectType = null)
    {
        var owner = (ObjectType)(objectType ?? template.ObjectTemplate?.ObjectType ?? 0);
        bool valueAttribute = owner switch
        {
            ObjectType.Data or ObjectType.Register or ObjectType.ExtendedRegister => template.Index == 2,
            ObjectType.DemandRegister => template.Index is 2 or 3,
            _ => false
        };
        return valueAttribute && (DataType)template.DataType is
            DataType.Int8 or DataType.UInt8 or DataType.Int16 or DataType.UInt16 or
            DataType.Int32 or DataType.UInt32 or DataType.Int64 or DataType.UInt64 or
            DataType.DeltaInt8 or DataType.DeltaUInt8 or DataType.DeltaInt16 or DataType.DeltaUInt16 or
            DataType.DeltaInt32 or DataType.DeltaUInt32 or DataType.Float32 or DataType.Float64 or
            DataType.Enum or DataType.Bcd or DataType.BitString;
    }

    /// <summary>Determines whether enum rows describe fields in a bit string.</summary>
    /// <param name="template">The attribute whose wire and display types are inspected.</param>
    /// <returns>True when either type is BitString.</returns>
    public static bool IsBitString(GXAttributeTemplate template) =>
        template.DataType == (int)DataType.BitString || template.UIDataType == (int)DataType.BitString;

    /// <summary>Returns a localized enum editor label or validation message from the existing resource collection.</summary>
    /// <param name="key">The resource key in the client's Properties resource file.</param>
    /// <returns>The resource text in the current UI culture.</returns>
    public static string Text(string key) => Properties.Resources.ResourceManager.GetString(key, CultureInfo.CurrentUICulture)!;

    /// <summary>Validates labels, duplicate values, primitive ranges and left-to-right bit-field coordinates.</summary>
    /// <param name="template">The attribute and its proposed enum rows.</param>
    /// <returns>A localized validation error, or null when the rows can be saved.</returns>
    public static string? GetError(GXAttributeTemplate template)
    {
        var values = new HashSet<(int Shift, int Width, int Value)>();
        var ids = new HashSet<Guid>();
        foreach (var item in template.ListItems ?? [])
        {
            if (item == null || string.IsNullOrWhiteSpace(item.UIValue))
            {
                return Text("AttributeEnumLabelRequired");
            }
            if (item.Id != Guid.Empty && !ids.Add(item.Id))
            {
                return Text("AttributeEnumDuplicateValue");
            }
            bool bitString = IsBitString(template);
            if (bitString && (item.Shift < 0 || item.MaskSize < 1 ||
                (long)item.Shift + item.MaskSize > int.MaxValue || item.Value < 0 ||
                (item.MaskSize < 31 && item.Value >= (1L << item.MaskSize))))
            {
                return Text("AttributeEnumInvalidBitField");
            }
            if (!bitString && !FitsPrimitive(template, item.Value))
            {
                return Text("AttributeEnumValueOutOfRange");
            }
            if (!values.Add((bitString ? item.Shift : 0, bitString ? item.MaskSize : 0, item.Value)))
            {
                return Text("AttributeEnumDuplicateValue");
            }
        }
        return null;
    }

    /// <summary>Checks values against the wire type while preserving the shared contract's signed 32-bit representation.</summary>
    /// <param name="template">The attribute supplying the wire data type.</param>
    /// <param name="value">The proposed enum value.</param>
    /// <returns>True when the value is representable by the selected primitive type.</returns>
    private static bool FitsPrimitive(GXAttributeTemplate template, int value) => (DataType)template.DataType switch
    {
        DataType.Boolean => value is 0 or 1,
        DataType.Int8 or DataType.DeltaInt8 => value is >= sbyte.MinValue and <= sbyte.MaxValue,
        DataType.UInt8 or DataType.Enum or DataType.DeltaUInt8 => value is >= byte.MinValue and <= byte.MaxValue,
        DataType.Int16 or DataType.DeltaInt16 => value is >= short.MinValue and <= short.MaxValue,
        DataType.UInt16 or DataType.DeltaUInt16 => value is >= ushort.MinValue and <= ushort.MaxValue,
        DataType.UInt32 or DataType.UInt64 or DataType.DeltaUInt32 => value >= 0,
        _ => true
    };
}
