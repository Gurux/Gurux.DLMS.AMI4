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

using Microsoft.AspNetCore.Components;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gurux.DLMS.AMI.Module;

/// <summary>Host-owned dispatcher; exported implementations cannot be retained after the callback.</summary>
public interface IGXAmiModuleServices
{
    /// <summary>Checks whether a service currently has an active module provider without invoking it.</summary>
    /// <typeparam name="TService">The exported service contract.</typeparam>
    /// <returns>True when an active provider is available. Availability may change before invocation.</returns>
    bool IsAvailable<TService>() where TService : class;

    /// <summary>Invokes an exported service within its owning module for the duration of the callback.</summary>
    /// <typeparam name="TService">Service contract exported by an active module.</typeparam>
    /// <param name="operation">Callback that must finish using the service before its task completes.</param>
    /// <param name="cancellationToken">Token used to cancel the invocation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task InvokeAsync<TService>(Func<TService, CancellationToken, Task> operation, CancellationToken cancellationToken = default) where TService : class;
    /// <summary>Invokes an exported service within its owning module for the duration of the callback.</summary>
    /// <typeparam name="TService">Service contract exported by an active module.</typeparam>
    /// <typeparam name="TResult">Result type that can be used independently of the module service scope.</typeparam>
    /// <param name="operation">Callback that must finish using the service before its task completes.</param>
    /// <param name="cancellationToken">Token used to cancel the invocation.</param>
    /// <returns>The result produced by the callback.</returns>
    Task<TResult> InvokeAsync<TService, TResult>(Func<TService, CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default) where TService : class;
    /// <summary>Invokes an available provider; false means callback never started.</summary>
    async Task<bool> TryInvokeAsync<TService>(Func<TService, CancellationToken, Task> operation, CancellationToken cancellationToken = default) where TService : class
    {
        var result = await TryInvokeAsync<TService, bool>(async (service, token) =>
        {
            await operation(service, token);
            return true;
        }, cancellationToken);
        return result.Invoked;
    }
    /// <summary>Invokes an available provider with an optional result.</summary>
    async Task<AmiModuleInvocationResult<TResult>> TryInvokeAsync<TService, TResult>(Func<TService, CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default) where TService : class
    {
        bool started = false;
        try
        {
            var result = await InvokeAsync<TService, TResult>((service, token) =>
            {
                started = true;
                return operation(service, token);
            }, cancellationToken);
            return new(true, result);
        }
        catch (AmiModuleUnavailableException) when (!started)
        {
            return new(false, default);
        }
        catch (OperationCanceledException) when (!started && !cancellationToken.IsCancellationRequested && !IsAvailable<TService>())
        {
            return new(false, default);
        }
    }
    /// <summary>Invokes with independent cancellation signals.</summary>
    Task InvokeWithContextAsync<TService>(Func<TService, AmiModuleInvocationContext, Task> operation, CancellationToken cancellationToken = default) where TService : class
        => InvokeAsync<TService>((service, _) => operation(service, new(cancellationToken)), cancellationToken);
    /// <summary>Invokes with independent cancellation signals and returns a result.</summary>
    Task<TResult> InvokeWithContextAsync<TService, TResult>(Func<TService, AmiModuleInvocationContext, Task<TResult>> operation, CancellationToken cancellationToken = default) where TService : class
        => InvokeAsync<TService, TResult>((service, _) => operation(service, new(cancellationToken)), cancellationToken);
    /// <summary>Optionally invokes with independent cancellation signals.</summary>
    async Task<bool> TryInvokeWithContextAsync<TService>(Func<TService, AmiModuleInvocationContext, Task> operation, CancellationToken cancellationToken = default) where TService : class
    {
        bool started = false;
        try
        {
            await InvokeWithContextAsync<TService>((service, context) =>
            {
                started = true;
                return operation(service, context);
            }, cancellationToken);
            return true;
        }
        catch (AmiModuleUnavailableException) when (!started)
        {
            return false;
        }
        catch (OperationCanceledException) when (!started && !cancellationToken.IsCancellationRequested && !IsAvailable<TService>())
        {
            return false;
        }
    }
}
