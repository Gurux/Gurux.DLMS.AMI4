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

using System.Reflection;

namespace Gurux.DLMS.AMI.Agent.Shared;

/// <summary>Declares a worker assembly's required host contract. A major change denotes a breaking contract change.</summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class GXAgentContractVersionAttribute : Attribute
{
    /// <summary>Declares the required contract version.</summary>
    /// <param name="major">Required contract major version.</param>
    /// <param name="minimumHostMinor">Minimum required host minor version within this major version.</param>
    public GXAgentContractVersionAttribute(int major, int minimumHostMinor)
        => (Major, MinimumHostMinor) = (major, minimumHostMinor);
    /// <summary>Gets the required major version.</summary>
    public int Major { get; }
    /// <summary>Gets the minimum host minor version.</summary>
    public int MinimumHostMinor { get; }
}

/// <summary>Checks compatibility using assembly metadata, without creating a worker or its attributes.</summary>
public static class GXAgentContract
{
    /// <summary>Current binary contract major version.</summary>
    public const int Major = 2;
    /// <summary>Current host contract minor version.</summary>
    public const int Minor = 0;

    /// <summary>Checks worker contract metadata before creating or initializing a worker.</summary>
    /// <param name="workerAssembly">Assembly containing the worker.</param>
    /// <exception cref="GXAgentContractException">Metadata is absent, invalid or incompatible.</exception>
    public static void EnsureCompatible(Assembly workerAssembly)
    {
        ArgumentNullException.ThrowIfNull(workerAssembly);
        var attributes = workerAssembly.GetCustomAttributesData()
            .Where(a => a.AttributeType.FullName == typeof(GXAgentContractVersionAttribute).FullName).ToArray();
        if (attributes.Length != 1 || attributes[0].ConstructorArguments.Count != 2 ||
            attributes[0].ConstructorArguments[0].Value is not int major ||
            attributes[0].ConstructorArguments[1].Value is not int minor)
            throw new GXAgentContractException("Worker contract metadata is missing or invalid. Deploy a worker built for this host.");
        if (major != Major || minor < 0 || minor > Minor)
            throw new GXAgentContractException($"Worker requires contract {major}.{minor}; host supports {Major}.{Minor}. Deploy matching host and worker versions.");
    }
}

/// <summary>A worker cannot be loaded under the current host contract.</summary>
public sealed class GXAgentContractException : InvalidOperationException
{
    /// <summary>Creates an actionable compatibility error.</summary>
    /// <param name="message">Compatibility failure description.</param>
    public GXAgentContractException(string message) : base(message) { }
}
