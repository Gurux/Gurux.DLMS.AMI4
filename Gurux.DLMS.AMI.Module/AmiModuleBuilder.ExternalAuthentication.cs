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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Gurux.DLMS.AMI.Module;

public sealed partial class AmiModuleBuilder
{
    /// <summary>Concrete scoped external-login definition source, or null when none was declared.</summary>
    public Type? ExternalLoginProviderType { get; private set; }

    /// <summary>Registers one scoped source of external login definitions owned by this module.</summary>
    /// <typeparam name="T">Concrete provider type constructed from module-scoped services.</typeparam>
    /// <remarks>
    /// A provider can return multiple login definitions. Like AddAuthenticator, this registers
    /// the concrete type only. The host resolves ExternalLoginProviderType within the module's
    /// service scope. This declaration does not export the service or alter host authentication.
    /// Existing unkeyed registrations for T must be scoped; an existing scoped factory is retained.
    /// </remarks>
    /// <exception cref="InvalidOperationException">A provider was already declared or an existing registration has an incompatible lifetime.</exception>
    /// <exception cref="ArgumentException">The provider type is abstract or contains unbound generic parameters.</exception>
    public void AddExternalLoginProvider<T>() where T : class, IGXAmiExternalLoginProvider
    {
        if (ExternalLoginProviderType is not null)
        {
            throw new InvalidOperationException("A module can register only one external login provider source.");
        }
        var type = typeof(T);
        if (type.IsAbstract || type.ContainsGenericParameters)
        {
            throw new ArgumentException("The external login provider must be a concrete closed type.", nameof(T));
        }
        if (Services.Any(service => !service.IsKeyedService && service.ServiceType == type && service.Lifetime != ServiceLifetime.Scoped))
        {
            throw new InvalidOperationException("An external login provider must have a scoped lifetime.");
        }
        Services.TryAddScoped<T>();
        ExternalLoginProviderType = type;
    }
}
