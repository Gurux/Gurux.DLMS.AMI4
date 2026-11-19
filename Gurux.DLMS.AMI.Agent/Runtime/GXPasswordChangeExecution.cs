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

namespace Gurux.DLMS.AMI.Agent.Worker;

/// <summary>Sequences a durable intent, meter mutation, association verification and server acknowledgement.</summary>
internal static class GXPasswordChangeExecution
{
    /// <summary>Orders password write, new-association verification and workflow confirmation so retries cannot repeat an uncertain secret change.</summary>
    /// <param name="alreadyUsingNewPassword">Whether a successful new-password association has already confirmed the write.</param>
    /// <param name="markWriting">Persists the workflow transition before modifying the device secret.</param>
    /// <param name="write">Writes the replacement password to the authenticated device association.</param>
    /// <param name="verifyNewAssociation">Verifies that the device accepts an association using the new secret.</param>
    /// <param name="confirm">Persists successful verification of the changed password.</param>
    /// <returns>A task representing this operation: Orders password write, new-association verification and workflow confirmation so retries cannot repeat an uncertain secret change.</returns>
    internal static async Task RunAsync(bool alreadyUsingNewPassword, Func<Task> markWriting, Func<Task> write,
        Func<Task> verifyNewAssociation, Func<Task> confirm)
    {
        if (!alreadyUsingNewPassword)
        {
            await markWriting();
            await write();
            await verifyNewAssociation();
        }
        await confirm();
    }
}