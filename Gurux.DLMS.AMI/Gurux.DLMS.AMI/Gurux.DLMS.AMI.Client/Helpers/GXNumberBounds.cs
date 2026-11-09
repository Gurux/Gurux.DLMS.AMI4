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

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Supplies type bounds when a numeric control would otherwise default to zero.</summary>
internal static class GXNumberBounds
{
    public static T? Minimum<T>(T value) => Bound<T>(minimum: true);
    public static T? Maximum<T>(T value) => Bound<T>(minimum: false);

    private static T? Bound<T>(bool minimum)
    {
        // Nullable inputs already represent an unspecified bound with null.
        if (Nullable.GetUnderlyingType(typeof(T)) != null) {
            return default;
        }
        object bound = Type.GetTypeCode(typeof(T)) switch
        {
            TypeCode.Byte => minimum ? byte.MinValue : byte.MaxValue,
            TypeCode.SByte => minimum ? sbyte.MinValue : sbyte.MaxValue,
            TypeCode.Int16 => minimum ? short.MinValue : short.MaxValue,
            TypeCode.UInt16 => minimum ? ushort.MinValue : ushort.MaxValue,
            TypeCode.Int32 => minimum ? int.MinValue : int.MaxValue,
            TypeCode.UInt32 => minimum ? uint.MinValue : uint.MaxValue,
            TypeCode.Int64 => minimum ? long.MinValue : long.MaxValue,
            TypeCode.UInt64 => minimum ? ulong.MinValue : ulong.MaxValue,
            TypeCode.Single => minimum ? float.MinValue : float.MaxValue,
            TypeCode.Double => minimum ? double.MinValue : double.MaxValue,
            TypeCode.Decimal => minimum ? decimal.MinValue : decimal.MaxValue,
            _ => throw new NotSupportedException($"Unsupported numeric type: {typeof(T).Name}.")
        };
        return (T)bound;
    }
}
