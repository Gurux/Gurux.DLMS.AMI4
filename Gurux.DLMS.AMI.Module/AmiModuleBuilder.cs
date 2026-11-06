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

using Gurux.DLMS.AMI.Module.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gurux.DLMS.AMI.Module;

/// <summary>Declarations collected before a module becomes active.</summary>
/// <param name="moduleId">Unique identifier of the module being configured.</param>
/// <param name="configuration">Host configuration exposed during module configuration.</param>
    /// <param name="settings">Host-provided snapshot for reading module configuration and durable state.</param>
public sealed partial class AmiModuleBuilder(string moduleId, IConfiguration configuration, IGXAmiModuleSettingsSnapshot settings)
{
    /// <summary>Unique module identifier.</summary>
    public string ModuleId { get; } = moduleId;

    /// <summary>Host configuration available to the module.</summary>
    public IConfiguration Configuration { get; } = configuration;

    /// <summary>Module settings snapshot.</summary>

    public IGXAmiModuleSettingsSnapshot Settings { get; } = settings;
    /// <summary>Services registered in the module scope.</summary>

    public IServiceCollection Services { get; } = new ServiceCollection();
    /// <summary>HTTP endpoints declared by the module.</summary>
    public List<AmiEndpointDefinition> Endpoints { get; } = [];
    /// <summary>Module-owned endpoint source types.</summary>
    public List<Type> EndpointSourceTypes { get; } = [];
    /// <summary>Database tables owned by the module.</summary>
    public List<AmiModuleTableRegistration> OwnedTables { get; } = [];
    /// <summary>Registers a database table owned by the module.</summary>
    /// <param name="kind">Ownership category used by the host when managing the module's table.</param>
    /// <typeparam name="T">Database table type owned by the module.</typeparam>
    public void OwnTable<T>(AmiModuleTableKind kind = AmiModuleTableKind.Data) => OwnedTables.Add(new(typeof(T), kind));
    /// <summary>Externally owned host singleton types shared with the module.</summary>
    public List<Type> HostSingletonTypes { get; } = [];
    /// <summary>Shares an externally owned host singleton without transferring disposal ownership.</summary>
    /// <typeparam name="TService">Service contract shared with or exported by the module.</typeparam>
    public void AddHostSingleton<TService>() where TService : class => HostSingletonTypes.Add(typeof(TService));
    /// <summary>Declares a module-owned ASP.NET endpoint source, validated by the host at startup.</summary>
    /// <typeparam name="TSource">Module-owned endpoint source type.</typeparam>
    public void AddEndpointSource<TSource>() where TSource : class => EndpointSourceTypes.Add(typeof(TSource));
    /// <summary>Middleware declared by the module.</summary>
    public List<AmiMiddlewareDefinition> Middleware { get; } = [];
    /// <summary>Service types exported to other modules.</summary>
    public HashSet<Type> Exports { get; } = [];
    /// <summary>Registered UI components.</summary>
    public List<AmiModuleView> Views { get; } = [];
    /// <summary>Pages declared by the module.</summary>
    public List<AmiModulePage> Pages { get; } = [];

    /// <summary>Registers a component at a unique module page route with an access policy.</summary>
    /// <param name="route">Unique URL segment identifying the module page.</param>
    /// <param name="policy">Access policy required to use the endpoint or view.</param>
    /// <param name="title">Optional display title for the page or view.</param>
    /// <param name="navigation">Whether the module page is exposed as a navigation entry.</param>
    /// <typeparam name="TComponent">Component type displayed by the host.</typeparam>
    public void AddPage<TComponent>(string route, string policy, string? title = null, bool navigation = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policy);
        if (!MyRegex().IsMatch(route) || Pages.Any(p => p.Route.Equals(route, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("Module page route must be a unique URL segment.", nameof(route));
        }

        Pages.Add(new(route, typeof(TComponent).FullName!, policy)
        {
            Title = title,
            Navigation = navigation
        });
    }
    /// <summary>Registers a policy-protected component for a host UI location.</summary>
    /// <param name="area">Host UI area in which the view is displayed.</param>
    /// <param name="policy">Access policy required to use the endpoint or view.</param>
    /// <param name="key">Optional view key, unique within the module and UI area.</param>
    /// <param name="title">Optional display title for the page or view.</param>
    /// <param name="icon">Icon identifier displayed for the registered view.</param>
    /// <param name="order">Execution or display order; lower values are processed first.</param>
    /// <param name="display">How the host presents the registered view.</param>
    /// <typeparam name="TComponent">Component type displayed by the host.</typeparam>
    public void AddView<TComponent>(AmiViewArea area, string policy, string? key = null,
        string? title = null, string? icon = null, int order = 0, AmiViewDisplay display = AmiViewDisplay.Tab)
        where TComponent : IComponent
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policy);
        if (typeof(TComponent).IsAbstract || typeof(TComponent).ContainsGenericParameters)
        {
            throw new ArgumentException("A view must be a concrete component.");
        }

        if (!Enum.IsDefined(area) || !Enum.IsDefined(display))
        {
            throw new ArgumentOutOfRangeException(nameof(area));
        }

        if (Views.Any(v => v.Area == area && string.Equals(v.Key, key, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("A view for this area and key is already registered.", nameof(key));
        }

        Views.Add(new(area, typeof(TComponent).FullName!, policy, key, title, icon, order, display));
    }

    /// <summary>Declares a service type available through the host dispatcher.</summary>
    /// <typeparam name="TService">Service contract shared with or exported by the module.</typeparam>
    public void Export<TService>() where TService : class => Exports.Add(typeof(TService));
    /// <summary>Registers a handler for HTTP GET requests.</summary>
    /// <param name="pattern">Route pattern for the module's HTTP endpoint.</param>
    /// <param name="handler">Asynchronous handler executed for the matched HTTP request.</param>
    /// <returns>The endpoint definition for further configuration.</returns>
    public AmiEndpointDefinition MapGet(string pattern, Func<AmiModuleRequest, CancellationToken, Task> handler)
        => MapMethods(pattern, ["GET"], handler);
    /// <summary>Registers a handler for HTTP POST requests.</summary>
    /// <param name="pattern">Route pattern for the module's HTTP endpoint.</param>
    /// <param name="handler">Asynchronous handler executed for the matched HTTP request.</param>
    /// <returns>The endpoint definition for further configuration.</returns>
    public AmiEndpointDefinition MapPost(string pattern, Func<AmiModuleRequest, CancellationToken, Task> handler)
        => MapMethods(pattern, ["POST"], handler);
    /// <summary>Registers a handler for the specified HTTP methods.</summary>
    /// <param name="pattern">Route pattern for the module's HTTP endpoint.</param>
    /// <param name="methods">HTTP methods accepted by the endpoint.</param>
    /// <param name="handler">Asynchronous handler executed for the matched HTTP request.</param>
    /// <returns>The endpoint definition for further configuration.</returns>
    public AmiEndpointDefinition MapMethods(string pattern, string[] methods, Func<AmiModuleRequest, CancellationToken, Task> handler)
    {
        var endpoint = new AmiEndpointDefinition(pattern, methods, handler);
        Endpoints.Add(endpoint);
        return endpoint;
    }
    /// <summary>Registers module middleware with the specified execution order.</summary>
    /// <param name="handler">Middleware receiving the module request, the next request delegate, and cancellation token.</param>
    /// <param name="order">Middleware execution order; lower values run first.</param>
    public void Use(Func<AmiModuleRequest, RequestDelegate, CancellationToken, Task> handler, int order = 0)
        => Middleware.Add(new(order, handler));
    /// <summary>Matches a single page route segment starting with a letter or digit.</summary>
    /// <returns>The generated route validator allowing letters, digits, underscores, and hyphens.</returns>
    [System.Text.RegularExpressions.GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9_-]*$")]
    private static partial System.Text.RegularExpressions.Regex MyRegex();
}
