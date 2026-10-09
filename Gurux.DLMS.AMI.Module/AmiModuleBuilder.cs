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

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gurux.DLMS.AMI.Module;

/// <summary>Declarations collected before a module becomes active.</summary>
public sealed class AmiModuleBuilder(string moduleId, IConfiguration configuration, AmiModuleSettings settings)
{
    /// <summary>Unique module identifier.</summary>
    public string ModuleId { get; } = moduleId;
    /// <summary>Host configuration available to the module.</summary>
    public IConfiguration Configuration { get; } = configuration;
    /// <summary>Module settings snapshot.</summary>
    public AmiModuleSettings Settings { get; } = settings;
    /// <summary>Services registered in the module scope.</summary>
    public IServiceCollection Services { get; } = new ServiceCollection();
    /// <summary>HTTP endpoints declared by the module.</summary>
    public List<AmiEndpointDefinition> Endpoints { get; } = [];
    /// <summary>Module-owned endpoint source types.</summary>
    public List<Type> EndpointSourceTypes { get; } = [];
    /// <summary>Database tables owned by the module.</summary>
    public List<AmiModuleTableRegistration> OwnedTables { get; } = [];
    /// <summary>Registers a database table owned by the module.</summary>
    public void OwnTable<T>(AmiModuleTableKind kind = AmiModuleTableKind.Data) => OwnedTables.Add(new(typeof(T), kind));
    /// <summary>Externally owned host singleton types shared with the module.</summary>
    public List<Type> HostSingletonTypes { get; } = [];
    /// <summary>Shares an externally owned host singleton without transferring disposal ownership.</summary>
    public void AddHostSingleton<TService>() where TService : class => HostSingletonTypes.Add(typeof(TService));
    /// <summary>Declares a module-owned ASP.NET endpoint source, validated by the host at startup.</summary>
    public void AddEndpointSource<TSource>() where TSource : class => EndpointSourceTypes.Add(typeof(TSource));
    /// <summary>Middleware declared by the module.</summary>
    public List<AmiMiddlewareDefinition> Middleware { get; } = [];
    /// <summary>Service types exported to other modules.</summary>
    public HashSet<Type> Exports { get; } = [];
    /// <summary>User and configuration tabs declared by the module.</summary>
    public List<AmiModuleTab> Tabs { get; } = [];
    /// <summary>Pages declared by the module.</summary>
    public List<AmiModulePage> Pages { get; } = [];
    /// <summary>Registers a component at a unique module page route with an access policy.</summary>
    public void AddPage<TComponent>(string route, string policy, string? title = null, bool navigation = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policy);
        if (!System.Text.RegularExpressions.Regex.IsMatch(route, "^[A-Za-z0-9][A-Za-z0-9_-]*$") || Pages.Any(p => p.Route.Equals(route, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("Module page route must be a unique URL segment.", nameof(route));
        }

        Pages.Add(new(route, typeof(TComponent).FullName!, policy)
        {
            Title = title,
            Navigation = navigation
        });
    }
    /// <summary>Registers a component in the User or Config tab area with an access policy.</summary>
    public void AddTab<TComponent>(string area, string title, string policy)
    {
        if (area is not ("User" or "Config"))
        {
            throw new ArgumentException("Unknown tab area.", nameof(area));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(policy);
        Tabs.Add(new(area, title, typeof(TComponent).FullName!, policy));
    }
    /// <summary>Declares a service type available through the host dispatcher.</summary>
    public void Export<TService>() where TService : class => Exports.Add(typeof(TService));
    /// <summary>Registers a handler for HTTP GET requests.</summary>
    public AmiEndpointDefinition MapGet(string pattern, Func<AmiModuleRequest, CancellationToken, Task> handler)
        => MapMethods(pattern, ["GET"], handler);
    /// <summary>Registers a handler for HTTP POST requests.</summary>
    public AmiEndpointDefinition MapPost(string pattern, Func<AmiModuleRequest, CancellationToken, Task> handler)
        => MapMethods(pattern, ["POST"], handler);
    /// <summary>Registers a handler for the specified HTTP methods.</summary>
    public AmiEndpointDefinition MapMethods(string pattern, string[] methods, Func<AmiModuleRequest, CancellationToken, Task> handler)
    {
        var endpoint = new AmiEndpointDefinition(pattern, methods, handler);
        Endpoints.Add(endpoint);
        return endpoint;
    }
    /// <summary>Registers module middleware with the specified execution order.</summary>
    public void Use(Func<AmiModuleRequest, RequestDelegate, CancellationToken, Task> handler, int order = 0)
        => Middleware.Add(new(order, handler));
}

/// <summary>HTTP endpoint declared by a module.</summary>
public sealed class AmiEndpointDefinition(string pattern, string[] methods, Func<AmiModuleRequest, CancellationToken, Task> handler)
{
    /// <summary>URL pattern matched by the endpoint.</summary>
    public string Pattern { get; } = pattern;
    /// <summary>HTTP methods accepted by the endpoint.</summary>
    public string[] Methods { get; } = methods;
    /// <summary>Asynchronous request handler.</summary>
    public Func<AmiModuleRequest, CancellationToken, Task> Handler { get; } = handler;
    /// <summary>Access policies required by the endpoint.</summary>
    public List<string> Policies { get; } = [];
    /// <summary>Adds a required access policy and returns this endpoint.</summary>
    public AmiEndpointDefinition RequirePolicy(string policy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policy);
        Policies.Add(policy);
        return this;
    }
}

/// <summary>Module middleware and its execution order.</summary>
/// <param name="Handler">Asynchronous middleware handler.</param>
/// <param name="Order">Middleware execution order.</param>
public sealed record AmiMiddlewareDefinition(int Order, Func<AmiModuleRequest, RequestDelegate, CancellationToken, Task> Handler);

/// <summary>Host-owned dispatcher; exported implementations cannot be retained after the callback.</summary>
public interface IAmiModuleServices
{
    /// <summary>Invokes an exported service within its owning module for the duration of the callback.</summary>
    Task InvokeAsync<TService>(Func<TService, CancellationToken, Task> operation, CancellationToken cancellationToken = default) where TService : class;
    /// <summary>Invokes an exported service within its owning module for the duration of the callback.</summary>
    Task<TResult> InvokeAsync<TService, TResult>(Func<TService, CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default) where TService : class;
}


/// <summary>Identifies a root route owned by an active module.</summary>
public sealed record AmiModuleRouteOwner(string ModuleId);


