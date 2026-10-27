//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using Microsoft.AspNetCore.Components;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gurux.DLMS.AMI.Module;

/// <summary>Host-owned dispatcher; exported implementations cannot be retained after the callback.</summary>
public interface IAmiModuleServices
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
}
