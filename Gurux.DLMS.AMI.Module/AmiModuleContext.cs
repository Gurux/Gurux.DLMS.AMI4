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
using System.Text.Json;

namespace Gurux.DLMS.AMI.Module;

/// <summary>Services and cancellation owned by one module operation.</summary>
public sealed class AmiModuleContext
{
    /// <summary>Validated operations on AMI-owned resources for this module.</summary>
    public IAmiModuleResources Resources { get; init; } = null!;
    /// <summary>Host-managed user data restricted to this module.</summary>
    public IAmiModuleUserSettings UserSettings { get; init; } = null!;
    /// <summary>Unique module identifier.</summary>
    public required string ModuleId
    {
        get; init;
    }

    /// <summary>Data removal options supplied to uninstallation; defaults to preserving module data.</summary>
    public AmiModuleUninstallOptions UninstallOptions { get; set; } = new();

    /// <summary>Services registered in the module scope.</summary>
    public required IServiceProvider Services
    {
        get; init;
    }

    /// <summary>Services provided by the host.</summary>
    public required IServiceProvider HostServices
    {
        get; init;
    }

    /// <summary>Module settings snapshot.</summary>
    public required IAmiModuleSettingsSnapshot Settings
    {
        get; init;
    }

    /// <summary>Cancellation token for the current module operation.</summary>
    public required CancellationToken CancellationToken
    {
        get; init;
    }

    /// <summary>Serializer options for the current module operation.</summary>
    public required JsonSerializerOptions JsonOptions
    {
        get; init;
    }

    /// <summary>Module package directory, or <see langword="null"/> when unavailable; use this instead of the location of a stream-loaded assembly.</summary>
    public string? PackageDirectory
    {
        get; init;
    }

    /// <summary>Registers resources for disposal at module stop; the default delegate throws when no host resource owner is configured.</summary>
    public Action<IDisposable> Track { get; init; } = _ => throw new InvalidOperationException("This context has no resource owner.");

    /// <summary>Tracks a resource for disposal when the module stops.</summary>
    /// <param name="resource">Resource whose disposal is owned by the module context.</param>
    public void Own(IDisposable resource) => Track(resource);
}
