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
