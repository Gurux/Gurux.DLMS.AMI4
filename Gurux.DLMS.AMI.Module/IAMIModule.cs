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

/// <summary>
/// Metadata, declarations and asynchronous lifecycle of a runtime module.
/// </summary>
public interface IAmiModule
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
    /// <summary>Help reference for the module, if available.</summary>
    string? Help
    {
        get;
    }
    /// <summary>Module icon, if available.</summary>
    string? Icon
    {
        get;
    }
    /// <summary>Configuration component type, if supported.</summary>
    Type? Configuration
    {
        get;
    }
    /// <summary>UI extension component type, if supported.</summary>
    Type? Extension
    {
        get;
    }
    /// <summary>Scheduling component type, if supported.</summary>
    Type? Schedule
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
    void Configure(AmiModuleBuilder builder);
    /// <summary>Installs the module.</summary>
    Task InstallAsync(AmiModuleContext context, CancellationToken cancellationToken);
    /// <summary>Updates the module from the specified previous version.</summary>
    Task UpdateAsync(AmiModuleContext context, string previousVersion, CancellationToken cancellationToken);
    /// <summary>Uninstalls the module using the context data removal options.</summary>
    Task UninstallAsync(AmiModuleContext context, CancellationToken cancellationToken);
    /// <summary>Starts the module.</summary>
    Task StartAsync(AmiModuleContext context, CancellationToken cancellationToken);
    /// <summary>Applies changed module settings.</summary>
    Task SettingsChangedAsync(AmiModuleContext context, CancellationToken cancellationToken);
    /// <summary>Executes the module with optional instance settings.</summary>
    Task ExecuteAsync(AmiModuleContext context, string? instanceSettings, CancellationToken cancellationToken);
    /// <summary>Stops the module.</summary>
    Task StopAsync(AmiModuleContext context, CancellationToken cancellationToken);
}
