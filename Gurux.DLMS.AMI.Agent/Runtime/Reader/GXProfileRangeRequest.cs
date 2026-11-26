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

using System.Buffers.Binary;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects;

namespace Gurux.DLMS.AMI.Agent.Worker;

/// <summary>Creates selective buffer reads using the actual captured Clock object's logical name.</summary>
internal static class GXProfileRangeRequest
{
    /// <summary>Bridges the pinned DLMS package's selective-read encoder until its public selector overload is available.</summary>
    private static readonly MethodInfo _selectiveRead = typeof(GXDLMSClient).GetMethod("Read",
        BindingFlags.Instance | BindingFlags.NonPublic, null,
        [typeof(object), typeof(ObjectType), typeof(int), typeof(GXByteBuffer), typeof(int)], null)
        ?? throw new MissingMethodException(typeof(GXDLMSClient).FullName, "Read");

    /// <summary>Builds range-read messages without assuming the standard clock OBIS code.</summary>
    /// <param name="client">The connected client supplying transport, ciphering, and date-time settings.</param>
    /// <param name="profile">The profile whose resolved sort object identifies the captured clock.</param>
    /// <param name="start">The beginning of the requested interval.</param>
    /// <param name="end">The end of the requested interval.</param>
    /// <returns>The request messages for buffer attribute 2 with range selector 1.</returns>
    /// <exception cref="InvalidOperationException">The resolved sort object is not a captured clock time attribute.</exception>
    internal static byte[][] Create(GXDLMSClient client, GXDLMSProfileGeneric profile, DateTime start, DateTime end)
    {
        var clock = profile.SortObject;
        if (clock?.ObjectType != ObjectType.Clock || !profile.CaptureObjects.Any(c =>
            c.Key.ObjectType == ObjectType.Clock && c.Key.LogicalName == clock.LogicalName &&
            c.Value.AttributeIndex == 2 && c.Value.DataIndex == 0))
        {
            throw new InvalidOperationException(Properties.Resources.ResourceManager.GetString("ProfileRangeClockRequired", System.Globalization.CultureInfo.CurrentUICulture));
        }
        profile.Buffer.Clear();
        var selection = new GXStructure
        {
            new GXStructure { (ushort)ObjectType.Clock, clock.LogicalName.Split('.').Select(byte.Parse).ToArray(), (sbyte)2, (ushort)0 },
            EncodeDate(client, start), EncodeDate(client, end), new GXArray()
        };
        var parameters = new GXByteBuffer();
        parameters.SetUInt8(1);
        parameters.Set(GXDLMSConverter.GetBytes(selection, DataType.Structure));
        // Let the library handle framing, fragmentation, invocation counters, and ciphering.
        // Do not modify already encoded or encrypted messages to replace the clock identifier.
        try
        {
            return (byte[][])_selectiveRead.Invoke(client,
                [profile.Name, profile.ObjectType, 2, parameters, (int)profile.GetAccess3(2)])!;
        }
        catch (TargetInvocationException error) when (error.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    /// <summary>Encodes an octet-string timestamp using the client's skip and deviation conventions.</summary>
    /// <param name="client">The connected client's date-time options.</param>
    /// <param name="value">The requested local or UTC boundary.</param>
    /// <returns>The twelve-byte DLMS date-time value without a data-type tag.</returns>
    private static byte[] EncodeDate(GXDLMSClient client, DateTime value)
    {
        var date = new GXDateTime(value);
        date.Skip |= client.DateTimeSkips;
        byte[] bytes = GXDLMSConverter.GetBytes(date, DataType.DateTime)[1..];
        short deviation = BinaryPrimitives.ReadInt16BigEndian(bytes.AsSpan(9, 2));
        if (client.UseUtc2NormalTime && deviation != short.MinValue)
        {
            BinaryPrimitives.WriteInt16BigEndian(bytes.AsSpan(9, 2), checked((short)-deviation));
        }
        return bytes;
    }
}
