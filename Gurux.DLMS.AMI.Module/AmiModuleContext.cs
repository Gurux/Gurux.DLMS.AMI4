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

using System.Text.Json;

namespace Gurux.DLMS.AMI.Module;

/// <summary>Services and cancellation owned by one module operation.</summary>
public sealed class AmiModuleContext
{
    /// <summary>Validated operations on AMI-owned resources for this module.</summary>
    public IGXAmiModuleResources Resources { get; init; } = null!;
    /// <summary>Host-managed user data restricted to this module.</summary>
    public IGXAmiModuleUserSettings UserSettings { get; init; } = null!;
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
    public required IGXAmiModuleSettingsSnapshot Settings
    {
        get; init;
    }

    /// <summary>Cancellation token for the current module operation.</summary>
    public required CancellationToken CancellationToken
    {
        get; init;
    }

    /// <summary>Caller cancellation only.</summary>
    public CancellationToken RequestCancellationToken { get; init; }
    /// <summary>Graceful shutdown requested.</summary>
    public CancellationToken StoppingToken { get; init; }
    /// <summary>Shutdown deadline expired.</summary>
    public CancellationToken ForcedCancellationToken { get; init; }
    /// <summary>Module-owned event subscriptions.</summary>
    public IGXAmiEvents Events { get; init; } = AmiEventHub.None;

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
