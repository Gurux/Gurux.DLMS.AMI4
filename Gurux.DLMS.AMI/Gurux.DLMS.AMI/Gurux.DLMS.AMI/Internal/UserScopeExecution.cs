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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Gurux.DLMS.AMI.Server.Internal;

static partial class ScopeHelpers
{
    /// <summary>Runs work in an isolated service scope with the supplied user's application permissions.</summary>
    public static Task RunAsUserAsync(this IServiceProvider serviceProvider, ClaimsPrincipal user,
        Func<IServiceProvider, Task> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        return serviceProvider.RunAsUserAsync(user, async services =>
        {
            await action(services);
            return true;
        }, cancellationToken);
    }

    /// <summary>Runs work as a user without inheriting or modifying the caller's HTTP context.</summary>
    public static Task<T> RunAsUserAsync<T>(this IServiceProvider serviceProvider, ClaimsPrincipal user,
        Func<IServiceProvider, Task<T>> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(action);

        // Carry only server-owned workflow origin metadata; arbitrary request execution context remains suppressed.
        var workflowScope = Gurux.DLMS.AMI.Server.Services.GXWorkflowService.CaptureExecutionScope();

        async Task<T> ExecuteAsync()
        {
            cancellationToken.ThrowIfCancellationRequested();
            IHttpContextAccessor? accessor = null;
            try
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
                accessor.HttpContext = new DefaultHttpContext
                {
                    User = user,
                    RequestServices = scope.ServiceProvider
                };
                return await Gurux.DLMS.AMI.Server.Services.GXWorkflowService.RunInExecutionScopeAsync(workflowScope,
                    () => action(scope.ServiceProvider));
            }
            finally
            {
                // Keep the user available during asynchronous scope disposal, then release it.
                if (accessor != null)
                {
                    accessor.HttpContext = null;
                }
            }
        }

        // HttpContextAccessor shares a mutable holder across flowed execution contexts.
        // Changing it in an ordinary child task would also clear the caller's context.
        if (ExecutionContext.IsFlowSuppressed())
        {
            return Task.Run(ExecuteAsync, cancellationToken);
        }
        using (ExecutionContext.SuppressFlow())
        {
            return Task.Run(ExecuteAsync, cancellationToken);
        }
    }
}
