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

using Gurux.DLMS.AMI.Shared.DTOs.Module;
namespace Gurux.DLMS.AMI.Module;

/// <summary>
/// Metadata, declarations and asynchronous lifecycle of a runtime module.
/// </summary>
public interface IGXAmiModule
{
    /// <summary>Unique module identifier.</summary>
    string Id
    {
        get;
    }
    /// <summary>Display name of the module.</summary>
    string Name
    {
        get;
    }
    /// <summary>Description of the module.</summary>
    string Description
    {
        get;
    }
    /// <summary>Protocols supported by the module, if specified.</summary>
    string? Protocols
    {
        get;
    }

    /// <summary>Module icon, if available.</summary>
    string? Icon
    {
        get;
    }

    /// <summary>Whether the module supports scheduled execution.</summary>
    bool CanSchedule
    {
        get;
    }
    /// <summary>Mandatory dependencies required by the module.</summary>
    IReadOnlyList<AmiModuleDependency> Dependencies
    {
        get;
    }
    /// <summary>Declares module services, endpoints and UI components.</summary>
    /// <param name="builder">Builder used to register the module's declarations.</param>
    void Configure(AmiModuleBuilder builder);
    /// <summary>Installs the module.</summary>
    /// <param name="context">Host-provided context for the current module operation.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task InstallAsync(AmiModuleContext context, CancellationToken cancellationToken);
    /// <summary>Updates the module from the specified previous version.</summary>
    /// <param name="context">Host-provided context for the current module operation.</param>
    /// <param name="previousVersion">Module version installed before this update.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task UpdateAsync(AmiModuleContext context, string previousVersion, CancellationToken cancellationToken);
    /// <summary>Uninstalls the module using the context data removal options.</summary>
    /// <param name="context">Host-provided context for the current module operation.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task UninstallAsync(AmiModuleContext context, CancellationToken cancellationToken);
    /// <summary>Starts the module.</summary>
    /// <param name="context">Host-provided context for the current module operation.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task StartAsync(AmiModuleContext context, CancellationToken cancellationToken);
    /// <summary>Applies changed module settings.</summary>
    /// <param name="context">Host-provided context for the current module operation.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task SettingsChangedAsync(AmiModuleContext context, CancellationToken cancellationToken);
    /// <summary>Executes the module with optional instance settings.</summary>
    /// <param name="context">Host-provided context for the current module operation.</param>
    /// <param name="instanceSettings">Optional serialized settings for this scheduled execution.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task ExecuteAsync(AmiModuleContext context, string? instanceSettings, CancellationToken cancellationToken);
    /// <summary>Stops the module.</summary>
    /// <param name="context">Host-provided context for the current module operation.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task StopAsync(AmiModuleContext context, CancellationToken cancellationToken);
}

