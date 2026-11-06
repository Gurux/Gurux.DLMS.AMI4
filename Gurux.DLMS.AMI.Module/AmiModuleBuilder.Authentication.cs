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
    /// <summary>Concrete scoped authenticator for this module's HTTP endpoints, or null for host authentication.</summary>
    public Type? AuthenticatorType { get; private set; }

    /// <summary>Registers the single scoped authenticator for HTTP endpoints owned by this module.</summary>
    /// <typeparam name="T">Concrete authenticator type constructed from module services.</typeparam>
    /// <remarks>
    /// This does not authenticate other modules or host endpoints. An existing registration for
    /// <typeparamref name="T"/> must also be scoped. Invalid applicable credentials must be rejected
    /// rather than returning <see cref="AmiAuthenticationResult.NotHandled"/>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">An authenticator is already registered or an existing service has another lifetime.</exception>
    /// <exception cref="ArgumentException">The authenticator type cannot be instantiated.</exception>
    public void AddAuthenticator<T>() where T : class, IGXAmiModuleAuthenticator
    {
        if (AuthenticatorType is not null)
        {
            throw new InvalidOperationException("A module can register only one authenticator.");
        }
        var type = typeof(T);
        if (type.IsAbstract || type.ContainsGenericParameters)
        {
            throw new ArgumentException("The authenticator must be a concrete closed type.", nameof(T));
        }
        if (Services.Any(service => service.ServiceType == type && service.Lifetime != ServiceLifetime.Scoped))
        {
            throw new InvalidOperationException("A module authenticator must have a scoped lifetime.");
        }
        Services.TryAddScoped<T>();
        AuthenticatorType = type;
    }
}
