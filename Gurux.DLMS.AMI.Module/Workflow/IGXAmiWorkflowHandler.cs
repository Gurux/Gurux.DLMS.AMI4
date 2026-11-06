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

/// <summary>Executes a module-defined workflow action under the host's invocation context.</summary>
public interface IGXAmiWorkflowHandler
{
    /// <summary>Stable action identifier, unique within the module.</summary>
    string Code { get; }
    /// <summary>Executes an action. Before is awaited inline and may reject; After cannot undo a commit.
    /// The host supplies cancellation for shutdown and time limits. Handlers must observe it.</summary>
    /// <param name="context">Trigger, user, target, and value snapshots for this invocation.</param>
    /// <param name="cancellationToken">Cancels execution when the host shuts down or the invocation times out.</param>
    /// <returns>The handler's continuation or rejection decision; rejection applies only before commit.</returns>
    ValueTask<AmiWorkflowResult> ExecuteAsync(AmiWorkflowContext context, CancellationToken cancellationToken = default);
}
