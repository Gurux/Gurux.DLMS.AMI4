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

using Gurux.DLMS.AMI.Shared.DTOs.Module;
namespace Gurux.DLMS.AMI.Module;

/// <summary>Base module with no dependencies, no scheduled execution, and lifecycle hooks that complete without work.</summary>
public abstract class GXAmiModuleBase : IAmiModule
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
    /// <summary>Gets the help reference, or <see langword="null"/> by default.</summary>
    public virtual string? Help => null;
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

