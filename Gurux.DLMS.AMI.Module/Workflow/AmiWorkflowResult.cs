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

namespace Gurux.DLMS.AMI.Module.Workflow;

/// <summary>Outcome of a module handler. Rejection is valid only for the Before phase.</summary>
public sealed record AmiWorkflowResult
{
    /// <summary>Creates the immutable decision returned by a workflow handler.</summary>
    /// <param name="allowed">Whether the proposed operation may continue.</param>
    /// <param name="message">Rejection explanation, or null for successful completion.</param>
    private AmiWorkflowResult(bool allowed, string? message) { Allowed = allowed; Message = message; }
    /// <summary>Whether the operation may continue.</summary>
    public bool Allowed { get; }
    /// <summary>Explanation of a rejection, suitable for returning to the caller.</summary>
    public string? Message { get; }
    /// <summary>Successful completion; allows the operation to continue.</summary>
    public static AmiWorkflowResult Continue { get; } = new(true, null);
    /// <summary>Rejects a proposed change. After handlers must report failures by throwing instead.</summary>
    /// <param name="message">Nonblank explanation of why the proposed change is rejected.</param>
    /// <returns>A result that prevents the proposed operation from continuing.</returns>
    /// <exception cref="ArgumentException">The explanation is null, empty, or whitespace.</exception>
    public static AmiWorkflowResult Reject(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new(false, message);
    }
}
