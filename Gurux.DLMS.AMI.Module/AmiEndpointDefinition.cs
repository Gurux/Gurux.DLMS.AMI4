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

/// <summary>HTTP endpoint declared by a module.</summary>
/// <param name="pattern">Route pattern matched relative to the module endpoint group.</param>
/// <param name="methods">HTTP methods accepted by the endpoint.</param>
/// <param name="handler">Handler invoked with the module request scope and cancellation token.</param>
public sealed class AmiEndpointDefinition(string pattern, string[] methods, Func<AmiModuleRequest, CancellationToken, Task> handler)
{
    /// <summary>Route pattern relative to the module endpoint group.</summary>
    public string Pattern { get; } = pattern;
    /// <summary>HTTP methods accepted by the endpoint.</summary>
    public string[] Methods { get; } = methods;
    /// <summary>Asynchronous request handler.</summary>
    public Func<AmiModuleRequest, CancellationToken, Task> Handler { get; } = handler;
    /// <summary>Authorization policies required by the endpoint; initially empty.</summary>
    public List<string> Policies { get; } = [];
    /// <summary>Adds a required access policy and returns this endpoint.</summary>
    /// <param name="policy">Access policy required to use the endpoint or view.</param>
    /// <returns>The endpoint definition for further configuration.</returns>
    public AmiEndpointDefinition RequirePolicy(string policy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policy);
        Policies.Add(policy);
        return this;
    }
}
