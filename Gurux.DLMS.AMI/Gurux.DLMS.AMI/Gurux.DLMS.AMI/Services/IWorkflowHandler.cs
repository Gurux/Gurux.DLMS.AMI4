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

using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using System.Runtime.Loader;

namespace Gurux.DLMS.AMI.Server.Services
{
    /// <summary>
    /// This interface handles workflow operations.
    /// </summary>
    public interface IWorkflowHandler
    {
        /// <summary>
        /// Add new workflow to executed.
        /// </summary>
        /// <param name="item">Item to add.</param>
        void Add(GXWorkflow item);

        /// <summary>
        /// Remove workflow.
        /// </summary>
        /// <param name="item">Item to remove.</param>
        void Delete(GXWorkflow item);

        /// <summary>
        /// The workflow item has changed and for example, the script needs to reload.
        /// </summary>
        /// <param name="source">Changed item.</param>
        void Update(object source);

        /// <summary>
        /// Invoke trigger.
        /// </summary>
        /// <param name="trigger">Trigger to execute.</param>
        /// <param name="activity">Selected activity.</param>
        /// <param name="source">Event payload; it need not be the initiating user.</param>
        /// <param name="actor">Explicit initiating principal, or the current authenticated request user.</param>
        void Execute(Type trigger, string activity, object? source, ClaimsPrincipal? actor = null);

        /// <summary>Publishes a trigger asynchronously, completing after its event is durably saved.</summary>
        /// <param name="trigger">Trigger type.</param>
        /// <param name="activity">Selected activity.</param>
        /// <param name="source">Event payload.</param>
        /// <param name="actor">Initiating principal, or the current authenticated request user.</param>
        /// <param name="cancellationToken">Cancels event publication.</param>
        Task ExecuteAsync(Type trigger, string activity, object? source, ClaimsPrincipal? actor = null, CancellationToken cancellationToken = default);

        /// <summary>Queues only the selected workflow as a manual invocation under the caller's identity.</summary>
        /// <param name="workflowId">Workflow identifier.</param>
        /// <param name="actor">Authenticated user who requested the run.</param>
        void ExecuteWorkflow(Guid workflowId, ClaimsPrincipal actor);

        /// <summary>Publishes a registered event snapshot for matching workflows.</summary>
        /// <param name="context">Server-authored event data.</param>
        /// <param name="actor">Initiating user, or an explicitly empty principal for a machine event.</param>
        void Publish(GXWorkflowEventContext context, ClaimsPrincipal? actor = null);

        /// <summary>
        /// Add loaded script.
        /// </summary>
        /// <param name="id">Script to add.</param>
        /// <param name="alc">Assembly load context.</param>
        void AddScript(Guid id, AssemblyLoadContext alc);

        /// <summary>
        /// Unload script
        /// </summary>
        /// <param name="id">Script to remove.</param>
        void UnloadScript(Guid id);

        /// <summary>
        /// Get loaded script.
        /// </summary>
        /// <param name="id">Script Id.</param>
        /// <returns>Assembly load context.</returns>
        AssemblyLoadContext? GetScript(Guid id);
    }
}
