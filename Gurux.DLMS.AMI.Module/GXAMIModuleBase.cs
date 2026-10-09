using Gurux.DLMS.AMI.Shared.DTOs.Module;
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
namespace Gurux.DLMS.AMI.Module;

/// <summary>Convenient defaults for modules with optional capabilities.</summary>
public abstract class GXAmiModuleBase : IAmiModule
{
    /// <inheritdoc />
    public abstract string Id
    {
        get;
    }
    /// <inheritdoc />
    public abstract string Name
    {
        get;
    }
    /// <inheritdoc />
    public abstract string Description
    {
        get;
    }
    /// <inheritdoc />
    public virtual string? Protocols => null;
    /// <inheritdoc />
    public virtual string? Help => null;
    /// <inheritdoc />
    public virtual string? Icon => null;
    /// <inheritdoc />
    public virtual Type? Configuration => null;
    /// <inheritdoc />
    public virtual Type? Extension => null;
    /// <inheritdoc />
    public virtual Type? Schedule => null;
    /// <inheritdoc />
    public virtual IReadOnlyList<AmiModuleDependency> Dependencies => [];
    /// <inheritdoc />
    public virtual bool CanSchedule => Schedule != null;
    /// <inheritdoc />
    public virtual void Configure(AmiModuleBuilder builder)
    {
    }
    /// <inheritdoc />
    public virtual Task InstallAsync(AmiModuleContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    /// <inheritdoc />
    public virtual Task UpdateAsync(AmiModuleContext context, string previousVersion, CancellationToken cancellationToken) => Task.CompletedTask;
    /// <inheritdoc />
    public virtual Task UninstallAsync(AmiModuleContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    /// <inheritdoc />
    public virtual Task StartAsync(AmiModuleContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    /// <inheritdoc />
    public virtual Task SettingsChangedAsync(AmiModuleContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    /// <inheritdoc />
    public virtual Task ExecuteAsync(AmiModuleContext context, string? instanceSettings, CancellationToken cancellationToken) => Task.CompletedTask;
    /// <inheritdoc />
    public virtual Task StopAsync(AmiModuleContext context, CancellationToken cancellationToken) => Task.CompletedTask;
}
