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

using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using System.Text.Json;

namespace Gurux.DLMS.AMI.Server.Repository;

internal static class GXValueWorkflowEvents
{
    internal static IEnumerable<GXWorkflowEventContext> Create(Guid attributeId, Guid? deviceId,
        string? oldValue, string? newValue, Guid operationId)
    {
        GXWorkflowEventContext Make(string code) => new()
        {
            EventCode = code, TargetId = attributeId.ToString(), AttributeId = attributeId,
            DeviceId = deviceId, OperationId = operationId, OccurredAt = DateTimeOffset.UtcNow,
            Outcome = "success", OldValue = JsonSerializer.SerializeToElement(oldValue),
            NewValue = JsonSerializer.SerializeToElement(newValue)
        };
        yield return Make("value.received");
        if (!string.Equals(oldValue, newValue, StringComparison.Ordinal))
            {
            yield return Make("value.changed");
        }
    }
}
