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

namespace Gurux.DLMS.AMI.Server.Services
{
    /// <summary>
    /// Action info is used to save detailed information from the invoked action.
    /// </summary>
    class GXActionInfo
    {
        /// <summary>
        /// Trigger type.
        /// </summary>
        public required Type Trigger;
        /// <summary>
        /// Activity.
        /// </summary>
        public required string Activity;
        /// <summary>
        /// Invoker.
        /// </summary>
        public required object? Sender;
        /// <summary>Workflow identifier selected for this invocation, if one is specified.</summary>
        public Guid? WorkflowId;
        /// <summary>Identifier assigned to the queued workflow event.</summary>
        public Guid EventId = Guid.NewGuid();
        /// <summary>UTC time when the operation entered the processing queue.</summary>
        public DateTimeOffset QueuedAt = DateTimeOffset.UtcNow;
        /// <summary>Identifier of the user or agent that initiated the operation.</summary>
        public string? ActorId;
        /// <summary>Indicates that the background operation was requested manually.</summary>
        public bool Manual;
        /// <summary>Event payload and metadata available to the workflow invocation.</summary>
        public GXWorkflowEventContext? EventContext;
        /// <summary>Identifier shared by related workflow invocations.</summary>
        public Guid ChainId;
        /// <summary>Nesting depth of the current workflow invocation chain.</summary>
        public int Depth;
        /// <summary>Identifier of the workflow delivery being processed.</summary>
        public Guid DeliveryId;
    }
}
