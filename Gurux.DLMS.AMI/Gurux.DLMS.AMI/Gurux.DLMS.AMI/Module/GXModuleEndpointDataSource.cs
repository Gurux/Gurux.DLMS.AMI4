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

using Gurux.DLMS.AMI.Module;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Primitives;

namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>Routing only retains host delegates and string metadata, never plugin handler instances.</summary>

public sealed class GXModuleEndpointDataSource : EndpointDataSource, IDisposable
{
    private readonly GXModuleRuntime runtime;
    private readonly Lock sync = new();
    private CancellationTokenSource changes = new();
    private IReadOnlyList<Endpoint> endpoints = [];
    private bool disposed;
    /// <summary>Initializes a new instance of <see cref="GXModuleEndpointDataSource"/>.</summary>
    /// <param name="runtime">The runtime.</param>
    public GXModuleEndpointDataSource(GXModuleRuntime runtime)
    {
        this.runtime = runtime;
        runtime.Changed += Refresh;
        Refresh();
    }
    /// <inheritdoc />
    public override IReadOnlyList<Endpoint> Endpoints
    {
        get
        {
            lock (sync)
            {
                return endpoints;
            }
        }
    }
    /// <inheritdoc />
    public override IChangeToken GetChangeToken()
    {
        lock (sync)
        {
            return new CancellationChangeToken(changes.Token);
        }
    }
    private void Refresh()
    {
        CancellationTokenSource old;
        lock (sync)
        {
            if (disposed)
            {
                return;
            }

            endpoints = [.. runtime.GetEndpoints().Select(Create), .. runtime.GetOwnedEndpoints()];
            old = changes;
            changes = new();
        }
        old.Cancel();
        old.Dispose();
    }
    private Endpoint Create(AmiPublishedEndpoint definition)
    {
        string id = definition.ModuleId;
        string generation = definition.Generation;
        int index = definition.Index;
        var builder = new RouteEndpointBuilder(context => runtime.DispatchEndpointAsync(id, index, context, generation), RoutePatternFactory.Parse(definition.Pattern), 0)
        {
            DisplayName = "Module " + id + " " + definition.Pattern
        };
        builder.Metadata.Add(new HttpMethodMetadata(definition.Methods));
        builder.Metadata.Add(new AmiModuleRouteOwner(id));
        builder.Metadata.Add(new GXModuleEndpointGeneration(generation, definition.HasAuthenticator));
        if (definition.IsAnonymous)
        {
            builder.Metadata.Add(new AllowAnonymousAttribute());
        }
        else if (definition.Policies.Length == 0)
        {
            builder.Metadata.Add(new AuthorizeAttribute());
        }

        foreach (string policy in definition.Policies)
        {
            builder.Metadata.Add(new AuthorizeAttribute(policy));
        }

        return builder.Build();
    }
    /// <inheritdoc />
    public void Dispose()
    {
        runtime.Changed -= Refresh;
        lock (sync)
        {
            disposed = true;
            changes.Dispose();
        }
    }
}


