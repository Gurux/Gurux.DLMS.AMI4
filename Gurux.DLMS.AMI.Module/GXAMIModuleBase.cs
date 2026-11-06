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

/// <summary>Base module with no dependencies, no scheduled execution, and lifecycle hooks that complete without work.</summary>
public abstract class GXAmiModuleBase : IGXAmiModule
{
    /// <inheritdoc />
    /// <summary>Gets the unique module identifier used by the host.</summary>
    public abstract string Id
    {
        get;
    }
    /// <inheritdoc />
    /// <summary>Gets the module display name.</summary>
    public abstract string Name
    {
        get;
    }
    /// <inheritdoc />
    /// <summary>Gets the description of the module's functionality.</summary>
    public abstract string Description
    {
        get;
    }
    /// <summary>Gets the supported protocol description, or <see langword="null"/> by default.</summary>
    public virtual string? Protocols => null;
    /// <inheritdoc />
    public virtual Task<AmiModuleHelp?> GetHelpAsync(CancellationToken cancellationToken) => Task.FromResult<AmiModuleHelp?>(null);
  
    /// <summary>Gets the module icon, or <see langword="null"/> by default.</summary>
    public virtual string? Icon => null;
  
    /// <summary>Gets the module's mandatory dependencies; empty by default.</summary>
    public virtual IReadOnlyList<AmiModuleDependency> Dependencies => [];
  
    /// <summary>Gets whether scheduled execution is supported; <see langword="false"/> by default.</summary>
    public virtual bool CanSchedule => false;
    /// <inheritdoc />
    /// <summary>Declares the module's services, endpoints, views and owned tables before activation.</summary>
    /// <param name="builder">Builder used to register the module's declarations.</param>
    public virtual void Configure(AmiModuleBuilder builder)
    {
    }
    /// <inheritdoc />
    /// <summary>Creates module defaults during installation; the default implementation completes without changes.</summary>
    /// <param name="context">Host-provided context for the current module operation.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public virtual Task InstallAsync(AmiModuleContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    /// <inheritdoc />
    /// <summary>Updates module resources from the previous version; the default implementation completes without changes.</summary>
    /// <param name="context">Host-provided context for the current module operation.</param>
    /// <param name="previousVersion">Module version installed before this update.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public virtual Task UpdateAsync(AmiModuleContext context, string previousVersion, CancellationToken cancellationToken) => Task.CompletedTask;
    /// <inheritdoc />
    /// <summary>Removes module resources according to the context's uninstall options; the default implementation completes without changes.</summary>
    /// <param name="context">Host-provided context for the current module operation.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public virtual Task UninstallAsync(AmiModuleContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    /// <inheritdoc />
    /// <summary>Starts module operations; the default implementation completes without work.</summary>
    /// <param name="context">Host-provided context for the current module operation.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public virtual Task StartAsync(AmiModuleContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    /// <inheritdoc />
    /// <summary>Applies changed module settings; the default implementation completes without work.</summary>
    /// <param name="context">Host-provided context for the current module operation.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public virtual Task SettingsChangedAsync(AmiModuleContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    /// <inheritdoc />
    /// <summary>Executes a scheduled module operation; the default implementation completes without work.</summary>
    /// <param name="context">Host-provided context for the current module operation.</param>
    /// <param name="instanceSettings">Optional serialized settings for this scheduled execution.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public virtual Task ExecuteAsync(AmiModuleContext context, string? instanceSettings, CancellationToken cancellationToken) => Task.CompletedTask;
    /// <inheritdoc />
    /// <summary>Stops module operations; the default implementation completes without work.</summary>
    /// <param name="context">Host-provided context for the current module operation.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public virtual Task StopAsync(AmiModuleContext context, CancellationToken cancellationToken) => Task.CompletedTask;
}

