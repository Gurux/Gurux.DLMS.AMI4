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

using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Device;

namespace Gurux.DLMS.AMI.Agent.Worker;

/// <summary>Retains the failed target alongside the original operation error.</summary>
internal static class GXTaskFailure
{
    /// <summary>Builds a task failure context containing available device and protocol identifiers.</summary>
    /// <param name="task">Task or asynchronous operation whose execution is being coordinated.</param>
    /// <param name="device">Device associated with the task or diagnostic record.</param>
    /// <returns>A safe diagnostic context containing task, device and protocol identifiers.</returns>
    internal static string Describe(GXTask task, GXDevice? device)
    {
        var obj = task.Object?.Template != null ? task.Object : task.Attribute?.Object;
        var devices = new[] { device, task.Device, obj?.Device };
        var named = devices.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d?.Name));
        var id = devices.FirstOrDefault(d => d != null && d.Id != Guid.Empty)?.Id ?? task.TargetDevice;
        var fields = new List<string>();
        if (named != null)
        {
            fields.Add("Device: " + named.Name);
        }
        else if (id != null && id != Guid.Empty)
        {
            fields.Add("Device: " + id);
        }
        if (!string.IsNullOrWhiteSpace(obj?.Template?.LogicalName))
        {
            fields.Add("OBIS: " + obj.Template.LogicalName);
        }
        if (!string.IsNullOrWhiteSpace(obj?.Template?.Name))
        {
            fields.Add("Description: " + obj.Template.Name);
        }
        int? index = task.Index is > 0 ? task.Index : task.Attribute?.Template?.Index;
        if (index is > 0)
        {
            fields.Add("Attribute index: " + index);
        }
        return string.Join("; ", fields);
    }

    /// <summary>Associates a task failure with the task and its device diagnostic state.</summary>
    /// <param name="task">Task or asynchronous operation whose execution is being coordinated.</param>
    /// <param name="device">Device associated with the task or diagnostic record.</param>
    /// <param name="error">Failure associated with the current operation, if available.</param>
    internal static void Record(GXTask task, GXDevice? device, string error)
    {
        string context = Describe(task, device);
        string message = context.Length == 0 ? error : context + ". " + error;
        task.Error = string.IsNullOrWhiteSpace(task.Error) ? message : task.Error + Environment.NewLine + message;
    }
}