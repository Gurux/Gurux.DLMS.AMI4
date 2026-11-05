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
using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects;

namespace Gurux.DLMS.AMI.Agent.Worker;

/// <summary>Applies persisted value XML while retaining legacy scheduled clock values.</summary>
internal static class GXTaskWriteValue
{
    internal static void Apply(GXDLMSClient client, GXDLMSObject target, int index, string? data)
    {
        // A queued clock synchronization must use execution time, not the browser's old timestamp.
        if (target is GXDLMSClock && index == 2)
        {
            client.UpdateValue(target, index, new GXDateTime(DateTime.Now));
            return;
        }
        // Value XML must be decoded before interpreting legacy plain date/time text.
        if (data?.TrimStart().StartsWith("<", StringComparison.Ordinal) == true)
        {
            object decoded = GXDLMSTranslator.XmlToValue(data);
            // XmlToValue returns DateTime for DateTime XML; COSEM date attributes expect GXDateTime.
            if (decoded is DateTime dateTime)
            {
                decoded = new GXDateTime(dateTime);
            }
            client.UpdateValue(target, index, decoded);
            return;
        }
        DataType type = target.GetDataType(index);
        DataType uiType = target.GetUIDataType(index);
        if (uiType == DataType.DateTime || uiType == DataType.Date || uiType == DataType.Time)
        {
            if (type == DataType.OctetString)
            {
                object value;
                if (string.IsNullOrEmpty(data))
                {
                    value = new GXDateTime(DateTime.Now);
                }
                else if (uiType == DataType.DateTime)
                {
                    value = new GXDateTime(data, CultureInfo.InvariantCulture);
                }
                else if (uiType == DataType.Date)
                {
                    value = new GXDate(data, CultureInfo.InvariantCulture);
                }
                else
                {
                    value = new GXTime(data, CultureInfo.InvariantCulture);
                }
                client.UpdateValue(target, index, value);
            }
        }
        else
        {
            client.UpdateValue(target, index, GXDLMSTranslator.XmlToValue(data));
        }
    }
}
