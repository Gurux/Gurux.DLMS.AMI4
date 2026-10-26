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

