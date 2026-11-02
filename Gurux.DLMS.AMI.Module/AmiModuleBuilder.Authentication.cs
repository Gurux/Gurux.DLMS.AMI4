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
    public void AddAuthenticator<T>() where T : class, IAmiModuleAuthenticator
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
