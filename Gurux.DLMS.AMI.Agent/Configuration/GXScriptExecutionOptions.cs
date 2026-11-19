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

using Microsoft.Extensions.Configuration;

namespace Gurux.DLMS.AMI.Agent.Shared;

/// <summary>Limits how long the agent waits for each script invocation.</summary>
internal sealed class GXScriptExecutionOptions
{
    /// <summary>Gets or sets the positive execution deadline, up to one day. Defaults to 30 seconds.</summary>
    public TimeSpan ExecutionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Rejects invalid limits before the owning runtime component starts work.</summary>
    internal void Validate()
    {
        if (ExecutionTimeout <= TimeSpan.Zero || ExecutionTimeout > TimeSpan.FromDays(1))
        {
            throw new ArgumentException("Scripts:ExecutionTimeout must be positive and at most one day.");
        }
    }

    /// <summary>Reads and validates script execution timeout and abandonment limits from application configuration.</summary>
    /// <param name="configuration">Application configuration containing execution settings.</param>
    /// <returns>Validated script timeout and abandoned-execution limits from configuration.</returns>
    internal static GXScriptExecutionOptions Read(IConfiguration configuration)
    {
        var options = new GXScriptExecutionOptions
        {
            ExecutionTimeout = configuration.GetValue("Scripts:ExecutionTimeout", TimeSpan.FromSeconds(30))
        };
        options.Validate();
        return options;
    }
}
